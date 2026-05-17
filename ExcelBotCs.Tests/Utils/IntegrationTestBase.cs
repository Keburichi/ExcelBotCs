using Discord;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Services.Discord.Interfaces;
using ExcelBotCs.TestFramework.Database;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;

namespace ExcelBotCs.Tests.Utils;

public abstract class IntegrationTestBase : MongoDbTest
{
    private TestAuthHandlerOptions _testAuthOptions = null!;
    private string _testConnectionString = null!;

    private IMongoClient _testMongoClient = null!;
    protected HttpClient Client = null!;
    protected WebApplicationFactory<Program> Factory = null!;

    protected IntegrationTestBase(MongoDbFixture fixture) : base(fixture)
    {
    }

    protected override void InitializeRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
    {
        _testMongoClient = mongoClient;
        _testConnectionString = databaseOptions.Value.ConnectionString;
    }

    protected override async Task OnAfterInitializeAsync()
    {
        Environment.SetEnvironmentVariable("EVENT_ENDPOINT_URL", "https://test.example.com/events");

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                builder.ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Warning);
                });

                builder.ConfigureTestServices(services =>
                {
                    var hostedServices = services.Where(d => d.ServiceType == typeof(IHostedService)).ToList();
                    foreach (var svc in hostedServices) services.Remove(svc);

                    services.RemoveAll<IMongoClient>();
                    services.AddSingleton(_testMongoClient);

                    services.RemoveAll<IOptions<DatabaseOptions>>();
                    services.RemoveAll<IOptionsSnapshot<DatabaseOptions>>();
                    services.RemoveAll<IOptionsMonitor<DatabaseOptions>>();
                    services.AddSingleton(
                        Options.Create(new DatabaseOptions
                        {
                            ConnectionString = _testConnectionString,
                            DatabaseName = "TestDatabase"
                        }));

                    services.RemoveAll<IDataProtectionProvider>();
                    services.AddSingleton<IDataProtectionProvider, EphemeralDataProtectionProvider>();

                    services.RemoveAll<IConfigureOptions<JwtBearerOptions>>();
                    services.RemoveAll<IPostConfigureOptions<JwtBearerOptions>>();

                    _testAuthOptions = new TestAuthHandlerOptions();

                    services.PostConfigure<AuthenticationOptions>(o =>
                    {
                        var cookieScheme = o.Schemes.FirstOrDefault(s =>
                            s.Name == CookieAuthenticationDefaults.AuthenticationScheme);
                        if (cookieScheme != null) cookieScheme.HandlerType = typeof(TestAuthHandler);
                    });

                    services.AddSingleton<IOptionsMonitor<TestAuthHandlerOptions>>(
                        new TestAuthOptionsMonitor(_testAuthOptions));

                    services.RemoveAll<IDiscordMessageService>();
                    var mockDiscordMessageService = new Mock<IDiscordMessageService>();
                    mockDiscordMessageService.Setup(x => x.PostInLotteryChannelAsync(It.IsAny<string>()))
                        .Returns(Task.CompletedTask);
                    mockDiscordMessageService.Setup(x => x.PostInAnnouncementChannelAsync(It.IsAny<string>()))
                        .Returns(Task.CompletedTask);
                    mockDiscordMessageService.Setup(x => x.PostInEventChannelAsync(It.IsAny<string>()))
                        .Returns(Task.CompletedTask);
                    mockDiscordMessageService.Setup(x => x.PostInUpcomingRosterChannelAsync(It.IsAny<string>()))
                        .ReturnsAsync((IUserMessage?)null);
                    mockDiscordMessageService.Setup(x => x.DeleteUpcomingRosterMessageAsync(It.IsAny<string>()))
                        .Returns(Task.CompletedTask);
                    mockDiscordMessageService.Setup(x => x.GetAnnouncementChannelMessagesAsync())
                        .ReturnsAsync(new List<IMessage>());
                    services.AddSingleton(mockDiscordMessageService.Object);
                });
            });

        Client = Factory.CreateClient();
        SetAuthenticatedUser(GenerateRandomDiscordId(), "Default Test User");

        await OnAfterIntegrationSetupAsync();
    }

    protected override async Task BeforeTearDownAsync()
    {
        Client?.Dispose();
        if (Factory != null)
            await Factory.DisposeAsync();
        Environment.SetEnvironmentVariable("EVENT_ENDPOINT_URL", null);
    }

    protected virtual Task OnAfterIntegrationSetupAsync() => Task.CompletedTask;

    protected void SetAuthenticatedUser(string discordId, string userName = "Test User")
    {
        _testAuthOptions.TestUser = TestUser.Create(discordId, userName);
    }

    protected void SetUnauthenticated()
    {
        _testAuthOptions.TestUser = null;
    }

    protected async Task AuthenticateAsAdmin()
    {
        var randomDiscordId = GenerateRandomDiscordId();
        await CreateAndAuthenticateAsMember(randomDiscordId, true);
    }

    protected async Task AuthenticateAsMember()
    {
        var randomDiscordId = GenerateRandomDiscordId();
        await CreateAndAuthenticateAsMember(randomDiscordId);
    }

    protected async Task AuthenticateAsDeveloper()
    {
        var randomDiscordId = GenerateRandomDiscordId();
        await CreateAndAuthenticateAsMember(randomDiscordId, false, true, true);
    }

    protected async Task<Member> CreateAndAuthenticateAsMember(
        string discordId,
        bool isAdmin = false,
        bool isMember = true,
        bool isDeveloper = false,
        string name = "Test Member")
    {
        var memberRole = new MemberRole
        {
            DiscordId = GenerateRandomDiscordId(),
            Name = $"Test Role for {name}",
            IsAdmin = isAdmin,
            IsMember = isMember,
            IsDeveloper = isDeveloper
        };

        var memberRoleRepository = Factory.Services.GetRequiredService<IMemberRoleRepository>();
        await memberRoleRepository.CreateAsync(memberRole);

        if (string.IsNullOrEmpty(memberRole.Id))
            throw new InvalidOperationException("MemberRole.Id was not populated after CreateAsync");

        var member = new Member
        {
            DiscordId = discordId,
            DiscordName = name,
            PlayerName = name,
            RoleIds = new List<string> { memberRole.Id },
            ExperienceIds = new List<string>()
        };

        var memberRepository = Factory.Services.GetRequiredService<IMemberRepository>();
        await memberRepository.CreateAsync(member);

        SetAuthenticatedUser(discordId, name);

        return member;
    }
}

internal class TestAuthOptionsMonitor : IOptionsMonitor<TestAuthHandlerOptions>
{
    public TestAuthOptionsMonitor(TestAuthHandlerOptions options)
    {
        CurrentValue = options;
    }

    public TestAuthHandlerOptions CurrentValue { get; }

    public TestAuthHandlerOptions Get(string? name)
    {
        return CurrentValue;
    }

    public IDisposable? OnChange(Action<TestAuthHandlerOptions, string?> listener)
    {
        return null;
    }
}
