using ExcelBotCs.Extensions;

namespace ExcelBotCs.Models.Database.Events;

public class Event : BaseEntity
{
    // Denormalized fields for efficient querying
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

    // Discord message tracking (internal only — not exposed via API)
    public string SignupPostId { get; set; }
    public string? UpcomingRosterMessageId { get; set; }
    public string? PictureUrl { get; set; }
    public string? FightId { get; set; }
    public string? AuthorId { get; set; }
    public string? Organizer { get; set; }
    public int MaxNumberOfParticipants { get; set; }
    public int RequiredParticipants { get; set; }

    // Signup button configuration (null = legacy 5-role buttons)
    public List<SignupButtonConfig> SignupButtonConfigs { get; set; } = new();

    // Occurrences - always has at least one
    public List<EventOccurrence> Occurrences { get; set; } = new();

    // Signups for this event
    public List<EventSignup> Signups { get; set; } = new();

    // Selected groups with participants for this event
    public List<EventGroup> Groups { get; set; } = new();

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
    public EventOccurrence GetCurrentOccurrence()
    {
        // Determine target occurrence: prefer next upcoming scheduled occurrence, fall back to the first; create if none exists
        var occurrence = Occurrences
                             ?.Where(o => o.OccurrenceDate >= DateTime.UtcNow)
                             .OrderBy(o => o.OccurrenceDate)
                             .FirstOrDefault()
                         ?? Occurrences?.FirstOrDefault();

        if (occurrence == null)
        {
            occurrence = new EventOccurrence
            {
                OccurrenceDate = StartDate,
                Status = OccurrenceStatus.Scheduled
            };
            Occurrences ??= new List<EventOccurrence>();
            Occurrences.Add(occurrence);
        }

        return occurrence;
    }
}