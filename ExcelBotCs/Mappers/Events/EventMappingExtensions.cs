using ExcelBotCs.Extensions;
using ExcelBotCs.Models.Database.Events;
using ExcelBotCs.Models.DTO;
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

    public static PagedResult<EventResponse> ToPagedEventResponse(this PagedResult<Event> pagedResult)
    {
        return new PagedResult<EventResponse>
        {
            Items = pagedResult.Items.ToEventResponse(),
            TotalCount = pagedResult.TotalCount,
            Page = pagedResult.Page,
            PageSize = pagedResult.PageSize
        };
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
            PictureUrl = fcEvent.PictureUrl,
            Type = fcEvent.Type,
            FightId = fcEvent.FightId,
            MaxNumberOfParticipants = fcEvent.MaxNumberOfParticipants,
            AuthorId = fcEvent.AuthorId,
            Organizer = fcEvent.Organizer,
            SignupButtonConfigs = fcEvent.SignupButtonConfigs?.Select(MapButtonConfigToDto).ToList(),
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

    private static DtoEventSignup MapSignupToDto(DbEventSignup signup)
    {
        return new DtoEventSignup
        {
            DiscordUserId = signup.DiscordUserId,
            Roles = signup.Roles,
            SignupSlugs = signup.SignupSlugs,
            SignupDate = signup.SignupDate
        };
    }

    private static SignupButtonConfigDto MapButtonConfigToDto(SignupButtonConfig config)
    {
        return new SignupButtonConfigDto
        {
            Slug = config.Slug,
            Label = config.Label,
            EmojiId = config.EmojiId,
            IsHelper = config.IsHelper,
            MappedRole = config.MappedRole
        };
    }

    private static SignupButtonConfig MapButtonConfigToEntity(SignupButtonConfigDto dto)
    {
        return new SignupButtonConfig
        {
            Slug = dto.Slug,
            Label = dto.Label,
            EmojiId = dto.EmojiId,
            IsHelper = dto.IsHelper,
            MappedRole = dto.MappedRole
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
        existing.SignupButtonConfigs = request.SignupButtonConfigs?.Select(MapButtonConfigToEntity).ToList();
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
            MaxNumberOfParticipants = createEventRequest.MaxNumberOfParticipants,
            SignupButtonConfigs = createEventRequest.SignupButtonConfigs?.Select(MapButtonConfigToEntity).ToList()
        };
    }
}