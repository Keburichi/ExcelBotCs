namespace ExcelBotCs.Models.Database;

/// <summary>
///     Defines how signups work for an event
/// </summary>
public enum SignupType
{
    /// <summary>
    ///     Single occurrence event - one signup list
    /// </summary>
    SingleEvent = 0,

    /// <summary>
    ///     Recurring event where each occurrence has independent signups
    ///     Different people can sign up for each occurrence
    /// </summary>
    IndependentSignups = 1,

    /// <summary>
    ///     Recurring event where the same group participates in all occurrences
    ///     Signups apply to all occurrences
    /// </summary>
    LockedGroup = 2
}