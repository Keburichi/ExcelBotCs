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

    /// <summary>
    ///     Creates a test event (NOT saved). Use CreateAndSaveTestEvent for saved events.
    ///     Note: The occurrence IDs will be regenerated when saved via _eventService.CreateAsync
    /// </summary>
    private Event CreateTestEvent(
        string name = "Test Event",
        SignupType signupType = SignupType.IndependentSignups,
        int maxParticipants = 8,
        DateTime? startDate = null,
        int occurrenceCount = 1)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(1);

        // For multiple occurrences, use iCal recurrence rule (weekly)
        // This ensures EventService.CreateAsync generates the correct number of occurrences
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

        // Pre-create occurrences so tests can add signups/participants before saving
        // Note: EventService.CreateAsync will regenerate these with new IDs
        for (var i = 0; i < occurrenceCount; i++)
            fcEvent.Occurrences.Add(new EventOccurrence
            {
                OccurrenceDate = start.AddDays(i * 7),
                Status = OccurrenceStatus.Scheduled
            });

        return fcEvent;
    }

    /// <summary>
    ///     Creates a test event and saves it to the database.
    ///     Returns the saved event with actual occurrence IDs (since CreateAsync regenerates occurrences).
    /// </summary>
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

        // Return the saved event with actual occurrence IDs
        return (await _eventService.GetAsync(fcEvent.Id))!;
    }

    /// <summary>
    ///     Creates a test event with multiple occurrences by using a recurrence rule.
    /// </summary>
    private async Task<Event> CreateAndSaveRecurringTestEvent(
        string name = "Test Recurring Event",
        SignupType signupType = SignupType.IndependentSignups,
        int maxParticipants = 8,
        DateTime? startDate = null,
        int weeklyCount = 3)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(1);

        // Create iCal string for weekly recurrence
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

        // Return the saved event with actual occurrence IDs
        return (await _eventService.GetAsync(fcEvent.Id))!;
    }

    #endregion

    #region Authentication Tests

    [Fact]
    public async Task SignupForOccurrence_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        SetUnauthenticated();
        var fcEvent = CreateTestEvent();
        await _eventService.CreateAsync(fcEvent);
        var occurrenceId = fcEvent.Occurrences[0].Id;

        // Act
        var response = await Client.PostAsJsonAsync(
            $"api/Events/{fcEvent.Id}/occurrences/{occurrenceId}/signup",
            new EventSignupDto { Roles = new List<Role> { Role.Tank } });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SignupForOccurrence_AsMember_ReturnsOk()
    {
        // Arrange
        await AuthenticateAsMember();
        var fcEvent = CreateTestEvent();
        await _eventService.CreateAsync(fcEvent);

        // Get the actual occurrence ID from saved event
        var savedEvent = await _eventService.GetAsync(fcEvent.Id);
        var occurrenceId = savedEvent!.Occurrences[0].Id;

        // Act
        var response = await Client.PostAsJsonAsync(
            $"api/Events/{fcEvent.Id}/occurrences/{occurrenceId}/signup",
            new EventSignupDto { Roles = new List<Role> { Role.Tank } });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SelectParticipants_AsMember_ReturnsForbidden()
    {
        // Arrange
        var member = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        var fcEvent = CreateTestEvent();
        await _eventService.CreateAsync(fcEvent);
        var occurrenceId = fcEvent.Occurrences[0].Id;

        var participants = new List<EventParticipant>
        {
            new() { DiscordUserId = member.DiscordId, Role = Role.Tank }
        };

        // Act
        var response = await Client.PostAsJsonAsync(
            $"api/Events/{fcEvent.Id}/occurrences/{occurrenceId}/participants",
            participants);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SelectParticipants_AsAdmin_ReturnsOk()
    {
        // Arrange
        var admin = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);
        var fcEvent = CreateTestEvent();
        await _eventService.CreateAsync(fcEvent);
        var occurrenceId = fcEvent.Occurrences[0].Id;

        var participants = new List<EventParticipant>
        {
            new() { DiscordUserId = admin.DiscordId, Role = Role.Tank }
        };

        // Act
        var response = await Client.PostAsJsonAsync(
            $"api/Events/{fcEvent.Id}/occurrences/{occurrenceId}/participants",
            participants);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    #endregion

    #region Signup Flow Tests

    [Fact]
    public async Task SignupForEvent_NewSignup_CreatesSignup()
    {
        // Arrange
        var member = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        var fcEvent = CreateTestEvent();
        await _eventService.CreateAsync(fcEvent);

        // Act
        var response = await Client.PostAsJsonAsync(
            $"api/Events/{fcEvent.Id}/signup",
            new EventSignupDto { Roles = new List<Role> { Role.Tank, Role.Healer } });

        // Assert
        response.EnsureSuccessStatusCode();

        // Verify in database
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
        // Arrange
        var member = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        var fcEvent = CreateTestEvent();
        fcEvent.Signups.Add(new EventSignup
        {
            DiscordUserId = member.DiscordId,
            Roles = new List<Role> { Role.Tank },
            SignupDate = DateTime.UtcNow
        });
        await _eventService.CreateAsync(fcEvent);
        var occurrenceId = fcEvent.Occurrences[0].Id;

        // Act
        var response = await Client.PostAsJsonAsync(
            $"api/Events/{fcEvent.Id}/signup",
            new EventSignupDto { Roles = new List<Role> { Role.Healer, Role.Caster } });

        // Assert
        response.EnsureSuccessStatusCode();

        var updatedEvent = await _eventService.GetAsync(fcEvent.Id);
        var signups = updatedEvent.Signups.Where(s => s.DiscordUserId == member.DiscordId).ToList();

        // Should only have one signup entry
        signups.Count.ShouldBe(1);
        signups[0].Roles.Count.ShouldBe(2);
        signups[0].Roles.ShouldContain(Role.Healer);
        signups[0].Roles.ShouldContain(Role.Caster);
    }

    [Fact]
    public async Task SignupForOccurrence_PastOccurrence_ReturnsBadRequest()
    {
        // Arrange
        var member = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        var fcEvent = CreateTestEvent(startDate: DateTime.UtcNow.AddDays(-2));
        await _eventService.CreateAsync(fcEvent);
        var occurrenceId = fcEvent.Occurrences[0].Id;

        // Act
        var response = await Client.PostAsJsonAsync(
            $"api/Events/{fcEvent.Id}/occurrences/{occurrenceId}/signup",
            new EventSignupDto { Roles = new List<Role> { Role.Tank } });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SignupForOccurrence_CancelledOccurrence_ReturnsBadRequest()
    {
        // Arrange - Use admin to cancel the occurrence, then regular member to try signup
        var admin = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);
        var fcEvent = CreateTestEvent();
        await _eventService.CreateAsync(fcEvent);

        // Get the created event to get the actual occurrence ID
        var savedEvent = await _eventService.GetAsync(fcEvent.Id);
        var occurrenceId = savedEvent!.Occurrences[0].Id;

        // Directly update the occurrence status in the database (bypasses EventService.UpdateAsync issues)
        savedEvent.Occurrences[0].Status = OccurrenceStatus.Cancelled;
        var eventRepository = Factory.Services.GetRequiredService<IEventRepository>();
        await eventRepository.UpdateAsync(fcEvent.Id, savedEvent);

        // Verify the status was updated
        savedEvent = await _eventService.GetAsync(fcEvent.Id);
        savedEvent!.Occurrences[0].Status.ShouldBe(OccurrenceStatus.Cancelled,
            "Occurrence status should be Cancelled after direct update");

        // Create a new member to try to sign up
        var member = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());

        // Act - Try to sign up for the cancelled occurrence
        var response = await Client.PostAsJsonAsync(
            $"api/Events/{fcEvent.Id}/occurrences/{occurrenceId}/signup",
            new EventSignupDto { Roles = new List<Role> { Role.Tank } });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    #endregion

    #region LockedGroup Signup Tests

    #endregion

    #region Participant Selection Tests

    #endregion

    #region Occurrence Management Tests

    [Fact]
    public async Task UpdateOccurrenceStatus_ValidTransition_UpdatesStatus()
    {
        // Arrange
        var admin = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);
        var fcEvent = CreateTestEvent(startDate: DateTime.UtcNow.AddHours(-1)); // Past occurrence
        await _eventService.CreateAsync(fcEvent);
        var occurrenceId = fcEvent.Occurrences[0].Id;

        // Act
        var response = await Client.PatchAsJsonAsync(
            $"api/Events/{fcEvent.Id}/occurrences/{occurrenceId}/status",
            new OccurrenceStatusUpdateDto { Status = OccurrenceStatus.Completed });

        // Assert
        response.EnsureSuccessStatusCode();

        var updatedEvent = await _eventService.GetAsync(fcEvent.Id);
        var occurrence = updatedEvent!.Occurrences.First(o => o.Id == occurrenceId);

        occurrence.Status.ShouldBe(OccurrenceStatus.Completed);
    }

    [Fact]
    public async Task UpdateOccurrenceStatus_FutureCompleted_ReturnsBadRequest()
    {
        // Arrange
        var admin = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);
        var fcEvent = CreateTestEvent(startDate: DateTime.UtcNow.AddDays(1)); // Future occurrence
        await _eventService.CreateAsync(fcEvent);
        var occurrenceId = fcEvent.Occurrences[0].Id;

        // Act
        var response = await Client.PatchAsJsonAsync(
            $"api/Events/{fcEvent.Id}/occurrences/{occurrenceId}/status",
            new OccurrenceStatusUpdateDto { Status = OccurrenceStatus.Completed });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CancelOccurrence_SetsStatusToCancelled()
    {
        // Arrange
        var admin = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);
        var fcEvent = CreateTestEvent();
        await _eventService.CreateAsync(fcEvent);
        var occurrenceId = fcEvent.Occurrences[0].Id;

        // Act
        var response = await Client.DeleteAsync(
            $"api/Events/{fcEvent.Id}/occurrences/{occurrenceId}");

        // Assert
        response.EnsureSuccessStatusCode();

        var updatedEvent = await _eventService.GetAsync(fcEvent.Id);
        var occurrence = updatedEvent!.Occurrences.First(o => o.Id == occurrenceId);

        occurrence.Status.ShouldBe(OccurrenceStatus.Cancelled);
    }

    [Fact]
    public async Task CancelOccurrence_AsMember_ReturnsForbidden()
    {
        // Arrange
        var member = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        var fcEvent = CreateTestEvent();
        await _eventService.CreateAsync(fcEvent);
        var occurrenceId = fcEvent.Occurrences[0].Id;

        // Act
        var response = await Client.DeleteAsync(
            $"api/Events/{fcEvent.Id}/occurrences/{occurrenceId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Event CRUD Tests

    [Fact]
    public async Task GetEvents_ReturnsAllEvents()
    {
        // Arrange
        await AuthenticateAsMember();
        var event1 = CreateTestEvent("Event 1");
        var event2 = CreateTestEvent("Event 2");
        await _eventService.CreateAsync(event1);
        await _eventService.CreateAsync(event2);

        // Act
        var response = await Client.GetAsync("api/Events");

        // Assert
        response.EnsureSuccessStatusCode();
        var events = await response.Content.ReadFromJsonAsync<List<EventResponse>>();
        events.ShouldNotBeNull();
        events.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetEvent_ReturnsEvent()
    {
        // Arrange
        await AuthenticateAsMember();
        var fcEvent = CreateTestEvent();
        await _eventService.CreateAsync(fcEvent);

        // Act
        var response = await Client.GetAsync($"api/Events/{fcEvent.Id}");

        // Assert
        response.EnsureSuccessStatusCode();
        var retrievedEvent = await response.Content.ReadFromJsonAsync<EventResponse>();
        retrievedEvent.ShouldNotBeNull();
        retrievedEvent.Id.ShouldBe(fcEvent.Id);
        retrievedEvent.Name.ShouldBe(fcEvent.Name);
    }

    [Fact]
    public async Task GetEvent_NotFound_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateAsMember();
        var nonExistentId = "507f1f77bcf86cd799439011";

        // Act
        var response = await Client.GetAsync($"api/Events/{nonExistentId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteEvent_RemovesEvent()
    {
        // Arrange
        await AuthenticateAsAdmin();
        var fcEvent = CreateTestEvent();
        await _eventService.CreateAsync(fcEvent);

        // Act
        var response = await Client.DeleteAsync($"api/Events/{fcEvent.Id}");

        // Assert
        response.EnsureSuccessStatusCode();

        var getResponse = await Client.GetAsync($"api/Events/{fcEvent.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task SignupForOccurrence_NonExistentEvent_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateAsMember();
        var nonExistentId = "507f1f77bcf86cd799439011";

        // Act
        var response = await Client.PostAsJsonAsync(
            $"api/Events/{nonExistentId}/occurrences/some-occurrence/signup",
            new EventSignupDto { Roles = new List<Role> { Role.Tank } });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SignupForOccurrence_NonExistentOccurrence_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateAsMember();
        var fcEvent = CreateTestEvent();
        await _eventService.CreateAsync(fcEvent);

        // Act
        var response = await Client.PostAsJsonAsync(
            $"api/Events/{fcEvent.Id}/occurrences/non-existent-occurrence/signup",
            new EventSignupDto { Roles = new List<Role> { Role.Tank } });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SelectParticipants_NonExistentEvent_ReturnsNotFound()
    {
        // Arrange
        var admin = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);
        var nonExistentId = "507f1f77bcf86cd799439011";

        // Act
        var response = await Client.PostAsJsonAsync(
            $"api/Events/{nonExistentId}/occurrences/some-occurrence/participants",
            new List<EventParticipant>());

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SelectParticipants_NonExistentOccurrence_ReturnsNotFound()
    {
        // Arrange
        var admin = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);
        var fcEvent = CreateTestEvent();
        await _eventService.CreateAsync(fcEvent);

        // Act
        var response = await Client.PostAsJsonAsync(
            $"api/Events/{fcEvent.Id}/occurrences/non-existent-occurrence/participants",
            new List<EventParticipant>());

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region Archive/Restore Tests

    [Fact]
    public async Task ArchiveEvent_AsAdmin_AllOccurrencesCompleted_Succeeds()
    {
        // Arrange
        var admin = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);
        var fcEvent = await CreateAndSaveTestEvent(startDate: DateTime.UtcNow.AddDays(-7));

        // Mark all occurrences as completed
        var savedEvent = await _eventService.GetAsync(fcEvent.Id);
        foreach (var occurrence in savedEvent!.Occurrences) occurrence.Status = OccurrenceStatus.Completed;
        var eventRepository = Factory.Services.GetRequiredService<IEventRepository>();
        await eventRepository.UpdateAsync(fcEvent.Id, savedEvent);

        // Act
        var response = await Client.PostAsync($"api/Events/{fcEvent.Id}/archive", null);

        // Assert
        response.EnsureSuccessStatusCode();

        var archivedEvent = await _eventService.GetAsync(fcEvent.Id);
        archivedEvent!.IsArchived.ShouldBeTrue();
        archivedEvent.ArchivedDate.ShouldNotBeNull();
        archivedEvent.ArchivedByUserId.ShouldBe(admin.DiscordId);
    }

    [Fact]
    public async Task ArchiveEvent_AsAdmin_HasScheduledOccurrences_ReturnsBadRequest()
    {
        // Arrange
        await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);
        var fcEvent = await CreateAndSaveTestEvent(startDate: DateTime.UtcNow.AddDays(1));

        // Act
        var response = await Client.PostAsync($"api/Events/{fcEvent.Id}/archive", null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ArchiveEvent_AsMember_ReturnsForbidden()
    {
        // Arrange
        await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        var fcEvent = await CreateAndSaveTestEvent(startDate: DateTime.UtcNow.AddDays(-7));

        // Mark all occurrences as completed
        var savedEvent = await _eventService.GetAsync(fcEvent.Id);
        foreach (var occurrence in savedEvent!.Occurrences) occurrence.Status = OccurrenceStatus.Completed;
        var eventRepository = Factory.Services.GetRequiredService<IEventRepository>();
        await eventRepository.UpdateAsync(fcEvent.Id, savedEvent);

        // Act
        var response = await Client.PostAsync($"api/Events/{fcEvent.Id}/archive", null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RestoreEvent_AsAdmin_ArchivedEvent_Succeeds()
    {
        // Arrange
        var admin = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);
        var fcEvent = await CreateAndSaveTestEvent(startDate: DateTime.UtcNow.AddDays(-7));

        // Mark all occurrences as completed and archive
        var savedEvent = await _eventService.GetAsync(fcEvent.Id);
        foreach (var occurrence in savedEvent!.Occurrences) occurrence.Status = OccurrenceStatus.Completed;
        savedEvent.IsArchived = true;
        savedEvent.ArchivedDate = DateTime.UtcNow;
        savedEvent.ArchivedByUserId = admin.DiscordId;
        var eventRepository = Factory.Services.GetRequiredService<IEventRepository>();
        await eventRepository.UpdateAsync(fcEvent.Id, savedEvent);

        // Act
        var response = await Client.PostAsync($"api/Events/{fcEvent.Id}/restore", null);

        // Assert
        response.EnsureSuccessStatusCode();

        var restoredEvent = await _eventService.GetAsync(fcEvent.Id);
        restoredEvent!.IsArchived.ShouldBeFalse();
        restoredEvent.ArchivedDate.ShouldBeNull();
        restoredEvent.ArchivedByUserId.ShouldBeNull();
    }

    [Fact]
    public async Task RestoreEvent_AsAdmin_NotArchivedEvent_ReturnsBadRequest()
    {
        // Arrange
        await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);
        var fcEvent = await CreateAndSaveTestEvent();

        // Act
        var response = await Client.PostAsync($"api/Events/{fcEvent.Id}/restore", null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetEvents_ExcludesArchivedByDefault()
    {
        // Arrange
        await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);

        // Create active event
        var activeEvent = await CreateAndSaveTestEvent("Active Event");

        // Create and archive an event
        var archivedEvent = await CreateAndSaveTestEvent("Archived Event", startDate: DateTime.UtcNow.AddDays(-7));
        var savedEvent = await _eventService.GetAsync(archivedEvent.Id);
        foreach (var occurrence in savedEvent!.Occurrences) occurrence.Status = OccurrenceStatus.Completed;
        savedEvent.IsArchived = true;
        savedEvent.ArchivedDate = DateTime.UtcNow;
        var eventRepository = Factory.Services.GetRequiredService<IEventRepository>();
        await eventRepository.UpdateAsync(archivedEvent.Id, savedEvent);

        // Act
        var response = await Client.GetAsync("api/Events");

        // Assert
        response.EnsureSuccessStatusCode();
        var events = await response.Content.ReadFromJsonAsync<List<EventResponse>>();
        events.ShouldNotBeNull();
        events!.Any(e => e.Name == "Active Event").ShouldBeTrue();
        events.Any(e => e.Name == "Archived Event").ShouldBeFalse();
    }

    [Fact]
    public async Task GetArchivedEvents_ReturnsOnlyArchivedEvents()
    {
        // Arrange
        await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);

        // Create active event
        var activeEvent = await CreateAndSaveTestEvent("Active Event");

        // Create and archive an event
        var archivedEvent = await CreateAndSaveTestEvent("Archived Event", startDate: DateTime.UtcNow.AddDays(-7));
        var savedEvent = await _eventService.GetAsync(archivedEvent.Id);
        foreach (var occurrence in savedEvent!.Occurrences) occurrence.Status = OccurrenceStatus.Completed;
        savedEvent.IsArchived = true;
        savedEvent.ArchivedDate = DateTime.UtcNow;
        var eventRepository = Factory.Services.GetRequiredService<IEventRepository>();
        await eventRepository.UpdateAsync(archivedEvent.Id, savedEvent);

        // Act
        var response = await Client.GetAsync("api/Events/archived");

        // Assert
        response.EnsureSuccessStatusCode();
        var events = await response.Content.ReadFromJsonAsync<List<EventResponse>>();
        events.ShouldNotBeNull();
        events!.Any(e => e.Name == "Archived Event").ShouldBeTrue();
        events.Any(e => e.Name == "Active Event").ShouldBeFalse();
    }

    [Fact]
    public async Task GetArchivedEvents_SearchByName_ReturnsMatches()
    {
        // Arrange
        await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);

        // Create and archive events
        var event1 = await CreateAndSaveTestEvent("Weekly Raid Night", startDate: DateTime.UtcNow.AddDays(-7));
        var event2 = await CreateAndSaveTestEvent("Social Gathering", startDate: DateTime.UtcNow.AddDays(-14));

        var eventRepository = Factory.Services.GetRequiredService<IEventRepository>();
        foreach (var eventToArchive in new[] { event1, event2 })
        {
            var savedEvent = await _eventService.GetAsync(eventToArchive.Id);
            foreach (var occurrence in savedEvent!.Occurrences) occurrence.Status = OccurrenceStatus.Completed;
            savedEvent.IsArchived = true;
            savedEvent.ArchivedDate = DateTime.UtcNow;
            await eventRepository.UpdateAsync(eventToArchive.Id, savedEvent);
        }

        // Act
        var response = await Client.GetAsync("api/Events/archived?searchText=Raid");

        // Assert
        response.EnsureSuccessStatusCode();
        var events = await response.Content.ReadFromJsonAsync<List<EventResponse>>();
        events.ShouldNotBeNull();
        events!.Count.ShouldBe(1);
        events[0].Name.ShouldBe("Weekly Raid Night");
    }

    #endregion

    #region Extend Event Tests

    [Fact]
    public async Task ExtendEvent_AsAdmin_RecurringEvent_AddsOccurrences()
    {
        // Arrange
        await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);
        var fcEvent = await CreateAndSaveRecurringTestEvent(weeklyCount: 3);

        var originalOccurrenceCount = fcEvent.Occurrences.Count;

        // Act
        var response = await Client.PostAsJsonAsync(
            $"api/Events/{fcEvent.Id}/extend",
            new ExtendEventRequest { Count = 2 });

        // Assert
        response.EnsureSuccessStatusCode();

        var updatedEvent = await _eventService.GetAsync(fcEvent.Id);
        updatedEvent!.Occurrences.Count.ShouldBe(originalOccurrenceCount + 2);
    }

    [Fact]
    public async Task ExtendEvent_AsAdmin_NonRecurringEvent_ReturnsBadRequest()
    {
        // Arrange
        await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);
        var fcEvent = await CreateAndSaveTestEvent(); // Non-recurring event

        // Act
        var response = await Client.PostAsJsonAsync(
            $"api/Events/{fcEvent.Id}/extend",
            new ExtendEventRequest { Count = 2 });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ExtendEvent_AsMember_ReturnsForbidden()
    {
        // Arrange
        await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        var fcEvent = await CreateAndSaveRecurringTestEvent(weeklyCount: 3);

        // Act
        var response = await Client.PostAsJsonAsync(
            $"api/Events/{fcEvent.Id}/extend",
            new ExtendEventRequest { Count = 2 });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ExtendEvent_InvalidCount_ReturnsBadRequest()
    {
        // Arrange
        await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);
        var fcEvent = await CreateAndSaveRecurringTestEvent(weeklyCount: 3);

        // Act
        var response = await Client.PostAsJsonAsync(
            $"api/Events/{fcEvent.Id}/extend",
            new ExtendEventRequest { Count = 0 });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ExtendEvent_NonExistentEvent_ReturnsBadRequest()
    {
        // Arrange
        await CreateAndAuthenticateAsMember(GenerateRandomDiscordId(), true);
        var nonExistentId = "507f1f77bcf86cd799439011";

        // Act
        var response = await Client.PostAsJsonAsync(
            $"api/Events/{nonExistentId}/extend",
            new ExtendEventRequest { Count = 2 });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    #endregion
}