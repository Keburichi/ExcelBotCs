namespace ExcelBotCs.Models.DTO;

/// <summary>
///     The admin Overview payload (spec §5.9, §8: <c>GET /api/lottery/admin/status</c>). It carries everything the
///     page shows without a second round-trip: the status pill inputs, the pot and the needs-attention counts, the
///     latest and blocking rounds, the next run, the last settings change and the deployment ids that are
///     deliberately read-only. All Discord ids are strings because a snowflake does not survive a JavaScript number.
/// </summary>
public sealed record LotteryAdminStatus(
	string AutomationStatus,
	string AutomationLabel,
	bool SchedulerEnabled,
	bool AutomationEnabled,
	bool DryRun,
	bool PauseAutomation,
	bool Degraded,
	bool Blocked,
	long Pot,
	int PrizeCount,
	int PendingPayouts,
	int PoolPrizes,
	int OptedIn,
	int EligibleReminders,
	bool RemindersEnabled,
	LotteryAdminRound? LatestRound,
	LotteryAdminRound? PendingRound,
	DateTime? NextRunAtUtc,
	string? NextRoundKey,
	string? LastError,
	LotteryAdminDeployment Deployment,
	LotteryAdminSettingsChange? LastSettingsChange,
	IReadOnlyList<string> InvalidSettingsFields);

/// <summary>One round as the admin page reads it: the drawn result, the failure detail and the pot movement.</summary>
public sealed record LotteryAdminRound(
	string RoundKey,
	string Status,
	DateTime ScheduledAt,
	DateTime? RunAt,
	int? WinningNumber,
	long? PotBefore,
	long? PotAfter,
	string? FailedStep,
	int Attempts,
	DateTime? NextRetryAt,
	string? LastError,
	string? AnnouncementMessageId);

/// <summary>
///     The deployment configuration the admin page only displays (spec §7): role, channel and emoji ids plus the web
///     base URL. None of it is admin-editable, so it never appears in the settings form.
/// </summary>
public sealed record LotteryAdminDeployment(
	string LotteryChannelId,
	string LotteryMemberRoleId,
	string LotteryCustomEmojiId,
	string WebBaseUrl);

/// <summary>The newest audit row of <c>lottery_settings_history</c>, shown as "last settings change".</summary>
public sealed record LotteryAdminSettingsChange(
	int Version,
	DateTime ChangedAtUtc,
	string? ActorDiscordId,
	IReadOnlyList<string> Fields);

/// <summary>
///     The settings form as the page pre-fills it (spec §5.9 "Schedule &amp; reminders" and "Messages"). It carries
///     the admin-editable fields, the read-only automation flags the status pill and the form headings show, the
///     version/actor of the stored document, the template defaults for "Reset to default" and the fields the stored
///     document had to repair, so the page can flag a degraded settings document.
/// </summary>
public sealed record LotterySettingsDto(
	string RunDay,
	string RunTime,
	string RunTimezone,
	string ReminderDay,
	string ReminderTime,
	string ReminderTimezone,
	bool RemindersEnabled,
	bool PauseAutomation,
	string WinTemplate,
	string NoWinTemplate,
	int Version,
	string? UpdatedByDiscordId,
	bool AutomationEnabled,
	bool DryRun,
	string AutomationStatus,
	string AutomationLabel,
	long BasePot,
	long WeeklyIncrement,
	IReadOnlyList<string> InvalidFields,
	LotterySettingsDefaultsDto Defaults);

/// <summary>The defaults every reset action on the page restores; template copy included.</summary>
public sealed record LotterySettingsDefaultsDto(
	string RunDay,
	string RunTime,
	string RunTimezone,
	string ReminderDay,
	string ReminderTime,
	string ReminderTimezone,
	string WinTemplate,
	string NoWinTemplate);

/// <summary>
///     One <c>PUT /api/lottery/admin/settings</c> body: the whole admin-editable set, because save is the only commit
///     (§5.9). <see cref="ResetTemplates" /> restores the §5.6 default copy regardless of the template fields, so the
///     page's "Reset to default" cannot be defeated by stale editor text.
/// </summary>
public sealed record LotterySettingsSaveRequest(
	string RunDay,
	string RunTime,
	string RunTimezone,
	string ReminderDay,
	string ReminderTime,
	string ReminderTimezone,
	bool RemindersEnabled,
	bool PauseAutomation,
	string WinTemplate,
	string NoWinTemplate,
	bool ResetTemplates);

