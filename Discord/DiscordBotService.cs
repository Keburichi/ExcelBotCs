using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ExcelBotCs.Commands;
using System.Reflection;

namespace ExcelBotCs.Discord;

public class DiscordBotService : BackgroundService
{
	public DiscordSocketClient Client { get; }
	public InteractionService Interaction { get; }
	private readonly DiscordBotOptions _config;
	private readonly IHostApplicationLifetime _lifeTime;
	private readonly IServiceProvider _serviceProvider;

	public DiscordBotService(IServiceScopeFactory scopeFactory, DiscordBotOptions config, IHostApplicationLifetime lifeTime, IServiceProvider serviceProvider) : base(scopeFactory)
	{
		Client = new DiscordSocketClient(new DiscordSocketConfig()
		{
			GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent | GatewayIntents.GuildMembers | GatewayIntents.GuildPresences
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
		Client.Disconnected += async (ex) => await StopAsync(CancellationToken.None);
		Client.InteractionCreated += ClientOnInteractionCreated;
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

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		await Client.LoginAsync(TokenType.Bot, _config.Token);
		await Client.StartAsync();
	}

	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		await Client.StopAsync();
		_lifeTime.StopApplication();
	}
}