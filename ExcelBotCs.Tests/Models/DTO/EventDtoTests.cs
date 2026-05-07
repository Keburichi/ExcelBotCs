using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.Modules.TeamFormation;

namespace ExcelBotCs.Tests.Models.DTO;

public class EventDtoTests
{
    [Fact]
    public void AvailableForSignup_EmptyOccurrences_ReturnsFalse()
    {
        var sut = new EventDto();

        sut.AvailableForSignup.ShouldBeFalse();
    }

    [Theory]
    [InlineData(OccurrenceStatus.Cancelled)]
    [InlineData(OccurrenceStatus.Completed)]
    [InlineData(OccurrenceStatus.InProgress)]
    public void AvailableForSignup_OccurrencesNotScheduled_ReturnsFalse(OccurrenceStatus occurrenceStatus)
    {
        var sut = new EventDto
        {
            MaxNumberOfParticipants = 1,
            Occurrences = new List<EventOccurrenceDto>
            {
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Status = occurrenceStatus,
                    OccurrenceDate = DateTime.UtcNow.AddMinutes(5),
                    Signups = new List<EventSignupDto>()
                }
            }
        };

        sut.AvailableForSignup.ShouldBeFalse();
    }

    [Fact]
    public void AvailableForSignup_PreviousOccurrenceNotInPast_ReturnsFalse()
    {
        var sut = new EventDto
        {
            MaxNumberOfParticipants = 1,
            Occurrences = new List<EventOccurrenceDto>
            {
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Status = OccurrenceStatus.Cancelled,
                    OccurrenceDate = DateTime.UtcNow.AddMinutes(5),
                    Signups = new List<EventSignupDto>()
                },
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Status = OccurrenceStatus.Scheduled,
                    OccurrenceDate = DateTime.UtcNow.AddDays(5),
                    Signups = new List<EventSignupDto>()
                }
            }
        };

        sut.AvailableForSignup.ShouldBeFalse();
    }

    [Fact]
    public void AvailableForSignup_ParticipantsSelected_ReturnsFalse()
    {
        var sut = new EventDto
        {
            MaxNumberOfParticipants = 1,
            Occurrences = new List<EventOccurrenceDto>
            {
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Status = OccurrenceStatus.Cancelled,
                    OccurrenceDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(5)),
                    Signups = new List<EventSignupDto>()
                },
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Status = OccurrenceStatus.Scheduled,
                    OccurrenceDate = DateTime.UtcNow.AddDays(5),
                    Signups = new List<EventSignupDto>(),
                    Participants = new List<EventParticipantDto>
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
        var sut = new EventDto
        {
            MaxNumberOfParticipants = 1,
            Occurrences = new List<EventOccurrenceDto>
            {
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Status = OccurrenceStatus.Cancelled,
                    OccurrenceDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(5)),
                    Signups = new List<EventSignupDto>()
                },
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Status = OccurrenceStatus.Scheduled,
                    OccurrenceDate = DateTime.UtcNow.AddDays(5),
                    Signups = new List<EventSignupDto>()
                }
            }
        };

        sut.AvailableForSignup.ShouldBeTrue();
    }

    [Fact]
    public void AvailableForSignup_SignupsExist_ReturnsTrue()
    {
        var sut = new EventDto
        {
            MaxNumberOfParticipants = 1,
            Occurrences = new List<EventOccurrenceDto>
            {
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Status = OccurrenceStatus.Cancelled,
                    OccurrenceDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(5)),
                    Signups = new List<EventSignupDto>()
                },
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Status = OccurrenceStatus.Scheduled,
                    OccurrenceDate = DateTime.UtcNow.AddDays(5),
                    Signups = new List<EventSignupDto>
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
