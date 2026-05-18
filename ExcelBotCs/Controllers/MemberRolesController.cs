using ExcelBotCs.Attributes;
using ExcelBotCs.Controllers.Interfaces;
using ExcelBotCs.Mappers.Members;
using ExcelBotCs.Models.DTO.Members;
using ExcelBotCs.Services.API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ExcelBotCs.Controllers;

[ApiController]
[MemberAuth]
[Route("api/[controller]")]
public class MemberRolesController : AuthorizedController, IBaseCrudController<MemberRoleDto>
{
    private readonly IMemberRoleService _memberRoleService;

    public MemberRolesController(ILogger<MemberRolesController> logger, IMemberRoleService memberRoleService) :
        base(logger)
    {
        _memberRoleService = memberRoleService;
    }

    [HttpGet]
    public async Task<ActionResult<List<MemberRoleDto>>> GetEntities()
    {
        var entities = await _memberRoleService.GetAsync();

        if (entities is null)
            return new List<MemberRoleDto>();

        var dtos = entities.Select(x => x.ToDto()).ToList();

        return dtos;
    }

    [HttpGet("{id:length(24)}")]
    public async Task<ActionResult<MemberRoleDto>> GetEntity(string id)
    {
        var entity = await _memberRoleService.GetAsync(id);

        if (entity is null)
            return NotFound();

        return entity.ToDto();
    }

    [AdminAuth]
    [HttpPost]
    public async Task<ActionResult<MemberRoleDto>> CreateEntity(MemberRoleDto entity)
    {
        var entit = entity.ToEntity();
        await _memberRoleService.CreateAsync(entit);
        return CreatedAtAction(nameof(CreateEntity), new { id = entit.Id }, entit.ToDto());
    }

    [AdminAuth]
    [HttpPut("{id:length(24)}")]
    public async Task<ActionResult<MemberRoleDto>> UpdateEntity(string id, MemberRoleDto updatedEntity)
    {
        Logger.LogInformation("Updating entity with id: {id}", id);

        await _memberRoleService.UpdateAsync(id, updatedEntity.ToEntity());

        return NoContent();
    }

    [AdminAuth]
    [HttpDelete("{id:length(24)}")]
    public async Task<ActionResult<MemberRoleDto>> DeleteEntity(string id)
    {
        var entity = await _memberRoleService.GetAsync(id);

        if (entity is null)
            return NotFound();

        await _memberRoleService.DeleteAsync(id);
        return NoContent();
    }
}