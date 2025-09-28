using ExcelBotCs.Database.DTO;
using ExcelBotCs.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExcelBotCs.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MembersController : BaseCrudController<Member>
{
    private readonly MemberService _memberService;
    private readonly ICurrentMemberAccessor _currentMemberAccessor;
    private readonly LodestoneService _lodestoneService;

    public MembersController(ILogger<MembersController> logger, MemberService memberService, ICurrentMemberAccessor currentMemberAccessor, LodestoneService lodestoneService) : base(logger,
        memberService)
    {
        _memberService = memberService;
        _currentMemberAccessor = currentMemberAccessor;
        _lodestoneService = lodestoneService;
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
            if (!string.IsNullOrWhiteSpace(bio) && bio.Contains(me.LodestoneVerificationToken, StringComparison.OrdinalIgnoreCase))
            {
                me.LodestoneId = lodestoneId;
                me.LodestoneVerificationToken = null; // clear token after success
                await _memberService.UpdateAsync(me.Id, me);
                return Ok(new { success = true, message = "Character verified and linked." });
            }

            return Ok(new { success = false, message = "Verification text not found in bio. Please try again after updating your Lodestone bio." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lodestone verification failed for member {MemberId}", me.Id);
            return StatusCode(500, "Failed to verify character. Please try again later.");
        }
    }

    private static string ParseLodestoneId(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        input = input.Trim();
        // If it's a URL, extract trailing digits
        var idx = input.LastIndexOf('/');
        var candidate = idx >= 0 ? input.Substring(idx + 1) : input;
        // Remove query/hash
        var q = candidate.IndexOfAny(new[] { '?', '#' });
        if (q >= 0) candidate = candidate.Substring(0, q);
        // Only digits expected by Lodestone
        if (candidate.All(char.IsDigit)) return candidate;
        return string.Empty;
    }
}