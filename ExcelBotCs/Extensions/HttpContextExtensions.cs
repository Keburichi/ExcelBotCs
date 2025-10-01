using ExcelBotCs.Models.Database;
using ExcelBotCs.Services;

namespace ExcelBotCs.Extensions;

public static class HttpContextExtensions
{
    public static Member? GetCurrentMember(this HttpContext httpContext)
    {
        return httpContext.Items.TryGetValue(CurrentMemberAccessor.HttpContextItemKey, out var value)
            ? value as Member
            : null;
    }
}