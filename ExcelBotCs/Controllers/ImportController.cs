using System.Text.Json;
using ExcelBotCs.Attributes;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.Services.Import;
using ExcelBotCs.Services.Lodestone;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ExcelBotCs.Controllers;

[ApiController]
[AdminAuth]
[Route("api/[controller]")]
public class ImportController : AuthorizedController
{
    private readonly IWebHostEnvironment _env;
    private readonly IFightService _fightService;
    private readonly ImportService _importService;
    private readonly LodestoneService _lodestoneService;
    private readonly IOptions<DiscordBotOptions> _discordBotOptions;

    public ImportController(IWebHostEnvironment env, IFightService fightService, ILogger<ImportController> logger,
        ImportService importService, LodestoneService lodestoneService, IOptions<DiscordBotOptions> discordBotOptions)
        : base(logger)
    {
        _env = env;
        _fightService = fightService;
        _importService = importService;
        _lodestoneService = lodestoneService;
        _discordBotOptions = discordBotOptions;
    }

    [HttpGet]
    [Route("lodestone")]
    public async Task<IActionResult> ImportLodestone()
    {
        var fcMembers = await _lodestoneService.ImportMembers();
        return Ok(new { number = fcMembers.Count, members = fcMembers });
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
                    Logger.LogWarning("Import file not found: {File}", file);
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
                        Logger.LogWarning("Unknown fight type '{Type}' in file {File} for name {Name}", item.type,
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
            Logger.LogError(ex, "Failed to import fights");
            return Problem(title: "Failed to import fights", detail: ex.Message);
        }
    }

    [HttpGet]
    [Route("members")]
    public async Task<IActionResult> ImportMembers()
    {
        var members = await _importService.ImportMembers(_discordBotOptions.Value.GuildId);
        return Ok(members);
    }

    [HttpGet]
    [Route("roles")]
    public async Task<IActionResult> ImportRoles()
    {
        var roles = await _importService.ImportRoles(_discordBotOptions.Value.GuildId);
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