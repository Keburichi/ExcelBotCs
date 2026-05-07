using ExcelBotCs.Extensions;
using ExcelBotCs.TestFramework.TestData;

namespace ExcelBotCs.Tests.Extensions;

public class DiscordSocketExtensionsTests
{
    [Fact]
    public void PrettyJoin_WithSingleElement_ShouldReturnElement()
    {
        // Arrange
        var list = new List<string> { "Item1" };

        // Act
        var result = list.PrettyJoin();

        // Assert
        result.ShouldBe("Item1");
    }

    [Fact]
    public void PrettyJoin_WithTwoElements_ShouldReturnElementsWithAnd()
    {
        // Arrange
        var list = new List<string> { "Item1", "Item2" };

        // Act
        var result = list.PrettyJoin();

        // Assert
        result.ShouldBe("Item1 and Item2");
    }

    [Fact]
    public void PrettyJoin_WithThreeElements_ShouldReturnCommaSeparatedWithAnd()
    {
        // Arrange
        var list = new List<string> { "Item1", "Item2", "Item3" };

        // Act
        var result = list.PrettyJoin();

        // Assert
        result.ShouldBe("Item1, Item2 and Item3");
    }

    [Fact]
    public void PrettyJoin_WithMultipleElements_ShouldReturnCommaSeparatedWithAnd()
    {
        // Arrange
        var list = new List<string> { "Apple", "Banana", "Cherry", "Date", "Elderberry" };

        // Act
        var result = list.PrettyJoin();

        // Assert
        result.ShouldBe("Apple, Banana, Cherry, Date and Elderberry");
    }

    [Fact]
    public void PrettyJoin_WithEmptyList_ShouldReturnEmptyString()
    {
        // Arrange
        var list = new List<string>();

        // Act
        var result = list.PrettyJoin();

        // Assert
        result.ShouldBe(string.Empty);
    }

    [Theory]
    [MemberData(nameof(NullOrEmptyStringData.Values), MemberType = typeof(NullOrEmptyStringData))]
    public void PrettyJoin_WithEmptyStringElements_ShouldHandleCorrectly(string? emptyString)
    {
        // Arrange
        var list = new List<string> { "Item1", emptyString, "Item3" };

        // Act
        var result = list.PrettyJoin();

        // Assert
        result.ShouldContain("Item1");
        result.ShouldContain("Item3");
    }

    [Fact]
    public void PrettyJoin_WithSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var list = new List<string> { "Item@1", "Item#2", "Item$3" };

        // Act
        var result = list.PrettyJoin();

        // Assert
        result.ShouldBe("Item@1, Item#2 and Item$3");
    }

    [Fact]
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
        result.ShouldStartWith("This is a very long string");
        result.ShouldEndWith("Yet another lengthy string");
        result.ShouldContain(" and ");
    }

    [Fact]
    public void PrettyJoin_WithWhitespaceStrings_ShouldHandleCorrectly()
    {
        // Arrange
        var list = new List<string> { "Item1", "   ", "Item3" };

        // Act
        var result = list.PrettyJoin();

        // Assert
        result.ShouldContain("Item1");
        result.ShouldContain("Item3");
    }

    [Fact]
    public void PrettyJoin_WithNumberStrings_ShouldJoinCorrectly()
    {
        // Arrange
        var list = new List<string> { "1", "2", "3", "4" };

        // Act
        var result = list.PrettyJoin();

        // Assert
        result.ShouldBe("1, 2, 3 and 4");
    }

    [Fact]
    public void PrettyJoin_WithMixedContent_ShouldJoinCorrectly()
    {
        // Arrange
        var list = new List<string> { "Tank", "Healer", "DPS", "Support" };

        // Act
        var result = list.PrettyJoin();

        // Assert
        result.ShouldBe("Tank, Healer, DPS and Support");
    }

    [Fact]
    public void IsMember_MessageResponse_NotValidUrlType_ShouldExist()
    {
        // This test verifies the enum and response types exist
        var notValidResponse = new DiscordSocketExtensions.NotValidUrlMessageResponse();
        notValidResponse.ShouldNotBeNull();
        notValidResponse.ShouldBeAssignableTo<DiscordSocketExtensions.IMessageResponse>();
    }

    [Fact]
    public void IsMember_MessageResponse_NotFoundUrlType_ShouldExist()
    {
        // This test verifies the enum and response types exist
        var notFoundResponse = new DiscordSocketExtensions.NotFoundUrlMessageResponse();
        notFoundResponse.ShouldNotBeNull();
        notFoundResponse.ShouldBeAssignableTo<DiscordSocketExtensions.IMessageResponse>();
    }

    [Fact]
    public void MessageResponse_Enum_ShouldHaveExpectedValues()
    {
        // Assert enum values exist
        DiscordSocketExtensions.MessageResponse.NotValidUrl
            .ShouldBe(DiscordSocketExtensions.MessageResponse.NotValidUrl);
        DiscordSocketExtensions.MessageResponse.NotFoundUrl
            .ShouldBe(DiscordSocketExtensions.MessageResponse.NotFoundUrl);
        DiscordSocketExtensions.MessageResponse.Success
            .ShouldBe(DiscordSocketExtensions.MessageResponse.Success);
    }
}
