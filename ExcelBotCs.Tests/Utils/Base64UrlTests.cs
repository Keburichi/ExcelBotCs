using System.Text;
using ExcelBotCs.Utilities;

namespace ExcelBotCs.Tests.Utils;

/// <summary>
///     The one base64url codec behind both keyset cursors (spec §5.4, §5.8). The cursors themselves are opaque, so this
///     pins the round trip and the invalid-input answer that turns into a typed 400 instead of a 500.
/// </summary>
public class Base64UrlTests
{
	private static readonly byte[] Payload = Encoding.UTF8.GetBytes("2026-11-10|638000000000000000|abc");

	[Fact]
	public void Encode_ProducesUnpaddedUrlSafeText()
	{
		var encoded = Base64Url.Encode(Payload);

		encoded.ShouldNotContain("=");
		encoded.ShouldNotContain("+");
		encoded.ShouldNotContain("/");
	}

	[Fact]
	public void TryDecode_RoundTripsEveryEncoderOutput()
	{
		var encoded = Base64Url.Encode(Payload);

		Base64Url.TryDecode(encoded, out var decoded).ShouldBeTrue();

		decoded.ShouldBe(Payload);
	}

	[Theory]
	[InlineData("not a cursor!")]
	[InlineData("%%%")]
	public void TryDecode_WithANonBase64Value_ReturnsFalseAndNoBytes(string value)
	{
		Base64Url.TryDecode(value, out var bytes).ShouldBeFalse();

		bytes.ShouldBeEmpty();
	}
}
