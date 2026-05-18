using Discord.WebSocket;
using ExcelBotCs.Attributes;
using ExcelBotCs.Discord;
using ExcelBotCs.Models.DTO.Discord;
using Microsoft.AspNetCore.Mvc;

namespace ExcelBotCs.Controllers;

[ApiController]
[MemberAuth]
[Route("api/[controller]")]
public class EmojisController : AuthorizedController
{
    private readonly IDiscordBotClient _discordClient;

    public EmojisController(ILogger<EmojisController> logger, IDiscordBotClient discordClient) : base(logger)
    {
        _discordClient = discordClient;
    }

    [HttpGet]
    public ActionResult<List<GuildEmojiResponse>> GetGuildEmojis()
    {
        SocketGuild? guild;
        try
        {
            guild = _discordClient.GetExcelGuild();
        }
        catch (InvalidOperationException)
        {
            return NotFound("Guild not found");
        }

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