/// <summary>The result of a settings save: accepted (with or without a change) or rejected field by field.</summary>
public sealed record LotterySettingsSaveResponse(
	bool Success,
	bool Changed,
	LotterySettingsDto? Settings,
	IReadOnlyList<LotteryFieldError> Errors);

/// <summary>
///     One rejected form field; <see cref="Field" /> is the DTO property name the editor binds to, so the page can
///     highlight the input without mapping prose back to a control.
/// </summary>
public sealed record LotteryFieldError(string Field, string Message);

/// <summary>
///     One <c>POST /api/lottery/admin/pot</c> body (spec §8): a positive <see cref="Amount" /> moved by
///     <see cref="Direction" /> (<c>increase</c> or <c>decrease</c>), a reason that goes in the ledger, and an optional
///     idempotency key so a double submit of the same form cannot move the pot twice.
/// </summary>
public sealed record PotAdjustmentRequest(
	long Amount,
	string Reason,
	string Direction,
	string? IdempotencyKey);

/// <summary>
///     The pot after an adjustment: the new balance, what was actually applied (a correction that would cross the
///     1,000,000 Gil floor is clamped, §5.4), the ledger entry, and whether an idempotency key replayed a stored entry.
/// </summary>
public sealed record PotAdjustmentResponse(
	bool Success,
	bool Replayed,
	long Balance,
	long RequestedAmount,
	long AppliedAmount,
	LotteryLedgerEntryDto? Entry,
	IReadOnlyList<LotteryFieldError> Errors);

/// <summary>One append-only pot ledger row, as the admin ledger table shows it.</summary>
public sealed record LotteryLedgerEntryDto(
	string Id,
	string? RoundKey,
	string Type,
	long Amount,
	string Reason,
	string? ActorDiscordId,
	long BalanceAfter,
	string IdempotencyKey,
	DateTime CreatedAtUtc);

/// <summary>One keyset page of the ledger, newest first; <see cref="NextCursor" /> is absent on the last page.</summary>
public sealed record LotteryLedgerPage(
	IReadOnlyList<LotteryLedgerEntryDto> Items,
	string? NextCursor);

/// <summary>
///     One winner row of the admin payouts table: the round, the guess, the Gil share, the bonus items, and the payout
///     state with its actor, time and note (spec §5.4, §5.9). The <c>BonusItems</c> list is the one the public history
///     card reads, so an assigned pool prize is visible in both places.
/// </summary>
public sealed record LotteryPayoutDto(
	string Id,
	string RoundKey,
	string? DiscordId,
	string DisplayName,
	int Guess,
	long GilPrize,
	IReadOnlyList<string> Items,
	string PayoutStatus,
	string? PayoutActorDiscordId,
	DateTime? PayoutAt,
	string? PayoutNote);

/// <summary>
///     One keyset page of the payouts table. <see cref="PendingCount" /> is the collection-wide number the Overview
///     shows, not the page's. <see cref="NextCursor" /> is absent on the last page.
/// </summary>
public sealed record LotteryPayoutPage(
	IReadOnlyList<LotteryPayoutDto> Items,
	string? NextCursor,
	int PendingCount);

/// <summary>
///     One <c>PATCH /api/lottery/admin/winners/{roundKey}/{winnerId}/payout</c> body: the new state (<c>pending</c>,
///     <c>paid</c> or <c>forfeited</c>) and an optional note that is stored next to the actor and the time.
/// </summary>
public sealed record PayoutUpdateRequest(string Status, string? Note);

/// <summary>
///     The payout update outcome. <see cref="Code" /> is one of the typed codes (<c>updated</c>, <c>unchanged</c>,
///     <c>not-found</c>, <c>invalid-status</c>, <c>invalid-transition</c>); <see cref="ReturnedPrizes" /> names the pool
///     items a forfeit handed back.
/// </summary>
public sealed record PayoutUpdateResponse(
	bool Success,
	string Code,
	string Message,
	LotteryPayoutDto? Payout,
	IReadOnlyList<string> ReturnedPrizes);

