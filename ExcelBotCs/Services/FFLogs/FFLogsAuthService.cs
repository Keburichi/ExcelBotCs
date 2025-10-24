using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ExcelBotCs.Models.Config;
using Microsoft.Extensions.Options;

namespace ExcelBotCs.Services.FFLogs;

public class FFLogsAuthService
{
    private readonly FFLogsOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<FFLogsAuthService> _logger;

    private string? _cachedAccessToken;
    private DateTime _tokenExpiryTime = DateTime.MinValue;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    public FFLogsAuthService(
        IOptions<FFLogsOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<FFLogsAuthService> logger)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Gets a valid access token, refreshing if necessary
    /// </summary>
    public async Task<string> GetAccessTokenAsync()
    {
        await _tokenLock.WaitAsync();
        try
        {
            // Return cached token if still valid
            if (!string.IsNullOrEmpty(_cachedAccessToken) && DateTime.UtcNow < _tokenExpiryTime)
            {
                return _cachedAccessToken;
            }

            // Refresh token
            _logger.LogInformation("Refreshing FFLogs access token");
            await RefreshTokenAsync();

            if (string.IsNullOrEmpty(_cachedAccessToken))
            {
                throw new InvalidOperationException("Failed to obtain FFLogs access token");
            }

            return _cachedAccessToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    /// <summary>
    /// Forces token refresh (called on 401 errors)
    /// </summary>
    public async Task RefreshTokenAsync()
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();

            // Prepare OAuth client credentials request
            var authValue = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));

            var request = new HttpRequestMessage(HttpMethod.Post, _options.TokenUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authValue);
            request.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            });

            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<FFLogsTokenResponse>(responseContent);

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.access_token))
            {
                throw new InvalidOperationException("Invalid token response from FFLogs");
            }

            _cachedAccessToken = tokenResponse.access_token;

            // Set expiry time with 5-minute buffer (default to 1 hour if not specified)
            var expiresIn = tokenResponse.expires_in > 0 ? tokenResponse.expires_in : 3600;
            _tokenExpiryTime = DateTime.UtcNow.AddSeconds(expiresIn - 300);

            _logger.LogInformation("FFLogs access token refreshed successfully. Expires at {ExpiryTime}", _tokenExpiryTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh FFLogs access token");
            _cachedAccessToken = null;
            _tokenExpiryTime = DateTime.MinValue;
            throw;
        }
    }

    /// <summary>
    /// Invalidates the cached token (useful for testing or error recovery)
    /// </summary>
    public void InvalidateToken()
    {
        _cachedAccessToken = null;
        _tokenExpiryTime = DateTime.MinValue;
        _logger.LogInformation("FFLogs access token invalidated");
    }

    private class FFLogsTokenResponse
    {
        public string access_token { get; set; } = string.Empty;
        public string token_type { get; set; } = string.Empty;
        public int expires_in { get; set; }
    }
}
