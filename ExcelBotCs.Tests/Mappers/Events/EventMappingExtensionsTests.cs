using ExcelBotCs.Mappers.Events;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.Database.Events;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.Models.DTO.Events;

namespace ExcelBotCs.Tests.Mappers.Events;

public class EventMappingExtensionsTests
{
    private static Event BuildEvent()
    {
        return new Event
        {
            Name = "Test Raid",
            Description = "Weekly static clear",
            Type = EventType.Raid,
            StartDate = new DateTime(2026, 6, 1, 20, 0, 0, DateTimeKind.Utc),
            Duration = 120,
            ICalString = "BEGIN:VCALENDAR\r\nEND:VCALENDAR",
            SignupType = SignupType.LockedGroup,
            PictureUrl = "https://example.com/pic.png",
            FightId = "fight-abc",
            AuthorId = "author-123",
            Organizer = "Keb",
            MaxNumberOfParticipants = 8,
            IsArchived = false,
            Occurrences = new List<EventOccurrence>(),
            Signups = new List<EventSignup>(),
            Groups = new List<EventGroup>()
        };
    }

    // -----------------------------------------------------------------------
    // ToEventResponse (single)
    // -----------------------------------------------------------------------

    [Fact]
    public void ToEventResponse_MapsAllScalarFields()
    {
        var fcEvent = BuildEvent();

        var response = fcEvent.ToEventResponse();

        response.ShouldNotBeNull();
        response.Id.ShouldBe(fcEvent.Id);
        response.Name.ShouldBe(fcEvent.Name);
        response.Description.ShouldBe(fcEvent.Description);
        response.Type.ShouldBe(fcEvent.Type);
        response.StartDate.ShouldBe(fcEvent.StartDate);
        response.Duration.ShouldBe(fcEvent.Duration);
        response.ICalString.ShouldBe(fcEvent.ICalString);
        response.SignupType.ShouldBe(fcEvent.SignupType);
        response.PictureUrl.ShouldBe(fcEvent.PictureUrl);
        response.FightId.ShouldBe(fcEvent.FightId);
        response.AuthorId.ShouldBe(fcEvent.AuthorId);
        response.Organizer.ShouldBe(fcEvent.Organizer);
        response.MaxNumberOfParticipants.ShouldBe(fcEvent.MaxNumberOfParticipants);
        response.IsArchived.ShouldBe(fcEvent.IsArchived);
    }

    [Fact]
    public void ToEventResponse_MapsCollections()
    {
        var fcEvent = BuildEvent();
        fcEvent.Occurrences.Add(new EventOccurrence
        {
            OccurrenceDate = new DateTime(2026, 6, 1, 20, 0, 0, DateTimeKind.Utc),
            Status = OccurrenceStatus.Scheduled
        });

        var response = fcEvent.ToEventResponse();

        response.Occurrences.Count.ShouldBe(1);
        response.Signups.Count.ShouldBe(0);
        response.Groups.Count.ShouldBe(0);
    }

    // -----------------------------------------------------------------------
    // ToEventResponse (list)
    // -----------------------------------------------------------------------

    [Fact]
    public void ToEventResponse_List_MapsAllEvents()
    {
        var events = new List<Event> { BuildEvent(), BuildEvent() };

        var responses = events.ToEventResponse();

        responses.Count.ShouldBe(2);
    }

    // -----------------------------------------------------------------------
    // ToPagedEventResponse
    // -----------------------------------------------------------------------

    [Fact]
    public void ToPagedEventResponse_PreservesPageMetadata()
    {
        var pagedResult = new PagedResult<Event>
        {
            TotalCount = 10,
            Page = 2,
            PageSize = 5,
            Items = new List<Event> { BuildEvent() }
        };

        var pagedResponse = pagedResult.ToPagedEventResponse();

        pagedResponse.TotalCount.ShouldBe(10);
        pagedResponse.Page.ShouldBe(2);
        pagedResponse.PageSize.ShouldBe(5);
        pagedResponse.Items.Count.ShouldBe(1);
    }

    // -----------------------------------------------------------------------
    // ToFcEvent
    // -----------------------------------------------------------------------

    [Fact]
    public void ToFcEvent_FromCreateRequest_MapsAllFields()
    {
        var request = new CreateEventRequest
        {
            Name = "New Raid",
            Description = "A brand new event",
            Type = EventType.Farming,
            StartDate = new DateTime(2026, 7, 15, 19, 0, 0, DateTimeKind.Utc),
            Duration = 90,
            ICalString = "BEGIN:VCALENDAR\r\nEND:VCALENDAR",
            SignupType = SignupType.IndependentSignups,
            PictureUrl = "https://example.com/raid.png",
            FightId = "fight-xyz",
            Organizer = "Alice",
            MaxNumberOfParticipants = 24
        };

        var fcEvent = request.ToFcEvent();

        fcEvent.ShouldNotBeNull();
        fcEvent.Name.ShouldBe(request.Name);
        fcEvent.Description.ShouldBe(request.Description);
        fcEvent.Type.ShouldBe(request.Type);
        fcEvent.StartDate.ShouldBe(request.StartDate);
        fcEvent.Duration.ShouldBe(request.Duration);
        fcEvent.ICalString.ShouldBe(request.ICalString);
        fcEvent.SignupType.ShouldBe(request.SignupType);
        fcEvent.PictureUrl.ShouldBe(request.PictureUrl);
        fcEvent.FightId.ShouldBe(request.FightId);
        fcEvent.Organizer.ShouldBe(request.Organizer);
        fcEvent.MaxNumberOfParticipants.ShouldBe(request.MaxNumberOfParticipants);
    }

    // -----------------------------------------------------------------------
    // ApplyUpdate
    // -----------------------------------------------------------------------

    [Fact]
    public void ApplyUpdate_OverwritesAllFields()
    {
        var existing = BuildEvent();

        var updateRequest = new UpdateEventRequest
        {
            Name = "Updated Raid",
            Description = "Updated description",
            Type = EventType.Social,
            StartDate = new DateTime(2026, 8, 1, 18, 0, 0, DateTimeKind.Utc),
            Duration = 60,
            ICalString = "BEGIN:VCALENDAR\r\nVERSION:2.0\r\nEND:VCALENDAR",
            SignupType = SignupType.SingleEvent,
            PictureUrl = "https://example.com/updated.png",
            FightId = "fight-updated",
            Organizer = "Bob",
            MaxNumberOfParticipants = 16
        };

        var result = existing.ApplyUpdate(updateRequest);

        result.ShouldBeSameAs(existing);
        result.Name.ShouldBe(updateRequest.Name);
        result.Description.ShouldBe(updateRequest.Description);
        result.Type.ShouldBe(updateRequest.Type);
        result.Duration.ShouldBe(updateRequest.Duration);
        result.Organizer.ShouldBe(updateRequest.Organizer);
        result.MaxNumberOfParticipants.ShouldBe(updateRequest.MaxNumberOfParticipants);
    }
}