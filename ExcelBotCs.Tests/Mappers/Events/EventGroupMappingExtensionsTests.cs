using ExcelBotCs.Mappers.Events;
using ExcelBotCs.Models.Database.Events;
using ExcelBotCs.Models.DTO.Events;
using ExcelBotCs.Modules.TeamFormation;

namespace ExcelBotCs.Tests.Mappers.Events;

public class EventGroupMappingExtensionsTests
{
    [Fact]
    public void ToEventGroup_MapsAllFields()
    {
        var request = new EventGroupRequest
        {
            Id = Guid.NewGuid(),
            Name = "Group A",
            Participants = new List<EventParticipantDto>
            {
                new() { DiscordUserId = "111", Role = Role.Tank, SelectionDate = DateTime.UtcNow }
            }
        };

        var entity = request.ToEventGroup();

        entity.Id.ShouldBe(request.Id);
        entity.Name.ShouldBe(request.Name);
        entity.Participants.Count.ShouldBe(1);
        entity.Participants[0].DiscordUserId.ShouldBe("111");
    }

    [Fact]
    public void ToEventGroupResponse_MapsAllFields()
    {
        var entity = new EventGroup
        {
            Id = Guid.NewGuid(),
            Name = "Group B",
            Participants = new List<EventParticipant>
            {
                new() { DiscordUserId = "222", Role = Role.Healer, SelectionDate = DateTime.UtcNow }
            }
        };

        var response = entity.ToEventGroupResponse();

        response.Id.ShouldBe(entity.Id);
        response.Name.ShouldBe(entity.Name);
        response.Participants.Count.ShouldBe(1);
        response.Participants[0].DiscordUserId.ShouldBe("222");
    }

    [Fact]
    public void ToEventGroups_MapsListCorrectly()
    {
        var requests = new List<EventGroupRequest>
        {
            new() { Id = Guid.NewGuid(), Name = "G1", Participants = new List<EventParticipantDto>() },
            new() { Id = Guid.NewGuid(), Name = "G2", Participants = new List<EventParticipantDto>() }
        };

        var entities = requests.ToEventGroups();

        entities.Count.ShouldBe(2);
        entities[0].Name.ShouldBe("G1");
        entities[1].Name.ShouldBe("G2");
    }

    [Fact]
    public void ToEventGroupResponses_MapsListCorrectly()
    {
        var entities = new List<EventGroup>
        {
            new() { Id = Guid.NewGuid(), Name = "G3", Participants = new List<EventParticipant>() },
            new() { Id = Guid.NewGuid(), Name = "G4", Participants = new List<EventParticipant>() }
        };

        var responses = entities.ToEventGroupResponses();

        responses.Count.ShouldBe(2);
        responses[0].Name.ShouldBe("G3");
        responses[1].Name.ShouldBe("G4");
    }
}