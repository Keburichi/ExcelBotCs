using System.Text;
using ExcelBotCs.Services.Lottery.Records;

namespace ExcelBotCs.Services.Lottery;

/// <summary>
///     Renders the reminder exactly as spec §5.7 shows it: one heading naming the next draw, one line per remaining
///     guess count, and the closing note that only opted-in members are pinged. The composer is pure so the scheduled
///     post, the officer's manual reminder and the admin test send all render the same bytes.
/// </summary>
public static class LotteryReminderComposer
{
	/// <summary>Discord rejects messages over 2,000 characters; the reminder is a few hundred even for a large FC.</summary>
	public const int MaxMessageLength = 2_000;

	/// <summary>
	///     The fixed first line, part of the copy. It is also the marker the once-per-week check looks for when it
	///     searches the channel for an already-posted reminder after a restart.
	/// </summary>
	public const string HeadingPrefix = "## Use your lottery guesses before";

	public static string Compose(LotteryReminderAudience audience, LotteryReminderTarget target)
	{
		var builder = new StringBuilder();

		builder.Append(HeadingPrefix).Append(' ').Append(target.RunDay).Append(' ').Append(target.RunTime)
			.AppendLine();

		foreach (var group in audience.Groups)
		{
			var noun = group.RemainingGuesses == 1 ? "guess" : "guesses";
			var mentions = string.Join(", ", group.DiscordIds.Select(id => $"<@{id}>"));
			builder.Append('<').Append(group.RemainingGuesses).Append(' ').Append(noun).Append(" remaining: ")
				.AppendLine(mentions);
		}

		builder.Append("Only members who opted in are pinged. The draw runs ").Append(target.RunDay).Append(" at ")
			.Append(target.RunTime).Append('.');

		return builder.ToString();
	}
}
