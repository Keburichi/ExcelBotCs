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
            CreateDate = note.CreateDate,
            EditDate = note.EditDate,
            Note = note.Note,
            Author = note.Author,
        };
    }
    
    public static MemberNote ToEntity(MemberNoteDto note)
    {
        return new MemberNote()
        {
            Id = note.Id,
            CreateDate = note.CreateDate,
            EditDate = note.EditDate,
            Note = note.Note,
            Author = note.Author,
        };
    }
}