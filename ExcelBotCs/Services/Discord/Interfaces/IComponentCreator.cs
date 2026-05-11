using Discord;
using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Services.Discord.Interfaces;

public interface IComponentCreator
{
    public Task<ComponentBuilderV2> CreateSignupComponents(Event fcEvent, Fight? fight = null);
}