using ExcelBotCs.Models.DTO.Resources;
using Microsoft.AspNetCore.Mvc;

namespace ExcelBotCs.Controllers.Interfaces;

public interface IResourcesController
{
    Task<ActionResult<List<ResourceResponse>>> GetResources(string fightId);
    Task<ActionResult<ResourceResponse>> GetResource(string fightId, string id);
    Task<ActionResult<ResourceResponse>> CreateResource(string fightId, CreateResourceRequest request);
    Task<ActionResult> UpdateResource(string fightId, string id, UpdateResourceRequest request);
    Task<ActionResult> DeleteResource(string fightId, string id);
}
