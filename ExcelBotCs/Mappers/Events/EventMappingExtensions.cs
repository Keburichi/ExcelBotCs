using ExcelBotCs.Extensions;
using ExcelBotCs.Models.Database.Events;
using ExcelBotCs.Models.DTO.Events;
using DbEventSignup = ExcelBotCs.Models.Database.Events.EventSignup;
using DtoEventSignup = ExcelBotCs.Models.DTO.EventSignupDto;

namespace ExcelBotCs.Mappers.Events;

public static class EventMappingExtensions
{
    public static List<EventResponse> ToEventResponse(this List<Event> events)
    {
        return events.IsNullOrEmpty()
            ? new List<EventResponse>()
            : events.Select(fcEvent => fcEvent.ToEventResponse()).ToList();
    }

    public static EventResponse ToEventResponse(this Event fcEvent)
    {
        return new EventResponse
        {
            Id = fcEvent.Id,
            Name = fcEvent.Name,
            Description = fcEvent.Description,
            Duration = fcEvent.Duration,
            StartDate = fcEvent.StartDate,
            ICalString = fcEvent.ICalString,
            SignupType = fcEvent.SignupType,
            DiscordMessageId = fcEvent.DiscordMessageId,
            PictureUrl = fcEvent.PictureUrl,
            Type = fcEvent.Type,
            FightId = fcEvent.FightId,
            MaxNumberOfParticipants = fcEvent.MaxNumberOfParticipants,
            AuthorId = fcEvent.AuthorId,
            Organizer = fcEvent.Organizer,
            Occurrences = fcEvent.Occurrences?.Select(MapOccurrenceToDto).ToList() ?? new List<EventOccurrenceDto>(),
            IsArchived = fcEvent.IsArchived,
            ArchivedDate = fcEvent.ArchivedDate,
            ArchivedByUserId = fcEvent.ArchivedByUserId,
            Signups = fcEvent.Signups?.Select(MapSignupToDto).ToList() ?? new List<DtoEventSignup>(),
            Groups = fcEvent.Groups.ToEventGroupResponses()
        };
    }

    private static EventOccurrenceDto MapOccurrenceToDto(EventOccurrence occurrence)
    {
        return new EventOccurrenceDto
        {
            Id = occurrence.Id,
            OccurrenceDate = occurrence.OccurrenceDate,
            Status = occurrence.Status
        };
    }

    private static EventOccurrence MapOccurrenceToEntity(EventOccurrenceDto dto)
    {
        return new EventOccurrence
        {
            Id = dto.Id,
            OccurrenceDate = dto.OccurrenceDate,
            Status = dto.Status
        };
    }

    private static DtoEventSignup MapSignupToDto(DbEventSignup signup)
    {
        return new DtoEventSignup
        {
            DiscordUserId = signup.DiscordUserId,
            Roles = signup.Roles,
            SignupDate = signup.SignupDate
        };
    }

    private static DbEventSignup MapSignupToEntity(DtoEventSignup dto)
    {
        return new DbEventSignup
        {
            DiscordUserId = dto.DiscordUserId,
            Roles = dto.Roles,
            SignupDate = dto.SignupDate
        };
    }

    private static EventParticipantDto MapParticipantToDto(EventParticipant participant)
    {
        return new EventParticipantDto
        {
            DiscordUserId = participant.DiscordUserId,
            Role = participant.Role,
            SelectionDate = participant.SelectionDate
        };
    }

    private static EventParticipant MapParticipantToEntity(EventParticipantDto dto)
    {
        return new EventParticipant
        {
            DiscordUserId = dto.DiscordUserId,
            Role = dto.Role,
            SelectionDate = dto.SelectionDate
        };
    }

    public static Event ApplyUpdate(this Event existing, UpdateEventRequest request)
    {
        existing.Name = request.Name;
        existing.Description = request.Description;
        existing.Type = request.Type;
        existing.StartDate = request.StartDate;
        existing.Duration = request.Duration;
        existing.ICalString = request.ICalString;
        existing.SignupType = request.SignupType;
        existing.PictureUrl = request.PictureUrl;
        existing.FightId = request.FightId;
        existing.Organizer = request.Organizer;
        existing.MaxNumberOfParticipants = request.MaxNumberOfParticipants;
        return existing;
    }

    public static Event ToFcEvent(this CreateEventRequest createEventRequest)
    {
        return new Event
        {
            Name = createEventRequest.Name,
            Description = createEventRequest.Description,
            Type = createEventRequest.Type,
            StartDate = createEventRequest.StartDate,
            Duration = createEventRequest.Duration,
            ICalString = createEventRequest.ICalString,
            SignupType = createEventRequest.SignupType,
            PictureUrl = createEventRequest.PictureUrl,
            FightId = createEventRequest.FightId,
            Organizer = createEventRequest.Organizer,
            MaxNumberOfParticipants = createEventRequest.MaxNumberOfParticipants
        };
    }
}