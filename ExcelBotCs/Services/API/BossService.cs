using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.Utilities;

namespace ExcelBotCs.Services.API;

public class BossService : IBossService
{
    private readonly IBossRepository _bossRepository;

    public BossService(IBossRepository bossRepository)
    {
        _bossRepository = bossRepository;
    }

    public async Task<List<Boss>> GetBossesAsync()
    {
        return await _bossRepository.GetAsync();
    }

    public async Task<Boss?> GetBossAsync(string id)
    {
        return await _bossRepository.GetAsync(id);
    }

    public async Task<Boss?> GetByNormalizationKeyAsync(string normalizationKey)
    {
        return await _bossRepository.GetByNormalizationKeyAsync(normalizationKey);
    }

    public async Task CreateAsync(Boss boss)
    {
        await _bossRepository.CreateAsync(boss);
    }

    public async Task UpdateAsync(string id, Boss updatedBoss)
    {
        await _bossRepository.UpdateAsync(id, updatedBoss);
    }

    public async Task DeleteAsync(string id)
    {
        await _bossRepository.DeleteAsync(id);
    }

    public async Task<Boss> GetOrCreateAsync(string encounterName, int? expansionId, bool isUltimate)
    {
        var normalizationKey = FightNormalization.GetNormalizationKey(encounterName);
        var existing = await _bossRepository.GetByNormalizationKeyAsync(normalizationKey);

        if (existing != null)
            return existing;

        var boss = new Boss
        {
            Name = FightNormalization.GetCanonicalBossName(encounterName),
            NormalizationKey = normalizationKey,
            FFLogsExpansionId = expansionId,
            IsUltimate = isUltimate
        };

        await _bossRepository.CreateAsync(boss);
        return boss;
    }
}
