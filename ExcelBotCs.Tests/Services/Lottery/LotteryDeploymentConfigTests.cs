using System.Text;
using ExcelBotCs.Extensions;
using ExcelBotCs.Models.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ExcelBotCs.Tests.Services.Lottery;

/// <summary>
///     Deployment configuration for the lottery (spec §7): the JSON file beats the class defaults, environment
///     variables beat the JSON file, and the new keys are registered through the same <c>LoadSettings</c> path the
///     application uses. The environment is the real provider, so the casing rule from AGENTS.md
///     (<c>Section__Key</c>) is exercised rather than simulated.
/// </summary>
public class LotteryDeploymentConfigTests : IDisposable
{
	private static readonly string[] EnvironmentKeys =
	[
		"Lottery__Automation__Enabled",
		"Lottery__Automation__DryRun",
		"Lottery__Automation__BasePot",
		"Lottery__Automation__WeeklyIncrement"
	];

	private readonly Dictionary<string, string?> _originalValues = new();

	public void Dispose()
	{
		foreach (var key in EnvironmentKeys)
			Environment.SetEnvironmentVariable(key, _originalValues.GetValueOrDefault(key));

		GC.SuppressFinalize(this);
	}

	private void SetEnvironment(string key, string value)
	{
		if (!_originalValues.ContainsKey(key))
			_originalValues[key] = Environment.GetEnvironmentVariable(key);

		Environment.SetEnvironmentVariable(key, value);
	}

	private static T ReadOptions<T>(string json, bool withEnvironment) where T : class
	{
		var builder = new ConfigurationBuilder()
			.AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(json)));

		if (withEnvironment)
			builder.AddEnvironmentVariables();

		var configuration = builder.Build();

		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSingleton<IConfiguration>(configuration);
		services.LoadSettings(configuration);

		using var provider = services.BuildServiceProvider();
		return provider.GetRequiredService<IOptions<T>>().Value;
	}

	[Fact]
	public void LotteryAutomation_WithNothingConfigured_UsesTheDeploymentDefaults()
	{
		var options = ReadOptions<LotteryOptions>("""{ "Lottery": { "Automation": {} } }""", false);

		options.Automation.Enabled.ShouldBeFalse();
		options.Automation.DryRun.ShouldBeTrue();
		options.Automation.BasePot.ShouldBe(1_000_000);
		options.Automation.WeeklyIncrement.ShouldBe(1_000_000);
	}

	[Fact]
	public void LotteryAutomation_ApplicationSettingsBeatTheClassDefaults()
	{
		var options = ReadOptions<LotteryOptions>(
			"""{ "Lottery": { "Automation": { "Enabled": true, "DryRun": false, "BasePot": 5000000 } } }""", false);

		options.Automation.Enabled.ShouldBeTrue();
		options.Automation.DryRun.ShouldBeFalse();
		options.Automation.BasePot.ShouldBe(5_000_000);
		options.Automation.WeeklyIncrement.ShouldBe(1_000_000);
	}

	[Fact]
	public void LotteryAutomation_EnvironmentVariablesBeatApplicationSettings()
	{
		SetEnvironment("Lottery__Automation__Enabled", "true");
		SetEnvironment("Lottery__Automation__DryRun", "false");
		SetEnvironment("Lottery__Automation__BasePot", "21000000");
		SetEnvironment("Lottery__Automation__WeeklyIncrement", "2000000");

		var options = ReadOptions<LotteryOptions>(
			"""{ "Lottery": { "Automation": { "Enabled": false, "DryRun": true, "BasePot": 5000000, "WeeklyIncrement": 3000000 } } }""",
			true);

		options.Automation.Enabled.ShouldBeTrue();
		options.Automation.DryRun.ShouldBeFalse();
		options.Automation.BasePot.ShouldBe(21_000_000);
		options.Automation.WeeklyIncrement.ShouldBe(2_000_000);
	}

	[Theory]
	[InlineData("""{ "WebBaseUrl": "https://lottery.example.com/", "Jwt": { "Issuer": "https://issuer.example.com" } }""",
		"https://lottery.example.com")]
	[InlineData("""{ "Jwt": { "Issuer": "https://issuer.example.com" } }""", "https://issuer.example.com")]
	[InlineData("""{ }""", "")]
	public void WebBaseUrl_ResolvesWebBaseUrlThenTheJwtIssuer(string json, string expected)
	{
		var options = ReadOptions<WebBaseUrlOptions>(json, false);

		options.BaseUrl.ShouldBe(expected);
	}

	[Fact]
	public void DiscordBot_LotteryDeploymentIdsAreBound()
	{
		var options = ReadOptions<DiscordBotOptions>(
			"""
			{
			  "DiscordBot": {
			    "Enabled": false,
			    "RiddlesChannel": "111111111111111111",
			    "LotteryMemberRoleId": "222222222222222222",
			    "LotteryCustomEmojiId": "333333333333333333"
			  }
			}
			""", false);

		options.RiddlesChannel.ShouldBe(111111111111111111ul);
		options.LotteryMemberRoleId.ShouldBe(222222222222222222ul);
		options.LotteryCustomEmojiId.ShouldBe(333333333333333333ul);
	}

	[Fact]
	public void DiscordBot_MissingLotteryIdsBindToZeroInsteadOfThrowing()
	{
		// The scheduler never starts with these missing (§5.10 degraded start), but reading the options must not
		// throw: the shipped appsettings leaves them unset until a deployment fills them in.
		var options = ReadOptions<DiscordBotOptions>(
			"""{ "DiscordBot": { "Enabled": false, "GuildId": "1", "LotteryChannel": "2" } }""", false);

		options.RiddlesChannel.ShouldBe(0ul);
		options.LotteryMemberRoleId.ShouldBe(0ul);
		options.LotteryCustomEmojiId.ShouldBe(0ul);
	}
}
