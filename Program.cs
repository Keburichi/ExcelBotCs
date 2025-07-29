using DotNetEnv;
using ExcelBotCs;
using ExcelBotCs.Data;
using ExcelBotCs.Discord;
using ExcelBotCs.Modules.Lottery;
using Microsoft.Extensions.FileProviders;

Env.Load();

var builder = WebApplication.CreateBuilder(args);
void AddHostedService<T>() where T : class, IHostedService
{
	builder.Services.AddHostedService<T>().AddSingleton(x => x.GetServices<IHostedService>().OfType<T>().First());
}
void AddInstance<T>(T instance, bool activate = true) where T : class
{
	var service = builder.Services.AddSingleton(instance);
	if (activate) service.ActivateSingleton<T>();
}

void AddConfig<T>(string key) where T : class
{
	AddInstance(Utils.GetEnvConfig<T>(key, nameof(T)));
}

void AddService<T>(bool activate = true) where T : class
{
	var service = builder.Services.AddSingleton<T>();
	if (activate) service.ActivateSingleton<T>();
}

AddHostedService<DiscordBotService>();
AddConfig<DiscordBotOptions>("DISCORD_CONFIG");
AddInstance(new DatabaseOptions
{
	ConnectionString = Utils.GetEnvVar("MONGODB_CONNECTION_STRING", nameof(DatabaseOptions)),
	DatabaseName = Utils.GetEnvVar("DATABASE_NAME", nameof(DatabaseOptions)),
});
AddInstance(new LotteryOptions
{
	Channel = ulong.Parse(Utils.GetEnvVar("LOTTERY_CHANNEL", nameof(LotteryOptions)))
});
AddService<Database>();
AddService<DiscordLogger>();
AddInstance(new Prng());

var app = builder.Build();

app.UseStaticFiles(new StaticFileOptions
{
	FileProvider = new PhysicalFileProvider(Path.Combine(AppContext.BaseDirectory, "Static")),
	RequestPath = "/public"
});

app.MapGet("/", () => "Hello World!");
app.Run();