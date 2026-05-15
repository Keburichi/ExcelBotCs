using System.Net;
using System.Net.Http.Json;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.Database.Events;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.Models.DTO.Events;
using ExcelBotCs.Modules.TeamFormation;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.TestFramework.Database;
using ExcelBotCs.TestFramework.Utils;
using ExcelBotCs.Tests.Utils;
using Ical.Net;
using Microsoft.Extensions.DependencyInjection;
using EventSignup = ExcelBotCs.Models.Database.Events.EventSignup;

namespace ExcelBotCs.Tests.Controllers;

public class EventsControllerIntegrationTests : IntegrationTestBase
{
    private IEventService _eventService = null!;

    public EventsControllerIntegrationTests(MongoDbFixture fixture) : base(fixture)
    {
    }

    protected override async Task OnAfterIntegrationSetupAsync()
    {
        await base.OnAfterIntegrationSetupAsync();
        _eventService = Factory.Services.GetRequiredService<IEventService>();
    }

    #region Helper Methods

    private Event CreateTestEvent(
        string name = "Test Event",
        SignupType signupType = SignupType.IndependentSignups,
        int maxParticipants = 8,
        DateTime? startDate = null,
        int occurrenceCount = 1)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(1);

        var iCalString = occurrenceCount > 1
            ? CalendarUtils.CreateICalString(start, FrequencyType.Weekly, occurrenceCount)
            : "";

        var fcEvent = new Event
        {
            Name = name,
            Description = "Test event description",
            Type = EventType.Raid,
            StartDate = start,
            Duration = 120,
            ICalString = iCalString,
            SignupType = signupType,
            MaxNumberOfParticipants = maxParticipants,
            Occurrences = new List<EventOccurrence>()
        };

        for (var i = 0; i < occurrenceCount; i++)
            fcEvent.Occurrences.Add(new EventOccurrence
            {
                OccurrenceDate = start.AddDays(i * 7),
                Status = OccurrenceStatus.Scheduled
            });

        return fcEvent;
    }

    private async Task<Event> CreateAndSaveTestEvent(
        string name = "Test Event",
        SignupType signupType = SignupType.IndependentSignups,
        int maxParticipants = 8,
        DateTime? startDate = null,
        int occurrenceCount = 1)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(1);

        var fcEvent = new Event
        {
            Name = name,
            Description = "Test event description",
            Type = EventType.Raid,
            StartDate = start,
            Duration = 120,
            ICalString = "",
            SignupType = signupType,
            MaxNumberOfParticipants = maxParticipants
        };

        await _eventService.CreateAsync(fcEvent);
        return (await _eventService.GetAsync(fcEvent.Id))!;
    }

    private async Task<Event> CreateAndSaveRecurringTestEvent(
        string name = "Test Recurring Event",
        SignupType signupType = SignupType.IndependentSignups,
        int maxParticipants = 8,
        DateTime? startDate = null,
        int weeklyCount = 3)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(1);

        var iCalString = $@"BEGIN:VCALENDAR
