using ExcelBotCs.Models.Database.Events;
using ExcelBotCs.Models.DTO.Events;

namespace ExcelBotCs.Mappers.Events;

public static class EventTemplateMappingExtensions
{
    public static EventTemplateResponse ToResponse(this EventTemplate template)
    {
        return new EventTemplateResponse
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            Type = template.Type,
            DayOfWeek = template.DayOfWeek,
            TimeOfDayMinutes = template.TimeOfDayMinutes,
            Duration = template.Duration,
            Organizer = template.Organizer,
            MaxNumberOfParticipants = template.MaxNumberOfParticipants,
            RequiredParticipants = template.RequiredParticipants > 0
                ? template.RequiredParticipants
                : template.MaxNumberOfParticipants,
            SignupButtonConfigs = template.SignupButtonConfigs?.Select(MapButtonConfigToDto).ToList()
        };
    }

    public static List<EventTemplateResponse> ToResponse(this List<EventTemplate> templates)
    {
        return templates.Select(t => t.ToResponse()).ToList();
    }

    public static EventTemplate ToEntity(this CreateEventTemplateRequest request)
    {
        return new EventTemplate
        {
            Name = request.Name,
            Description = request.Description,
            Type = request.Type,
            DayOfWeek = request.DayOfWeek,
            TimeOfDayMinutes = request.TimeOfDayMinutes,
            Duration = request.Duration,
            Organizer = request.Organizer,
            MaxNumberOfParticipants = request.MaxNumberOfParticipants,
            RequiredParticipants = request.RequiredParticipants,
            SignupButtonConfigs = request.SignupButtonConfigs?.Select(MapButtonConfigToEntity).ToList()
        };
    }

    public static EventTemplate ApplyUpdate(this EventTemplate existing, UpdateEventTemplateRequest request)
    {
        existing.Name = request.Name;
        existing.Description = request.Description;
        existing.Type = request.Type;
        existing.DayOfWeek = request.DayOfWeek;
        existing.TimeOfDayMinutes = request.TimeOfDayMinutes;
        existing.Duration = request.Duration;
        existing.Organizer = request.Organizer;
        existing.MaxNumberOfParticipants = request.MaxNumberOfParticipants;
        existing.RequiredParticipants = request.RequiredParticipants;
        existing.SignupButtonConfigs = request.SignupButtonConfigs?.Select(MapButtonConfigToEntity).ToList();
        return existing;
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
}
