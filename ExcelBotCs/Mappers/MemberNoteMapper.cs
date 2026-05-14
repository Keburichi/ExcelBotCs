using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO.Members;

namespace ExcelBotCs.Mappers;

public static class MemberNoteMapper
{
    public static NoteResponse ToDto(MemberNote note)
    {
        return new NoteResponse
        {
            Id = note.Id,
            DateCreated = note.DateCreated,
            DateModified = note.DateModified,
            Note = note.Note,
            Author = note.Author,
        };
    }

    public static MemberNote ToEntity(NoteResponse note)
    {
        return new MemberNote()
        {
            Id = note.Id,
            DateCreated = note.DateCreated,
            DateModified = note.DateModified,
            Note = note.Note,
            Author = note.Author,
        };
    }
}