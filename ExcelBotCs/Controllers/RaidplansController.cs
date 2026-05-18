using ExcelBotCs.Attributes;
using ExcelBotCs.Mappers.Fights;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.Services;
using ExcelBotCs.Services.API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ExcelBotCs.Controllers;

[ApiController]
[MemberAuth]
[Route("api/fights/{fightId}/raidplans")]
public class RaidplansController : AuthorizedController
{
    private readonly IRaidplanService _raidplanService;
    private readonly ICurrentMemberAccessor _currentMemberAccessor;

    public RaidplansController(
        ILogger<RaidplansController> logger,
        IRaidplanService raidplanService,
        ICurrentMemberAccessor currentMemberAccessor) : base(logger)
    {
        _raidplanService = raidplanService;
        _currentMemberAccessor = currentMemberAccessor;
    }

    // GET /api/fights/{fightId}/raidplans
    [HttpGet]
    public async Task<ActionResult<List<RaidplanDto>>> GetRaidplans(string fightId)
    {
        var raidplans = await _raidplanService.GetByFightIdAsync(fightId);
        var dtos = raidplans.Select(x => x.ToDto()).ToList();
        return Ok(dtos);
    }

    // GET /api/fights/{fightId}/raidplans/{id}
    [HttpGet("{id:length(24)}")]
    public async Task<ActionResult<RaidplanDto>> GetRaidplan(string fightId, string id)
    {
        var raidplan = await _raidplanService.GetAsync(id);

        if (raidplan == null)
            return NotFound();

        return Ok(raidplan.ToDto());
    }

    // POST /api/fights/{fightId}/raidplans
    [HttpPost]
    public async Task<ActionResult<RaidplanDto>> CreateRaidplan(string fightId, [FromBody] RaidplanDto dto)
    {
        var member = await _currentMemberAccessor.GetCurrentAsync();
        if (member == null)
            return Unauthorized("Member not found for the current user");

        // Set the author to the current member
        dto.AuthorId = member.Id;

        var entity = dto.ToEntity();
        await _raidplanService.CreateAsync(fightId, entity);

        return CreatedAtAction(
            nameof(GetRaidplan),
            new { fightId, id = entity.Id },
            entity.ToDto());
    }

    // PUT /api/fights/{fightId}/raidplans/{id}
    [HttpPut("{id:length(24)}")]
    public async Task<ActionResult> UpdateRaidplan(string fightId, string id, [FromBody] RaidplanDto dto)
    {
        var member = await _currentMemberAccessor.GetCurrentAsync();
        if (member == null)
            return Unauthorized("Member not found for the current user");

        var existingRaidplan = await _raidplanService.GetAsync(id);
        if (existingRaidplan == null)
            return NotFound();

        // Check authorization: User can edit their own raidplan OR user is admin
        var isAdmin = member.IsAdmin == true;
        var isOwner = existingRaidplan.AuthorId == member.Id;

        if (!isAdmin && !isOwner)
            return Forbid();

        var entity = dto.ToEntity();
        entity.AuthorId = existingRaidplan.AuthorId; // Preserve original author

        await _raidplanService.UpdateAsync(fightId, id, entity);
        return NoContent();
    }

    // DELETE /api/fights/{fightId}/raidplans/{id}
    [HttpDelete("{id:length(24)}")]
    public async Task<ActionResult> DeleteRaidplan(string fightId, string id)
    {
        var member = await _currentMemberAccessor.GetCurrentAsync();
        if (member == null)
            return Unauthorized("Member not found for the current user");

        var existingRaidplan = await _raidplanService.GetAsync(id);
        if (existingRaidplan == null)
            return NotFound();

        // Check authorization: Only admins can delete raidplans
        var isAdmin = member.IsAdmin == true;

        if (!isAdmin)
            return Forbid();

        await _raidplanService.DeleteAsync(fightId, id);
        return NoContent();
    }
}