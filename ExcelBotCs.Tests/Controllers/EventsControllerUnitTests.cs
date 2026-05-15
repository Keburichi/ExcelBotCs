using ExcelBotCs.Controllers;
using ExcelBotCs.Models.Database.Events;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.Modules.TeamFormation;
using ExcelBotCs.Services;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.Services.Discord.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace ExcelBotCs.Tests.Controllers;

public class EventsControllerUnitTests
{
    private readonly EventsController _controller;
    private readonly Mock<IEventService> _eventServiceMock;
    private readonly Mock<ICurrentMemberAccessor> _currentMemberAccessorMock;

    public EventsControllerUnitTests()
    {
        _eventServiceMock = new Mock<IEventService>();
        _currentMemberAccessorMock = new Mock<ICurrentMemberAccessor>();
        var loggerMock = new Mock<ILogger<EventsController>>();
        var discordMessageServiceMock = new Mock<IDiscordMessageService>();
        var iCalServiceMock = new Mock<IICalService>();
        var discordMessageCreatorMock = new Mock<IDiscordMessageCreator>();

        Environment.SetEnvironmentVariable("EVENT_ENDPOINT_URL", "https://test.example.com");

        _controller = new EventsController(
            loggerMock.Object,
            _eventServiceMock.Object,
            _currentMemberAccessorMock.Object,
            discordMessageServiceMock.Object,
            iCalServiceMock.Object,
            discordMessageCreatorMock.Object);
    }

    #region ManualSignup Unit Tests

