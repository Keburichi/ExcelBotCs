using ExcelBotCs.Services.Lottery;
using ExcelBotCs.Services.Lottery.Records;

namespace ExcelBotCs.Tests.Services.Lottery;

/// <summary>
///     The lottery settings validation matrix (spec §7 "Precedence and validation"): run/reminder day, time and
///     timezone are validated on save and when a stored document is read. Templates are validated by #111, so only
///     the schedule surface is pinned here.
/// </summary>
public class LotterySettingsValidationTests
{
	[Theory]
	[InlineData("Monday")]
	[InlineData("Tuesday")]
	[InlineData("Wednesday")]
	[InlineData("Thursday")]
	[InlineData("Friday")]
	[InlineData("Saturday")]
	[InlineData("Sunday")]
	public void ValidateDay_AcceptsEveryEnglishDayName(string day)
	{
		var result = LotterySettingsValidator.ValidateDay(day);

		result.IsValid.ShouldBeTrue();
		result.Normalized.ShouldBe(day);
	}

	[Theory]
	[InlineData("tuesday", "Tuesday")]
	[InlineData("WEDNESDAY", "Wednesday")]
	[InlineData("  Friday  ", "Friday")]
	public void ValidateDay_NormalizesCaseAndWhitespace(string stored, string expected)
	{
		var result = LotterySettingsValidator.ValidateDay(stored);

		result.IsValid.ShouldBeTrue();
		result.Normalized.ShouldBe(expected);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("Funday")]
	[InlineData("Tue")]
	[InlineData("1")]
	public void ValidateDay_RejectsAnythingElse(string? stored)
	{
		var result = LotterySettingsValidator.ValidateDay(stored);

		result.IsValid.ShouldBeFalse();
		result.Error.ShouldNotBeNullOrWhiteSpace();
	}

	[Theory]
	[InlineData("00:00")]
	[InlineData("09:05")]
	[InlineData("20:00")]
	[InlineData("23:59")]
	public void ValidateTime_AcceptsATwentyFourHourHHmmValue(string time)
	{
		var result = LotterySettingsValidator.ValidateTime(time);

		result.IsValid.ShouldBeTrue();
		result.Normalized.ShouldBe(time);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("8pm")]
	[InlineData("24:00")]
	[InlineData("20:60")]
	[InlineData("9:05")]
	[InlineData("20:00:00")]
	[InlineData("-1:00")]
	public void ValidateTime_RejectsAnythingElse(string? stored)
	{
		var result = LotterySettingsValidator.ValidateTime(stored);

		result.IsValid.ShouldBeFalse();
		result.Error.ShouldNotBeNullOrWhiteSpace();
	}

	[Theory]
	[InlineData("Europe/Berlin")]
	[InlineData("UTC")]
	public void ValidateTimezone_AcceptsAKnownTimeZoneId(string timezone)
	{
		var result = LotterySettingsValidator.ValidateTimezone(timezone);

		result.IsValid.ShouldBeTrue();
		result.Normalized.ShouldBe(timezone);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("Mars/Olympus")]
	[InlineData("Europe/Atlantis")]
	public void ValidateTimezone_RejectsAnUnknownTimeZoneId(string? stored)
	{
		var result = LotterySettingsValidator.ValidateTimezone(stored);

		result.IsValid.ShouldBeFalse();
		result.Error.ShouldNotBeNullOrWhiteSpace();
	}

	[Fact]
	public void Validate_AcceptsTheDocumentedDefaults()
	{
		var errors = LotterySettingsValidator.Validate(LotterySettingsUpdate.FromDefaults());

		errors.ShouldBeEmpty();
	}

	[Fact]
	public void Validate_ReportsEveryInvalidFieldByName()
	{
		var update = LotterySettingsUpdate.FromDefaults() with
		{
			RunDay = "Funday",
			RunTime = "25:00",
			RunTimezone = "Mars/Olympus",
			ReminderDay = "Mo",
			ReminderTime = "evening",
			ReminderTimezone = "Nowhere/Else"
		};

		var errors = LotterySettingsValidator.Validate(update);

		errors.Count.ShouldBe(6);
		errors.Select(e => e.Field).ShouldBe(
		[
			"RunDay", "RunTime", "RunTimezone", "ReminderDay", "ReminderTime", "ReminderTimezone"
		]);
	}

	[Fact]
	public void Validate_ReportsOnlyTheInvalidField()
	{
		var update = LotterySettingsUpdate.FromDefaults() with { ReminderTime = "18:00:00" };

		var errors = LotterySettingsValidator.Validate(update);

		errors.Count.ShouldBe(1);
		errors[0].Field.ShouldBe("ReminderTime");
		errors[0].Message.ShouldNotBeNullOrWhiteSpace();
	}
}
