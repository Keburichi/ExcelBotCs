using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Reflection;

namespace ExcelBotCs.Discord;

public class DiscordBotService : BackgroundService
{
	public DiscordSocketClient Client { get; }
	public InteractionService Interaction { get; }
	private readonly DiscordBotOptions _config;
	private readonly IHostApplicationLifetime _lifeTime;
	private readonly IServiceProvider _serviceProvider;

	public DiscordBotService(IServiceScopeFactory scopeFactory, DiscordBotOptions config,
		IHostApplicationLifetime lifeTime, IServiceProvider serviceProvider) : base(scopeFactory)
	{
		Client = new DiscordSocketClient(new DiscordSocketConfig()
		{
			GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent |
							 GatewayIntents.GuildMembers | GatewayIntents.GuildPresences,
			AlwaysDownloadUsers = true,
			MessageCacheSize = 200
		});
		Client.Log += message =>
		{
			Console.WriteLine(message);
			return Task.CompletedTask;
		};

		Interaction = new InteractionService(Client);
		_config = config;
		_lifeTime = lifeTime;
		_serviceProvider = serviceProvider;

		Client.Ready += ClientOnReady;
		Client.Disconnected += async (ex) => await ReconnectAsync();
		Client.InteractionCreated += ClientOnInteractionCreated;

		KeepAlive();
	}

	private async Task ClientOnReady()
	{
		await Interaction.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);
		await Interaction.RegisterCommandsToGuildAsync(Constants.GuildId);
	}

	private async Task ClientOnInteractionCreated(SocketInteraction interaction)
	{
		var scope = _serviceProvider.CreateScope();
		var context = new SocketInteractionContext(Client, interaction);
		await Interaction.ExecuteCommandAsync(context, scope.ServiceProvider);
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken) => await LoginAsync();

	private async Task LoginAsync() => await Client.LoginAsync(TokenType.Bot, _config.Token);

	private async Task ReconnectAsync()
	{
		await Client.StopAsync();
		await LoginAsync();
		await Client.StartAsync();
	}

	private void KeepAlive() => _ = Task.Run(async () =>
	{
		while (true)
		{
			if (Client.ConnectionState is ConnectionState.Disconnected or ConnectionState.Disconnecting)
			{
				await ReconnectAsync();
			}

			await Task.Delay(20000);
		}
	});
}