namespace ExcelBotCs.Models.Config;

/// <summary>
///     The public base URL of this web app, used in links the bot posts (spec §7: <c>WebBaseUrl</c>, with
///     <c>Jwt:Issuer</c> allowed to stand in for it). It is a top-level scalar rather than a section, so
///     <see cref="Extensions.ConfigExtensions" /> binds it directly instead of through
///     <see cref="Attributes.OptionsSectionAttribute" />. A missing value is not fatal: the startup check reports it.
/// </summary>
public class WebBaseUrlOptions
{
	public const string Key = "WebBaseUrl";
	public const string IssuerKey = "Jwt:Issuer";

	/// <summary>The resolved base URL, without a trailing slash, or an empty string when unconfigured.</summary>
	public string BaseUrl { get; set; } = string.Empty;

	/// <summary><c>WebBaseUrl</c>, falling back to <c>Jwt:Issuer</c>; either may be absent.</summary>
	public static string Resolve(IConfiguration configuration)
	{
		var configured = configuration[Key];
		if (!string.IsNullOrWhiteSpace(configured))
			return Normalize(configured);

		return Normalize(configuration[IssuerKey]);
	}

	private static string Normalize(string? value)
	{
		// Trailing slashes are stripped so callers can append "/lottery/history" without producing a double slash.
		return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().TrimEnd('/');
	}
}
