using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Utilities;

namespace ExcelBotCs.Services.Migration;

public class BossMigrationService
{
    private readonly IFightRepository _fightRepository;
    private readonly IBossRepository _bossRepository;
    private readonly IResourceRepository _resourceRepository;
    private readonly ILogger<BossMigrationService> _logger;

    public BossMigrationService(
        IFightRepository fightRepository,
        IBossRepository bossRepository,
        IResourceRepository resourceRepository,
        ILogger<BossMigrationService> logger)
    {
        _fightRepository = fightRepository;
        _bossRepository = bossRepository;
        _resourceRepository = resourceRepository;
        _logger = logger;
    }

    public async Task<MigrationResult> MigrateAsync()
    {
        var result = new MigrationResult();

        _logger.LogInformation("Starting Boss migration");

        var allFights = await _fightRepository.GetAsync();
        var bossCache = new Dictionary<string, Boss>();

        foreach (var fight in allFights)
        {
            try
            {
                // Skip fights that are already linked to a boss
                if (!string.IsNullOrEmpty(fight.BossId))
                {
                    result.FightsSkipped++;
                    continue;
                }

                // Determine boss name from fight name
                var bossName = FightNormalization.GetCanonicalBossName(fight.Name);
                var normalizationKey = FightNormalization.GetNormalizationKey(bossName);

                // Find or create boss
                if (!bossCache.TryGetValue(normalizationKey, out var boss))
                {
                    boss = await _bossRepository.GetByNormalizationKeyAsync(normalizationKey);
                    if (boss == null)
                    {
                        boss = new Boss
                        {
                            Name = bossName,
                            NormalizationKey = normalizationKey,
                            FFLogsExpansionId = fight.FFLogsExpansionId,
                            IsUltimate = fight.Type == FightType.Ultimate
                        };

                        await _bossRepository.CreateAsync(boss);
                        result.BossesCreated++;
                        _logger.LogDebug("Created boss: {BossName}", boss.Name);
                    }

                    bossCache[normalizationKey] = boss;
                }

                // Link fight to boss
                fight.BossId = boss.Id;

                // Migrate raidplans to resources
                if (fight.Raidplans != null && fight.Raidplans.Any())
                {
                    foreach (var raidplan in fight.Raidplans)
                    {
                        var resource = new Resource
                        {
                            Name = raidplan.Name,
                            Description = raidplan.Description,
                            Url = raidplan.Url,
                            Type = ResourceType.Raidplan,
                            FightId = fight.Id,
                            AuthorId = raidplan.AuthorId
                        };

                        await _resourceRepository.CreateAsync(resource);
                        result.ResourcesMigrated++;
                    }
                }

                await _fightRepository.UpdateAsync(fight.Id, fight);
                result.FightsLinked++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error migrating fight: {FightName} ({FightId})", fight.Name, fight.Id);
                result.Errors++;
            }
        }

        _logger.LogInformation(
            "Migration complete. Bosses created: {BossesCreated}, Fights linked: {FightsLinked}, " +
            "Resources migrated: {ResourcesMigrated}, Skipped: {Skipped}, Errors: {Errors}",
            result.BossesCreated, result.FightsLinked, result.ResourcesMigrated,
            result.FightsSkipped, result.Errors);

        return result;
    }
}

public class MigrationResult
{
    public int BossesCreated { get; set; }
    public int FightsLinked { get; set; }
    public int FightsSkipped { get; set; }
    public int ResourcesMigrated { get; set; }
    public int Errors { get; set; }
}
