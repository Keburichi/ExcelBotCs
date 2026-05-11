using System.Text.Json;
using ExcelBotCs.Models.Tasks;
using ExcelBotCs.Modules.TeamFormation;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.Services.Tasks.Handlers;
using Moq;

namespace ExcelBotCs.Tests.Services.Tasks;

public class PostEventSignupHandlerTests
{
    private readonly Mock<IEventService> _eventServiceMock;
    private readonly PostEventSignupHandler _handler;

    public PostEventSignupHandlerTests()
    {
        _eventServiceMock = new Mock<IEventService>();
        _handler = new PostEventSignupHandler(_eventServiceMock.Object);
    }

    [Fact]
    public void TaskType_IsPostEventSignup()
    {
        _handler.TaskType.ShouldBe(BotTaskTypes.PostEventSignup);
    }

    [Fact]
    public async Task ExecuteAsync_CallsHandleSignupAsync_WithDeserializedPayload()
    {
        var payload = new PostEventSignupPayload
        {
            EventId = "event123",
            Role = Role.Healer,
            DiscordUserId = 42UL
        };
        _eventServiceMock
            .Setup(x => x.HandleSignupAsync(payload.EventId, payload.Role, payload.DiscordUserId))
            .Returns(Task.CompletedTask);

        await _handler.ExecuteAsync(JsonSerializer.Serialize(payload));

        _eventServiceMock.Verify(
            x => x.HandleSignupAsync("event123", Role.Healer, 42UL),
            Times.Once());
    }
}
