using Discord;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.Database.Events;
using ExcelBotCs.Models.DTO;
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

    #region GetAsync

    [Fact]
    public async Task GetAsync_ReturnsEmptyList_WhenRepositoryReturnsNull()
    {
        _eventRepositoryMock.Setup(x => x.GetAsync()).ReturnsAsync((List<Event>)null);

        var result = await _eventService.GetAsync();

        result.ShouldBeEmpty();
        _eventRepositoryMock.Verify(x => x.GetAsync(), Times.Once());
    }

    [Fact]
    public async Task GetAsync_ReturnsList_OrderedByStartDate()
    {
        var events = new List<Event>
        {
            new Event { StartDate = DateTime.UtcNow.AddDays(5) }.PopulateWithRandomData(),
            new Event { StartDate = DateTime.UtcNow.AddDays(1) }.PopulateWithRandomData(),
            new Event { StartDate = DateTime.UtcNow.AddDays(3) }.PopulateWithRandomData()
        };
        events.ForEach(x => x.IsArchived = false);

        _eventRepositoryMock.Setup(x => x.GetAsync()).ReturnsAsync(events);

        var result = await _eventService.GetAsync();

        result.ShouldNotBeNull();
        result.Count.ShouldBe(3);
        result[0].StartDate.ShouldBeLessThan(result[1].StartDate);
        result[1].StartDate.ShouldBeLessThan(result[2].StartDate);
        _eventRepositoryMock.Verify(x => x.GetAsync(), Times.Once());
    }

    [Fact]
    public async Task GetAsync_ExcludesArchivedByDefault()
    {
        var active = new Event().PopulateWithRandomData();
        active.IsArchived = false;
        var archived = new Event().PopulateWithRandomData();
        archived.IsArchived = true;
        var events = new List<Event> { active, archived };
        _eventRepositoryMock.Setup(x => x.GetAsync()).ReturnsAsync(events);

        var result = await _eventService.GetAsync();

        result.Count.ShouldBe(1);
        result[0].IsArchived.ShouldBeFalse();
    }

    [Fact]
    public async Task GetAsync_IncludesArchived_WhenRequested()
    {
        var active = new Event().PopulateWithRandomData();
        active.IsArchived = false;
        var archived = new Event().PopulateWithRandomData();
        archived.IsArchived = true;
        _eventRepositoryMock.Setup(x => x.GetAsync()).ReturnsAsync(new List<Event> { active, archived });

        var result = await _eventService.GetAsync(true);

        result.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetAsync_ById_ReturnsNull()
    {
        var id = Guid.NewGuid().ToString();
        _eventRepositoryMock.Setup(x => x.GetAsync(id)).ReturnsAsync((Event)null);

        var result = await _eventService.GetAsync(id);

        result.ShouldBeNull();
        _eventRepositoryMock.Verify(x => x.GetAsync(id), Times.Once());
    }

    [Fact]
    public async Task GetAsync_ById_ReturnsItem()
    {
        var id = Guid.NewGuid().ToString();
        var eventItem = new Event().PopulateWithRandomData();
        _eventRepositoryMock.Setup(x => x.GetAsync(id)).ReturnsAsync(eventItem);

        var result = await _eventService.GetAsync(id);

        result.ShouldNotBeNull();
        result.ShouldBe(eventItem);
        _eventRepositoryMock.Verify(x => x.GetAsync(id), Times.Once());
    }

    #endregion

    #region GetPagedAsync

    [Fact]
    public async Task GetPagedAsync_ReturnsEmptyResult_WhenRepositoryReturnsNull()
    {
        _eventRepositoryMock.Setup(x => x.GetAsync()).ReturnsAsync((List<Event>)null);

        var result = await _eventService.GetPagedAsync(1, 10);

        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
        result.Page.ShouldBe(1);
        result.PageSize.ShouldBe(10);
        result.HasMore.ShouldBeFalse();
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsFirstPage()
    {
        var events = Enumerable.Range(0, 5)
            .Select(i =>
            {
                var e = new Event().PopulateWithRandomData();
                e.IsArchived = false;
                e.DateCreated = DateTime.UtcNow.AddDays(-i);
                return e;
            }).ToList();
        _eventRepositoryMock.Setup(x => x.GetAsync()).ReturnsAsync(events);

        var result = await _eventService.GetPagedAsync(1, 3);

        result.Items.Count.ShouldBe(3);
        result.TotalCount.ShouldBe(5);
        result.HasMore.ShouldBeTrue();
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsLastPage()
    {
        var events = Enumerable.Range(0, 5)
            .Select(i =>
            {
                var e = new Event().PopulateWithRandomData();
                e.IsArchived = false;
                e.DateCreated = DateTime.UtcNow.AddDays(-i);
                return e;
            }).ToList();
        _eventRepositoryMock.Setup(x => x.GetAsync()).ReturnsAsync(events);

        var result = await _eventService.GetPagedAsync(2, 3);

        result.Items.Count.ShouldBe(2);
        result.TotalCount.ShouldBe(5);
        result.HasMore.ShouldBeFalse();
    }

    [Fact]
    public async Task GetPagedAsync_ExcludesArchivedEvents()
    {
        var e1 = new Event().PopulateWithRandomData();
        e1.IsArchived = false;
        var e2 = new Event().PopulateWithRandomData();
        e2.IsArchived = true;
        var e3 = new Event().PopulateWithRandomData();
        e3.IsArchived = false;
        _eventRepositoryMock.Setup(x => x.GetAsync()).ReturnsAsync(new List<Event> { e1, e2, e3 });

        var result = await _eventService.GetPagedAsync(1, 10);

        result.Items.Count.ShouldBe(2);
        result.TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task GetPagedAsync_OrdersByDateCreatedDescending()
    {
        var oldest = new Event().PopulateWithRandomData();
        oldest.IsArchived = false;
        oldest.DateCreated = DateTime.UtcNow.AddDays(-10);

        var newest = new Event().PopulateWithRandomData();
        newest.IsArchived = false;
        newest.DateCreated = DateTime.UtcNow;

        var middle = new Event().PopulateWithRandomData();
        middle.IsArchived = false;
        middle.DateCreated = DateTime.UtcNow.AddDays(-5);

        _eventRepositoryMock.Setup(x => x.GetAsync()).ReturnsAsync(new List<Event> { oldest, newest, middle });

        var result = await _eventService.GetPagedAsync(1, 10);

        result.Items[0].DateCreated.ShouldBeGreaterThan(result.Items[1].DateCreated);
        result.Items[1].DateCreated.ShouldBeGreaterThan(result.Items[2].DateCreated);
    }

    #endregion

    #region GetArchivedPagedAsync

    [Fact]
    public async Task GetArchivedPagedAsync_ReturnsOnlyArchivedEvents()
    {
        var a1 = new Event().PopulateWithRandomData();
        a1.IsArchived = true;
        a1.ArchivedDate = DateTime.UtcNow;
        var active = new Event().PopulateWithRandomData();
        active.IsArchived = false;
        var a2 = new Event().PopulateWithRandomData();
        a2.IsArchived = true;
        a2.ArchivedDate = DateTime.UtcNow.AddDays(-1);
        _eventRepositoryMock.Setup(x => x.GetAsync()).ReturnsAsync(new List<Event> { a1, active, a2 });

        var result = await _eventService.GetArchivedPagedAsync(1, 10);

        result.Items.Count.ShouldBe(2);
        result.TotalCount.ShouldBe(2);
        result.Items.ShouldAllBe(e => e.IsArchived);
    }

    [Fact]
    public async Task GetArchivedPagedAsync_PaginatesCorrectly()
    {
        var events = Enumerable.Range(0, 7)
            .Select(i =>
            {
                var e = new Event().PopulateWithRandomData();
                e.IsArchived = true;
                e.ArchivedDate = DateTime.UtcNow.AddDays(-i);
                return e;
            }).ToList();
        _eventRepositoryMock.Setup(x => x.GetAsync()).ReturnsAsync(events);

        var page1 = await _eventService.GetArchivedPagedAsync(1, 3);
        var page2 = await _eventService.GetArchivedPagedAsync(2, 3);
        var page3 = await _eventService.GetArchivedPagedAsync(3, 3);

        page1.Items.Count.ShouldBe(3);
        page1.HasMore.ShouldBeTrue();
        page2.Items.Count.ShouldBe(3);
        page2.HasMore.ShouldBeTrue();
        page3.Items.Count.ShouldBe(1);
        page3.HasMore.ShouldBeFalse();
    }

    [Fact]
    public async Task GetArchivedPagedAsync_FiltersSearchText()
    {
        var events = new List<Event>
        {
            new Event { IsArchived = true, Name = "Weekly Raid Night", ArchivedDate = DateTime.UtcNow }
                .PopulateWithRandomData(),
            new Event { IsArchived = true, Name = "Social Gathering", ArchivedDate = DateTime.UtcNow }
                .PopulateWithRandomData()
        };
        _eventRepositoryMock.Setup(x => x.GetAsync()).ReturnsAsync(events);

        var result = await _eventService.GetArchivedPagedAsync(1, 10,
            new ArchiveSearchParams { SearchText = "raid" });

        result.Items.Count.ShouldBe(1);
        result.Items[0].Name.ShouldBe("Weekly Raid Night");
    }

    [Fact]
    public async Task GetArchivedPagedAsync_FiltersEventType()
    {
        var raid = new Event().PopulateWithRandomData();
        raid.IsArchived = true;
        raid.Type = EventType.Raid;
        raid.ArchivedDate = DateTime.UtcNow;
        var social = new Event().PopulateWithRandomData();
        social.IsArchived = true;
        social.Type = EventType.Social;
        social.ArchivedDate = DateTime.UtcNow;
        _eventRepositoryMock.Setup(x => x.GetAsync()).ReturnsAsync(new List<Event> { raid, social });

        var result = await _eventService.GetArchivedPagedAsync(1, 10,
            new ArchiveSearchParams { EventType = EventType.Raid });

        result.Items.Count.ShouldBe(1);
        result.Items[0].Type.ShouldBe(EventType.Raid);
    }

    [Fact]
    public async Task GetArchivedPagedAsync_FiltersDateRange()
    {
        var now = DateTime.UtcNow;
        var events = new List<Event>
        {
            new Event { IsArchived = true, StartDate = now.AddDays(-30), ArchivedDate = now }
                .PopulateWithRandomData(),
            new Event { IsArchived = true, StartDate = now.AddDays(-5), ArchivedDate = now }
                .PopulateWithRandomData(),
            new Event { IsArchived = true, StartDate = now.AddDays(-60), ArchivedDate = now }
                .PopulateWithRandomData()
        };
        _eventRepositoryMock.Setup(x => x.GetAsync()).ReturnsAsync(events);

        var result = await _eventService.GetArchivedPagedAsync(1, 10,
            new ArchiveSearchParams { StartDate = now.AddDays(-35), EndDate = now.AddDays(-1) });

        result.Items.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetArchivedPagedAsync_SearchReturnsAllMatches_IgnoringPagination()
    {
        var events = Enumerable.Range(0, 15)
            .Select(i =>
            {
                var e = new Event().PopulateWithRandomData();
                e.IsArchived = true;
                e.Name = i < 10 ? $"Raid Night {i}" : $"Social {i}";
                e.ArchivedDate = DateTime.UtcNow.AddDays(-i);
                return e;
            }).ToList();
        _eventRepositoryMock.Setup(x => x.GetAsync()).ReturnsAsync(events);

        var result = await _eventService.GetArchivedPagedAsync(1, 5,
            new ArchiveSearchParams { SearchText = "Raid" });

        result.TotalCount.ShouldBe(10);
        result.Items.Count.ShouldBe(5);
        result.HasMore.ShouldBeTrue();
    }

    #endregion

    #region CreateAsync

    [Fact]
    public async Task CreateAsync_WithICalString_UpdatesDatesAndCreatesOccurrences()
    {
        var eventItem = new Event
        {
            StartDate = DateTime.UtcNow,
            Duration = 120
        }.PopulateWithRandomData();
        eventItem.ICalString = CalendarUtils.CreateICalString(eventItem, FrequencyType.Weekly, 3);
        _eventRepositoryMock.Setup(x => x.CreateAsync(eventItem)).Returns(Task.CompletedTask);

        await _eventService.CreateAsync(eventItem);

        eventItem.Occurrences.Count.ShouldBe(3);
        for (var i = 0; i < eventItem.Occurrences.Count; i++)
            eventItem.Occurrences[i].OccurrenceDate
                .ShouldBe(eventItem.StartDate.AddDays(i * 7), TimeSpan.FromMinutes(1));
        _eventRepositoryMock.Verify(x => x.CreateAsync(eventItem), Times.Once());
    }

    [Fact]
    public async Task CreateAsync_WithoutICalString_CalculatesEndDateAndCreatesSingleOccurrence()
    {
        var startDate = DateTime.UtcNow;
        var eventItem = new Event().PopulateWithRandomData();
        eventItem.StartDate = startDate;
        eventItem.Duration = 120;
        eventItem.Occurrences = new List<EventOccurrence>();
        eventItem.ICalString = null;
        _eventRepositoryMock.Setup(x => x.CreateAsync(eventItem)).Returns(Task.CompletedTask);

        await _eventService.CreateAsync(eventItem);

        eventItem.EndDate.ShouldBe(startDate.AddMinutes(120), TimeSpan.FromSeconds(1));
        eventItem.Occurrences.Count.ShouldBe(1);
        _eventRepositoryMock.Verify(x => x.CreateAsync(eventItem), Times.Once());
    }

    [Fact]
    public async Task CreateAsync_PostsDiscordSignupMessage()
    {
        var eventItem = new Event { StartDate = DateTime.UtcNow, Duration = 60 }.PopulateWithRandomData();
        eventItem.ICalString = null;
        _eventRepositoryMock.Setup(x => x.CreateAsync(eventItem)).Returns(Task.CompletedTask);

        await _eventService.CreateAsync(eventItem);

        _discordMessageServiceMock.Verify(x => x.PostEventSignupAsync(eventItem), Times.Once());
    }

    [Fact]
    public async Task CreateAsync_StoresSignupPostId_WhenDiscordMessageIsPosted()
    {
        var eventItem = new Event { StartDate = DateTime.UtcNow, Duration = 60 }.PopulateWithRandomData();
        eventItem.ICalString = null;
        var mockMessage = new Mock<IUserMessage>();
        mockMessage.Setup(m => m.Id).Returns(987654321UL);
        _discordMessageServiceMock.Setup(x => x.PostEventSignupAsync(eventItem))
            .ReturnsAsync(mockMessage.Object);
        _eventRepositoryMock.Setup(x => x.CreateAsync(eventItem)).Returns(Task.CompletedTask);
        _eventRepositoryMock.Setup(x => x.UpdateAsync(eventItem.Id, eventItem)).Returns(Task.CompletedTask);

        await _eventService.CreateAsync(eventItem);

        eventItem.SignupPostId.ShouldBe("987654321");
        _eventRepositoryMock.Verify(x => x.UpdateAsync(eventItem.Id, eventItem), Times.Once());
    }

    [Fact]
    public async Task CreateAsync_SkipsSignupPostId_WhenDiscordMessageIsNull()
    {
        var eventItem = new Event { StartDate = DateTime.UtcNow, Duration = 60 }.PopulateWithRandomData();
        eventItem.ICalString = null;
        eventItem.SignupPostId = null;
        _discordMessageServiceMock.Setup(x => x.PostEventSignupAsync(eventItem))
            .ReturnsAsync((IUserMessage?)null);
        _eventRepositoryMock.Setup(x => x.CreateAsync(eventItem)).Returns(Task.CompletedTask);

        await _eventService.CreateAsync(eventItem);

        eventItem.SignupPostId.ShouldBeNull();
        _eventRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<Event>()), Times.Never());
    }

    #endregion

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_WithICalString_UpdatesDatesAndRegeneratesOccurrences()
    {
        var id = Guid.NewGuid().ToString();
        var existingEvent = new Event { StartDate = DateTime.UtcNow.AddDays(-1) }.PopulateWithRandomData();
        existingEvent.ICalString = CalendarUtils.CreateICalString(existingEvent, FrequencyType.Weekly, 2);

        var updatedEvent = new Event { StartDate = DateTime.UtcNow, Duration = 120 }.PopulateWithRandomData();
        updatedEvent.ICalString = CalendarUtils.CreateICalString(updatedEvent, FrequencyType.Weekly, 3);

        _eventRepositoryMock.Setup(x => x.GetAsync(id)).ReturnsAsync(existingEvent);
        _eventRepositoryMock.Setup(x => x.UpdateAsync(id, updatedEvent)).Returns(Task.CompletedTask);

        await _eventService.UpdateAsync(id, updatedEvent);

        updatedEvent.Occurrences.Count.ShouldBe(3);
        _eventRepositoryMock.Verify(x => x.GetAsync(id), Times.Once());
        _eventRepositoryMock.Verify(x => x.UpdateAsync(id, updatedEvent), Times.Once());
    }

    [Fact]
    public async Task UpdateAsync_WithoutICalString_CalculatesEndDate()
    {
        var id = Guid.NewGuid().ToString();
        var existingEvent = new Event { StartDate = DateTime.UtcNow.AddDays(-1) }.PopulateWithRandomData();
        existingEvent.ICalString = null;

        var startDate = DateTime.UtcNow;
        var updatedEvent = new Event { StartDate = startDate, Duration = 90 }.PopulateWithRandomData();
        updatedEvent.ICalString = null;

        _eventRepositoryMock.Setup(x => x.GetAsync(id)).ReturnsAsync(existingEvent);
        _eventRepositoryMock.Setup(x => x.UpdateAsync(id, updatedEvent)).Returns(Task.CompletedTask);

        await _eventService.UpdateAsync(id, updatedEvent);

        updatedEvent.EndDate.ShouldBe(startDate.AddMinutes(90), TimeSpan.FromSeconds(1));
        _eventRepositoryMock.Verify(x => x.GetAsync(id), Times.Once());
        _eventRepositoryMock.Verify(x => x.UpdateAsync(id, updatedEvent), Times.Once());
    }

    [Fact]
    public async Task UpdateAsync_PreservesExistingOccurrenceData()
    {
        var id = Guid.NewGuid().ToString();
        var occurrenceDate = DateTime.UtcNow;

        var existingEvent = new Event
        {
            StartDate = occurrenceDate,
            ICalString = CalendarUtils.CreateICalString(occurrenceDate, FrequencyType.Weekly, 2),
            SignupPostId = "123456",
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

        await _eventService.UpdateAsync(id, updatedEvent);

        updatedEvent.Occurrences.Count.ShouldBe(3);
        var preservedOccurrence = updatedEvent.Occurrences.OrderBy(x => x.OccurrenceDate).First();
        preservedOccurrence.Id.ShouldBe(existingEvent.Occurrences.First().Id);
        preservedOccurrence.Status.ShouldBe(existingEvent.Occurrences.First().Status);
        _eventRepositoryMock.Verify(x => x.UpdateAsync(id, updatedEvent), Times.Once());
    }

    [Fact]
    public async Task UpdateAsync_UpdatesDiscordMessage_WhenMessageIdExists()
    {
        var id = Guid.NewGuid().ToString();
        var existingEvent = new Event { StartDate = DateTime.UtcNow, SignupPostId = "123" }.PopulateWithRandomData();
        existingEvent.ICalString = null;

        var updatedEvent = new Event
        {
            StartDate = existingEvent.StartDate,
            Duration = 60,
            SignupPostId = "123"
        }.PopulateWithRandomData();
        updatedEvent.ICalString = null;

        _eventRepositoryMock.Setup(x => x.GetAsync(id)).ReturnsAsync(existingEvent);
        _eventRepositoryMock.Setup(x => x.UpdateAsync(id, updatedEvent)).Returns(Task.CompletedTask);

        await _eventService.UpdateAsync(id, updatedEvent);

        _discordMessageServiceMock.Verify(x => x.UpdateSignupMessage(updatedEvent), Times.Once());
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_DeletesDiscordMessage_WhenMessageIdExists()
    {
        var id = Guid.NewGuid().ToString();
        var fcEvent = new Event { SignupPostId = "123456789" }.PopulateWithRandomData();
        _eventRepositoryMock.Setup(x => x.GetAsync(id)).ReturnsAsync(fcEvent);
        _eventRepositoryMock.Setup(x => x.DeleteAsync(id)).Returns(Task.CompletedTask);

        await _eventService.DeleteAsync(id);

        _discordMessageServiceMock.Verify(x => x.DeleteEventMessageAsync("123456789"), Times.Once());
        _eventRepositoryMock.Verify(x => x.DeleteAsync(id), Times.Once());
    }

    [Fact]
    public async Task DeleteAsync_SkipsDiscordDeletion_WhenMessageIdIsNull()
    {
        var id = Guid.NewGuid().ToString();
        var fcEvent = new Event().PopulateWithRandomData();
        fcEvent.SignupPostId = null;
        _eventRepositoryMock.Setup(x => x.GetAsync(id)).ReturnsAsync(fcEvent);
        _eventRepositoryMock.Setup(x => x.DeleteAsync(id)).Returns(Task.CompletedTask);

        await _eventService.DeleteAsync(id);

        _discordMessageServiceMock.Verify(x => x.DeleteEventMessageAsync(It.IsAny<string>()), Times.Never());
        _eventRepositoryMock.Verify(x => x.DeleteAsync(id), Times.Once());
    }

    [Fact]
    public async Task DeleteAsync_StillDeletesFromRepository_WhenEventNotFound()
    {
        var id = Guid.NewGuid().ToString();
        _eventRepositoryMock.Setup(x => x.GetAsync(id)).ReturnsAsync((Event)null);
        _eventRepositoryMock.Setup(x => x.DeleteAsync(id)).Returns(Task.CompletedTask);

        await _eventService.DeleteAsync(id);

        _discordMessageServiceMock.Verify(x => x.DeleteEventMessageAsync(It.IsAny<string>()), Times.Never());
        _eventRepositoryMock.Verify(x => x.DeleteAsync(id), Times.Once());
    }

    #endregion

    #region HandleSignupAsync

    [Fact]
    public async Task HandleSignupAsync_DoesNothing_WhenEventNotFound()
    {
        _eventRepositoryMock.Setup(x => x.GetAsync(It.IsAny<string>())).ReturnsAsync((Event)null);

        await _eventService.HandleSignupAsync("nonexistent", Role.Tank, 123456789UL);

        _eventRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<Event>()), Times.Never());
    }

    [Fact]
    public async Task HandleSignupAsync_AddsNewSignup_WhenUserNotSignedUp()
    {
        var fcEvent = new Event().PopulateWithRandomData();
        fcEvent.Signups = new List<EventSignup>();
        fcEvent.ICalString = null;
        _eventRepositoryMock.Setup(x => x.GetAsync(fcEvent.Id)).ReturnsAsync(fcEvent);
        _eventRepositoryMock.Setup(x => x.UpdateAsync(fcEvent.Id, fcEvent)).Returns(Task.CompletedTask);

        await _eventService.HandleSignupAsync(fcEvent.Id, Role.Tank, 123UL);

        fcEvent.Signups.Count.ShouldBe(1);
        fcEvent.Signups[0].DiscordUserId.ShouldBe("123");
        fcEvent.Signups[0].Roles.ShouldContain(Role.Tank);
    }

    [Fact]
    public async Task HandleSignupAsync_AddsRole_WhenUserAlreadySignedUpWithDifferentRole()
    {
        var fcEvent = new Event
        {
            Signups = new List<EventSignup>
            {
                new EventSignup
                {
                    DiscordUserId = "123",
                    Roles = new List<Role> { Role.Tank },
                    SignupDate = DateTime.UtcNow
                }
            }
        }.PopulateWithRandomData();
        fcEvent.ICalString = null;
        _eventRepositoryMock.Setup(x => x.GetAsync(fcEvent.Id)).ReturnsAsync(fcEvent);
        _eventRepositoryMock.Setup(x => x.UpdateAsync(fcEvent.Id, fcEvent)).Returns(Task.CompletedTask);

        await _eventService.HandleSignupAsync(fcEvent.Id, Role.Healer, 123UL);

        fcEvent.Signups.Count.ShouldBe(1);
        fcEvent.Signups[0].Roles.ShouldContain(Role.Tank);
        fcEvent.Signups[0].Roles.ShouldContain(Role.Healer);
    }

    [Fact]
    public async Task HandleSignupAsync_RemovesRole_WhenUserAlreadySignedUpWithSameRole()
    {
        var fcEvent = new Event
        {
            Signups = new List<EventSignup>
            {
                new EventSignup
                {
                    DiscordUserId = "123",
                    Roles = new List<Role> { Role.Tank, Role.Healer },
                    SignupDate = DateTime.UtcNow
                }
            }
        }.PopulateWithRandomData();
        fcEvent.ICalString = null;
        _eventRepositoryMock.Setup(x => x.GetAsync(fcEvent.Id)).ReturnsAsync(fcEvent);
        _eventRepositoryMock.Setup(x => x.UpdateAsync(fcEvent.Id, fcEvent)).Returns(Task.CompletedTask);

        await _eventService.HandleSignupAsync(fcEvent.Id, Role.Tank, 123UL);

        fcEvent.Signups.Count.ShouldBe(1);
        fcEvent.Signups[0].Roles.ShouldNotContain(Role.Tank);
        fcEvent.Signups[0].Roles.ShouldContain(Role.Healer);
    }

    #endregion

    #region ArchiveAsync

    [Fact]
    public async Task ArchiveAsync_ReturnsError_WhenEventNotFound()
    {
        _eventRepositoryMock.Setup(x => x.GetAsync("id")).ReturnsAsync((Event)null);

        var (success, error) = await _eventService.ArchiveAsync("id", "user1");

        success.ShouldBeFalse();
        error.ShouldBe("Event not found");
    }

    [Fact]
    public async Task ArchiveAsync_ReturnsError_WhenAlreadyArchived()
    {
        var fcEvent = new Event { IsArchived = true }.PopulateWithRandomData();
        _eventRepositoryMock.Setup(x => x.GetAsync(fcEvent.Id)).ReturnsAsync(fcEvent);

        var (success, error) = await _eventService.ArchiveAsync(fcEvent.Id, "user1");

        success.ShouldBeFalse();
        error.ShouldBe("Event is already archived");
    }

    [Fact]
    public async Task ArchiveAsync_ReturnsError_WhenNotAllOccurrencesCompleted()
    {
        var fcEvent = new Event
        {
            Occurrences = new List<EventOccurrence>
            {
                new EventOccurrence { Status = OccurrenceStatus.Completed },
                new EventOccurrence { Status = OccurrenceStatus.Scheduled }
            }
        }.PopulateWithRandomData();
        _eventRepositoryMock.Setup(x => x.GetAsync(fcEvent.Id)).ReturnsAsync(fcEvent);

        var (success, error) = await _eventService.ArchiveAsync(fcEvent.Id, "user1");

        success.ShouldBeFalse();
    }

    [Fact]
    public async Task ArchiveAsync_Succeeds_WhenAllOccurrencesCompleted()
    {
        var fcEvent = new Event().PopulateWithRandomData();
        fcEvent.IsArchived = false;
        fcEvent.Occurrences = new List<EventOccurrence>
        {
            new EventOccurrence { Status = OccurrenceStatus.Completed },
            new EventOccurrence { Status = OccurrenceStatus.Cancelled }
        };
        _eventRepositoryMock.Setup(x => x.GetAsync(fcEvent.Id)).ReturnsAsync(fcEvent);

        var (success, _) = await _eventService.ArchiveAsync(fcEvent.Id, "user1");

        success.ShouldBeTrue();
        fcEvent.IsArchived.ShouldBeTrue();
        fcEvent.ArchivedByUserId.ShouldBe("user1");
        fcEvent.ArchivedDate.ShouldNotBeNull();
        _eventRepositoryMock.Verify(x => x.UpdateAsync(fcEvent.Id, fcEvent), Times.Once());
    }

    [Fact]
    public async Task ArchiveAsync_UpdatesDiscordMessage()
    {
        var fcEvent = new Event().PopulateWithRandomData();
        fcEvent.IsArchived = false;
        fcEvent.SignupPostId = "12345";
        fcEvent.Occurrences = new List<EventOccurrence>
        {
            new EventOccurrence { Status = OccurrenceStatus.Completed }
        };
        _eventRepositoryMock.Setup(x => x.GetAsync(fcEvent.Id)).ReturnsAsync(fcEvent);

        await _eventService.ArchiveAsync(fcEvent.Id, "user1");

        _discordMessageServiceMock.Verify(x => x.UpdateSignupMessage(fcEvent), Times.Once());
    }

    [Fact]
    public async Task ArchiveAsync_DeletesUpcomingRosterMessage_WhenRosterMessageIdExists()
    {
        var fcEvent = new Event().PopulateWithRandomData();
        fcEvent.IsArchived = false;
        fcEvent.SignupPostId = null;
        fcEvent.UpcomingRosterMessageId = "99999";
        fcEvent.Occurrences = new List<EventOccurrence>
        {
            new() { Status = OccurrenceStatus.Completed }
        };
        _eventRepositoryMock.Setup(x => x.GetAsync(fcEvent.Id)).ReturnsAsync(fcEvent);

        await _eventService.ArchiveAsync(fcEvent.Id, "user1");

        _discordMessageServiceMock.Verify(x => x.DeleteUpcomingRosterMessageAsync("99999"), Times.Once());
    }

    [Fact]
    public async Task ArchiveAsync_SkipsRosterDeletion_WhenRosterMessageIdIsNull()
    {
        var fcEvent = new Event().PopulateWithRandomData();
        fcEvent.IsArchived = false;
        fcEvent.SignupPostId = null;
        fcEvent.UpcomingRosterMessageId = null;
        fcEvent.Occurrences = new List<EventOccurrence>
        {
            new() { Status = OccurrenceStatus.Completed }
        };
        _eventRepositoryMock.Setup(x => x.GetAsync(fcEvent.Id)).ReturnsAsync(fcEvent);

        await _eventService.ArchiveAsync(fcEvent.Id, "user1");

        _discordMessageServiceMock.Verify(x => x.DeleteUpcomingRosterMessageAsync(It.IsAny<string>()), Times.Never());
    }

    #endregion

    #region TryAutoArchiveAsync

    [Fact]
    public async Task TryAutoArchiveAsync_ReturnsFalse_WhenEventNotFound()
    {
        _eventRepositoryMock.Setup(x => x.GetAsync("id")).ReturnsAsync((Event)null);

        var result = await _eventService.TryAutoArchiveAsync("id", "user1");

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task TryAutoArchiveAsync_ReturnsFalse_WhenAlreadyArchived()
    {
        var fcEvent = new Event { IsArchived = true }.PopulateWithRandomData();
        _eventRepositoryMock.Setup(x => x.GetAsync(fcEvent.Id)).ReturnsAsync(fcEvent);

        var result = await _eventService.TryAutoArchiveAsync(fcEvent.Id, "user1");

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task TryAutoArchiveAsync_ReturnsFalse_WhenCannotBeArchived()
    {
        var fcEvent = new Event
        {
            Occurrences = new List<EventOccurrence>
            {
                new EventOccurrence { Status = OccurrenceStatus.Scheduled }
            }
        }.PopulateWithRandomData();
        _eventRepositoryMock.Setup(x => x.GetAsync(fcEvent.Id)).ReturnsAsync(fcEvent);

        var result = await _eventService.TryAutoArchiveAsync(fcEvent.Id, "user1");

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task TryAutoArchiveAsync_ArchivesAndUpdatesDiscordMessage()
    {
        var fcEvent = new Event().PopulateWithRandomData();
        fcEvent.IsArchived = false;
        fcEvent.SignupPostId = "12345";
        fcEvent.Occurrences = new List<EventOccurrence>
        {
            new EventOccurrence { Status = OccurrenceStatus.Completed }
        };
        _eventRepositoryMock.Setup(x => x.GetAsync(fcEvent.Id)).ReturnsAsync(fcEvent);

        var result = await _eventService.TryAutoArchiveAsync(fcEvent.Id, "user1");

        result.ShouldBeTrue();
        fcEvent.IsArchived.ShouldBeTrue();
        _discordMessageServiceMock.Verify(x => x.UpdateSignupMessage(fcEvent), Times.Once());
    }

    [Fact]
    public async Task TryAutoArchiveAsync_DeletesUpcomingRosterMessage_WhenRosterMessageIdExists()
    {
        var fcEvent = new Event().PopulateWithRandomData();
        fcEvent.IsArchived = false;
        fcEvent.SignupPostId = null;
        fcEvent.UpcomingRosterMessageId = "77777";
        fcEvent.Occurrences = new List<EventOccurrence>
        {
            new() { Status = OccurrenceStatus.Completed }
        };
        _eventRepositoryMock.Setup(x => x.GetAsync(fcEvent.Id)).ReturnsAsync(fcEvent);

        var result = await _eventService.TryAutoArchiveAsync(fcEvent.Id, "user1");

        result.ShouldBeTrue();
        _discordMessageServiceMock.Verify(x => x.DeleteUpcomingRosterMessageAsync("77777"), Times.Once());
    }

    #endregion

    #region RestoreAsync

    [Fact]
    public async Task RestoreAsync_ReturnsError_WhenEventNotFound()
    {
        _eventRepositoryMock.Setup(x => x.GetAsync("id")).ReturnsAsync((Event)null);

        var (success, error) = await _eventService.RestoreAsync("id");

        success.ShouldBeFalse();
        error.ShouldBe("Event not found");
    }

    [Fact]
    public async Task RestoreAsync_ReturnsError_WhenNotArchived()
    {
        var fcEvent = new Event().PopulateWithRandomData();
        fcEvent.IsArchived = false;
        _eventRepositoryMock.Setup(x => x.GetAsync(fcEvent.Id)).ReturnsAsync(fcEvent);

        var (success, error) = await _eventService.RestoreAsync(fcEvent.Id);

        success.ShouldBeFalse();
        error.ShouldBe("Event is not archived");
    }

    [Fact]
    public async Task RestoreAsync_Succeeds()
    {
        var fcEvent = new Event
        {
            IsArchived = true,
            ArchivedDate = DateTime.UtcNow,
            ArchivedByUserId = "user1"
        }.PopulateWithRandomData();
        _eventRepositoryMock.Setup(x => x.GetAsync(fcEvent.Id)).ReturnsAsync(fcEvent);

        var (success, _) = await _eventService.RestoreAsync(fcEvent.Id);

        success.ShouldBeTrue();
        fcEvent.IsArchived.ShouldBeFalse();
        fcEvent.ArchivedDate.ShouldBeNull();
        fcEvent.ArchivedByUserId.ShouldBeNull();
        _eventRepositoryMock.Verify(x => x.UpdateAsync(fcEvent.Id, fcEvent), Times.Once());
    }

    #endregion

    #region ExtendEventAsync

    [Fact]
    public async Task ExtendEventAsync_ReturnsError_WhenCountIsZero()
    {
        var (result, error) = await _eventService.ExtendEventAsync("id", 0);

        result.ShouldBeNull();
        error.ShouldBe("Count must be greater than 0");
    }

    [Fact]
    public async Task ExtendEventAsync_ReturnsError_WhenEventNotFound()
    {
        _eventRepositoryMock.Setup(x => x.GetAsync("id")).ReturnsAsync((Event)null);

        var (result, error) = await _eventService.ExtendEventAsync("id", 2);

        result.ShouldBeNull();
        error.ShouldBe("Event not found");
    }

    [Fact]
    public async Task ExtendEventAsync_ReturnsError_WhenNotRecurring()
    {
        var fcEvent = new Event().PopulateWithRandomData();
        fcEvent.ICalString = null;
        _eventRepositoryMock.Setup(x => x.GetAsync(fcEvent.Id)).ReturnsAsync(fcEvent);

        var (result, error) = await _eventService.ExtendEventAsync(fcEvent.Id, 2);

        result.ShouldBeNull();
        error.ShouldContain("non-recurring");
    }

    #endregion
}
