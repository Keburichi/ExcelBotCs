using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Models.DTO.Fights;

public class UpdateFightRequest
{
    public string? Name { get; set; }
    public FightType? Type { get; set; }
    public string? BossId { get; set; }
}
