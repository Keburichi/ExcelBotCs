using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO;

namespace ExcelBotCs.Mappers;

public static class MemberNoteMapper
{
    public static MemberNoteDto ToDto(MemberNote note)
    {
        return new MemberNoteDto()
        {
            Id = note.Id,
            DateCreated = note.DateCreated,
            DateModified = note.DateModified,
            Note = note.Note,
            Author = note.Author,
        };
    }

    public static MemberNote ToEntity(MemberNoteDto note)
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
