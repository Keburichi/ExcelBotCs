using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.Discord.Records;

namespace ExcelBotCs.Services.Lottery;

/// <summary>
///     The posting reconciliation of spec §5.10: a round writes <c>PostingStartedAt</c> before the Discord send, so a
///     crash between the send and the state write leaves a round that may or may not have posted. On the next attempt
///     the channel is searched for a message that can only be this round's announcement — bot author, inside the post
///     window, and carrying this round's rendered body or, when the template changed since, the generated markers
///     every announcement has. That message is adopted instead of posted twice.
///     <para>
///         A dry run is never adopted: it goes to the log channel prefixed with <c>[DRY RUN]</c>, and the prefix is
///         rejected here as well so a misconfigured log channel cannot turn a preview into the real post.
///     </para>
/// </summary>
public static class LotteryAnnouncementReconciler
{
	/// <summary>How many recent lottery-channel messages one search looks at.</summary>
	public const int RecentMessageLimit = 50;

	/// <summary>Allowance for the Discord clock and the writer's clock disagreeing by a moment.</summary>
	public static readonly TimeSpan ClockSkew = TimeSpan.FromMinutes(2);

	/// <summary>The prefix a dry-run preview is posted with; such a message is never a real announcement.</summary>
	public const string DryRunPrefix = "[DRY RUN]";

	private const string HistoryMarker = "Full history: ";

	public static bool IsThisRoundsPost(DiscordChannelMessage message, LotteryRound round, string renderedContent)
	{
		if (string.IsNullOrWhiteSpace(message.Content)) return false;

		// Only the bot posts the announcement, and a person replying in the window is not a match.
		if (!message.IsBot) return false;

		// A dry run lives in the log channel; even if one leaked into the lottery channel it must never be adopted.
		if (message.Content.TrimStart().StartsWith(DryRunPrefix, StringComparison.Ordinal)) return false;

		if (!IsInsidePostingWindow(message.CreatedAt, round)) return false;

		// The strong match: the channel message is byte-for-byte what this round renders today.
		if (Normalize(message.Content) == Normalize(renderedContent)) return true;

		// The tolerant match: the template or the pool changed between the send and the restart, so the result block
		// differs, but every announcement carries the generated rules nudge and the history link.
		return message.Content.Contains(LotteryAnnouncementComposer.PinnedCommandsLine, StringComparison.Ordinal)
		       && message.Content.Contains(HistoryMarker, StringComparison.Ordinal);
	}

	private static bool IsInsidePostingWindow(DateTimeOffset createdAt, LotteryRound round)
	{
		// Prefer the marker; fall back to the scheduled slot for a round written before the marker existed.
		var anchor = round.PostingStartedAt ?? round.ScheduledAt;
		var anchorOffset = new DateTimeOffset(DateTime.SpecifyKind(anchor, DateTimeKind.Utc));

		var start = anchorOffset - ClockSkew;
		var end = anchorOffset + LotteryRetryPolicy.AnnouncementWindow + ClockSkew;

		return createdAt >= start && createdAt <= end;
	}

	private static string Normalize(string content)
	{
		return content.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
	}
}
