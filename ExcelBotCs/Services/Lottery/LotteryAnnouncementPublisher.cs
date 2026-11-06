using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.Discord.Interfaces;
using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.Services.Lottery.Records;

namespace ExcelBotCs.Services.Lottery;

/// <summary>
///     The minimal announcement publisher for this ticket: a short plain line that proves the state machine and keeps
///     the manual run functional. It deliberately has no templates, role ping, emoji, last-five or history link — #111
///     replaces this class with the real post and the posting reconciliation.
/// </summary>
public class LotteryAnnouncementPublisher : ILotteryAnnouncementPublisher
{
	private readonly IDiscordMessageService _messages;

	public LotteryAnnouncementPublisher(IDiscordMessageService messages)
	{
		_messages = messages;
	}

	public async Task<LotteryAnnouncementPost> PublishAsync(LotteryAnnouncement announcement,
		CancellationToken cancellationToken = default)
	{
		var round = announcement.Round;
		var winners = announcement.Winners.Count == 0
			? "Nobody won."
			: $"Winners: {string.Join(", ", announcement.Winners.Select(winner => winner.DisplayName))}.";

		var message =
			$"The lottery winning number is {round.WinningNumber}. {winners} " +
			$"The pot is now {announcement.PotAfter:N0} Gil.";

		if (announcement.DryRun)
		{
			await _messages.PostInLogChannelAsync($"[DRY RUN] {message}");
			return new LotteryAnnouncementPost(null);
		}

		await _messages.PostInLotteryChannelAsync(message);

		// IDiscordMessageService currently returns nothing from its post methods; #111 adds a post that returns the
		// message so the round can store AnnouncementMessageId.
		return new LotteryAnnouncementPost(null);
	}

	public Task<string?> FindExistingPostAsync(LotteryRound round, CancellationToken cancellationToken = default)
	{
		// The channel search is part of #111's posting reconciliation.
		return Task.FromResult<string?>(null);
	}

	public async Task AnnotateVoidedAsync(LotteryRound round, string reason,
		CancellationToken cancellationToken = default)
	{
		await _messages.PostInLotteryChannelAsync(
			$"The lottery round {round.RoundKey} was voided: {reason}");
	}
}
