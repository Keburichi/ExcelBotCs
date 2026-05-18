using ExcelBotCs.Mappers.Events;
using ExcelBotCs.Models.Database.Events;
using ExcelBotCs.Models.DTO.Events;
using ExcelBotCs.Modules.TeamFormation;

namespace ExcelBotCs.Tests.Mappers.Events;

public class EventParticipantMappingExtensionsTests
{
    [Fact]
    public void ToEventParticipant_MapsAllFields()
    {
        var dto = new EventParticipantDto
        {
            DiscordUserId = "123456789",
            Role = Role.Tank,
            SelectionDate = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc)
        };

        var entity = dto.ToEventParticipant();

        entity.ShouldNotBeNull();
        entity.DiscordUserId.ShouldBe(dto.DiscordUserId);
        entity.Role.ShouldBe(dto.Role);
        entity.SelectionDate.ShouldBe(dto.SelectionDate);
    }

    [Fact]
    public void ToEventParticipantDto_MapsAllFields()
    {
        var entity = new EventParticipant
        {
            DiscordUserId = "987654321",
            Role = Role.Healer,
            SelectionDate = new DateTime(2025, 6, 2, 10, 0, 0, DateTimeKind.Utc)
        };

        var dto = entity.ToEventParticipantDto();

        dto.ShouldNotBeNull();
        dto.DiscordUserId.ShouldBe(entity.DiscordUserId);
        dto.Role.ShouldBe(entity.Role);
        dto.SelectionDate.ShouldBe(entity.SelectionDate);
    }

    [Fact]
    public void ToEventParticipants_MapsListCorrectly()
    {
        var dtos = new List<EventParticipantDto>
        {
            new() { DiscordUserId = "111", Role = Role.Tank, SelectionDate = DateTime.UtcNow },
            new() { DiscordUserId = "222", Role = Role.Healer, SelectionDate = DateTime.UtcNow }
        };

        var entities = dtos.ToEventParticipants();

        entities.Count.ShouldBe(2);
        entities[0].DiscordUserId.ShouldBe("111");
        entities[1].DiscordUserId.ShouldBe("222");
    }

    [Fact]
    public void ToEventParticipantDtos_MapsListCorrectly()
    {
        var entities = new List<EventParticipant>
        {
            new() { DiscordUserId = "333", Role = Role.Melee, SelectionDate = DateTime.UtcNow },
            new() { DiscordUserId = "444", Role = Role.Tank, SelectionDate = DateTime.UtcNow }
        };

        var dtos = entities.ToEventParticipantDtos();

        dtos.Count.ShouldBe(2);
        dtos[0].DiscordUserId.ShouldBe("333");
        dtos[1].DiscordUserId.ShouldBe("444");
    }
}