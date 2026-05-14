using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Exceptions;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO.Members;
using ExcelBotCs.Services.API.Interfaces;

namespace ExcelBotCs.Services.API;

public class MemberService : BaseEntityService<Member, IMemberRepository>, IMemberService
{
    public MemberService(IMemberRepository memberRepository) : base(memberRepository)
    {
    }

    public override async Task<List<Member>> GetAsync()
    {
        var members = await Repository.GetAsync();
        return members is null ? null : members.OrderBy(x => x.DiscordName).ToList();
    }

    public override async Task UpdateAsync(string id, Member updatedEntity)
    {
        // Load the current DB state
        var dbEntity = await Repository.GetAsync(id);
        if (dbEntity is null)
            throw new NotFoundException();

        // Update all properties
        updatedEntity.DateCreated = dbEntity.DateCreated;
        updatedEntity.ExperienceIds = dbEntity.ExperienceIds;

        // Enforce: LodestoneId can only be set/changed via the verification flow
        // Prevent any modifications to LodestoneId through generic PUT updates
        updatedEntity.LodestoneId = dbEntity.LodestoneId;

        await Repository.UpdateAsync(id, updatedEntity);
    }

    public async Task UpdateDiscordRoles(string id, List<string> roleIds)
    {
        // Load the current DB state
        var dbEntity = await Repository.GetAsync(id);
        if (dbEntity is null)
            throw new NotFoundException();

        dbEntity.RoleIds = roleIds;

        await Repository.UpdateAsync(id, dbEntity);
    }

    public async Task UpdateMemberProfileAsync(string id, UpdateMemberRequest request)
    {
        // Load the current DB state
        var dbEntity = await Repository.GetAsync(id);
        if (dbEntity is null)
            throw new NotFoundException();

        dbEntity.PlayerName = request.PlayerName;
        dbEntity.Subbed = request.Subbed;
        dbEntity.LodestoneId = request.LodestoneId;

        await Repository.UpdateAsync(id, dbEntity);
    }

    public async Task<Member> GetByDiscordId(string discordId)
    {
        return await Repository.GetByDiscordId(discordId);
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
        return await Repository.GetByLodestoneId(lodestoneId);
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