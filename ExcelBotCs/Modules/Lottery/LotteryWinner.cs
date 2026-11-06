using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Modules.Lottery;

public class LotteryWinner : BaseEntity
{
	public string RoundKey { get; set; } = string.Empty;

	/// <summary><c>null</c> for the historical rows, which were recorded with a display name only.</summary>
	public ulong? DiscordId { get; set; }

	public string DisplayName { get; set; } = string.Empty;

	/// <summary>The winning number this winner guessed.</summary>
	public int Guess { get; set; }

	/// <summary>This winner's share of the pot.</summary>
	public long GilPrize { get; set; }

	/// <summary>Non-Gil prizes awarded to this winner; free text, as posted.</summary>
	public string[] BonusItems { get; set; } = [];

	public string PayoutStatus { get; set; } = LotteryPayoutStatus.Pending;
	public ulong? PayoutActorDiscordId { get; set; }
	public DateTime? PayoutAt { get; set; }
	public string? PayoutNote { get; set; }
}
