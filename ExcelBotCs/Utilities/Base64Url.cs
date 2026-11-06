namespace ExcelBotCs.Utilities;

/// <summary>
///     The base64url codec behind the opaque keyset cursors (spec §5.4, §5.8). One helper so both cursors encode and
///     decode exactly the same way: a cursor is opaque to the client, but it must round-trip through its own endpoint.
/// </summary>
public static class Base64Url
{
	/// <summary>Encodes bytes as unpadded base64url (<c>-</c> and <c>_</c> instead of <c>+</c> and <c>/</c>).</summary>
	public static string Encode(byte[] bytes)
	{
		return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
	}

	/// <summary>
	///     Decodes a base64url value, restoring the padding the encoder dropped. <c>false</c> (with empty bytes) means
	///     the value is not base64url at all, which the caller answers with a typed 400 rather than a 500.
	/// </summary>
	public static bool TryDecode(string value, out byte[] bytes)
	{
		var padded = value.Replace('-', '+').Replace('_', '/');
		padded = (padded.Length % 4) switch
		{
			2 => padded + "==",
			3 => padded + "=",
			_ => padded
		};

		try
		{
			bytes = Convert.FromBase64String(padded);
			return true;
		}
		catch (FormatException)
		{
			bytes = [];
			return false;
		}
	}
}
