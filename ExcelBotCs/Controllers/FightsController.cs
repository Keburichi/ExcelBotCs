using ExcelBotCs.Controllers.Interfaces;
using ExcelBotCs.Mappers;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.Services.API;
using ExcelBotCs.Services.API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ExcelBotCs.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FightsController : AuthorizedController, IBaseCrudController<FightDto>
{
    private readonly IFightService _fightService;

    public FightsController(ILogger<FightsController> logger, IFightService fightService) : base(logger)
    {
        _fightService = fightService;
    }

    [HttpGet]
    public async Task<ActionResult<List<FightDto>>> GetEntities()
    {
        var entities = await _fightService.GetAsync();
        
        if(entities is null)
            return new List<FightDto>();
        
        var dtos = entities.Select(FightMapper.ToDto).ToList();

        return dtos;
    }

    [HttpGet("{id:length(24)}")]
    public async Task<ActionResult<FightDto>> GetEntity(string id)
    {
        var entity = await _fightService.GetAsync(id);

        if (entity is null)
            return NotFound();

        return FightMapper.ToDto(entity);
    }

    [HttpPost]
    public async Task<ActionResult<FightDto>> CreateEntity(FightDto entity)
    {
        await _fightService.CreateAsync(FightMapper.ToEntity(entity));
        return CreatedAtAction(nameof(CreateEntity), new { id = entity.Id }, entity);
    }

    [HttpPut("{id:length(24)}")]
    public async Task<ActionResult<FightDto>> UpdateEntity(string id, FightDto updatedEntity)
    {
        Logger.LogInformation("Updating entity with id: {id}", id);

        await _fightService.UpdateAsync(id, FightMapper.ToEntity(updatedEntity));

        return NoContent();
    }

    [HttpDelete("{id:length(24)}")]
    public async Task<ActionResult<FightDto>> DeleteEntity(string id)
    {
        var entity = await _fightService.GetAsync(id);

        if (entity is null)
            return NotFound();

        await _fightService.DeleteAsync(id);
        return NoContent();
    }
}