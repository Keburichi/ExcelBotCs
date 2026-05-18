using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO.Members;

namespace ExcelBotCs.Mappers.Members;

public static class MemberNoteMappingExtensions
{
    public static NoteResponse ToDto(this MemberNote note)
    {
        return new NoteResponse
        {
            Id = note.Id,
            DateCreated = note.DateCreated,
            DateModified = note.DateModified,
            Note = note.Note,
            Author = note.Author
        };
    }

    public static MemberNote ToEntity(this NoteResponse note)
    {
        return new MemberNote
        {
            Id = note.Id,
            DateCreated = note.DateCreated,
            DateModified = note.DateModified,
            Note = note.Note,
            Author = note.Author
        };
    }
}