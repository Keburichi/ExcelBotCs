using ExcelBotCs.Models.Config;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NUnit.Framework;
using Testcontainers.MongoDb;

namespace ExcelBotCs.TestFramework.Database;

public abstract class MongoDbTest
{
    private const string DatabaseName = "TestDatabase";
    private IMongoClient _mongoClient = null!;
    private MongoDbContainer _mongoContainer = null!;

    [OneTimeSetUp]
    public async Task OnceTimeSetup()
    {
        _mongoContainer = new MongoDbBuilder()
            .WithImage("mongo:7.0")
            .Build();

        await _mongoContainer.StartAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        // Stop and dispose container
        if (_mongoContainer != null)
        {
            await _mongoContainer.StopAsync();
            await _mongoContainer.DisposeAsync();
        }
    }

    [SetUp]
    public void SetUp()
    {
        // Create new client and repository for each test
        _mongoClient = new MongoClient(_mongoContainer.GetConnectionString());

        var databaseOptions = Options.Create(new DatabaseOptions
        {
            ConnectionString = _mongoContainer.GetConnectionString(),
            DatabaseName = DatabaseName
        });

        InitializeRepository(_mongoClient, databaseOptions);
    }

    protected abstract void InitializeRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions);

    [TearDown]
    public async Task TearDown()
    {
        // Clean up database after each test
        await _mongoClient.DropDatabaseAsync(DatabaseName);
    }

    [Test]
    public void InitializationTest()
    {
        Assert.Pass("Repository initialized successfully.");
    }

    /// <summary>
    ///     Generates a random Discord ID (18-digit snowflake format)
    /// </summary>
    protected static string GenerateRandomDiscordId()
    {
        // Discord IDs are 18-digit snowflakes
        // Generate a random 18-digit number as a string
        var random = new Random();
        var discordId = random.NextInt64(100000000000000000, 999999999999999999);
        return discordId.ToString();
    }
}