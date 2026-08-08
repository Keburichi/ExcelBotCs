using ExcelBotCs.Attributes;
using ExcelBotCs.Controllers.Interfaces;
using ExcelBotCs.Mappers.Members;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO.Members;
using ExcelBotCs.Services;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.Services.Lodestone;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace ExcelBotCs.Controllers;

[ApiController]
[MemberAuth]
[Route("api/[controller]")]
public class MembersController : AuthorizedController, IMembersController
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
    public async Task<ActionResult<List<MemberResponse>>> GetMembers()
    {
        var entities = await _memberService.GetAsync();

        if (entities is null)
            return new List<MemberResponse>();

        var dtos = entities.Select(x => x.ToDto()).ToList();

        return dtos;
    }

    [HttpGet("{id:length(24)}")]
    public async Task<ActionResult<MemberResponse>> GetMemberById(string id)
    {
        var entity = await _memberService.GetAsync(id);

        if (entity is null)
            return NotFound();

        return entity.ToDto();
    }

    [HttpPut("{id:length(24)}")]
    public async Task<ActionResult<MemberResponse>> UpdateMember(string id, UpdateMemberRequest updateMember)
    {
        // Only allow users to update their own profile, unless they are an admin
        var me = await _currentMemberAccessor.GetCurrentAsync();

        if (me is null || (me.Id != id && !me.IsAdmin.GetValueOrDefault()))
            return Forbid();

        Logger.LogInformation("Updating entity with id: {id}", id);

        await _memberService.UpdateMemberProfileAsync(id, updateMember);

        return NoContent();
    }

    [HttpDelete("{id:length(24)}")]
    [AdminAuth]
    public async Task<ActionResult> DeleteMember(string id)
    {
        var entity = await _memberService.GetAsync(id);

        if (entity is null)
            return NotFound();

        await _memberService.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("{id:length(24)}/lodestone-token")]
    public async Task<ActionResult<string>> GenerateLodestoneToken(string id)
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
                await _memberService.SetVerifiedLodestoneAsync(me.Id, lodestoneId);
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

    [HttpPost("{id:length(24)}/minecraft")]
    public async Task<ActionResult<object>> SetMinecraftUsername(string id, [FromBody] SetMinecraftUsernameRequest request)
    {
        // Only allow users to update their own Minecraft link, unless they are an admin
        var me = await _currentMemberAccessor.GetCurrentAsync();
        if (me is null || (me.Id != id && !me.IsAdmin.GetValueOrDefault()))
            return Forbid();

        var (success, message) = await _memberService.SetMinecraftUsernameAsync(id, request?.MinecraftUsername);
        return Ok(new { success, message });
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
    public async Task<ActionResult<NoteResponse>> AddNote(string memberId, [FromBody] AddNoteRequest request)
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
            DateCreated = DateTime.UtcNow,
            DateModified = DateTime.UtcNow
        };

        member.Notes ??= new List<MemberNote>();
        member.Notes.Add(note);

        await _memberService.UpdateAsync(memberId, member);

        return CreatedAtAction(nameof(AddNote), new { memberId, noteId = note.Id }, note.ToDto());
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
        note.DateModified = DateTime.UtcNow;

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