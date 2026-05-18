using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ExcelBotCs.Extensions;
using ExcelBotCs.Models.Config;
using Microsoft.Extensions.Options;

namespace ExcelBotCs.Discord;

public class DiscordBotService : BackgroundService
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interaction;
    private readonly DiscordBotOptions _config;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DiscordBotService> _logger;

    public DiscordBotService(
        IServiceScopeFactory scopeFactory,
        IOptions<DiscordBotOptions> config,
        IServiceProvider serviceProvider,
        IDiscordBotClient discordBotClient,
        ILogger<DiscordBotService> logger) : base(scopeFactory)
    {
        _client = discordBotClient.Client;
        _config = config.Value;
        _serviceProvider = serviceProvider;
        _logger = logger;

        _interaction = new InteractionService(_client, new InteractionServiceConfig
        {
            DefaultRunMode = RunMode.Sync
        });

        _interaction.Log += message =>
        {
            var level = message.Severity switch
            {
                LogSeverity.Critical => LogLevel.Critical,
                LogSeverity.Error => LogLevel.Error,
                LogSeverity.Warning => LogLevel.Warning,
                LogSeverity.Info => LogLevel.Information,
                _ => LogLevel.Debug
            };
            logger.Log(level, message.Exception, "{Source}: {Message}", message.Source, message.ToString());
            return Task.CompletedTask;
        };

        _interaction.InteractionExecuted += (_, context, result) =>
        {
            if (!result.IsSuccess)
                logger.LogError("Interaction '{Name}' failed: {Error} — {Reason}",
                    context?.Interaction.Data is IApplicationCommandInteractionData d ? d.Name : "unknown",
                    result.Error, result.ErrorReason);
            return Task.CompletedTask;
        };

        _client.Ready += ClientOnReady;
        _client.InteractionCreated += ClientOnInteractionCreated;
    }

    private async Task ClientOnReady()
    {
        if (!_interaction.Modules.IsNullOrEmpty())
            foreach (var module in _interaction.Modules)
                await _interaction.RemoveModuleAsync(module);

        await _interaction.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);
        await _interaction.RegisterCommandsGloballyAsync(true);
    }

    private async Task ClientOnInteractionCreated(SocketInteraction interaction)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = new SocketInteractionContext(_client, interaction);
        await _interaction.ExecuteCommandAsync(context, scope.ServiceProvider);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _client.LoginAsync(TokenType.Bot, _config.Token);
        await _client.StartAsync();
        _logger.LogInformation("Discord bot started");
        await Task.Delay(Timeout.Infinite, stoppingToken)
            .ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Discord bot service stopping");
        await _client.LogoutAsync();
        await _client.StopAsync();
        await base.StopAsync(cancellationToken);
    }
}
