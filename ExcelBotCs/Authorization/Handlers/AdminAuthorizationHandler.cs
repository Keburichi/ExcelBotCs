using ExcelBotCs.Authorization.Requirements;
using ExcelBotCs.Services;
using Microsoft.AspNetCore.Authorization;

namespace ExcelBotCs.Authorization.Handlers;

public sealed class AdminAuthorizationHandler(ICurrentMemberAccessor current)
    : AuthorizationHandler<AdminRequirement>
{
    private readonly ICurrentMemberAccessor _current = current;

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminRequirement requirement)
    {
        // Must be authenticated
        if (context.User?.Identity is null || !context.User.Identity.IsAuthenticated)
        {
            return; // no success
        }

        var member = await _current.GetCurrentAsync();
        if (member?.IsAdmin == true)
        {
            context.Succeed(requirement);
        }
    }
}
