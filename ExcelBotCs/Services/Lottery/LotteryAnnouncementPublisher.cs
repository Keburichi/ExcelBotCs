using System.Globalization;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.Discord.Interfaces;
using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.Services.Lottery.Records;
using Microsoft.Extensions.Options;

namespace ExcelBotCs.Services.Lottery;

/// <summary>
///     The real announcement publisher (spec §5.6 and §5.10). It renders the post the composer and the winner history
///     describe, posts it as one plain message (never an embed, never pinned), and returns the message id so the round
///     can store <c>AnnouncementMessageId</c>.
///     <para>
///         Delivery rules: a dry run posts the same text with a <c>[DRY RUN]</c> prefix to the log channel and mentions
///         suppressed; a template that does not satisfy the contract — or a post that would exceed the 2,000-character
///         limit — falls back to the default copy and raises one needs-admin alert; and a restart that finds this
///         round's post in the channel adopts it instead of posting a second time (see
///         <see cref="LotteryAnnouncementReconciler" />). Posting failures are thrown on purpose: the runner records
///         them as an announcement failure and retries the same result without re-drawing.
///     </para>
/// </summary>
public class LotteryAnnouncementPublisher : ILotteryAnnouncementPublisher
{
	private readonly ILotteryAlertService _alerts;
	private readonly IOptions<DiscordBotOptions> _discordOptions;
	private readonly ILogger<LotteryAnnouncementPublisher> _logger;
	private readonly IDiscordMessageService _messages;
	private readonly ILotteryPrizePoolRepository _prizes;
	private readonly ILotterySettingsService _settings;
	private readonly IOptions<WebBaseUrlOptions> _webOptions;
	private readonly ILotteryWinnerRepository _winners;

	public LotteryAnnouncementPublisher(
		IDiscordMessageService messages,
		ILotterySettingsService settings,
		ILotteryWinnerRepository winners,
		ILotteryPrizePoolRepository prizes,
		ILotteryAlertService alerts,
		IOptions<DiscordBotOptions> discordOptions,
		IOptions<WebBaseUrlOptions> webOptions,
		ILogger<LotteryAnnouncementPublisher> logger)
	{
		_messages = messages;
		_settings = settings;
		_winners = winners;
		_prizes = prizes;
		_alerts = alerts;
		_discordOptions = discordOptions;
		_webOptions = webOptions;
		_logger = logger;
	}

	public async Task<LotteryAnnouncementPost> PublishAsync(LotteryAnnouncement announcement,
		CancellationToken cancellationToken = default)
	{
		var message = await RenderAsync(announcement, cancellationToken);
		var roundKey = announcement.Round.RoundKey;

		if (announcement.DryRun)
		{
			var preview = await _messages.PostDryRunAsync(
				LotteryAnnouncementReconciler.DryRunPrefix + " " + message.Text);

			if (preview is null)
				throw new InvalidOperationException(
					"The lottery dry run could not be posted: the log channel is unreachable.");

			_logger.LogInformation(
				"Dry run announcement for round {RoundKey} posted to the log channel ({Length} characters)",
				roundKey, message.RawLength);

			return new LotteryAnnouncementPost(null);
		}

		var posted = await _messages.PostLotteryAnnouncementAsync(message.Text);

		if (posted is null)
			throw new InvalidOperationException(
				"The lottery announcement could not be posted: the lottery channel is unreachable.");

		var messageId = posted.Id.ToString(CultureInfo.InvariantCulture);
		_logger.LogInformation("Round {RoundKey} announcement posted as message {MessageId} ({Length} characters)",
			roundKey, messageId, message.RawLength);

		return new LotteryAnnouncementPost(messageId);
	}

	public async Task<string?> FindExistingPostAsync(LotteryRound round,
		CancellationToken cancellationToken = default)
	{
		var messages = await _messages.GetRecentLotteryChannelMessagesAsync(
			LotteryAnnouncementReconciler.RecentMessageLimit) ?? [];

		if (messages.Count == 0) return null;

		var winners = await _winners.GetByRoundKeyAsync(round.RoundKey);
		var rendered = await RenderAsync(
			new LotteryAnnouncement(round, winners, round.PotBefore ?? 0, round.PotAfter ?? 0, false),
			cancellationToken);

		var match = messages.FirstOrDefault(message =>
			LotteryAnnouncementReconciler.IsThisRoundsPost(message, round, rendered.Text));

		return match?.Id.ToString(CultureInfo.InvariantCulture);
	}

	public async Task AnnotateVoidedAsync(LotteryRound round, string reason,
		CancellationToken cancellationToken = default)
	{
		await _messages.PostInLotteryChannelAsync(
			$"⚠️ The announcement of lottery round {round.RoundKey} is void: {reason}. The pot movements of the round " +
			$"were reversed, so do not pay out the result above. Admin page: {AdminPageUrl()}");
	}

