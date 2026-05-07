using ExcelBotCs.Models.Database;
using ExcelBotCs.Modules.TeamFormation;
using ExcelBotCs.Services;

namespace ExcelBotCs.Tests.Models.Database;

public class EventTests
{
    private readonly IICalService _iCalService;

    public EventTests()
    {
        _iCalService = new ICalService();
    }

    [Fact]
    public void AvailableForSignup_EmptyOccurrences_ReturnsFalse()
    {
        var sut = new Event();

        sut.AvailableForSignup.ShouldBeFalse();
    }

    [Theory]
    [InlineData(OccurrenceStatus.Cancelled)]
    [InlineData(OccurrenceStatus.Completed)]
    [InlineData(OccurrenceStatus.InProgress)]
    public void AvailableForSignup_OccurrencesNotScheduled_ReturnsFalse(OccurrenceStatus occurrenceStatus)
    {
        var sut = new Event
        {
            MaxNumberOfParticipants = 1,
            Occurrences = new List<EventOccurrence>
            {
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Status = occurrenceStatus,
                    OccurrenceDate = DateTime.UtcNow.AddMinutes(5),
                    Signups = new List<EventSignup>()
                }
            }
        };

        sut.AvailableForSignup.ShouldBeFalse();
    }

    [Fact]
    public void AvailableForSignup_PreviousOccurrenceNotInPast_ReturnsFalse()
    {
        var sut = new Event
        {
            MaxNumberOfParticipants = 1,
            Occurrences = new List<EventOccurrence>
            {
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Status = OccurrenceStatus.Cancelled,
                    OccurrenceDate = DateTime.UtcNow.AddMinutes(5),
                    Signups = new List<EventSignup>()
                },
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Status = OccurrenceStatus.Scheduled,
                    OccurrenceDate = DateTime.UtcNow.AddDays(5),
                    Signups = new List<EventSignup>()
                }
            }
        };

        sut.AvailableForSignup.ShouldBeFalse();
    }

    [Fact]
    public void AvailableForSignup_ParticipantsSelected_ReturnsFalse()
    {
        var sut = new Event
        {
            MaxNumberOfParticipants = 1,
            Occurrences = new List<EventOccurrence>
            {
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Status = OccurrenceStatus.Cancelled,
                    OccurrenceDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(5)),
                    Signups = new List<EventSignup>()
                },
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Status = OccurrenceStatus.Scheduled,
                    OccurrenceDate = DateTime.UtcNow.AddDays(5),
                    Signups = new List<EventSignup>(),
                    Participants = new List<EventParticipant>
                    {
                        new()
                        {
                            DiscordUserId = "1234567890",
                            Role = Role.Caster,
                            SelectionDate = DateTime.UtcNow
                        }
                    }
                }
            }
        };

        sut.AvailableForSignup.ShouldBeFalse();
    }

    [Fact]
    public void AvailableForSignup_NoSignups_ReturnsTrue()
    {
        var sut = new Event
        {
            MaxNumberOfParticipants = 1,
            Occurrences = new List<EventOccurrence>
            {
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Status = OccurrenceStatus.Cancelled,
                    OccurrenceDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(5)),
                    Signups = new List<EventSignup>()
                },
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Status = OccurrenceStatus.Scheduled,
                    OccurrenceDate = DateTime.UtcNow.AddDays(5),
                    Signups = new List<EventSignup>()
                }
            }
        };

        sut.AvailableForSignup.ShouldBeTrue();
    }

    [Fact]
    public void AvailableForSignup_SignupsExist_ReturnsTrue()
    {
        var sut = new Event
        {
            MaxNumberOfParticipants = 1,
            Occurrences = new List<EventOccurrence>
            {
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Status = OccurrenceStatus.Cancelled,
                    OccurrenceDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(5)),
                    Signups = new List<EventSignup>()
                },
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Status = OccurrenceStatus.Scheduled,
                    OccurrenceDate = DateTime.UtcNow.AddDays(5),
                    Signups = new List<EventSignup>
                    {
                        new()
                        {
                            DiscordUserId = "1234567890",
                            Roles = new List<Role>
                            {
                                Role.Caster,
                                Role.Healer
                            },
                            SignupDate = DateTime.UtcNow
                        }
                    }
                }
            }
        };

        sut.AvailableForSignup.ShouldBeTrue();
    }
}
