using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO;

namespace ExcelBotCs.Mappers;

public static class EventMapper
{
    public static EventDto ToDto(Event fcEvent)
    {
        return new EventDto()
        {
            Id = fcEvent.Id,
            Name = fcEvent.Name,
            Description = fcEvent.Description,
            Duration = fcEvent.Duration,
            StartDate = fcEvent.StartDate,
            DiscordMessageId = fcEvent.DiscordMessageId,
            PictureUrl = fcEvent.PictureUrl,
            MaxNumberOfParticipants = fcEvent.MaxNumberOfParticipants,
            Author = fcEvent.Author,
            Participants = fcEvent.Participants,
            Signups = fcEvent.Signups,
        };
    }

    public static Event ToEntity(EventDto fcEvent)
    {
        return new Event()
        {
            Id = fcEvent.Id,
            Name = fcEvent.Name,
            Description = fcEvent.Description,
            Duration = fcEvent.Duration,
            StartDate = fcEvent.StartDate,
            DiscordMessageId = fcEvent.DiscordMessageId,
            PictureUrl = fcEvent.PictureUrl,
            MaxNumberOfParticipants = fcEvent.MaxNumberOfParticipants,
            Author = fcEvent.Author,
            Participants = fcEvent.Participants,
            Signups = fcEvent.Signups,
        };
    }
}