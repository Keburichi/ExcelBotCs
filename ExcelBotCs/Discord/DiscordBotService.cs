using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ExcelBotCs.Models.Config;
using Microsoft.Extensions.Options;

namespace ExcelBotCs.Discord;

public class DiscordBotService : BackgroundService
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interaction;
    private readonly DiscordBotOptions _config;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DiscordSocketClient> _logger;

    public DiscordBotService(IServiceScopeFactory scopeFactory, IOptions<DiscordBotOptions> config,
        IServiceProvider serviceProvider, ILogger<DiscordSocketClient> logger) : base(scopeFactory)
    {
        _client = serviceProvider.GetRequiredService<DiscordSocketClient>();

        _client.Log += message =>
        {
            logger.LogInformation(message.ToString());
            // Console.WriteLine(message);
            return Task.CompletedTask;
        };

        _interaction = new InteractionService(_client);
        _config = config.Value;
        _serviceProvider = serviceProvider;
        _logger = logger;

        _client.Ready += ClientOnReady;
        _client.Disconnected += async (ex) => await StopAsync(CancellationToken.None);
        _client.InteractionCreated += ClientOnInteractionCreated;
    }

    private async Task ClientOnReady()
    {
        await _interaction.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);

        // Instead of registering the commands only for the Excel discord, register them for all servers
        await _interaction.RegisterCommandsGloballyAsync(true);
        // await Interaction.RegisterCommandsToGuildAsync(Constants.GuildId);
    }

    private async Task ClientOnInteractionCreated(SocketInteraction interaction)
    {
        var scope = _serviceProvider.CreateScope();
        var context = new SocketInteractionContext(_client, interaction);
        await _interaction.ExecuteCommandAsync(context, scope.ServiceProvider);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _client.LoginAsync(TokenType.Bot, _config.Token);
        await _client.StartAsync();
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Discord bot disconnected. Attempting to re-connect...");
        await _client.StopAsync();

        // Lets attempt to restart this
        _logger.LogInformation("Restarting bot");
        await _client.LoginAsync(TokenType.Bot, _config.Token);
        await _client.StartAsync();

        _logger.LogInformation("Discord bot is re-started.");
        // _lifeTime.StopApplication();
    }
}