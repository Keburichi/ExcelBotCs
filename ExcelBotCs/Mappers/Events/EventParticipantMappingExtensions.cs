using ExcelBotCs.Models.Database.Events;
using ExcelBotCs.Models.DTO.Events;

namespace ExcelBotCs.Mappers.Events;

public static class EventParticipantMappingExtensions
{
    public static List<EventParticipant> ToEventParticipants(this List<EventParticipantDto> participants)
    {
        return participants.Select(ToEventParticipant).ToList();
    }

    public static EventParticipant ToEventParticipant(this EventParticipantDto participantDto)
    {
        return new EventParticipant
        {
            DiscordUserId = participantDto.DiscordUserId,
            Role = participantDto.Role,
            SelectionDate = participantDto.SelectionDate
        };
    }

    public static List<EventParticipantDto> ToEventParticipantDtos(this List<EventParticipant> participants)
    {
        return participants.Select(ToEventParticipantDto).ToList();
    }

    public static EventParticipantDto ToEventParticipantDto(this EventParticipant participant)
    {
        return new EventParticipantDto
        {
            DiscordUserId = participant.DiscordUserId,
            Role = participant.Role,
            SelectionDate = participant.SelectionDate
        };
    }
}