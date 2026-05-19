using ExcelBotCs.Attributes;
using ExcelBotCs.Controllers.Interfaces;
using ExcelBotCs.Mappers.Bosses;
using ExcelBotCs.Models.DTO.Bosses;
using ExcelBotCs.Services.API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ExcelBotCs.Controllers;

[ApiController]
[MemberAuth]
[Route("api/[controller]")]
public class BossesController : AuthorizedController, IBossesController
{
    private readonly IBossService _bossService;
    private readonly IFightService _fightService;

    public BossesController(
        ILogger<BossesController> logger,
        IBossService bossService,
        IFightService fightService) : base(logger)
    {
        _bossService = bossService;
        _fightService = fightService;
    }

    [HttpGet]
    public async Task<ActionResult<List<BossResponse>>> GetBosses()
    {
        var bosses = await _bossService.GetBossesAsync();
        var fights = await _fightService.GetFightsAsync();

        var fightsByBoss = fights
            .Where(f => f.BossId != null)
            .GroupBy(f => f.BossId!)
            .ToDictionary(g => g.Key, g => g.ToList());

        var responses = bosses.Select(b =>
            b.ToBossResponse(fightsByBoss.GetValueOrDefault(b.Id))).ToList();

        return Ok(responses);
    }

    [HttpGet("{id:length(24)}")]
    public async Task<ActionResult<BossResponse>> GetBoss(string id)
    {
        var boss = await _bossService.GetBossAsync(id);
        if (boss == null)
            return NotFound();

        var fights = await _fightService.GetFightsAsync();
        var bossFights = fights.Where(f => f.BossId == id).ToList();

        return Ok(boss.ToBossResponse(bossFights));
    }

    [HttpPost]
    [AdminAuth]
    public async Task<ActionResult<BossResponse>> CreateBoss([FromBody] CreateBossRequest request)
    {
        var boss = request.ToEntity();
        await _bossService.CreateAsync(boss);

        return CreatedAtAction(nameof(GetBoss), new { id = boss.Id }, boss.ToBossResponse());
    }

    [HttpPut("{id:length(24)}")]
    [AdminAuth]
    public async Task<ActionResult> UpdateBoss(string id, [FromBody] UpdateBossRequest request)
    {
        var boss = await _bossService.GetBossAsync(id);
        if (boss == null)
            return NotFound();

        boss.ApplyUpdate(request);
        await _bossService.UpdateAsync(id, boss);

        return NoContent();
    }

    [HttpDelete("{id:length(24)}")]
    [AdminAuth]
    public async Task<ActionResult> DeleteBoss(string id)
    {
        var boss = await _bossService.GetBossAsync(id);
        if (boss == null)
            return NotFound();

        await _bossService.DeleteAsync(id);
        return NoContent();
    }
}
