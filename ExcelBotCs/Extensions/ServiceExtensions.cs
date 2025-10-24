using System.Reflection;
using Discord;
using Discord.WebSocket;
using ExcelBotCs.Database;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Mappers;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Services.API;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.Services.FFLogs;

namespace ExcelBotCs.Extensions;

public static class ServiceExtensions
{
    public static void AddDatabaseRepositories(this IServiceCollection services)
    {
        services.AddSingleton<IEventRepository, EventRepository>();
        services.AddSingleton<IFcMemberRepository, FcMemberRepository>();
        services.AddSingleton<IFightRepository, FightRepository>();
        services.AddSingleton<IMemberRepository, MemberRepository>();
        services.AddSingleton<IMemberRoleRepository, MemberRoleRepository>();
        services.AddSingleton<IFFLogsImportLogRepository, FFLogsImportLogRepository>();
    }

    public static void AddApiServices(this IServiceCollection services)
    {
        services.AddSingleton<IEventService, EventService>();
        services.AddSingleton<IFcMemberService, FcMemberService>();
        services.AddSingleton<IFightService, FightService>();
        services.AddSingleton<IMemberService, MemberService>();
        services.AddSingleton<IMemberRoleService, MemberRoleService>();
    }

    public static void AddDiscordClient(this IServiceCollection services)
    {
        var config = new DiscordSocketConfig()
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent |
                             GatewayIntents.GuildMembers | GatewayIntents.GuildPresences,
            AlwaysDownloadUsers = true,
            MessageCacheSize = 200
        };

        services.AddSingleton(config)
            .AddSingleton<DiscordSocketClient>();
    }

    public static void AddFFLogsServices(this IServiceCollection services)
    {
        // Add HttpClient for FFLogs API calls
        services.AddHttpClient();

        // FFLogs Services
        services.AddSingleton<FFLogsAuthService>();
        services.AddSingleton<FFLogsGraphQLService>();
        services.AddSingleton<FFLogsSyncService>();
    }
}