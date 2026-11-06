using System.Net;
using System.Net.Http.Json;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.TestFramework.Database;
using ExcelBotCs.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace ExcelBotCs.Tests.Controllers;

/// <summary>
///     The member history timeline (spec §5.8, §8, #113). The tests read the API contract the Vue page consumes:
///     newest first by <c>RoundKey</c>, keyset pagination that never splits or duplicates a shared draw, the summary
///     totals, and the member-only gate. Payout status is admin-only, so it must not leak here.
/// </summary>
public class LotteryHistoryIntegrationTests : IntegrationTestBase
{
    private const string BaseUrl = "api/Lottery/history";

    public LotteryHistoryIntegrationTests(MongoDbFixture fixture) : base(fixture)
    {
    }

    #region Contract mirrors

    // The response contract the frontend types mirror. Reading into these records fails if the payload drifts.
    public sealed record HistoryPage(List<HistoryItem> Items, string? NextCursor, HistoryTotals Totals);

    public sealed record HistoryItem(
        string Id,
        string RoundKey,
        string Date,
        string DisplayName,
        string? DiscordId,
        string? AvatarUrl,
        int Guess,
        long GilPrize,
        List<string> PrizeItems);

    public sealed record HistoryTotals(int Winners, int Draws, long CurrentPot, string? LastDrawRoundKey);

    #endregion

    #region Permission Tests

