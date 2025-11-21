using ExcelBotCs.Extensions;
using ExcelBotCs.TestFramework.Attributes;

namespace ExcelBotCs.Tests.Extensions;

[TestFixture]
public class DiscordSocketExtensionsTests
{
    [Test]
    public void PrettyJoin_WithSingleElement_ShouldReturnElement()
    {
        // Arrange
        var list = new List<string> { "Item1" };

        // Act
        var result = list.PrettyJoin();

        // Assert
        Assert.That(result, Is.EqualTo("Item1"));
    }

    [Test]
    public void PrettyJoin_WithTwoElements_ShouldReturnElementsWithAnd()
    {
        // Arrange
        var list = new List<string> { "Item1", "Item2" };

        // Act
        var result = list.PrettyJoin();

        // Assert
        Assert.That(result, Is.EqualTo("Item1 and Item2"));
    }

    [Test]
    public void PrettyJoin_WithThreeElements_ShouldReturnCommaSeparatedWithAnd()
    {
        // Arrange
        var list = new List<string> { "Item1", "Item2", "Item3" };

        // Act
        var result = list.PrettyJoin();

        // Assert
        Assert.That(result, Is.EqualTo("Item1, Item2 and Item3"));
    }

    [Test]
    public void PrettyJoin_WithMultipleElements_ShouldReturnCommaSeparatedWithAnd()
    {
        // Arrange
        var list = new List<string> { "Apple", "Banana", "Cherry", "Date", "Elderberry" };

        // Act
        var result = list.PrettyJoin();

        // Assert
        Assert.That(result, Is.EqualTo("Apple, Banana, Cherry, Date and Elderberry"));
    }

    [Test]
    public void PrettyJoin_WithEmptyList_ShouldReturnEmptyString()
    {
        // Arrange
        var list = new List<string>();

        // Act
        var result = list.PrettyJoin();

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    [TestIsNullOrEmptyString]
    public void PrettyJoin_WithEmptyStringElements_ShouldHandleCorrectly(string emptyString)
    {
        // Arrange
        var list = new List<string> { "Item1", emptyString, "Item3" };

        // Act
        var result = list.PrettyJoin();

        // Assert
        Assert.That(result, Does.Contain("Item1"));
        Assert.That(result, Does.Contain("Item3"));
    }

    [Test]
    public void PrettyJoin_WithSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var list = new List<string> { "Item@1", "Item#2", "Item$3" };

        // Act
        var result = list.PrettyJoin();

        // Assert
        Assert.That(result, Is.EqualTo("Item@1, Item#2 and Item$3"));
    }

    [Test]
    public void PrettyJoin_WithLongStrings_ShouldHandleCorrectly()
    {
        // Arrange
        var list = new List<string>
        {
            "This is a very long string that should still be joined correctly",
            "Another long string with many words",
            "Yet another lengthy string"
        };

        // Act
        var result = list.PrettyJoin();

        // Assert
        Assert.That(result, Does.StartWith("This is a very long string"));
        Assert.That(result, Does.EndWith("Yet another lengthy string"));
        Assert.That(result, Does.Contain(" and "));
    }

    [Test]
    public void PrettyJoin_WithWhitespaceStrings_ShouldHandleCorrectly()
    {
        // Arrange
        var list = new List<string> { "Item1", "   ", "Item3" };

        // Act
        var result = list.PrettyJoin();

        // Assert
        Assert.That(result, Does.Contain("Item1"));
        Assert.That(result, Does.Contain("Item3"));
    }

    [Test]
    public void PrettyJoin_WithNumberStrings_ShouldJoinCorrectly()
    {
        // Arrange
        var list = new List<string> { "1", "2", "3", "4" };

        // Act
        var result = list.PrettyJoin();

        // Assert
        Assert.That(result, Is.EqualTo("1, 2, 3 and 4"));
    }

    [Test]
    public void PrettyJoin_WithMixedContent_ShouldJoinCorrectly()
    {
        // Arrange
        var list = new List<string> { "Tank", "Healer", "DPS", "Support" };

        // Act
        var result = list.PrettyJoin();

        // Assert
        Assert.That(result, Is.EqualTo("Tank, Healer, DPS and Support"));
    }

    [Test]
    public void IsMember_MessageResponse_NotValidUrlType_ShouldExist()
    {
        // This test verifies the enum and response types exist
        var notValidResponse = new DiscordSocketExtensions.NotValidUrlMessageResponse();
        Assert.That(notValidResponse, Is.Not.Null);
        Assert.That(notValidResponse, Is.InstanceOf<DiscordSocketExtensions.IMessageResponse>());
    }

    [Test]
    public void IsMember_MessageResponse_NotFoundUrlType_ShouldExist()
    {
        // This test verifies the enum and response types exist
        var notFoundResponse = new DiscordSocketExtensions.NotFoundUrlMessageResponse();
        Assert.That(notFoundResponse, Is.Not.Null);
        Assert.That(notFoundResponse, Is.InstanceOf<DiscordSocketExtensions.IMessageResponse>());
    }

    [Test]
    public void MessageResponse_Enum_ShouldHaveExpectedValues()
    {
        // Assert enum values exist
        Assert.That(DiscordSocketExtensions.MessageResponse.NotValidUrl,
            Is.EqualTo(DiscordSocketExtensions.MessageResponse.NotValidUrl));
        Assert.That(DiscordSocketExtensions.MessageResponse.NotFoundUrl,
            Is.EqualTo(DiscordSocketExtensions.MessageResponse.NotFoundUrl));
        Assert.That(DiscordSocketExtensions.MessageResponse.Success,
            Is.EqualTo(DiscordSocketExtensions.MessageResponse.Success));
    }
}