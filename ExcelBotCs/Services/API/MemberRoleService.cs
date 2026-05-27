using ExcelBotCs.Caching;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Services.API.Interfaces;

namespace ExcelBotCs.Services.API;

public class MemberRoleService : IMemberRoleService
{
    private readonly IMemberRoleRepository _memberRoleRepository;
    private readonly ICacheAccessor<MemberRole> _cache;
    private readonly IEntityCacheService _cacheService;

    public MemberRoleService(
        IMemberRoleRepository memberRoleRepository,
        ICacheAccessor<MemberRole> cache,
        IEntityCacheService cacheService)
    {
        _memberRoleRepository = memberRoleRepository;
        _cache = cache;
        _cacheService = cacheService;
    }

    public async Task<List<MemberRole>> GetAsync()
    {
        var cached = _cache.GetAll();
        if (cached.Count > 0) return cached;
        return await _memberRoleRepository.GetAsync();
    }

    public async Task<MemberRole?> GetAsync(string id)
    {
        return _cache.GetById(id) ?? await _memberRoleRepository.GetAsync(id);
    }

    public async Task CreateAsync(MemberRole memberRole)
    {
        await _memberRoleRepository.CreateAsync(memberRole);
        _cache.Update(memberRole);
        await _cacheService.FillAsync("Member");
    }

    public async Task UpdateAsync(string id, MemberRole updatedMemberRole)
    {
        await _memberRoleRepository.UpdateAsync(id, updatedMemberRole);
        _cache.Update(updatedMemberRole);
        await _cacheService.FillAsync("Member");
    }

    public async Task DeleteAsync(string id)
    {
        await _memberRoleRepository.DeleteAsync(id);
        _cache.Remove(id);
        await _cacheService.FillAsync("Member");
    }

    public async Task<MemberRole> GetByDiscordId(string discordId)
    {
        if (_cache.IsPopulated)
        {
            var cached = _cache.GetAll()
                .FirstOrDefault(r => r.DiscordId == discordId);
            if (cached != null) return cached;
        }

        return await _memberRoleRepository.GetByDiscordId(discordId);
    }
}