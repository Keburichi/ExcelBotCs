using ExcelBotCs.Extensions;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.Database.Events;

namespace ExcelBotCs.Models.DTO.Events;

public class EventResponse : BaseDto
{
    public string Name { get; set; }
    public string Description { get; set; }
    public EventType Type { get; set; } = EventType.Other;
    public DateTime StartDate { get; set; } // First occurrence start date

    public DateTime EndDate
    {
        get
        {
            // If there are no occurrences, simply calculate the date by adding the duration 
            if (Occurrences.IsNullOrEmpty())
                return StartDate.AddMinutes(Duration);

            // If there are occurrences, we calculate the end date of the last occurrence
            return Occurrences.OrderByDescending(x => x.OccurrenceDate).First().OccurrenceDate.AddMinutes(Duration);
        }
    }

    public int Duration { get; set; } // Duration in minutes

    // iCal source of truth
    public string ICalString { get; set; }

    // Signup configuration
    public SignupType SignupType { get; set; } = SignupType.SingleEvent;

    public string? PictureUrl { get; set; }
    public string? FightId { get; set; }
    public string? AuthorId { get; set; }
    public string? Organizer { get; set; }
    public int MaxNumberOfParticipants { get; set; }

    public List<SignupButtonConfigDto>? SignupButtonConfigs { get; set; }

    // Occurrences
    public List<EventOccurrenceDto> Occurrences { get; set; } = new();

    // Signups for this event
    public List<EventSignupDto> Signups { get; set; } = new();

    // Selected groups with participants for this event
    public List<EventGroupResponse> Groups { get; set; } = new();

    // Archive properties
    public bool IsArchived { get; set; } = false;
    public DateTime? ArchivedDate { get; set; }
    public string? ArchivedByUserId { get; set; }

    /// <summary>
    ///     An event can be archived when all occurrences are either Completed or Cancelled
    /// </summary>
    public bool CanBeArchived
    {
        get
        {
            if (Occurrences == null || !Occurrences.Any())
                return false;

            return Occurrences.All(o => o.Status == OccurrenceStatus.Completed ||
                                        o.Status == OccurrenceStatus.Cancelled);
        }
    }

    public bool AvailableForSignup
    {
        get
        {
            if (Occurrences == null || !Occurrences.Any())
                return false;

            if (GetCurrentOccurrence().Status != OccurrenceStatus.Scheduled)
                return false;

            return Groups.IsNullOrEmpty();
        }
    }

    /// <summary>
    ///     Can be used to get the current occurrence of an event to check if it has already been concluded.
    ///     This info is useful for determining if lottery guesses have been awarded
    /// </summary>
    /// <returns></returns>
    private EventOccurrenceDto GetCurrentOccurrence()
    {
        var occurrence = Occurrences
                             ?.Where(o => o.OccurrenceDate >= DateTime.UtcNow)
                             .OrderBy(o => o.OccurrenceDate)
                             .FirstOrDefault()
                         ?? Occurrences?.FirstOrDefault();

        if (occurrence == null)
        {
            occurrence = new EventOccurrenceDto
            {
                OccurrenceDate = StartDate,
                Status = OccurrenceStatus.Scheduled
            };
            Occurrences ??= new List<EventOccurrenceDto>();
            Occurrences.Add(occurrence);
        }

        return occurrence;
    }
}