    [Fact]
    public async Task ManualSignup_EventNotFound_ReturnsNotFound()
    {
        _eventServiceMock.Setup(x => x.GetAsync("nonexistent"))
            .ReturnsAsync((Event)null!);

        var result = await _controller.ManualSignup("nonexistent",
            new EventSignupDto
            {
                DiscordUserId = "12345",
                Roles = new List<Role> { Role.Tank }
            });

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task ManualSignup_NullDiscordUserId_ReturnsBadRequest()
    {
        var fcEvent = new Event { Signups = new List<EventSignup>() };
        _eventServiceMock.Setup(x => x.GetAsync("evt1")).ReturnsAsync(fcEvent);

        var result = await _controller.ManualSignup("evt1",
            new EventSignupDto
            {
                DiscordUserId = null,
                Roles = new List<Role> { Role.Tank }
            });

        var badRequest = result.ShouldBeOfType<BadRequestObjectResult>();
        badRequest.Value.ShouldBe("DiscordUserId is required");
    }

    [Fact]
    public async Task ManualSignup_EmptyDiscordUserId_ReturnsBadRequest()
    {
        var fcEvent = new Event { Signups = new List<EventSignup>() };
        _eventServiceMock.Setup(x => x.GetAsync("evt1")).ReturnsAsync(fcEvent);

        var result = await _controller.ManualSignup("evt1",
            new EventSignupDto
            {
                DiscordUserId = "   ",
                Roles = new List<Role> { Role.Tank }
            });

        var badRequest = result.ShouldBeOfType<BadRequestObjectResult>();
        badRequest.Value.ShouldBe("DiscordUserId is required");
    }

    [Fact]
    public async Task ManualSignup_EmptyRoles_ReturnsBadRequest()
    {
        var fcEvent = new Event { Signups = new List<EventSignup>() };
        _eventServiceMock.Setup(x => x.GetAsync("evt1")).ReturnsAsync(fcEvent);

        var result = await _controller.ManualSignup("evt1",
            new EventSignupDto
            {
                DiscordUserId = "12345",
                Roles = new List<Role>()
            });

        var badRequest = result.ShouldBeOfType<BadRequestObjectResult>();
        badRequest.Value.ShouldBe("At least one role is required");
    }

    [Fact]
    public async Task ManualSignup_NullRoles_ReturnsBadRequest()
    {
        var fcEvent = new Event { Signups = new List<EventSignup>() };
        _eventServiceMock.Setup(x => x.GetAsync("evt1")).ReturnsAsync(fcEvent);

        var result = await _controller.ManualSignup("evt1",
            new EventSignupDto
            {
                DiscordUserId = "12345",
                Roles = null!
            });

        var badRequest = result.ShouldBeOfType<BadRequestObjectResult>();
        badRequest.Value.ShouldBe("At least one role is required");
    }

    [Fact]
    public async Task ManualSignup_NewSignup_AddsToSignupsAndCallsUpdate()
    {
        var fcEvent = new Event { Id = "evt1", Signups = new List<EventSignup>() };
        _eventServiceMock.Setup(x => x.GetAsync("evt1")).ReturnsAsync(fcEvent);
        _eventServiceMock.Setup(x => x.UpdateAsync("evt1", fcEvent)).Returns(Task.CompletedTask);

        var result = await _controller.ManualSignup("evt1",
            new EventSignupDto
            {
                DiscordUserId = "12345",
                Roles = new List<Role> { Role.Tank, Role.Healer }
            });

        result.ShouldBeOfType<OkResult>();
        fcEvent.Signups.Count.ShouldBe(1);
        fcEvent.Signups[0].DiscordUserId.ShouldBe("12345");
        fcEvent.Signups[0].Roles.ShouldContain(Role.Tank);
        fcEvent.Signups[0].Roles.ShouldContain(Role.Healer);
        _eventServiceMock.Verify(x => x.UpdateAsync("evt1", fcEvent), Times.Once);
    }

    [Fact]
    public async Task ManualSignup_ExistingSignup_UpdatesRolesInPlace()
    {
        var fcEvent = new Event
        {
            Id = "evt1",
            Signups = new List<EventSignup>
            {
                new()
                {
                    DiscordUserId = "12345",
                    Roles = new List<Role> { Role.Tank },
                    SignupDate = DateTime.UtcNow
                }
            }
        };
        _eventServiceMock.Setup(x => x.GetAsync("evt1")).ReturnsAsync(fcEvent);
        _eventServiceMock.Setup(x => x.UpdateAsync("evt1", fcEvent)).Returns(Task.CompletedTask);

        var result = await _controller.ManualSignup("evt1",
            new EventSignupDto
            {
                DiscordUserId = "12345",
                Roles = new List<Role> { Role.Healer, Role.Caster }
            });

        result.ShouldBeOfType<OkResult>();
        fcEvent.Signups.Count.ShouldBe(1);
        fcEvent.Signups[0].Roles.Count.ShouldBe(2);
        fcEvent.Signups[0].Roles.ShouldContain(Role.Healer);
        fcEvent.Signups[0].Roles.ShouldContain(Role.Caster);
        fcEvent.Signups[0].Roles.ShouldNotContain(Role.Tank);
    }

    [Fact]
    public async Task ManualSignup_DoesNotModifyOtherSignups()
    {
        var fcEvent = new Event
        {
            Id = "evt1",
            Signups = new List<EventSignup>
            {
                new()
                {
                    DiscordUserId = "existing-user",
                    Roles = new List<Role> { Role.Melee },
                    SignupDate = DateTime.UtcNow
                }
            }
        };
        _eventServiceMock.Setup(x => x.GetAsync("evt1")).ReturnsAsync(fcEvent);
        _eventServiceMock.Setup(x => x.UpdateAsync("evt1", fcEvent)).Returns(Task.CompletedTask);

        var result = await _controller.ManualSignup("evt1",
            new EventSignupDto
            {
                DiscordUserId = "new-user",
                Roles = new List<Role> { Role.Ranged }
            });

        result.ShouldBeOfType<OkResult>();
        fcEvent.Signups.Count.ShouldBe(2);

        var existingSignup = fcEvent.Signups.First(s => s.DiscordUserId == "existing-user");
        existingSignup.Roles.Count.ShouldBe(1);
        existingSignup.Roles.ShouldContain(Role.Melee);

        var newSignup = fcEvent.Signups.First(s => s.DiscordUserId == "new-user");
        newSignup.Roles.Count.ShouldBe(1);
        newSignup.Roles.ShouldContain(Role.Ranged);
    }

    #endregion
}
