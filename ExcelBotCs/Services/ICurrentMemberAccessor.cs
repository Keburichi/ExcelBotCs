using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Services;

public interface ICurrentMemberAccessor
{
    /// <summary>
    /// Returns the current authenticated Member resolved for this request, or null if the request is unauthenticated
    /// or the Member was not found in the database.
    /// </summary>
    Task<Member?> GetCurrentAsync();

    /// <summary>
    /// Returns the resolved Member if it has already been loaded for this request. Otherwise returns null.
    /// </summary>
    Member? Current { get; }
}