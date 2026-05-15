using Discord.WebSocket;
using ExcelBotCs.Attributes;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.DTO.Discord;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ExcelBotCs.Controllers;

[ApiController]
[MemberAuth]
[Route("api/[controller]")]
public class EmojisController : AuthorizedController
{
    private readonly DiscordSocketClient _discordSocketClient;
    private readonly DiscordBotOptions _discordBotOptions;

    public EmojisController(
        ILogger<EmojisController> logger,
        DiscordSocketClient discordSocketClient,
        IOptions<DiscordBotOptions> discordBotOptions) : base(logger)
    {
        _discordSocketClient = discordSocketClient;
        _discordBotOptions = discordBotOptions.Value;
    }

    [HttpGet]
    public ActionResult<List<GuildEmojiResponse>> GetGuildEmojis()
    {
        var guild = _discordSocketClient.Guilds
            .FirstOrDefault(g => g.Id == _discordBotOptions.GuildId);

        if (guild == null)
            return NotFound("Guild not found");

        var emojis = guild.Emotes.Select(e => new GuildEmojiResponse
        {
            Id = e.Id.ToString(),
            Name = e.Name,
            Url = e.Url,
            IsAnimated = e.Animated
        }).ToList();

        return Ok(emojis);
    }
}
