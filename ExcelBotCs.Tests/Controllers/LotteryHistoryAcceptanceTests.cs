using System.Net.Http.Json;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.Lottery;
using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.TestFramework.Database;
using ExcelBotCs.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace ExcelBotCs.Tests.Controllers;

/// <summary>
///     The member history page over the real cutover seed (spec §5.8, §6 "Manual seed shapes", §12 step 3; criteria 6,
///     13 and 21). Every other history test seeds a handful of rows it invented; this one inserts the 35 historical
///     winners from <c>docs/lottery/manual-seed.md</c> and asserts what an operator sees on the first day: the summary
///     reads 35 winners across 34 draws, the timeline starts with the last draw, the shared draw is two cards, and
///     paging to the end reaches every row exactly once.
/// </summary>
public class LotteryHistoryAcceptanceTests : IntegrationTestBase
{
    private const string BaseUrl = "api/Lottery/history";
    private const long OpeningPot = 21_000_000;

    public LotteryHistoryAcceptanceTests(MongoDbFixture fixture) : base(fixture)
    {
    }

    #region Contract mirror

    private sealed record HistoryPage(List<HistoryItem> Items, string? NextCursor, HistoryTotals Totals);

    private sealed record HistoryItem(string Id, string RoundKey, string Date, string DisplayName, string? DiscordId,
        string? AvatarUrl, int Guess, long GilPrize, List<string> PrizeItems);

    private sealed record HistoryTotals(int Winners, int Draws, long CurrentPot, string? LastDrawRoundKey);

    #endregion

    [Fact]
    public async Task History_OverTheManualSeed_ReadsThirtyFiveWinnersAcrossThirtyFourDraws()
    {
        await AuthenticateAsMember();
        await SeedCutoverAsync();

        var page = await GetHistoryAsync();

        // The summary cards of the history page: 35 winners recorded, across 34 draws, the seeded pot, the last draw.
        page.Totals.Winners.ShouldBe(LotteryHistoryFixture.WinnerCount);
        page.Totals.Draws.ShouldBe(LotteryHistoryFixture.DrawCount);
        page.Totals.CurrentPot.ShouldBe(OpeningPot);
        page.Totals.LastDrawRoundKey.ShouldBe(LotteryHistoryFixture.NewestRoundKey);

        // The first batch is 10 cards, newest first, starting with the newest draw of the seed.
        page.Items.Count.ShouldBe(LotteryHistoryService.DefaultLimit);
        page.NextCursor.ShouldNotBeNull();

        var newest = page.Items[0];
        newest.RoundKey.ShouldBe("2026-07-21");
        newest.Date.ShouldBe("21.07.26");
        newest.DisplayName.ShouldBe("Siegward");
        newest.Guess.ShouldBe(56);
        newest.GilPrize.ShouldBe(80_000_000);
        newest.PrizeItems.ShouldBe(["Chaotic Mount"]);

        page.Items.Select(item => item.RoundKey)
            .ShouldBe(page.Items.Select(item => item.RoundKey).OrderByDescending(key => key, StringComparer.Ordinal));
    }

    [Fact]
    public async Task History_OverTheManualSeed_PagesToTheEndWithoutSplittingTheSharedDraw()
    {
        await AuthenticateAsMember();
        await SeedCutoverAsync();

        var seen = new List<HistoryItem>();
        var cursor = (string?)null;
        var pages = 0;
        do
        {
            var page = await GetHistoryAsync(cursor);
            page.Items.Count.ShouldBeLessThanOrEqualTo(LotteryHistoryService.DefaultLimit);
            seen.AddRange(page.Items);
            cursor = page.NextCursor;
            pages++;
        } while (cursor is not null);

        // 35 winners in batches of 10 is four pages: 10 + 10 + 10 + 5, and the last one says the list is complete.
        pages.ShouldBe(4);
        seen.Count.ShouldBe(LotteryHistoryFixture.WinnerCount);
        seen.Select(item => item.Id).Distinct().Count().ShouldBe(LotteryHistoryFixture.WinnerCount);
        seen.Select(item => item.RoundKey).Distinct(StringComparer.Ordinal).Count()
            .ShouldBe(LotteryHistoryFixture.DrawCount);

        // The shared draw (2026-01-01, rows 25 and 26) is two cards with the same date and guess.
        var shared = seen.Where(item => item.RoundKey == LotteryHistoryFixture.SharedDrawRoundKey).ToList();
        shared.Count.ShouldBe(2);
        shared.Select(item => item.DisplayName).ShouldBe(["Karya", "Uma"], ignoreOrder: true);
        shared.Select(item => item.Date).Distinct().ShouldBe(["01.01.26"]);
        shared.Select(item => item.Guess).Distinct().ShouldBe([66]);

        // Names keep their apostrophes, and the oldest row of the seed is the last card.
        seen.Select(item => item.DisplayName).ShouldContain("Ta'veren");
        seen.Select(item => item.DisplayName).ShouldContain("H'molosi");
        seen[^1].RoundKey.ShouldBe("2024-09-15");
        seen[^1].DisplayName.ShouldBe("Skor");
        seen[^1].Date.ShouldBe("15.09.24");
    }

    #region Helpers

    /// <summary>The seed of §12 step 3: the 35 winner rows and the single opening-balance ledger entry.</summary>
    private async Task SeedCutoverAsync()
    {
        await LotteryHistoryFixture.SeedAsync(
            Factory.Services.GetRequiredService<ILotteryWinnerRepository>());

        await Factory.Services.GetRequiredService<ILotteryPotLedgerService>()
            .AppendAsync(null, LotteryPotLedgerType.Correction, OpeningPot, OpeningPot,
                "opening balance from manual history", null, "opening-balance");
    }

    private async Task<HistoryPage> GetHistoryAsync(string? cursor = null)
    {
        var url = cursor is null ? BaseUrl : $"{BaseUrl}?cursor={Uri.EscapeDataString(cursor)}";
        var response = await Client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var page = await response.Content.ReadFromJsonAsync<HistoryPage>();
        page.ShouldNotBeNull();
        return page;
    }

    #endregion
}
