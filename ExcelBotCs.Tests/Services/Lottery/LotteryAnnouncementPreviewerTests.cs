using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.Lottery;
using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.Services.Lottery.Records;
using ExcelBotCs.Tests.Utils;
using ExcelBotCs.Utilities;
using Microsoft.Extensions.Options;
using Moq;

namespace ExcelBotCs.Tests.Services.Lottery;

/// <summary>
///     The admin preview (spec §5.9 Messages tab, for #114): both variants render the stored templates with the live
///     pot and prize pool, and the result reports the visible character count the editor shows next to the 2,000
///     character budget.
/// </summary>
public class LotteryAnnouncementPreviewerTests
{
	private const ulong MemberRoleId = 111_111_111_111_111_111;
	private const long LivePot = 20_000_000;

	private readonly Mock<ILotteryPotLedgerService> _pot = new();
	private readonly Mock<ILotteryPrizePoolRepository> _prizes = new();
	private readonly StubLotterySettingsService _settings = new(TestLotterySettings.Create(
		winTemplate: LotterySettingsDefaults.WinTemplate,
		noWinTemplate: LotterySettingsDefaults.NoWinTemplate));

	public LotteryAnnouncementPreviewerTests()
	{
		_pot.Setup(x => x.GetBalanceAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(LivePot);
		_prizes.Setup(x => x.GetInPoolAsync()).ReturnsAsync([]);
	}

	[Fact]
	public async Task RenderAsync_NoWin_UsesTheStoredTemplateAndTheLivePot()
	{
		var preview = await BuildPreviewer().RenderAsync(LotteryAnnouncementVariant.NoWin);

		preview.Variant.ShouldBe(LotteryAnnouncementVariant.NoWin);
		preview.Text.ShouldContain("the winning number was 1! So no one won this week again");
		preview.Text.ShouldContain("The jackpot goes up to 21,000,000 Gil!!");
		preview.Text.ShouldContain("Current pot: 21,000,000 Gil");
		preview.Text.ShouldContain("Full history: https://lottery.test/lottery/history");
		preview.Text.ShouldContain($"Hello <@&{MemberRoleId}>,");
	}

	[Fact]
	public async Task RenderAsync_Win_UsesTheStoredTemplateAndTheLivePot()
	{
		var preview = await BuildPreviewer().RenderAsync(LotteryAnnouncementVariant.Win);

		preview.Variant.ShouldBe(LotteryAnnouncementVariant.Win);
		preview.Text.ShouldContain("the winning number was 42! Congratulations to <@7001>! 🎉");
		preview.Text.ShouldContain("They take the 20,000,000 Gil pot.");
		preview.Text.ShouldContain("The jackpot resets to 1,000,000 Gil for next week.");
		preview.Text.ShouldContain("Last 5 winners:");
	}

	[Fact]
	public async Task RenderAsync_WithAPooledPrize_NamesItNextToTheWinnerAndCountsTheRest()
	{
		_prizes.Setup(x => x.GetInPoolAsync()).ReturnsAsync([
			new LotteryPrize { Name = "Chaotic Mount", Status = LotteryPrizePoolStatus.InPool },
			new LotteryPrize { Name = "Fuax Mount", Status = LotteryPrizePoolStatus.InPool }
		]);

		var preview = await BuildPreviewer().RenderAsync(LotteryAnnouncementVariant.Win);

		preview.Text.ShouldContain("They take the 20,000,000 Gil pot plus the Chaotic Mount.");
		preview.Text.ShouldContain("Aurora | 42 - 25,000,000 Gil + Chaotic Mount (10.11.26)");
		preview.Text.ShouldContain("Current pot: 1,000,000 Gil + Fuax Mount");
	}

	[Fact]
	public async Task RenderAsync_ReportsTheVisibleCharacterCount()
	{
		var preview = await BuildPreviewer().RenderAsync(LotteryAnnouncementVariant.NoWin);

		preview.VisibleLength.ShouldBe(DiscordVisibleText.Measure(preview.Text));
		preview.VisibleLength.ShouldBeLessThan(preview.RawLength);
		preview.VisibleLength.ShouldBeGreaterThan(800);
		preview.RawLength.ShouldBeLessThan(LotteryAnnouncementComposer.MaxMessageLength);
	}

	[Fact]
	public async Task RenderAsync_WithAnUnknownVariant_Throws()
	{
		await Should.ThrowAsync<ArgumentException>(() =>
			BuildPreviewer().RenderAsync("draw"));
	}

	private LotteryAnnouncementPreviewer BuildPreviewer()
	{
		var discord = new DiscordBotOptions
		{
			GuildId = 1,
			LotteryChannel = 2,
			AnnouncementChannel = 3,
			EventsChannel = 4,
			UpcomingRosterChannel = 5,
			HallOfClearsChannel = 6,
			LogChannel = 7,
			RiddlesChannel = 8,
			LotteryMemberRoleId = MemberRoleId,
			LotteryCustomEmojiId = 9
		};

		return new LotteryAnnouncementPreviewer(_settings, _pot.Object, _prizes.Object, Options.Create(discord),
			Options.Create(new WebBaseUrlOptions { BaseUrl = "https://lottery.test" }));
	}
}
