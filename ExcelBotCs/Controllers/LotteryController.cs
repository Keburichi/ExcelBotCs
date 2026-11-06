using System.Globalization;
using ExcelBotCs.Attributes;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.Services;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.Services.Lottery;
using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.Services.Lottery.Records;
using Microsoft.AspNetCore.Mvc;

namespace ExcelBotCs.Controllers;

[ApiController]
[MemberAuth]
[Route("api/[controller]")]
public class LotteryController : AuthorizedController
{
    private readonly ICurrentMemberAccessor _currentMemberAccessor;
    private readonly ILotteryHistoryService _historyService;
    private readonly ILotteryService _lotteryService;
    private readonly ILotteryReminderService _reminderService;
    private readonly IMemberService _memberService;

    public LotteryController(ILogger<LotteryController> logger, ILotteryService lotteryService,
        ILotteryReminderService reminderService, ILotteryHistoryService historyService,
        ICurrentMemberAccessor currentMemberAccessor, IMemberService memberService) : base(logger)
    {
        _lotteryService = lotteryService;
        _reminderService = reminderService;
        _historyService = historyService;
        _currentMemberAccessor = currentMemberAccessor;
        _memberService = memberService;
    }

    [HttpPost]
    [Route("guess/{number:int}")]
    public async Task<IActionResult> Guess(int number)
    {
        var guessResponse = await _lotteryService.GuessAsync(await GetCurrentUserDiscordId(), number);
        return Ok(new { guessResponse = LotteryResponseFormatter.FormatGuessResponse(guessResponse) });
    }

    [HttpGet]
    [Route("unused")]
    public async Task<IActionResult> GetUnusedNumbers()
    {
        var result = await _lotteryService.GetUnusedNumbersAsync();
        return Ok(new { result = LotteryResponseFormatter.FormatUnusedNumbersResponse(result) });
    }

    [HttpGet]
    [Route("view")]
    public async Task<IActionResult> View()
    {
        var view = await _lotteryService.ViewAsync(await GetCurrentUserDiscordId());

        if (view is ViewResponse r)
            return Ok(new
            {
                view = LotteryResponseFormatter.FormatViewResponse(view),
                usedGuesses = r.UsedGuesses,
                totalGuesses = r.TotalGuesses
            });

        return Ok(new
        {
            view = LotteryResponseFormatter.FormatViewResponse(view),
            usedGuesses = 0,
            totalGuesses = 0
        });
    }

    [HttpPost]
    [Route("change")]
    public async Task<IActionResult> ChangeGuess([FromBody] ChangeGuessRequest request)
    {
        var changeResponse = await _lotteryService.ChangeGuessAsync(
            await GetCurrentUserDiscordId(),
            request.OldNumber,
            request.NewNumber);
        return Ok(new
        {
            changeResponse =
                LotteryResponseFormatter.FormatChangeGuessResponse(changeResponse, request.OldNumber, request.NewNumber)
        });
    }

    [HttpGet]
    [Route("who-guessed/{number:int}")]
    public async Task<IActionResult> WhoGuessed(int number)
    {
        var whoGuessed = await _lotteryService.WhoGuessedAsync(number);
        return Ok(new { whoGuessed = LotteryResponseFormatter.FormatWhoGuessedResponse(whoGuessed) });
    }

    [HttpGet]
    [Route("all-guesses")]
    public async Task<IActionResult> GetAllGuesses()
    {
        var allGuesses = await _lotteryService.GetAllGuessesAsync();
        return Ok(allGuesses
            .Where(g => g.Users.Count > 0)
            .Select(g => new GuessInfoDto { Number = g.Number, Guessers = g.Users })
            .ToList());
    }

    /// <summary>
    ///     The member-facing reminder toggle (spec §5.7). Default off; the web record is the source of truth, and the
    ///     response is the resulting state so the toggle never has to guess.
    /// </summary>
    [HttpGet]
    [Route("reminders")]
    public async Task<IActionResult> GetReminders()
    {
        var preference = await _reminderService.GetPreferenceAsync(await GetCurrentUserDiscordId());
        return Ok(new ReminderPreferenceResponse(preference.Enabled));
    }

