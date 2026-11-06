namespace ExcelBotCs.Services.Discord.Records;

/// <summary>
///     A snapshot of one Discord message, taken from the lottery channel. The posting reconciliation (spec §5.10) has
///     to search for a message a crashed attempt may already have sent, and a Discord message object cannot be built
///     in a test, so the service hands back this plain record instead.
/// </summary>
public sealed record DiscordChannelMessage(
	ulong Id,
	ulong AuthorId,
	bool IsBot,
	string Content,
	DateTimeOffset CreatedAt);
