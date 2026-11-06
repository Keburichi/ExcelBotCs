using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.Discord.Interfaces;
using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.Services.Lottery.Records;
using ExcelBotCs.TestFramework.Database;
using ExcelBotCs.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace ExcelBotCs.Tests.Controllers;

/// <summary>
///     The admin API behind <c>/lottery/admin</c> (spec §5.9, §8, #114). The tests read the HTTP contract the Vue
///     page consumes, so the shapes here mirror the TypeScript types one-for-one: the status pill and counts, the
///     settings form and its per-field validation, the pot floor and ledger, payouts, the prize pool, opt-ins, the
///     preview and the run/recovery dispositions. Payout status never appears on the member timeline; only here.
/// </summary>
public class LotteryAdminIntegrationTests : IntegrationTestBase
{
    private const string StatusUrl = "api/lottery/admin/status";
    private const string SettingsUrl = "api/lottery/admin/settings";
    private const string PotUrl = "api/lottery/admin/pot";
    private const string LedgerUrl = "api/lottery/admin/ledger";
    private const string PayoutsUrl = "api/lottery/admin/payouts";
    private const string PrizesUrl = "api/lottery/admin/prizes";
    private const string OptInsUrl = "api/lottery/admin/optins";
    private const string ReminderTestUrl = "api/lottery/admin/reminders/test";
    private const string PreviewUrl = "api/lottery/admin/preview";
    private const string RunUrl = "api/lottery/admin/run";
    private const string SkipUrl = "api/lottery/admin/skip";
    private const string VoidUrl = "api/lottery/admin/void";
    private const string RetryUrl = "api/lottery/admin/retry";
    private const string DryRunUrl = "api/lottery/admin/dry-run";
    private const string RescheduleUrl = "api/lottery/admin/reschedule";

    public LotteryAdminIntegrationTests(MongoDbFixture fixture) : base(fixture)
    {
    }

    #region Contract mirrors

    // Reading the payload into these records fails the moment a field the page needs disappears or changes type.
    public sealed record AdminStatus(
        string AutomationStatus,
        string AutomationLabel,
        bool SchedulerEnabled,
        bool AutomationEnabled,
        bool DryRun,
        bool PauseAutomation,
        bool Degraded,
        bool Blocked,
        long Pot,
        int PendingPayouts,
        int PoolPrizes,
        int OptedIn,
        int EligibleReminders,
        bool RemindersEnabled,
        AdminRound? LatestRound,
        AdminRound? PendingRound,
        DateTime? NextRunAtUtc,
        string? NextRoundKey,
        string? LastError,
        AdminDeployment Deployment,
        AdminSettingsChange? LastSettingsChange,
        List<string> InvalidSettingsFields);

    public sealed record AdminRound(
        string RoundKey,
        string Status,
        DateTime ScheduledAt,
        DateTime? RunAt,
        int? WinningNumber,
        long? PotBefore,
        long? PotAfter,
        string? FailedStep,
        int Attempts,
        DateTime? NextRetryAt,
        string? LastError,
        string? AnnouncementMessageId);

    public sealed record AdminDeployment(
        string LotteryChannelId,
        string LotteryMemberRoleId,
        string LotteryCustomEmojiId,
        string WebBaseUrl);

    public sealed record AdminSettingsChange(
        int Version,
        DateTime ChangedAtUtc,
        string? ActorDiscordId,
        List<string> Fields);

    public sealed record Settings(
        string RunDay,
        string RunTime,
        string RunTimezone,
        string ReminderDay,
        string ReminderTime,
        string ReminderTimezone,
        bool RemindersEnabled,
        bool PauseAutomation,
        string WinTemplate,
        string NoWinTemplate,
        int Version,
        string? UpdatedByDiscordId,
        bool AutomationEnabled,
        bool DryRun,
        string AutomationStatus,
        string AutomationLabel,
        long BasePot,
        long WeeklyIncrement,
        List<string> InvalidFields,
        SettingsDefaults Defaults);

    public sealed record SettingsDefaults(
        string RunDay,
        string RunTime,
        string RunTimezone,
        string ReminderDay,
        string ReminderTime,
        string ReminderTimezone,
        string WinTemplate,
        string NoWinTemplate);

    public sealed record SaveResponse(bool Success, bool Changed, Settings? Settings, List<FieldError> Errors);

    public sealed record FieldError(string Field, string Message);

    public sealed record PotResponse(
        bool Success,
        bool Replayed,
        long Balance,
        long RequestedAmount,
        long AppliedAmount,
        LedgerEntry? Entry,
        List<FieldError> Errors);

    public sealed record LedgerEntry(
        string Id,
        string? RoundKey,
        string Type,
        long Amount,
        string Reason,
        string? ActorDiscordId,
        long BalanceAfter,
        string IdempotencyKey,
        DateTime CreatedAtUtc);

    public sealed record LedgerPage(List<LedgerEntry> Items, string? NextCursor);

    public sealed record Payout(
        string Id,
        string RoundKey,
        string? DiscordId,
        string DisplayName,
        int Guess,
        long GilPrize,
        List<string> Items,
        string PayoutStatus,
        string? PayoutActorDiscordId,
        DateTime? PayoutAt,
        string? PayoutNote);

    public sealed record PayoutPage(List<Payout> Items, string? NextCursor, int PendingCount);

    public sealed record PayoutUpdateResponse(
        bool Success,
        string Code,
        string Message,
        Payout? Payout,
        List<string> ReturnedPrizes);

    public sealed record PrizeItem(
        string Id,
        string Name,
        string Status,
        string? AwardedToDiscordId,
        string? AwardedToName,
        string? AwardedRoundKey,
        string? AddedByDiscordId,
        DateTime AddedAt);

    public sealed record PrizePool(List<PrizeItem> Items, int InPoolCount, int AwardedCount);

    public sealed record PrizeResponse(
        bool Success,
        string Code,
        string Message,
        PrizeItem? Prize,
        Payout? Winner);

    public sealed record OptIn(
        string DiscordId,
        string? DisplayName,
        bool IsCurrentMember,
        bool IsEligible,
        DateTime OptedInAtUtc);

    public sealed record OptIns(List<OptIn> Items, int OptedIn, int Eligible);

    public sealed record ReminderTest(
        bool Posted,
        string Disposition,
        string Message,
        string? RoundKey,
        int Recipients);

    public sealed record Preview(string Variant, string Text, int VisibleLength, int RawLength);

    public sealed record ManualAction(
        string Disposition,
        bool Success,
        string Message,
        string? RoundKey,
        long? Pot);

    public sealed record RescheduleBody(string? RoundKey, DateTimeOffset? ScheduledAtUtc);

    #endregion

    #region Permission matrix