    /// <summary>Sets the caller's reminder opt-in and returns the resulting state. Member auth comes from the controller.</summary>
    [HttpPut]
    [Route("reminders")]
    public async Task<IActionResult> SetReminders([FromBody] SetRemindersRequest request)
    {
        // [ApiController] rejects a missing/invalid Enabled before this runs and returns typed validation errors.
        var preference = await _reminderService.SetPreferenceAsync(await GetCurrentUserDiscordId(),
            request.Enabled!.Value);
        return Ok(new ReminderPreferenceResponse(preference.Enabled));
    }

    /// <summary>
    ///     The member history timeline (spec §5.8, §8, #113): winners newest first, keyset paginated so a shared draw is
    ///     never split or duplicated, with the pot, draw and winner totals the page shows. Payout status is admin-only
    ///     and does not appear here.
    /// </summary>
    [HttpGet]
    [Route("history")]
    public async Task<IActionResult> GetHistory([FromQuery] string? cursor = null, [FromQuery] int? limit = null)
    {
        var result = await _historyService.GetPageAsync(cursor, limit, HttpContext.RequestAborted);

        if (result is LotteryHistoryInvalidCursorResult invalid)
            return BadRequest(new LotteryHistoryError("invalid_cursor", invalid.Message));

        var page = (LotteryHistoryPageResult)result;
        return Ok(new LotteryHistoryPage(
            page.Items.Select(item => new LotteryHistoryItem(
                item.Id,
                item.RoundKey,
                item.Date,
                item.DisplayName,
                item.DiscordId?.ToString(CultureInfo.InvariantCulture),
                item.AvatarUrl,
                item.Guess,
                item.GilPrize,
                item.PrizeItems)).ToList(),
            page.NextCursor,
            new LotteryHistoryTotals(page.Totals.Winners, page.Totals.Draws, page.Totals.CurrentPot,
                page.Totals.LastDrawRoundKey)));
    }

    [HttpPost]
    [Route("run")]
    [AdminAuth]
    public async Task<IActionResult> RunLottery()
    {
        await _lotteryService.RunLotteryAsync(await GetCurrentUserDiscordId());
        return Ok(new { message = "Lottery executed successfully" });
    }

    [HttpPost]
    [Route("award")]
    [AdminAuth]
    public async Task<IActionResult> AwardUsers([FromBody] AwardUsersRequest request)
    {
        // Convert usernames to Discord IDs
        var allMembers = await _memberService.GetAsync();
        var userIds = new List<ulong>();

        foreach (var userName in request.UserNames)
        {
            var member = allMembers.FirstOrDefault(m =>
                string.Equals(m.DiscordName, userName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(m.PlayerName ?? string.Empty, userName, StringComparison.OrdinalIgnoreCase));
            if (member != null && ulong.TryParse(member.DiscordId, out var userId)) userIds.Add(userId);
        }

        if (userIds.Count == 0) return BadRequest(new { message = "No valid users found" });

        var response = await _lotteryService.TryAwardUsersAsync(request.Reason, userIds);

        if (response is SuccessAwardResponse success)
        {
            await _lotteryService.AwardUsersAsync(success);
            return Ok(new { message = success.PrettyUsersAwarded });
        }

        return BadRequest(new { message = "No users to award" });
    }

    private async Task<ulong> GetCurrentUserDiscordId()
    {
        var user = await _currentMemberAccessor.GetCurrentAsync();
        return ulong.Parse(user?.DiscordId ?? "0");
    }
}

public record ChangeGuessRequest(int OldNumber, int NewNumber);

public record AwardUsersRequest(string Reason, List<string> UserNames);
/// <summary>The reminder toggle request; <c>Enabled</c> is required so a typo cannot silently turn reminders off.</summary>
public sealed record SetRemindersRequest([System.ComponentModel.DataAnnotations.Required] bool? Enabled);

/// <summary>The reminder toggle response: the state the member now has.</summary>
public sealed record ReminderPreferenceResponse(bool Enabled);
