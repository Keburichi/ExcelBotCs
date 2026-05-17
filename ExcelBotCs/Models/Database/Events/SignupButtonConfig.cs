using ExcelBotCs.Modules.TeamFormation;

namespace ExcelBotCs.Models.Database.Events;

public class SignupButtonConfig
{
    public string Slug { get; set; }
    public string Label { get; set; }
    public string? EmojiId { get; set; }
    public bool IsHelper { get; set; }
    public Role? MappedRole { get; set; }
}
