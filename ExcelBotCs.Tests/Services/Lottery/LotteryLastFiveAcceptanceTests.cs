using Discord;
using ExcelBotCs.Database;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.HealthChecks;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.Discord.Interfaces;
using ExcelBotCs.Services.Lottery;
using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.Services.Lottery.Records;
using ExcelBotCs.TestFramework.Database;
using ExcelBotCs.Tests.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;

namespace ExcelBotCs.Tests.Services.Lottery;

/// <summary>
///     The announcement's "Last 5 winners" block over the real cutover seed (spec §5.6, §6; criteria 9 and 13). The
///     other publisher tests hand the composer a list they wrote; this one inserts the 35 historical rows from
///     <c>docs/lottery/manual-seed.md</c> and lets the real repository pick the newest five, so the query, the order and
///     the row copy are proven together on the data the operator actually has.
/// </summary>
[Collection("MongoDB")]
public class LotteryLastFiveAcceptanceTests : MongoDbTest
{
	private const ulong MemberRoleId = 111_111_111_111_111_111;

	private readonly Mock<ILotteryAlertService> _alerts = new();
	private readonly Mock<IDiscordMessageService> _messages = new();
	private readonly Mock<ILotteryPrizePoolRepository> _prizes = new();

	private ILotteryWinnerRepository _winners = null!;

	public LotteryLastFiveAcceptanceTests(MongoDbFixture fixture) : base(fixture)
	{
	}

	protected override void InitializeRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
	{
		_winners = new LotteryWinnerRepository(mongoClient, databaseOptions);
	}

	[Fact]
	public async Task TheAnnouncementListsTheNewestFiveRowsOfTheManualSeedNewestFirst()
	{
		await LotteryHistoryFixture.SeedAsync(_winners);

		// A later, automated no-win round: its post carries the five newest historical winners, not the whole archive.
		var announcement = new LotteryAnnouncement(
			new LotteryRound
			{
				RoundKey = "2026-11-10",
				ScheduledAt = new DateTime(2026, 11, 10, 19, 0, 0, DateTimeKind.Utc),
				Status = LotteryRoundStatus.Drawn,
				WinningNumber = 7,
				PotBefore = 21_000_000,
				PotAfter = 22_000_000
			},
			[], 22_000_000, 22_000_000, false);

		string? posted = null;
		_messages.Setup(messages => messages.PostLotteryAnnouncementAsync(It.IsAny<string>()))
			.Callback<string>(text => posted = text)
			.ReturnsAsync(Mock.Of<IUserMessage>());

		await BuildPublisher().PublishAsync(announcement);

		posted.ShouldNotBeNull();

		// The block is exactly the five newest seed rows, newest first, in the accepted row shape of #101.
		posted.ShouldContain(
			"Last 5 winners:\n" +
			"Siegward | 56 - 80,000,000 Gil + Chaotic Mount (21.07.26)\n" +
			"Elara | 99 - 6,700,000 Gil (17.06.26)\n" +
			"Elara | 94 - 1,100,000 Gil (26.05.26)\n" +
			"Skor | 90 - 17,050,000 Gil (12.05.26)\n" +
			"Oriel | 24 - 24,100,000 Gil (05.05.26)\n");

		// The order matches the fixture's literal expectation, not an accident of insertion order.
		var positions = LotteryHistoryFixture.LastFive
			.Select(row => posted!.IndexOf($"{row.DisplayName} | {row.Guess} - ", StringComparison.Ordinal))
			.ToList();
		positions.ShouldAllBe(position => position >= 0);
		positions.ShouldBe(positions.OrderBy(position => position).ToList());

		// The sixth-newest row is not in the post, and there is no participants list or pinned-message claim.
		posted.ShouldNotContain("Oriana");
		posted.ShouldNotContain("Participants:");
		posted.Length.ShouldBeLessThan(LotteryAnnouncementComposer.MaxMessageLength);
	}

	private LotteryAnnouncementPublisher BuildPublisher()
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
			LotteryCustomEmojiId = 9,
			AdminRoleIds = [10]
		};

		_prizes.Setup(prizes => prizes.GetInPoolAsync()).ReturnsAsync([]);

		return new LotteryAnnouncementPublisher(_messages.Object,
			new StubLotterySettingsService(TestLotterySettings.Create(
				winTemplate: LotterySettingsDefaults.WinTemplate,
				noWinTemplate: LotterySettingsDefaults.NoWinTemplate)),
			_winners, _prizes.Object, _alerts.Object, Mock.Of<IBotConnectionMonitor>(
				monitor => monitor.IsHealthy(It.IsAny<TimeSpan>())), Options.Create(discord),
			Options.Create(new WebBaseUrlOptions { BaseUrl = "https://lottery.test" }),
			Mock.Of<ILogger<LotteryAnnouncementPublisher>>());
	}
}
