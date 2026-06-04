using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Extensions;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Utilities;
using Microsoft.Extensions.Options;

namespace ExcelBotCs.Services.FFLogs;

public class FFLogsSyncService
{
    private readonly FFLogsOptions _options;
    private readonly FFLogsGraphQLService _graphQLService;
    private readonly IFightRepository _fightRepository;
    private readonly IBossRepository _bossRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly IFFLogsImportLogRepository _importLogRepository;
    private readonly ILogger<FFLogsSyncService> _logger;

    private DateTime _lastFightImportTime = DateTime.MinValue;
    private readonly TimeSpan _fightImportInterval = TimeSpan.FromHours(24); // Daily import

    public FFLogsSyncService(
        IOptions<FFLogsOptions> options,
        FFLogsGraphQLService graphQLService,
        IFightRepository fightRepository,
        IBossRepository bossRepository,
        IMemberRepository memberRepository,
        IFFLogsImportLogRepository importLogRepository,
        ILogger<FFLogsSyncService> logger)
    {
        _options = options.Value;
        _graphQLService = graphQLService;
        _fightRepository = fightRepository;
        _bossRepository = bossRepository;
        _memberRepository = memberRepository;
        _importLogRepository = importLogRepository;
        _logger = logger;
    }

    /// <summary>
    /// Imports/updates fights from FFLogs (runs daily)
    /// </summary>
    public async Task SyncFightsAsync()
    {
        // Check if we should run the import (daily)
        if (DateTime.UtcNow - _lastFightImportTime < _fightImportInterval)
        {
            _logger.LogDebug("Skipping fight import - last import was at {LastImportTime}", _lastFightImportTime);
            return;
        }

        var log = new FFLogsImportLog
        {
            StartTime = DateTime.UtcNow,
            ImportType = FFLogsImportType.FightImport,
            ItemsProcessed = 0,
            ItemsUpdated = 0,
            ItemsSkipped = 0,
            ApiRequestCount = 0,
            Success = false
        };

        try
        {
            _logger.LogInformation("Starting FFLogs fight import");

            // Fetch world data from FFLogs
            var worldData = await _graphQLService.GetWorldDataAsync();
            log.ApiRequestCount++;

            // Get existing fights keyed by (EncounterId, DifficultyId) composite
            var existingFights = await _fightRepository.GetAsync();

            var existingKeys = new HashSet<(int encounterId, int difficultyId)>(
                existingFights
                    .Where(f => f.FFLogsEncounterId.HasValue && f.FFLogsDifficultyId.HasValue)
                    .Select(f => (f.FFLogsEncounterId!.Value, f.FFLogsDifficultyId!.Value))
            );

            // Cache bosses by normalization key to avoid repeated DB lookups
            var bossCache = new Dictionary<string, Boss>();

            // Process all expansions and zones in chronological order
            foreach (var expansion in worldData.worldData.expansions.OrderBy(x => x.id))
            {
                foreach (var zone in expansion.zones)
                {
                    // Each zone can have multiple difficulties — create a fight per (encounter, difficulty)
                    var difficultyMappings = MapFightTypes(zone.name, zone.difficulties);

                    foreach (var (fightType, difficulty) in difficultyMappings)
                    {
                        foreach (var encounter in zone.encounters)
                        {
                            log.ItemsProcessed++;

                            // Skip if this (encounter, difficulty) combo already exists
                            if (existingKeys.Contains((encounter.id, difficulty.id)))
                            {
                                log.ItemsSkipped++;
                                continue;
                            }

                            // Find or create the parent Boss
                            var boss = await GetOrCreateBossAsync(
                                encounter.name, expansion.id, fightType, bossCache);

                            // Create new fight linked to boss
                            var fight = new Fight
                            {
                                Name = encounter.name,
                                Type = fightType,
                                Raidplans = new List<Raidplan>(),
                                BossId = boss.Id,
                                FFLogsEncounterId = encounter.id,
                                FFLogsZoneId = zone.id,
                                FFLogsZoneName = zone.name,
                                FFLogsDifficultyId = difficulty.id,
                                FFLogsExpansionId = expansion.id,
                                FFLogsExpansionName = expansion.name,
                                IsFrozen = zone.frozen
                            };

                            await _fightRepository.CreateAsync(fight);
                            log.ItemsUpdated++;

                            _logger.LogDebug(
                                "Imported fight: {FightName} (ID: {EncounterId}, Zone: {ZoneName}, Type: {FightType}, Boss: {BossName})",
                                encounter.name, encounter.id, zone.name, fightType, boss.Name);
                        }
                    }
                }
            }

            log.Success = true;
            _lastFightImportTime = DateTime.UtcNow;

            _logger.LogInformation(
                "FFLogs fight import completed. Processed: {Processed}, Updated: {Updated}, Skipped: {Skipped}",
                log.ItemsProcessed, log.ItemsUpdated, log.ItemsSkipped);
        }
        catch (Exception ex)
        {
            log.Success = false;
            log.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Error during FFLogs fight import");
        }
        finally
        {
            log.EndTime = DateTime.UtcNow;
            await _importLogRepository.CreateAsync(log);
        }
    }

