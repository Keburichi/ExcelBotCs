using ExcelBotCs.Database;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database;
using ExcelBotCs.TestFramework.Database;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Tests.Database;

[Collection("MongoDB")]
public class BotTaskRepositoryTests : MongoDbTest
{
    private BotTaskRepository _repository = null!;

    public BotTaskRepositoryTests(MongoDbFixture fixture) : base(fixture) { }

    protected override void InitializeRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
    {
        _repository = new BotTaskRepository(mongoClient, databaseOptions);
    }

    [Fact]
    public async Task GetPendingTasksAsync_ReturnsEmpty_WhenNoPendingTasks()
    {
        var result = await _repository.GetPendingTasksAsync();
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPendingTasksAsync_ReturnsPendingTasks()
    {
        await _repository.CreateAsync(new BotTask { TaskType = "Test", Payload = "{}", Status = BotTaskStatus.Pending });
        var result = await _repository.GetPendingTasksAsync();
        result.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetPendingTasksAsync_ExcludesCompletedAndFailed()
    {
        await _repository.CreateAsync(new BotTask { TaskType = "T", Payload = "{}", Status = BotTaskStatus.Completed });
        await _repository.CreateAsync(new BotTask { TaskType = "T", Payload = "{}", Status = BotTaskStatus.Failed });
        var result = await _repository.GetPendingTasksAsync();
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPendingTasksAsync_ExcludesFutureScheduledTasks()
    {
        await _repository.CreateAsync(new BotTask
        {
            TaskType = "T", Payload = "{}", Status = BotTaskStatus.Pending,
            ScheduledAt = DateTime.UtcNow.AddHours(1)
        });
        var result = await _repository.GetPendingTasksAsync();
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task MarkInProgressAsync_UpdatesStatus()
    {
        var task = new BotTask { TaskType = "T", Payload = "{}", Status = BotTaskStatus.Pending };
        await _repository.CreateAsync(task);

        await _repository.MarkInProgressAsync(task.Id);

        var updated = await _repository.GetAsync(task.Id);
        updated!.Status.ShouldBe(BotTaskStatus.InProgress);
    }

    [Fact]
    public async Task MarkCompletedAsync_UpdatesStatusAndSetsExecutedAt()
    {
        var task = new BotTask { TaskType = "T", Payload = "{}", Status = BotTaskStatus.InProgress };
        await _repository.CreateAsync(task);

        await _repository.MarkCompletedAsync(task.Id);

        var updated = await _repository.GetAsync(task.Id);
        updated!.Status.ShouldBe(BotTaskStatus.Completed);
        updated.ExecutedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task MarkFailedAsync_UpdatesStatusAndSetsErrorMessage()
    {
        var task = new BotTask { TaskType = "T", Payload = "{}", Status = BotTaskStatus.InProgress };
        await _repository.CreateAsync(task);

        await _repository.MarkFailedAsync(task.Id, "something broke");

        var updated = await _repository.GetAsync(task.Id);
        updated!.Status.ShouldBe(BotTaskStatus.Failed);
        updated.ErrorMessage.ShouldBe("something broke");
        updated.ExecutedAt.ShouldNotBeNull();
    }
}
