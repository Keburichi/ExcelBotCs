using ExcelBotCs.Models.DTO;
using ExcelBotCs.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExcelBotCs.Controllers;

/// <summary>
///     The one actor helper of the two lottery controllers (spec §8, the audit trail). They used to carry their own
///     <c>GetCurrentUserDiscordId</c> with divergent semantics — one called <c>ulong.Parse</c> and threw on a malformed
///     snowflake, the other fell back to <c>0</c> and audited a user that does not exist. Here an unresolvable id is
///     <c>null</c>, and the caller answers with a typed 401 instead of writing it.
/// </summary>
internal static class LotteryActorExtensions
{
	/// <summary>The code the typed failure carries, so the page tells a lost session from a rejected action.</summary>
	internal const string UnauthenticatedCode = "unauthenticated";

	/// <summary>The acting Discord id, or <c>null</c> when the request has no member or an unparsable id.</summary>
	internal static async Task<ulong?> TryGetActorIdAsync(this ICurrentMemberAccessor accessor)
	{
		var member = await accessor.GetCurrentAsync();
		return ulong.TryParse(member?.DiscordId, out var discordId) ? discordId : null;
	}

	/// <summary>The typed 401 a mutation returns instead of recording actor 0.</summary>
	internal static IActionResult MissingActor(this ControllerBase controller)
	{
		return controller.Unauthorized(new LotteryHistoryError(UnauthenticatedCode,
			"Your session has no Discord id; sign in again before changing the lottery."));
	}
}
