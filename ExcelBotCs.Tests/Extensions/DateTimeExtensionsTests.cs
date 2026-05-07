using ExcelBotCs.Extensions;

namespace ExcelBotCs.Tests.Extensions;

public class DateTimeExtensionsTests
{
    private static readonly DateTime TestDateTime = new(2025, 1, 15, 14, 30, 45, DateTimeKind.Utc);
    private static readonly long TestUnixTimestamp = ((DateTimeOffset)TestDateTime).ToUnixTimeSeconds();

    [Fact]
    public void ToShortDiscordTime_ShouldReturnCorrectFormat()
    {
        // Act
        var result = TestDateTime.ToShortDiscordTime();

        // Assert
        result.ShouldBe($"<t:{TestUnixTimestamp}:t>");
    }

    [Fact]
    public void ToLongDiscordTime_ShouldReturnCorrectFormat()
    {
        // Act
        var result = TestDateTime.ToLongDiscordTime();

        // Assert
        result.ShouldBe($"<t:{TestUnixTimestamp}:T>");
    }

    [Fact]
    public void ToShortDiscordDate_ShouldReturnCorrectFormat()
    {
        // Act
        var result = TestDateTime.ToShortDiscordDate();

        // Assert
        result.ShouldBe($"<t:{TestUnixTimestamp}:d>");
    }

    [Fact]
    public void ToLongDiscordDate_ShouldReturnCorrectFormat()
    {
        // Act
        var result = TestDateTime.ToLongDiscordDate();

        // Assert
        result.ShouldBe($"<t:{TestUnixTimestamp}:D>");
    }

    [Fact]
    public void ToLongDiscordDateShortTime_ShouldReturnCorrectFormat()
    {
        // Act
        var result = TestDateTime.ToLongDiscordDateShortTime();

        // Assert
        result.ShouldBe($"<t:{TestUnixTimestamp}:f>");
    }

    [Fact]
    public void ToLongDiscordDateLongTime_ShouldReturnCorrectFormat()
    {
        // Act
        var result = TestDateTime.ToLongDiscordDateLongTime();

        // Assert
        result.ShouldBe($"<t:{TestUnixTimestamp}:F>");
    }

    [Fact]
    public void ToRelativeDiscordTime_ShouldReturnCorrectFormat()
    {
        // Act
        var result = TestDateTime.ToRelativeDiscordTime();

        // Assert
        result.ShouldBe($"<t:{TestUnixTimestamp}:R>");
    }

    [Fact]
    public void MinDateTime_ShouldHandleMinValue()
    {
        // Arrange
        // DateTime.MinValue with Kind=Unspecified is valid in UTC (equals DateTimeOffset.MinValue)
        var minDateTime = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);

        // Act
        var result = minDateTime.ToShortDiscordTime();

        // Assert
        result.ShouldStartWith("<t:");
        result.ShouldEndWith(":t>");
    }

    [Fact]
    public void MaxDateTime_ShouldHandleMaxValue()
    {
        // Arrange
        var maxDateTime = DateTime.MaxValue;

        // Act
        var result = maxDateTime.ToShortDiscordTime();

        // Assert - Should produce valid format
        result.ShouldStartWith("<t:");
        result.ShouldEndWith(":t>");
    }

    [Fact]
    public void ReasonableDateTime_ShouldHandleCorrectly()
    {
        // Arrange - Use a reasonable date that can be converted to Unix timestamp
        var reasonableDateTime = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var result = reasonableDateTime.ToShortDiscordTime();

        // Assert - Should produce valid format
        result.ShouldStartWith("<t:");
        result.ShouldEndWith(":t>");
    }

    [Fact]
    public void AllFormats_ShouldStartWithTimestampPrefix()
    {
        // Act & Assert
        TestDateTime.ToShortDiscordTime().ShouldStartWith("<t:");
        TestDateTime.ToLongDiscordTime().ShouldStartWith("<t:");
        TestDateTime.ToShortDiscordDate().ShouldStartWith("<t:");
        TestDateTime.ToLongDiscordDate().ShouldStartWith("<t:");
        TestDateTime.ToLongDiscordDateShortTime().ShouldStartWith("<t:");
        TestDateTime.ToLongDiscordDateLongTime().ShouldStartWith("<t:");
        TestDateTime.ToRelativeDiscordTime().ShouldStartWith("<t:");
    }

    [Fact]
    public void AllFormats_ShouldEndWithCorrectSuffix()
    {
        // Assert
        TestDateTime.ToShortDiscordTime().ShouldEndWith(":t>");
        TestDateTime.ToLongDiscordTime().ShouldEndWith(":T>");
        TestDateTime.ToShortDiscordDate().ShouldEndWith(":d>");
        TestDateTime.ToLongDiscordDate().ShouldEndWith(":D>");
        TestDateTime.ToLongDiscordDateShortTime().ShouldEndWith(":f>");
        TestDateTime.ToLongDiscordDateLongTime().ShouldEndWith(":F>");
        TestDateTime.ToRelativeDiscordTime().ShouldEndWith(":R>");
    }
}
