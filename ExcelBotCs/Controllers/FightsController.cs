using ExcelBotCs.Models.Database;
using ExcelBotCs.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExcelBotCs.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FightsController : BaseCrudController<Fight>
{
    private readonly FightService _fightService;

    public FightsController(ILogger<FightsController> logger, FightService fightService) : base(logger, fightService)
    {
        _fightService = fightService;
    }
}