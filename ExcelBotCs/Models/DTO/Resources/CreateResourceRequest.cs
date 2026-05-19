using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Models.DTO.Resources;

public class CreateResourceRequest
{
    public string Name { get; set; }
    public string? Description { get; set; }
    public string Url { get; set; }
    public ResourceType Type { get; set; }
}
