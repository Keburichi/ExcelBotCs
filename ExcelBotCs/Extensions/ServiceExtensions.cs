using Discord;
using Discord.WebSocket;
using ExcelBotCs.Database;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Discord;
using ExcelBotCs.Services;
using ExcelBotCs.Services.API;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.Services.FFLogs;
using ExcelBotCs.Services.Minecraft;

namespace ExcelBotCs.Extensions;

public static class ServiceExtensions
{
    public static void AddDatabaseRepositories(this IServiceCollection services)
    {
        services.AddSingleton<IEventRepository, EventRepository>();
        services.AddSingleton<IFcMemberRepository, FcMemberRepository>();
        services.AddSingleton<IBossRepository, BossRepository>();
        services.AddSingleton<IFightRepository, FightRepository>();
        services.AddSingleton<IResourceRepository, ResourceRepository>();
        services.AddSingleton<IMemberRepository, MemberRepository>();
        services.AddSingleton<IMemberRoleRepository, MemberRoleRepository>();
        services.AddSingleton<IFFLogsImportLogRepository, FFLogsImportLogRepository>();
        services.AddSingleton<ILodestoneDutyRepository, LodestoneDutyRepository>();
        services.AddSingleton<IRaidplanRepository, RaidplanRepository>();
        services.AddSingleton<ILotteryGuessRepository, LotteryGuessRepository>();
        services.AddSingleton<IExtraLotteryGuessRepository, ExtraLotteryGuessRepository>();
        services.AddSingleton<ILotteryResultRepository, LotteryResultRepository>();
        services.AddSingleton<IEventDetailsRepository, EventDetailsRepository>();
        services.AddSingleton<IEventTemplateRepository, EventTemplateRepository>();
    }

    public static void AddApiServices(this IServiceCollection services)
    {
        services.AddSingleton<IEventService, EventService>();
        services.AddSingleton<IFcMemberService, FcMemberService>();
        services.AddSingleton<IBossService, BossService>();
        services.AddSingleton<IFightService, FightService>();
        services.AddSingleton<IResourceService, ResourceService>();
        services.AddSingleton<IMemberService, MemberService>();
        services.AddSingleton<IMemberRoleService, MemberRoleService>();
        services.AddSingleton<ILodestoneDutyService, LodestoneDutyService>();
        services.AddSingleton<IRaidplanService, RaidplanService>();
        services.AddSingleton<IEventTemplateService, EventTemplateService>();
    }

    public static void AddDomainServices(this IServiceCollection services)
    {
        services.AddSingleton<IICalService, ICalService>();
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
            .AddSingleton<DiscordSocketClient>()
            .AddSingleton<IDiscordBotClient, DiscordClient>()
            .ActivateSingleton<IDiscordBotClient>();
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

    public static void AddMinecraftServices(this IServiceCollection services)
    {
        services.AddSingleton<IMinecraftRconService, MinecraftRconService>();
    }

}