    [Fact]
    public async Task Status_CheckPermissions()
    {
        SetUnauthenticated();
        (await Client.GetAsync(StatusUrl)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        SetAuthenticatedUser("12355");
        (await Client.GetAsync(StatusUrl)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        await AuthenticateAsMember();
        (await Client.GetAsync(StatusUrl)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        await AuthenticateAsAdmin();
        (await Client.GetAsync(StatusUrl)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    #endregion

    #region Status

    [Fact]
    public async Task Status_ReportsTheAutomationPillAndTheDeploymentConfigReadOnly()
    {
        await AuthenticateAsAdmin();

        var status = await GetStatusAsync();

        // appsettings.Testing.json leaves Lottery:Automation:Enabled off, so the deployment kill switch wins.
        status.AutomationStatus.ShouldBe(LotteryAutomationStatus.DisabledKind);
        status.AutomationLabel.ShouldBe("Disabled");
        status.AutomationEnabled.ShouldBeFalse();
        status.SchedulerEnabled.ShouldBeFalse();
        status.DryRun.ShouldBeTrue();
        status.PauseAutomation.ShouldBeFalse();
        status.Degraded.ShouldBeFalse();
        status.Blocked.ShouldBeFalse();
        status.LastError.ShouldBeNull();

        // The ids are deployment config: read-only here, and stringified so a snowflake survives JavaScript numbers.
        status.Deployment.LotteryChannelId.ShouldBe("111111111111111111");
        status.Deployment.LotteryMemberRoleId.ShouldBe("0");
        status.Deployment.LotteryCustomEmojiId.ShouldBe("0");
        status.Deployment.WebBaseUrl.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Status_ReportsThePotTheCountsAndTheNewestRound()
    {
        await AuthenticateAsAdmin();

        await SeedPotAsync(7_500_000);
        await SeedPrizeAsync("Chaotic Mount");
        await SeedPrizeAsync("Fuax Mount");
        await SeedPrizeAsync("Awarded Mount", LotteryPrizePoolStatus.Awarded);
        await SeedWinnerAsync("2026-07-28", "Elara", 94, 7_500_000, LotteryPayoutStatus.Pending);
        await SeedWinnerAsync("2026-07-21", "Siegward", 56, 1_000_000, LotteryPayoutStatus.Paid);
        await SeedOptInAsync(1111);
        await SeedOptInAsync(2222);
        await Factory.Services.GetRequiredService<ILotteryRoundRepository>().ImportCompletedRoundAsync(
            "2026-07-28", new DateTime(2026, 7, 28, 18, 0, 0, DateTimeKind.Utc), 94);

        var status = await GetStatusAsync();

        status.Pot.ShouldBe(7_500_000);
        status.PoolPrizes.ShouldBe(2);
        status.PendingPayouts.ShouldBe(1);
        status.OptedIn.ShouldBe(2);

        // No member rows exist for the opted-in ids, so nobody is eligible for a reminder yet.
        status.EligibleReminders.ShouldBe(0);
        status.RemindersEnabled.ShouldBeFalse();

        status.LatestRound.ShouldNotBeNull();
        status.LatestRound!.RoundKey.ShouldBe("2026-07-28");
        status.LatestRound.Status.ShouldBe(LotteryRoundStatus.Completed);
        status.LatestRound.WinningNumber.ShouldBe(94);
    }

    [Fact]
    public async Task Status_ReportsAPendingRoundTheLastErrorAndTheLastSettingsChange()
    {
        await AuthenticateAsAdmin();

        await Factory.Services.GetRequiredService<ILotteryRoundRepository>()
            .EnsureScheduledAsync("2026-07-28", new DateTime(2026, 7, 28, 18, 0, 0, DateTimeKind.Utc));
        await Factory.Services.GetRequiredService<ILotteryRoundRepository>().RegisterFailureAsync(
            "2026-07-28", LotteryFailedStep.Announcement, LotteryRoundStatus.Drawn, 3,
            "Discord is unreachable", new DateTime(2026, 7, 28, 19, 0, 0, DateTimeKind.Utc), null);

        var settings = await Factory.Services.GetRequiredService<ILotterySettingsService>()
            .GetEffectiveAsync();
        var saved = await Factory.Services.GetRequiredService<ILotterySettingsService>()
            .SaveAsync(settings.ToUpdate() with { RemindersEnabled = true }, 9001);

        var status = await GetStatusAsync();

        status.Degraded.ShouldBeTrue();
        status.Blocked.ShouldBeTrue();
        status.PendingRound.ShouldNotBeNull();
        status.PendingRound!.RoundKey.ShouldBe("2026-07-28");
        status.PendingRound.FailedStep.ShouldBe(LotteryFailedStep.Announcement);
        status.LastError.ShouldBe("Discord is unreachable");

        saved.Success.ShouldBeTrue();
        status.LastSettingsChange.ShouldNotBeNull();
        status.LastSettingsChange!.Version.ShouldBe(saved.Settings!.Version);
        status.LastSettingsChange.ActorDiscordId.ShouldBe("9001");
        status.LastSettingsChange.Fields.ShouldContain(nameof(LotterySettings.RemindersEnabled));
    }

    #endregion

    #region Settings

    [Fact]
    public async Task Settings_CheckPermissions()
    {
        SetUnauthenticated();
        (await Client.GetAsync(SettingsUrl)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await Client.PutAsJsonAsync(SettingsUrl, DefaultsRequest())).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        SetAuthenticatedUser("12355");
        (await Client.GetAsync(SettingsUrl)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await Client.PutAsJsonAsync(SettingsUrl, DefaultsRequest())).StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        await AuthenticateAsMember();
        (await Client.GetAsync(SettingsUrl)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await Client.PutAsJsonAsync(SettingsUrl, DefaultsRequest())).StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        await AuthenticateAsAdmin();
        (await Client.GetAsync(SettingsUrl)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await Client.PutAsJsonAsync(SettingsUrl, DefaultsRequest())).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Settings_ReturnsTheEditableSetTheDefaultsAndTheDeploymentFlags()
    {
        await AuthenticateAsAdmin();

        var settings = await GetSettingsAsync();

        settings.RunDay.ShouldBe("Tuesday");
        settings.RunTime.ShouldBe("20:00");
        settings.RunTimezone.ShouldBe("Europe/Berlin");
        settings.ReminderDay.ShouldBe("Monday");
        settings.ReminderTime.ShouldBe("18:00");
        settings.RemindersEnabled.ShouldBeFalse();
        settings.PauseAutomation.ShouldBeFalse();
        settings.Version.ShouldBe(0);
        settings.AutomationStatus.ShouldBe(LotteryAutomationStatus.DisabledKind);
        settings.BasePot.ShouldBe(1_000_000);
        settings.WeeklyIncrement.ShouldBe(1_000_000);
        settings.InvalidFields.ShouldBeEmpty();

        // The Messages tab resets the editors from these without a separate defaults endpoint.
        settings.Defaults.WinTemplate.ShouldBe(LotterySettingsDefaults.WinTemplate);
        settings.Defaults.NoWinTemplate.ShouldBe(LotterySettingsDefaults.NoWinTemplate);
        settings.Defaults.RunDay.ShouldBe("Tuesday");

        // The template text must carry the placeholders the editor offers as chips.
        settings.WinTemplate.ShouldContain("{number}");
        settings.WinTemplate.ShouldContain("{nextPot}");
        settings.NoWinTemplate.ShouldContain("{emoji}");
    }

    [Fact]
    public async Task Settings_RejectsAnInvalidFieldWithoutWritingAnything()
    {
        await AuthenticateAsAdmin();

        var response = await Client.PutAsJsonAsync(SettingsUrl, DefaultsRequest() with
        {
            RunTime = "8pm",
            ReminderDay = "Funday"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await ReadAsync<SaveResponse>(response);
        body.Success.ShouldBeFalse();
        body.Changed.ShouldBeFalse();
        body.Settings.ShouldBeNull();
        body.Errors.Select(e => e.Field).ShouldBe(new[] { "RunTime", "ReminderDay" }, ignoreOrder: true);
        body.Errors.ShouldAllBe(e => !string.IsNullOrWhiteSpace(e.Message));

        // A rejected save writes nothing: the stored settings and the audit trail are untouched.
        var settings = await GetSettingsAsync();
        settings.RunTime.ShouldBe("20:00");
        settings.Version.ShouldBe(0);
        (await Factory.Services.GetRequiredService<ILotterySettingsHistoryRepository>()
                .GetBySettingsKeyAsync(LotterySettings.SingletonKey)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Settings_RejectsAnUnknownTemplatePlaceholder()
    {
        await AuthenticateAsAdmin();

        var response = await Client.PutAsJsonAsync(SettingsUrl, DefaultsRequest() with
        {
            WinTemplate = "the winning number was {number}! Congratulations to {winners}! {mystery}"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await ReadAsync<SaveResponse>(response);
        body.Errors.ShouldContain(e => e.Field == "WinTemplate");
        body.Errors.Single(e => e.Field == "WinTemplate").Message.ShouldContain("{mystery}");
    }

    [Fact]
    public async Task Settings_SavesTheEditableSetAndWritesOneAuditRow()
    {
        var admin = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);

        var response = await Client.PutAsJsonAsync(SettingsUrl, DefaultsRequest() with
        {
            RunDay = "Wednesday",
            RunTime = "19:30",
            RemindersEnabled = true,
            PauseAutomation = true
        });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await ReadAsync<SaveResponse>(response);
        body.Success.ShouldBeTrue();
        body.Changed.ShouldBeTrue();
        body.Errors.ShouldBeEmpty();
        body.Settings!.RunDay.ShouldBe("Wednesday");
        body.Settings.RunTime.ShouldBe("19:30");
        body.Settings.RemindersEnabled.ShouldBeTrue();
        body.Settings.PauseAutomation.ShouldBeTrue();
        body.Settings.Version.ShouldBe(1);
        body.Settings.UpdatedByDiscordId.ShouldBe(admin.DiscordId);

        var stored = await GetSettingsAsync();
        stored.RunDay.ShouldBe("Wednesday");
        stored.Version.ShouldBe(1);

        var history = await Factory.Services.GetRequiredService<ILotterySettingsHistoryRepository>()
            .GetBySettingsKeyAsync(LotterySettings.SingletonKey);
        var row = history.ShouldHaveSingleItem();
        row.Version.ShouldBe(1);
        row.ActorDiscordId.ShouldBe(ulong.Parse(admin.DiscordId));
        row.OldValues["RunDay"].ShouldBe("Tuesday");
        row.NewValues["RunDay"].ShouldBe("Wednesday");
        row.NewValues["PauseAutomation"].ShouldBe(true);
        row.OldValues.ContainsKey("WinTemplate").ShouldBeFalse();

        // The Overview's "last settings change" reads the same row.
        var status = await GetStatusAsync();
        status.LastSettingsChange!.Version.ShouldBe(1);
        status.LastSettingsChange.Fields.ShouldContain("RunDay");
    }

    [Fact]
    public async Task Settings_ARepeatOfTheStoredValuesIsNotAnAuditedChange()
    {
        await AuthenticateAsAdmin();

        (await Client.PutAsJsonAsync(SettingsUrl, DefaultsRequest() with { RunTime = "19:30" }))
            .StatusCode.ShouldBe(HttpStatusCode.OK);

        var response = await Client.PutAsJsonAsync(SettingsUrl, DefaultsRequest() with { RunTime = "19:30" });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await ReadAsync<SaveResponse>(response);
        body.Success.ShouldBeTrue();
        body.Changed.ShouldBeFalse();
        body.Settings!.Version.ShouldBe(1);

        var history = await Factory.Services.GetRequiredService<ILotterySettingsHistoryRepository>()
            .GetBySettingsKeyAsync(LotterySettings.SingletonKey);
        history.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Settings_ResetTemplatesRestoresTheDefaultCopy()
    {
        await AuthenticateAsAdmin();

        await Client.PutAsJsonAsync(SettingsUrl, DefaultsRequest() with
        {
            WinTemplate = "the winning number was {number}. {winners} take it.",
            NoWinTemplate = "nobody won. {pot} grows."
        });

        var response = await Client.PutAsJsonAsync(SettingsUrl, DefaultsRequest() with
        {
            WinTemplate = "ignored because ResetTemplates wins",
            NoWinTemplate = "also ignored",
            ResetTemplates = true
        });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await ReadAsync<SaveResponse>(response);
        body.Settings!.WinTemplate.ShouldBe(LotterySettingsDefaults.WinTemplate);
        body.Settings.NoWinTemplate.ShouldBe(LotterySettingsDefaults.NoWinTemplate);
    }

    #endregion

    #region Pot and ledger

    [Fact]
    public async Task PotAndLedger_CheckPermissions()
    {
        SetUnauthenticated();
        (await Client.GetAsync(LedgerUrl)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await Client.PostAsJsonAsync(PotUrl, PotRequest(1_000_000, "increase"))).StatusCode
            .ShouldBe(HttpStatusCode.Unauthorized);

        SetAuthenticatedUser("12355");
        (await Client.GetAsync(LedgerUrl)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await Client.PostAsJsonAsync(PotUrl, PotRequest(1_000_000, "increase"))).StatusCode
            .ShouldBe(HttpStatusCode.Forbidden);

        await AuthenticateAsMember();
        (await Client.GetAsync(LedgerUrl)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await Client.PostAsJsonAsync(PotUrl, PotRequest(1_000_000, "increase"))).StatusCode
            .ShouldBe(HttpStatusCode.Forbidden);

        await AuthenticateAsAdmin();
        (await Client.GetAsync(LedgerUrl)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await Client.PostAsJsonAsync(PotUrl, PotRequest(1_000_000, "increase"))).StatusCode
            .ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Pot_AppliesAnIncreaseAsAnAuditedCorrection()
    {
        var admin = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);
        await SeedPotAsync(5_000_000);

        var response = await Client.PostAsJsonAsync(PotUrl,
            new PotAdjustmentRequest(2_000_000, "Guild donation", "increase", null));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await ReadAsync<PotResponse>(response);
        body.Success.ShouldBeTrue();
        body.Replayed.ShouldBeFalse();
        body.Balance.ShouldBe(7_000_000);
        body.AppliedAmount.ShouldBe(2_000_000);
        body.Errors.ShouldBeEmpty();

        var entry = body.Entry.ShouldNotBeNull();
        entry.Type.ShouldBe(LotteryPotLedgerType.Correction);
        entry.Amount.ShouldBe(2_000_000);
        entry.BalanceAfter.ShouldBe(7_000_000);
        entry.Reason.ShouldBe("Guild donation");
        entry.ActorDiscordId.ShouldBe(admin.DiscordId);
        entry.RoundKey.ShouldBeNull();
        entry.IdempotencyKey.ShouldNotBeNullOrWhiteSpace();

        (await GetStatusAsync()).Pot.ShouldBe(7_000_000);
        var page = await GetLedgerAsync();
        page.Items[0].Id.ShouldBe(entry.Id);
    }

    [Fact]
    public async Task Pot_NeverTakesTheBalanceBelowTheOneMillionFloor()
    {
        await AuthenticateAsAdmin();
        await SeedPotAsync(1_500_000);

        var response = await Client.PostAsJsonAsync(PotUrl,
            new PotAdjustmentRequest(2_000_000, "Correcting an over-credit", "decrease", null));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await ReadAsync<PotResponse>(response);
        body.Success.ShouldBeTrue();
        body.RequestedAmount.ShouldBe(2_000_000);

        // The full 2M would land at -500k, so the correction is clamped to what the 1M floor allows.
        body.AppliedAmount.ShouldBe(-500_000);
        body.Balance.ShouldBe(1_000_000);
        body.Entry!.Amount.ShouldBe(-500_000);
        body.Entry.BalanceAfter.ShouldBe(1_000_000);
    }

    [Fact]
    public async Task Pot_AtTheFloorRecordsANoOpCorrectionInsteadOfFailing()
    {
        await AuthenticateAsAdmin();
        await SeedPotAsync(1_000_000);

        var body = await ReadAsync<PotResponse>(await Client.PostAsJsonAsync(PotUrl,
            new PotAdjustmentRequest(500_000, "Nothing to take", "decrease", null)));

        body.Success.ShouldBeTrue();
        body.AppliedAmount.ShouldBe(0);
        body.Balance.ShouldBe(1_000_000);

        // The ledger still records the attempt, because a correction is audited even when it moves nothing.
        body.Entry.ShouldNotBeNull();
        body.Entry!.Amount.ShouldBe(0);
        body.Entry.BalanceAfter.ShouldBe(1_000_000);
    }

    [Fact]
    public async Task Pot_RejectsAnInvalidAdjustmentWithPerFieldErrors()
    {
        await AuthenticateAsAdmin();

        var zero = await Client.PostAsJsonAsync(PotUrl, new PotAdjustmentRequest(0, "", "sideways", null));
        zero.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await ReadAsync<PotResponse>(zero);
        body.Success.ShouldBeFalse();
        body.Entry.ShouldBeNull();
        body.Errors.Select(e => e.Field).ShouldBe(new[] { "Amount", "Reason", "Direction" }, ignoreOrder: true);

        (await GetLedgerAsync()).Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Pot_ReplaysTheStoredEntryForTheSameIdempotencyKey()
    {
        await AuthenticateAsAdmin();
        await SeedPotAsync(3_000_000);

        var first = await ReadAsync<PotResponse>(await Client.PostAsJsonAsync(PotUrl,
            new PotAdjustmentRequest(1_000_000, "Riddle prize", "increase", "admin-form-abc")));
        var second = await ReadAsync<PotResponse>(await Client.PostAsJsonAsync(PotUrl,
            new PotAdjustmentRequest(1_000_000, "Riddle prize", "increase", "admin-form-abc")));

        first.Replayed.ShouldBeFalse();
        second.Replayed.ShouldBeTrue();
        second.Entry!.Id.ShouldBe(first.Entry!.Id);
        second.Balance.ShouldBe(4_000_000);

        // One form submit, one pot movement, one ledger row.
        (await GetLedgerAsync()).Items.Count(entry => entry.Reason == "Riddle prize").ShouldBe(1);
        (await GetStatusAsync()).Pot.ShouldBe(4_000_000);
    }

    [Fact]
    public async Task Ledger_IsNewestFirstAndPagesWithACursor()
    {
        await AuthenticateAsAdmin();
        for (var day = 1; day <= 5; day++)
            await Factory.Services.GetRequiredService<ILotteryPotLedgerService>().AppendAsync(null,
                LotteryPotLedgerType.Donation, 100_000 * day, 1_000_000 + 100_000 * day, $"donation {day}", null,
                $"ledger-page-{day}");

        var first = await GetLedgerAsync(limit: 2);
        first.Items.Count.ShouldBe(2);
        first.Items[0].Reason.ShouldBe("donation 5");
        first.Items[1].Reason.ShouldBe("donation 4");
        first.NextCursor.ShouldNotBeNull();

        var seen = new List<string> { first.Items[0].Id, first.Items[1].Id };
        var next = await GetLedgerAsync(first.NextCursor, 2);
        seen.AddRange(next.Items.Select(entry => entry.Id));
        next.Items.Select(entry => entry.Reason).ShouldBe(new[] { "donation 3", "donation 2" });

        var last = await GetLedgerAsync(next.NextCursor, 2);
        seen.AddRange(last.Items.Select(entry => entry.Id));
        last.Items.Select(entry => entry.Reason).ShouldBe(new[] { "donation 1" });
        last.NextCursor.ShouldBeNull();

        // Every row exactly once: the keyset never repeats or skips an entry.
        seen.Distinct().Count().ShouldBe(5);
        seen.Count.ShouldBe(5);
    }

    [Fact]
    public async Task Ledger_RejectsAnInvalidCursorWithATypedBadRequest()
    {
        await AuthenticateAsAdmin();

        var response = await Client.GetAsync($"{LedgerUrl}?cursor=not-a-cursor");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).ShouldContain("invalid_cursor");
        (await Client.GetAsync($"{LedgerUrl}?cursor=")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    #endregion

    #region Payouts

    [Fact]
    public async Task Payouts_CheckPermissions()
    {
        var winnerPath = "api/lottery/admin/winners/2026-07-28/abc/payout";

        SetUnauthenticated();
        (await Client.GetAsync(PayoutsUrl)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await Client.PatchAsJsonAsync(winnerPath, new PayoutUpdateRequest(LotteryPayoutStatus.Paid, null)))
            .StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        SetAuthenticatedUser("12355");
        (await Client.GetAsync(PayoutsUrl)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await Client.PatchAsJsonAsync(winnerPath, new PayoutUpdateRequest(LotteryPayoutStatus.Paid, null)))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        await AuthenticateAsMember();
        (await Client.GetAsync(PayoutsUrl)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await Client.PatchAsJsonAsync(winnerPath, new PayoutUpdateRequest(LotteryPayoutStatus.Paid, null)))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        await AuthenticateAsAdmin();
        (await Client.GetAsync(PayoutsUrl)).StatusCode.ShouldBe(HttpStatusCode.OK);

        // The winner path is admin-reachable; the missing row is a typed 404 rather than an auth failure.
        (await Client.PatchAsJsonAsync(winnerPath, new PayoutUpdateRequest(LotteryPayoutStatus.Paid, null)))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Payouts_ListWinnersNewestFirstWithThePayoutFields()
    {
        await AuthenticateAsAdmin();
        await SeedWinnerAsync("2026-07-21", "Siegward", 56, 1_000_000, LotteryPayoutStatus.Paid);
        await SeedWinnerAsync("2026-07-28", "Elara", 94, 7_500_000, LotteryPayoutStatus.Pending,
            ["Chaotic Mount"], discordId: 123456789012345678);

        var page = await GetPayoutsAsync();

        page.Items.Count.ShouldBe(2);
        page.Items[0].DisplayName.ShouldBe("Elara");
        page.Items[0].RoundKey.ShouldBe("2026-07-28");
        page.Items[0].Guess.ShouldBe(94);
        page.Items[0].GilPrize.ShouldBe(7_500_000);
        page.Items[0].Items.ShouldBe(new[] { "Chaotic Mount" });
        page.Items[0].PayoutStatus.ShouldBe(LotteryPayoutStatus.Pending);
        page.Items[0].DiscordId.ShouldBe("123456789012345678");
        page.PendingCount.ShouldBe(1);

        page.Items[1].PayoutStatus.ShouldBe(LotteryPayoutStatus.Paid);
    }

    [Fact]
    public async Task Payouts_FilterByStatusAndRejectAnUnknownOne()
    {
        await AuthenticateAsAdmin();
        await SeedWinnerAsync("2026-07-21", "Siegward", 56, 1_000_000, LotteryPayoutStatus.Paid);
        await SeedWinnerAsync("2026-07-28", "Elara", 94, 1_000_000, LotteryPayoutStatus.Pending);

        var pending = await GetPayoutsAsync(status: LotteryPayoutStatus.Pending);
        pending.Items.ShouldHaveSingleItem().DisplayName.ShouldBe("Elara");

        var paid = await GetPayoutsAsync(status: LotteryPayoutStatus.Paid);
        paid.Items.ShouldHaveSingleItem().DisplayName.ShouldBe("Siegward");

        var response = await Client.GetAsync($"{PayoutsUrl}?status=banana");
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).ShouldContain("invalid_status");
    }

    [Fact]
    public async Task Payouts_PageAStableKeysetIncludingASharedDraw()
    {
        await AuthenticateAsAdmin();

        var drawnAt = new DateTime(2026, 1, 1, 18, 0, 0, DateTimeKind.Utc);
        await SeedWinnerAsync("2026-01-01", "Karya", 66, 2_500_000, LotteryPayoutStatus.Pending, createdUtc: drawnAt);
        await SeedWinnerAsync("2026-01-01", "Uma", 66, 2_500_000, LotteryPayoutStatus.Pending, createdUtc: drawnAt);
        await SeedWinnerAsync("2025-12-25", "Siegward", 30, 1_000_000, LotteryPayoutStatus.Pending);

        var seen = new List<string>();
        string? cursor = null;
        for (var pageIndex = 0; pageIndex < 5; pageIndex++)
        {
            var page = await GetPayoutsAsync(cursor: cursor, limit: 1);
            seen.AddRange(page.Items.Select(item => item.Id));
            cursor = page.NextCursor;
            if (cursor is null) break;
        }

        seen.Count.ShouldBe(3);
        seen.Distinct().Count().ShouldBe(3);
    }

    [Fact]
    public async Task Payout_MarksAWinnerPaidWithTheActorTheTimeAndTheNote()
    {
        var admin = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);
        var winner = await SeedWinnerAsync("2026-07-28", "Elara", 94, 7_500_000, LotteryPayoutStatus.Pending);

        var response = await Client.PatchAsJsonAsync(PayoutUrl(winner),
            new PayoutUpdateRequest(LotteryPayoutStatus.Paid, "Handed over in Limsa"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await ReadAsync<PayoutUpdateResponse>(response);
        body.Success.ShouldBeTrue();
        body.Code.ShouldBe("updated");
        body.Payout!.PayoutStatus.ShouldBe(LotteryPayoutStatus.Paid);
        body.Payout.PayoutActorDiscordId.ShouldBe(admin.DiscordId);
        body.Payout.PayoutAt.ShouldNotBeNull();
        body.Payout.PayoutNote.ShouldBe("Handed over in Limsa");
        body.ReturnedPrizes.ShouldBeEmpty();

        var stored = await Factory.Services.GetRequiredService<ILotteryWinnerRepository>()
            .GetAsync(winner.Id);
        stored!.PayoutStatus.ShouldBe(LotteryPayoutStatus.Paid);
        stored.PayoutActorDiscordId.ShouldBe(ulong.Parse(admin.DiscordId));

        (await GetPayoutsAsync(status: LotteryPayoutStatus.Pending)).Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Payout_ForfeitReturnsTheWinnersPrizesToThePool()
    {
        await AuthenticateAsAdmin();
        var winner = await SeedWinnerAsync("2026-07-28", "Elara", 94, 7_500_000, LotteryPayoutStatus.Pending,
            ["Chaotic Mount"], discordId: 123456789012345678);
        var prize = await SeedPrizeAsync("Chaotic Mount", LotteryPrizePoolStatus.Awarded,
            roundKey: "2026-07-28", awardedTo: 123456789012345678, awardedToName: "Elara");

        var response = await Client.PatchAsJsonAsync(PayoutUrl(winner),
            new PayoutUpdateRequest(LotteryPayoutStatus.Forfeited, "Left the FC before claiming"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await ReadAsync<PayoutUpdateResponse>(response);
        body.Payout!.PayoutStatus.ShouldBe(LotteryPayoutStatus.Forfeited);
        body.ReturnedPrizes.ShouldBe(new[] { "Chaotic Mount" });

        // The item is indivisible: it goes back to the pool, unassigned, and leaves the winner's history row.
        var returned = await Factory.Services.GetRequiredService<ILotteryPrizePoolRepository>()
            .GetAsync(prize.Id);
        returned!.Status.ShouldBe(LotteryPrizePoolStatus.InPool);
        returned.AwardedToDiscordId.ShouldBeNull();
        returned.AwardedToName.ShouldBeNull();
        returned.AwardedRoundKey.ShouldBeNull();

        var storedWinner = await Factory.Services.GetRequiredService<ILotteryWinnerRepository>()
            .GetAsync(winner.Id);
        storedWinner!.BonusItems.ShouldBeEmpty();
        storedWinner.PayoutNote.ShouldBe("Left the FC before claiming");
    }

    [Fact]
    public async Task Payout_RejectsAnInvalidTransitionAnUnknownStatusAndAnUnknownWinner()
    {
        await AuthenticateAsAdmin();
        var winner = await SeedWinnerAsync("2026-07-28", "Elara", 94, 7_500_000, LotteryPayoutStatus.Forfeited);

        var backward = await Client.PatchAsJsonAsync(PayoutUrl(winner),
            new PayoutUpdateRequest(LotteryPayoutStatus.Pending, null));
        backward.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var backwardBody = await ReadAsync<PayoutUpdateResponse>(backward);
        backwardBody.Success.ShouldBeFalse();
        backwardBody.Code.ShouldBe("invalid-transition");
        backwardBody.Message.ShouldContain(LotteryPayoutStatus.Forfeited);

        var unknownStatus = await Client.PatchAsJsonAsync(PayoutUrl(winner),
            new PayoutUpdateRequest("refunded", null));
        unknownStatus.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ReadAsync<PayoutUpdateResponse>(unknownStatus)).Code.ShouldBe("invalid-status");

        var missing = await Client.PatchAsJsonAsync("api/lottery/admin/winners/2026-07-28/000000000000000000000000/payout",
            new PayoutUpdateRequest(LotteryPayoutStatus.Paid, null));
        missing.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await ReadAsync<PayoutUpdateResponse>(missing)).Code.ShouldBe("not-found");

        // A winner id under the wrong round key is not this winner.
        var wrongRound = await Client.PatchAsJsonAsync($"api/lottery/admin/winners/2026-07-21/{winner.Id}/payout",
            new PayoutUpdateRequest(LotteryPayoutStatus.Paid, null));
        wrongRound.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // A malformed id is a typed 404, never a raw exception from the driver.
        var malformed = await Client.PatchAsJsonAsync("api/lottery/admin/winners/2026-07-28/abc/payout",
            new PayoutUpdateRequest(LotteryPayoutStatus.Paid, null));
        malformed.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await ReadAsync<PayoutUpdateResponse>(malformed)).Code.ShouldBe("not-found");
    }

    [Fact]
    public async Task Payout_RepeatingTheStoredStatusIsAnUnchangedSuccess()
    {
        await AuthenticateAsAdmin();
        var winner = await SeedWinnerAsync("2026-07-28", "Elara", 94, 7_500_000, LotteryPayoutStatus.Paid);

        var response = await Client.PatchAsJsonAsync(PayoutUrl(winner),
            new PayoutUpdateRequest(LotteryPayoutStatus.Paid, "second click"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await ReadAsync<PayoutUpdateResponse>(response);
        body.Success.ShouldBeTrue();
        body.Code.ShouldBe("unchanged");
        body.Payout!.PayoutStatus.ShouldBe(LotteryPayoutStatus.Paid);
    }

    #endregion

    #region Prize pool

    [Fact]
    public async Task Prizes_CheckPermissions()
    {
        const string prizeId = "000000000000000000000000";

        SetUnauthenticated();
        (await Client.GetAsync(PrizesUrl)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await Client.PostAsJsonAsync(PrizesUrl, new CreatePrizeRequest("Mount"))).StatusCode
            .ShouldBe(HttpStatusCode.Unauthorized);
        (await Client.DeleteAsync($"{PrizesUrl}/{prizeId}")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await Client.PostAsJsonAsync($"{PrizesUrl}/{prizeId}/assign",
            new AssignPrizeRequest("2026-07-28", prizeId))).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        SetAuthenticatedUser("12355");
        (await Client.GetAsync(PrizesUrl)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await Client.PostAsJsonAsync(PrizesUrl, new CreatePrizeRequest("Mount"))).StatusCode
            .ShouldBe(HttpStatusCode.Forbidden);
        (await Client.DeleteAsync($"{PrizesUrl}/{prizeId}")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        await AuthenticateAsMember();
        (await Client.GetAsync(PrizesUrl)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await Client.DeleteAsync($"{PrizesUrl}/{prizeId}")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        await AuthenticateAsAdmin();
        (await Client.GetAsync(PrizesUrl)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await Client.DeleteAsync($"{PrizesUrl}/{prizeId}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await Client.PostAsJsonAsync($"{PrizesUrl}/{prizeId}/assign",
            new AssignPrizeRequest("2026-07-28", prizeId))).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Prizes_ListThePoolAndTheAwardedItemsWithCounts()
    {
        await AuthenticateAsAdmin();
        await SeedPrizeAsync("Chaotic Mount");
        await SeedPrizeAsync("Fuax Mount");
        await SeedPrizeAsync("Cloudsie", LotteryPrizePoolStatus.Awarded, "2026-07-21", 123456789012345678, "Siegward");

        var pool = await GetPrizesAsync();

        pool.InPoolCount.ShouldBe(2);
        pool.AwardedCount.ShouldBe(1);
        pool.Items.Count.ShouldBe(3);

        var awarded = pool.Items.Single(item => item.Name == "Cloudsie");
        awarded.Status.ShouldBe(LotteryPrizePoolStatus.Awarded);
        awarded.AwardedToName.ShouldBe("Siegward");
        awarded.AwardedToDiscordId.ShouldBe("123456789012345678");
        awarded.AwardedRoundKey.ShouldBe("2026-07-21");
    }

    [Fact]
    public async Task Prizes_AddAnItemAndRecordWhoAddedIt()
    {
        var admin = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);

        var response = await Client.PostAsJsonAsync(PrizesUrl, new CreatePrizeRequest("  Chaotic Mount  "));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await ReadAsync<PrizeResponse>(response);
        body.Success.ShouldBeTrue();
        body.Code.ShouldBe("created");
        body.Prize!.Name.ShouldBe("Chaotic Mount");
        body.Prize.Status.ShouldBe(LotteryPrizePoolStatus.InPool);
        body.Prize.AddedByDiscordId.ShouldBe(admin.DiscordId);

        var pool = await GetPrizesAsync();
        pool.Items.ShouldHaveSingleItem().Id.ShouldBe(body.Prize.Id);
    }

    [Fact]
    public async Task Prizes_RejectAnEmptyOrDuplicateName()
    {
        await AuthenticateAsAdmin();
        await SeedPrizeAsync("Chaotic Mount");

        var empty = await Client.PostAsJsonAsync(PrizesUrl, new CreatePrizeRequest("   "));
        empty.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ReadAsync<PrizeResponse>(empty)).Code.ShouldBe("invalid");

        var duplicate = await Client.PostAsJsonAsync(PrizesUrl, new CreatePrizeRequest("chaotic mount"));
        duplicate.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ReadAsync<PrizeResponse>(duplicate)).Code.ShouldBe("duplicate");

        (await GetPrizesAsync()).Items.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Prizes_RemoveAnInPoolItemButNotAnAwardedOne()
    {
        await AuthenticateAsAdmin();
        var inPool = await SeedPrizeAsync("Fuax Mount");
        var awarded = await SeedPrizeAsync("Cloudsie", LotteryPrizePoolStatus.Awarded, "2026-07-21",
            123456789012345678, "Siegward");

        (await Client.DeleteAsync($"{PrizesUrl}/{inPool.Id}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await GetPrizesAsync()).Items.ShouldNotContain(item => item.Id == inPool.Id);

        // An awarded item is indivisible and owned by a winner; it returns to the pool only through the forfeit path.
        var refused = await Client.DeleteAsync($"{PrizesUrl}/{awarded.Id}");
        refused.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ReadAsync<PrizeResponse>(refused)).Code.ShouldBe("awarded");

        (await Client.DeleteAsync($"{PrizesUrl}/000000000000000000000000")).StatusCode
            .ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Prizes_AssignAnItemAndItBecomesTheWinnersBonusItemOnHistory()
    {
        await AuthenticateAsAdmin();
        await SeedPotAsync(5_000_000);
        var winner = await SeedWinnerAsync("2026-07-28", "Elara", 94, 7_500_000, LotteryPayoutStatus.Pending,
            discordId: 123456789012345678);
        var prize = await SeedPrizeAsync("Chaotic Mount");

        var response = await Client.PostAsJsonAsync($"{PrizesUrl}/{prize.Id}/assign",
            new AssignPrizeRequest("2026-07-28", winner.Id));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await ReadAsync<PrizeResponse>(response);
        body.Success.ShouldBeTrue();
        body.Code.ShouldBe("assigned");
        body.Prize!.Status.ShouldBe(LotteryPrizePoolStatus.Awarded);
        body.Prize.AwardedToDiscordId.ShouldBe("123456789012345678");
        body.Prize.AwardedToName.ShouldBe("Elara");
        body.Prize.AwardedRoundKey.ShouldBe("2026-07-28");
        body.Winner!.Items.ShouldBe(new[] { "Chaotic Mount" });

        (await GetPrizesAsync()).InPoolCount.ShouldBe(0);

        // A prize is not money: assigning it moves no Gil and writes no ledger entry (spec §3).
        (await GetStatusAsync()).Pot.ShouldBe(5_000_000);

        // The same write is what the public history reads: the item appears on the winner card.
        var historyResponse = await Client.GetAsync("api/Lottery/history");
        historyResponse.EnsureSuccessStatusCode();
        var history = await historyResponse.Content.ReadFromJsonAsync<JsonElement>();
        var items = history.GetProperty("Items");
        items.GetArrayLength().ShouldBe(1);
        items[0].GetProperty("PrizeItems")[0].GetString().ShouldBe("Chaotic Mount");
    }

    [Fact]
    public async Task Prizes_RejectAnUnknownTargetAnAwardedItemAndAForfeitedWinner()
    {
        await AuthenticateAsAdmin();
        var winner = await SeedWinnerAsync("2026-07-28", "Elara", 94, 7_500_000, LotteryPayoutStatus.Pending,
            discordId: 123456789012345678);
        var forfeited = await SeedWinnerAsync("2026-07-21", "Siegward", 56, 1_000_000,
            LotteryPayoutStatus.Forfeited, discordId: 999999999999999999);
        var prize = await SeedPrizeAsync("Chaotic Mount");
        var historical = await SeedWinnerAsync("2026-07-14", "Old Winner", 12, 1_000_000);

        var malformedId = await Client.DeleteAsync($"{PrizesUrl}/not-an-id");
        malformedId.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await ReadAsync<PrizeResponse>(malformedId)).Code.ShouldBe("not-found");

        var missingPrize = await Client.PostAsJsonAsync($"{PrizesUrl}/000000000000000000000000/assign",
            new AssignPrizeRequest("2026-07-28", winner.Id));
        missingPrize.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await ReadAsync<PrizeResponse>(missingPrize)).Code.ShouldBe("not-found");

        var missingWinner = await Client.PostAsJsonAsync($"{PrizesUrl}/{prize.Id}/assign",
            new AssignPrizeRequest("2026-07-28", "000000000000000000000000"));
        missingWinner.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await ReadAsync<PrizeResponse>(missingWinner)).Code.ShouldBe("not-found");

        // A historical winner has no Discord id, so an item cannot be linked to them.
        var historicalWinner = await Client.PostAsJsonAsync($"{PrizesUrl}/{prize.Id}/assign",
            new AssignPrizeRequest("2026-07-14", historical.Id));
        historicalWinner.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ReadAsync<PrizeResponse>(historicalWinner)).Code.ShouldBe("invalid");

        var forfeitedWinner = await Client.PostAsJsonAsync($"{PrizesUrl}/{prize.Id}/assign",
            new AssignPrizeRequest("2026-07-21", forfeited.Id));
        forfeitedWinner.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ReadAsync<PrizeResponse>(forfeitedWinner)).Code.ShouldBe("invalid-transition");

        // Assign twice: an awarded item cannot go to a second winner.
        (await Client.PostAsJsonAsync($"{PrizesUrl}/{prize.Id}/assign",
            new AssignPrizeRequest("2026-07-28", winner.Id))).StatusCode.ShouldBe(HttpStatusCode.OK);
        var second = await Client.PostAsJsonAsync($"{PrizesUrl}/{prize.Id}/assign",
            new AssignPrizeRequest("2026-07-21", forfeited.Id));
        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ReadAsync<PrizeResponse>(second)).Code.ShouldBe("awarded");
    }

    #endregion

    #region Opt-ins and the test reminder

    [Fact]
    public async Task OptInsAndReminderTest_CheckPermissions()
    {
        SetUnauthenticated();
        (await Client.GetAsync(OptInsUrl)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await Client.DeleteAsync($"{OptInsUrl}/12345")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await Client.PostAsync(ReminderTestUrl, null)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        SetAuthenticatedUser("12355");
        (await Client.GetAsync(OptInsUrl)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await Client.DeleteAsync($"{OptInsUrl}/12345")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await Client.PostAsync(ReminderTestUrl, null)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        await AuthenticateAsMember();
        (await Client.GetAsync(OptInsUrl)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await Client.PostAsync(ReminderTestUrl, null)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        await AuthenticateAsAdmin();
        (await Client.GetAsync(OptInsUrl)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await Client.DeleteAsync($"{OptInsUrl}/12345")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await Client.PostAsync(ReminderTestUrl, null)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task OptIns_ListTheEnabledRecordsWithNamesAndEligibility()
    {
        var member = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        await SeedOptInAsync(ulong.Parse(member.DiscordId));
        await SeedOptInAsync(1111);
        await AuthenticateAsAdmin();

        var optIns = await GetOptInsAsync();

        optIns.OptedIn.ShouldBe(2);

        var current = optIns.Items.Single(item => item.DiscordId == member.DiscordId);
        current.IsCurrentMember.ShouldBeTrue();
        current.DisplayName.ShouldBe(member.DiscordName);
        current.IsEligible.ShouldBeTrue();

        // An opt-in survives leaving the FC, so an id with no member row is listed but is not pinged.
        var stranger = optIns.Items.Single(item => item.DiscordId == "1111");
        stranger.IsCurrentMember.ShouldBeFalse();
        stranger.IsEligible.ShouldBeFalse();
        stranger.DisplayName.ShouldBeNull();

        optIns.Eligible.ShouldBe(1);
    }

    [Fact]
    public async Task OptIns_RemoveAnOptInAndAnswerNotFoundForAnUnknownOne()
    {
        var member = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        await SeedOptInAsync(ulong.Parse(member.DiscordId));
        await AuthenticateAsAdmin();

        (await Client.DeleteAsync($"{OptInsUrl}/{member.DiscordId}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await GetOptInsAsync()).OptedIn.ShouldBe(0);

        (await Client.DeleteAsync($"{OptInsUrl}/{member.DiscordId}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // A non-numeric id cannot be a Discord id, so it is a typed 400 rather than an unparsed crash.
        (await Client.DeleteAsync($"{OptInsUrl}/not-a-number")).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReminderTest_IsDeliveredOnlyToTheRequestingAdmin()
    {
        var member = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        await SeedOptInAsync(ulong.Parse(member.DiscordId));
        await AuthenticateAsAdmin();

        var response = await Client.PostAsync(ReminderTestUrl, null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await ReadAsync<ReminderTest>(response);
        body.Posted.ShouldBeTrue();
        body.Disposition.ShouldBe(LotteryReminderDisposition.Sent);
        body.Recipients.ShouldBe(1);
        body.RoundKey.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ReminderTest_IsTypedWhenNobodyIsOptedIn()
    {
        await AuthenticateAsAdmin();

        var response = await Client.PostAsync(ReminderTestUrl, null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await ReadAsync<ReminderTest>(response);
        body.Posted.ShouldBeFalse();
        body.Disposition.ShouldBe(LotteryReminderDisposition.NobodyOptedIn);
        body.Recipients.ShouldBe(0);
        body.Message.ShouldNotBeNullOrWhiteSpace();
    }

    #endregion

    #region Preview, run and recovery

    [Fact]
    public async Task PreviewAndActions_CheckPermissions()
    {
        SetUnauthenticated();
        (await Client.GetAsync($"{PreviewUrl}?result=win")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        foreach (var route in ActionRoutes())
            (await Client.PostAsJsonAsync(route, new ManualActionRequest(null, null))).StatusCode
                .ShouldBe(HttpStatusCode.Unauthorized);

        SetAuthenticatedUser("12355");
        foreach (var route in ActionRoutes())
            (await Client.PostAsJsonAsync(route, new ManualActionRequest(null, null))).StatusCode
                .ShouldBe(HttpStatusCode.Forbidden);

        await AuthenticateAsMember();
        foreach (var route in ActionRoutes())
            (await Client.PostAsJsonAsync(route, new ManualActionRequest(null, null))).StatusCode
                .ShouldBe(HttpStatusCode.Forbidden);

        await AuthenticateAsAdmin();
        (await Client.GetAsync($"{PreviewUrl}?result=win")).StatusCode.ShouldBe(HttpStatusCode.OK);

        // Every action route is reachable by an admin; the exact disposition is asserted by the functional tests below.
        foreach (var route in ActionRoutes())
        {
            var status = (await Client.PostAsJsonAsync(route, new ManualActionRequest(null, null))).StatusCode;
            status.ShouldNotBe(HttpStatusCode.Unauthorized);
            status.ShouldNotBe(HttpStatusCode.Forbidden);
        }
    }

    [Fact]
    public async Task Preview_RendersBothVariantsWithTheLivePot()
    {
        await AuthenticateAsAdmin();
        await SeedPotAsync(21_000_000);
        await SeedPrizeAsync("Chaotic Mount");
        await SeedPrizeAsync("Fuax Mount");

        var win = await GetPreviewAsync("win");
        win.Variant.ShouldBe("win");
        win.Text.ShouldContain("21,000,000");

        // The test deployment configures no lottery member role, so the composer falls back to plain text instead of
        // posting a broken mention; the greeting and the generated tail are still there.
        win.Text.ShouldContain("Hello ");
        win.Text.ShouldContain("Submit your guesses by ");
        win.Text.ShouldContain("Full history:");
        win.Text.ShouldContain("Chaotic Mount");
        win.VisibleLength.ShouldBeGreaterThan(0);
        win.VisibleLength.ShouldBeLessThan(2_000);
        win.RawLength.ShouldBeGreaterThan(win.VisibleLength);

        var noWin = await GetPreviewAsync("nowin");
        noWin.Variant.ShouldBe("nowin");
        noWin.Text.ShouldContain("22,000,000");
        noWin.VisibleLength.ShouldBeLessThan(2_000);

        // An absent variant is the no-win copy, the one the page opens on.
        var (defaulted, _) = (await ReadAsync<Preview>(await Client.GetAsync(PreviewUrl)), 0);
        defaulted.Variant.ShouldBe("nowin");

        var invalid = await Client.GetAsync($"{PreviewUrl}?result=banana");
        invalid.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await invalid.Content.ReadAsStringAsync()).ShouldContain("invalid_variant");
    }

    [Fact]
    public async Task DryRun_PostsThePrefixedAnnouncementToTheLogAndWritesNothing()
    {
        await AuthenticateAsAdmin();
        await SeedPotAsync(4_000_000);
        await SeedGuessAsync(4242, 42);
        await SeedExtraGuessAsync(4242);

        var response = await Client.PostAsJsonAsync(DryRunUrl, new ManualActionRequest(null, null));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await ReadAsync<ManualAction>(response);
        body.Success.ShouldBeTrue();
        body.Disposition.ShouldBe(LotteryRunDisposition.DryRun);

        // criterion 20, the whole contract: the *same* announcement, prefixed, on the mentions-suppressed log seam —
        // never the live announcement post.
        var posted = DiscordMessages.Invocations
            .Where(call => call.Method.Name == nameof(IDiscordMessageService.PostDryRunAsync))
            .Select(call => (string)call.Arguments[0]!)
            .ShouldHaveSingleItem();
        posted.ShouldStartWith("[DRY RUN] ");
        posted.ShouldContain("Hello ");
        DiscordMessages.Verify(
            messages => messages.PostLotteryAnnouncementAsync(It.IsAny<string>()), Times.Never);

        // And no writes: no round, no frozen entries, no winners, no ledger movement, no guess flush, same pot.
        (await Factory.Services.GetRequiredService<ILotteryRoundRepository>().GetLatestAsync(10)).ShouldBeEmpty();
        (await Factory.Services.GetRequiredService<ILotteryEntryRepository>().GetAsync()).ShouldBeEmpty();
        (await Factory.Services.GetRequiredService<ILotteryWinnerRepository>().GetAsync()).ShouldBeEmpty();
        var ledger = await Factory.Services.GetRequiredService<ILotteryPotLedgerService>()
            .GetByRoundKeyAsync("anything");
        ledger.ShouldBeEmpty();
        (await GetStatusAsync()).Pot.ShouldBe(4_000_000);
        (await Factory.Services.GetRequiredService<ILotteryGuessRepository>().GetAsync()).ShouldHaveSingleItem();
        (await Factory.Services.GetRequiredService<IExtraLotteryGuessRepository>().GetAsync())
            .ShouldHaveSingleItem();
    }

    [Fact]
    public async Task AdminMutations_WithAnUnresolvableDiscordId_ReturnATypedUnauthorizedInsteadOfAuditingActorZero()
    {
        // §8 audit trail: an admin mutation is never recorded against actor 0. A session whose Discord id cannot be
        // parsed is a typed `unauthenticated` 401 and writes nothing.
        await CreateAndAuthenticateAsMember("not-a-snowflake", isAdmin: true);

        var response = await Client.PostAsJsonAsync(SkipUrl, new ManualActionRequest("Holiday week", null));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await response.Content.ReadAsStringAsync()).ShouldContain("unauthenticated");

        (await Factory.Services.GetRequiredService<ILotteryRoundRepository>().GetLatestAsync(10)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Run_RunsTheDueRoundOnceAndASecondClickIsAlreadyRun()
    {
        await AuthenticateAsAdmin();

        var response = await Client.PostAsJsonAsync(RunUrl, new ManualActionRequest(null, null));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await ReadAsync<ManualAction>(response);
        body.Success.ShouldBeTrue();
        body.Disposition.ShouldBe(LotteryRunDisposition.Ran);
        body.RoundKey.ShouldNotBeNullOrWhiteSpace();
        body.Pot.ShouldBe(2_000_000);

        var rounds = await Factory.Services.GetRequiredService<ILotteryRoundRepository>().GetLatestAsync(10);
        var round = rounds.ShouldHaveSingleItem();
        round.RoundKey.ShouldBe(body.RoundKey);
        round.Status.ShouldBe(LotteryRoundStatus.Completed);

        var again = await Client.PostAsJsonAsync(RunUrl, new ManualActionRequest(null, null));
        again.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var againBody = await ReadAsync<ManualAction>(again);
        againBody.Disposition.ShouldBe(LotteryRunDisposition.AlreadyRun);
        againBody.Success.ShouldBeFalse();

        // No silent redo: the draw exists once.
        (await Factory.Services.GetRequiredService<ILotteryRoundRepository>().GetLatestAsync(10)).Count.ShouldBe(1);
    }

    [Fact]
    public async Task Run_IsBlockedWhileAnUnfinishedRoundExists()
    {
        await AuthenticateAsAdmin();
        var rounds = Factory.Services.GetRequiredService<ILotteryRoundRepository>();
        await rounds.EnsureScheduledAsync("2026-07-28", new DateTime(2026, 7, 28, 18, 0, 0, DateTimeKind.Utc));
        await rounds.RegisterFailureAsync("2026-07-28", LotteryFailedStep.Announcement, LotteryRoundStatus.Drawn,
            3, "Discord is unreachable", new DateTime(2026, 7, 28, 19, 0, 0, DateTimeKind.Utc), null);

        var response = await Client.PostAsJsonAsync(RunUrl, new ManualActionRequest(null, null));

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var body = await ReadAsync<ManualAction>(response);
        body.Disposition.ShouldBe(LotteryRunDisposition.Blocked);
        body.RoundKey.ShouldBe("2026-07-28");
        body.Message.ShouldContain("2026-07-28");
    }

    [Fact]
    public async Task Run_WithAutomationRolledBackToEnabledFalse_StillWorksAndDeletesNothing()
    {
        // Rollback (criterion 22): the deployment kill switch is off in this configuration, so the scheduler would do
        // nothing — but the manual run is the documented escape hatch and no existing data may change.
        await AuthenticateAsAdmin();
        await SeedPotAsync(3_000_000);
        await SeedWinnerAsync("2026-03-17", "Keburichi", 92, 33_000_000, LotteryPayoutStatus.Paid,
            ["Cosmic Mount"]);

        var status = await GetStatusAsync();
        status.AutomationStatus.ShouldBe(LotteryAutomationStatus.DisabledKind);
        status.SchedulerEnabled.ShouldBeFalse();

        var response = await Client.PostAsJsonAsync(RunUrl, new ManualActionRequest(null, null));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await ReadAsync<ManualAction>(response);
        body.Disposition.ShouldBe(LotteryRunDisposition.Ran);

        // Nobody guessed, so the manual round grew the pot by the weekly increment.
        (await GetStatusAsync()).Pot.ShouldBe(4_000_000);
        (await Factory.Services.GetRequiredService<ILotteryRoundRepository>()
            .GetByRoundKeyAsync(body.RoundKey!))!.Status.ShouldBe(LotteryRoundStatus.Completed);

        // The seeded history survives untouched: a rollback is a flag, never a data migration.
        var seeded = (await Factory.Services.GetRequiredService<ILotteryWinnerRepository>().GetAsync())
            .ShouldHaveSingleItem();
        seeded.DisplayName.ShouldBe("Keburichi");
        seeded.BonusItems.ShouldBe(["Cosmic Mount"]);
        seeded.PayoutStatus.ShouldBe(LotteryPayoutStatus.Paid);
    }

    [Fact]
    public async Task Skip_MarksTheUpcomingRoundSkippedAndLeavesThePotAlone()
    {
        await AuthenticateAsAdmin();
        await SeedPotAsync(3_000_000);

        var response = await Client.PostAsJsonAsync(SkipUrl,
            new ManualActionRequest("Holiday week", null));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await ReadAsync<ManualAction>(response);
        body.Disposition.ShouldBe(LotteryRunDisposition.Skipped);
        body.Pot.ShouldBe(3_000_000);

        var round = (await Factory.Services.GetRequiredService<ILotteryRoundRepository>()
            .GetByRoundKeyAsync(body.RoundKey!)).ShouldNotBeNull();
        round.Status.ShouldBe(LotteryRoundStatus.Skipped);
    }

    [Fact]
    public async Task Void_VoidsTheLastDrawAndReversesItsLedger()
    {
        await AuthenticateAsAdmin();
        await Factory.Services.GetRequiredService<ILotteryRoundRepository>().ImportCompletedRoundAsync(
            "2026-07-28", new DateTime(2026, 7, 28, 18, 0, 0, DateTimeKind.Utc), 94);
        await Factory.Services.GetRequiredService<ILotteryPotLedgerService>().AppendAsync("2026-07-28",
            LotteryPotLedgerType.WeeklyIncrement, 1_000_000, 2_000_000, "weekly increment", null,
            "2026-07-28:weekly_increment");

        var response = await Client.PostAsJsonAsync(VoidUrl,
            new ManualActionRequest("Drawn with the wrong number", null));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await ReadAsync<ManualAction>(response);
        body.Disposition.ShouldBe(LotteryRunDisposition.Voided);
        body.RoundKey.ShouldBe("2026-07-28");

        var round = (await Factory.Services.GetRequiredService<ILotteryRoundRepository>()
            .GetByRoundKeyAsync("2026-07-28")).ShouldNotBeNull();
        round.Status.ShouldBe(LotteryRoundStatus.Voided);

        var entries = await Factory.Services.GetRequiredService<ILotteryPotLedgerService>()
            .GetByRoundKeyAsync("2026-07-28");
        entries.Any(entry => entry.Type == LotteryPotLedgerType.Correction && entry.Amount == -1_000_000)
            .ShouldBeTrue();
    }

    [Fact]
    public async Task Void_RequiresAReasonAndARoundToVoid()
    {
        await AuthenticateAsAdmin();

        var noReason = await Client.PostAsJsonAsync(VoidUrl, new ManualActionRequest("   ", null));
        noReason.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var noReasonBody = await ReadAsync<ManualAction>(noReason);
        noReasonBody.Success.ShouldBeFalse();
        noReasonBody.Disposition.ShouldBe(LotteryRunDisposition.InvalidRequest);

        var noRound = await Client.PostAsJsonAsync(VoidUrl, new ManualActionRequest("Nothing to void", null));
        noRound.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await ReadAsync<ManualAction>(noRound)).Disposition.ShouldBe(LotteryRunDisposition.NotFound);
    }

    [Fact]
    public async Task Reschedule_MovesTheRoundKeyToTheNewLocalDateAndLeavesTheGuessesAndThePot()
    {
        await AuthenticateAsAdmin();
        await SeedPotAsync(3_000_000);
        var guesses = Factory.Services.GetRequiredService<ILotteryGuessRepository>();
        await guesses.UpsertAsync(new LotteryGuess { DiscordId = 7001, Number = 42 });

        // The round key follows the new local date: 2026-12-03T23:30Z is already 2026-12-04 in Europe/Berlin.
        var response = await Client.PostAsJsonAsync(RescheduleUrl,
            new RescheduleBody("2026-07-28", new DateTimeOffset(2026, 12, 3, 23, 30, 0, TimeSpan.Zero)));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await ReadAsync<ManualAction>(response);
        body.Success.ShouldBeTrue();
        body.Disposition.ShouldBe(LotteryRunDisposition.Rescheduled);
        body.RoundKey.ShouldBe("2026-12-04");

        var rounds = Factory.Services.GetRequiredService<ILotteryRoundRepository>();
        (await rounds.GetByRoundKeyAsync("2026-07-28")).ShouldBeNull();
        (await rounds.GetByRoundKeyAsync("2026-12-04")).ShouldNotBeNull();

        // Guesses stay with the round and the pot is untouched until the run.
        (await guesses.GetAsync()).ShouldHaveSingleItem();
        (await Factory.Services.GetRequiredService<ILotteryPotLedgerService>().GetBalanceAsync(1_000_000))
            .ShouldBe(3_000_000);
    }

    [Fact]
    public async Task Reschedule_IsATypedConflictWhenTheTargetDateAlreadyHasARound()
    {
        await AuthenticateAsAdmin();
        var rounds = Factory.Services.GetRequiredService<ILotteryRoundRepository>();
        await rounds.ImportCompletedRoundAsync("2026-12-04",
            new DateTime(2026, 12, 4, 19, 0, 0, DateTimeKind.Utc), 17);

        var response = await Client.PostAsJsonAsync(RescheduleUrl,
            new RescheduleBody("2026-07-28", new DateTimeOffset(2026, 12, 3, 23, 30, 0, TimeSpan.Zero)));

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var body = await ReadAsync<ManualAction>(response);
        body.Success.ShouldBeFalse();
        body.Disposition.ShouldBe(LotteryRunDisposition.RescheduleConflict);

        // Neither round moved: the source still exists on its original date.
        (await rounds.GetByRoundKeyAsync("2026-07-28")).ShouldNotBeNull();
        (await rounds.GetByRoundKeyAsync("2026-12-04"))!.Status.ShouldBe(LotteryRoundStatus.Completed);
    }

    [Fact]
    public async Task Reschedule_RejectsAMissingInstantAndAMalformedRoundKeyWithTypedBadRequests()
    {
        await AuthenticateAsAdmin();

        var noInstant = await Client.PostAsJsonAsync(RescheduleUrl, new RescheduleBody("2026-07-28", null));
        noInstant.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ReadAsync<ManualAction>(noInstant)).Disposition.ShouldBe(LotteryRunDisposition.InvalidRequest);

        var badKey = await Client.PostAsJsonAsync(RescheduleUrl,
            new RescheduleBody("next-week", new DateTimeOffset(2026, 12, 4, 17, 0, 0, TimeSpan.Zero)));
        badKey.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ReadAsync<ManualAction>(badKey)).Disposition.ShouldBe(LotteryRunDisposition.InvalidRequest);
    }

    [Fact]
    public async Task Retry_IsTypedWhenThereIsNothingToResume()
    {
        await AuthenticateAsAdmin();

        var response = await Client.PostAsJsonAsync(RetryUrl, new ManualActionRequest(null, null));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await ReadAsync<ManualAction>(response);
        body.Success.ShouldBeTrue();
        body.Disposition.ShouldBe(LotteryRunDisposition.NothingDue);
        body.Message.ShouldNotBeNullOrWhiteSpace();
    }

    #endregion

    #region Helpers

    private async Task<AdminStatus> GetStatusAsync()
    {
        var response = await Client.GetAsync(StatusUrl);
        response.EnsureSuccessStatusCode();
        return await ReadAsync<AdminStatus>(response);
    }

    private async Task<Settings> GetSettingsAsync()
    {
        var response = await Client.GetAsync(SettingsUrl);
        response.EnsureSuccessStatusCode();
        return await ReadAsync<Settings>(response);
    }

    private async Task<LedgerPage> GetLedgerAsync(string? cursor = null, int? limit = null)
    {
        var query = new List<string>();
        if (cursor is not null) query.Add($"cursor={Uri.EscapeDataString(cursor)}");
        if (limit is not null) query.Add($"limit={limit}");

        var url = query.Count == 0 ? LedgerUrl : $"{LedgerUrl}?{string.Join('&', query)}";
        var response = await Client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await ReadAsync<LedgerPage>(response);
    }

    private async Task<PayoutPage> GetPayoutsAsync(string? status = null, string? cursor = null, int? limit = null)
    {
        var query = new List<string>();
        if (status is not null) query.Add($"status={Uri.EscapeDataString(status)}");
        if (cursor is not null) query.Add($"cursor={Uri.EscapeDataString(cursor)}");
        if (limit is not null) query.Add($"limit={limit}");

        var url = query.Count == 0 ? PayoutsUrl : $"{PayoutsUrl}?{string.Join('&', query)}";
        var response = await Client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await ReadAsync<PayoutPage>(response);
    }

    private async Task<PrizePool> GetPrizesAsync()
    {
        var response = await Client.GetAsync(PrizesUrl);
        response.EnsureSuccessStatusCode();
        return await ReadAsync<PrizePool>(response);
    }

    private static IEnumerable<string> ActionRoutes()
    {
        return ["api/lottery/admin/run", "api/lottery/admin/skip", "api/lottery/admin/void",
            "api/lottery/admin/retry", "api/lottery/admin/dry-run", "api/lottery/admin/reschedule"];
    }

    private async Task<Preview> GetPreviewAsync(string result)
    {
        var response = await Client.GetAsync($"{PreviewUrl}?result={result}");
        response.EnsureSuccessStatusCode();
        return await ReadAsync<Preview>(response);
    }

    private async Task<OptIns> GetOptInsAsync()
    {
        var response = await Client.GetAsync(OptInsUrl);
        response.EnsureSuccessStatusCode();
        return await ReadAsync<OptIns>(response);
    }

    private static string PayoutUrl(LotteryWinner winner)
    {
        return $"api/lottery/admin/winners/{winner.RoundKey}/{winner.Id}/payout";
    }

    private static PotAdjustmentRequest PotRequest(long amount, string direction)
    {
        return new PotAdjustmentRequest(amount, "test adjustment", direction, null);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response) where T : class
    {
        var body = await response.Content.ReadFromJsonAsync<T>();
        body.ShouldNotBeNull();
        return body;
    }

    /// <summary>The defaults as the page's form would send them: every editable field, no reset flag.</summary>
    private static LotterySettingsSaveRequest DefaultsRequest()
    {
        return new LotterySettingsSaveRequest(
            LotterySettingsDefaults.RunDay,
            LotterySettingsDefaults.RunTime,
            LotterySettingsDefaults.RunTimezone,
            LotterySettingsDefaults.ReminderDay,
            LotterySettingsDefaults.ReminderTime,
            LotterySettingsDefaults.ReminderTimezone,
            LotterySettingsDefaults.RemindersEnabled,
            LotterySettingsDefaults.PauseAutomation,
            LotterySettingsDefaults.WinTemplate,
            LotterySettingsDefaults.NoWinTemplate,
            false);
    }

    private async Task SeedGuessAsync(ulong discordId, int number)
    {
        await Factory.Services.GetRequiredService<ILotteryGuessRepository>().UpsertAsync(new LotteryGuess
        {
            DiscordId = discordId,
            Number = number,
            DateCreated = DateTime.UtcNow
        });
    }

    private async Task SeedExtraGuessAsync(ulong discordId)
    {
        await Factory.Services.GetRequiredService<IExtraLotteryGuessRepository>().CreateAsync(new ExtraLotteryGuess
        {
            DiscordId = discordId,
            Reason = "test event award",
            DateCreated = DateTime.UtcNow
        });
    }

    private async Task SeedPotAsync(long balance)
    {
        await Factory.Services.GetRequiredService<ILotteryPotLedgerService>()
            .AppendAsync(null, LotteryPotLedgerType.Correction, balance, balance, "admin test seed", null,
                $"admin-test-pot-{Guid.NewGuid()}");
    }

    private async Task<LotteryPrize> SeedPrizeAsync(string name, string status = LotteryPrizePoolStatus.InPool,
        string? roundKey = null, ulong? awardedTo = null, string? awardedToName = null)
    {
        var prize = new LotteryPrize
        {
            Name = name,
            Status = status,
            AwardedRoundKey = roundKey,
            AwardedToDiscordId = awardedTo,
            AwardedToName = awardedToName,
            AddedAt = DateTime.UtcNow
        };

        await Factory.Services.GetRequiredService<ILotteryPrizePoolRepository>().CreateAsync(prize);
        return prize;
    }

    private async Task<LotteryWinner> SeedWinnerAsync(string roundKey, string displayName, int guess,
        long gilPrize, string payoutStatus = LotteryPayoutStatus.Pending, string[]? bonusItems = null,
        ulong? discordId = null, DateTime? createdUtc = null)
    {
        var winner = new LotteryWinner
        {
            RoundKey = roundKey,
            DiscordId = discordId,
            DisplayName = displayName,
            Guess = guess,
            GilPrize = gilPrize,
            BonusItems = bonusItems ?? [],
            PayoutStatus = payoutStatus
        };

        var repository = Factory.Services.GetRequiredService<ILotteryWinnerRepository>();
        if (createdUtc is null)
        {
            await repository.CreateAsync(winner);
        }
        else
        {
            // Upsert keeps the explicit creation instant, which the keyset cursor uses as the tie-breaker.
            winner.DateCreated = createdUtc.Value;
            await repository.UpsertAsync(winner);
        }

        return winner;
    }

    private async Task SeedOptInAsync(ulong discordId)
    {
        await Factory.Services.GetRequiredService<ILotteryReminderOptinRepository>()
            .SetEnabledAsync(discordId, true);
    }

    #endregion
}
