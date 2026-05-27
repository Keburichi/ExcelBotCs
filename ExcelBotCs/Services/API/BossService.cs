using ExcelBotCs.Caching;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.Utilities;

namespace ExcelBotCs.Services.API;

public class BossService : IBossService
{
    private readonly IBossRepository _bossRepository;
    private readonly ICacheAccessor<Boss> _cache;

    public BossService(IBossRepository bossRepository, ICacheAccessor<Boss> cache)
    {
        _bossRepository = bossRepository;
        _cache = cache;
    }

    public async Task<List<Boss>> GetBossesAsync()
    {
        var cached = _cache.GetAll();
        if (cached.Count > 0) return cached;
        return await _bossRepository.GetAsync();
    }

    public async Task<Boss?> GetBossAsync(string id)
    {
        return _cache.GetById(id) ?? await _bossRepository.GetAsync(id);
    }

    public async Task<Boss?> GetByNormalizationKeyAsync(string normalizationKey)
    {
        if (_cache.IsPopulated)
        {
            var cached = _cache.GetAll()
                .FirstOrDefault(b => b.NormalizationKey == normalizationKey);
            if (cached != null) return cached;
        }

        return await _bossRepository.GetByNormalizationKeyAsync(normalizationKey);
    }

    public async Task CreateAsync(Boss boss)
    {
        await _bossRepository.CreateAsync(boss);
        _cache.Update(boss);
    }

    public async Task UpdateAsync(string id, Boss updatedBoss)
    {
        await _bossRepository.UpdateAsync(id, updatedBoss);
        _cache.Update(updatedBoss);
    }

    public async Task DeleteAsync(string id)
    {
        await _bossRepository.DeleteAsync(id);
        _cache.Remove(id);
    }

    public async Task<Boss> GetOrCreateAsync(string encounterName, int? expansionId, bool isUltimate)
    {
        var normalizationKey = FightNormalization.GetNormalizationKey(encounterName);
        var existing = await GetByNormalizationKeyAsync(normalizationKey);

        if (existing != null)
            return existing;

        var boss = new Boss
        {
            Name = FightNormalization.GetCanonicalBossName(encounterName),
            NormalizationKey = normalizationKey,
            FFLogsExpansionId = expansionId,
            IsUltimate = isUltimate
        };

        await CreateAsync(boss);
        return boss;
    }
}
