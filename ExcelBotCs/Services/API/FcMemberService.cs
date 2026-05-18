using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Services.API.Interfaces;

namespace ExcelBotCs.Services.API;

public class FcMemberService : IFcMemberService
{
    private readonly IFcMemberRepository _repository;

    public FcMemberService(IFcMemberRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<FcMember>> GetAsync()
    {
        return await _repository.GetAsync();
    }

    public async Task<FcMember?> GetAsync(string id)
    {
        return await _repository.GetAsync(id);
    }

    public async Task CreateAsync(FcMember fcMember)
    {
        await _repository.CreateAsync(fcMember);
    }

    public async Task UpdateAsync(string id, FcMember updatedFcMember)
    {
        await _repository.UpdateAsync(id, updatedFcMember);
    }

    public async Task DeleteAsync(string id)
    {
        await _repository.DeleteAsync(id);
    }

    public async Task<FcMember> GetByCharacterId(string characterId)
    {
        return await _repository.GetByCharacterId(characterId);
    }
}