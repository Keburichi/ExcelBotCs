using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Modules.Lottery;

/// <summary>A member's choice to receive the pre-draw reminder. Default off; one document per Discord id.</summary>
public class LotteryReminderOptin : BaseEntity
{
	public ulong DiscordId { get; set; }
	public bool Enabled { get; set; }
}
