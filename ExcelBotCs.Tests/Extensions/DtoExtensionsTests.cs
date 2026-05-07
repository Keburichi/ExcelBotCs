using ExcelBotCs.Extensions;
using ExcelBotCs.Models.DTO;

namespace ExcelBotCs.Tests.Extensions;

public class DtoExtensionsTests
{
    private class TestDto : BaseDto
    {
        public string? Name { get; set; }
        public int Age { get; set; }
        public string? Email { get; set; }
        public bool IsActive { get; set; }
    }

    [Fact]
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
        target.Id.ShouldBe("1");
        target.Name.ShouldBe("John Doe");
        target.Age.ShouldBe(30);
        target.Email.ShouldBe("john@example.com");
        target.IsActive.ShouldBeTrue();
    }

    [Fact]
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
        target.Id.ShouldBe("1");
        target.Name.ShouldBe("Jane Smith"); // Should not be updated
        target.Age.ShouldBe(30);
        target.Email.ShouldBe("jane@example.com"); // Should not be updated
        target.IsActive.ShouldBeTrue();
    }

    [Fact]
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
        target.Id.ShouldBe("1");
        target.Name.ShouldBe("John Doe");
        target.Age.ShouldBe(30);
        target.Email.ShouldBe("john@example.com");
        target.IsActive.ShouldBeTrue();
    }

    [Fact]
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
        target.Id.ShouldBe("1");
        target.Name.ShouldBe("John Doe");
        target.Age.ShouldBe(30);
        target.Email.ShouldBe("john@example.com");
        target.IsActive.ShouldBeTrue();
    }

    [Fact]
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
        target.Id.ShouldBe("1");
        target.Name.ShouldBe("John Doe");
        target.Age.ShouldBe(0);
        target.Email.ShouldBe("jane@example.com"); // Should remain unchanged (null in source)
        target.IsActive.ShouldBeTrue();
    }
}
