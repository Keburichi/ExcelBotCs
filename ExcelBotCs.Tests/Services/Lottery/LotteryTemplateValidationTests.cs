using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.Lottery;
using ExcelBotCs.Services.Lottery.Records;
using ExcelBotCs.Tests.Utils;

namespace ExcelBotCs.Tests.Services.Lottery;

/// <summary>
///     The result-template contract (spec §5.6, criterion 10): exactly seven placeholders, the required ones present,
///     and a fully assembled message under Discord's 2,000-character limit. The template is the admin-editable half
///     of the announcement, so every rule here is enforced on save as well as when a run renders the message.
/// </summary>
public class LotteryTemplateValidationTests
{
	[Fact]
	public void AllPlaceholders_AreExactlyTheDocumentedSet()
	{
		LotteryTemplateValidator.AllPlaceholders.ShouldBe(
			["{number}", "{winners}", "{prize}", "{bonusItems}", "{pot}", "{nextPot}", "{emoji}"]);
	}

	[Fact]
	public void ValidateWinTemplate_AcceptsTheDefault()
	{
		var validation = LotteryTemplateValidator.ValidateWinTemplate(LotterySettingsDefaults.WinTemplate);

		validation.IsValid.ShouldBeTrue();
		validation.Normalized.ShouldBe(LotterySettingsDefaults.WinTemplate);
	}

	[Fact]
	public void ValidateNoWinTemplate_AcceptsTheDefault()
	{
		var validation = LotteryTemplateValidator.ValidateNoWinTemplate(LotterySettingsDefaults.NoWinTemplate);

		validation.IsValid.ShouldBeTrue();
		validation.Normalized.ShouldBe(LotterySettingsDefaults.NoWinTemplate);
	}

	[Fact]
	public void ValidateWinTemplate_AcceptsEveryAllowedPlaceholder()
	{
		var templates = new[]
		{
			"the winning number was {number}! Congratulations to {winners}! 🎉\n" +
			"They take the {prize} pot{bonusItems}.\n" +
			"The jackpot resets to {nextPot} Gil for next week.\n" +
			"The pot was {pot} and {emoji} stays for the losers."
		};

		foreach (var template in templates)
			LotteryTemplateValidator.ValidateWinTemplate(template).IsValid.ShouldBeTrue(template);
	}

	[Theory]
	[InlineData("{number} {winners} {prize} {jackpot}")]
	[InlineData("{number} {winners} {prize} {Number}")]
	[InlineData("{number} {winners} {prize} {})")]
	[InlineData("{number} {winners} {prize} {pot }")]
	public void ValidateWinTemplate_RejectsAnUnknownPlaceholder(string template)
	{
		var validation = LotteryTemplateValidator.ValidateWinTemplate(template);

		validation.IsValid.ShouldBeFalse();
		validation.Error.ShouldNotBeNullOrWhiteSpace();
		validation.Error.ShouldContain("placeholder");
	}

	[Theory]
	[InlineData("{number} {pot} {jackpot}")]
	[InlineData("{number} {pot} {EMOJI}")]
	public void ValidateNoWinTemplate_RejectsAnUnknownPlaceholder(string template)
	{
		var validation = LotteryTemplateValidator.ValidateNoWinTemplate(template);

		validation.IsValid.ShouldBeFalse();
		validation.Error.ShouldContain("placeholder");
	}

	[Theory]
	[InlineData("Congratulations to {winners}! They take the {prize} pot.", "{number}")]
	[InlineData("the winning number was {number}! They take the {prize} pot.", "{winners}")]
	[InlineData("the winning number was {number}! Congratulations to {winners}!", "{prize}")]
	public void ValidateWinTemplate_RejectsAMissingRequiredPlaceholder(string template, string missing)
	{
		var validation = LotteryTemplateValidator.ValidateWinTemplate(template);

		validation.IsValid.ShouldBeFalse();
		validation.Error.ShouldContain(missing);
		validation.Error.ShouldContain("required");
	}

	[Theory]
	[InlineData("So no one won this week again {emoji}. The jackpot goes up to {pot}!!", "{number}")]
	[InlineData("the winning number was {number}! So no one won this week again {emoji}.", "{pot}")]
	public void ValidateNoWinTemplate_RejectsAMissingRequiredPlaceholder(string template, string missing)
	{
		var validation = LotteryTemplateValidator.ValidateNoWinTemplate(template);

		validation.IsValid.ShouldBeFalse();
		validation.Error.ShouldContain(missing);
	}

	[Fact]
	public void ValidateWinTemplate_RejectsATemplateThatPushesTheMessageOverTheLimit()
	{
		var template = LotterySettingsDefaults.WinTemplate + "\n" + new string('x', 1_500);

		var validation = LotteryTemplateValidator.ValidateWinTemplate(template);

		validation.IsValid.ShouldBeFalse();
		validation.Error.ShouldContain("2,000");
	}

	[Fact]
	public void ValidateNoWinTemplate_RejectsATemplateThatPushesTheMessageOverTheLimit()
	{
		var template = LotterySettingsDefaults.NoWinTemplate + "\n" + new string('y', 1_500);

		var validation = LotteryTemplateValidator.ValidateNoWinTemplate(template);

		validation.IsValid.ShouldBeFalse();
		validation.Error.ShouldContain("2,000");
	}

	[Fact]
	public void ValidateWinTemplate_AcceptsATemplateThatIsLongButStillUnderTheLimit()
	{
		var template = LotterySettingsDefaults.WinTemplate + "\n" + new string('z', 400);

		var validation = LotteryTemplateValidator.ValidateWinTemplate(template);

		validation.IsValid.ShouldBeTrue();
	}

	[Fact]
	public void Validate_ReportsBothTemplateFieldsByName()
	{
		var errors = LotterySettingsValidator.Validate(LotterySettingsUpdate.FromDefaults() with
		{
			WinTemplate = "no placeholders at all",
			NoWinTemplate = "still no {unknown} places"
		});

		errors.Select(error => error.Field).ShouldBe(["WinTemplate", "NoWinTemplate"]);
	}

	[Fact]
	public void Validate_AcceptsTheDocumentedDefaultTemplates()
	{
		LotterySettingsValidator.Validate(LotterySettingsUpdate.FromDefaults()).ShouldBeEmpty();
	}

	[Fact]
	public void ToUpdate_PrefillsTheAdminFormWithTheStoredTemplates()
	{
		var settings = TestLotterySettings.Create(
			winTemplate: "custom {number} {winners} {prize}",
			noWinTemplate: "custom {number} {pot}");

		var prefilled = settings.ToUpdate();

		prefilled.WinTemplate.ShouldBe("custom {number} {winners} {prize}");
		prefilled.NoWinTemplate.ShouldBe("custom {number} {pot}");
		LotterySettingsValidator.Validate(prefilled).ShouldBeEmpty();
	}

	[Fact]
	public void ResetTemplates_RestoresTheDocumentedDefaults()
	{
		var edited = LotterySettingsUpdate.FromDefaults() with
		{
			WinTemplate = "the winning number was {number}! Congratulations to {winners}! They take the {prize} pot.",
			NoWinTemplate = "the winning number was {number}! The jackpot goes up to {pot}!!"
		};

		var reset = LotterySettingsValidator.ResetTemplates(edited);

		reset.WinTemplate.ShouldBe(LotterySettingsDefaults.WinTemplate);
		reset.NoWinTemplate.ShouldBe(LotterySettingsDefaults.NoWinTemplate);
		LotterySettingsValidator.Validate(reset).ShouldBeEmpty();
	}
}
