using ExcelBotCs.Extensions;
using ExcelBotCs.Services;
using ExcelBotCs.Services.API;
using ExcelBotCs.Services.API.Interfaces;

namespace ExcelBotCs.Middleware;

public class CurrentMemberMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context, IMemberService memberService)
    {
        // Resolve Member only for authenticated requests; keep it cheap otherwise
        var user = context.User;
        if (user?.Identity is not null && user.Identity.IsAuthenticated)
        {
            if (!context.Items.ContainsKey(CurrentMemberAccessor.HttpContextItemKey))
            {
                var discordId = user.Claims.ToList().GetDiscordId();
                if (!string.IsNullOrWhiteSpace(discordId))
                {
                    var member = await memberService.GetByDiscordId(discordId);
                    if (member is not null)
                    {
                        context.Items[CurrentMemberAccessor.HttpContextItemKey] = member;
                    }
                }
            }
        }

        await _next(context);
    }
}