    [Fact]
    public async Task History_CheckPermissions()
    {
        // No Auth = Unauthorized
        SetUnauthenticated();
        var response = await Client.GetAsync(BaseUrl);
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        // No Member = Forbidden
        SetAuthenticatedUser("12355");
        response = await Client.GetAsync(BaseUrl);
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        // Member = Ok
        await AuthenticateAsMember();
        response = await Client.GetAsync(BaseUrl);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Admin = Ok
        await AuthenticateAsAdmin();
        response = await Client.GetAsync(BaseUrl);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    #endregion

    #region Timeline

    [Fact]
    public async Task History_WithNoWinners_ReturnsAnEmptyTimelineAndTheCurrentPot()
    {
        await AuthenticateAsMember();
        await SeedPotAsync(4200000);

        var page = await GetHistoryAsync();

        page.Items.ShouldBeEmpty();
        page.NextCursor.ShouldBeNull();
        page.Totals.Winners.ShouldBe(0);
        page.Totals.Draws.ShouldBe(0);
        page.Totals.LastDrawRoundKey.ShouldBeNull();
        page.Totals.CurrentPot.ShouldBe(4200000);
    }

    [Fact]
    public async Task History_ReturnsNewestRoundFirstWithTheMemberFacingFields()
    {
        await AuthenticateAsMember();
        await SeedPotAsync(9000000);

        await SeedWinnerAsync("2026-07-21", "Siegward", 56, 80000000, ["Chaotic Mount"]);
        await SeedWinnerAsync("2026-07-28", "Elara", 94, 1100000, []);

        var response = await Client.GetAsync(BaseUrl);
        response.EnsureSuccessStatusCode();

        // The payload is PascalCase, like every other controller response in this app.
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldContain("\"Items\"");
        content.ShouldContain("\"Totals\"");
        content.ShouldContain("\"Winners\"");
        content.ShouldContain("\"CurrentPot\"");

        var page = await ReadPageAsync(response);
        page.Items.Count.ShouldBe(2);
        page.Items.Select(i => i.DisplayName).ShouldBe(new[] { "Elara", "Siegward" });

        var newest = page.Items[0];
        newest.RoundKey.ShouldBe("2026-07-28");
        newest.Date.ShouldBe("28.07.26");
        newest.Guess.ShouldBe(94);
        newest.GilPrize.ShouldBe(1100000);
        newest.PrizeItems.ShouldBeEmpty();
        newest.Id.ShouldNotBeNullOrWhiteSpace();
        newest.DiscordId.ShouldBeNull();
        newest.AvatarUrl.ShouldBeNull();

        var older = page.Items[1];
        older.RoundKey.ShouldBe("2026-07-21");
        older.Date.ShouldBe("21.07.26");
        older.Guess.ShouldBe(56);
        older.GilPrize.ShouldBe(80000000);
        older.PrizeItems.ShouldBe(new[] { "Chaotic Mount" });

        // Payout status is admin-only (spec §5.8) and must never reach the member timeline.
        content.ShouldNotContain("Payout");
        page.NextCursor.ShouldBeNull();
    }

    [Fact]
    public async Task History_ResolvesTheDiscordAvatarOfAKnownMember()
    {
        await AuthenticateAsMember();
        const string avatar = "https://cdn.discordapp.com/avatars/1234/abc.webp";

        var member = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        member.DiscordAvatar = avatar;
        await Factory.Services.GetRequiredService<IMemberRepository>().UpdateAsync(member.Id, member);

        await SeedWinnerAsync("2026-08-04", "Test Member", 12, 5000000, [],
            ulong.Parse(member.DiscordId));
        await SeedWinnerAsync("2026-07-28", "Unknown Winner", 12, 5000000, [], 999999999999999999);

        var page = await GetHistoryAsync();

        var known = page.Items[0];
        known.DisplayName.ShouldBe("Test Member");
        known.DiscordId.ShouldBe(member.DiscordId);
        known.AvatarUrl.ShouldBe(avatar);

        // A winner whose Discord id no member row carries falls back to the initials UI.
        page.Items[1].AvatarUrl.ShouldBeNull();
    }

    [Fact]
    public async Task History_TotalsCountWinnersAndDistinctDraws()
    {
        await AuthenticateAsMember();

        await SeedWinnerAsync("2026-01-01", "Karya", 66, 2500000, []);
        await SeedWinnerAsync("2026-01-01", "Uma", 66, 2500000, ["Fuax Mount"]);
        await SeedWinnerAsync("2026-07-28", "Elara", 94, 1100000, []);

        var page = await GetHistoryAsync();

        page.Totals.Winners.ShouldBe(3);
        page.Totals.Draws.ShouldBe(2);
        page.Totals.LastDrawRoundKey.ShouldBe("2026-07-28");
    }

    #endregion

    #region Cursor pagination

    [Fact]
    public async Task History_PagesThroughASharedDrawWithoutSplittingOrDuplicatingIt()
    {
        await AuthenticateAsMember();

        // A shared draw: two winners, same date and same guess, recorded at the same instant.
        var drawnAt = new DateTime(2026, 1, 1, 18, 0, 0, DateTimeKind.Utc);
        await SeedWinnerAsync("2026-01-01", "Karya", 66, 2500000, [], createdUtc: drawnAt);
        await SeedWinnerAsync("2026-01-01", "Uma", 66, 2500000, [], createdUtc: drawnAt);
        await SeedWinnerAsync("2025-12-25", "Siegward", 30, 1000000, []);

        var seen = new List<HistoryItem>();
        string? cursor = null;
        for (var pageIndex = 0; pageIndex < 5; pageIndex++)
        {
            var page = await GetHistoryAsync(cursor, limit: 1);
            seen.AddRange(page.Items);
            cursor = page.NextCursor;
            if (cursor is null) break;
        }

        // Every winner appears exactly once: a page boundary inside the shared draw neither repeats nor skips a row.
        seen.Count.ShouldBe(3);
        seen.Select(i => i.Id).Distinct().Count().ShouldBe(3);
        seen.Select(i => i.DisplayName).ShouldBe(new[] { "Karya", "Uma", "Siegward" }, ignoreOrder: true);

        // The two shared winners keep the same date and guess, and the draw stays newest-first overall.
        var shared = seen.Where(i => i.RoundKey == "2026-01-01").ToList();
        shared.Count.ShouldBe(2);
        shared.Select(i => i.Date).Distinct().ShouldBe(new[] { "01.01.26" });
        shared.Select(i => i.Guess).Distinct().ShouldBe(new[] { 66 });
        seen[^1].RoundKey.ShouldBe("2025-12-25");
    }

    [Fact]
    public async Task History_DefaultsToTenWinnersPerPage()
    {
        await AuthenticateAsMember();
        for (var day = 1; day <= 12; day++)
            await SeedWinnerAsync(new DateTime(2026, 3, day).ToString("yyyy-MM-dd"), $"Winner {day}", day, 1000000, []);

        var first = await GetHistoryAsync();

        first.Items.Count.ShouldBe(10);
        first.NextCursor.ShouldNotBeNull();
        first.Items[0].DisplayName.ShouldBe("Winner 12");

        var second = await GetHistoryAsync(first.NextCursor);

        second.Items.Count.ShouldBe(2);
        second.NextCursor.ShouldBeNull();
        second.Totals.Winners.ShouldBe(12);
        second.Totals.Draws.ShouldBe(12);

        // The summary numbers are the collection totals, not the page totals.
        first.Totals.Winners.ShouldBe(12);
        first.Totals.Draws.ShouldBe(12);
    }

    [Fact]
    public async Task History_ClampsThePageSizeToTheSupportedRange()
    {
        await AuthenticateAsMember();
        for (var day = 1; day <= 55; day++)
            await SeedWinnerAsync(new DateTime(2026, 1, 1).AddDays(day - 1).ToString("yyyy-MM-dd"), $"Winner {day}",
                day, 1000000, []);

        var capped = await GetHistoryAsync(limit: 500);
        capped.Items.Count.ShouldBe(50);

        var floor = await GetHistoryAsync(limit: 0);
        floor.Items.Count.ShouldBe(1);
    }

    [Fact]
    public async Task History_RejectsAnInvalidCursorWithATypedBadRequest()
    {
        await AuthenticateAsMember();

        var response = await Client.GetAsync($"{BaseUrl}?cursor=not-a-cursor");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldContain("invalid_cursor");

        // An absent or empty cursor is simply the first page.
        (await Client.GetAsync($"{BaseUrl}?cursor=")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    #endregion

    #region Helpers

    private async Task<HistoryPage> GetHistoryAsync(string? cursor = null, int? limit = null)
    {
        var query = new List<string>();
        if (cursor is not null) query.Add($"cursor={Uri.EscapeDataString(cursor)}");
        if (limit is not null) query.Add($"limit={limit}");

        var url = query.Count == 0 ? BaseUrl : $"{BaseUrl}?{string.Join('&', query)}";
        var response = await Client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await ReadPageAsync(response);
    }

    private static async Task<HistoryPage> ReadPageAsync(HttpResponseMessage response)
    {
        var page = await response.Content.ReadFromJsonAsync<HistoryPage>();
        page.ShouldNotBeNull();
        return page;
    }

    private async Task SeedWinnerAsync(string roundKey, string displayName, int guess, long gilPrize,
        string[] bonusItems, ulong? discordId = null, DateTime? createdUtc = null)
    {
        var repository = Factory.Services.GetRequiredService<ILotteryWinnerRepository>();
        var winner = new LotteryWinner
        {
            RoundKey = roundKey,
            DiscordId = discordId,
            DisplayName = displayName,
            Guess = guess,
            GilPrize = gilPrize,
            BonusItems = bonusItems,
            PayoutStatus = LotteryPayoutStatus.Paid
        };

        // Upsert keeps the explicit creation instant, which the cursor uses as the within-round tie-breaker.
        if (createdUtc is not null) winner.DateCreated = createdUtc.Value;
        await repository.UpsertAsync(winner);
    }

    private async Task SeedPotAsync(long balance)
    {
        await Factory.Services.GetRequiredService<ILotteryPotLedgerService>()
            .AppendAsync(null, LotteryPotLedgerType.Correction, balance, balance, "history test seed", null,
                $"history-test-pot-{Guid.NewGuid()}");
    }

    #endregion
}
