namespace ExcelBotCs.Utilities;

/// <summary>
///     Measures how much text a Discord message actually puts in front of a reader. Mentions and custom emoji are
///     posted as raw markup (<c>&lt;@&amp;id&gt;</c>, <c>&lt;#id&gt;</c>, <c>&lt;:kekDoggo:id&gt;</c>) that Discord
///     replaces with a rendered glyph or name: a role or member mention renders as <c>@</c> plus the name, a channel
///     mention as <c>#</c> plus the name, and a custom emoji as <c>:name:</c>.
///     <para>
///         The ids are deployment configuration and the names are Discord-side data, so this measure collapses a
///         mention to its glyph and keeps an emoji's name. That is why the assembled announcement reports a visible
///         count slightly below its raw length (spec §5.6: ~928 no-win, ~1,003 win, raw markup under ~1,100).
///     </para>
/// </summary>
public static class DiscordVisibleText
{
	private const string RoleMentionPattern = @"<@&\d+>";
	private const string UserMentionPattern = @"<@!?\d+>";
	private const string ChannelMentionPattern = @"<#\d+>";
	private const string CustomEmojiPattern = @"<a?:([A-Za-z0-9_]+):\d+>";

	private static readonly System.Text.RegularExpressions.Regex RoleMention = Compile(RoleMentionPattern);
	private static readonly System.Text.RegularExpressions.Regex UserMention = Compile(UserMentionPattern);
	private static readonly System.Text.RegularExpressions.Regex ChannelMention = Compile(ChannelMentionPattern);
	private static readonly System.Text.RegularExpressions.Regex CustomEmoji = Compile(CustomEmojiPattern);

	/// <summary>The rendered length of a message: mentions become their glyph, custom emoji keep their name.</summary>
	public static int Measure(string text)
	{
		var visible = RoleMention.Replace(text, "@");
		visible = UserMention.Replace(visible, "@");
		visible = ChannelMention.Replace(visible, "#");
		visible = CustomEmoji.Replace(visible, ":$1:");
		return visible.Length;
	}

	private static System.Text.RegularExpressions.Regex Compile(string pattern)
	{
		return new System.Text.RegularExpressions.Regex(pattern,
			System.Text.RegularExpressions.RegexOptions.Compiled);
	}
}
