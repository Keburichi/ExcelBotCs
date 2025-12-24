using ExcelBotCs.Attributes;
using ExcelBotCs.Controllers.Interfaces;
using ExcelBotCs.Mappers;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.Services.API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ExcelBotCs.Controllers;

[ApiController]
[MemberAuth]
[Route("api/[controller]")]
public class FcMembersController : AuthorizedController, IBaseCrudController<FcMemberDto>
{
    private readonly IFcMemberService _fcMemberService;

    public FcMembersController(ILogger<FcMembersController> logger, IFcMemberService fcMemberService) : base(logger)
    {
        _fcMemberService = fcMemberService;
    }

    [HttpGet]
    public async Task<ActionResult<List<FcMemberDto>>> GetEntities()
    {
        var entities = await _fcMemberService.GetAsync();

        if (entities is null)
            return new List<FcMemberDto>();

        var dtos = entities.Select(FcMemberMapper.ToDto).ToList();

        return dtos;
    }

    [HttpGet("{id:length(24)}")]
    public async Task<ActionResult<FcMemberDto>> GetEntity(string id)
    {
        var entity = await _fcMemberService.GetAsync(id);

        if (entity is null)
            return NotFound();

        return FcMemberMapper.ToDto(entity);
    }

    [HttpPost]
    public async Task<ActionResult<FcMemberDto>> CreateEntity(FcMemberDto entity)
    {
        var fcMember = FcMemberMapper.ToEntity(entity);
        await _fcMemberService.CreateAsync(fcMember);
        return CreatedAtAction(nameof(CreateEntity), new { id = fcMember.Id }, FcMemberMapper.ToDto(fcMember));
    }

    [HttpPut("{id:length(24)}")]
    public async Task<ActionResult<FcMemberDto>> UpdateEntity(string id, FcMemberDto updatedEntity)
    {
        Logger.LogInformation("Updating entity with id: {id}", id);

        await _fcMemberService.UpdateAsync(id, FcMemberMapper.ToEntity(updatedEntity));

        return NoContent();
    }

    [HttpDelete("{id:length(24)}")]
    public async Task<ActionResult<FcMemberDto>> DeleteEntity(string id)
    {
        var entity = await _fcMemberService.GetAsync(id);

        if (entity is null)
            return NotFound();

        await _fcMemberService.DeleteAsync(id);
        return NoContent();
    }
}