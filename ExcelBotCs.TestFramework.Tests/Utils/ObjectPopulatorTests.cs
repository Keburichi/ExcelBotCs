using ExcelBotCs.Models.Database;
using ExcelBotCs.TestFramework.Utils;

namespace ExcelBotCs.TestFramework.Tests.Utils;

[TestFixture]
public class ObjectPopulatorTests
{
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
        Assert.That(event1.Id, Is.Not.EqualTo(event2.Id));
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
        Assert.That(member.Id, Is.Not.Null);
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
}