using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ExcelBotCs.HealthChecks;

/// <summary>
///     The <c>/health</c> response body (#115, spec §5.10 "Health", §14). The aggregate text writer only names the
///     worst status, which cannot carry the <c>lottery</c> detail the runbook asks operators to watch, so the endpoint
///     writes one JSON report: the aggregate status, the total duration and every check with its own status,
///     description and data.
///     <para>
///         The body is diagnostics, not the probe contract: the endpoint's status code stays the signal, and it is
///         mapped explicitly in <c>Program</c> — <see cref="HealthStatus.Degraded" /> is a 200, so a degraded lottery
///         detail can never take the container down.
///     </para>
///     <para>
///         The endpoint is anonymous, so it redacts the data keys that can carry raw dependency errors (see
///         <see cref="SensitiveDataKeys" />). The authenticated admin status endpoint keeps that detail.
///     </para>
/// </summary>
public static class HealthResponseWriter
{
	/// <summary>The content type of the report.</summary>
	public const string ContentType = "application/json; charset=utf-8";

	/// <summary>What a redacted data value reads as.</summary>
	public const string Redacted = "[redacted]";

	/// <summary>
	///     Data keys that are never written to the anonymous report: they can carry raw exception text from a
	///     dependency — the lottery round's <c>lastError</c> and the startup check's <c>problems</c> are the two that do
	///     today. <c>/health</c> is unauthenticated, so no exception message leaves it; the admin status endpoint is the
	///     surface for the detail.
	/// </summary>
	private static readonly HashSet<string> SensitiveDataKeys = new(StringComparer.OrdinalIgnoreCase)
	{
		"lastError", "problems", "exception", "error", "stackTrace"
	};

	public static async Task WriteAsync(HttpContext context, HealthReport report)
	{
		// Rendered into a buffer first: Utf8JsonWriter flushes synchronously, and a host may (the test server does)
		// forbid synchronous response streams.
		using var buffer = new MemoryStream();
		await using (var writer = new Utf8JsonWriter(buffer))
		{
			Write(writer, report);
		}

		context.Response.ContentType = ContentType;
		await context.Response.Body.WriteAsync(buffer.ToArray());
	}

	private static void Write(Utf8JsonWriter writer, HealthReport report)
	{
		writer.WriteStartObject();

		writer.WriteString("status", report.Status.ToString());
		writer.WriteNumber("totalDurationMs", Math.Round(report.TotalDuration.TotalMilliseconds, 1));

		writer.WriteStartObject("checks");
		foreach (var (name, entry) in report.Entries)
		{
			writer.WriteStartObject(name);
			writer.WriteString("status", entry.Status.ToString());

			if (!string.IsNullOrWhiteSpace(entry.Description))
				writer.WriteString("description", entry.Description);

			writer.WriteNumber("durationMs", Math.Round(entry.Duration.TotalMilliseconds, 1));

			if (entry.Data.Count > 0)
			{
				writer.WriteStartObject("data");
				foreach (var (key, value) in entry.Data)
				{
					writer.WritePropertyName(key);
					if (SensitiveDataKeys.Contains(key))
						writer.WriteStringValue(Redacted);
					else
						JsonSerializer.Serialize(writer, value);
				}

				writer.WriteEndObject();
			}

			writer.WriteEndObject();
		}

		writer.WriteEndObject();
		writer.WriteEndObject();
	}
}
