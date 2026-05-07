using ExcelBotCs.Models.Config;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Xunit;

namespace ExcelBotCs.TestFramework.Database;

[Collection("MongoDB")]
public abstract class MongoDbTest : IAsyncLifetime
{
    private const string DatabaseName = "TestDatabase";
    private readonly MongoDbFixture _fixture;
    private IMongoClient _mongoClient = null!;

    protected MongoDbTest(MongoDbFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _mongoClient = new MongoClient(_fixture.ConnectionString);

        var databaseOptions = Options.Create(new DatabaseOptions
        {
            ConnectionString = _fixture.ConnectionString,
            DatabaseName = DatabaseName
        });

        InitializeRepository(_mongoClient, databaseOptions);
        await OnAfterInitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await BeforeTearDownAsync();
        await _mongoClient.DropDatabaseAsync(DatabaseName);
    }

    protected abstract void InitializeRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions);

    protected virtual Task OnAfterInitializeAsync() => Task.CompletedTask;

    protected virtual Task BeforeTearDownAsync() => Task.CompletedTask;

    protected static string GenerateRandomDiscordId()
    {
        var random = new Random();
        var discordId = random.NextInt64(100000000000000000, 999999999999999999);
        return discordId.ToString();
    }
}
