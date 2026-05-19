using ExcelBotCs.Attributes;
using ExcelBotCs.Controllers.Interfaces;
using ExcelBotCs.Mappers.Fights;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO.Fights;
using ExcelBotCs.Services.API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ExcelBotCs.Controllers;

[ApiController]
[MemberAuth]
[Route("api/[controller]")]
public class FightsController : AuthorizedController, IFightsController
{
    private readonly IFightService _fightService;
    private readonly IBossService _bossService;
    private readonly IResourceService _resourceService;

    public FightsController(
        ILogger<FightsController> logger,
        IFightService fightService,
        IBossService bossService,
        IResourceService resourceService) : base(logger)
    {
        _fightService = fightService;
        _bossService = bossService;
        _resourceService = resourceService;
    }

    [HttpGet]
    public async Task<ActionResult<List<FightResponse>>> GetFights()
    {
        var fights = await _fightService.GetFightsAsync();
        var bosses = await _bossService.GetBossesAsync();
        var bossLookup = bosses.ToDictionary(b => b.Id);

        var responses = fights.Select(f =>
        {
            Boss? boss = f.BossId != null && bossLookup.TryGetValue(f.BossId, out var b) ? b : null;
            return f.ToFightResponse(boss);
        }).ToList();

        return Ok(responses);
    }

    [HttpGet("{id:length(24)}")]
    public async Task<ActionResult<FightResponse>> GetFight(string id)
    {
        var fight = await _fightService.GetFightAsync(id);
        if (fight == null)
            return NotFound();

        Boss? boss = null;
        if (fight.BossId != null)
            boss = await _bossService.GetBossAsync(fight.BossId);

        var resources = await _resourceService.GetByFightIdAsync(id);
        return Ok(fight.ToFightResponse(boss, resources));
    }

    [HttpPost]
    [AdminAuth]
    public async Task<ActionResult<FightResponse>> CreateFight([FromBody] CreateFightRequest request)
    {
        var fight = request.ToEntity();
        await _fightService.CreateAsync(fight);

        Boss? boss = null;
        if (fight.BossId != null)
            boss = await _bossService.GetBossAsync(fight.BossId);

        return CreatedAtAction(nameof(GetFight), new { id = fight.Id }, fight.ToFightResponse(boss));
    }

    [HttpPut("{id:length(24)}")]
    [AdminAuth]
    public async Task<ActionResult> UpdateFight(string id, [FromBody] UpdateFightRequest request)
    {
        var fight = await _fightService.GetFightAsync(id);
        if (fight == null)
            return NotFound();

        fight.ApplyUpdate(request);
        await _fightService.UpdateAsync(id, fight);

        return NoContent();
    }

    [HttpDelete("{id:length(24)}")]
    [AdminAuth]
    public async Task<ActionResult> DeleteFight(string id)
    {
        var fight = await _fightService.GetFightAsync(id);
        if (fight == null)
            return NotFound();

        await _fightService.DeleteAsync(id);
        return NoContent();
    }
}
