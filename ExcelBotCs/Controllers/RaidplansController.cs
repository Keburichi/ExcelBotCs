using ExcelBotCs.Models.Database;
using ExcelBotCs.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExcelBotCs.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RaidplansController : BaseCrudController<Raidplan>
{
    private readonly RaidplanService _raidplanService;

    public RaidplansController(ILogger<RaidplansController> logger, RaidplanService raidplanService) : base(logger,
        raidplanService)
    {
        _raidplanService = raidplanService;
    }
}