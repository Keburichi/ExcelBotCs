using System.Text.Json;
using ExcelBotCs.Database.DTO;
using ExcelBotCs.Discord;
using ExcelBotCs.Services;
using ExcelBotCs.Services.Import;
using Microsoft.AspNetCore.Mvc;

namespace ExcelBotCs.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImportController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly FightService _fightService;
    private readonly ILogger<ImportController> _logger;
    private readonly ImportService _importService;

    public ImportController(IWebHostEnvironment env, FightService fightService, ILogger<ImportController> logger,
        ImportService importService)
    {
        _env = env;
        _fightService = fightService;
        _logger = logger;
        _importService = importService;
    }

    [HttpGet]
    [Route("fights")]
    public async Task<IActionResult> ImportFights()
    {
        try
        {
            var staticDir = Path.Combine(_env.ContentRootPath, "static");
            var files = new[] { "extremes.json", "savage.json", "ultimate.json" }
                .Select(f => Path.Combine(staticDir, f))
                .ToArray();

            var fights = new List<Fight>();
            foreach (var file in files)
            {
                if (!System.IO.File.Exists(file))
                {
                    _logger.LogWarning("Import file not found: {File}", file);
                    continue;
                }

                var json = await System.IO.File.ReadAllTextAsync(file);
                var model = JsonSerializer.Deserialize<FightImportFile>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (model?.fights == null) continue;

                foreach (var item in model.fights)
                {
                    if (string.IsNullOrWhiteSpace(item.name) || string.IsNullOrWhiteSpace(item.type))
                        continue;

                    if (!Enum.TryParse<FightType>(item.type, true, out var type))
                    {
                        _logger.LogWarning("Unknown fight type '{Type}' in file {File} for name {Name}", item.type,
                            file, item.name);
                        continue;
                    }

                    fights.Add(new Fight
                    {
                        Name = item.name.Trim(),
                        Description = string.IsNullOrWhiteSpace(item.description) ? null : item.description.Trim(),
                        ImageUrl = string.IsNullOrWhiteSpace(item.image_url) ? null : item.image_url.Trim(),
                        Type = type,
                        Raidplans = new List<Raidplan>()
                    });
                }
            }

            var (inserted, updated) = await _fightService.BulkUpsertAsync(fights);
            return Ok(new
            {
                total = fights.Count,
                inserted,
                updated
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import fights");
            return Problem(title: "Failed to import fights", detail: ex.Message);
        }
    }

    [HttpGet]
    [Route("members")]
    public async Task<IActionResult> ImportMembers()
    {
        var members = await _importService.ImportMembers();
        return Ok(members);
    }

    [HttpGet]
    [Route("roles")]
    public async Task<IActionResult> ImportRoles()
    {
        var roles = await _importService.ImportRoles();
        return Ok(roles);
    }

    private class FightImportFile
    {
        public List<FightImportItem> fights { get; set; }
    }

    private class FightImportItem
    {
        public string name { get; set; }
        public string description { get; set; }
        public string type { get; set; }
        public string image_url { get; set; }
    }
}