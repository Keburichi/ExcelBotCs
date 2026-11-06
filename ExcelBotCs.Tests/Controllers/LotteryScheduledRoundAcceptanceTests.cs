using System.Net.Http.Json;
using System.Text.Json;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.Discord.Interfaces;
using ExcelBotCs.Services.Lottery;
using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.Services.Lottery.Records;
using ExcelBotCs.TestFramework.Database;
using ExcelBotCs.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Moq;

namespace ExcelBotCs.Tests.Controllers;

/// <summary>
///     A whole scheduled round through the in-memory host (spec §5.1, §5.3, §5.6, §5.10; criteria 2, 3, 5, 9, 13, 15,
///     17 and 25). Nothing here calls the round engine directly: a member guesses through the member API, the fake clock
///     is placed on the configured Europe/Berlin run instant, and the host's own <see cref="LotterySchedulerService" />
///     tick has to draw, pay, post, complete and schedule the next round. Everything is then read back through the same
///     HTTP surfaces the admin page and the history page use.
/// </summary>
public class LotteryScheduledRoundAcceptanceTests : IntegrationTestBase
{
    private const string StatusUrl = "api/lottery/admin/status";
    private const string HistoryUrl = "api/Lottery/history";

    /// <summary>Tuesday 2026-11-10 20:00 Europe/Berlin — the default run instant, expressed in UTC.</summary>
    private static readonly DateTimeOffset RunInstant = TestInstants.Utc("2026-11-10T19:00:00");

    private readonly FakeTimeProvider _time = new(RunInstant);

    public LotteryScheduledRoundAcceptanceTests(MongoDbFixture fixture) : base(fixture)
    {
    }

    protected override void OnConfigureTestServices(IServiceCollection services)
    {
        // The fake Tuesday (criterion 25) and a draw that always hits 42, so the winner's guess is deterministic.
        services.RemoveAll<TimeProvider>();
        services.AddSingleton<TimeProvider>(_time);
        services.RemoveAll<ILotteryNumberGenerator>();
        services.AddSingleton<ILotteryNumberGenerator>(new FakeLotteryNumberGenerator { DefaultNumber = 42 });

        // A healthy deployment: the integration host removes the background startup check, so it is run explicitly.
        services.RemoveAll<ILotteryStartupCheck>();
        services.AddSingleton<ILotteryStartupCheck>(new HealthyStartupCheck());

        // Live automation for this test only: the host defaults to Enabled=false / DryRun=true (the shipped rollout).
        services.PostConfigure<LotteryOptions>(options =>
        {
            options.Automation.Enabled = true;
            options.Automation.DryRun = false;
        });
    }

    protected override async Task OnAfterIntegrationSetupAsync()
    {
        var startup = Factory.Services.GetRequiredService<LotteryStartupCheckService>();
        await startup.StartAsync(CancellationToken.None);
        await startup.Completion;
    }

