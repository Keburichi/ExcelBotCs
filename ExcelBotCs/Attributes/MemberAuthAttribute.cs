using Microsoft.AspNetCore.Authorization;
using ExcelBotCs.Authorization;

namespace ExcelBotCs.Attributes;

public class MemberAuthAttribute : AuthorizeAttribute
{
    public MemberAuthAttribute()
    {
        Policy = Policies.Member;
    }
}
