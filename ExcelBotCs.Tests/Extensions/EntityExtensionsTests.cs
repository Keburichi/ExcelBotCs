using ExcelBotCs.Attributes.API;
using ExcelBotCs.Extensions;
using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Tests.Extensions;

[TestFixture]
public class EntityExtensionsTests
{
    private class TestEntity : BaseEntity
    {
        public string? Name { get; set; }
        public int Age { get; set; }
        public string? Email { get; set; }

        [IgnoreUpdate] public string? IgnoredProperty { get; set; }

        public bool IsActive { get; set; }
    }

    [Test]
    public void UpdateUpdatedAttributes_ShouldUpdateAllNonNullProperties()
    {
        // Arrange
        var source = new TestEntity
        {
            Id = "1",
            Name = "John Doe",
            Age = 30,
            Email = "john@example.com",
            IsActive = true,
            IgnoredProperty = "Should not update"
        };

        var target = new TestEntity
        {
            Id = "2",
            Name = "Jane Smith",
            Age = 25,
            Email = "jane@example.com",
            IsActive = false,
            IgnoredProperty = "Original value"
        };

        // Act
        source.UpdateUpdatedAttributes(target);

        // Assert
        Assert.That(target.Id, Is.EqualTo("1"));
        Assert.That(target.Name, Is.EqualTo("John Doe"));
        Assert.That(target.Age, Is.EqualTo(30));
        Assert.That(target.Email, Is.EqualTo("john@example.com"));
        Assert.That(target.IsActive, Is.True);
        Assert.That(target.IgnoredProperty, Is.EqualTo("Original value")); // Should not be updated
    }

    [Test]
    public void UpdateUpdatedAttributes_ShouldNotUpdateNullProperties()
    {
        // Arrange
        var source = new TestEntity
        {
            Id = "1",
            Name = null,
            Age = 30,
            Email = null,
            IsActive = true
        };

        var target = new TestEntity
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
    public void UpdateUpdatedAttributes_ShouldRespectIgnoreUpdateAttribute()
    {
        // Arrange
        var source = new TestEntity
        {
            Id = "1",
            Name = "John Doe",
            Age = 30,
            Email = "john@example.com",
            IsActive = true,
            IgnoredProperty = "New Value"
        };

        var target = new TestEntity
        {
            Id = "2",
            Name = "Jane Smith",
            Age = 25,
            Email = "jane@example.com",
            IsActive = false,
            IgnoredProperty = "Original Value"
        };

        // Act
        source.UpdateUpdatedAttributes(target);

        // Assert
        Assert.That(target.IgnoredProperty, Is.EqualTo("Original Value")); // Should not be updated
        Assert.That(target.Name, Is.EqualTo("John Doe")); // Other properties should be updated
    }

    [Test]
    public void UpdateUpdatedAttributes_ShouldNotUpdateIfValuesAreEqual()
    {
        // Arrange
        var source = new TestEntity
        {
            Id = "1",
            Name = "John Doe",
            Age = 30,
            Email = "john@example.com",
            IsActive = true
        };

        var target = new TestEntity
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
        var source = new TestEntity
        {
            Id = "1",
            Name = "John Doe",
            Age = 30,
            Email = "john@example.com",
            IsActive = true
        };

        var target = new TestEntity();

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
        var source = new TestEntity
        {
            Id = "1",
            Name = "John Doe",
            Age = 0,
            Email = null,
            IsActive = true,
            IgnoredProperty = "New Value"
        };

        var target = new TestEntity
        {
            Id = "2",
            Name = "Jane Smith",
            Age = 25,
            Email = "jane@example.com",
            IsActive = false,
            IgnoredProperty = "Original Value"
        };

        // Act
        source.UpdateUpdatedAttributes(target);

        // Assert - Only non-null, different values should update, and IgnoreUpdate should be respected
        Assert.That(target.Id, Is.EqualTo("1"));
        Assert.That(target.Name, Is.EqualTo("John Doe"));
        Assert.That(target.Age, Is.EqualTo(0));
        Assert.That(target.Email, Is.EqualTo("jane@example.com")); // Should remain unchanged (null in source)
        Assert.That(target.IsActive, Is.True);
        Assert.That(target.IgnoredProperty, Is.EqualTo("Original Value")); // Should not be updated
    }

    [Test]
    public void UpdateUpdatedAttributes_ShouldHandleMultipleIgnoredProperties()
    {
        // Create a test entity with multiple ignored properties
        var testEntityType = typeof(TestEntityWithMultipleIgnored);
        var source = new TestEntityWithMultipleIgnored
        {
            Id = "1",
            Name = "John",
            IgnoredProp1 = "New1",
            IgnoredProp2 = "New2",
            NormalProp = "NewNormal"
        };

        var target = new TestEntityWithMultipleIgnored
        {
            Id = "2",
            Name = "Jane",
            IgnoredProp1 = "Old1",
            IgnoredProp2 = "Old2",
            NormalProp = "OldNormal"
        };

        // Act
        source.UpdateUpdatedAttributes(target);

        // Assert
        Assert.That(target.IgnoredProp1, Is.EqualTo("Old1"));
        Assert.That(target.IgnoredProp2, Is.EqualTo("Old2"));
        Assert.That(target.NormalProp, Is.EqualTo("NewNormal"));
        Assert.That(target.Name, Is.EqualTo("John"));
    }

    private class TestEntityWithMultipleIgnored : BaseEntity
    {
        public string? Name { get; set; }

        [IgnoreUpdate] public string? IgnoredProp1 { get; set; }

        [IgnoreUpdate] public string? IgnoredProp2 { get; set; }

        public string? NormalProp { get; set; }
    }
}