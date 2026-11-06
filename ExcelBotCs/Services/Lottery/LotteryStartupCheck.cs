using ExcelBotCs.Models.Config;
using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.Services.Lottery.Records;
using Microsoft.Extensions.Options;

namespace ExcelBotCs.Services.Lottery;

/// <summary>
///     The startup guard for the automated lottery (spec §5.10, §7). Every problem it finds is collected, then
///     reported as a single error log line — the existing <see cref="Discord.DiscordLogger" /> mirrors that line into
///     the log channel, so there is exactly one alert and no second alerting mechanism. Nothing in here throws: a
///     lottery misconfiguration must never take the web app or the bot down.
/// </summary>
public class LotteryStartupCheck : ILotteryStartupCheck
{
	private readonly IOptions<DiscordBotOptions> _discordOptions;
	private readonly ILogger<LotteryStartupCheck> _logger;
	private readonly IOptions<LotteryOptions> _lotteryOptions;
	private readonly ILotterySettingsService _settingsService;
	private readonly IOptions<WebBaseUrlOptions> _webOptions;

	public LotteryStartupCheck(
		IOptions<DiscordBotOptions> discordOptions,
		IOptions<LotteryOptions> lotteryOptions,
		IOptions<WebBaseUrlOptions> webOptions,
		ILotterySettingsService settingsService,
		ILogger<LotteryStartupCheck> logger)
	{
		_discordOptions = discordOptions;
		_lotteryOptions = lotteryOptions;
		_webOptions = webOptions;
		_settingsService = settingsService;
		_logger = logger;
	}

	public async Task<LotteryStartupResult> CheckAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			return await CheckCoreAsync(cancellationToken);
		}
		catch (Exception exception)
		{
			// The guard itself failing (an options binder bug, a disposed provider) is still a degraded start,
			// not a crash.
			_logger.LogError(exception, "Lottery startup check failed; lottery automation starts degraded");
			return LotteryStartupResult.Degraded([$"The lottery startup check failed: {exception.Message}"]);
		}
	}

	private async Task<LotteryStartupResult> CheckCoreAsync(CancellationToken cancellationToken)
	{
		var discord = _discordOptions.Value;

		// Without a bot there is no announcement, no reminder and no scheduler: nothing to degrade and nobody to
		// alert. Botless checkouts (parallel worktrees, CI) therefore stay quiet.
		if (!discord.Enabled)
		{
			_logger.LogInformation("Lottery startup check skipped: DiscordBot:Enabled is false");
			return LotteryStartupResult.Healthy;
		}

		var problems = new List<string>();

		AddMissingId(problems, "DiscordBot:GuildId", discord.GuildId);
		AddMissingId(problems, "DiscordBot:LotteryChannel", discord.LotteryChannel);
		AddMissingId(problems, "DiscordBot:LotteryMemberRoleId", discord.LotteryMemberRoleId);
		AddMissingId(problems, "DiscordBot:RiddlesChannel", discord.RiddlesChannel);

		// DiscordBot:LotteryCustomEmojiId is deliberately absent: without it the template uses the literal text
		// (§5.6 "an id is configured"), which is a fallback rather than a fault.

		var automation = _lotteryOptions.Value.Automation;
		if (automation.BasePot <= 0)
			problems.Add($"Lottery:Automation:BasePot must be greater than zero (configured: {automation.BasePot}).");
		if (automation.WeeklyIncrement < 0)
			problems.Add(
				$"Lottery:Automation:WeeklyIncrement must not be negative (configured: {automation.WeeklyIncrement}).");

		AddWebBaseUrlProblems(problems);
		await AddStoredSettingsProblemsAsync(problems, cancellationToken);

		if (problems.Count == 0)
		{
			_logger.LogInformation("Lottery automation configuration is valid");
			return LotteryStartupResult.Healthy;
		}

		// One line, one alert: the Discord log channel receives the whole list at once.
		_logger.LogError(
			"Lottery automation starts degraded and will not schedule until the configuration is fixed: {ProblemCount} " +
			"problem(s): {Problems}",
			problems.Count, string.Join(" ", problems));

		return LotteryStartupResult.Degraded(problems);
	}

	private void AddWebBaseUrlProblems(List<string> problems)
	{
		var baseUrl = _webOptions.Value.BaseUrl;
		if (string.IsNullOrWhiteSpace(baseUrl))
		{
			problems.Add("WebBaseUrl (or Jwt:Issuer) is not configured; announcement history links would be broken.");
			return;
		}

		if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) ||
		    (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
			problems.Add($"WebBaseUrl '{baseUrl}' is not an absolute http(s) URL.");
	}

	private async Task AddStoredSettingsProblemsAsync(List<string> problems, CancellationToken cancellationToken)
	{
		LotteryEffectiveSettings effective;
		try
		{
			effective = await _settingsService.GetEffectiveAsync(cancellationToken);
		}
		catch (Exception exception)
		{
			problems.Add($"lottery_settings could not be read: {exception.Message}");
			return;
		}

		foreach (var field in effective.InvalidFields)
			problems.Add($"lottery_settings.{field} is invalid and fell back to its default.");
	}

	private static void AddMissingId(List<string> problems, string key, ulong value)
	{
		if (value == 0)
			problems.Add($"{key} is not configured (0).");
	}
}
