using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Exceptions;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO.Members;
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
        return members is null ? null : members.OrderBy(x => x.DiscordName).ToList();
    }

    public async Task<Member?> GetAsync(string id)
    {
        return await _memberRepository.GetAsync(id);
    }

    public async Task CreateAsync(Member member)
    {
        await _memberRepository.CreateAsync(member);
    }

    public async Task UpdateAsync(string id, Member updatedMember)
    {
        // Load the current DB state
        var dbEntity = await _memberRepository.GetAsync(id);
        if (dbEntity is null)
            throw new NotFoundException();

        // Update all properties
        updatedMember.DateCreated = dbEntity.DateCreated;
        updatedMember.ExperienceIds = dbEntity.ExperienceIds;

        // Enforce: LodestoneId can only be set/changed via the verification flow
        // Prevent any modifications to LodestoneId through generic PUT updates
        updatedMember.LodestoneId = dbEntity.LodestoneId;

        await _memberRepository.UpdateAsync(id, updatedMember);
    }

    public async Task DeleteAsync(string id)
    {
        await _memberRepository.DeleteAsync(id);
    }

    public async Task UpdateDiscordRoles(string id, List<string> roleIds)
    {
        // Load the current DB state
        var dbEntity = await _memberRepository.GetAsync(id);
        if (dbEntity is null)
            throw new NotFoundException();

        dbEntity.RoleIds = roleIds;

        await _memberRepository.UpdateAsync(id, dbEntity);
    }

    public async Task UpdateMemberProfileAsync(string id, UpdateMemberRequest request)
    {
        // Load the current DB state
        var dbEntity = await _memberRepository.GetAsync(id);
        if (dbEntity is null)
            throw new NotFoundException();

        dbEntity.PlayerName = request.PlayerName;
        dbEntity.Subbed = request.Subbed;
        dbEntity.LodestoneId = request.LodestoneId;

        await _memberRepository.UpdateAsync(id, dbEntity);
    }

    public async Task<Member> GetByDiscordId(string discordId)
    {
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
        return await _memberRepository.GetByLodestoneId(lodestoneId);
    }

    /// <summary>
    /// Get all members that are members of the fc.
    /// </summary>
    public async Task<List<Member>> GetFcMembers()
    {
        var allMembers = await GetAsync();
        return allMembers.Where(x => x.IsMember.HasValue && x.IsMember.Value).ToList();
    }
}