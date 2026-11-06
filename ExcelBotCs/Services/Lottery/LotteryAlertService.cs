using System.Globalization;
using System.Text;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Services.Discord.Interfaces;
using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.Services.Lottery.Records;
using Microsoft.Extensions.Options;

namespace ExcelBotCs.Services.Lottery;

/// <summary>
///     Formats and posts the lottery alerts (spec §5.10). Every alert names the round, the failed step, the error, the
///     attempt count and the next retry, and always links the admin page; the admin role is pinged only when the alert
///     is a terminal/needs-admin one. The Discord post is wrapped so a broken channel can never turn into a broken run.
/// </summary>
public class LotteryAlertService : ILotteryAlertService
{
	private readonly IOptions<DiscordBotOptions> _discordOptions;
	private readonly ILogger<LotteryAlertService> _logger;
	private readonly IDiscordMessageService _messages;
	private readonly IOptions<WebBaseUrlOptions> _webOptions;

	public LotteryAlertService(IDiscordMessageService messages, IOptions<DiscordBotOptions> discordOptions,
		IOptions<WebBaseUrlOptions> webOptions, ILogger<LotteryAlertService> logger)
	{
		_messages = messages;
		_discordOptions = discordOptions;
		_webOptions = webOptions;
		_logger = logger;
	}

	public async Task PublishAsync(LotteryAlert alert, CancellationToken cancellationToken = default)
	{
		var message = Format(alert, AdminPageUrl());
		var adminRoleId = alert.NeedsAdmin ? ResolveAdminRoleId() : null;

		try
		{
			await _messages.PostAdminAlertAsync(message, adminRoleId);
		}
		catch (Exception exception)
		{
			// The round state is the record of truth; an unreachable Discord must not fail the step that raised the
			// alert, and must not mask the original error with a second one.
			_logger.LogWarning(exception,
				"Lottery alert '{Title}' for round {RoundKey} could not be posted to the log channel",
				alert.Title, alert.RoundKey ?? "unknown");
		}
	}

	/// <summary>
	///     The alert body. Kept public so the admin page and the tests can assert the exact shape without a Discord
	///     channel; every line except the generated body is optional, but the admin link is always present.
	/// </summary>
	public string Format(LotteryAlert alert, string adminPageUrl)
	{
		var builder = new StringBuilder();

		builder.Append(alert.NeedsAdmin ? "⚠️ Lottery needs an admin" : "Lottery alert")
			.Append(" — ").Append(alert.Title).AppendLine();
		builder.Append("Round: ").AppendLine(alert.RoundKey ?? "unknown");

		if (!string.IsNullOrWhiteSpace(alert.FailedStep))
			builder.Append("Failed step: ").AppendLine(alert.FailedStep);

		if (!string.IsNullOrWhiteSpace(alert.Error))
			builder.Append("Error: ").AppendLine(alert.Error);

		builder.Append("Attempts: ").AppendLine(alert.Attempts.ToString(CultureInfo.InvariantCulture));
		builder.Append("Next retry: ").AppendLine(alert.NextRetryAt is { } nextRetry
			? FormatInstant(nextRetry)
			: "none (the round is terminal or needs an admin)");
		builder.Append("Admin page: ").Append(adminPageUrl);

		return builder.ToString();
	}

	private string AdminPageUrl()
	{
		var baseUrl = _webOptions.Value.BaseUrl;
		return string.IsNullOrWhiteSpace(baseUrl) ? "/lottery/admin" : baseUrl.TrimEnd('/') + "/lottery/admin";
	}

	/// <summary>The first configured admin role is the alert ping (spec §7: the ids are deployment config).</summary>
	private ulong? ResolveAdminRoleId()
	{
		var roleId = _discordOptions.Value.AdminRoleIds.FirstOrDefault(candidate => candidate != 0);
		return roleId == 0 ? null : roleId;
	}

	private static string FormatInstant(DateTime instant)
	{
		var utc = instant.Kind == DateTimeKind.Utc ? instant : DateTime.SpecifyKind(instant, DateTimeKind.Utc);
		return utc.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture);
	}
}
