using System.Text.RegularExpressions;

namespace ExcelBotCs.Utilities;

public static partial class FightNormalization
{
    private static readonly Regex SuffixPattern = SuffixRegex();

    public static string GetCanonicalBossName(string encounterName)
    {
        var stripped = SuffixPattern.Replace(encounterName, "").Trim();
        return string.IsNullOrWhiteSpace(stripped) ? encounterName.Trim() : stripped;
    }

    public static string GetNormalizationKey(string bossName)
    {
        return GetCanonicalBossName(bossName).ToLowerInvariant();
    }

    [GeneratedRegex(@"\s*\((Extreme|Savage|Ultimate|Chaotic|Unreal|Normal|Hard)\)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex SuffixRegex();
}
