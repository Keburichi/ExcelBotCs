using ExcelBotCs.Caching;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Exceptions;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO.Members;
using ExcelBotCs.Services.API.Interfaces;

namespace ExcelBotCs.Services.API;

public class MemberService : IMemberService
{
    private readonly IMemberRepository _memberRepository;
    private readonly ICacheAccessor<Member> _cache;

    public MemberService(IMemberRepository memberRepository, ICacheAccessor<Member> cache)
    {
        _memberRepository = memberRepository;
        _cache = cache;
    }

    public async Task<List<Member>> GetAsync()
    {
        var cached = _cache.GetAll();
        if (cached.Count > 0)
            return cached.OrderBy(x => x.DiscordName).ToList();

        var members = await _memberRepository.GetAsync();
        return members is null ? null : members.OrderBy(x => x.DiscordName).ToList();
    }

    public async Task<Member?> GetAsync(string id)
    {
        return _cache.GetById(id) ?? await _memberRepository.GetAsync(id);
    }

    public async Task CreateAsync(Member member)
    {
        await _memberRepository.CreateAsync(member);
        _cache.Update(member);
    }

    public async Task UpdateAsync(string id, Member updatedMember)
    {
        var dbEntity = await _memberRepository.GetAsync(id);
        if (dbEntity is null)
            throw new NotFoundException();

        updatedMember.DateCreated = dbEntity.DateCreated;
        updatedMember.ExperienceIds = dbEntity.ExperienceIds;
        updatedMember.LodestoneId = dbEntity.LodestoneId;

        await _memberRepository.UpdateAsync(id, updatedMember);
        _cache.Update(updatedMember);
    }

    public async Task DeleteAsync(string id)
    {
        await _memberRepository.DeleteAsync(id);
        _cache.Remove(id);
    }

    public async Task UpdateDiscordRoles(string id, List<string> roleIds)
    {
        var dbEntity = await _memberRepository.GetAsync(id);
        if (dbEntity is null)
            throw new NotFoundException();

        dbEntity.RoleIds = roleIds;

        await _memberRepository.UpdateAsync(id, dbEntity);
        _cache.Update(dbEntity);
    }

    public async Task UpdateMemberProfileAsync(string id, UpdateMemberRequest request)
    {
        var dbEntity = await _memberRepository.GetAsync(id);
        if (dbEntity is null)
            throw new NotFoundException();

        dbEntity.PlayerName = request.PlayerName;
        dbEntity.Subbed = request.Subbed;
        dbEntity.LodestoneId = request.LodestoneId;

        await _memberRepository.UpdateAsync(id, dbEntity);
        _cache.Update(dbEntity);
    }

    public async Task<Member> GetByDiscordId(string discordId)
    {
        if (_cache.IsPopulated)
        {
            var cached = _cache.GetAll()
                .FirstOrDefault(m => m.DiscordId == discordId);
            if (cached != null) return cached;
        }

        return await _memberRepository.GetByDiscordId(discordId);
    }

    public async Task<Member> GetByDiscordId(ulong discordId)
        => await GetByDiscordId(discordId.ToString());

    public async Task<List<Member>> GetByDiscordIds(IEnumerable<ulong> discordIds)
    {
        var discordIdStrings = discordIds.Select(id => id.ToString()).ToHashSet();
        var allMembers = await GetAsync();
        return allMembers.Where(m => discordIdStrings.Contains(m.DiscordId)).ToList();
    }

    public async Task<Member> GetByLodestoneId(string lodestoneId)
    {
        if (_cache.IsPopulated)
        {
            var cached = _cache.GetAll()
                .FirstOrDefault(m => m.LodestoneId == lodestoneId);
            if (cached != null) return cached;
        }

        return await _memberRepository.GetByLodestoneId(lodestoneId);
    }

    public async Task SetVerifiedLodestoneAsync(string id, string lodestoneId)
    {
        var dbEntity = await _memberRepository.GetAsync(id);
        if (dbEntity is null)
            throw new NotFoundException();

        dbEntity.LodestoneId = lodestoneId;
        dbEntity.LodestoneVerificationToken = null;

        await _memberRepository.UpdateAsync(id, dbEntity);
        _cache.Update(dbEntity);
    }

    public async Task<List<Member>> GetFcMembers()
    {
        var allMembers = await GetAsync();
        return allMembers.Where(x => x.IsMember.HasValue && x.IsMember.Value).ToList();
    }
}