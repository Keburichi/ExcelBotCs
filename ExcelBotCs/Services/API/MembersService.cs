using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Exceptions;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Services.API.Interfaces;

namespace ExcelBotCs.Services.API;

public class MemberService : IMemberService
{
    private readonly IMemberRepository _memberRepository;

    public MemberService(IMemberRepository memberRepository)
    {
        _memberRepository = memberRepository;
    }

    public async Task<List<Member>> GetAsync()
    {
        var members = await _memberRepository.GetAsync();
        return members;
    }

    public async Task<Member> GetAsync(string id)
    {
        var member = await _memberRepository.GetAsync(id);
        return member;
    }

    public async Task CreateAsync(Member entity)
    {
        await _memberRepository.CreateAsync(entity);
    }

    public async Task UpdateAsync(string id, Member updatedEntity)
    {
        // Load the current DB state
        var dbEntity = await _memberRepository.GetAsync(id);
        if (dbEntity is null)
            throw new NotFoundException();
        
        // Update all properties
        updatedEntity.CreateDate = dbEntity.CreateDate;
        updatedEntity.ExperienceIds = dbEntity.ExperienceIds;
        updatedEntity.RoleIds = dbEntity.RoleIds;

        // Enforce: LodestoneId can only be set/changed via the verification flow
        // Prevent any modifications to LodestoneId through generic PUT updates
        updatedEntity.LodestoneId = dbEntity.LodestoneId;

        // Also prevent clients from tampering with the verification token via generic PUT
        updatedEntity.LodestoneVerificationToken = dbEntity.LodestoneVerificationToken;

        await _memberRepository.UpdateAsync(id, updatedEntity);
    }

    public async Task DeleteAsync(string id)
    {
        await _memberRepository.DeleteAsync(id);
    }

    public async Task<Member> GetByDiscordId(string discordId)
    {
        var member = await _memberRepository.GetByDiscordId(discordId);
        return member;
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
        var member = await _memberRepository.GetByLodestoneId(lodestoneId);
        return member;
    }

    /// <summary>
    /// Get all members that are members of the fc.
    /// </summary>
    /// <returns></returns>
    public async Task<List<Member>> GetFcMembers()
    {
        var allMembers = await GetAsync();
        return allMembers.Where(x => x.IsMember.HasValue && x.IsMember.Value).ToList();
    }
}