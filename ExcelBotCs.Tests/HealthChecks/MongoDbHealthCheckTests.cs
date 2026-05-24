using ExcelBotCs.HealthChecks;
using ExcelBotCs.Models.Config;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;

namespace ExcelBotCs.Tests.HealthChecks;

public class MongoDbHealthCheckTests
{
    private readonly Mock<IMongoDatabase> _mockDatabase = new();
    private readonly Mock<IMongoClient> _mockClient = new();
    private readonly MongoDbHealthCheck _check;
    private readonly HealthCheckContext _context;

    public MongoDbHealthCheckTests()
    {
        _mockClient
            .Setup(c => c.GetDatabase(It.IsAny<string>(), It.IsAny<MongoDatabaseSettings>()))
            .Returns(_mockDatabase.Object);

        var options = Options.Create(new DatabaseOptions
        {
            ConnectionString = "mongodb://localhost:27017",
            DatabaseName = "TestDatabase"
        });

        _check = new MongoDbHealthCheck(_mockClient.Object, options);
        _context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("mongodb", _ => _check, null, null)
        };
    }

    [Fact]
    public async Task CheckHealthAsync_WhenPingSucceeds_ReturnsHealthy()
    {
        _mockDatabase
            .Setup(db => db.RunCommandAsync(
                It.IsAny<Command<BsonDocument>>(),
                It.IsAny<ReadPreference>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BsonDocument());

        var result = await _check.CheckHealthAsync(_context);

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenPingFails_ReturnsUnhealthy()
    {
        _mockDatabase
            .Setup(db => db.RunCommandAsync(
                It.IsAny<Command<BsonDocument>>(),
                It.IsAny<ReadPreference>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MongoException("Connection refused"));

        var result = await _check.CheckHealthAsync(_context);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenPingFails_ExceptionIsAttached()
    {
        var exception = new MongoException("Connection refused");
        _mockDatabase
            .Setup(db => db.RunCommandAsync(
                It.IsAny<Command<BsonDocument>>(),
                It.IsAny<ReadPreference>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        var result = await _check.CheckHealthAsync(_context);

        result.Exception.ShouldBe(exception);
    }

    [Fact]
    public async Task CheckHealthAsync_QueriesConfiguredDatabase()
    {
        _mockDatabase
            .Setup(db => db.RunCommandAsync(
                It.IsAny<Command<BsonDocument>>(),
                It.IsAny<ReadPreference>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BsonDocument());

        await _check.CheckHealthAsync(_context);

        _mockClient.Verify(c => c.GetDatabase("TestDatabase", It.IsAny<MongoDatabaseSettings>()), Times.Once);
    }
}