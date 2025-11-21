using ExcelBotCs.Extensions;

namespace ExcelBotCs.Tests.Extensions;

[TestFixture]
public class DateTimeExtensionsTests
{
    private static readonly DateTime TestDateTime = new(2025, 1, 15, 14, 30, 45, DateTimeKind.Utc);
    private static readonly long TestUnixTimestamp = ((DateTimeOffset)TestDateTime).ToUnixTimeSeconds();

    [Test]
    public void ToShortDiscordTime_ShouldReturnCorrectFormat()
    {
        // Act
        var result = TestDateTime.ToShortDiscordTime();

        // Assert
        Assert.That(result, Is.EqualTo($"<t:{TestUnixTimestamp}:t>"));
    }

    [Test]
    public void ToLongDiscordTime_ShouldReturnCorrectFormat()
    {
        // Act
        var result = TestDateTime.ToLongDiscordTime();

        // Assert
        Assert.That(result, Is.EqualTo($"<t:{TestUnixTimestamp}:T>"));
    }

    [Test]
    public void ToShortDiscordDate_ShouldReturnCorrectFormat()
    {
        // Act
        var result = TestDateTime.ToShortDiscordDate();

        // Assert
        Assert.That(result, Is.EqualTo($"<t:{TestUnixTimestamp}:d>"));
    }

    [Test]
    public void ToLongDiscordDate_ShouldReturnCorrectFormat()
    {
        // Act
        var result = TestDateTime.ToLongDiscordDate();

        // Assert
        Assert.That(result, Is.EqualTo($"<t:{TestUnixTimestamp}:D>"));
    }

    [Test]
    public void ToLongDiscordDateShortTime_ShouldReturnCorrectFormat()
    {
        // Act
        var result = TestDateTime.ToLongDiscordDateShortTime();

        // Assert
        Assert.That(result, Is.EqualTo($"<t:{TestUnixTimestamp}:f>"));
    }

    [Test]
    public void ToLongDiscordDateLongTime_ShouldReturnCorrectFormat()
    {
        // Act
        var result = TestDateTime.ToLongDiscordDateLongTime();

        // Assert
        Assert.That(result, Is.EqualTo($"<t:{TestUnixTimestamp}:F>"));
    }

    [Test]
    public void ToRelativeDiscordTime_ShouldReturnCorrectFormat()
    {
        // Act
        var result = TestDateTime.ToRelativeDiscordTime();

        // Assert
        Assert.That(result, Is.EqualTo($"<t:{TestUnixTimestamp}:R>"));
    }

    [Test]
    public void MinDateTime_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var minDateTime = DateTime.MinValue;

        // Act & Assert - Should throw ArgumentOutOfRangeException as DateTimeOffset cannot represent DateTime.MinValue
        Assert.Throws<ArgumentOutOfRangeException>(() => minDateTime.ToShortDiscordTime());
    }

    [Test]
    public void MaxDateTime_ShouldHandleMaxValue()
    {
        // Arrange
        var maxDateTime = DateTime.MaxValue;

        // Act
        var result = maxDateTime.ToShortDiscordTime();

        // Assert - Should produce valid format
        Assert.That(result, Does.StartWith("<t:"));
        Assert.That(result, Does.EndWith(":t>"));
    }

    [Test]
    public void ReasonableDateTime_ShouldHandleCorrectly()
    {
        // Arrange - Use a reasonable date that can be converted to Unix timestamp
        var reasonableDateTime = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var result = reasonableDateTime.ToShortDiscordTime();

        // Assert - Should produce valid format
        Assert.That(result, Does.StartWith("<t:"));
        Assert.That(result, Does.EndWith(":t>"));
    }

    [Test]
    public void AllFormats_ShouldStartWithTimestampPrefix()
    {
        // Act & Assert
        Assert.That(TestDateTime.ToShortDiscordTime(), Does.StartWith("<t:"));
        Assert.That(TestDateTime.ToLongDiscordTime(), Does.StartWith("<t:"));
        Assert.That(TestDateTime.ToShortDiscordDate(), Does.StartWith("<t:"));
        Assert.That(TestDateTime.ToLongDiscordDate(), Does.StartWith("<t:"));
        Assert.That(TestDateTime.ToLongDiscordDateShortTime(), Does.StartWith("<t:"));
        Assert.That(TestDateTime.ToLongDiscordDateLongTime(), Does.StartWith("<t:"));
        Assert.That(TestDateTime.ToRelativeDiscordTime(), Does.StartWith("<t:"));
    }

    [Test]
    public void AllFormats_ShouldEndWithCorrectSuffix()
    {
        // Assert
        Assert.That(TestDateTime.ToShortDiscordTime(), Does.EndWith(":t>"));
        Assert.That(TestDateTime.ToLongDiscordTime(), Does.EndWith(":T>"));
        Assert.That(TestDateTime.ToShortDiscordDate(), Does.EndWith(":d>"));
        Assert.That(TestDateTime.ToLongDiscordDate(), Does.EndWith(":D>"));
        Assert.That(TestDateTime.ToLongDiscordDateShortTime(), Does.EndWith(":f>"));
        Assert.That(TestDateTime.ToLongDiscordDateLongTime(), Does.EndWith(":F>"));
        Assert.That(TestDateTime.ToRelativeDiscordTime(), Does.EndWith(":R>"));
    }
}