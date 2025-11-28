using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Services;
using ExcelBotCs.Services.API;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.TestFramework.Utils;
using Ical.Net;
using Moq;

namespace ExcelBotCs.Tests.Services.API;

[TestFixture]
public class EventServiceTests
{
    private IEventService _eventService;
    private Mock<IEventRepository> _eventRepositoryMock;
    private IICalService _iCalService;

    [SetUp]
    public void SetUp()
    {
        _eventRepositoryMock = new Mock<IEventRepository>();
        _iCalService = new ICalService();
        _eventService = new EventService(_eventRepositoryMock.Object, _iCalService);
    }

    [Test]
    public async Task GetAsync_ReturnsNull()
    {
        // Arrange
        _eventRepositoryMock.Setup(x => x.GetAsync()).ReturnsAsync((List<Event>)null);

        // Act
        var result = await _eventService.GetAsync();

        // Assert
        Assert.That(result, Is.Empty);

        _eventRepositoryMock.Verify(x => x.GetAsync(), Times.Once());
    }

    [Test]
    public async Task GetAsync_ReturnsList_OrderedByStartDate()
    {
        // Arrange
        var events = new List<Event>
        {
            new Event { StartDate = DateTime.UtcNow.AddDays(5) }.PopulateWithRandomData(),
            new Event { StartDate = DateTime.UtcNow.AddDays(1) }.PopulateWithRandomData(),
            new Event { StartDate = DateTime.UtcNow.AddDays(3) }.PopulateWithRandomData()
        };
        _eventRepositoryMock.Setup(x => x.GetAsync()).ReturnsAsync(events);

        // Act
        var result = await _eventService.GetAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(3));
        Assert.That(result[0].StartDate, Is.LessThan(result[1].StartDate));
        Assert.That(result[1].StartDate, Is.LessThan(result[2].StartDate));

        _eventRepositoryMock.Verify(x => x.GetAsync(), Times.Once());
    }

    [Test]
    public async Task GetAsync_ById_ReturnsNull()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        _eventRepositoryMock.Setup(x => x.GetAsync(id)).ReturnsAsync((Event)null);

        // Act
        var result = await _eventService.GetAsync(id);

        // Assert
        Assert.That(result, Is.Null);

        _eventRepositoryMock.Verify(x => x.GetAsync(id), Times.Once());
    }

    [Test]
    public async Task GetAsync_ById_ReturnsItem()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var eventItem = new Event().PopulateWithRandomData();
        _eventRepositoryMock.Setup(x => x.GetAsync(id)).ReturnsAsync(eventItem);

        // Act
        var result = await _eventService.GetAsync(id);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(eventItem));

        _eventRepositoryMock.Verify(x => x.GetAsync(id), Times.Once());
    }

    [Test]
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
        Assert.That(eventItem.Occurrences, Has.Count.EqualTo(3));

        for (var i = 0; i < eventItem.Occurrences.Count; i++)
            Assert.That(eventItem.Occurrences[i].OccurrenceDate,
                Is.EqualTo(eventItem.StartDate.AddDays(i * 7)).Within(TimeSpan.FromMinutes(1)));

        _eventRepositoryMock.Verify(x => x.CreateAsync(eventItem), Times.Once());
    }

    [Test]
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
        Assert.That(eventItem.EndDate, Is.EqualTo(startDate.AddMinutes(120)).Within(TimeSpan.FromSeconds(1)));
        Assert.That(eventItem.Occurrences.Count, Is.EqualTo(occurrences.Count));
        Assert.That(eventItem.Occurrences.First().OccurrenceDate, Is.EqualTo(occurrences.First().OccurrenceDate));

        _eventRepositoryMock.Verify(x => x.CreateAsync(eventItem), Times.Once());
    }

    [Test]
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
        Assert.That(updatedEvent.Occurrences, Has.Count.EqualTo(3));

        _eventRepositoryMock.Verify(x => x.GetAsync(id), Times.Once());
        _eventRepositoryMock.Verify(x => x.UpdateAsync(id, updatedEvent), Times.Once());
    }

    [Test]
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
        Assert.That(updatedEvent.EndDate, Is.EqualTo(startDate.AddMinutes(90)).Within(TimeSpan.FromSeconds(1)));

        _eventRepositoryMock.Verify(x => x.GetAsync(id), Times.Once());
        _eventRepositoryMock.Verify(x => x.UpdateAsync(id, updatedEvent), Times.Once());
    }

    [Test]
    public async Task UpdateAsync_PreservesExistingOccurrenceData()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var occurrenceDate = DateTime.UtcNow;

        var existingEvent = new Event
        {
            StartDate = occurrenceDate,
            ICalString = CalendarUtils.CreateICalString(occurrenceDate, FrequencyType.Weekly, 2),
            Occurrences = new List<EventOccurrence>
            {
                new EventOccurrence
                {
                    Id = "existing-occ-id",
                    OccurrenceDate = occurrenceDate,
                    Status = OccurrenceStatus.Scheduled,
                    DiscordMessageId = "123456",
                    Signups = new List<EventSignup> { new EventSignup().PopulateWithRandomData() },
                    Participants = new List<EventParticipant> { new EventParticipant().PopulateWithRandomData() }
                }.PopulateWithRandomData()
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
        Assert.That(updatedEvent.Occurrences, Has.Count.EqualTo(3));

        var preservedOccurrence = updatedEvent.Occurrences.OrderBy(x => x.OccurrenceDate).First();
        Assert.That(preservedOccurrence.Id, Is.EqualTo(existingEvent.Occurrences.First().Id));
        Assert.That(preservedOccurrence.Status, Is.EqualTo(existingEvent.Occurrences.First().Status));
        Assert.That(preservedOccurrence.DiscordMessageId,
            Is.EqualTo(existingEvent.Occurrences.First().DiscordMessageId));
        Assert.That(preservedOccurrence.Signups, Is.Not.Null);
        Assert.That(preservedOccurrence.Participants, Is.Not.Null);

        _eventRepositoryMock.Verify(x => x.UpdateAsync(id, updatedEvent), Times.Once());
    }

    [Test]
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
}