    /// <summary>
    /// Syncs member activity from FFLogs in waves
    /// </summary>
    public async Task SyncMemberActivityAsync()
    {
        var log = new FFLogsImportLog
        {
            StartTime = DateTime.UtcNow,
            ImportType = FFLogsImportType.MemberActivitySync,
            ItemsProcessed = 0,
            ItemsUpdated = 0,
            ItemsSkipped = 0,
            ApiRequestCount = 0,
            Success = false
        };

        try
        {
            _logger.LogInformation("Starting FFLogs member activity sync");

            // Get all members with Lodestone IDs, ordered by last sync time (oldest first)
            var allMembers = await _memberRepository.GetAsync();
            var membersToSync = allMembers
                .Where(m => !string.IsNullOrEmpty(m.LodestoneId))
                .OrderBy(m => m.LastFFLogsSyncTime ?? DateTime.MinValue)
                .Take(_options.MembersPerWave)
                .ToList();

            if (!membersToSync.Any())
            {
                _logger.LogInformation("No members to sync");
                log.Success = true;
                return;
            }

            _logger.LogInformation("Syncing {Count} members", membersToSync.Count);

            // Get all fights for matching
            var allFights = await _fightRepository.GetAsync();
            var fightsByFFLogsId = allFights
                .Where(f => f.FFLogsEncounterId.HasValue && f.IsFightDifficult())
                .ToDictionary(f => f.FFLogsEncounterId!.Value, f => f);

            foreach (var member in membersToSync)
            {
                log.ItemsProcessed++;

                try
                {
                    if (member.LodestoneId.IsNullOrEmpty())
                        continue;

                    if (!long.TryParse(member.LodestoneId, out var lodestoneId))
                    {
                        _logger.LogWarning("Invalid Lodestone ID for member {MemberId}: {LodestoneId}",
                            member.Id, member.LodestoneId);
                        log.ItemsSkipped++;
                        continue;
                    }

                    // Only sync high-end content for member activity
                    var highEndTypes = new HashSet<FightType>
                    {
                        FightType.Savage, FightType.Ultimate, FightType.Extreme, FightType.Chaotic
                    };

                    // Gather which fights haven't been cleared yet that still accept logs
                    var unclearedFights = allFights
                        .Where(x => highEndTypes.Contains(x.Type)
                                    && !x.IsFrozen
                                    && !(member.ExperienceIds ?? new List<string>()).Contains(x.Id))
                        .ToList();

                    // Build zone query requests — group by (ZoneId, DifficultyId) since
                    // the same zone can have multiple difficulties
                    var zoneRequests = unclearedFights
                        .Where(x => x.FFLogsZoneId.HasValue && x.FFLogsDifficultyId.HasValue)
                        .GroupBy(x => (x.FFLogsZoneId!.Value, x.FFLogsDifficultyId!.Value))
                        .Select(g => new ZoneQueryRequest
                        {
                            ZoneId = g.Key.Item1,
                            DifficultyId = g.Key.Item2
                        })
                        .ToList();

                    var updatedExperience = new List<string>();
                    var newClears = 0;

                    // Fetch all zone data in a single batched request
                    var batchedZoneRankings =
                        await _graphQLService.GetCharacterActivityBatchedAsync(lodestoneId, zoneRequests);
                    log.ApiRequestCount++;

                    // Process each zone+difficulty's rankings
                    foreach (var entry in batchedZoneRankings)
                    {
                        var zoneRankings = entry.Value;
                        if (zoneRankings?.rankings == null || !zoneRankings.rankings.Any())
                        {
                            continue;
                        }

                        // Process each cleared encounter
                        foreach (var encounterRanking in zoneRankings.rankings)
                        {
                            if (encounterRanking.totalKills == 0)
                                continue;

                            if (fightsByFFLogsId.TryGetValue(encounterRanking.encounter.id, out var fight))
                            {
                                // Check if member already has this fight in their experience
                                var hasExperience = member.ExperienceIds?.Any(f => f == fight.Id) ?? false;

                                if (!hasExperience)
                                {
                                    updatedExperience.Add(fight.Id);
                                    newClears++;

                                    _logger.LogInformation(
                                        "Member {MemberName} cleared {FightName} - Best: {BestAmount:F2}, Rank: {RankPercent:F2}%",
                                        member.DiscordName, fight.Name, encounterRanking.bestAmount,
                                        encounterRanking.rankPercent);
                                }
                            }
                        }
                    }

                    // Update member experience if there are new clears
                    if (newClears > 0)
                    {
                        member.ExperienceIds ??= new List<string>();
                        member.ExperienceIds.AddRange(updatedExperience);
                        member.LastFFLogsSyncTime = DateTime.UtcNow;
                        await _memberRepository.UpdateAsync(member.Id, member);

                        log.ItemsUpdated++;
                        _logger.LogInformation("Updated member {MemberId} with {NewClears} new clears",
                            member.Id, newClears);
                    }
                    else
                    {
                        // Still update sync time even if no new clears
                        member.LastFFLogsSyncTime = DateTime.UtcNow;
                        await _memberRepository.UpdateAsync(member.Id, member);

                        log.ItemsSkipped++;
                        _logger.LogDebug("No new clears for member {MemberId}", member.Id);
                    }

                    // Add delay between members
                    await Task.Delay(_options.DelayBetweenRequestsMs);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error syncing member {MemberId}", member.Id);
                }
            }

            log.Success = true;

            _logger.LogInformation(
                "FFLogs member activity sync completed. Processed: {Processed}, Updated: {Updated}, Skipped: {Skipped}, API Requests: {ApiRequests}",
                log.ItemsProcessed, log.ItemsUpdated, log.ItemsSkipped, log.ApiRequestCount);
        }
        catch (Exception ex)
        {
            log.Success = false;
            log.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Error during FFLogs member activity sync");
        }
        finally
        {
            log.EndTime = DateTime.UtcNow;
            await _importLogRepository.CreateAsync(log);
        }
    }

