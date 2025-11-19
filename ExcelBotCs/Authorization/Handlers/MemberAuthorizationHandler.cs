using ExcelBotCs.Authorization.Requirements;
using ExcelBotCs.Services;
using Microsoft.AspNetCore.Authorization;

namespace ExcelBotCs.Authorization.Handlers;

public sealed class MemberAuthorizationHandler(ICurrentMemberAccessor current)
    : AuthorizationHandler<MemberRequirement>
{
    private readonly ICurrentMemberAccessor _current = current;

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, MemberRequirement requirement)
    {
        if (context.User?.Identity is null || !context.User.Identity.IsAuthenticated)
        {
            return;
        }

        var member = await _current.GetCurrentAsync();
        if (member?.IsMember == true)
        {
            context.Succeed(requirement);
        }
    }
}
