using ExcelBotCs.Extensions;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Services.API;
using ExcelBotCs.Services.API.Interfaces;

namespace ExcelBotCs.Services;

public sealed class CurrentMemberAccessor(IHttpContextAccessor httpContextAccessor, IMemberService memberService)
    : ICurrentMemberAccessor
{
    public const string HttpContextItemKey = "CurrentMember";

    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IMemberService _memberService = memberService;

    public Member? Current => _httpContextAccessor.HttpContext?.Items[HttpContextItemKey] as Member;

    public async Task<Member?> GetCurrentAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null) return null;

        if (httpContext.Items.TryGetValue(HttpContextItemKey, out var existing) && existing is Member m)
            return m;

        // Not resolved yet; load from claims if authenticated
        var user = httpContext.User;
        if (user?.Identity is null || !user.Identity.IsAuthenticated) return null;

        var discordId = user.Claims.ToList().GetDiscordId();
        if (string.IsNullOrWhiteSpace(discordId))
            return null;

        var member = await _memberService.GetByDiscordId(discordId);
        if (member is not null)
        {
            httpContext.Items[HttpContextItemKey] = member;
        }

        return member;
    }
}