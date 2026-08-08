using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using ExcelBotCs.Models.Config;
using Microsoft.Extensions.Options;

namespace ExcelBotCs.Services.Minecraft;

// Minimal Source RCON client (https://developer.valvesoftware.com/wiki/Source_RCON_Protocol),
// which Minecraft's built-in RCON server implements. Only what whitelist add/remove needs.
public partial class MinecraftRconService : IMinecraftRconService
{
    private const int TypeAuth = 3;
    private const int TypeExecCommand = 2;
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(10);

    // Real Mojang usernames are 3-16 characters, letters/digits/underscore only. Enforcing this
    // before building the RCON command body prevents a username containing control characters
    // (e.g. embedded newlines) from being interpreted as extra console commands by the server.
    [GeneratedRegex("^[A-Za-z0-9_]{3,16}$")]
    private static partial Regex ValidUsernameRegex();

    private readonly MinecraftOptions _options;
    private readonly ILogger<MinecraftRconService> _logger;

    public MinecraftRconService(IOptions<MinecraftOptions> options, ILogger<MinecraftRconService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<(bool Success, string Message)> WhitelistAddAsync(string username) =>
        RunWhitelistCommandAsync("add", username);

    public Task<(bool Success, string Message)> WhitelistRemoveAsync(string username) =>
        RunWhitelistCommandAsync("remove", username);

    private Task<(bool Success, string Message)> RunWhitelistCommandAsync(string action, string username)
    {
        if (!ValidUsernameRegex().IsMatch(username))
            return Task.FromResult((false, "Not a valid Minecraft username."));

        return RunCommandAsync($"whitelist {action} {username}");
    }

    private async Task<(bool Success, string Message)> RunCommandAsync(string command)
    {
        try
        {
            using var client = new TcpClient();
            using var cts = new CancellationTokenSource(CommandTimeout);
            await client.ConnectAsync(_options.RconHost, _options.RconPort, cts.Token);
            await using var stream = client.GetStream();

            if (!await AuthenticateAsync(stream, cts.Token))
            {
                _logger.LogError("RCON authentication to {Host}:{Port} failed", _options.RconHost,
                    _options.RconPort);
                return (false, "Could not authenticate with the Minecraft server.");
            }

            var response = await SendCommandAsync(stream, command, cts.Token);
            return (IsSuccessResponse(response), response.Trim());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RCON command '{Command}' failed", command);
            return (false, "Could not reach the Minecraft server.");
        }
    }

    // Fail closed: only recognized vanilla whitelist success phrasing counts as success.
    // Anything else - "That player does not exist", "Unknown command", a truncated/garbled
    // response, server-language differences - is treated as failure so a bad or unexpected
    // response can never get silently persisted as a successful whitelist change.
    //   "Added <name> to the whitelist" / "Removed <name> from the whitelist"
    //   "Nothing changed. The player already is whitelisted" / "...is not whitelisted" (idempotent, still success)
    private static bool IsSuccessResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return false;
        return response.Contains("Added", StringComparison.OrdinalIgnoreCase) ||
               response.Contains("Removed", StringComparison.OrdinalIgnoreCase) ||
               response.Contains("Nothing changed", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<bool> AuthenticateAsync(NetworkStream stream, CancellationToken ct)
    {
        const int authRequestId = 1;
        await WritePacketAsync(stream, authRequestId, TypeAuth, _options.RconPassword, ct);
        var (requestId, _) = await ReadPacketAsync(stream, ct);
        return requestId == authRequestId;
    }

    private static async Task<string> SendCommandAsync(NetworkStream stream, string command, CancellationToken ct)
    {
        await WritePacketAsync(stream, 2, TypeExecCommand, command, ct);
        var (_, body) = await ReadPacketAsync(stream, ct);
        return body;
    }

    private static async Task WritePacketAsync(NetworkStream stream, int requestId, int type, string body,
        CancellationToken ct)
    {
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        // Size field covers everything after itself: id(4) + type(4) + body + null terminator + empty-string terminator
        var payloadSize = 4 + 4 + bodyBytes.Length + 2;
        var packet = new byte[4 + payloadSize];

        var offset = 0;
        BitConverter.GetBytes(payloadSize).CopyTo(packet, offset);
        offset += 4;
        BitConverter.GetBytes(requestId).CopyTo(packet, offset);
        offset += 4;
        BitConverter.GetBytes(type).CopyTo(packet, offset);
        offset += 4;
        bodyBytes.CopyTo(packet, offset);
        // remaining two bytes stay zero-initialized (body null terminator + packet terminator)

        await stream.WriteAsync(packet, ct);
    }

    private static async Task<(int RequestId, string Body)> ReadPacketAsync(NetworkStream stream, CancellationToken ct)
    {
        var lengthBuffer = await ReadExactAsync(stream, 4, ct);
        var length = BitConverter.ToInt32(lengthBuffer);
        var payload = await ReadExactAsync(stream, length, ct);

        var requestId = BitConverter.ToInt32(payload, 0);
        var bodyLength = length - 4 - 4 - 2;
        var body = bodyLength > 0 ? Encoding.UTF8.GetString(payload, 8, bodyLength) : string.Empty;
        return (requestId, body);
    }

    private static async Task<byte[]> ReadExactAsync(NetworkStream stream, int count, CancellationToken ct)
    {
        var buffer = new byte[count];
        var read = 0;
        while (read < count)
        {
            var n = await stream.ReadAsync(buffer.AsMemory(read, count - read), ct);
            if (n == 0)
                throw new IOException("RCON connection closed unexpectedly.");
            read += n;
        }

        return buffer;
    }
}
