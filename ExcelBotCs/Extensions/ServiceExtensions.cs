using Discord;
using Discord.WebSocket;
using ExcelBotCs.Caching;
using ExcelBotCs.Database;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Discord;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Services;
using ExcelBotCs.Services.API;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.Services.FFLogs;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

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

    public static void AddCaching(this IServiceCollection services, IConfiguration configuration)
    {
        var cacheOptions = configuration.GetSection("Cache").Get<CacheOptions>() ?? new CacheOptions();

        if (cacheOptions.Provider == "Redis" && !string.IsNullOrEmpty(cacheOptions.RedisConnectionString))
        {
            services.AddSingleton<IConnectionMultiplexer>(
                ConnectionMultiplexer.Connect(cacheOptions.RedisConnectionString));

            services.AddSingleton<IEntityCacheStore<Boss>, RedisEntityCacheStore<Boss>>();
            services.AddSingleton<IEntityCacheStore<Fight>, RedisEntityCacheStore<Fight>>();
            services.AddSingleton<IEntityCacheStore<Member>, RedisEntityCacheStore<Member>>();
            services.AddSingleton<IEntityCacheStore<MemberRole>, RedisEntityCacheStore<MemberRole>>();
        }
        else
        {
            services.AddSingleton<IEntityCacheStore<Boss>, InMemoryEntityCacheStore<Boss>>();
            services.AddSingleton<IEntityCacheStore<Fight>, InMemoryEntityCacheStore<Fight>>();
            services.AddSingleton<IEntityCacheStore<Member>, InMemoryEntityCacheStore<Member>>();
            services.AddSingleton<IEntityCacheStore<MemberRole>, InMemoryEntityCacheStore<MemberRole>>();
        }

        services.AddSingleton<ICacheAccessor<Boss>, CacheAccessor<Boss>>();
        services.AddSingleton<ICacheAccessor<Fight>, CacheAccessor<Fight>>();
        services.AddSingleton<ICacheAccessor<Member>, CacheAccessor<Member>>();
        services.AddSingleton<ICacheAccessor<MemberRole>, CacheAccessor<MemberRole>>();
        services.AddSingleton<IEntityCacheService, EntityCacheService>();
    }
}