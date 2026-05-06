using ExcelBotCs.Extensions;
using ExcelBotCs.Models.DTO;

namespace ExcelBotCs.Tests.Extensions;

[TestFixture]
public class DtoExtensionsTests
{
    private class TestDto : BaseDto
    {
        public string? Name { get; set; }
        public int Age { get; set; }
        public string? Email { get; set; }
        public bool IsActive { get; set; }
    }

    [Test]
    public void UpdateUpdatedAttributes_ShouldUpdateAllNonNullProperties()
    {
        // Arrange
        var source = new TestDto
        {
            Id = "1",
            Name = "John Doe",
            Age = 30,
            Email = "john@example.com",
            IsActive = true
        };

        var target = new TestDto
        {
            Id = "2",
            Name = "Jane Smith",
            Age = 25,
            Email = "jane@example.com",
            IsActive = false
        };

        // Act
        source.UpdateUpdatedAttributes(target);

        // Assert
        Assert.That(target.Id, Is.EqualTo("1"));
        Assert.That(target.Name, Is.EqualTo("John Doe"));
        Assert.That(target.Age, Is.EqualTo(30));
        Assert.That(target.Email, Is.EqualTo("john@example.com"));
        Assert.That(target.IsActive, Is.True);
    }

    [Test]
    public void UpdateUpdatedAttributes_ShouldNotUpdateNullProperties()
    {
        // Arrange
        var source = new TestDto
        {
            Id = "1",
            Name = null,
            Age = 30,
            Email = null,
            IsActive = true
        };

        var target = new TestDto
        {
            Id = "2",
            Name = "Jane Smith",
            Age = 25,
            Email = "jane@example.com",
            IsActive = false
        };

        // Act
        source.UpdateUpdatedAttributes(target);

        // Assert
        Assert.That(target.Id, Is.EqualTo("1"));
        Assert.That(target.Name, Is.EqualTo("Jane Smith")); // Should not be updated
        Assert.That(target.Age, Is.EqualTo(30));
        Assert.That(target.Email, Is.EqualTo("jane@example.com")); // Should not be updated
        Assert.That(target.IsActive, Is.True);
    }

    [Test]
    public void UpdateUpdatedAttributes_ShouldNotUpdateIfValuesAreEqual()
    {
        // Arrange
        var source = new TestDto
        {
            Id = "1",
            Name = "John Doe",
            Age = 30,
            Email = "john@example.com",
            IsActive = true
        };

        var target = new TestDto
        {
            Id = "1",
            Name = "John Doe",
            Age = 30,
            Email = "john@example.com",
            IsActive = true
        };

        // Act
        source.UpdateUpdatedAttributes(target);

        // Assert - All values should remain the same
        Assert.That(target.Id, Is.EqualTo("1"));
        Assert.That(target.Name, Is.EqualTo("John Doe"));
        Assert.That(target.Age, Is.EqualTo(30));
        Assert.That(target.Email, Is.EqualTo("john@example.com"));
        Assert.That(target.IsActive, Is.True);
    }

    [Test]
    public void UpdateUpdatedAttributes_ShouldHandleEmptyTarget()
    {
        // Arrange
        var source = new TestDto
        {
            Id = "1",
            Name = "John Doe",
            Age = 30,
            Email = "john@example.com",
            IsActive = true
        };

        var target = new TestDto();

        // Act
        source.UpdateUpdatedAttributes(target);

        // Assert
        Assert.That(target.Id, Is.EqualTo("1"));
        Assert.That(target.Name, Is.EqualTo("John Doe"));
        Assert.That(target.Age, Is.EqualTo(30));
        Assert.That(target.Email, Is.EqualTo("john@example.com"));
        Assert.That(target.IsActive, Is.True);
    }

    [Test]
    public void UpdateUpdatedAttributes_ShouldHandlePartialUpdate()
    {
        // Arrange
        var source = new TestDto
        {
            Id = "1",
            Name = "John Doe",
            Age = 0,
            Email = null,
            IsActive = true
        };

        var target = new TestDto
        {
            Id = "2",
            Name = "Jane Smith",
            Age = 25,
            Email = "jane@example.com",
            IsActive = false
        };

        // Act
        source.UpdateUpdatedAttributes(target);

        // Assert - Only non-null and different values should update
        Assert.That(target.Id, Is.EqualTo("1"));
        Assert.That(target.Name, Is.EqualTo("John Doe"));
        Assert.That(target.Age, Is.EqualTo(0));
        Assert.That(target.Email, Is.EqualTo("jane@example.com")); // Should remain unchanged (null in source)
        Assert.That(target.IsActive, Is.True);
    }
}