using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database;
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

namespace ExcelBotCs.Tests.Utils;

/// <summary>
///     Base class for API integration tests. It spins up a MongoDB test container via MongoDbTest
///     and wires the application to use that database, disables hosted background services,
///     installs a lightweight test authentication handler, and configures DataProtection keys
///     to be stored in-memory (avoiding any external Mongo connections during tests).
/// </summary>
public abstract class IntegrationTestBase : MongoDbTest
{
    private TestAuthHandlerOptions _testAuthOptions = null!;
    private string _testConnectionString = null!;

    private IMongoClient _testMongoClient = null!;
    protected HttpClient Client = null!;
    protected WebApplicationFactory<Program> Factory = null!;

    protected override void InitializeRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
    {
        _testMongoClient = mongoClient;
        _testConnectionString = databaseOptions.Value.ConnectionString;
    }

    [SetUp]
    public new void SetUp()
    {
        base.SetUp();

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
                    // Remove any hosted services to avoid background work in tests
                    var hostedServices = services.Where(d => d.ServiceType == typeof(IHostedService)).ToList();
                    foreach (var svc in hostedServices) services.Remove(svc);

                    // Ensure the app uses the test Mongo client
                    services.RemoveAll<IMongoClient>();
                    services.AddSingleton(_testMongoClient);

                    // Override DatabaseOptions so anything built from it uses our test DB
                    services.RemoveAll<IOptions<DatabaseOptions>>();
                    services.RemoveAll<IOptionsSnapshot<DatabaseOptions>>();
                    services.RemoveAll<IOptionsMonitor<DatabaseOptions>>();
                    services.AddSingleton(
                        Options.Create(new DatabaseOptions
                        {
                            ConnectionString = _testConnectionString,
                            DatabaseName = "TestDatabase"
                        }));

                    // Replace DataProtection with an ephemeral provider to avoid Mongo-based key repository
                    services.RemoveAll<IDataProtectionProvider>();
                    services.AddSingleton<IDataProtectionProvider, EphemeralDataProtectionProvider>();

                    // Remove JwtBearer options configuration to ensure no DB access via RSA service in tests
                    services.RemoveAll<IConfigureOptions<JwtBearerOptions>>();
                    services.RemoveAll<IPostConfigureOptions<JwtBearerOptions>>();

                    // Create a shared TestAuthHandlerOptions instance that can be configured per-test
                    _testAuthOptions = new TestAuthHandlerOptions();

                    // Override Cookie authentication with our configurable test handler
                    // This way controllers using Cookie auth will accept our test authentication
                    services.PostConfigure<AuthenticationOptions>(o =>
                    {
                        // Replace the Cookie scheme handler with our test handler
                        var cookieScheme = o.Schemes.FirstOrDefault(s =>
                            s.Name == CookieAuthenticationDefaults.AuthenticationScheme);
                        if (cookieScheme != null) cookieScheme.HandlerType = typeof(TestAuthHandler);
                    });

                    // Register the shared options instance
                    services.AddSingleton<IOptionsMonitor<TestAuthHandlerOptions>>(
                        new TestAuthOptionsMonitor(_testAuthOptions));
                });
            });

        Client = Factory.CreateClient();

        // By default, set up an authenticated admin user for backward compatibility
        // Tests can override this using SetAuthenticatedUser, SetUnauthenticated, etc.
        SetAuthenticatedUser(GenerateRandomDiscordId(), "Default Test User");
    }

    [TearDown]
    public new async Task TearDown()
    {
        Client?.Dispose();
        if (Factory is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
        else
            Factory?.Dispose();

        await base.TearDown();
    }

    /// <summary>
    ///     Configures the test client to authenticate as a user with the given Discord ID.
    ///     The corresponding Member must exist in the database with appropriate permissions.
    /// </summary>
    protected void SetAuthenticatedUser(string discordId, string userName = "Test User")
    {
        _testAuthOptions.TestUser = TestUser.Create(discordId, userName);
    }

    /// <summary>
    ///     Configures the test client to make unauthenticated requests (no user logged in).
    /// </summary>
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

    /// <summary>
    ///     Creates a Member in the database and configures the test client to authenticate as that member.
    ///     Returns the created Member for further test assertions.
    /// </summary>
    protected async Task<Member> CreateAndAuthenticateAsMember(
        string discordId,
        bool isAdmin = false,
        bool isMember = true,
        bool isDeveloper = false,
        string name = "Test Member")
    {
        // First, create a MemberRole document in the database
        var memberRole = new MemberRole
        {
            DiscordId = GenerateRandomDiscordId(),
            Name = $"Test Role for {name}",
            IsAdmin = isAdmin,
            IsMember = isMember,
            IsDeveloper = isDeveloper
        };

        // Get the MemberRole repository from services
        var memberRoleRepository = Factory.Services.GetRequiredService<IMemberRoleRepository>();
        await memberRoleRepository.CreateAsync(memberRole);

        // MongoDB automatically populates the Id after CreateAsync
        if (string.IsNullOrEmpty(memberRole.Id))
            throw new InvalidOperationException("MemberRole.Id was not populated after CreateAsync");

        // RoleIds contains MongoDB ObjectIds that reference MemberRole documents
        var member = new Member
        {
            DiscordId = discordId,
            DiscordName = name,
            PlayerName = name,
            RoleIds = new List<string> { memberRole.Id }, // Reference by MongoDB ObjectId
            ExperienceIds = new List<string>()
        };

        // Insert the member into the test database
        var memberRepository = Factory.Services.GetRequiredService<IMemberRepository>();
        await memberRepository.CreateAsync(member);

        // Configure authentication to use this member
        SetAuthenticatedUser(discordId, name);

        return member;
    }
}

/// <summary>
///     Simple IOptionsMonitor implementation that wraps a shared TestAuthHandlerOptions instance.
///     This allows tests to modify authentication options that are picked up by the auth handler.
/// </summary>
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