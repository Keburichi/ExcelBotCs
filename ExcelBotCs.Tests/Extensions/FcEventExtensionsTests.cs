using ExcelBotCs.Extensions;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Modules.TeamFormation;
using ExcelBotCs.TestFramework.TestData;

namespace ExcelBotCs.Tests.Extensions;

public class FcEventExtensionsTests
{
    [Theory]
    [MemberData(nameof(NullOrEmptyStringData.Values), MemberType = typeof(NullOrEmptyStringData))]
    public void CreateUpcomingRosterMessage_WithNullOrEmptyEventName_ShouldHandleCorrectly(string? eventName)
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
        result.ShouldNotBeNull();
        result.ShouldContain("Upcoming roster for:");
    }

    [Fact]
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
        result.ShouldContain("No upcoming occurrences");
        result.ShouldContain("Test Event");
    }

    [Fact]
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
        result.ShouldContain("No upcoming occurrences");
    }

    [Fact]
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
        result.ShouldContain("Upcoming roster for: Raid Night");
        result.ShouldContain("**Date:**");
        result.ShouldContain("**In:**");
        result.ShouldContain("**Duration:** 120 minutes");
        result.ShouldContain("<@user1>");
        result.ShouldContain("<@user2>");
    }

    [Fact]
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
        result.ShouldContain(":RoleTank:");
        result.ShouldContain(":RoleHealer:");
        result.ShouldContain(":RoleMelee:");
        result.ShouldContain(":RoleCaster:");
        result.ShouldContain(":RoleRanged:");
        result.ShouldContain("<@tank1>");
        result.ShouldContain("<@tank2>");
        result.ShouldContain("<@healer1>");
        result.ShouldContain("<@melee1>");
        result.ShouldContain("<@caster1>");
        result.ShouldContain("<@ranged1>");
    }

    [Fact]
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
        result.ShouldContain("Upcoming roster for: Weekly Raid");
        result.ShouldContain("<@user1>");
    }

    [Fact]
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
        result.ShouldContain("Upcoming roster for: Empty Event");
        result.ShouldContain(":RoleTank:");
        result.ShouldContain(":RoleHealer:");
    }

    [Fact]
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
        result.ShouldContain("Upcoming roster for: No Signups Yet");
        result.ShouldContain(":RoleTank:");
        result.ShouldContain(":RoleHealer:");
    }

    [Fact]
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
        result.ShouldContain("<@tank1>");
        result.ShouldContain("<@tank2>");
        result.ShouldContain("<@tank3>");
    }

    [Fact]
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
        result.ShouldContain("Upcoming roster for: Recurring Event");
        result.ShouldContain("<@user1>");
    }

    [Fact]
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
        result.ShouldContain("**Duration:** 180 minutes");
    }
}
