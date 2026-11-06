using System.Globalization;
using System.Text;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.Lottery.Records;
using ExcelBotCs.Utilities;

namespace ExcelBotCs.Services.Lottery;

/// <summary>
///     Renders the weekly announcement exactly as spec §5.6 and the accepted copy in #101 describe it: one plain
///     message, never an embed and never a participants list. Only the result block is admin-editable; the role ping,
///     the events/riddles nudge, the rules, the deadline, the pot summary, the last five and the history link are
///     generated here.
///     <para>
///         The composer is pure: it takes a <see cref="LotteryAnnouncementRenderRequest" /> and returns the text, so
///         the run, the dry run, the reconciliation search and the admin preview all render the same bytes. It does
///         not validate the template — <see cref="LotteryTemplateValidator" /> owns that, and the publisher falls back
///         to the default before calling in here.
///     </para>
/// </summary>
public static class LotteryAnnouncementComposer
{
	/// <summary>
	///     Discord rejects messages over 2,000 characters. The generated body is roughly 900 raw characters, so only a
	///     runaway admin template can approach this.
	/// </summary>
	public const int MaxMessageLength = 2_000;

	/// <summary>Part of the generated body; the reconciliation search uses it as a template-independent marker.</summary>
	public const string PinnedCommandsLine = "Check the pinned posts for an explanation on commands.";

	private const string HistoryPath = "/lottery/history";
	private const string NoHistoryYet = "No winners yet.";

	public static LotteryAssembledMessage Compose(LotteryAnnouncementRenderRequest request)
	{
		var deployment = request.Deployment;
		var builder = new StringBuilder();

		builder.Append("Hello ").Append(RoleMention(deployment)).AppendLine(",");
		builder.AppendLine();
		builder.AppendLine(ResultBlock(request));
		builder.AppendLine();
		builder.Append("Make sure to look out for new posts in ").Append(EventsMention(deployment))
			.Append(" to earn extra lottery entries and if you wanna increase the jackpot even more, go and solve " +
			        "the riddle in ").Append(RiddlesMention(deployment)).AppendLine("!");
		builder.AppendLine();
		builder.AppendLine("You can make the guesses for this week now! Remember the rules are:");
		builder.AppendLine("1) Number is a natural number between 1-99 (1 and 99 included)");
		builder.AppendLine("2) For every FC event you participate in, you get an extra guess to your one weekly guess!");
		builder.AppendLine();
		builder.AppendLine(PinnedCommandsLine);
		builder.Append("Submit your guesses by ").Append(deployment.RunDay).Append(' ').Append(deployment.RunTime)
			.AppendLine(", when the draw runs.");
		builder.AppendLine();
		builder.Append("Current pot: ").AppendLine(CurrentPot(request));
		builder.AppendLine();
		builder.AppendLine("Last 5 winners:");
		builder.AppendLine(WinnersRows(request.LastFive));
		builder.AppendLine();
		builder.Append("Full history: ").Append(HistoryUrl(deployment));

		return new LotteryAssembledMessage(builder.ToString());
	}

	/// <summary>The history deep link the post advertises (spec §5.8); a missing base URL keeps a relative path.</summary>
	public static string HistoryUrl(LotteryAnnouncementDeployment deployment)
	{
		return string.IsNullOrWhiteSpace(deployment.WebBaseUrl)
			? HistoryPath
			: deployment.WebBaseUrl.TrimEnd('/') + HistoryPath;
	}

	#region Result block

