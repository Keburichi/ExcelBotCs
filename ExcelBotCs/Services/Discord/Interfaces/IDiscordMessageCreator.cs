using Discord;
using ExcelBotCs.Models.Database.Events;

namespace ExcelBotCs.Services.Discord.Interfaces;

public interface IDiscordMessageCreator
{
    public Task<ComponentBuilderV2> CreateSignupComponents(Event fcEvent);
    public Task<string> CreateUpcomingRosterMessage(Event fcEvent);
}