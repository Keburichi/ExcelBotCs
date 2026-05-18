using ExcelBotCs.Mappers.Events;
using ExcelBotCs.Models.Database.Events;
using ExcelBotCs.Models.DTO.Events;

namespace ExcelBotCs.Tests.Mappers.Events;

public class EventTemplateMappingExtensionsTests
{
    [Fact]
    public void ToResponse_MapsAllFields()
    {
        var entity = new EventTemplate
        {
            Id = "507f1f77bcf86cd799439011",
            Name = "Weekly Raid",
            Description = "Weekly savage raid",
            Type = EventType.Raid,
            DayOfWeek = DayOfWeek.Saturday,
            TimeOfDayMinutes = 1200,
            Duration = 120,
            Organizer = "Leader",
            MaxNumberOfParticipants = 8,
            SignupButtonConfigs = null
        };

        var response = entity.ToResponse();

        response.Id.ShouldBe(entity.Id);
        response.Name.ShouldBe(entity.Name);
        response.Description.ShouldBe(entity.Description);
        response.Type.ShouldBe(entity.Type);
        response.DayOfWeek.ShouldBe(entity.DayOfWeek);
        response.TimeOfDayMinutes.ShouldBe(entity.TimeOfDayMinutes);
        response.Duration.ShouldBe(entity.Duration);
        response.Organizer.ShouldBe(entity.Organizer);
        response.MaxNumberOfParticipants.ShouldBe(entity.MaxNumberOfParticipants);
        response.SignupButtonConfigs.ShouldBeNull();
    }

    [Fact]
    public void ToResponse_MapsSignupButtonConfigs()
    {
        var entity = new EventTemplate
        {
            Id = "507f1f77bcf86cd799439011",
            Name = "Weekly Raid",
            Description = "Weekly savage raid",
            Type = EventType.Raid,
            DayOfWeek = DayOfWeek.Saturday,
            TimeOfDayMinutes = 1200,
            Duration = 120,
            Organizer = "Leader",
            MaxNumberOfParticipants = 8,
            SignupButtonConfigs = new List<SignupButtonConfig>
            {
                new() { Slug = "tank", Label = "Tank", EmojiId = "123", IsHelper = false, MappedRole = null },
                new() { Slug = "helper", Label = "Helper", EmojiId = null, IsHelper = true, MappedRole = null }
            }
        };

        var response = entity.ToResponse();

        response.SignupButtonConfigs.ShouldNotBeNull();
        response.SignupButtonConfigs!.Count.ShouldBe(2);
        response.SignupButtonConfigs[0].Slug.ShouldBe("tank");
        response.SignupButtonConfigs[0].Label.ShouldBe("Tank");
        response.SignupButtonConfigs[0].EmojiId.ShouldBe("123");
        response.SignupButtonConfigs[0].IsHelper.ShouldBeFalse();
        response.SignupButtonConfigs[1].Slug.ShouldBe("helper");
        response.SignupButtonConfigs[1].IsHelper.ShouldBeTrue();
    }

    [Fact]
    public void ToResponse_List_MapsAllTemplates()
    {
        var entities = new List<EventTemplate>
        {
            new() { Id = "507f1f77bcf86cd799439011", Name = "T1", Description = "d1", Organizer = "o1" },
            new() { Id = "507f1f77bcf86cd799439012", Name = "T2", Description = "d2", Organizer = "o2" }
        };

        var responses = entities.ToResponse();

        responses.Count.ShouldBe(2);
        responses[0].Name.ShouldBe("T1");
        responses[1].Name.ShouldBe("T2");
    }

    [Fact]
    public void ToEntity_FromCreateRequest_MapsAllFields()
    {
        var request = new CreateEventTemplateRequest
        {
            Name = "New Template",
            Description = "desc",
            Type = EventType.Social,
            DayOfWeek = DayOfWeek.Friday,
            TimeOfDayMinutes = 900,
            Duration = 60,
            Organizer = "GM",
            MaxNumberOfParticipants = 4,
            SignupButtonConfigs = null
        };

        var entity = request.ToEntity();

        entity.Name.ShouldBe(request.Name);
        entity.Description.ShouldBe(request.Description);
        entity.Type.ShouldBe(request.Type);
        entity.DayOfWeek.ShouldBe(request.DayOfWeek);
        entity.TimeOfDayMinutes.ShouldBe(request.TimeOfDayMinutes);
        entity.Duration.ShouldBe(request.Duration);
        entity.Organizer.ShouldBe(request.Organizer);
        entity.MaxNumberOfParticipants.ShouldBe(request.MaxNumberOfParticipants);
        entity.SignupButtonConfigs.ShouldBeNull();
    }

