using ExcelBotCs.Controllers.Interfaces;
using ExcelBotCs.Mappers;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.Services;
using ExcelBotCs.Services.API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ExcelBotCs.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MembersController : AuthorizedController, IBaseCrudController<MemberDto>
{
    private readonly IMemberService _memberService;
    private readonly ICurrentMemberAccessor _currentMemberAccessor;
    private readonly LodestoneService _lodestoneService;

    public MembersController(ILogger<MembersController> logger, IMemberService memberService,
        ICurrentMemberAccessor currentMemberAccessor, LodestoneService lodestoneService) : base(logger)
    {
        _memberService = memberService;
        _currentMemberAccessor = currentMemberAccessor;
        _lodestoneService = lodestoneService;
    }

    [HttpGet]
    public async Task<ActionResult<List<MemberDto>>> GetEntities()
    {
        var entities = await _memberService.GetAsync();

        if (entities is null)
            return new List<MemberDto>();

        var dtos = entities.Select(MemberMapper.ToDto).ToList();

        return dtos;
    }

    [HttpGet("{id:length(24)}")]
    public async Task<ActionResult<MemberDto>> GetEntity(string id)
    {
        var entity = await _memberService.GetAsync(id);

        if (entity is null)
            return NotFound();

        return MemberMapper.ToDto(entity);
    }

    [HttpPost]
    public async Task<ActionResult<MemberDto>> CreateEntity(MemberDto entity)
    {
        await _memberService.CreateAsync(MemberMapper.ToEntity(entity));
        return CreatedAtAction(nameof(CreateEntity), new { id = entity.Id }, entity);
    }

    [HttpPut("{id:length(24)}")]
    public async Task<ActionResult<MemberDto>> UpdateEntity(string id, MemberDto updatedEntity)
    {
        // Only allow users to update their own profile
        var me = await _currentMemberAccessor.GetCurrentAsync();
        if (me is null || me.Id != updatedEntity.Id)
            return Forbid();
        
        Logger.LogInformation("Updating entity with id: {id}", id);

        await _memberService.UpdateAsync(id, MemberMapper.ToEntity(updatedEntity));

        return NoContent();
    }

    [HttpDelete("{id:length(24)}")]
    public async Task<ActionResult<MemberDto>> DeleteEntity(string id)
    {
        var entity = await _memberService.GetAsync(id);

        if (entity is null)
            return NotFound();

        await _memberService.DeleteAsync(id);
        return NoContent();
    }

    public record LodestoneVerifyRequest(string LodestoneInput);

    [HttpPost("{id:length(24)}/lodestone-token")]
    public async Task<ActionResult<object>> GenerateLodestoneToken(string id)
    {
        var me = await _currentMemberAccessor.GetCurrentAsync();
        if (me is null || me.Id != id)
            return Forbid();

        // Reuse existing token if present to avoid churn
        var token = me.LodestoneVerificationToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            token = $"ExcelsiorFc-{Guid.NewGuid().ToString("N").ToUpperInvariant()}";
            me.LodestoneVerificationToken = token;
            await _memberService.UpdateAsync(me.Id, me);
        }

        return Ok(new { token });
    }

    [HttpPost("{id:length(24)}/verify-lodestone")]
    public async Task<ActionResult<object>> VerifyLodestone(string id, [FromBody] LodestoneVerifyRequest req)
    {
        var me = await _currentMemberAccessor.GetCurrentAsync();
        if (me is null || me.Id != id)
            return Forbid();

        if (me.LodestoneVerificationToken is null)
            return BadRequest("No verification token generated yet.");

        var lodestoneId = ParseLodestoneId(req?.LodestoneInput);
        if (string.IsNullOrWhiteSpace(lodestoneId))
            return BadRequest("Invalid Lodestone id or url.");

        // Ensure client available
        try
        {
            var bio = await _lodestoneService.GetCharacterBioById(lodestoneId);
            if (!string.IsNullOrWhiteSpace(bio) &&
                bio.Contains(me.LodestoneVerificationToken, StringComparison.OrdinalIgnoreCase))
            {
                me.LodestoneId = lodestoneId;
                me.LodestoneVerificationToken = null; // clear token after success
                await _memberService.UpdateAsync(me.Id, me);
                return Ok(new { success = true, message = "Character verified and linked." });
            }

            return Ok(new
            {
                success = false,
                message = "Verification text not found in bio. Please try again after updating your Lodestone bio."
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Lodestone verification failed for member {MemberId}", me.Id);
            return StatusCode(500, "Failed to verify character. Please try again later.");
        }
    }

    private static string ParseLodestoneId(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var s = input.Trim();

        // Quick path: plain numeric id
        if (s.All(char.IsDigit)) return s;

        // Strip query/hash for URL handling
        var noQueryOrHash = s.Split('?', '#')[0];

        // Try to extract after "/character/" segment
        const string marker = "/character/";
        var markerIdx = noQueryOrHash.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (markerIdx >= 0)
        {
            var after = noQueryOrHash.Substring(markerIdx + marker.Length);
            // Take the next path segment (in case of trailing slash)
            var idPart = after.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
            if (idPart.All(char.IsDigit)) return idPart;
        }

        // Fallback: take last non-empty segment from path and remove non-digits
        var lastSegment = noQueryOrHash.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ??
                          string.Empty;
        var digits = new string(lastSegment.Where(char.IsDigit).ToArray());
        if (!string.IsNullOrWhiteSpace(digits)) return digits;

        return string.Empty;
    }
}