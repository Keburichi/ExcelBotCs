using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Services.Lodestone;

/// <summary>
///     Service responsible for matching Fight entities to LodestoneDuty entities.
///     Contains pure matching logic with no I/O operations.
/// </summary>
public class DutyMatchingService
{
    private readonly ILogger<DutyMatchingService> _logger;

    public DutyMatchingService(ILogger<DutyMatchingService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    ///     Finds the best matching LodestoneDuty for a given Fight.
    ///     Uses a multi-strategy approach with expansion-aware filtering.
    /// </summary>
    public LodestoneDuty? FindBestMatch(Fight fight, List<LodestoneDuty> duties)
    {
        var normalizedFightName = NormalizeName(fight.Name);

        // Filter by expansion if available (do this FIRST to prevent cross-expansion matches)
        var expansionFiltered = fight.FFLogsExpansionId.HasValue
            ? duties.Where(d => d.ExpansionId == fight.FFLogsExpansionId.Value).ToList()
            : duties;

        // 1. Try exact match within expansion (highest priority)
        var exactMatch = expansionFiltered.FirstOrDefault(d =>
            NormalizeName(d.Name).Equals(normalizedFightName, StringComparison.OrdinalIgnoreCase));

        if (exactMatch != null)
        {
            _logger.LogDebug("Found exact match: '{FightName}' -> '{DutyName}'", fight.Name, exactMatch.Name);
            return exactMatch;
        }

        // 2. Fallback: exact match across all expansions (in case expansion mapping is wrong)
        var exactMatchGlobal = duties.FirstOrDefault(d =>
            NormalizeName(d.Name).Equals(normalizedFightName, StringComparison.OrdinalIgnoreCase));

        if (exactMatchGlobal != null)
        {
            _logger.LogWarning(
                "Found exact match outside expected expansion: '{FightName}' -> '{DutyName}' (Exp: {ExpId})",
                fight.Name, exactMatchGlobal.Name, exactMatchGlobal.ExpansionId);
            return exactMatchGlobal;
        }

        // 3. Boss name exact match within expansion
        var bossExactMatch = expansionFiltered.FirstOrDefault(d =>
            d.BossNames.Any(boss =>
                NormalizeName(boss).Equals(normalizedFightName, StringComparison.OrdinalIgnoreCase)));

        if (bossExactMatch != null)
        {
            _logger.LogDebug("Matched by boss name (exact): '{FightName}' -> '{DutyName}'", fight.Name,
                bossExactMatch.Name);
            return bossExactMatch;
        }

        // 4. Contains match within expansion (with minimum 4-char length check to prevent "Titan" matching "Titania")
        var containsMatch = expansionFiltered.FirstOrDefault(d =>
        {
            var normalizedDutyName = NormalizeName(d.Name);
            // Only match if both strings are at least 4 characters
            // This prevents short substrings like "Titan" from matching "Titania"
            if (normalizedDutyName.Length >= 4 && normalizedFightName.Length >= 4)
                return normalizedDutyName.Contains(normalizedFightName, StringComparison.OrdinalIgnoreCase) ||
                       normalizedFightName.Contains(normalizedDutyName, StringComparison.OrdinalIgnoreCase);

            return false;
        });

        if (containsMatch != null)
        {
            _logger.LogDebug("Matched by contains: '{FightName}' -> '{DutyName}'", fight.Name, containsMatch.Name);
            return containsMatch;
        }

        // 5. Boss name partial match within expansion
        var bossPartialMatch = expansionFiltered.FirstOrDefault(d =>
            d.BossNames.Any(boss =>
            {
                var normalizedBoss = NormalizeName(boss);
                // Apply minimum length check here too
                if (normalizedBoss.Length >= 4 && normalizedFightName.Length >= 4)
                    return normalizedBoss.Contains(normalizedFightName, StringComparison.OrdinalIgnoreCase) ||
                           normalizedFightName.Contains(normalizedBoss, StringComparison.OrdinalIgnoreCase);

                return false;
            }));

        if (bossPartialMatch != null)
        {
            _logger.LogDebug("Matched by boss name (partial): '{FightName}' -> '{DutyName}'",
                fight.Name, bossPartialMatch.Name);
            return bossPartialMatch;
        }

        // 6. Word-based matching within expansion
        var fightWords = ExtractSignificantWords(normalizedFightName);

        if (fightWords.Any())
        {
            var wordMatch = expansionFiltered.FirstOrDefault(d =>
            {
                var dutyWords = ExtractSignificantWords(NormalizeName(d.Name));
                return fightWords.Intersect(dutyWords, StringComparer.OrdinalIgnoreCase).Any();
            });

            if (wordMatch != null)
            {
                _logger.LogDebug("Matched by word matching: '{FightName}' -> '{DutyName}'",
                    fight.Name, wordMatch.Name);
                return wordMatch;
            }
        }

        _logger.LogWarning("No match found for fight: {FightName} (Expansion: {ExpId})",
            fight.Name, fight.FFLogsExpansionId);
        return null;
    }

    /// <summary>
    ///     Extracts significant words from text, filtering out stopwords and short words.
    ///     Used for word-based matching strategy.
    /// </summary>
    public List<string> ExtractSignificantWords(string text)
    {
        if (string.IsNullOrEmpty(text))
            return new List<string>();

        // Common words to ignore
        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "the", "of", "and", "or", "in", "on", "at", "to", "a", "an",
            "extreme", "savage", "ultimate", "chaotic", "normal",
            "circle", "floor", "gate", "turn", "tier", "raid", "trial"
        };

        return text.Split(new[] { ' ', ':', '-', '(', ')' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(word => word.Length >= 3 && !stopWords.Contains(word))
            .ToList();
    }

    /// <summary>
    ///     Normalizes a duty/fight name by removing difficulty suffixes and converting to lowercase.
    /// </summary>
    public string NormalizeName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return string.Empty;

        // Remove difficulty suffixes
        var normalized = name
            .Replace(" (Extreme)", "", StringComparison.OrdinalIgnoreCase)
            .Replace(" (Savage)", "", StringComparison.OrdinalIgnoreCase)
            .Replace(" (Ultimate)", "", StringComparison.OrdinalIgnoreCase)
            .Replace(" (Chaotic)", "", StringComparison.OrdinalIgnoreCase)
            .Trim()
            .ToLowerInvariant();

        return normalized;
    }
}