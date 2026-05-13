using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.Database.Events;
using ExcelBotCs.Modules.TeamFormation;
using ExcelBotCs.Services;
using ExcelBotCs.Services.API;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.Services.Discord.Interfaces;
using ExcelBotCs.TestFramework.Utils;
using Ical.Net;
using Moq;

namespace ExcelBotCs.Tests.Services.API;

public class EventServiceTests
{
    private readonly IEventService _eventService;
    private readonly Mock<IEventRepository> _eventRepositoryMock;
    private readonly Mock<IDiscordMessageService> _discordMessageServiceMock;
    private readonly IICalService _iCalService;

    public EventServiceTests()
    {
        _eventRepositoryMock = new Mock<IEventRepository>();
        _discordMessageServiceMock = new Mock<IDiscordMessageService>();
        _iCalService = new ICalService();
        _eventService = new EventService(_eventRepositoryMock.Object, _iCalService,
            _discordMessageServiceMock.Object);
    }

    [Fact]
    public async Task GetAsync_ReturnsEmptyList_WhenRepositoryReturnsNull()
    {
        // Arrange
        _eventRepositoryMock.Setup(x => x.GetAsync()).ReturnsAsync((List<Event>)null);

        // Act
        var result = await _eventService.GetAsync();

        // Assert
        result.ShouldBeEmpty();

        _eventRepositoryMock.Verify(x => x.GetAsync(), Times.Once());
    }

    [Fact]
    public async Task GetAsync_ReturnsList_OrderedByStartDate()
    {
        // Arrange
        var events = new List<Event>
        {
            new Event { StartDate = DateTime.UtcNow.AddDays(5) }.PopulateWithRandomData(),
            new Event { StartDate = DateTime.UtcNow.AddDays(1) }.PopulateWithRandomData(),
            new Event { StartDate = DateTime.UtcNow.AddDays(3) }.PopulateWithRandomData()
        };

        events.ForEach(x => x.IsArchived = false);

        _eventRepositoryMock.Setup(x => x.GetAsync()).ReturnsAsync(events);

        // Act
        var result = await _eventService.GetAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(3);
        result[0].StartDate.ShouldBeLessThan(result[1].StartDate);
        result[1].StartDate.ShouldBeLessThan(result[2].StartDate);

        _eventRepositoryMock.Verify(x => x.GetAsync(), Times.Once());
    }

    [Fact]
    public async Task GetAsync_ById_ReturnsNull()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        _eventRepositoryMock.Setup(x => x.GetAsync(id)).ReturnsAsync((Event)null);

        // Act
        var result = await _eventService.GetAsync(id);

        // Assert
        result.ShouldBeNull();

