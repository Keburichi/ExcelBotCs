using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Exceptions;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO.Members;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.Services.Minecraft;

namespace ExcelBotCs.Services.API;

public class MemberService : IMemberService
{
    private readonly IMemberRepository _memberRepository;
    private readonly IMinecraftRconService _minecraftRconService;

    public MemberService(IMemberRepository memberRepository, IMinecraftRconService minecraftRconService)
    {
        _memberRepository = memberRepository;
        _minecraftRconService = minecraftRconService;
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

        // Enforce: LodestoneId and MinecraftUsername can only be changed via their own dedicated
        // flows (verification / whitelist push). Prevent modification through the generic PUT.
        updatedMember.LodestoneId = dbEntity.LodestoneId;
        updatedMember.MinecraftUsername = dbEntity.MinecraftUsername;

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

    public async Task SetVerifiedLodestoneAsync(string id, string lodestoneId)
    {
        var dbEntity = await _memberRepository.GetAsync(id);
        if (dbEntity is null)
            throw new NotFoundException();

        dbEntity.LodestoneId = lodestoneId;
        dbEntity.LodestoneVerificationToken = null;

        await _memberRepository.UpdateAsync(id, dbEntity);
    }

    public async Task<(bool Success, string Message)> SetMinecraftUsernameAsync(string id, string? minecraftUsername)
    {
        var dbEntity = await _memberRepository.GetAsync(id);
        if (dbEntity is null)
            throw new NotFoundException();

        var newUsername = string.IsNullOrWhiteSpace(minecraftUsername) ? null : minecraftUsername.Trim();
        var oldUsername = dbEntity.MinecraftUsername;

        if (string.Equals(oldUsername, newUsername, StringComparison.OrdinalIgnoreCase))
            return (true, "Minecraft username unchanged.");

        // Best-effort: an old entry failing to remove shouldn't block linking the new one.
        if (!string.IsNullOrWhiteSpace(oldUsername))
            await _minecraftRconService.WhitelistRemoveAsync(oldUsername);

        if (newUsername is not null)
        {
            var (success, message) = await _minecraftRconService.WhitelistAddAsync(newUsername);
            if (!success)
                return (false, message);
        }

        dbEntity.MinecraftUsername = newUsername;
        await _memberRepository.UpdateAsync(id, dbEntity);

        return (true, newUsername is null
            ? "Removed from the Minecraft whitelist."
            : $"Whitelisted as {newUsername}.");
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