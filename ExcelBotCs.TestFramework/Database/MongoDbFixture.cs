using Testcontainers.MongoDb;
using Xunit;

namespace ExcelBotCs.TestFramework.Database;

public class MongoDbFixture : IAsyncLifetime
{
    private MongoDbContainer _mongoContainer = null!;

    public string ConnectionString => _mongoContainer.GetConnectionString();

    public async Task InitializeAsync()
    {
        _mongoContainer = new MongoDbBuilder()
            .WithImage("mongo:7.0")
            .Build();

        await _mongoContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_mongoContainer != null)
        {
            await _mongoContainer.StopAsync();
            await _mongoContainer.DisposeAsync();
        }
    }
}
