using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO.Resources;

namespace ExcelBotCs.Mappers.Resources;

public static class ResourceMappingExtensions
{
    public static ResourceResponse ToResourceResponse(this Resource resource)
    {
        return new ResourceResponse
        {
            Id = resource.Id,
            Name = resource.Name,
            Description = resource.Description,
            Url = resource.Url,
            Type = resource.Type,
            FightId = resource.FightId,
            AuthorId = resource.AuthorId
        };
    }

    public static Resource ToEntity(this CreateResourceRequest request, string fightId, string authorId)
    {
        return new Resource
        {
            Name = request.Name,
            Description = request.Description,
            Url = request.Url,
            Type = request.Type,
            FightId = fightId,
            AuthorId = authorId
        };
    }

    public static void ApplyUpdate(this Resource resource, UpdateResourceRequest request)
    {
        if (request.Name != null)
            resource.Name = request.Name;

        if (request.Description != null)
            resource.Description = request.Description;

        if (request.Url != null)
            resource.Url = request.Url;

        if (request.Type.HasValue)
            resource.Type = request.Type.Value;
    }
}