	private static string ResultBlock(LotteryAnnouncementRenderRequest request)
	{
		var template = request.Winners.Count == 0 ? request.NoWinTemplate : request.WinTemplate;

		// One pass over the template. The placeholder names are case-sensitive, so {pot} and {nextPot} cannot collide;
		// the order below is the order the admin page lists them in.
		var filled = template
			.Replace(LotteryTemplateValidator.BonusItems, BonusItemsText(request), StringComparison.Ordinal)
			.Replace(LotteryTemplateValidator.NextPot, Amount(request.CurrentPot), StringComparison.Ordinal)
			.Replace(LotteryTemplateValidator.Number,
				request.WinningNumber.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
			.Replace(LotteryTemplateValidator.Winners, WinnersText(request), StringComparison.Ordinal)
			.Replace(LotteryTemplateValidator.Prize, Gil(request.PrizePot), StringComparison.Ordinal)
			.Replace(LotteryTemplateValidator.Pot, Gil(request.CurrentPot), StringComparison.Ordinal)
			.Replace(LotteryTemplateValidator.Emoji, EmojiMention(request.Deployment), StringComparison.Ordinal);

		// A shared pot is split: the accepted copy keeps the template's prize line and only changes the verb (§5.6).
		// This is a wording transform on the template, not a rewrite: a template that does not contain "They take the "
		// is emitted untouched, so a custom result block is never rewritten behind the admin's back.
		return request.Winners.Count > 1
			? filled.Replace("They take the ", "They split the ", StringComparison.Ordinal)
			: filled;
	}

	private static string WinnersText(LotteryAnnouncementRenderRequest request)
	{
		if (request.Winners.Count == 0) return "nobody";

		var names = request.Winners.Select(NameOrMention).ToList();
		return HumanJoin(names);
	}

	/// <summary>
	///     Single winner: " plus the Chaotic Mount" (the template has "They take the {prize} pot{bonusItems}."). Shared
	///     draw: one "Bonus item: @X — Chaotic Mount." line per winner with items, placed after the split line through
	///     the placeholder's own position in the template.
	/// </summary>
	private static string BonusItemsText(LotteryAnnouncementRenderRequest request)
	{
		if (request.Winners.Count == 0) return string.Empty;

		if (request.Winners.Count == 1)
		{
			var items = request.Winners[0].BonusItems;
			return items is { Length: > 0 } ? $" plus the {ItemList(items)}" : string.Empty;
		}

		var lines = request.Winners
			.Where(winner => winner.BonusItems is { Length: > 0 })
			.Select(winner => $"Bonus item: {NameOrMention(winner)} — {ItemList(winner.BonusItems)}.")
			.ToList();

		return lines.Count == 0 ? string.Empty : "\n" + string.Join("\n", lines);
	}

	#endregion

	#region Generated sections

	private static string CurrentPot(LotteryAnnouncementRenderRequest request)
	{
		var pot = Gil(request.CurrentPot);

		return request.PoolPrizes.Count switch
		{
			0 => pot,
			1 => $"{pot} + {request.PoolPrizes[0]}",
			_ => $"{pot} + {request.PoolPrizes.Count.ToString(CultureInfo.InvariantCulture)} prizes"
		};
	}

	private static string WinnersRows(IReadOnlyList<LotteryWinner> lastFive)
	{
		return lastFive.Count == 0 ? NoHistoryYet : string.Join("\n", lastFive.Select(WinnerRow));
	}

	/// <summary>"Name | guess - amount Gil + item (DD.MM.YY)", the row shape accepted in #101.</summary>
	private static string WinnerRow(LotteryWinner winner)
	{
		var date = LotteryRoundKey.TryParse(winner.RoundKey, out var localDate)
			? localDate.ToString("dd.MM.yy", CultureInfo.InvariantCulture)
			: winner.RoundKey;

		var items = winner.BonusItems is { Length: > 0 } ? $" + {string.Join(", ", winner.BonusItems)}" : string.Empty;

		return $"{winner.DisplayName} | {winner.Guess.ToString(CultureInfo.InvariantCulture)} - " +
		       $"{Gil(winner.GilPrize)}{items} ({date})";
	}

	private static string RoleMention(LotteryAnnouncementDeployment deployment)
	{
		return deployment.MemberRoleId != 0 ? $"<@&{deployment.MemberRoleId}>" : "everyone";
	}

	private static string EventsMention(LotteryAnnouncementDeployment deployment)
	{
		return deployment.EventsChannelId != 0 ? $"<#{deployment.EventsChannelId}>" : "the events channel";
	}

	private static string RiddlesMention(LotteryAnnouncementDeployment deployment)
	{
		return deployment.RiddlesChannelId != 0 ? $"<#{deployment.RiddlesChannelId}>" : "the riddles channel";
	}

	/// <summary><c>:kekDoggo:</c> is posted as the custom emoji when its id is configured, literal text otherwise.</summary>
	private static string EmojiMention(LotteryAnnouncementDeployment deployment)
	{
		return deployment.CustomEmojiId != 0
			? $"<:kekDoggo:{deployment.CustomEmojiId}>"
			: ":kekDoggo:";
	}

	#endregion

	#region Formatting

	private static string NameOrMention(LotteryWinner winner)
	{
		return winner.DiscordId is { } discordId && discordId != 0 ? $"<@{discordId}>" : winner.DisplayName;
	}

	private static string Gil(long amount)
	{
		return Amount(amount) + " Gil";
	}

	private static string Amount(long amount)
	{
		return amount.ToString("N0", CultureInfo.InvariantCulture);
	}

	private static string ItemList(IReadOnlyList<string> items)
	{
		return HumanJoin(items);
	}

	/// <summary>"A", "A and B", "A, B, and C" — the accepted copy uses the serial comma.</summary>
	private static string HumanJoin(IReadOnlyList<string> values)
	{
		return values.Count switch
		{
			0 => string.Empty,
			1 => values[0],
			2 => $"{values[0]} and {values[1]}",
			_ => string.Join(", ", values.Take(values.Count - 1)) + $", and {values[^1]}"
		};
	}

	#endregion
}