/// <summary>
///     One non-Gil prize item (spec §5.5): the pool entry, its state and — once awarded — the winner it belongs to.
///     The item is indivisible, so an awarded entry names exactly one winner and one round.
/// </summary>
public sealed record PrizePoolItem(
	string Id,
	string Name,
	string Status,
	string? AwardedToDiscordId,
	string? AwardedToName,
	string? AwardedRoundKey,
	string? AddedByDiscordId,
	DateTime AddedAt);

/// <summary>The prize pool card: every item plus the in-pool and awarded counts the pot summary shows.</summary>
public sealed record LotteryPrizePool(
	IReadOnlyList<PrizePoolItem> Items,
	int InPoolCount,
	int AwardedCount);

/// <summary>One <c>POST /api/lottery/admin/prizes</c> body: the item's name, which is its only editable field.</summary>
public sealed record CreatePrizeRequest(string Name);

/// <summary>One <c>POST /api/lottery/admin/prizes/{id}/assign</c> body: the round and the winner row to link it to.</summary>
public sealed record AssignPrizeRequest(string RoundKey, string WinnerId);

/// <summary>
///     The prize mutation outcome. <see cref="Code" /> is a typed code (<c>created</c>, <c>removed</c>,
///     <c>assigned</c>, <c>not-found</c>, <c>invalid</c>, <c>duplicate</c>, <c>awarded</c>,
///     <c>invalid-transition</c>). <see cref="Winner" /> is the winner after an assignment, so the payout row updates
///     in place.
/// </summary>
public sealed record PrizeResponse(
	bool Success,
	string Code,
	string Message,
	PrizePoolItem? Prize,
	LotteryPayoutDto? Winner,
	IReadOnlyList<LotteryFieldError> Errors);

/// <summary>
///     One opted-in member on the Schedule &amp; reminders tab. Names come from the member record when there is one; an
///     id with no member row is still listed (opt-in survives leaving the FC) but is not eligible to be pinged.
/// </summary>
public sealed record ReminderOptInDto(
	string DiscordId,
	string? DisplayName,
	bool IsCurrentMember,
	bool IsEligible,
	DateTime OptedInAtUtc);

/// <summary>The opt-in list and the two counts: everyone opted in, and the audience the reminder would actually ping.</summary>
public sealed record LotteryOptIns(
	IReadOnlyList<ReminderOptInDto> Items,
	int OptedIn,
	int Eligible);

/// <summary>
///     The outcome of <c>POST /api/lottery/admin/reminders/test</c>: the reminder's typed disposition. It is delivered to
///     the requesting admin only — never the channel and never the audience — so <see cref="Recipients" /> counts who the
///     real post would ping, not who received the test.
/// </summary>
public sealed record ReminderTestResponse(
	bool Posted,
	string Disposition,
	string Message,
	string? RoundKey,
	int Recipients);

/// <summary>
///     One rendered announcement preview (spec §5.6, §5.9 Messages tab): the assembled post exactly as it would be
///     sent, the visible character count the editor shows against the 2,000-character budget, and the raw length that
///     counts the Discord markup.
/// </summary>
public sealed record LotteryAnnouncementPreviewDto(
	string Variant,
	string Text,
	int VisibleLength,
	int RawLength);

/// <summary>
///     The body of a manual round action: <c>run</c>, <c>skip</c>, <c>void</c>, <c>retry</c> and <c>dry-run</c>. The
///     reason is required for a void (the ledger reversal and the round annotation are audited with it) and optional
///     for a skip; the round key is optional everywhere and defaults to the round the action naturally targets.
/// </summary>
public sealed record ManualActionRequest(string? Reason, string? RoundKey);

/// <summary>
///     The typed outcome of a manual round action. <see cref="Disposition" /> is the engine's disposition verbatim, so
///     the page can tell "ran" from "already-run", "blocked" and "nothing-due" without reading the message.
/// </summary>
public sealed record ManualActionResponse(
	string Disposition,
	bool Success,
	string Message,
	string? RoundKey,
	long? Pot);
