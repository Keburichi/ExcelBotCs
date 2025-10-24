using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ExcelBotCs.Models.Config;
using Microsoft.Extensions.Options;

namespace ExcelBotCs.Services.FFLogs;

public class FFLogsGraphQLService
{
    private readonly FFLogsOptions _options;
    private readonly FFLogsAuthService _authService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<FFLogsGraphQLService> _logger;

    public FFLogsGraphQLService(
        IOptions<FFLogsOptions> options,
        FFLogsAuthService authService,
        IHttpClientFactory httpClientFactory,
        ILogger<FFLogsGraphQLService> logger)
    {
        _options = options.Value;
        _authService = authService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Executes a GraphQL query with automatic retry on 401 and rate limit handling
    /// </summary>
    public async Task<T> ExecuteQueryAsync<T>(string query, object? variables = null, int maxRetries = 3)
    {
        var attempt = 0;
        Exception? lastException = null;

        while (attempt < maxRetries)
        {
            attempt++;
            try
            {
                var accessToken = await _authService.GetAccessTokenAsync();
                var httpClient = _httpClientFactory.CreateClient();

                var request = new HttpRequestMessage(HttpMethod.Post, _options.ApiUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var requestBody = new
                {
                    query = query,
                    variables = variables
                };

                request.Content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json");

                var response = await httpClient.SendAsync(request);

                // Handle rate limiting (429 Too Many Requests)
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    var retryAfter = GetRetryAfterSeconds(response);
                    _logger.LogWarning("FFLogs rate limit exceeded. Retrying after {RetryAfter} seconds (attempt {Attempt}/{MaxRetries})",
                        retryAfter, attempt, maxRetries);

                    if (attempt < maxRetries)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(retryAfter));
                        continue;
                    }

                    throw new HttpRequestException($"FFLogs rate limit exceeded after {maxRetries} retries");
                }

                // Handle unauthorized (token expired)
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("FFLogs returned 401 Unauthorized. Refreshing token (attempt {Attempt}/{MaxRetries})",
                        attempt, maxRetries);

                    if (attempt < maxRetries)
                    {
                        _authService.InvalidateToken();
                        await _authService.RefreshTokenAsync();
                        continue;
                    }

                    throw new HttpRequestException($"FFLogs authentication failed after {maxRetries} retries");
                }

                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var graphQLResponse = JsonSerializer.Deserialize<GraphQLResponse<T>>(responseContent);

                if (graphQLResponse == null)
                {
                    throw new InvalidOperationException("Failed to deserialize GraphQL response");
                }

                // Check for GraphQL errors
                if (graphQLResponse.errors != null && graphQLResponse.errors.Count > 0)
                {
                    var errorMessages = string.Join(", ", graphQLResponse.errors.Select(e => e.message));
                    throw new InvalidOperationException($"GraphQL errors: {errorMessages}");
                }

                if (graphQLResponse.data == null)
                {
                    throw new InvalidOperationException("GraphQL response contained no data");
                }

                return graphQLResponse.data;
            }
            catch (Exception ex) when (attempt < maxRetries && IsRetryableException(ex))
            {
                lastException = ex;
                var delay = CalculateExponentialBackoff(attempt);
                _logger.LogWarning(ex, "FFLogs query failed (attempt {Attempt}/{MaxRetries}). Retrying after {Delay}ms",
                    attempt, maxRetries, delay);
                await Task.Delay(delay);
            }
        }

        throw new InvalidOperationException(
            $"FFLogs query failed after {maxRetries} attempts",
            lastException);
    }

    /// <summary>
    /// Fetches all FFXIV encounters from FFLogs
    /// </summary>
    public async Task<WorldData> GetWorldDataAsync()
    {
        const string query = @"
query GetAllFFXIVFights {
  worldData {
    expansions {
      id
      name
      zones {
        id
        name
        frozen
        encounters {
          id
          name
        }
        difficulties {
          id
          name
        }
      }
    }
  }
}";

        _logger.LogInformation("Fetching world data from FFLogs");
        return await ExecuteQueryAsync<WorldData>(query);
    }

    /// <summary>
    /// Fetches character clears for a specific Lodestone ID and zone
    /// </summary>
    public async Task<CharacterData> GetCharacterActivityAsync(long lodestoneId, int? zoneId = null, int? difficulty = null)
    {
        // Build dynamic query based on parameters
        var queryBuilder = new StringBuilder(@"
query GetCharacterClears($lodestoneID: Int!");

        if (zoneId.HasValue)
        {
            queryBuilder.Append(", $zoneID: Int!");
        }

        if (difficulty.HasValue)
        {
            queryBuilder.Append(", $difficulty: Int!");
        }

        queryBuilder.AppendLine(@") {
  characterData {
    character(lodestoneID: $lodestoneID) {
      id
      name");

        if (zoneId.HasValue)
        {
            queryBuilder.Append(@"
      zoneRankings(zoneID: $zoneID");

            if (difficulty.HasValue)
            {
                queryBuilder.Append(", difficulty: $difficulty");
            }

            queryBuilder.AppendLine(@", metric: rdps) {
        encounterRanks {
          encounter {
            id
            name
          }
          ranks {
            totalKills
            startTime
            reportCode
            fightID
          }
        }
      }");
        }

        queryBuilder.AppendLine(@"
    }
  }
}");

        var variables = new Dictionary<string, object>
        {
            { "lodestoneID", lodestoneId }
        };

        if (zoneId.HasValue)
        {
            variables["zoneID"] = zoneId.Value;
        }

        if (difficulty.HasValue)
        {
            variables["difficulty"] = difficulty.Value;
        }

        _logger.LogDebug("Fetching character activity for Lodestone ID {LodestoneId}, Zone {ZoneId}, Difficulty {Difficulty}",
            lodestoneId, zoneId, difficulty);

        return await ExecuteQueryAsync<CharacterData>(queryBuilder.ToString(), variables);
    }

    /// <summary>
    /// Checks rate limit status
    /// </summary>
    public async Task<RateLimitData> GetRateLimitDataAsync()
    {
        const string query = @"
query {
  rateLimitData {
    limitPerHour
    pointsSpentThisHour
    pointsResetIn
  }
}";

        return await ExecuteQueryAsync<RateLimitData>(query);
    }

    private static int GetRetryAfterSeconds(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("Retry-After", out var values))
        {
            var retryAfterValue = values.FirstOrDefault();
            if (int.TryParse(retryAfterValue, out var seconds))
            {
                return seconds;
            }
        }

        // Default to exponential backoff starting at 60 seconds
        return 60;
    }

    private static bool IsRetryableException(Exception ex)
    {
        return ex is HttpRequestException or TaskCanceledException or TimeoutException;
    }

    private static int CalculateExponentialBackoff(int attempt)
    {
        // Exponential backoff: 1s, 2s, 4s, 8s...
        return (int)Math.Pow(2, attempt - 1) * 1000;
    }

    #region Response Models

    private class GraphQLResponse<T>
    {
        public T? data { get; set; }
        public List<GraphQLError>? errors { get; set; }
    }

    private class GraphQLError
    {
        public string message { get; set; } = string.Empty;
    }

    #endregion
}

#region FFLogs Data Models

public class WorldData
{
    public WorldDataContainer worldData { get; set; } = new();
}

public class WorldDataContainer
{
    public List<Expansion> expansions { get; set; } = new();
}

public class Expansion
{
    public int id { get; set; }
    public string name { get; set; } = string.Empty;
    public List<Zone> zones { get; set; } = new();
    
    public override string ToString()
    {
        return $"{name} - {id}";
    }
}

public class Zone
{
    public int id { get; set; }
    public string name { get; set; } = string.Empty;
    public bool frozen { get; set; }
    public List<Encounter> encounters { get; set; } = new();
    public List<Difficulty> difficulties { get; set; } = new();
    
    public override string ToString()
    {
        return $"{name} - {id}";
    }
}

public class Encounter
{
    public int id { get; set; }
    public string name { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"{name} - {id}";
    }
}

public class Difficulty
{
    public int id { get; set; }
    public string name { get; set; } = string.Empty;
    
    public override string ToString()
    {
        return $"{name} - {id}";
    }
}

public class CharacterData
{
    public CharacterDataContainer characterData { get; set; } = new();
}

public class CharacterDataContainer
{
    public Character? character { get; set; }
}

public class Character
{
    public int id { get; set; }
    public string name { get; set; } = string.Empty;
    public ZoneRankings? zoneRankings { get; set; }
    
    public override string ToString()
    {
        return $"{name} - {id}";
    }
}

public class ZoneRankings
{
    public List<EncounterRank> encounterRanks { get; set; } = new();
}

public class EncounterRank
{
    public Encounter encounter { get; set; } = new();
    public List<Rank> ranks { get; set; } = new();
}

public class Rank
{
    public int totalKills { get; set; }
    public long startTime { get; set; }
    public string reportCode { get; set; } = string.Empty;
    public int fightID { get; set; }
}

public class RateLimitData
{
    public RateLimitDataContainer rateLimitData { get; set; } = new();
}

public class RateLimitDataContainer
{
    public int limitPerHour { get; set; }
    public int pointsSpentThisHour { get; set; }
    public int pointsResetIn { get; set; }
}

#endregion