	private async Task<LotteryAssembledMessage> RenderAsync(LotteryAnnouncement announcement,
		CancellationToken cancellationToken)
	{
		var settings = await _settings.GetEffectiveAsync(cancellationToken);
		var deployment = LotteryAnnouncementDeployment.From(_discordOptions.Value, _webOptions.Value, settings);

		// Run-time template enforcement: the save path rejects a bad template, but a hand-edited document or an older
		// build can still put one in the database. Fall back per variant and tell the admins (spec §5.6).
		var winTemplate = settings.WinTemplate;
		var winValidation = LotteryTemplateValidator.ValidateWinTemplate(winTemplate);
		if (!winValidation.IsValid)
		{
			await AlertInvalidTemplateAsync(announcement.Round.RoundKey, "winner", winValidation.Error!,
				cancellationToken);
			winTemplate = LotterySettingsDefaults.WinTemplate;
		}

		var noWinTemplate = settings.NoWinTemplate;
		var noWinValidation = LotteryTemplateValidator.ValidateNoWinTemplate(noWinTemplate);
		if (!noWinValidation.IsValid)
		{
			await AlertInvalidTemplateAsync(announcement.Round.RoundKey, "no-winner", noWinValidation.Error!,
				cancellationToken);
			noWinTemplate = LotterySettingsDefaults.NoWinTemplate;
		}

		var lastFive = await ReadLastFiveAsync(announcement);
		var poolPrizes = await LotteryPrizePoolNames.InPoolAsync(_prizes, cancellationToken);

		var request = new LotteryAnnouncementRenderRequest(
			winTemplate,
			noWinTemplate,
			announcement.Round.WinningNumber ?? 0,
			announcement.PotBefore,
			announcement.PotAfter,
			announcement.Winners,
			lastFive,
			poolPrizes,
			deployment);

		var message = LotteryAnnouncementComposer.Compose(request);

		// The live ids can be longer than the save-time measurement assumed; never send a message Discord rejects.
		if (message.RawLength >= LotteryAnnouncementComposer.MaxMessageLength)
		{
			await AlertInvalidTemplateAsync(announcement.Round.RoundKey, "assembled",
				$"The assembled announcement is " +
				$"{message.RawLength.ToString("N0", CultureInfo.InvariantCulture)} characters, over the " +
				$"{LotteryAnnouncementComposer.MaxMessageLength.ToString("N0", CultureInfo.InvariantCulture)} " +
				"character limit.", cancellationToken);

			message = LotteryAnnouncementComposer.Compose(request with
			{
				WinTemplate = LotterySettingsDefaults.WinTemplate,
				NoWinTemplate = LotterySettingsDefaults.NoWinTemplate
			});

			// Even the default copy does not fit: posting it would make Discord reject the message, so fail the step
			// and let the runner record it instead of pretending the announcement went out.
			if (message.RawLength >= LotteryAnnouncementComposer.MaxMessageLength)
				throw new InvalidOperationException(
					$"The lottery announcement is " +
					$"{message.RawLength.ToString("N0", CultureInfo.InvariantCulture)} characters even with the " +
					"default templates; Discord rejects messages over 2,000. The winner history is too long to post.");
		}

		return message;
	}

	/// <summary>
	///     The five newest winner rows. A dry run has not persisted its simulated winners, so they are prepended to the
	///     stored history — the preview then shows the rows the real post would (§5.6: the current winner is included,
	///     and a shared draw contributes one row per winner).
	/// </summary>
	private async Task<IReadOnlyList<LotteryWinner>> ReadLastFiveAsync(LotteryAnnouncement announcement)
	{
		var stored = await _winners.GetLatestAsync(5) ?? [];

		if (!announcement.DryRun || announcement.Winners.Count == 0) return stored;

		var simulated = announcement.Winners.ToList();
		simulated.AddRange(stored.Where(candidate => !announcement.Winners.Any(winner =>
			winner.RoundKey == candidate.RoundKey && winner.DiscordId == candidate.DiscordId
			                                      && winner.Guess == candidate.Guess)));

		return simulated.Take(5).ToList();
	}

	private async Task AlertInvalidTemplateAsync(string roundKey, string which, string error,
		CancellationToken cancellationToken)
	{
		_logger.LogWarning(
			"Round {RoundKey} has an invalid {Which} template and falls back to the default: {Error}", roundKey,
			which, error);

		await _alerts.PublishAsync(new LotteryAlert(
			Title: $"The stored lottery {which} template is invalid; the default was posted",
			RoundKey: roundKey,
			FailedStep: LotteryFailedStep.Announcement,
			Error: error,
			Attempts: 0,
			NextRetryAt: null,
			NeedsAdmin: true), cancellationToken);
	}

	private string AdminPageUrl()
	{
		var baseUrl = _webOptions.Value.BaseUrl;
		return string.IsNullOrWhiteSpace(baseUrl) ? "/lottery/admin" : baseUrl.TrimEnd('/') + "/lottery/admin";
	}
}
