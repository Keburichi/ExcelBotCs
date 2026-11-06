using ExcelBotCs.Models.Config;
using ExcelBotCs.Utilities;
using ExcelBotCs.Modules.Lottery;

namespace ExcelBotCs.Services.Lottery.Records;

/// <summary>
///     The deployment half of the announcement (spec §7): the ids that are read-only for the admin page and the
///     configured run day/time used for the deadline. A zero id means "not configured" and the composer falls back to
///     plain text instead of posting a broken mention.
/// </summary>
public sealed record LotteryAnnouncementDeployment(
	ulong MemberRoleId,
	ulong EventsChannelId,
	ulong RiddlesChannelId,
	ulong CustomEmojiId,
	string WebBaseUrl,
	string RunDay,
	string RunTime)
{
	/// <summary>A deliberately unconfigured deployment, used to measure a template without live ids.</summary>
	public static LotteryAnnouncementDeployment Empty { get; } =
		new(0, 0, 0, 0, string.Empty, "Tuesday", "20:00");

	/// <summary>The live deployment: the ids from <c>DiscordBot</c>, the web base URL and the stored run day/time.</summary>
	public static LotteryAnnouncementDeployment From(DiscordBotOptions discord, WebBaseUrlOptions web,
		LotteryEffectiveSettings settings)
	{
		return new LotteryAnnouncementDeployment(
			discord.LotteryMemberRoleId,
			discord.EventsChannel,
			discord.RiddlesChannel,
			discord.LotteryCustomEmojiId,
			web.BaseUrl,
			settings.RunDay,
			settings.RunTime);
	}
}

/// <summary>
///     Everything the composer needs to render one announcement post. The winners are the round's current winner
///     rows; <see cref="LastFive" /> is the newest-first history read from the winner collection (the current winners
///     are already in it, because the draw is persisted before the post). <see cref="PrizePot" /> is the pot at the
///     draw (what the winner or winners take) and <see cref="CurrentPot" /> the pot after the round (what the post
///     shows). <see cref="PoolPrizes" /> are the non-Gil items still in the pool, named in the pot summary.
/// </summary>
public sealed record LotteryAnnouncementRenderRequest(
	string WinTemplate,
	string NoWinTemplate,
	int WinningNumber,
	long PrizePot,
	long CurrentPot,
	IReadOnlyList<LotteryWinner> Winners,
	IReadOnlyList<LotteryWinner> LastFive,
	IReadOnlyList<string> PoolPrizes,
	LotteryAnnouncementDeployment Deployment);

/// <summary>
///     The finished post. <see cref="Text" /> is what the bot sends (raw Discord markup included);
///     <see cref="VisibleLength" /> is what a member reads (see <see cref="DiscordVisibleText" />).
/// </summary>
public sealed record LotteryAssembledMessage(string Text)
{
	public int RawLength => Text.Length;

	public int VisibleLength => DiscordVisibleText.Measure(Text);
}

/// <summary>The two result-block variants the admin page previews and the run renders (spec §5.6).</summary>
public static class LotteryAnnouncementVariant
{
	public const string Win = "win";
	public const string NoWin = "nowin";

	public static readonly IReadOnlyList<string> All = [Win, NoWin];

	public static bool IsKnown(string? variant)
	{
		return variant is Win or NoWin;
	}
}

/// <summary>
///     The rendered preview the admin page (#114) shows next to the template editor: the assembled post and its
///     visible character count, so an admin can see the 2,000-character budget before saving.
/// </summary>
public sealed record LotteryAnnouncementPreview(string Variant, string Text, int VisibleLength)
{
	public int RawLength => Text.Length;
}
