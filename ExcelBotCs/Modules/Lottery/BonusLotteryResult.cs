using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Modules.Lottery;

public class BonusLotteryResult : BaseEntity
{
    public bool HasWinner { get; set; }
    public ulong? WinnerDiscordId { get; set; }
    public string? WinnerName { get; set; }
    public string Prize { get; set; }
    public List<BonusLotteryEntry> Entries { get; set; } = new();
}

public class BonusLotteryEntry
{
    public ulong DiscordId { get; set; }
    public string DiscordName { get; set; }
    public string Reason { get; set; }
}
