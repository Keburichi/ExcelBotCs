using ExcelBotCs.Mappers;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.TestFramework.Utils;

namespace ExcelBotCs.Tests.Mappers;

public class EventMapperTests
{
    [Fact]
    public void ToDto()
    {
        // Arrange
        var entity = new Event().PopulateWithRandomData();

        // Act
        var entityDto = EventMapper.ToDto(entity);

        // Assert
        entityDto.ShouldNotBeNull();
        entityDto.Id.ShouldBe(entity.Id);
        entityDto.Name.ShouldBe(entity.Name);
        entityDto.Description.ShouldBe(entity.Description);
        entityDto.Duration.ShouldBe(entity.Duration);
        entityDto.StartDate.ShouldBe(entity.StartDate);
        entityDto.EndDate.ShouldBe(entity.EndDate);
        entityDto.ICalString.ShouldBe(entity.ICalString);
        entityDto.SignupType.ShouldBe(entity.SignupType);
        entityDto.DiscordMessageId.ShouldBe(entity.DiscordMessageId);
        entityDto.PictureUrl.ShouldBe(entity.PictureUrl);
        entityDto.Type.ShouldBe(entity.Type);
        entityDto.FightId.ShouldBe(entity.FightId);
        entityDto.MaxNumberOfParticipants.ShouldBe(entity.MaxNumberOfParticipants);
        entityDto.AuthorId.ShouldBe(entity.AuthorId);
        entityDto.Organizer.ShouldBe(entity.Organizer);
        entityDto.Occurrences.Count.ShouldBe(entity.Occurrences.Count);
        entityDto.AvailableForSignup.ShouldBe(entity.AvailableForSignup);
    }

    [Fact]
    public void ToEntity()
    {
        // Arrange
        var entityDto = new EventDto().PopulateWithRandomData();

        // Act
        var entity = EventMapper.ToEntity(entityDto);

        // Assert
        entity.ShouldNotBeNull();
        entity.Id.ShouldBe(entityDto.Id);
        entity.Name.ShouldBe(entityDto.Name);
        entity.Description.ShouldBe(entityDto.Description);
        entity.Duration.ShouldBe(entityDto.Duration);
        entity.StartDate.ShouldBe(entityDto.StartDate);
        entity.EndDate.ShouldBe(entityDto.EndDate);
        entity.ICalString.ShouldBe(entityDto.ICalString);
        entity.SignupType.ShouldBe(entityDto.SignupType);
        entity.DiscordMessageId.ShouldBe(entityDto.DiscordMessageId);
        entity.PictureUrl.ShouldBe(entityDto.PictureUrl);
        entity.Type.ShouldBe(entityDto.Type);
        entity.FightId.ShouldBe(entityDto.FightId);
        entity.MaxNumberOfParticipants.ShouldBe(entityDto.MaxNumberOfParticipants);
        entity.AuthorId.ShouldBe(entityDto.AuthorId);
        entity.Organizer.ShouldBe(entityDto.Organizer);
        entity.Occurrences.Count.ShouldBe(entityDto.Occurrences.Count);
        entity.AvailableForSignup.ShouldBe(entityDto.AvailableForSignup);
    }
}