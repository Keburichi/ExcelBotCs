using ExcelBotCs.Controllers.Interfaces;
using ExcelBotCs.Mappers;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.Services.API;
using ExcelBotCs.Services.API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ExcelBotCs.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MemberRolesController : AuthorizedController, IBaseCrudController<MemberRoleDto>
{
    private readonly IMemberRoleService _memberRoleService;
    private readonly IMemberService _memberService;
    private readonly MemberRoleMapper _mapper;

    public MemberRolesController(ILogger<MemberRolesController> logger, IMemberRoleService memberRoleService,
        IMemberService memberService, MemberRoleMapper mapper) : base(logger)
    {
        _memberRoleService = memberRoleService;
        _memberService = memberService;
        _mapper = mapper;
    }
    
    [HttpGet]
    public async Task<ActionResult<List<MemberRoleDto>>> GetEntities()
    {
        var entities = await _memberRoleService.GetAsync();

        if (entities is null)
            return new List<MemberRoleDto>();

        var dtos = entities.Select(x => _mapper.ToDto(x)).ToList();

        return dtos;
    }

    [HttpGet("{id:length(24)}")]
    public async Task<ActionResult<MemberRoleDto>> GetEntity(string id)
    {
        var entity = await _memberRoleService.GetAsync(id);

        if (entity is null)
            return NotFound();

        return _mapper.ToDto(entity);
    }

    [HttpPost]
    public async Task<ActionResult<MemberRoleDto>> CreateEntity(MemberRoleDto entity)
    {
        await _memberRoleService.CreateAsync(_mapper.ToEntity(entity));
        return CreatedAtAction(nameof(CreateEntity), new { id = entity.Id }, entity);
    }

    [HttpPut("{id:length(24)}")]
    public async Task<ActionResult<MemberRoleDto>> UpdateEntity(string id, MemberRoleDto updatedEntity)
    {
        Logger.LogInformation("Updating entity with id: {id}", id);

        await _memberRoleService.UpdateAsync(id, _mapper.ToEntity(updatedEntity));

        return NoContent();
    }

    [HttpDelete("{id:length(24)}")]
    public async Task<ActionResult<MemberRoleDto>> DeleteEntity(string id)
    {
        var entity = await _memberRoleService.GetAsync(id);

        if (entity is null)
            return NotFound();

        await _memberService.DeleteAsync(id);
        return NoContent();
    }
}