    [Fact]
    public async Task TheSchedulerRunsTheConfiguredBerlinSlotOnceThroughTheHost()
    {
        var winner = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), isAdmin: true,
            name: "Scheduled Winner");
        (await Client.PostAsync("api/Lottery/guess/42", null)).EnsureSuccessStatusCode();

        // One scheduler tick, exactly as the hosted loop would run it on the run minute.
        await TickAsync();

        // Criterion 2: the slot is the configured Tuesday 20:00 Europe/Berlin date, keyed by its local date.
        var status = await GetStatusAsync();
        status.AutomationStatus.ShouldBe(LotteryAutomationStatus.LiveKind);
        status.LatestRound.ShouldNotBeNull();
        status.LatestRound!.RoundKey.ShouldBe("2026-11-10");
        status.LatestRound.Status.ShouldBe(LotteryRoundStatus.Completed);
        status.LatestRound.WinningNumber.ShouldBe(42);
        status.LatestRound.AnnouncementMessageId.ShouldNotBeNullOrWhiteSpace();
        status.PendingRound.ShouldBeNull();

        // Criterion 5: the whole pot went to the single winner and the pot reset to the base pot; both ledger
        // movements are stored under the round's idempotency keys.
        status.LatestRound.PotBefore.ShouldBe(1_000_000);
        status.LatestRound.PotAfter.ShouldBe(1_000_000);
        status.Pot.ShouldBe(1_000_000);

        var ledger = Factory.Services.GetRequiredService<ILotteryPotLedgerService>();
        (await ledger.FindAsync(LotteryPotLedgerKeys.Payout("2026-11-10"))).ShouldNotBeNull();
        (await ledger.FindAsync(LotteryPotLedgerKeys.Reset("2026-11-10"))).ShouldNotBeNull();

        var winners = await Factory.Services.GetRequiredService<ILotteryWinnerRepository>().GetAsync();
        var stored = winners.ShouldHaveSingleItem();
        stored.RoundKey.ShouldBe("2026-11-10");
        stored.DiscordId.ShouldBe(ulong.Parse(winner.DiscordId));
        stored.Guess.ShouldBe(42);
        stored.GilPrize.ShouldBe(1_000_000);
        stored.PayoutStatus.ShouldBe(LotteryPayoutStatus.Pending);

        // Criterion 9: one plain announcement on the lottery channel, no dry-run prefix, no participants list, and the
        // winner appears in the last-five block.
        var announcements = PostedAnnouncements();
        var announcement = announcements.ShouldHaveSingleItem();
        announcement.ShouldNotStartWith("[DRY RUN]");
        announcement.ShouldContain("Last 5 winners:");
        announcement.ShouldContain("Scheduled Winner | 42 - 1,000,000 Gil (10.11.26)");
        announcement.ShouldNotContain("Participants:");
        announcement.Length.ShouldBeLessThan(LotteryAnnouncementComposer.MaxMessageLength);

        // The completed round flushed the live guesses and scheduled the next slot.
        (await Factory.Services.GetRequiredService<ILotteryGuessRepository>().GetAsync()).ShouldBeEmpty();
        status.NextRoundKey.ShouldBe("2026-11-17");

        // Criterion 3/15: a second tick for the same slot draws nothing twice.
        await TickAsync();

        status = await GetStatusAsync();
        status.Pot.ShouldBe(1_000_000);
        (await Factory.Services.GetRequiredService<ILotteryRoundRepository>().GetAsync()).ShouldHaveSingleItem();
        PostedAnnouncements().Count.ShouldBe(1);

        // Criterion 13: the member history page sees the new draw, and criterion 17: a healthy round leaves the health
        // detail healthy instead of degrading on the automation flags.
        var history = await GetHistoryAsync();
        history.Totals.Winners.ShouldBe(1);
        history.Totals.Draws.ShouldBe(1);
        history.Totals.LastDrawRoundKey.ShouldBe("2026-11-10");
        history.Items.ShouldHaveSingleItem().DisplayName.ShouldBe("Scheduled Winner");

        var health = await GetLotteryHealthDetailAsync();
        health.GetProperty("status").GetString().ShouldBe("Healthy");
        health.GetProperty("data").GetProperty("state").GetString().ShouldBe(LotteryAutomationStatus.LiveKind);
        health.GetProperty("data").GetProperty("startupCheck").GetString().ShouldBe("healthy");
        health.GetProperty("data").GetProperty("degraded").GetBoolean().ShouldBeFalse();
        health.GetProperty("data").GetProperty("nextRoundKey").GetString().ShouldBe("2026-11-17");

        DiscordMessages.Verify(messages =>
            messages.PostLotteryAnnouncementAsync(It.IsAny<string>()), Times.Once);
    }

    #region Helpers

    private async Task TickAsync()
    {
        await Factory.Services.GetRequiredService<LotterySchedulerService>().TickOnceAsync();
    }

    /// <summary>Every announcement text that reached the fake Discord lottery channel, in call order.</summary>
    private List<string> PostedAnnouncements()
    {
        return DiscordMessages.Invocations
            .Where(call => call.Method.Name == nameof(IDiscordMessageService.PostLotteryAnnouncementAsync))
            .Select(call => (string)call.Arguments[0]!)
            .ToList();
    }

    private async Task<AdminStatus> GetStatusAsync()
    {
        var status = await Client.GetFromJsonAsync<AdminStatus>(StatusUrl);
        status.ShouldNotBeNull();
        return status;
    }

    private async Task<HistoryPage> GetHistoryAsync()
    {
        var page = await Client.GetFromJsonAsync<HistoryPage>(HistoryUrl);
        page.ShouldNotBeNull();
        return page;
    }

    private async Task<JsonElement> GetLotteryHealthDetailAsync()
    {
        using var report = JsonDocument.Parse(await Client.GetStringAsync("/health"));
        return report.RootElement.GetProperty("checks").GetProperty("lottery").Clone();
    }

    /// <summary>A deployment that passes the startup check, so the round is allowed to run.</summary>
    private sealed class HealthyStartupCheck : ILotteryStartupCheck
    {
        public Task<LotteryStartupResult> CheckAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(LotteryStartupResult.Healthy);
        }
    }

    // The admin-status and history payloads the pages read, mirrored so a contract change fails these assertions.
    private sealed record AdminStatus(string AutomationStatus, bool SchedulerEnabled, long Pot, AdminRound? LatestRound,
        AdminRound? PendingRound, string? NextRoundKey, string? LastError);

    private sealed record AdminRound(string RoundKey, string Status, int? WinningNumber, long? PotBefore, long? PotAfter,
        string? AnnouncementMessageId);

    private sealed record HistoryPage(List<HistoryItem> Items, string? NextCursor, HistoryTotals Totals);

    private sealed record HistoryItem(string Id, string RoundKey, string Date, string DisplayName, int Guess,
        long GilPrize);

    private sealed record HistoryTotals(int Winners, int Draws, long CurrentPot, string? LastDrawRoundKey);

    #endregion
}
