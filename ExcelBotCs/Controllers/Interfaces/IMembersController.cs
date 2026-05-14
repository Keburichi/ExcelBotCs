using ExcelBotCs.Models.DTO.Members;
using Microsoft.AspNetCore.Mvc;

namespace ExcelBotCs.Controllers.Interfaces;

public interface IMembersController
{
    // CRUD operations
    public Task<ActionResult<List<MemberResponse>>> GetMembers();
    public Task<ActionResult<MemberResponse>> GetMemberById(string id);
    public Task<ActionResult<MemberResponse>> UpdateMember(string id, UpdateMemberRequest updateMember);
    public Task<ActionResult> DeleteMember(string id);

    // Lodestone link operations
    public Task<ActionResult<string>> GenerateLodestoneToken(string id);
    public Task<ActionResult<object>> VerifyLodestone(string id, LodestoneVerifyRequest token);

    // Note operations
    public Task<ActionResult<NoteResponse>> AddNote(string memberId, AddNoteRequest request);
    public Task<ActionResult> UpdateNote(string memberId, string noteId, UpdateNoteRequest request);
    public Task<ActionResult> DeleteNote(string memberId, string noteId);
}