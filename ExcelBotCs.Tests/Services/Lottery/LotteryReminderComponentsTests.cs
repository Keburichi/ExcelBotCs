using Discord;
using ExcelBotCs.Modules.Lottery;

namespace ExcelBotCs.Tests.Services.Lottery;

/// <summary>
///     The reminder post's two components and its mention restriction (spec §5.7, §11): a secondary opt-out button with
///     the agreed custom id, a link button to the web page, and only the audience is mentionable.
/// </summary>
public class LotteryReminderComponentsTests
{
	[Fact]
	public void OptOutCustomId_IsTheAgreedId()
	{
		LotteryReminderComponents.OptOutCustomId.ShouldBe("lottery_reminders:off");
	}

	private static List<ButtonComponent> Buttons(string webBaseUrl)
	{
		return LotteryReminderComponents.Build(webBaseUrl).Components
			.SelectMany(row => row.ShouldBeOfType<ActionRowComponent>().Components)
			.Select(component => component.ShouldBeOfType<ButtonComponent>())
			.ToList();
	}

	[Fact]
	public void Build_AlwaysCarriesTheOptOutButton()
	{
		var components = Buttons(string.Empty);

		components.Count.ShouldBe(1);
		var button = components[0];
		button.CustomId.ShouldBe("lottery_reminders:off");
		button.Label.ShouldBe("Turn off reminders");
		button.Style.ShouldBe(ButtonStyle.Secondary);
	}

	[Fact]
	public void Build_WithAWebBaseUrl_AddsTheLinkToTheLotteryPage()
	{
		var components = Buttons("https://excel.example/");

		components.Count.ShouldBe(2);
		var link = components[1];
		link.Style.ShouldBe(ButtonStyle.Link);
		link.Label.ShouldBe("Open lottery page");
		link.Url.ShouldBe("https://excel.example/lottery");
	}

	[Fact]
	public void PageUrl_IsNullWithoutAConfiguredBaseUrl()
	{
		LotteryReminderComponents.PageUrl("  ").ShouldBeNull();
	}

	[Fact]
	public void MentionsFor_AllowsOnlyTheAudienceUsers()
	{
		var mentions = LotteryReminderComponents.MentionsFor([11UL, 22UL]);

		mentions.AllowedTypes.ShouldBe(AllowedMentionTypes.Users);
		mentions.UserIds.ShouldBe([11UL, 22UL]);
		(mentions.RoleIds is null || mentions.RoleIds.Count == 0).ShouldBeTrue();
	}

	[Fact]
	public void MentionsFor_AnEmptyAudienceAllowsNothing()
	{
		var types = LotteryReminderComponents.MentionsFor([]).AllowedTypes ?? AllowedMentionTypes.None;

		types.ShouldBe(AllowedMentionTypes.None);
		(LotteryReminderComponents.MentionsFor([]).UserIds is null
		 || LotteryReminderComponents.MentionsFor([]).UserIds.Count == 0).ShouldBeTrue();
	}
}
