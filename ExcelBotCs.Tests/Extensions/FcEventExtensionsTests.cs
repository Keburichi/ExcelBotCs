using ExcelBotCs.Extensions;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Modules.TeamFormation;
using ExcelBotCs.TestFramework.Attributes;

namespace ExcelBotCs.Tests.Extensions;

[TestFixture]
public class FcEventExtensionsTests
{
    [TestIsNullOrEmptyString]
    public void CreateUpcomingRosterMessage_WithNullOrEmptyEventName_ShouldHandleCorrectly(string eventName)
    {
        // Arrange
        var fcEvent = new Event
        {
            Id = "1",
            Name = eventName,
            Duration = 60,
            Occurrences = new List<EventOccurrence>
            {
                new()
                {
                    Id = "occ1",
                    OccurrenceDate = DateTime.UtcNow.AddDays(1),
                    Status = OccurrenceStatus.Scheduled,
                    Participants = new List<EventParticipant>()
                }
            }
        };

        // Act
        var result = fcEvent.CreateUpcomingRosterMessage();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Does.Contain("Upcoming roster for:"));
    }

    [Test]
    public void CreateUpcomingRosterMessage_WithNoOccurrences_ShouldReturnNoUpcomingMessage()
    {
        // Arrange
        var fcEvent = new Event
        {
            Id = "1",
            Name = "Test Event",
            Duration = 60,
            Occurrences = null
        };

        // Act
        var result = fcEvent.CreateUpcomingRosterMessage();

        // Assert
        Assert.That(result, Does.Contain("No upcoming occurrences"));
        Assert.That(result, Does.Contain("Test Event"));
    }

    [Test]
    public void CreateUpcomingRosterMessage_WithEmptyOccurrences_ShouldReturnNoUpcomingMessage()
    {
        // Arrange
        var fcEvent = new Event
        {
            Id = "1",
            Name = "Test Event",
            Duration = 60,
            Occurrences = new List<EventOccurrence>()
        };

        // Act
        var result = fcEvent.CreateUpcomingRosterMessage();

        // Assert
        Assert.That(result, Does.Contain("No upcoming occurrences"));
    }

    [Test]
    public void CreateUpcomingRosterMessage_WithScheduledOccurrence_ShouldReturnFormattedMessage()
    {
        // Arrange
        var occurrenceDate = DateTime.UtcNow.AddDays(1);
        var fcEvent = new Event
        {
            Id = "1",
            Name = "Raid Night",
            Duration = 120,
            Occurrences = new List<EventOccurrence>
            {
                new()
                {
                    Id = "occ1",
                    OccurrenceDate = occurrenceDate,
                    Status = OccurrenceStatus.Scheduled,
                    Participants = new List<EventParticipant>
                    {
                        new() { DiscordUserId = "user1", Role = Role.Tank },
                        new() { DiscordUserId = "user2", Role = Role.Healer }
                    }
                }
            }
        };

        // Act
        var result = fcEvent.CreateUpcomingRosterMessage();

        // Assert
        Assert.That(result, Does.Contain("Upcoming roster for: Raid Night"));
        Assert.That(result, Does.Contain("**Date:**"));
        Assert.That(result, Does.Contain("**In:**"));
        Assert.That(result, Does.Contain("**Duration:** 120 minutes"));
        Assert.That(result, Does.Contain("<@user1>"));
        Assert.That(result, Does.Contain("<@user2>"));
    }

    [Test]
    public void CreateUpcomingRosterMessage_WithMultipleRoles_ShouldIncludeAllRoles()
    {
        // Arrange
        var occurrenceDate = DateTime.UtcNow.AddDays(1);
        var fcEvent = new Event
        {
            Id = "1",
            Name = "Static Practice",
            Duration = 90,
            Occurrences = new List<EventOccurrence>
            {
                new()
                {
                    Id = "occ1",
                    OccurrenceDate = occurrenceDate,
                    Status = OccurrenceStatus.Scheduled,
                    Participants = new List<EventParticipant>
                    {
                        new() { DiscordUserId = "tank1", Role = Role.Tank },
                        new() { DiscordUserId = "tank2", Role = Role.Tank },
                        new() { DiscordUserId = "healer1", Role = Role.Healer },
                        new() { DiscordUserId = "melee1", Role = Role.Melee },
                        new() { DiscordUserId = "caster1", Role = Role.Caster },
                        new() { DiscordUserId = "ranged1", Role = Role.Ranged }
                    }
                }
            }
        };

        // Act
        var result = fcEvent.CreateUpcomingRosterMessage();

        // Assert
        Assert.That(result, Does.Contain(":RoleTank:"));
        Assert.That(result, Does.Contain(":RoleHealer:"));
        Assert.That(result, Does.Contain(":RoleMelee:"));
        Assert.That(result, Does.Contain(":RoleCaster:"));
        Assert.That(result, Does.Contain(":RoleRanged:"));
        Assert.That(result, Does.Contain("<@tank1>"));
        Assert.That(result, Does.Contain("<@tank2>"));
        Assert.That(result, Does.Contain("<@healer1>"));
        Assert.That(result, Does.Contain("<@melee1>"));
        Assert.That(result, Does.Contain("<@caster1>"));
        Assert.That(result, Does.Contain("<@ranged1>"));
    }

    [Test]
    public void CreateUpcomingRosterMessage_WithPastOccurrence_ShouldSelectFirstScheduledFutureOccurrence()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddDays(2);
        var fcEvent = new Event
        {
            Id = "1",
            Name = "Weekly Raid",
            Duration = 60,
            Occurrences = new List<EventOccurrence>
            {
                new()
                {
                    Id = "occ1",
                    OccurrenceDate = DateTime.UtcNow.AddDays(-1), // Past
                    Status = OccurrenceStatus.Scheduled,
                    Participants = new List<EventParticipant>()
                },
                new()
                {
                    Id = "occ2",
                    OccurrenceDate = futureDate, // Future
                    Status = OccurrenceStatus.Scheduled,
                    Participants = new List<EventParticipant>
                    {
                        new() { DiscordUserId = "user1", Role = Role.Tank }
                    }
                }
            }
        };

        // Act
        var result = fcEvent.CreateUpcomingRosterMessage();

        // Assert
        Assert.That(result, Does.Contain("Upcoming roster for: Weekly Raid"));
        Assert.That(result, Does.Contain("<@user1>"));
    }

    [Test]
    public void CreateUpcomingRosterMessage_WithNullParticipants_ShouldHandleGracefully()
    {
        // Arrange
        var fcEvent = new Event
        {
            Id = "1",
            Name = "Empty Event",
            Duration = 60,
            Occurrences = new List<EventOccurrence>
            {
                new()
                {
                    Id = "occ1",
                    OccurrenceDate = DateTime.UtcNow.AddDays(1),
                    Status = OccurrenceStatus.Scheduled,
                    Participants = null
                }
            }
        };

        // Act
        var result = fcEvent.CreateUpcomingRosterMessage();

        // Assert
        Assert.That(result, Does.Contain("Upcoming roster for: Empty Event"));
        Assert.That(result, Does.Contain(":RoleTank:"));
        Assert.That(result, Does.Contain(":RoleHealer:"));
    }

    [Test]
    public void CreateUpcomingRosterMessage_WithEmptyParticipants_ShouldShowEmptyRoles()
    {
        // Arrange
        var fcEvent = new Event
        {
            Id = "1",
            Name = "No Signups Yet",
            Duration = 60,
            Occurrences = new List<EventOccurrence>
            {
                new()
                {
                    Id = "occ1",
                    OccurrenceDate = DateTime.UtcNow.AddDays(1),
                    Status = OccurrenceStatus.Scheduled,
                    Participants = new List<EventParticipant>()
                }
            }
        };

        // Act
        var result = fcEvent.CreateUpcomingRosterMessage();

        // Assert
        Assert.That(result, Does.Contain("Upcoming roster for: No Signups Yet"));
        Assert.That(result, Does.Contain(":RoleTank:"));
        Assert.That(result, Does.Contain(":RoleHealer:"));
    }

    [Test]
    public void CreateUpcomingRosterMessage_WithMultipleTanks_ShouldListAllTanks()
    {
        // Arrange
        var fcEvent = new Event
        {
            Id = "1",
            Name = "Tank Party",
            Duration = 60,
            Occurrences = new List<EventOccurrence>
            {
                new()
                {
                    Id = "occ1",
                    OccurrenceDate = DateTime.UtcNow.AddDays(1),
                    Status = OccurrenceStatus.Scheduled,
                    Participants = new List<EventParticipant>
                    {
                        new() { DiscordUserId = "tank1", Role = Role.Tank },
                        new() { DiscordUserId = "tank2", Role = Role.Tank },
                        new() { DiscordUserId = "tank3", Role = Role.Tank }
                    }
                }
            }
        };

        // Act
        var result = fcEvent.CreateUpcomingRosterMessage();

        // Assert
        Assert.That(result, Does.Contain("<@tank1>"));
        Assert.That(result, Does.Contain("<@tank2>"));
        Assert.That(result, Does.Contain("<@tank3>"));
    }

    [Test]
    public void CreateUpcomingRosterMessage_WithCompletedOccurrence_ShouldSkipAndFindNextScheduled()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddDays(3);
        var fcEvent = new Event
        {
            Id = "1",
            Name = "Recurring Event",
            Duration = 60,
            Occurrences = new List<EventOccurrence>
            {
                new()
                {
                    Id = "occ1",
                    OccurrenceDate = DateTime.UtcNow.AddDays(1),
                    Status = OccurrenceStatus.Completed,
                    Participants = new List<EventParticipant>()
                },
                new()
                {
                    Id = "occ2",
                    OccurrenceDate = futureDate,
                    Status = OccurrenceStatus.Scheduled,
                    Participants = new List<EventParticipant>
                    {
                        new() { DiscordUserId = "user1", Role = Role.Healer }
                    }
                }
            }
        };

        // Act
        var result = fcEvent.CreateUpcomingRosterMessage();

        // Assert
        Assert.That(result, Does.Contain("Upcoming roster for: Recurring Event"));
        Assert.That(result, Does.Contain("<@user1>"));
    }

    [Test]
    public void CreateUpcomingRosterMessage_ShouldFormatDurationCorrectly()
    {
        // Arrange
        var fcEvent = new Event
        {
            Id = "1",
            Name = "Long Event",
            Duration = 180,
            Occurrences = new List<EventOccurrence>
            {
                new()
                {
                    Id = "occ1",
                    OccurrenceDate = DateTime.UtcNow.AddDays(1),
                    Status = OccurrenceStatus.Scheduled,
                    Participants = new List<EventParticipant>()
                }
            }
        };

        // Act
        var result = fcEvent.CreateUpcomingRosterMessage();

        // Assert
        Assert.That(result, Does.Contain("**Duration:** 180 minutes"));
    }
}