VERSION:2.0
BEGIN:VEVENT
DTSTART:{start:yyyyMMddTHHmmssZ}
RRULE:FREQ=WEEKLY;COUNT={weeklyCount}
END:VEVENT
END:VCALENDAR";

        var fcEvent = new Event
        {
            Name = name,
            Description = "Test event description",
            Type = EventType.Raid,
            StartDate = start,
            Duration = 120,
            ICalString = iCalString,
            SignupType = signupType,
            MaxNumberOfParticipants = maxParticipants
        };

        await _eventService.CreateAsync(fcEvent);
        return (await _eventService.GetAsync(fcEvent.Id))!;
    }

    private async Task ArchiveEvent(Event fcEvent, string archivedByUserId = "admin1")
    {
        var savedEvent = await _eventService.GetAsync(fcEvent.Id);
        foreach (var occurrence in savedEvent!.Occurrences)
            occurrence.Status = OccurrenceStatus.Completed;
        savedEvent.IsArchived = true;
        savedEvent.ArchivedDate = DateTime.UtcNow;
        savedEvent.ArchivedByUserId = archivedByUserId;
        var eventRepository = Factory.Services.GetRequiredService<IEventRepository>();
        await eventRepository.UpdateAsync(fcEvent.Id, savedEvent);
    }

    #endregion

    #region Authentication Tests

    [Fact]
    public async Task SignupForEvent_Unauthenticated_ReturnsUnauthorized()
    {
        SetUnauthenticated();
        var fcEvent = CreateTestEvent();
        await _eventService.CreateAsync(fcEvent);

        var response = await Client.PostAsJsonAsync(
            $"api/Events/{fcEvent.Id}/signup",
            new EventSignupDto { Roles = new List<Role> { Role.Tank } });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SignupForEvent_AsMember_ReturnsOk()
    {
        await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        var fcEvent = CreateTestEvent();
        await _eventService.CreateAsync(fcEvent);

        var response = await Client.PostAsJsonAsync(
            $"api/Events/{fcEvent.Id}/signup",
            new EventSignupDto { Roles = new List<Role> { Role.Tank } });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SelectParticipants_AsMember_ReturnsForbidden()
    {
        var member = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        var fcEvent = CreateTestEvent();
        await _eventService.CreateAsync(fcEvent);

        var groups = new List<EventGroupRequest>
        {
            new()
            {
                Name = "Group 1",
                Participants = new List<EventParticipantDto>
                {
                    new() { DiscordUserId = member.DiscordId, Role = Role.Tank }
                }
            }
        };

        var response = await Client.PostAsJsonAsync(
            $"api/Events/{fcEvent.Id}/participants",
            groups);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SelectParticipants_AsAdmin_ReturnsOk()
    {
        var admin = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);
        var fcEvent = CreateTestEvent();
        await _eventService.CreateAsync(fcEvent);

        var groups = new List<EventGroupRequest>
        {
            new()
            {
                Name = "Group 1",
                Participants = new List<EventParticipantDto>
                {
                    new() { DiscordUserId = admin.DiscordId, Role = Role.Tank }
                }
            }
        };

        var response = await Client.PostAsJsonAsync(
            $"api/Events/{fcEvent.Id}/participants",
            groups);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    #endregion

    #region Pagination Tests

    [Fact]
    public async Task GetEvents_ReturnsPaginatedResult()
    {
        await AuthenticateAsMember();
        var event1 = CreateTestEvent("Event 1");
        var event2 = CreateTestEvent("Event 2");
        await _eventService.CreateAsync(event1);
        await _eventService.CreateAsync(event2);

        var response = await Client.GetAsync("api/Events");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<EventResponse>>();
        result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(2);
        result.TotalCount.ShouldBe(2);
        result.HasMore.ShouldBeFalse();
    }

    [Fact]
    public async Task GetEvents_RespectsPageSize()
    {
        await AuthenticateAsMember();
        for (var i = 0; i < 5; i++)
        {
            var e = CreateTestEvent($"Event {i}");
            await _eventService.CreateAsync(e);
        }

        var response = await Client.GetAsync("api/Events?page=1&pageSize=3");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<EventResponse>>();
        result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(3);
        result.TotalCount.ShouldBe(5);
        result.HasMore.ShouldBeTrue();
        result.Page.ShouldBe(1);
        result.PageSize.ShouldBe(3);
    }

    [Fact]
    public async Task GetEvents_SecondPageReturnsRemainingItems()
    {
        await AuthenticateAsMember();
        for (var i = 0; i < 5; i++)
        {
            var e = CreateTestEvent($"Event {i}");
            await _eventService.CreateAsync(e);
        }

        var response = await Client.GetAsync("api/Events?page=2&pageSize=3");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<EventResponse>>();
        result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(2);
        result.TotalCount.ShouldBe(5);
        result.HasMore.ShouldBeFalse();
    }

    [Fact]
    public async Task GetEvents_ExcludesArchivedEvents()
    {
        await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);

        var activeEvent = await CreateAndSaveTestEvent("Active Event");
        var archivedEvent = await CreateAndSaveTestEvent("Archived Event", startDate: DateTime.UtcNow.AddDays(-7));
        await ArchiveEvent(archivedEvent);

        var response = await Client.GetAsync("api/Events");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<EventResponse>>();
        result.ShouldNotBeNull();
        result.Items.Any(e => e.Name == "Active Event").ShouldBeTrue();
        result.Items.Any(e => e.Name == "Archived Event").ShouldBeFalse();
    }

    [Fact]
    public async Task GetArchivedEvents_ReturnsPaginatedResult()
    {
        await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);

        var event1 = await CreateAndSaveTestEvent("Archived 1", startDate: DateTime.UtcNow.AddDays(-7));
        var event2 = await CreateAndSaveTestEvent("Archived 2", startDate: DateTime.UtcNow.AddDays(-14));
        await ArchiveEvent(event1);
        await ArchiveEvent(event2);

        await CreateAndSaveTestEvent("Active Event");

        var response = await Client.GetAsync("api/Events/archived");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<EventResponse>>();
        result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(2);
        result.TotalCount.ShouldBe(2);
        result.Items.Any(e => e.Name == "Active Event").ShouldBeFalse();
    }

    [Fact]
    public async Task GetArchivedEvents_RespectsPageSize()
    {
        await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);

        for (var i = 0; i < 5; i++)
        {
            var e = await CreateAndSaveTestEvent($"Archived {i}", startDate: DateTime.UtcNow.AddDays(-7 - i));
            await ArchiveEvent(e);
        }

        var response = await Client.GetAsync("api/Events/archived?page=1&pageSize=2");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<EventResponse>>();
        result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(2);
        result.TotalCount.ShouldBe(5);
        result.HasMore.ShouldBeTrue();
    }

    [Fact]
    public async Task GetArchivedEvents_SearchByName_ReturnsFilteredPaginatedResult()
    {
        await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);

        var raid = await CreateAndSaveTestEvent("Weekly Raid Night", startDate: DateTime.UtcNow.AddDays(-7));
        var social = await CreateAndSaveTestEvent("Social Gathering", startDate: DateTime.UtcNow.AddDays(-14));
        await ArchiveEvent(raid);
        await ArchiveEvent(social);

        var response = await Client.GetAsync("api/Events/archived?searchText=Raid");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<EventResponse>>();
        result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(1);
        result.TotalCount.ShouldBe(1);
        result.Items[0].Name.ShouldBe("Weekly Raid Night");
    }

    #endregion

    #region Signup Flow Tests

    [Fact]
    public async Task SignupForEvent_NewSignup_CreatesSignup()
    {
        var member = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        var fcEvent = CreateTestEvent();
        await _eventService.CreateAsync(fcEvent);

        var response = await Client.PostAsJsonAsync(
            $"api/Events/{fcEvent.Id}/signup",
            new EventSignupDto { Roles = new List<Role> { Role.Tank, Role.Healer } });

        response.EnsureSuccessStatusCode();

        var updatedEvent = await _eventService.GetAsync(fcEvent.Id);
        var signup = updatedEvent.Signups.FirstOrDefault(s => s.DiscordUserId == member.DiscordId);
        signup.ShouldNotBeNull();
        signup.Roles.Count.ShouldBe(2);
        signup.Roles.ShouldContain(Role.Tank);
        signup.Roles.ShouldContain(Role.Healer);
    }

    [Fact]
    public async Task SignupForEvent_ExistingSignup_UpdatesRoles()
    {
        var member = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        var fcEvent = CreateTestEvent();
        fcEvent.Signups.Add(new EventSignup
        {
            DiscordUserId = member.DiscordId,
            Roles = new List<Role> { Role.Tank },
            SignupDate = DateTime.UtcNow
        });
        await _eventService.CreateAsync(fcEvent);

        var response = await Client.PostAsJsonAsync(
            $"api/Events/{fcEvent.Id}/signup",
            new EventSignupDto { Roles = new List<Role> { Role.Healer, Role.Caster } });

        response.EnsureSuccessStatusCode();

        var updatedEvent = await _eventService.GetAsync(fcEvent.Id);
        var signups = updatedEvent.Signups.Where(s => s.DiscordUserId == member.DiscordId).ToList();
        signups.Count.ShouldBe(1);
        signups[0].Roles.Count.ShouldBe(2);
        signups[0].Roles.ShouldContain(Role.Healer);
        signups[0].Roles.ShouldContain(Role.Caster);
    }

    [Fact]
    public async Task CancelSignup_RemovesSignup()
    {
        var member = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        var fcEvent = CreateTestEvent();
        fcEvent.Signups.Add(new EventSignup
        {
            DiscordUserId = member.DiscordId,
            Roles = new List<Role> { Role.Tank },
            SignupDate = DateTime.UtcNow
        });
        await _eventService.CreateAsync(fcEvent);

        var response = await Client.DeleteAsync($"api/Events/{fcEvent.Id}/signup");

        response.EnsureSuccessStatusCode();

        var updatedEvent = await _eventService.GetAsync(fcEvent.Id);
        updatedEvent.Signups.Any(s => s.DiscordUserId == member.DiscordId).ShouldBeFalse();
    }

    [Fact]
    public async Task CancelSignup_NonExistentSignup_ReturnsOk()
    {
        await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        var fcEvent = CreateTestEvent();
        await _eventService.CreateAsync(fcEvent);

        var response = await Client.DeleteAsync($"api/Events/{fcEvent.Id}/signup");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SignupForEvent_NonExistentEvent_ReturnsNotFound()
    {
        await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        var nonExistentId = "507f1f77bcf86cd799439011";

        var response = await Client.PostAsJsonAsync(
            $"api/Events/{nonExistentId}/signup",
            new EventSignupDto { Roles = new List<Role> { Role.Tank } });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region Event CRUD Tests

    [Fact]
    public async Task GetEvent_ReturnsEvent()
    {
        await AuthenticateAsMember();
        var fcEvent = CreateTestEvent();
        await _eventService.CreateAsync(fcEvent);

        var response = await Client.GetAsync($"api/Events/{fcEvent.Id}");

        response.EnsureSuccessStatusCode();
        var retrievedEvent = await response.Content.ReadFromJsonAsync<EventResponse>();
        retrievedEvent.ShouldNotBeNull();
        retrievedEvent.Id.ShouldBe(fcEvent.Id);
        retrievedEvent.Name.ShouldBe(fcEvent.Name);
    }

    [Fact]
    public async Task GetEvent_NotFound_ReturnsNotFound()
    {
        await AuthenticateAsMember();
        var nonExistentId = "507f1f77bcf86cd799439011";

        var response = await Client.GetAsync($"api/Events/{nonExistentId}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteEvent_RemovesEvent()
    {
        await AuthenticateAsAdmin();
        var fcEvent = CreateTestEvent();
        await _eventService.CreateAsync(fcEvent);

        var response = await Client.DeleteAsync($"api/Events/{fcEvent.Id}");

        response.EnsureSuccessStatusCode();

        var getResponse = await Client.GetAsync($"api/Events/{fcEvent.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteEvent_AsMember_ReturnsForbidden()
    {
        await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        var fcEvent = CreateTestEvent();
        await _eventService.CreateAsync(fcEvent);

        var response = await Client.DeleteAsync($"api/Events/{fcEvent.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteEvent_NonExistent_ReturnsNotFound()
    {
        await AuthenticateAsAdmin();
        var nonExistentId = "507f1f77bcf86cd799439011";

        var response = await Client.DeleteAsync($"api/Events/{nonExistentId}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateEvent_AsAdmin_UpdatesFields()
    {
        await AuthenticateAsAdmin();
        var fcEvent = await CreateAndSaveTestEvent("Original Name");

        var updateRequest = new UpdateEventRequest
        {
            Name = "Updated Name",
            Description = "Updated description",
            Type = EventType.Social,
            StartDate = fcEvent.StartDate,
            Duration = 180,
            ICalString = "",
            SignupType = SignupType.SingleEvent,
            MaxNumberOfParticipants = 16
        };

        var response = await Client.PutAsJsonAsync($"api/Events/{fcEvent.Id}", updateRequest);

        response.EnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<EventResponse>();
        updated.ShouldNotBeNull();
        updated.Name.ShouldBe("Updated Name");
        updated.Description.ShouldBe("Updated description");
        updated.Type.ShouldBe(EventType.Social);
        updated.Duration.ShouldBe(180);
    }

    [Fact]
    public async Task UpdateEvent_AsMember_ReturnsForbidden()
    {
        await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        var fcEvent = await CreateAndSaveTestEvent();

        var updateRequest = new UpdateEventRequest
        {
            Name = "Updated",
            Description = "",
            Type = EventType.Raid,
            StartDate = DateTime.UtcNow,
            Duration = 60,
            ICalString = "",
            SignupType = SignupType.SingleEvent,
            MaxNumberOfParticipants = 8
        };

        var response = await Client.PutAsJsonAsync($"api/Events/{fcEvent.Id}", updateRequest);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateEvent_NonExistent_ReturnsNotFound()
    {
        await AuthenticateAsAdmin();
        var nonExistentId = "507f1f77bcf86cd799439011";

        var updateRequest = new UpdateEventRequest
        {
            Name = "Updated",
            Description = "",
            Type = EventType.Raid,
            StartDate = DateTime.UtcNow,
            Duration = 60,
            ICalString = "",
            SignupType = SignupType.SingleEvent,
            MaxNumberOfParticipants = 8
        };

        var response = await Client.PutAsJsonAsync($"api/Events/{nonExistentId}", updateRequest);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateEvent_PreservesSignupsAndGroups()
    {
        var admin = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);
        var fcEvent = CreateTestEvent();
        fcEvent.Signups.Add(new EventSignup
        {
            DiscordUserId = admin.DiscordId,
            Roles = new List<Role> { Role.Tank },
            SignupDate = DateTime.UtcNow
        });
        await _eventService.CreateAsync(fcEvent);

        var updateRequest = new UpdateEventRequest
        {
            Name = "Updated Name",
            Description = "Updated",
            Type = EventType.Raid,
            StartDate = fcEvent.StartDate,
            Duration = fcEvent.Duration,
            ICalString = "",
            SignupType = fcEvent.SignupType,
            MaxNumberOfParticipants = fcEvent.MaxNumberOfParticipants
        };

        var response = await Client.PutAsJsonAsync($"api/Events/{fcEvent.Id}", updateRequest);

        response.EnsureSuccessStatusCode();

        var saved = await _eventService.GetAsync(fcEvent.Id);
        saved.Name.ShouldBe("Updated Name");
        saved.Signups.Count.ShouldBe(1);
        saved.Signups[0].DiscordUserId.ShouldBe(admin.DiscordId);
    }

    #endregion

    #region Occurrence Management Tests

    [Fact]
    public async Task UpdateOccurrenceStatus_ValidTransition_UpdatesStatus()
    {
        var admin = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);
        var fcEvent = CreateTestEvent(startDate: DateTime.UtcNow.AddHours(-1));
        await _eventService.CreateAsync(fcEvent);
        var occurrenceId = fcEvent.Occurrences[0].Id;

        var response = await Client.PatchAsJsonAsync(
            $"api/Events/{fcEvent.Id}/occurrences/{occurrenceId}/status",
            new UpdateOccurrenceStatusRequest { Status = OccurrenceStatus.Completed });

        response.EnsureSuccessStatusCode();

        var updatedEvent = await _eventService.GetAsync(fcEvent.Id);
        var occurrence = updatedEvent!.Occurrences.First(o => o.Id == occurrenceId);
        occurrence.Status.ShouldBe(OccurrenceStatus.Completed);
    }

    [Fact]
    public async Task UpdateOccurrenceStatus_FutureCompleted_ReturnsBadRequest()
    {
        var admin = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);
        var fcEvent = CreateTestEvent(startDate: DateTime.UtcNow.AddDays(1));
        await _eventService.CreateAsync(fcEvent);
        var occurrenceId = fcEvent.Occurrences[0].Id;

        var response = await Client.PatchAsJsonAsync(
            $"api/Events/{fcEvent.Id}/occurrences/{occurrenceId}/status",
            new UpdateOccurrenceStatusRequest { Status = OccurrenceStatus.Completed });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CancelOccurrence_SetsStatusToCancelled()
    {
        var admin = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);
        var fcEvent = CreateTestEvent();
        await _eventService.CreateAsync(fcEvent);
        var occurrenceId = fcEvent.Occurrences[0].Id;

        var response = await Client.DeleteAsync(
            $"api/Events/{fcEvent.Id}/occurrences/{occurrenceId}");

        response.EnsureSuccessStatusCode();

        var updatedEvent = await _eventService.GetAsync(fcEvent.Id);
        var occurrence = updatedEvent!.Occurrences.First(o => o.Id == occurrenceId);
        occurrence.Status.ShouldBe(OccurrenceStatus.Cancelled);
    }

    [Fact]
    public async Task CancelOccurrence_AsMember_ReturnsForbidden()
    {
        var member = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        var fcEvent = CreateTestEvent();
        await _eventService.CreateAsync(fcEvent);
        var occurrenceId = fcEvent.Occurrences[0].Id;

        var response = await Client.DeleteAsync(
            $"api/Events/{fcEvent.Id}/occurrences/{occurrenceId}");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateOccurrenceStatus_AutoArchivesWhenAllCompleted()
    {
        var admin = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);
        var fcEvent = CreateTestEvent(startDate: DateTime.UtcNow.AddHours(-1));
        await _eventService.CreateAsync(fcEvent);
        var occurrenceId = fcEvent.Occurrences[0].Id;

        await Client.PatchAsJsonAsync(
            $"api/Events/{fcEvent.Id}/occurrences/{occurrenceId}/status",
            new UpdateOccurrenceStatusRequest { Status = OccurrenceStatus.Completed });

        var updatedEvent = await _eventService.GetAsync(fcEvent.Id);
        updatedEvent!.IsArchived.ShouldBeTrue();
    }

    #endregion

    #region Archive/Restore Tests

    [Fact]
    public async Task ArchiveEvent_AsAdmin_AllOccurrencesCompleted_Succeeds()
    {
        var admin = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);
        var fcEvent = await CreateAndSaveTestEvent(startDate: DateTime.UtcNow.AddDays(-7));

        var savedEvent = await _eventService.GetAsync(fcEvent.Id);
        foreach (var occurrence in savedEvent!.Occurrences) occurrence.Status = OccurrenceStatus.Completed;
        var eventRepository = Factory.Services.GetRequiredService<IEventRepository>();
        await eventRepository.UpdateAsync(fcEvent.Id, savedEvent);

        var response = await Client.PostAsync($"api/Events/{fcEvent.Id}/archive", null);

        response.EnsureSuccessStatusCode();

        var archivedEvent = await _eventService.GetAsync(fcEvent.Id);
        archivedEvent!.IsArchived.ShouldBeTrue();
        archivedEvent.ArchivedDate.ShouldNotBeNull();
        archivedEvent.ArchivedByUserId.ShouldBe(admin.DiscordId);
    }

    [Fact]
    public async Task ArchiveEvent_AsAdmin_HasScheduledOccurrences_ReturnsBadRequest()
    {
        await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);
        var fcEvent = await CreateAndSaveTestEvent(startDate: DateTime.UtcNow.AddDays(1));

        var response = await Client.PostAsync($"api/Events/{fcEvent.Id}/archive", null);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ArchiveEvent_AsMember_ReturnsForbidden()
    {
        await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        var fcEvent = await CreateAndSaveTestEvent(startDate: DateTime.UtcNow.AddDays(-7));

        var savedEvent = await _eventService.GetAsync(fcEvent.Id);
        foreach (var occurrence in savedEvent!.Occurrences) occurrence.Status = OccurrenceStatus.Completed;
        var eventRepository = Factory.Services.GetRequiredService<IEventRepository>();
        await eventRepository.UpdateAsync(fcEvent.Id, savedEvent);

        var response = await Client.PostAsync($"api/Events/{fcEvent.Id}/archive", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RestoreEvent_AsAdmin_ArchivedEvent_Succeeds()
    {
        var admin = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);
        var fcEvent = await CreateAndSaveTestEvent(startDate: DateTime.UtcNow.AddDays(-7));

        var savedEvent = await _eventService.GetAsync(fcEvent.Id);
        foreach (var occurrence in savedEvent!.Occurrences) occurrence.Status = OccurrenceStatus.Completed;
        savedEvent.IsArchived = true;
        savedEvent.ArchivedDate = DateTime.UtcNow;
        savedEvent.ArchivedByUserId = admin.DiscordId;
        var eventRepository = Factory.Services.GetRequiredService<IEventRepository>();
        await eventRepository.UpdateAsync(fcEvent.Id, savedEvent);

        var response = await Client.PostAsync($"api/Events/{fcEvent.Id}/restore", null);

        response.EnsureSuccessStatusCode();

        var restoredEvent = await _eventService.GetAsync(fcEvent.Id);
        restoredEvent!.IsArchived.ShouldBeFalse();
        restoredEvent.ArchivedDate.ShouldBeNull();
        restoredEvent.ArchivedByUserId.ShouldBeNull();
    }

    [Fact]
    public async Task RestoreEvent_AsAdmin_NotArchivedEvent_ReturnsBadRequest()
    {
        await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);
        var fcEvent = await CreateAndSaveTestEvent();

        var response = await Client.PostAsync($"api/Events/{fcEvent.Id}/restore", null);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Extend Event Tests

    [Fact]
    public async Task ExtendEvent_AsAdmin_RecurringEvent_AddsOccurrences()
    {
        await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);
        var fcEvent = await CreateAndSaveRecurringTestEvent(weeklyCount: 3);
        var originalOccurrenceCount = fcEvent.Occurrences.Count;

        var response = await Client.PostAsJsonAsync(
            $"api/Events/{fcEvent.Id}/extend",
            new ExtendEventRequest { Count = 2 });

        response.EnsureSuccessStatusCode();

        var updatedEvent = await _eventService.GetAsync(fcEvent.Id);
        updatedEvent!.Occurrences.Count.ShouldBe(originalOccurrenceCount + 2);
    }

    [Fact]
    public async Task ExtendEvent_AsAdmin_NonRecurringEvent_ReturnsBadRequest()
    {
        await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);
        var fcEvent = await CreateAndSaveTestEvent();

        var response = await Client.PostAsJsonAsync(
            $"api/Events/{fcEvent.Id}/extend",
            new ExtendEventRequest { Count = 2 });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ExtendEvent_AsMember_ReturnsForbidden()
    {
        await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        var fcEvent = await CreateAndSaveRecurringTestEvent(weeklyCount: 3);

        var response = await Client.PostAsJsonAsync(
            $"api/Events/{fcEvent.Id}/extend",
            new ExtendEventRequest { Count = 2 });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ExtendEvent_InvalidCount_ReturnsBadRequest()
    {
        await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);
        var fcEvent = await CreateAndSaveRecurringTestEvent(weeklyCount: 3);

        var response = await Client.PostAsJsonAsync(
            $"api/Events/{fcEvent.Id}/extend",
            new ExtendEventRequest { Count = 0 });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ExtendEvent_NonExistentEvent_ReturnsBadRequest()
    {
        await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);
        var nonExistentId = "507f1f77bcf86cd799439011";

        var response = await Client.PostAsJsonAsync(
            $"api/Events/{nonExistentId}/extend",
            new ExtendEventRequest { Count = 2 });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Manual Signup Tests

    [Fact]
    public async Task ManualSignup_AsAdmin_CreatesSignup()
    {
        await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);
        var fcEvent = CreateTestEvent();
        await _eventService.CreateAsync(fcEvent);

        var targetDiscordId = GenerateRandomDiscordId();
        var response = await Client.PostAsJsonAsync(
            $"api/Events/{fcEvent.Id}/signup/manual",
            new EventSignupDto
            {
                DiscordUserId = targetDiscordId,
                Roles = new List<Role> { Role.Tank, Role.Healer }
            });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var updatedEvent = await _eventService.GetAsync(fcEvent.Id);
        var signup = updatedEvent.Signups.FirstOrDefault(s => s.DiscordUserId == targetDiscordId);
        signup.ShouldNotBeNull();
        signup.Roles.Count.ShouldBe(2);
        signup.Roles.ShouldContain(Role.Tank);
        signup.Roles.ShouldContain(Role.Healer);
    }

    [Fact]
    public async Task ManualSignup_AsMember_ReturnsForbidden()
    {
        await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        var fcEvent = CreateTestEvent();
        await _eventService.CreateAsync(fcEvent);

        var response = await Client.PostAsJsonAsync(
            $"api/Events/{fcEvent.Id}/signup/manual",
            new EventSignupDto
            {
                DiscordUserId = GenerateRandomDiscordId(),
                Roles = new List<Role> { Role.Tank }
            });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ManualSignup_Unauthenticated_ReturnsUnauthorized()
    {
        SetUnauthenticated();
        var fcEvent = CreateTestEvent();
        await _eventService.CreateAsync(fcEvent);

        var response = await Client.PostAsJsonAsync(
            $"api/Events/{fcEvent.Id}/signup/manual",
            new EventSignupDto
            {
                DiscordUserId = GenerateRandomDiscordId(),
                Roles = new List<Role> { Role.Tank }
            });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ManualSignup_NonExistentEvent_ReturnsNotFound()
    {
        await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);
        var nonExistentId = "507f1f77bcf86cd799439011";

        var response = await Client.PostAsJsonAsync(
            $"api/Events/{nonExistentId}/signup/manual",
            new EventSignupDto
            {
                DiscordUserId = GenerateRandomDiscordId(),
                Roles = new List<Role> { Role.Tank }
            });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ManualSignup_MissingDiscordUserId_ReturnsBadRequest()
    {
        await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);
        var fcEvent = CreateTestEvent();
        await _eventService.CreateAsync(fcEvent);

        var response = await Client.PostAsJsonAsync(
            $"api/Events/{fcEvent.Id}/signup/manual",
            new EventSignupDto
            {
                DiscordUserId = null,
                Roles = new List<Role> { Role.Tank }
            });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ManualSignup_EmptyRoles_ReturnsBadRequest()
    {
        await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);
        var fcEvent = CreateTestEvent();
        await _eventService.CreateAsync(fcEvent);

        var response = await Client.PostAsJsonAsync(
            $"api/Events/{fcEvent.Id}/signup/manual",
            new EventSignupDto
            {
                DiscordUserId = GenerateRandomDiscordId(),
                Roles = new List<Role>()
            });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ManualSignup_ExistingSignup_UpdatesRoles()
    {
        await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);
        var targetDiscordId = GenerateRandomDiscordId();
        var fcEvent = CreateTestEvent();
        fcEvent.Signups.Add(new EventSignup
        {
            DiscordUserId = targetDiscordId,
            Roles = new List<Role> { Role.Tank },
            SignupDate = DateTime.UtcNow
        });
        await _eventService.CreateAsync(fcEvent);

        var response = await Client.PostAsJsonAsync(
            $"api/Events/{fcEvent.Id}/signup/manual",
            new EventSignupDto
            {
                DiscordUserId = targetDiscordId,
                Roles = new List<Role> { Role.Healer, Role.Caster }
            });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var updatedEvent = await _eventService.GetAsync(fcEvent.Id);
        var signups = updatedEvent.Signups.Where(s => s.DiscordUserId == targetDiscordId).ToList();
        signups.Count.ShouldBe(1);
        signups[0].Roles.Count.ShouldBe(2);
        signups[0].Roles.ShouldContain(Role.Healer);
        signups[0].Roles.ShouldContain(Role.Caster);
    }

    [Fact]
    public async Task ManualSignup_DoesNotAffectOtherSignups()
    {
        await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);
        var existingUserId = GenerateRandomDiscordId();
        var newUserId = GenerateRandomDiscordId();

        var fcEvent = CreateTestEvent();
        fcEvent.Signups.Add(new EventSignup
        {
            DiscordUserId = existingUserId,
            Roles = new List<Role> { Role.Tank },
            SignupDate = DateTime.UtcNow
        });
        await _eventService.CreateAsync(fcEvent);

        var response = await Client.PostAsJsonAsync(
            $"api/Events/{fcEvent.Id}/signup/manual",
            new EventSignupDto
            {
                DiscordUserId = newUserId,
                Roles = new List<Role> { Role.Melee }
            });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var updatedEvent = await _eventService.GetAsync(fcEvent.Id);
        updatedEvent.Signups.Count.ShouldBe(2);
        updatedEvent.Signups.Any(s => s.DiscordUserId == existingUserId).ShouldBeTrue();
        updatedEvent.Signups.Any(s => s.DiscordUserId == newUserId).ShouldBeTrue();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task SelectParticipants_NonExistentEvent_ReturnsNotFound()
    {
        await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);
        var nonExistentId = "507f1f77bcf86cd799439011";

        var groups = new List<EventGroupRequest>
        {
            new()
            {
                Name = "Group 1",
                Participants = new List<EventParticipantDto>()
            }
        };

        var response = await Client.PostAsJsonAsync(
            $"api/Events/{nonExistentId}/participants",
            groups);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetEvents_EmptyDatabase_ReturnsEmptyPagedResult()
    {
        await AuthenticateAsMember();

        var response = await Client.GetAsync("api/Events");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<EventResponse>>();
        result.ShouldNotBeNull();
        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
        result.HasMore.ShouldBeFalse();
    }

    [Fact]
    public async Task GetEvents_BeyondLastPage_ReturnsEmptyItems()
    {
        await AuthenticateAsMember();
        var fcEvent = CreateTestEvent();
        await _eventService.CreateAsync(fcEvent);

        var response = await Client.GetAsync("api/Events?page=100&pageSize=10");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<EventResponse>>();
        result.ShouldNotBeNull();
        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(1);
    }

    #endregion
}
