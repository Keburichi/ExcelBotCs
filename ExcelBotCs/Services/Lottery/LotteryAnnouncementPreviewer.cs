using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.Services.Lottery.Records;
using Microsoft.Extensions.Options;

namespace ExcelBotCs.Services.Lottery;

/// <summary>
///     The admin preview of the assembled announcement (spec §5.9 Messages tab, for #114). It renders the real post
///     with the stored templates and the live deployment ids, using the worked example from #101 for the winner rows
///     and — where the data exists — the real current pot and prize pool, so the admin sees the same text and count
///     the next run would post.
/// </summary>
public class LotteryAnnouncementPreviewer : ILotteryAnnouncementPreviewer
{
	private readonly IOptions<DiscordBotOptions> _discordOptions;
	private readonly ILotteryPotLedgerService _pot;
	private readonly ILotteryPrizePoolRepository _prizes;
	private readonly ILotterySettingsService _settings;
	private readonly IOptions<WebBaseUrlOptions> _webOptions;

	public LotteryAnnouncementPreviewer(
		ILotterySettingsService settings,
		ILotteryPotLedgerService pot,
		ILotteryPrizePoolRepository prizes,
		IOptions<DiscordBotOptions> discordOptions,
		IOptions<WebBaseUrlOptions> webOptions)
	{
		_settings = settings;
		_pot = pot;
		_prizes = prizes;
		_discordOptions = discordOptions;
		_webOptions = webOptions;
	}

	public async Task<LotteryAnnouncementPreview> RenderAsync(string variant,
		CancellationToken cancellationToken = default)
	{
		if (!LotteryAnnouncementVariant.IsKnown(variant))
			throw new ArgumentException(
				$"Unknown announcement variant '{variant}'. Use '{LotteryAnnouncementVariant.Win}' or " +
				$"'{LotteryAnnouncementVariant.NoWin}'.", nameof(variant));

		var settings = await _settings.GetEffectiveAsync(cancellationToken);
		var deployment = LotteryAnnouncementDeployment.From(_discordOptions.Value, _webOptions.Value, settings);

		var pot = await _pot.GetBalanceAsync(settings.BasePot, cancellationToken);
		var poolPrizes = await LotteryPrizePoolNames.InPoolAsync(_prizes, cancellationToken);

		var request = variant == LotteryAnnouncementVariant.Win
			? WinRequest(settings, pot, poolPrizes)
			: LotteryAnnouncementSamples.NoWinRequest(settings.NoWinTemplate,
				currentPot: pot + settings.WeeklyIncrement, poolPrizes: poolPrizes);

		var message = LotteryAnnouncementComposer.Compose(request with { Deployment = deployment });

		return new LotteryAnnouncementPreview(variant, message.Text, message.VisibleLength);
	}

	private static LotteryAnnouncementRenderRequest WinRequest(LotteryEffectiveSettings settings, long pot,
		IReadOnlyList<string> poolPrizes)
	{
		// The first pooled prize stands in as the winner's bonus item, so the {bonusItems} copy is visible in the
		// preview; the pot summary then lists what is left in the pool.
		var item = poolPrizes.FirstOrDefault();
		var winner = item is null
			? LotteryAnnouncementSamples.SampleWinnerWith()
			: LotteryAnnouncementSamples.SampleWinnerWith(item);

		return LotteryAnnouncementSamples.WinRequest(
			settings.WinTemplate,
			prizePot: pot,
			currentPot: settings.BasePot,
			poolPrizes: item is null ? poolPrizes : poolPrizes.Skip(1).ToList(),
			winners: [winner]);
	}
}
