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
    private readonly ILotteryService _lotteryService;
    private readonly IMemberService _memberService;

    public LotteryController(ILogger<LotteryController> logger, ILotteryService lotteryService,
        ICurrentMemberAccessor currentMemberAccessor, IMemberService memberService) : base(logger)
    {
        _lotteryService = lotteryService;
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

    [HttpPost]
    [Route("bonus-lottery")]
    [AdminAuth]
    public async Task<IActionResult> RunBonusLottery([FromBody] BonusLotteryRequest request)
    {
        try
        {
            var result = await _lotteryService.RunBonusLotteryAsync(
                await GetCurrentUserDiscordId(),
                request.Prize);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    [Route("bonus-lottery/entries")]
    [AdminAuth]
    public async Task<IActionResult> GetBonusLotteryEntries()
    {
        var entries = await _lotteryService.GetBonusLotteryEntriesAsync();
        return Ok(entries);
    }

    private async Task<ulong> GetCurrentUserDiscordId()
    {
        var user = await _currentMemberAccessor.GetCurrentAsync();
        return ulong.Parse(user?.DiscordId ?? "0");
    }
}

public record ChangeGuessRequest(int OldNumber, int NewNumber);

public record AwardUsersRequest(string Reason, List<string> UserNames);

public record BonusLotteryRequest(string Prize);