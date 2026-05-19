namespace ExcelBotCs.Models.Database;

public class Boss : BaseEntity
{
    public string Name { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsUltimate { get; set; }
    public string NormalizationKey { get; set; }
    public int? FFLogsExpansionId { get; set; }
}
