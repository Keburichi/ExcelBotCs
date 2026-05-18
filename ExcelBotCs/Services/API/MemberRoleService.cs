using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Services.API.Interfaces;

namespace ExcelBotCs.Services.API;

public class MemberRoleService : IMemberRoleService
{
    private readonly IMemberRoleRepository _memberRoleRepository;

    public MemberRoleService(IMemberRoleRepository memberRoleRepository)
    {
        _memberRoleRepository = memberRoleRepository;
    }

    public async Task<List<MemberRole>> GetAsync()
    {
        return await _memberRoleRepository.GetAsync();
    }

    public async Task<MemberRole?> GetAsync(string id)
    {
        return await _memberRoleRepository.GetAsync(id);
    }

    public async Task CreateAsync(MemberRole memberRole)
    {
        await _memberRoleRepository.CreateAsync(memberRole);
    }

    public async Task UpdateAsync(string id, MemberRole updatedMemberRole)
    {
        await _memberRoleRepository.UpdateAsync(id, updatedMemberRole);
    }

    public async Task DeleteAsync(string id)
    {
        await _memberRoleRepository.DeleteAsync(id);
    }

    public async Task<MemberRole> GetByDiscordId(string discordId)
    {
        return await _memberRoleRepository.GetByDiscordId(discordId);
    }
}