using ExcelBotCs.Services.Lottery.Records;

namespace ExcelBotCs.Services.Lottery.Interfaces;

/// <summary>
///     The reminder seam (spec §5.7): the member opt-in, its audience, the once-per-week scheduled post, the officer's
///     manual reminder and the admin test send. The scheduler drives <see cref="TickAsync" />; the slash commands drive
///     the preference and the manual send; the admin page (#114) drives the test send.
/// </summary>
public interface ILotteryReminderService
{
	/// <summary>The member's stored opt-in; absent means off.</summary>
	Task<LotteryReminderPreference> GetPreferenceAsync(ulong discordId, CancellationToken cancellationToken = default);

	/// <summary>
	///     Turns the member's reminder on or off and returns the resulting state. Idempotent, and deliberately free of
	///     any FC membership check: the opt-out button must keep working after the caller left the FC.
	/// </summary>
	Task<LotteryReminderPreference> SetPreferenceAsync(ulong discordId, bool enabled,
		CancellationToken cancellationToken = default);

	/// <summary>
	///     Current FC members who opted in and have at least one unused guess for the upcoming round, ordered by
	///     remaining count (descending) then Discord id.
	/// </summary>
	Task<LotteryReminderAudience> GetAudienceAsync(CancellationToken cancellationToken = default);

	/// <summary>
	///     One scheduler tick: posts this week's reminder when it is due and nothing has been posted for the round yet.
	///     Honours the deployment kill switch, the pause and the reminder toggle. Never throws.
	/// </summary>
	Task<LotteryReminderOutcome> TickAsync(CancellationToken cancellationToken = default);

	/// <summary>
	///     The officer's `/lottery remind`: the same audience and copy, posted immediately, regardless of the window or
	///     the toggles. When nobody is eligible nothing is posted and the outcome says why.
	/// </summary>
	Task<LotteryReminderOutcome> RemindNowAsync(CancellationToken cancellationToken = default);

	/// <summary>
	///     Sends the reminder copy to the requesting admin only (a DM), never to the channel and never pinging the
	///     audience. This is the admin page's "Send test reminder" (#114).
	/// </summary>
	Task<LotteryReminderOutcome> SendTestAsync(ulong requestingAdminDiscordId,
		CancellationToken cancellationToken = default);
}
