using Discord;
using ExcelBotCs.Controllers;
using ExcelBotCs.Models.Database.Events;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.Models.DTO.Events;
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
    private readonly Mock<IDiscordMessageService> _discordMessageServiceMock;
    private readonly Mock<IDiscordMessageCreator> _discordMessageCreatorMock;

    public EventsControllerUnitTests()
    {
        _eventServiceMock = new Mock<IEventService>();
        _currentMemberAccessorMock = new Mock<ICurrentMemberAccessor>();
        _discordMessageServiceMock = new Mock<IDiscordMessageService>();
        _discordMessageCreatorMock = new Mock<IDiscordMessageCreator>();
        var loggerMock = new Mock<ILogger<EventsController>>();
        var iCalServiceMock = new Mock<IICalService>();

        Environment.SetEnvironmentVariable("EVENT_ENDPOINT_URL", "https://test.example.com");

        _controller = new EventsController(
            loggerMock.Object,
            _eventServiceMock.Object,
            _currentMemberAccessorMock.Object,
            _discordMessageServiceMock.Object,
            iCalServiceMock.Object,
            _discordMessageCreatorMock.Object);
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
        badRequest.Value.ShouldBe("At least one signup slug is required");
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
        badRequest.Value.ShouldBe("At least one signup slug is required");
    }

    [Fact]
    public async Task ManualSignup_NewSignup_AddsToSignupsAndCallsUpdate()
    {
        var fcEvent = new Event
        {
            Id = "evt1",
            Signups = new List<EventSignup>(),
            SignupButtonConfigs = new List<SignupButtonConfig>
            {
                new() { MappedRole = Role.Tank, Slug = "tank" },
                new() { MappedRole = Role.Healer, Slug = "healer" }
            }
        };
        
        _eventServiceMock.Setup(x => x.GetAsync("evt1")).ReturnsAsync(fcEvent);
        _eventServiceMock.Setup(x => x.UpdateAsync("evt1", fcEvent)).Returns(Task.CompletedTask);

        var result = await _controller.ManualSignup("evt1",
            new EventSignupDto
            {
                DiscordUserId = "12345",
                Roles = new List<Role> { Role.Tank, Role.Healer },
                SignupSlugs = new List<string>(new[] { "tank", "healer" })
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
                    SignupDate = DateTime.UtcNow,
                    SignupSlugs = new List<string>(new[] { "tank" })
                }
            },
            SignupButtonConfigs = new List<SignupButtonConfig>
            {
                new() { MappedRole = Role.Tank, Slug = "tank" },
                new() { MappedRole = Role.Healer, Slug = "healer" },
                new() { MappedRole = Role.Caster, Slug = "caster" }
            }
        };
        _eventServiceMock.Setup(x => x.GetAsync("evt1")).ReturnsAsync(fcEvent);
        _eventServiceMock.Setup(x => x.UpdateAsync("evt1", fcEvent)).Returns(Task.CompletedTask);

        var result = await _controller.ManualSignup("evt1",
            new EventSignupDto
            {
                DiscordUserId = "12345",
                Roles = new List<Role> { Role.Healer, Role.Caster },
                SignupSlugs = new List<string>(new[] { "healer", "caster" })
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
                    SignupDate = DateTime.UtcNow,
                    SignupSlugs = new List<string>(new[] { "melee" })
                }
            },
            SignupButtonConfigs = new List<SignupButtonConfig>
            {
                new() { MappedRole = Role.Melee, Slug = "melee" },
                new() { MappedRole = Role.Ranged, Slug = "ranged" }
            }
        };
        _eventServiceMock.Setup(x => x.GetAsync("evt1")).ReturnsAsync(fcEvent);
        _eventServiceMock.Setup(x => x.UpdateAsync("evt1", fcEvent)).Returns(Task.CompletedTask);

        var result = await _controller.ManualSignup("evt1",
            new EventSignupDto
            {
                DiscordUserId = "new-user",
                Roles = new List<Role> { Role.Ranged },
                SignupSlugs = new List<string> { "ranged" }
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

    #region SelectParticipants Unit Tests

    [Fact]
    public async Task SelectParticipants_ReturnsNotFound_WhenEventNotFound()
    {
        _eventServiceMock.Setup(x => x.GetAsync("missing")).ReturnsAsync((Event)null!);

        var result = await _controller.SelectParticipants("missing", new List<EventGroupRequest>());

        result.ShouldBeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SelectParticipants_StoresUpcomingRosterMessageId_WhenDiscordReturnsMessage()
    {
        var fcEvent = new Event { Id = "evt1", Groups = new List<EventGroup>() };
        var mockMessage = new Mock<IUserMessage>();
        mockMessage.Setup(m => m.Id).Returns(111222333UL);

        _eventServiceMock.Setup(x => x.GetAsync("evt1")).ReturnsAsync(fcEvent);
        _discordMessageCreatorMock.Setup(x => x.CreateUpcomingRosterMessage(fcEvent))
            .ReturnsAsync("roster text");
        _discordMessageServiceMock.Setup(x => x.PostInUpcomingRosterChannelAsync("roster text"))
            .ReturnsAsync(mockMessage.Object);
        _eventServiceMock.Setup(x => x.UpdateAsync("evt1", fcEvent)).Returns(Task.CompletedTask);

        await _controller.SelectParticipants("evt1", new List<EventGroupRequest>());

        fcEvent.UpcomingRosterMessageId.ShouldBe("111222333");
    }

    [Fact]
    public async Task SelectParticipants_DoesNotSetUpcomingRosterMessageId_WhenDiscordReturnsNull()
    {
        var fcEvent = new Event { Id = "evt1", Groups = new List<EventGroup>() };

        _eventServiceMock.Setup(x => x.GetAsync("evt1")).ReturnsAsync(fcEvent);
        _discordMessageCreatorMock.Setup(x => x.CreateUpcomingRosterMessage(fcEvent))
            .ReturnsAsync("roster text");
        _discordMessageServiceMock.Setup(x => x.PostInUpcomingRosterChannelAsync("roster text"))
            .ReturnsAsync((IUserMessage?)null);
        _eventServiceMock.Setup(x => x.UpdateAsync("evt1", fcEvent)).Returns(Task.CompletedTask);

        await _controller.SelectParticipants("evt1", new List<EventGroupRequest>());

        fcEvent.UpcomingRosterMessageId.ShouldBeNull();
    }

    [Fact]
    public async Task SelectParticipants_CallsUpdateAsync_WithEventIncludingRosterMessageId()
    {
        var fcEvent = new Event { Id = "evt1", Groups = new List<EventGroup>() };
        var mockMessage = new Mock<IUserMessage>();
        mockMessage.Setup(m => m.Id).Returns(555666777UL);
        Event? capturedEvent = null;

        _eventServiceMock.Setup(x => x.GetAsync("evt1")).ReturnsAsync(fcEvent);
        _discordMessageCreatorMock.Setup(x => x.CreateUpcomingRosterMessage(fcEvent))
            .ReturnsAsync("roster text");
        _discordMessageServiceMock.Setup(x => x.PostInUpcomingRosterChannelAsync("roster text"))
            .ReturnsAsync(mockMessage.Object);
        _eventServiceMock.Setup(x => x.UpdateAsync("evt1", It.IsAny<Event>()))
            .Callback<string, Event>((_, e) => capturedEvent = e)
            .Returns(Task.CompletedTask);

        var result = await _controller.SelectParticipants("evt1", new List<EventGroupRequest>());

        result.ShouldBeOfType<OkResult>();
        capturedEvent.ShouldNotBeNull();
        capturedEvent!.UpcomingRosterMessageId.ShouldBe("555666777");
    }

    [Fact]
    public async Task SelectParticipants_ReturnsOk_WhenSuccessful()
    {
        var fcEvent = new Event { Id = "evt1", Groups = new List<EventGroup>() };

        _eventServiceMock.Setup(x => x.GetAsync("evt1")).ReturnsAsync(fcEvent);
        _discordMessageCreatorMock.Setup(x => x.CreateUpcomingRosterMessage(fcEvent))
            .ReturnsAsync("roster text");
        _discordMessageServiceMock.Setup(x => x.PostInUpcomingRosterChannelAsync(It.IsAny<string>()))
            .ReturnsAsync((IUserMessage?)null);
        _eventServiceMock.Setup(x => x.UpdateAsync("evt1", fcEvent)).Returns(Task.CompletedTask);

        var result = await _controller.SelectParticipants("evt1", new List<EventGroupRequest>());

        result.ShouldBeOfType<OkResult>();
    }

    #endregion
}
