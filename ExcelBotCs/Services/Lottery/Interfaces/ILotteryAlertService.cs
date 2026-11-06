using ExcelBotCs.Services.Lottery.Records;

namespace ExcelBotCs.Services.Lottery.Interfaces;

/// <summary>
///     The alert seam for the automated lottery (spec §5.10, criterion 16). It posts to the log channel — the Discord
///     service falls back to the lottery channel when no log channel is configured — and pings the admin role only for
///     a terminal or needs-admin state. Publishing never throws: an alert is a side effect and must not change the
///     outcome of the run that raised it.
/// </summary>
public interface ILotteryAlertService
{
	Task PublishAsync(LotteryAlert alert, CancellationToken cancellationToken = default);
}
