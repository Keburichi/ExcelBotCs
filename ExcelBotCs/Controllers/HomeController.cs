using ExcelBotCs.Models;
using ExcelBotCs.Services.Discord.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ExcelBotCs.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HomeController : AuthorizedController
{
    private readonly IDiscordMessageService _discordMessageService;

    public HomeController(IDiscordMessageService discordMessageService)
    {
        _discordMessageService = discordMessageService;
    }
    
    [HttpGet]
    [Route("announcements")]
    public async Task<List<Announcement>> GetAnnouncements()
    {
        var announcements = await _discordMessageService.GetAnnouncementChannelMessagesAsync();
        return announcements.Select(x => new Announcement(x)).ToList();
    }
}