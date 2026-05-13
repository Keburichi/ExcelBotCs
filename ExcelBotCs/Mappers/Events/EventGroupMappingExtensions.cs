using ExcelBotCs.Models.Database.Events;
using ExcelBotCs.Models.DTO.Events;

namespace ExcelBotCs.Mappers.Events;

public static class EventGroupMappingExtensions
{
    public static List<EventGroup> ToEventGroups(this List<EventGroupRequest> eventGroups)
    {
        return eventGroups.Select(ToEventGroup).ToList();
    }

    public static EventGroup ToEventGroup(this EventGroupRequest eventGroupRequests)
    {
        return new EventGroup
        {
            Id = eventGroupRequests.Id,
            Name = eventGroupRequests.Name,
            Participants = eventGroupRequests.Participants.ToEventParticipants()
        };
    }

    public static List<EventGroupResponse> ToEventGroupResponses(this List<EventGroup> eventGroups)
    {
        return eventGroups.Select(ToEventGroupResponse).ToList();
    }

    public static EventGroupResponse ToEventGroupResponse(this EventGroup eventGroup)
    {
        return new EventGroupResponse
        {
            Id = eventGroup.Id,
            Name = eventGroup.Name,
            Participants = eventGroup.Participants.ToEventParticipantDtos()
        };
    }
}