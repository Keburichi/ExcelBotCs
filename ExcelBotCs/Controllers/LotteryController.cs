using ExcelBotCs.Services;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.Services.Lottery;
using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.Services.Lottery.Records;
using Microsoft.AspNetCore.Authorization;
using ExcelBotCs.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace ExcelBotCs.Controllers;

[ApiController]
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
        return Ok(new { view = LotteryResponseFormatter.FormatViewResponse(view) });
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
        var allGuesses = new List<GuessInfo>();
        for (var i = 1; i <= 100; i++)
        {
            var whoGuessed = await _lotteryService.WhoGuessedAsync(i);
            if (whoGuessed.Users.Count > 0) allGuesses.Add(new GuessInfo { Number = i, Guessers = whoGuessed.Users });
        }

        return Ok(allGuesses);
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

public record GuessInfo
{
    public int Number { get; set; }
    public List<LotteryUser> Guessers { get; set; }
}