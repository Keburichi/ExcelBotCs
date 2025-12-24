using ExcelBotCs.Attributes;
using ExcelBotCs.Controllers.Interfaces;
using ExcelBotCs.Mappers;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.Services;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.Services.Lodestone;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace ExcelBotCs.Controllers;

[ApiController]
[MemberAuth]
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

    // Note management endpoints
    [HttpPost("{memberId:length(24)}/notes")]
    [AdminAuth]
    public async Task<ActionResult<MemberNoteDto>> AddNote(string memberId, [FromBody] AddNoteRequest request)
    {
        var currentUser = await _currentMemberAccessor.GetCurrentAsync();
        if (currentUser is null)
            return Unauthorized();

        var member = await _memberService.GetAsync(memberId);
        if (member is null)
            return NotFound();

        var note = new MemberNote
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Note = request.Note,
            Author = currentUser.DiscordName,
            CreateDate = DateTime.UtcNow,
            EditDate = DateTime.UtcNow
        };

        member.Notes ??= new List<MemberNote>();
        member.Notes.Add(note);

        await _memberService.UpdateAsync(memberId, member);

        return CreatedAtAction(nameof(AddNote), new { memberId, noteId = note.Id }, MemberNoteMapper.ToDto(note));
    }

    [HttpPut("{memberId:length(24)}/notes/{noteId:length(24)}")]
    [AdminAuth]
    public async Task<ActionResult> UpdateNote(string memberId, string noteId, [FromBody] UpdateNoteRequest request)
    {
        var currentUser = await _currentMemberAccessor.GetCurrentAsync();
        if (currentUser is null)
            return Unauthorized();

        var member = await _memberService.GetAsync(memberId);
        if (member is null)
            return NotFound("Member not found");

        if (member.Notes is null || member.Notes.Count == 0)
            return NotFound("No notes found");

        var note = member.Notes.FirstOrDefault(n => n.Id == noteId);
        if (note is null)
            return NotFound("Note not found");

        note.Note = request.Note;
        note.EditDate = DateTime.UtcNow;

        await _memberService.UpdateAsync(memberId, member);

        return NoContent();
    }

    [HttpDelete("{memberId:length(24)}/notes/{noteId:length(24)}")]
    [AdminAuth]
    public async Task<ActionResult> DeleteNote(string memberId, string noteId)
    {
        var currentUser = await _currentMemberAccessor.GetCurrentAsync();
        if (currentUser is null)
            return Unauthorized();

        var member = await _memberService.GetAsync(memberId);
        if (member is null)
            return NotFound("Member not found");

        if (member.Notes is null || member.Notes.Count == 0)
            return NotFound("No notes found");

        var noteIndex = member.Notes.FindIndex(n => n.Id == noteId);
        if (noteIndex == -1)
            return NotFound("Note not found");

        member.Notes.RemoveAt(noteIndex);

        await _memberService.UpdateAsync(memberId, member);

        return NoContent();
    }
}