namespace ExcelBotCs.Services.Lottery.Records;

/// <summary>
///     One alert for the log channel (spec §5.10). <see cref="NeedsAdmin" /> is what decides the admin ping: only a
///     terminal or needs-admin state (<c>failed</c>, a stuck round, an invalid template fallback) pings, a scheduled
///     retry only logs. <see cref="Title" /> is the human summary; the rest fills the machine-readable fields the
///     admin needs to act.
/// </summary>
public sealed record LotteryAlert(
	string Title,
	string? RoundKey = null,
	string? FailedStep = null,
	string? Error = null,
	int Attempts = 0,
	DateTime? NextRetryAt = null,
	bool NeedsAdmin = true);
