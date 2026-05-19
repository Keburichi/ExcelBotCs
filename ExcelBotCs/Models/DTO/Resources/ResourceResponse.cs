using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Models.DTO.Resources;

public class ResourceResponse : BaseDto
{
    public string Name { get; set; }
    public string? Description { get; set; }
    public string Url { get; set; }
    public ResourceType Type { get; set; }
    public string FightId { get; set; }
    public string AuthorId { get; set; }
}
