namespace ExcelBotCs.Models.DTO.Bosses;

public class CreateBossRequest
{
    public string Name { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsUltimate { get; set; }
}
