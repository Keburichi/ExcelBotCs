using Microsoft.AspNetCore.Authorization;
using ExcelBotCs.Authorization;

namespace ExcelBotCs.Attributes;

public class AdminAuthAttribute : AuthorizeAttribute
{
    public AdminAuthAttribute()
    {
        // Use custom policy that checks Member.IsAdmin via ICurrentMemberAccessor
        Policy = Policies.Admin;
    }
}