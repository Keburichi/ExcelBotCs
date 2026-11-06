using Discord;

namespace ExcelBotCs.Modules.Lottery;

/// <summary>
///     The two buttons the reminder post carries (spec §5.7) and the mention restriction that keeps the post from
///     pinging anyone outside the audience:
///     <list type="bullet">
///         <item><b>Turn off reminders</b> — secondary, custom id <see cref="OptOutCustomId" />.</item>
///         <item><b>Open lottery page</b> — a link to <c>&lt;WebBaseUrl&gt;/lottery</c>, omitted when no base URL is configured.</item>
///     </list>
/// </summary>
public static class LotteryReminderComponents
{
	/// <summary>The custom id of the opt-out button; also the handler's <c>ComponentInteraction</c> attribute.</summary>
	public const string OptOutCustomId = "lottery_reminders:off";

	public const string OptOutLabel = "Turn off reminders";
	public const string OpenPageLabel = "Open lottery page";
	public const string PagePath = "/lottery";

	/// <summary>The reminder's components, in the order the spec lists them.</summary>
	public static MessageComponent Build(string webBaseUrl)
	{
		var builder = new ComponentBuilder()
			.WithButton(new ButtonBuilder
			{
				Label = OptOutLabel,
				CustomId = OptOutCustomId,
				Style = ButtonStyle.Secondary
			});

		// A link button with a relative or empty URL is rejected by Discord; without a configured base URL the post
		// keeps only the opt-out button.
		if (PageUrl(webBaseUrl) is { } pageUrl)
			builder.WithButton(new ButtonBuilder
			{
				Label = OpenPageLabel,
				Url = pageUrl,
				Style = ButtonStyle.Link
			});

		return builder.Build();
	}

	/// <summary><c>&lt;WebBaseUrl&gt;/lottery</c>, or <c>null</c> when the deployment has no web base URL.</summary>
	public static string? PageUrl(string webBaseUrl)
	{
		return string.IsNullOrWhiteSpace(webBaseUrl) ? null : webBaseUrl.TrimEnd('/') + PagePath;
	}

	/// <summary>
	///     Only the mentioned members may be pinged. No roles, no @everyone and no stray user id: the audience is the
	///     explicit set the composer rendered (spec §5.7, §11).
	/// </summary>
	public static AllowedMentions MentionsFor(IReadOnlyCollection<ulong> mentionedUserIds)
	{
		return mentionedUserIds.Count == 0
			? AllowedMentions.None
			: new AllowedMentions
			{
				AllowedTypes = AllowedMentionTypes.Users,
				UserIds = mentionedUserIds.ToList()
			};
	}
}
