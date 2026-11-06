using Discord;
using Discord.WebSocket;
using ExcelBotCs.Database;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Discord;
using ExcelBotCs.Services;
using ExcelBotCs.Services.API;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.Services.FFLogs;
using ExcelBotCs.Services.Lottery;
using ExcelBotCs.Services.Lottery.Interfaces;
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
        services.AddSingleton<ILotteryRoundRepository, LotteryRoundRepository>();
        services.AddSingleton<ILotteryEntryRepository, LotteryEntryRepository>();
        services.AddSingleton<ILotteryWinnerRepository, LotteryWinnerRepository>();
        services.AddSingleton<ILotteryPotLedgerRepository, LotteryPotLedgerRepository>();
        services.AddSingleton<ILotteryReminderOptinRepository, LotteryReminderOptinRepository>();
        services.AddSingleton<ILotteryAwardRepository, LotteryAwardRepository>();
        services.AddSingleton<ILotteryPrizePoolRepository, LotteryPrizePoolRepository>();
        services.AddSingleton<ILotterySettingsRepository, LotterySettingsRepository>();
        services.AddSingleton<ILotterySettingsHistoryRepository, LotterySettingsHistoryRepository>();
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

        // Lottery data maintenance: the index migration and the one-time lottery_results backfill (§6, §12).
        services.AddSingleton<ILotteryIndexInitializer, LotteryIndexInitializer>();
        services.AddSingleton<ILotteryArchiveBackfillService, LotteryArchiveBackfillService>();

        // Lottery settings precedence, validation and the settings audit trail (§7).
        services.AddSingleton<ILotterySettingsService, LotterySettingsService>();

        // Lottery startup guard (§5.10): bad or missing config degrades the scheduler and raises one alert.
        services.AddSingleton<ILotteryStartupCheck, LotteryStartupCheck>();
        services.AddSingleton<LotteryStartupCheckService>();
        services.AddHostedService(provider => provider.GetRequiredService<LotteryStartupCheckService>());

        // Lottery round engine (§5.1, §5.3, §5.10): the clock, the schedule, the pot and the announcement seam.
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ILotteryNumberGenerator, PrngLotteryNumberGenerator>();
        services.AddSingleton<ILotteryPotLedgerService, LotteryPotLedgerService>();
        services.AddSingleton<ILotteryScheduleService, LotteryScheduleService>();
        services.AddSingleton<ILotteryAnnouncementPublisher, LotteryAnnouncementPublisher>();
        services.AddSingleton<ILotteryRoundRunner, LotteryRoundRunner>();

        // The in-process scheduler. It is a singleton as well as a hosted service so tests and the admin page can
        // reach TickOnceAsync; removing the IHostedService registrations leaves the singleton intact.
        services.AddSingleton<LotterySchedulerService>();
        services.AddHostedService(provider => provider.GetRequiredService<LotterySchedulerService>());
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