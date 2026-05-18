using Discord;
using ExcelBotCs.Discord;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.Database.Events;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.Services.Discord;
using ExcelBotCs.TestFramework.Extensions;
using ExcelBotCs.TestFramework.TestData;
using Moq;

namespace ExcelBotCs.Tests.Services.Discord;

public class DiscordMessageCreatorTests
{
    private readonly Mock<IDiscordBotClient> _discordClientMock;
    private readonly Mock<IFightService> _fightServiceMock;
    private readonly DiscordMessageCreator _discordMessageCreator;

    public DiscordMessageCreatorTests()
    {
        _discordClientMock = new Mock<IDiscordBotClient>();
        _fightServiceMock = new Mock<IFightService>();
        _discordMessageCreator = new DiscordMessageCreator(_discordClientMock.Object, _fightServiceMock.Object);
    }

    [Fact]
    public async Task CreateSignupComponents_EventIsNull_ThrowsException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _discordMessageCreator.CreateSignupComponents(null));
    }

    [Fact]
    public async Task CreateSignupComponents_ButtonConfigIsNull_ThrowsException()
    {
        var fcEvent = new Event
        {
            Id = string.Empty,
            Name = "Test Event",
            FightId = string.Empty,
            StartDate = DateTime.UtcNow,
            Duration = 60
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _discordMessageCreator.CreateSignupComponents(fcEvent));
    }

    [Theory]
    [MemberData(nameof(NullOrEmptyStringData.Values), MemberType = typeof(NullOrEmptyStringData))]
    public async Task CreateSignupComponents_FightIdIsNullOrEmpty_UsesTypeForHeading(string? fightId)
    {
        var fcEvent = new Event
        {
            Id = string.Empty,
            FightId = fightId,
            StartDate = DateTime.UtcNow,
            Duration = 60,
            SignupButtonConfigs = new List<SignupButtonConfig>().WithRoleButtons(),
            Type = EventType.Unreal
        };

        var componentBuilder = await _discordMessageCreator.CreateSignupComponents(fcEvent);
        var messageComponent = componentBuilder.Build();

        foreach (var component in messageComponent.Components.Where(x => x is TextDisplayComponent))
        {
            if (component is not TextDisplayComponent textDisplayComponent)
                continue;

            if (textDisplayComponent.Content.StartsWith("## "))
                Assert.Equal("## Unreal", textDisplayComponent.Content);
        }
    }

    [Fact]
    public async Task CreateSignupComponents_FightIdExists_UsesFightNameForHeading()
    {
        var fight = new Fight
        {
            Id = "fight-123",
            Name = "Test Fight"
        };

        var fcEvent = new Event
        {
            Id = string.Empty,
            FightId = fight.Id,
            StartDate = DateTime.UtcNow,
            Duration = 60,
            SignupButtonConfigs = new List<SignupButtonConfig>().WithRoleButtons(),
            Type = EventType.Unreal,
            PictureUrl = "https://example.com/image.jpg"
        };

        _fightServiceMock.Setup(x => x.GetFightAsync(fight.Id)).ReturnsAsync(fight);

        var componentBuilder = await _discordMessageCreator.CreateSignupComponents(fcEvent);
        var messageComponent = componentBuilder.Build();

        foreach (var component in messageComponent.Components.Where(x => x is TextDisplayComponent))
        {
            if (component is not TextDisplayComponent textDisplayComponent)
                continue;

            if (textDisplayComponent.Content.StartsWith("## "))
                Assert.Equal("## Unreal - Test Fight", textDisplayComponent.Content);
        }

        _fightServiceMock.Verify(x => x.GetFightAsync(fight.Id), Times.Once());
    }

    [Fact]
    public async Task CreateSignupComponents_FightIdDoesntExist_UsesTypeForHeading()
    {
        var fcEvent = new Event
        {
            Id = string.Empty,
            FightId = "fight-123",
            StartDate = DateTime.UtcNow,
            Duration = 60,
            SignupButtonConfigs = new List<SignupButtonConfig>().WithRoleButtons(),
            Type = EventType.Unreal,
            PictureUrl = "https://example.com/image.jpg"
        };

        _fightServiceMock.Setup(x => x.GetFightAsync(fcEvent.FightId)).ReturnsAsync(() => null);

        var componentBuilder = await _discordMessageCreator.CreateSignupComponents(fcEvent);
        var messageComponent = componentBuilder.Build();

        foreach (var component in messageComponent.Components.Where(x => x is TextDisplayComponent))
        {
            if (component is not TextDisplayComponent textDisplayComponent)
                continue;

            if (textDisplayComponent.Content.StartsWith("## "))
                Assert.Equal("## Unreal", textDisplayComponent.Content);
        }

        _fightServiceMock.Verify(x => x.GetFightAsync(fcEvent.FightId), Times.Once());
    }

    [Theory]
    [MemberData(nameof(NullOrEmptyStringData.Values), MemberType = typeof(NullOrEmptyStringData))]
    public async Task CreateSignupComponents_PictureUrlIsNullOrEmpty_NoMediaGallery(string? pictureUrl)
    {
        var fcEvent = new Event
        {
            Id = string.Empty,
            StartDate = DateTime.UtcNow,
            Duration = 60,
            SignupButtonConfigs = new List<SignupButtonConfig>().WithRoleButtons(),
            Type = EventType.Unreal,
            PictureUrl = pictureUrl
        };

        var componentBuilder = await _discordMessageCreator.CreateSignupComponents(fcEvent);
        var messageComponent = componentBuilder.Build();

        Assert.DoesNotContain(messageComponent.Components, x => x is MediaGalleryComponent);
    }

    [Fact]
    public async Task CreateSignupComponents_PictureUrlIsFilled_MediaGalleryExists()
    {
        var fcEvent = new Event
        {
            Id = string.Empty,
            StartDate = DateTime.UtcNow,
            Duration = 60,
            SignupButtonConfigs = new List<SignupButtonConfig>().WithRoleButtons(),
            Type = EventType.Unreal,
            PictureUrl = "https://example.com/image.jpg"
        };

        var componentBuilder = await _discordMessageCreator.CreateSignupComponents(fcEvent);
        var messageComponent = componentBuilder.Build();

        Assert.Contains(messageComponent.Components, x => x is MediaGalleryComponent);
    }

    [Fact]
    public async Task CreateSignupComponents_SignupsAreOpen_RendersButtons()
    {
        var fcEvent = new Event
        {
            Id = string.Empty,
            StartDate = DateTime.UtcNow,
            Duration = 60,
            SignupButtonConfigs = new List<SignupButtonConfig>().WithRoleButtons(),
            Type = EventType.Unreal,
            PictureUrl = "https://example.com/image.jpg",
            Occurrences = new List<EventOccurrence>
            {
                new() { Id = "1", OccurrenceDate = DateTime.UtcNow, Status = OccurrenceStatus.Scheduled }
            }
        };

        var componentBuilder = await _discordMessageCreator.CreateSignupComponents(fcEvent);
        var messageComponent = componentBuilder.Build();

        Assert.Equal(1, messageComponent.Components.Count(x => x is ActionRowComponent));

        var actionRowComponent = (ActionRowComponent)messageComponent.Components.First(x => x is ActionRowComponent);

        Assert.Equal(5, actionRowComponent.Components.Count(x => x is ButtonComponent));
        foreach (var component in actionRowComponent.Components.Where(x => x is ButtonComponent))
        {
            var buttonComponent = (ButtonComponent)component;
            Assert.Null(buttonComponent.Emote);
        }
    }

    [Fact]
    public async Task CreateSignupComponents_SignupsAreOpen_RendersButtonsWithEmotes()
    {
        var fcEvent = new Event
        {
            Id = string.Empty,
            StartDate = DateTime.UtcNow,
            Duration = 60,
            SignupButtonConfigs = new List<SignupButtonConfig>().WithEmoteRoleButtons(),
            Type = EventType.Unreal,
            PictureUrl = "https://example.com/image.jpg",
            Occurrences = new List<EventOccurrence>
            {
                new() { Id = "1", OccurrenceDate = DateTime.UtcNow, Status = OccurrenceStatus.Scheduled }
            }
        };

        _discordClientMock.Setup(x => x.GetEmoteById(1234567890))
            .Returns(new Emote(1234567890, "test"));

        var componentBuilder = await _discordMessageCreator.CreateSignupComponents(fcEvent);
        var messageComponent = componentBuilder.Build();

        Assert.Equal(1, messageComponent.Components.Count(x => x is ActionRowComponent));

        var actionRowComponent = (ActionRowComponent)messageComponent.Components.First(x => x is ActionRowComponent);

        Assert.Equal(5, actionRowComponent.Components.Count(x => x is ButtonComponent));
        foreach (var component in actionRowComponent.Components.Where(x => x is ButtonComponent))
        {
            var buttonComponent = (ButtonComponent)component;
            Assert.NotNull(buttonComponent.Emote);
        }
    }
}