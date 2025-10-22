using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Services.API.Interfaces;

namespace ExcelBotCs.Services.API;

public class FcMemberService : IFcMemberService
{
    private readonly IFcMemberRepository _fcMemberRepository;

    public FcMemberService(IFcMemberRepository fcMemberRepository)
    {
        _fcMemberRepository = fcMemberRepository;
    }

    public async Task<List<FcMember>> GetAsync()
    {
        return await _fcMemberRepository.GetAsync();
    }

    public async Task<FcMember> GetAsync(string id)
    {
        return await _fcMemberRepository.GetAsync(id);
    }

    public async Task CreateAsync(FcMember entity)
    {
        await _fcMemberRepository.CreateAsync(entity);
    }

    public async Task UpdateAsync(string id, FcMember updatedEntity)
    {
        await _fcMemberRepository.UpdateAsync(id, updatedEntity);
    }

    public async Task DeleteAsync(string id)
    {
        await _fcMemberRepository.DeleteAsync(id);
    }
    
    public async Task<FcMember> GetByCharacterId(string characterId)
    {
        return await _fcMemberRepository.GetByCharacterId(characterId);
    }
}