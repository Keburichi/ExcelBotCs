using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.Database.Events;
using ExcelBotCs.TestFramework.Utils;
using MongoDB.Bson.Serialization.Attributes;

namespace ExcelBotCs.TestFramework.Tests.Utils;

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

    [Fact]
    public void PopulateWithRandomData_ShouldPopulateAllProperties()
    {
        // Arrange
        var testEvent = new Event();

        // Act
        testEvent.PopulateWithRandomData();

        // Assert
        testEvent.Name.ShouldNotBeNull();
        testEvent.Name.ShouldStartWith("Name_");
        testEvent.Description.ShouldNotBeNull();
        testEvent.Description.ShouldStartWith("Description_");
        testEvent.Duration.ShouldBeGreaterThan(0);
        testEvent.MaxNumberOfParticipants.ShouldBeGreaterThan(0);
        testEvent.StartDate.ShouldNotBe(default(DateTime));
        testEvent.EndDate.ShouldNotBe(default(DateTime));
    }

    [Fact]
    public void PopulateWithRandomData_ShouldPopulateLists()
    {
        // Arrange
        var testEvent = new Event();

        // Act
        testEvent.PopulateWithRandomData();

        // Assert
        testEvent.Occurrences.ShouldNotBeNull();
        testEvent.Occurrences.ShouldNotBeEmpty();
        testEvent.Occurrences.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void PopulateWithRandomData_ShouldGenerateDifferentValues()
    {
        // Arrange
        var event1 = new Event();
        var event2 = new Event();

        // Act
        event1.PopulateWithRandomData();
        event2.PopulateWithRandomData();

        // Assert - Names should be different due to random UUIDs
        event1.Name.ShouldNotBe(event2.Name);
        event1.Description.ShouldNotBe(event2.Description);
        // Note: Id is not populated because it has [BsonId] attribute
    }

    [Fact]
    public void PopulateWithRandomData_ShouldReturnSameInstance()
    {
        // Arrange
        var testEvent = new Event();

        // Act
        var result = testEvent.PopulateWithRandomData();

        // Assert
        result.ShouldBeSameAs(testEvent);
    }

    [Fact]
    public void PopulateWithRandomData_ShouldHandleNestedObjects()
    {
        // Arrange
        var member = new Member();

        // Act
        member.PopulateWithRandomData();

        // Assert
        // Note: Id is not populated because it has [BsonId] attribute
        member.DiscordName.ShouldNotBeNull();
        member.DiscordId.ShouldNotBeNull();
    }

    [Fact]
    public void PopulateWithRandomData_CanBeChained()
    {
        // Act
        var testEvent = new Event().PopulateWithRandomData();

        // Assert
        testEvent.ShouldNotBeNull();
        testEvent.Name.ShouldNotBeNull();
    }

    [Fact]
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
        entity.Id.ShouldBe("fixed-id-123");
        entity.Name.ShouldNotBeEmpty();
        entity.Description.ShouldNotBeEmpty();
    }

    [Fact]
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
        entity.CustomId.ShouldBe("custom-id-456");
        entity.Name.ShouldNotBeEmpty();
        entity.Value.ShouldBeGreaterThan(0);
    }

    [Fact]
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
        entity.Id.ShouldBe("original-id");
        entity.Name.ShouldNotBeEmpty();
        entity.Age.ShouldBeGreaterThan(0);
    }

    [Fact]
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
        member.Id.ShouldBe("member-id-789");
        member.DiscordName.ShouldNotBeEmpty();
    }

    [Fact]
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
        entity.Id.ShouldBe("persistent-id-999");
        firstPopulation.ShouldBe(secondPopulation);
    }

    [Fact]
    public void PopulateWithRandomData_ShouldSetDateCreatedAndDateModifiedButNotId()
    {
        // Arrange
        var entity = new Event
        {
            Id = "test-id-000"
        };

        // Act
        entity.PopulateWithRandomData();

        // Assert
        entity.Id.ShouldBe("test-id-000");
        entity.DateCreated.ShouldNotBe(default(DateTime));
        entity.DateModified.ShouldNotBe(default(DateTime));
    }
}