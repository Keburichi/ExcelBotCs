using ExcelBotCs.Services;
using ExcelBotCs.Services.Lottery.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ExcelBotCs.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LotteryController : ControllerBase
{
    private readonly ILotteryService _lotteryService;
    private readonly ICurrentMemberAccessor _currentMemberAccessor;

    public LotteryController(ILotteryService lotteryService, ICurrentMemberAccessor currentMemberAccessor)
    {
        _lotteryService = lotteryService;
        _currentMemberAccessor = currentMemberAccessor;
    }

    [HttpPost]
    [Route("guess/{number:int}")]
    public async Task<IActionResult> Guess(int number)
    {
        var guessResponse = await _lotteryService.GuessAsync(await GetCurrentUserDiscordId(), number);
        return Ok(new {guessResponse});
    }

    [HttpGet]
    [Route("unused")]
    public async Task<IActionResult> GetUnusedNumbers()
    {
        var result = await _lotteryService.GetUnusedNumbersAsync();
        return Ok(new {result});
    }

    [HttpGet]
    [Route("view")]
    public async Task<IActionResult> View()
    {
        var view = await _lotteryService.ViewAsync(await GetCurrentUserDiscordId());
        return Ok(new {view});
    }
    
    private async Task<ulong> GetCurrentUserDiscordId()
    {
        var user =  await _currentMemberAccessor.GetCurrentAsync();
        return ulong.Parse(user?.DiscordId ?? "0");
    }
}