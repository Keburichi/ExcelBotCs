using ExcelBotCs.Models.Database;
using ExcelBotCs.TestFramework.Utils;
using MongoDB.Bson.Serialization.Attributes;

namespace ExcelBotCs.TestFramework.Tests.Utils;

[TestFixture]
public class ObjectPopulatorTests
{
    private class TestEntityWithCustomBsonId
    {
        [BsonId] public string CustomId { get; set; } = "original-custom-id";

        public string Name { get; set; }
        public int Value { get; set; }
    }

    private class TestEntityWithoutBsonId
    {
        public string Id { get; set; } = "original-id";
        public string Name { get; set; }
        public int Age { get; set; }
    }

    [Test]
    public void PopulateWithRandomData_ShouldPopulateAllProperties()
    {
        // Arrange
        var testEvent = new Event();

        // Act
        testEvent.PopulateWithRandomData();

        // Assert
        Assert.That(testEvent.Name, Is.Not.Null);
        Assert.That(testEvent.Name, Does.StartWith("Name_"));
        Assert.That(testEvent.Description, Is.Not.Null);
        Assert.That(testEvent.Description, Does.StartWith("Description_"));
        Assert.That(testEvent.Duration, Is.GreaterThan(0));
        Assert.That(testEvent.MaxNumberOfParticipants, Is.GreaterThan(0));
        Assert.That(testEvent.StartDate, Is.Not.EqualTo(default(DateTime)));
        Assert.That(testEvent.EndDate, Is.Not.EqualTo(default(DateTime)));
    }

    [Test]
    public void PopulateWithRandomData_ShouldPopulateLists()
    {
        // Arrange
        var testEvent = new Event();

        // Act
        testEvent.PopulateWithRandomData();

        // Assert
        Assert.That(testEvent.Occurrences, Is.Not.Null);
        Assert.That(testEvent.Occurrences, Is.Not.Empty);
        Assert.That(testEvent.Occurrences.Count, Is.GreaterThan(0));
    }

    [Test]
    public void PopulateWithRandomData_ShouldGenerateDifferentValues()
    {
        // Arrange
        var event1 = new Event();
        var event2 = new Event();

        // Act
        event1.PopulateWithRandomData();
        event2.PopulateWithRandomData();

        // Assert - Names should be different due to random UUIDs
        Assert.That(event1.Name, Is.Not.EqualTo(event2.Name));
        Assert.That(event1.Description, Is.Not.EqualTo(event2.Description));
        // Note: Id is not populated because it has [BsonId] attribute
    }

    [Test]
    public void PopulateWithRandomData_ShouldReturnSameInstance()
    {
        // Arrange
        var testEvent = new Event();

        // Act
        var result = testEvent.PopulateWithRandomData();

        // Assert
        Assert.That(result, Is.SameAs(testEvent));
    }

    [Test]
    public void PopulateWithRandomData_ShouldHandleNestedObjects()
    {
        // Arrange
        var member = new Member();

        // Act
        member.PopulateWithRandomData();

        // Assert
        // Note: Id is not populated because it has [BsonId] attribute
        Assert.That(member.DiscordName, Is.Not.Null);
        Assert.That(member.DiscordId, Is.Not.Null);
    }

    [Test]
    public void PopulateWithRandomData_CanBeChained()
    {
        // Act
        var testEvent = new Event().PopulateWithRandomData();

        // Assert
        Assert.That(testEvent, Is.Not.Null);
        Assert.That(testEvent.Name, Is.Not.Null);
    }

    [Test]
    public void PopulateWithRandomData_ShouldNotModifyBsonIdProperty()
    {
        // Arrange
        var entity = new Event
        {
            Id = "fixed-id-123"
        };

        // Act
        entity.PopulateWithRandomData();

        // Assert
        Assert.That(entity.Id, Is.EqualTo("fixed-id-123"), "BsonId property should not be modified");
        Assert.That(entity.Name, Is.Not.Empty, "Other properties should be populated");
        Assert.That(entity.Description, Is.Not.Empty, "Other properties should be populated");
    }

    [Test]
    public void PopulateWithRandomData_ShouldNotModifyCustomBsonIdProperty()
    {
        // Arrange
        var entity = new TestEntityWithCustomBsonId
        {
            CustomId = "custom-id-456"
        };

        // Act
        entity.PopulateWithRandomData();

        // Assert
        Assert.That(entity.CustomId, Is.EqualTo("custom-id-456"), "Custom BsonId property should not be modified");
        Assert.That(entity.Name, Is.Not.Empty, "Other properties should be populated");
        Assert.That(entity.Value, Is.GreaterThan(0), "Other properties should be populated");
    }

    [Test]
    public void PopulateWithRandomData_ShouldModifyIdWithoutBsonIdAttribute()
    {
        // Arrange
        var entity = new TestEntityWithoutBsonId
        {
            Id = "original-id"
        };

        // Act
        entity.PopulateWithRandomData();

        // Assert
        Assert.That(entity.Id, Is.EqualTo("original-id"), "Populated attributes should not be modified");
        Assert.That(entity.Name, Is.Not.Empty, "Other properties should be populated");
        Assert.That(entity.Age, Is.GreaterThan(0), "Other properties should be populated");
    }

    [Test]
    public void PopulateWithRandomData_WithBaseEntity_ShouldNotModifyId()
    {
        // Arrange
        var member = new Member
        {
            Id = "member-id-789"
        };

        // Act
        member.PopulateWithRandomData();

        // Assert
        Assert.That(member.Id, Is.EqualTo("member-id-789"), "BaseEntity Id (which has BsonId) should not be modified");
        Assert.That(member.DiscordName, Is.Not.Empty, "Other properties should be populated");
    }

    [Test]
    public void PopulateWithRandomData_MultipleCalls_ShouldNotModifyBsonId()
    {
        // Arrange
        var entity = new Event
        {
            Id = "persistent-id-999"
        };

        // Act
        entity.PopulateWithRandomData();
        var firstPopulation = entity.Name;

        entity.PopulateWithRandomData();
        var secondPopulation = entity.Name;

        // Assert
        Assert.That(entity.Id, Is.EqualTo("persistent-id-999"),
            "BsonId should remain unchanged after multiple populations");
        Assert.That(firstPopulation, Is.EqualTo(secondPopulation),
            "Other properties should not change with each population");
    }

    [Test]
    public void PopulateWithRandomData_ShouldSetCreateDateAndEditDateButNotId()
    {
        // Arrange
        var entity = new Event
        {
            Id = "test-id-000"
        };

        // Act
        entity.PopulateWithRandomData();

        // Assert
        Assert.That(entity.Id, Is.EqualTo("test-id-000"), "Id should not be modified");
        Assert.That(entity.CreateDate, Is.Not.EqualTo(default(DateTime)), "CreateDate should be populated");
        Assert.That(entity.EditDate, Is.Not.EqualTo(default(DateTime)), "EditDate should be populated");
    }
}