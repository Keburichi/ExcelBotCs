using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Exceptions;
using ExcelBotCs.Extensions;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Services.API.Interfaces;

namespace ExcelBotCs.Services.API;

public class MemberService : IMemberService
{
    private readonly IMemberRepository _memberRepository;
    private readonly IMemberRoleRepository _memberRoleRepository;

    public MemberService(IMemberRepository memberRepository, IMemberRoleRepository memberRoleRepository)
    {
        _memberRepository = memberRepository;
        _memberRoleRepository = memberRoleRepository;
    }

    public async Task<List<Member>> GetAsync()
    {
        return await _memberRepository.GetAsync();
    }

    public async Task<Member> GetAsync(string id)
    {
        return await _memberRepository.GetAsync(id);
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

        // Check if the roles changed
        
        // Update all properties
        updatedEntity.UpdateUpdatedAttributes(dbEntity);

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
        return await _memberRepository.GetByDiscordId(discordId);
    }

    public async Task<Member> GetByDiscordId(ulong discordId)
        => await GetByDiscordId(discordId.ToString());

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