    [Fact]
    public void ToEntity_FromCreateRequest_MapsSignupButtonConfigs()
    {
        var request = new CreateEventTemplateRequest
        {
            Name = "New Template",
            Description = "desc",
            Type = EventType.Social,
            DayOfWeek = DayOfWeek.Friday,
            TimeOfDayMinutes = 900,
            Duration = 60,
            Organizer = "GM",
            MaxNumberOfParticipants = 4,
            SignupButtonConfigs = new List<SignupButtonConfigDto>
            {
                new() { Slug = "dps", Label = "DPS", EmojiId = "456", IsHelper = false, MappedRole = null }
            }
        };

        var entity = request.ToEntity();

        entity.SignupButtonConfigs.ShouldNotBeNull();
        entity.SignupButtonConfigs!.Count.ShouldBe(1);
        entity.SignupButtonConfigs[0].Slug.ShouldBe("dps");
        entity.SignupButtonConfigs[0].Label.ShouldBe("DPS");
        entity.SignupButtonConfigs[0].EmojiId.ShouldBe("456");
        entity.SignupButtonConfigs[0].IsHelper.ShouldBeFalse();
    }

    [Fact]
    public void ApplyUpdate_OverwritesAllFields()
    {
        var entity = new EventTemplate
        {
            Id = "507f1f77bcf86cd799439011",
            Name = "Old Name",
            Description = "Old desc",
            Type = EventType.Raid,
            DayOfWeek = DayOfWeek.Monday,
            TimeOfDayMinutes = 600,
            Duration = 90,
            Organizer = "OldOrg",
            MaxNumberOfParticipants = 8
        };
        var request = new UpdateEventTemplateRequest
        {
            Name = "New Name",
            Description = "New desc",
            Type = EventType.Social,
            DayOfWeek = DayOfWeek.Wednesday,
            TimeOfDayMinutes = 720,
            Duration = 45,
            Organizer = "NewOrg",
            MaxNumberOfParticipants = 6
        };

        entity.ApplyUpdate(request);

        entity.Name.ShouldBe("New Name");
        entity.Description.ShouldBe("New desc");
        entity.Type.ShouldBe(EventType.Social);
        entity.DayOfWeek.ShouldBe(DayOfWeek.Wednesday);
        entity.TimeOfDayMinutes.ShouldBe(720);
        entity.Duration.ShouldBe(45);
        entity.Organizer.ShouldBe("NewOrg");
        entity.MaxNumberOfParticipants.ShouldBe(6);
    }

    [Fact]
    public void ApplyUpdate_ReturnsUpdatedEntity()
    {
        var entity = new EventTemplate
        {
            Id = "507f1f77bcf86cd799439011",
            Name = "Old Name",
            Description = "Old desc",
            Type = EventType.Raid,
            DayOfWeek = DayOfWeek.Monday,
            TimeOfDayMinutes = 600,
            Duration = 90,
            Organizer = "OldOrg",
            MaxNumberOfParticipants = 8
        };
        var request = new UpdateEventTemplateRequest
        {
            Name = "New Name",
            Description = "New desc",
            Type = EventType.Social,
            DayOfWeek = DayOfWeek.Wednesday,
            TimeOfDayMinutes = 720,
            Duration = 45,
            Organizer = "NewOrg",
            MaxNumberOfParticipants = 6
        };

        var result = entity.ApplyUpdate(request);

        result.ShouldBeSameAs(entity);
    }

    [Fact]
    public void ApplyUpdate_IdIsPreserved()
    {
        var entity = new EventTemplate
        {
            Id = "507f1f77bcf86cd799439011",
            Name = "Old Name",
            Description = "Old desc",
            Type = EventType.Raid,
            DayOfWeek = DayOfWeek.Monday,
            TimeOfDayMinutes = 600,
            Duration = 90,
            Organizer = "OldOrg",
            MaxNumberOfParticipants = 8
        };
        var request = new UpdateEventTemplateRequest
        {
            Name = "New Name",
            Description = "New desc",
            Type = EventType.Social,
            DayOfWeek = DayOfWeek.Wednesday,
            TimeOfDayMinutes = 720,
            Duration = 45,
            Organizer = "NewOrg",
            MaxNumberOfParticipants = 6
        };

        entity.ApplyUpdate(request);

        entity.Id.ShouldBe("507f1f77bcf86cd799439011");
    }
}