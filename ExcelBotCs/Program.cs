using System.Text.Json.Serialization;
using DotNetEnv;
using ExcelBotCs;
using ExcelBotCs.Data;
using ExcelBotCs.Discord;
using ExcelBotCs.Extensions;
using ExcelBotCs.Filters;
using ExcelBotCs.Middleware;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services;
using ExcelBotCs.Services.Import;
using ExcelBotCs.Utilities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;

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


AddService<ImportService>();

builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection("ExcelDatabase"));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

builder.Services.AddDataProtection().SetApplicationName("ExcelBotCs");
builder.Services.Configure<KeyManagementOptions>(opt =>
{
    opt.XmlRepository = new MongoXmlRepository(Utils.GetEnvVar("MONGODB_CONNECTION_STRING", nameof(DatabaseOptions)),
        Utils.GetEnvVar("DATABASE_NAME", nameof(DatabaseOptions)));
});

// Add services to the container.
builder.Services.AddAuthorization();

// configure serialization to omit null values
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// register services
builder.Services.AddScoped<RsaKeyService>();
builder.Services.AddLogging();
// builder.Services.AddHttpLogging(options => { });

// per-request user context helpers
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentMemberAccessor, CurrentMemberAccessor>();

// register the database services
builder.Services.AddDatabaseServices();

// configure the serialization settings to remove sensitive data
builder.Services.AddControllers(options => { options.Filters.Add<RoleRedactionResultFilter>(); })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// Configure the discord authentication
builder.Services.AddAppAuthentication(builder.Configuration);

var app = builder.Build();

app.UseHttpsRedirection();

// Serve static files for the SPA (built by Vite into wwwroot)
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
// Populate the current Member for authenticated requests
app.UseMiddleware<CurrentMemberMiddleware>();
app.UseAuthorization();

// disable static file serving for the public folder for now
// app.UseStaticFiles(new StaticFileOptions
// {
// 	FileProvider = new PhysicalFileProvider(Path.Combine(AppContext.BaseDirectory, "Static")),
// 	RequestPath = "/public"
// });

// For unknown API routes, return 404 instead of serving the SPA
app.MapMethods("/api/{**path}", new[] { "GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS" },
    () => Results.NotFound());

app.MapControllers();

// SPA fallback: route unmatched non-API paths to index.html
app.MapFallbackToFile("/index.html");

app.Run();