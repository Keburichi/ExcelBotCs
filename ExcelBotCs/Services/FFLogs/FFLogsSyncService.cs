using ExcelBotCs.Database;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Extensions;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database;
using Microsoft.Extensions.Options;

namespace ExcelBotCs.Services.FFLogs;

public class FFLogsSyncService
{
    private readonly FFLogsOptions _options;
    private readonly FFLogsGraphQLService _graphQLService;
    private readonly IFightRepository _fightRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly IFFLogsImportLogRepository _importLogRepository;
    private readonly ILogger<FFLogsSyncService> _logger;

    private DateTime _lastFightImportTime = DateTime.MinValue;
    private readonly TimeSpan _fightImportInterval = TimeSpan.FromHours(24); // Daily import

    public FFLogsSyncService(
        IOptions<FFLogsOptions> options,
        FFLogsGraphQLService graphQLService,
        IFightRepository fightRepository,
        IMemberRepository memberRepository,
        IFFLogsImportLogRepository importLogRepository,
        ILogger<FFLogsSyncService> logger)
    {
        _options = options.Value;
        _graphQLService = graphQLService;
        _fightRepository = fightRepository;
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

            // Get existing fights with FFLogs IDs
            var existingFights = await _fightRepository.GetAsync();
            
            var existingFFLogsIds = new HashSet<int>(
                existingFights
                    .Where(f => f.FFLogsEncounterId.HasValue)
                    .Select(f => f.FFLogsEncounterId!.Value)
            );

            // Process all expansions and zones in chronological order
            foreach (var expansion in worldData.worldData.expansions.OrderBy(x => x.id))
            {
                var isLatestExpansion = !worldData.worldData.expansions.Any(x => x.id > expansion.id);

                foreach (var zone in expansion.zones)
                {
                    // Determine fight type early to filter
                    var fightMapping = MapFightType(zone.name, zone.difficulties, isLatestExpansion);

                    // Skip non-high-end content (Normal, Chaotic, etc.)
                    if (fightMapping.fightType != FightType.Extreme &&
                        fightMapping.fightType != FightType.Savage &&
                        fightMapping.fightType != FightType.LegacySavage &&
                        fightMapping.fightType != FightType.Ultimate && 
                        fightMapping.fightType != FightType.Chaotic)
                    {
                        _logger.LogDebug("Skipping non-high-end zone: {ZoneName} (Type: {FightType})",
                            zone.name, fightMapping.fightType);
                        continue;
                    }

                    foreach (var encounter in zone.encounters)
                    {
                        log.ItemsProcessed++;

                        // Skip if already exists
                        if (existingFFLogsIds.Contains(encounter.id))
                        {
                            log.ItemsSkipped++;
                            continue;
                        }

                        // Create new fight
                        var fight = new Fight
                        {
                            Name = encounter.name,
                            Description = $"{zone.name} - {expansion.name}",
                            ImageUrl = string.Empty, // No image URL from FFLogs API
                            Type = fightMapping.fightType,
                            Raidplans = new List<Raidplan>(),
                            FFLogsEncounterId = encounter.id,
                            FFLogsZoneId = zone.id,
                            FFLogsZoneName = zone.name,
                            FFLogsDifficultyId = fightMapping.difficulty.id,
                            FFLogsExpansionId = expansion.id,
                            FFLogsExpansionName = expansion.name,
                            IsFrozen = zone.frozen
                        };

                        await _fightRepository.CreateAsync(fight);
                        log.ItemsUpdated++;

                        _logger.LogDebug(
                            "Imported fight: {FightName} (ID: {EncounterId}, Zone: {ZoneName}, Type: {FightType})",
                            encounter.name, encounter.id, zone.name, fightMapping.fightType);
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
                .Where(f => f.FFLogsEncounterId.HasValue)
                .ToDictionary(f => f.FFLogsEncounterId!.Value, f => f);

            foreach (var member in membersToSync)
            {
                log.ItemsProcessed++;

                try
                {
                    if(member.LodestoneId.IsNullOrEmpty())
                        continue;
                    
                    if (!long.TryParse(member.LodestoneId, out var lodestoneId))
                    {
                        _logger.LogWarning("Invalid Lodestone ID for member {MemberId}: {LodestoneId}",
                            member.Id, member.LodestoneId);
                        log.ItemsSkipped++;
                        continue;
                    }

                    // gather which fights haven't been cleared yet that still accepts logs
                    var unclearedFights = new List<Fight>();
                    if(member.Experience != null && !member.Experience.IsNullOrEmpty())
                        unclearedFights = allFights.Where(x => !x.IsFrozen && member.ExperienceIds.All(e => e != x.Id)).ToList();
                    else
                        unclearedFights = allFights;
                    
                    // Since FFLogs returns all fights for a specific zone we only need to get a list of distinct zone ids
                    // to gather all information required
                    var unclearedFightZones = unclearedFights.Select(x => x.FFLogsZoneId).Distinct().ToList();
                    
                    var updatedExperience = new List<string>();
                    var newClears = 0;

                    foreach (var unclearedFightZone in unclearedFightZones)
                    {
                        // gather information for all uncleared fights
                        var characterData = await _graphQLService.GetCharacterActivityAsync(lodestoneId,
                            unclearedFightZone,
                            unclearedFights.First(x => x.FFLogsZoneId == unclearedFightZone).FFLogsDifficultyId);

                        // Parse the JSON zoneRankings data
                        var zoneRankings = characterData.characterData.character?.GetZoneRankings();

                        if (zoneRankings?.rankings == null || !zoneRankings.rankings.Any())
                        {
                            continue;
                        }

                        // Process each cleared encounter
                        foreach (var encounterRanking in zoneRankings.rankings)
                        {
                            if(encounterRanking.totalKills == 0)
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
                                        member.DiscordName, fight.Name, encounterRanking.bestAmount, encounterRanking.rankPercent);
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

    /// <summary>
    /// Maps FFLogs zone and difficulty information to FightType enum
    /// </summary>
    private static (FightType fightType, Difficulty difficulty) MapFightType(string zoneName, List<Difficulty> difficulties, bool isLatestExpansion)
    {
        var lowerZoneName = zoneName.ToLowerInvariant();
        var lowerDifficultyNames = difficulties.Select(x => x.name.ToLowerInvariant()).ToList();

        // If the difficulty for savage exists, we know this is a savage fight
        if (lowerDifficultyNames.Contains("savage"))
        {
            return isLatestExpansion
                ? (FightType.Savage, difficulties.First(x => x.name.ToLowerInvariant() == "savage"))
                : (FightType.LegacySavage, difficulties.First(x => x.name.ToLowerInvariant() == "savage"));
        }

        // Check for Ultimate (highest priority)
        if (lowerZoneName.Contains("ultimate"))
            return (FightType.Ultimate, difficulties.First());
        
        // Check for Extreme
        if (lowerZoneName.Contains("extreme") || lowerZoneName.Contains("minstrel"))
            return (FightType.Extreme,  difficulties.First());
        
        // Check for chaotic
        if(lowerZoneName.Contains("chaotic"))
            return (FightType.Chaotic, difficulties.First());
        
        // Check for savage
        if(lowerZoneName.Contains("savage"))
            return isLatestExpansion
                ? (FightType.Savage, difficulties.First())
                : (FightType.LegacySavage, difficulties.First());

        // This is a last ditch effort since the FFLogs API is kinda shite to identify 
        // special fights that do not have their difficulty mentioned in the name or zone
        if (lowerZoneName.Contains("futures rewritten")
            || lowerZoneName.Contains("omega protocol")
            || lowerZoneName.Contains("dragonsong's reprise")
            || lowerZoneName.Contains("the epic of alexander")
            || lowerZoneName.Contains("the unending coil of bahamut")
            || lowerZoneName.Contains("the weapon's refrain"))
            return (FightType.Ultimate, difficulties.First());

        // Default to Normal for all other content
        return (FightType.Normal, difficulties.First());
    }
}