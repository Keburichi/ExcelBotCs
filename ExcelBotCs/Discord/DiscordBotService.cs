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
    private readonly ILogger<DiscordSocketClient> _logger;
    private TaskCompletionSource _disconnectedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private const int MaxConsecutiveFailures = 10;
    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(60);

    public DiscordBotService(IServiceScopeFactory scopeFactory, IOptions<DiscordBotOptions> config,
        IServiceProvider serviceProvider, ILogger<DiscordSocketClient> logger) : base(scopeFactory)
    {
        _client = serviceProvider.GetRequiredService<DiscordSocketClient>();

        _client.Log += message =>
        {
            logger.LogInformation(message.ToString());
            return Task.CompletedTask;
        };

        _interaction = new InteractionService(_client);
        _config = config.Value;
        _serviceProvider = serviceProvider;
        _logger = logger;

        _client.Ready += ClientOnReady;
        _client.Disconnected += OnDisconnected;
        _client.InteractionCreated += ClientOnInteractionCreated;
    }

    private Task OnDisconnected(Exception ex)
    {
        _logger.LogWarning(ex, "Discord client disconnected");
        _disconnectedTcs.TrySetResult();
        return Task.CompletedTask;
    }

    private async Task ClientOnReady()
    {
        // Check if any modules are already registered and remove them before registering them again
        // (this happens during re-connects from the bot)
        if (!_interaction.Modules.IsNullOrEmpty())
            foreach (var interactionModule in _interaction.Modules)
                await _interaction.RemoveModuleAsync(interactionModule);

        await _interaction.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);

        // Instead of registering the commands only for the Excel discord, register them for all servers
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
        var consecutiveFailures = 0;
        var delay = InitialDelay;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _disconnectedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

                await _client.LoginAsync(TokenType.Bot, _config.Token);
                await _client.StartAsync();

                _logger.LogInformation("Discord bot started successfully");
                consecutiveFailures = 0;
                delay = InitialDelay;

                // Wait until either disconnected or cancellation requested
                await Task.WhenAny(
                    _disconnectedTcs.Task,
                    Task.Delay(Timeout.Infinite, stoppingToken)
                );

                if (stoppingToken.IsCancellationRequested)
                    break;

                _logger.LogWarning("Discord bot disconnected, will retry in {Delay}", delay);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                consecutiveFailures++;
                if (consecutiveFailures >= MaxConsecutiveFailures)
                {
                    _logger.LogCritical(ex, "Discord bot failed {Count} consecutive times, giving up",
                        consecutiveFailures);
                    break;
                }

                _logger.LogError(ex, "Discord bot failed to connect (attempt {Count}/{Max}), retrying in {Delay}",
                    consecutiveFailures, MaxConsecutiveFailures, delay);
            }

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            delay = TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, MaxDelay.Ticks));
        }

        _logger.LogInformation("Discord bot ExecuteAsync loop exited");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Discord bot service stopping");
        _disconnectedTcs.TrySetResult();
        await _client.StopAsync();
        await base.StopAsync(cancellationToken);
    }
}