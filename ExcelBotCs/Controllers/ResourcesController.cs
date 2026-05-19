using ExcelBotCs.Attributes;
using ExcelBotCs.Controllers.Interfaces;
using ExcelBotCs.Mappers.Resources;
using ExcelBotCs.Models.DTO.Resources;
using ExcelBotCs.Services;
using ExcelBotCs.Services.API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ExcelBotCs.Controllers;

[ApiController]
[MemberAuth]
[Route("api/fights/{fightId}/resources")]
public class ResourcesController : AuthorizedController, IResourcesController
{
    private readonly IResourceService _resourceService;
    private readonly ICurrentMemberAccessor _currentMemberAccessor;

    public ResourcesController(
        ILogger<ResourcesController> logger,
        IResourceService resourceService,
        ICurrentMemberAccessor currentMemberAccessor) : base(logger)
    {
        _resourceService = resourceService;
        _currentMemberAccessor = currentMemberAccessor;
    }

    [HttpGet]
    public async Task<ActionResult<List<ResourceResponse>>> GetResources(string fightId)
    {
        var resources = await _resourceService.GetByFightIdAsync(fightId);
        var responses = resources.Select(r => r.ToResourceResponse()).ToList();
        return Ok(responses);
    }

    [HttpGet("{id:length(24)}")]
    public async Task<ActionResult<ResourceResponse>> GetResource(string fightId, string id)
    {
        var resource = await _resourceService.GetAsync(id);
        if (resource == null)
            return NotFound();

        return Ok(resource.ToResourceResponse());
    }

    [HttpPost]
    public async Task<ActionResult<ResourceResponse>> CreateResource(string fightId, [FromBody] CreateResourceRequest request)
    {
        var member = await _currentMemberAccessor.GetCurrentAsync();
        if (member == null)
            return Unauthorized("Member not found for the current user");

        var entity = request.ToEntity(fightId, member.Id);
        await _resourceService.CreateAsync(entity);

        return CreatedAtAction(
            nameof(GetResource),
            new { fightId, id = entity.Id },
            entity.ToResourceResponse());
    }

    [HttpPut("{id:length(24)}")]
    public async Task<ActionResult> UpdateResource(string fightId, string id, [FromBody] UpdateResourceRequest request)
    {
        var member = await _currentMemberAccessor.GetCurrentAsync();
        if (member == null)
            return Unauthorized("Member not found for the current user");

        var existing = await _resourceService.GetAsync(id);
        if (existing == null)
            return NotFound();

        var isAdmin = member.IsAdmin == true;
        var isOwner = existing.AuthorId == member.Id;

        if (!isAdmin && !isOwner)
            return Forbid();

        existing.ApplyUpdate(request);
        await _resourceService.UpdateAsync(id, existing);

        return NoContent();
    }

    [HttpDelete("{id:length(24)}")]
    public async Task<ActionResult> DeleteResource(string fightId, string id)
    {
        var member = await _currentMemberAccessor.GetCurrentAsync();
        if (member == null)
            return Unauthorized("Member not found for the current user");

        var existing = await _resourceService.GetAsync(id);
        if (existing == null)
            return NotFound();

        var isAdmin = member.IsAdmin == true;
        var isOwner = existing.AuthorId == member.Id;

        if (!isAdmin && !isOwner)
            return Forbid();

        await _resourceService.DeleteAsync(id);
        return NoContent();
    }
}