    private async Task<Boss> GetOrCreateBossAsync(
        string encounterName, int expansionId, FightType fightType, Dictionary<string, Boss> cache)
    {
        var normalizationKey = FightNormalization.GetNormalizationKey(encounterName);

        if (cache.TryGetValue(normalizationKey, out var cached))
            return cached;

        var existing = await _bossRepository.GetByNormalizationKeyAsync(normalizationKey);
        if (existing != null)
        {
            cache[normalizationKey] = existing;
            return existing;
        }

        var boss = new Boss
        {
            Name = FightNormalization.GetCanonicalBossName(encounterName),
            NormalizationKey = normalizationKey,
            FFLogsExpansionId = expansionId,
            IsUltimate = fightType == FightType.Ultimate
        };

        await _bossRepository.CreateAsync(boss);
        cache[normalizationKey] = boss;

        _logger.LogDebug("Created boss: {BossName} (Key: {NormalizationKey})", boss.Name, normalizationKey);
        return boss;
    }

    /// <summary>
    /// Maps FFLogs zone and difficulty information to FightType entries.
    /// Returns one entry per difficulty in the zone.
    /// </summary>
    private static List<(FightType fightType, Difficulty difficulty)> MapFightTypes(string zoneName,
        List<Difficulty> difficulties)
    {
        var lowerZoneName = zoneName.ToLowerInvariant();

        // Ultimates identified by zone name — always a single difficulty
        if (lowerZoneName.Contains("ultimate")
            || lowerZoneName.Contains("futures rewritten")
            || lowerZoneName.Contains("omega protocol")
            || lowerZoneName.Contains("dragonsong's reprise")
            || lowerZoneName.Contains("the epic of alexander")
            || lowerZoneName.Contains("the unending coil of bahamut")
            || lowerZoneName.Contains("the weapon's refrain")
            || lowerZoneName.Contains("dancing mad"))
            return [(FightType.Ultimate, difficulties.First())];

        if (lowerZoneName.Contains("unreal"))
            return [(FightType.Unreal, difficulties.First())];

        if (lowerZoneName.Contains("extreme") || lowerZoneName.Contains("minstrel"))
            return [(FightType.Extreme, difficulties.First())];

        if (lowerZoneName.Contains("chaotic"))
            return [(FightType.Chaotic, difficulties.First())];

        // For zones with multiple difficulties (e.g., Normal + Savage), create an entry per difficulty
        var results = new List<(FightType fightType, Difficulty difficulty)>();

        foreach (var difficulty in difficulties)
        {
            var fightType = difficulty.name.ToLowerInvariant() switch
            {
                "savage" => FightType.Savage,
                "normal" => FightType.Normal,
                _ => FightType.Normal
            };
            results.Add((fightType, difficulty));
        }

        // If no difficulties listed, default to Normal
        if (results.Count == 0)
            results.Add((FightType.Normal, new Difficulty { id = 0, name = "Normal" }));

        return results;
    }
}