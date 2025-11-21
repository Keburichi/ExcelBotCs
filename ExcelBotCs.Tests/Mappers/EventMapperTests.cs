using ExcelBotCs.Mappers;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.TestFramework.Utils;

namespace ExcelBotCs.Tests.Mappers;

[TestFixture]
public class EventMapperTests
{
    [Test]
    public void ToDto()
    {
        // Arrange
        var entity = new Event().PopulateWithRandomData();

        // Act
        var entityDto = EventMapper.ToDto(entity);

        // Assert
        Assert.That(entityDto, Is.Not.Null);
        Assert.That(entityDto.Id, Is.EqualTo(entity.Id));
        Assert.That(entityDto.Name, Is.EqualTo(entity.Name));
        Assert.That(entityDto.Description, Is.EqualTo(entity.Description));
        Assert.That(entityDto.Duration, Is.EqualTo(entity.Duration));
        Assert.That(entityDto.StartDate, Is.EqualTo(entity.StartDate));
        Assert.That(entityDto.EndDate, Is.EqualTo(entity.EndDate));
        Assert.That(entityDto.ICalString, Is.EqualTo(entity.ICalString));
        Assert.That(entityDto.SignupType, Is.EqualTo(entity.SignupType));
        Assert.That(entityDto.DiscordMessageId, Is.EqualTo(entity.DiscordMessageId));
        Assert.That(entityDto.PictureUrl, Is.EqualTo(entity.PictureUrl));
        Assert.That(entityDto.Type, Is.EqualTo(entity.Type));
        Assert.That(entityDto.FightId, Is.EqualTo(entity.FightId));
        Assert.That(entityDto.MaxNumberOfParticipants, Is.EqualTo(entity.MaxNumberOfParticipants));
        Assert.That(entityDto.AuthorId, Is.EqualTo(entity.AuthorId));
        Assert.That(entityDto.Organizer, Is.EqualTo(entity.Organizer));
        Assert.That(entityDto.Occurrences.Count, Is.EqualTo(entity.Occurrences.Count));
        Assert.That(entityDto.AvailableForSignup, Is.EqualTo(entity.AvailableForSignup));
    }

    [Test]
    public void ToEntity()
    {
        // Arrange
        var entityDto = new EventDto().PopulateWithRandomData();

        // Act
        var entity = EventMapper.ToEntity(entityDto);

        // Assert
        Assert.That(entity, Is.Not.Null);
        Assert.That(entity.Id, Is.EqualTo(entityDto.Id));
        Assert.That(entity.Name, Is.EqualTo(entityDto.Name));
        Assert.That(entity.Description, Is.EqualTo(entityDto.Description));
        Assert.That(entity.Duration, Is.EqualTo(entityDto.Duration));
        Assert.That(entity.StartDate, Is.EqualTo(entityDto.StartDate));
        Assert.That(entity.EndDate, Is.EqualTo(entityDto.EndDate));
        Assert.That(entity.ICalString, Is.EqualTo(entityDto.ICalString));
        Assert.That(entity.SignupType, Is.EqualTo(entityDto.SignupType));
        Assert.That(entity.DiscordMessageId, Is.EqualTo(entityDto.DiscordMessageId));
        Assert.That(entity.PictureUrl, Is.EqualTo(entityDto.PictureUrl));
        Assert.That(entity.Type, Is.EqualTo(entityDto.Type));
        Assert.That(entity.FightId, Is.EqualTo(entityDto.FightId));
        Assert.That(entity.MaxNumberOfParticipants, Is.EqualTo(entityDto.MaxNumberOfParticipants));
        Assert.That(entity.AuthorId, Is.EqualTo(entityDto.AuthorId));
        Assert.That(entity.Organizer, Is.EqualTo(entityDto.Organizer));
        Assert.That(entity.Occurrences.Count, Is.EqualTo(entityDto.Occurrences.Count));
        Assert.That(entity.AvailableForSignup, Is.EqualTo(entityDto.AvailableForSignup));
    }
}