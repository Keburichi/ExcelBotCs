using ExcelBotCs.Services.Lottery.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ExcelBotCs.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LotteryController : ControllerBase
{
    private readonly ILotteryService _lotteryService;

    public LotteryController(ILotteryService lotteryService)
    {
        _lotteryService = lotteryService;
    }

    [HttpGet]
    [Route("guessedNumbers")]
    public async Task<IActionResult> GetGuessNumbers()
    {
        var result = await _lotteryService.GetUnusedNumbersAsync();
        return Ok(result);
    }
}