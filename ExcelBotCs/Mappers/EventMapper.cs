using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO;
using DbEventSignup = ExcelBotCs.Models.Database.EventSignup;
using DtoEventSignup = ExcelBotCs.Models.DTO.EventSignupDto;

namespace ExcelBotCs.Mappers;

public static class EventMapper
{
    public static EventDto ToDto(Event fcEvent)
    {
        return new EventDto
        {
            Id = fcEvent.Id,
            Name = fcEvent.Name,
            Description = fcEvent.Description,
            Duration = fcEvent.Duration,
            StartDate = fcEvent.StartDate,
            EndDate = fcEvent.EndDate,
            ICalString = fcEvent.ICalString,
            SignupType = fcEvent.SignupType,
            DiscordMessageId = fcEvent.DiscordMessageId,
            PictureUrl = fcEvent.PictureUrl,
            Type = fcEvent.Type,
            FightId = fcEvent.FightId,
            MaxNumberOfParticipants = fcEvent.MaxNumberOfParticipants,
            AuthorId = fcEvent.AuthorId,
            Organizer = fcEvent.Organizer,
            Occurrences = fcEvent.Occurrences?.Select(MapOccurrenceToDto).ToList() ?? new List<EventOccurrenceDto>()
        };
    }

    public static Event ToEntity(EventDto fcEvent)
    {
        return new Event
        {
            Id = fcEvent.Id,
            Name = fcEvent.Name,
            Description = fcEvent.Description,
            Duration = fcEvent.Duration,
            StartDate = fcEvent.StartDate,
            EndDate = fcEvent.EndDate,
            ICalString = fcEvent.ICalString,
            SignupType = fcEvent.SignupType,
            DiscordMessageId = fcEvent.DiscordMessageId,
            PictureUrl = fcEvent.PictureUrl,
            Type = fcEvent.Type,
            FightId = fcEvent.FightId,
            MaxNumberOfParticipants = fcEvent.MaxNumberOfParticipants,
            AuthorId = fcEvent.AuthorId,
            Organizer = fcEvent.Organizer,
            Occurrences = fcEvent.Occurrences?.Select(MapOccurrenceToEntity).ToList() ?? new List<EventOccurrence>()
        };
    }

    private static EventOccurrenceDto MapOccurrenceToDto(EventOccurrence occurrence)
    {
        return new EventOccurrenceDto
        {
            Id = occurrence.Id,
            OccurrenceDate = occurrence.OccurrenceDate,
            Status = occurrence.Status,
            DiscordMessageId = occurrence.DiscordMessageId,
            Signups = occurrence.Signups?.Select(MapSignupToDto).ToList() ?? new List<DtoEventSignup>(),
            Participants = occurrence.Participants?.Select(MapParticipantToDto).ToList() ??
                           new List<EventParticipantDto>()
        };
    }

    private static EventOccurrence MapOccurrenceToEntity(EventOccurrenceDto dto)
    {
        return new EventOccurrence
        {
            Id = dto.Id,
            OccurrenceDate = dto.OccurrenceDate,
            Status = dto.Status,
            DiscordMessageId = dto.DiscordMessageId,
            Signups = dto.Signups?.Select(MapSignupToEntity).ToList() ?? new List<DbEventSignup>(),
            Participants = dto.Participants?.Select(MapParticipantToEntity).ToList() ?? new List<EventParticipant>()
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
}