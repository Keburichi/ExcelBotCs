using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Services.API.Interfaces;

namespace ExcelBotCs.Services.API;

public class ResourceService : IResourceService
{
    private readonly IResourceRepository _resourceRepository;

    public ResourceService(IResourceRepository resourceRepository)
    {
        _resourceRepository = resourceRepository;
    }

    public async Task<List<Resource>> GetByFightIdAsync(string fightId)
    {
        return await _resourceRepository.GetByFightIdAsync(fightId);
    }

    public async Task<Resource?> GetAsync(string id)
    {
        return await _resourceRepository.GetAsync(id);
    }

    public async Task CreateAsync(Resource resource)
    {
        await _resourceRepository.CreateAsync(resource);
    }

    public async Task UpdateAsync(string id, Resource updatedResource)
    {
        await _resourceRepository.UpdateAsync(id, updatedResource);
    }

    public async Task DeleteAsync(string id)
    {
        await _resourceRepository.DeleteAsync(id);
    }
}
