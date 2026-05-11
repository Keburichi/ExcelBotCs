using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.Tasks;
using ExcelBotCs.Modules.TeamFormation;
using ExcelBotCs.Services.API;
using ExcelBotCs.Services.API.Interfaces;
using Moq;

namespace ExcelBotCs.Tests.Services.API;

public class BotTaskServiceTests
{
    private readonly IBotTaskService _botTaskService;
    private readonly Mock<IBotTaskRepository> _repositoryMock;

    public BotTaskServiceTests()
    {
        _repositoryMock = new Mock<IBotTaskRepository>();
        _botTaskService = new BotTaskService(_repositoryMock.Object);
    }

    [Fact]
    public async Task EnqueueAsync_CreatesTaskWithCorrectTypeAndSerializedPayload()
    {
        BotTask? captured = null;
        _repositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<BotTask>()))
            .Callback<BotTask>(t => captured = t)
            .Returns(Task.CompletedTask);

        var payload = new PostEventSignupPayload { EventId = "abc123", Role = Role.Tank, DiscordUserId = 999UL };

        await _botTaskService.EnqueueAsync(BotTaskTypes.PostEventSignup, payload);

        captured.ShouldNotBeNull();
        captured!.TaskType.ShouldBe(BotTaskTypes.PostEventSignup);
        captured.Status.ShouldBe(BotTaskStatus.Pending);
        captured.Payload.ShouldContain("abc123");
        captured.ScheduledAt.ShouldBeNull();
    }

    [Fact]
    public async Task EnqueueAsync_SetsScheduledAt_WhenProvided()
    {
        BotTask? captured = null;
        _repositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<BotTask>()))
            .Callback<BotTask>(t => captured = t)
            .Returns(Task.CompletedTask);

        var scheduledAt = DateTime.UtcNow.AddHours(2);

        await _botTaskService.EnqueueAsync(BotTaskTypes.PostDiscordMessage,
            new PostDiscordMessagePayload { ChannelId = 1UL, Message = "hello" },
            scheduledAt);

        captured!.ScheduledAt.ShouldBe(scheduledAt);
    }
}