        _eventRepositoryMock.Verify(x => x.GetAsync(id), Times.Once());
    }

    [Fact]
    public async Task GetAsync_ById_ReturnsItem()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var eventItem = new Event().PopulateWithRandomData();
        _eventRepositoryMock.Setup(x => x.GetAsync(id)).ReturnsAsync(eventItem);

        // Act
        var result = await _eventService.GetAsync(id);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(eventItem);

        _eventRepositoryMock.Verify(x => x.GetAsync(id), Times.Once());
    }

    [Fact]
    public async Task CreateAsync_WithICalString_UpdatesDatesAndCreatesOccurrences()
    {
        // Arrange
        var eventItem = new Event
        {
            StartDate = DateTime.UtcNow,
            Duration = 120
        }.PopulateWithRandomData();

        eventItem.ICalString = CalendarUtils.CreateICalString(eventItem, FrequencyType.Weekly, 3);

        _eventRepositoryMock.Setup(x => x.CreateAsync(eventItem)).Returns(Task.CompletedTask);

        // Act
        await _eventService.CreateAsync(eventItem);

        // Assert
        eventItem.Occurrences.Count.ShouldBe(3);

        for (var i = 0; i < eventItem.Occurrences.Count; i++)
            eventItem.Occurrences[i].OccurrenceDate
                .ShouldBe(eventItem.StartDate.AddDays(i * 7), TimeSpan.FromMinutes(1));

        _eventRepositoryMock.Verify(x => x.CreateAsync(eventItem), Times.Once());
    }

    [Fact]
    public async Task CreateAsync_WithoutICalString_CalculatesEndDateAndCreatesSingleOccurrence()
    {
        // Arrange
        var startDate = DateTime.UtcNow;
        var eventItem = new Event().PopulateWithRandomData();
        eventItem.StartDate = startDate;
        eventItem.Duration = 120;
        eventItem.Occurrences = new List<EventOccurrence>();
        eventItem.ICalString = null;

        var occurrence = new EventOccurrence().PopulateWithRandomData();
        occurrence.OccurrenceDate = startDate;

        var occurrences = new List<EventOccurrence>
        {
            occurrence
        };

        _eventRepositoryMock.Setup(x => x.CreateAsync(eventItem)).Returns(Task.CompletedTask);

        // Act
        await _eventService.CreateAsync(eventItem);

        // Assert
        eventItem.EndDate.ShouldBe(startDate.AddMinutes(120), TimeSpan.FromSeconds(1));
        eventItem.Occurrences.Count.ShouldBe(occurrences.Count);
        eventItem.Occurrences.First().OccurrenceDate.ShouldBe(occurrences.First().OccurrenceDate);

        _eventRepositoryMock.Verify(x => x.CreateAsync(eventItem), Times.Once());
    }

    [Fact]
    public async Task UpdateAsync_WithICalString_UpdatesDatesAndRegeneratesOccurrences()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var existingEvent = new Event
        {
            StartDate = DateTime.UtcNow.AddDays(-1)
        }.PopulateWithRandomData();

        existingEvent.ICalString = CalendarUtils.CreateICalString(existingEvent, FrequencyType.Weekly, 2);

        var updatedEvent = new Event
        {
            StartDate = DateTime.UtcNow,
            Duration = 120
        }.PopulateWithRandomData();

        updatedEvent.ICalString = CalendarUtils.CreateICalString(updatedEvent, FrequencyType.Weekly, 3);

        _eventRepositoryMock.Setup(x => x.GetAsync(id)).ReturnsAsync(existingEvent);
        _eventRepositoryMock.Setup(x => x.UpdateAsync(id, updatedEvent)).Returns(Task.CompletedTask);

        // Act
        await _eventService.UpdateAsync(id, updatedEvent);

        // Assert
        updatedEvent.Occurrences.Count.ShouldBe(3);

        _eventRepositoryMock.Verify(x => x.GetAsync(id), Times.Once());
        _eventRepositoryMock.Verify(x => x.UpdateAsync(id, updatedEvent), Times.Once());
    }

    [Fact]
    public async Task UpdateAsync_WithoutICalString_CalculatesEndDate()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var existingEvent = new Event
        {
            StartDate = DateTime.UtcNow.AddDays(-1)
        }.PopulateWithRandomData();
        existingEvent.ICalString = null;

        var startDate = DateTime.UtcNow;
        var updatedEvent = new Event
        {
            StartDate = startDate,
            Duration = 90
        }.PopulateWithRandomData();
        updatedEvent.ICalString = null;

        var occurrences = new List<EventOccurrence>
        {
            new EventOccurrence { OccurrenceDate = startDate }.PopulateWithRandomData()
        };

        _eventRepositoryMock.Setup(x => x.GetAsync(id)).ReturnsAsync(existingEvent);
        _eventRepositoryMock.Setup(x => x.UpdateAsync(id, updatedEvent)).Returns(Task.CompletedTask);

        // Act
        await _eventService.UpdateAsync(id, updatedEvent);

        // Assert
        updatedEvent.EndDate.ShouldBe(startDate.AddMinutes(90), TimeSpan.FromSeconds(1));

        _eventRepositoryMock.Verify(x => x.GetAsync(id), Times.Once());
        _eventRepositoryMock.Verify(x => x.UpdateAsync(id, updatedEvent), Times.Once());
    }

    [Fact]
    public async Task UpdateAsync_PreservesExistingOccurrenceData()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var occurrenceDate = DateTime.UtcNow;

        var existingEvent = new Event
        {
            StartDate = occurrenceDate,
            ICalString = CalendarUtils.CreateICalString(occurrenceDate, FrequencyType.Weekly, 2),
            DiscordMessageId = "123456",
            Occurrences = new List<EventOccurrence>
            {
                new EventOccurrence
                {
                    Id = "existing-occ-id",
                    OccurrenceDate = occurrenceDate,
                    Status = OccurrenceStatus.Scheduled
                }.PopulateWithRandomData()
            },
            Signups = new List<EventSignup> { new EventSignup().PopulateWithRandomData() },
            Groups = new List<EventGroup>
            {
                new EventGroup
                {
                    Participants = new List<EventParticipant> { new EventParticipant().PopulateWithRandomData() }
                }
            }
        }.PopulateWithRandomData();

        var updatedEvent = new Event
        {
            StartDate = occurrenceDate,
            Duration = 120,
            ICalString = CalendarUtils.CreateICalString(occurrenceDate, FrequencyType.Weekly, 3)
        }.PopulateWithRandomData();

        _eventRepositoryMock.Setup(x => x.GetAsync(id)).ReturnsAsync(existingEvent);
        _eventRepositoryMock.Setup(x => x.UpdateAsync(id, updatedEvent)).Returns(Task.CompletedTask);

        // Act
        await _eventService.UpdateAsync(id, updatedEvent);

        // Assert
        updatedEvent.Occurrences.Count.ShouldBe(3);

        var preservedOccurrence = updatedEvent.Occurrences.OrderBy(x => x.OccurrenceDate).First();
        preservedOccurrence.Id.ShouldBe(existingEvent.Occurrences.First().Id);
        preservedOccurrence.Status.ShouldBe(existingEvent.Occurrences.First().Status);

        _eventRepositoryMock.Verify(x => x.UpdateAsync(id, updatedEvent), Times.Once());
    }

    [Fact]
    public async Task DeleteAsync_CallsRepository()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        _eventRepositoryMock.Setup(x => x.DeleteAsync(id)).Returns(Task.CompletedTask);

        // Act
        await _eventService.DeleteAsync(id);

        // Assert
        _eventRepositoryMock.Verify(x => x.DeleteAsync(id), Times.Once());
    }

    [Fact]
    public async Task HandleSignupAsync_DoesNothing_WhenEventNotFound()
    {
        // Arrange
        _eventRepositoryMock.Setup(x => x.GetAsync(It.IsAny<string>())).ReturnsAsync((Event)null);

        // Act
        await _eventService.HandleSignupAsync("nonexistent", Role.Tank, 123456789UL);

        // Assert
        _eventRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<Event>()), Times.Never());
    }
}