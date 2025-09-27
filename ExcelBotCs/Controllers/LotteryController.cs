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

    [HttpGet]
    [Route("guessedNumbers")]
    public async Task<IActionResult> GetGuessNumbers()
    {
        var result = await _lotteryService.GetUnusedNumbersAsync();
        return Ok(result);
    }

    [HttpGet]
    [Route("view")]
    public async Task<IActionResult> View()
    {
        var user = await _currentMemberAccessor.GetCurrentAsync();
        var view = await _lotteryService.ViewAsync(ulong.Parse(user.DiscordId));
        return Ok(view);
    }
}