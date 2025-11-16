using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.Domain;
using ExcelBotCs.Services.API;
using ExcelBotCs.Services.API.Interfaces;
using Microsoft.Extensions.Options;
using NetStone;
using NetStone.Model.Parseables.FreeCompany.Members;

namespace ExcelBotCs.Services;

public class LodestoneService
{
    private LodestoneClient _lodestoneClient;
    private readonly IOptions<LodestoneOptions> _options;
    private readonly IFcMemberService _fcMemberService;
    private readonly IFightService _fightService;
    private readonly ILogger<LodestoneService> _logger;
    private readonly HttpClient _httpClient;

    // Cache for Lodestone duties to minimize HTTP requests
    private static Dictionary<string, List<LodestoneDuty>>? _dutyCache;
    private static DateTime? _cacheRefreshTime;
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromDays(1);

    public LodestoneService(IOptions<LodestoneOptions> options, IFcMemberService fcMemberService,
        IFightService fightService, ILogger<LodestoneService> logger, HttpClient httpClient)
    {
        _options = options;
        _fcMemberService = fcMemberService;
        _fightService = fightService;
        _logger = logger;
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "ExcelBotCs/1.0");
        Task.Run(InitializeClientAsync);
    }

    private async Task InitializeClientAsync()
    {
        try
        {
            _lodestoneClient = await LodestoneClient.GetClientAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<string> GetCharacterBioById(string characterId)
    {
        if (_lodestoneClient == null) throw new InvalidOperationException("Lodestone client not initialized");
        var character = await _lodestoneClient.GetCharacter(characterId);
        return character?.Bio ?? string.Empty;
    }

    public async Task<List<FreeCompanyMembersEntry>> ImportMembers()
    {
        if (_lodestoneClient == null)
            return new List<FreeCompanyMembersEntry>();
        
        var fc = await _lodestoneClient.GetFreeCompany(_options.Value.FCId);
        var members = await fc.GetMembers();
        
        var fcMembers = new List<FreeCompanyMembersEntry>();

        while (members != null && members.CurrentPage <= members.NumPages)
        {
            Console.WriteLine("Adding members");
            fcMembers.AddRange(members.Members);
            members = await members.GetNextPage();
        }
        
        // Import all fc members into the database or update existing entries
        foreach (var freeCompanyMembersEntry in fcMembers)
        {
            var dbFcMember = await _fcMemberService.GetByCharacterId(freeCompanyMembersEntry.Id);

            if (dbFcMember == null)
            {
                await _fcMemberService.CreateAsync(await CreateFcMember(freeCompanyMembersEntry));
            }
            else
            {
                var fcMember = await CreateFcMember(freeCompanyMembersEntry);
                fcMember.Id =  dbFcMember.Id;
                
                await _fcMemberService.UpdateAsync(dbFcMember.Id, fcMember);
            }
        }
        
        return fcMembers;
    }

    private async Task<FcMember> CreateFcMember(FreeCompanyMembersEntry freeCompanyMembersEntry)
    {
        var lodestoneCharacter = await _lodestoneClient.GetCharacter(freeCompanyMembersEntry.Id);

        return new FcMember()
        {
            CharacterId = freeCompanyMembersEntry.Id,
            Avatar = freeCompanyMembersEntry.Avatar?.ToString() ?? string.Empty,
            LastSynchronisation = DateTime.UtcNow,
            FcRank = freeCompanyMembersEntry.FreeCompanyRank,
            Name = freeCompanyMembersEntry.Name,
            Title = lodestoneCharacter?.Title ??  string.Empty,
            Bio = lodestoneCharacter.Bio
        };
    }

    public async Task SyncFightImagesAsync()
    {
        try
        {
            _logger.LogInformation("Starting fight image synchronization from Lodestone");

            // Get all fights that need images
            var allFights = await _fightService.GetAsync();
            var fightsNeedingImages = allFights.Where(f =>
                string.IsNullOrEmpty(f.ImageUrl) &&
                (f.Type == FightType.Extreme || f.Type == FightType.Savage ||
                 f.Type == FightType.Ultimate || f.Type == FightType.Chaotic)
            ).ToList();

            if (!fightsNeedingImages.Any())
            {
                _logger.LogInformation("No fights require image synchronization");
                return;
            }

            _logger.LogInformation("Found {Count} fights needing images", fightsNeedingImages.Count);

            // Build or refresh duty cache
            var dutyLookup = await BuildDutyLookupAsync();

            int updated = 0;
            int failed = 0;

            // Process each fight
            foreach (var fight in fightsNeedingImages)
            {
                try
                {
                    _logger.LogDebug("Processing fight: {FightName} ({FightType})", fight.Name, fight.Type);

                    // Find best matching Lodestone duty (boss names already populated during cache build)
                    var matchedDuty = FindBestMatch(fight, dutyLookup);

                    if (matchedDuty == null)
                    {
                        _logger.LogWarning("No Lodestone match found for fight: {FightName}", fight.Name);
                        failed++;
                        continue;
                    }

                    _logger.LogDebug("Matched fight '{FightName}' to Lodestone duty '{DutyName}' (ID: {DutyId})",
                        fight.Name, matchedDuty.Name, matchedDuty.LodestoneId);

                    // Extract image URL from individual duty page
                    var imageUrl = await ExtractImageFromDutyPageAsync(matchedDuty.Url, matchedDuty);

                    if (string.IsNullOrEmpty(imageUrl))
                    {
                        _logger.LogWarning("Failed to extract image for fight: {FightName}", fight.Name);
                        failed++;
                        continue;
                    }

                    // Update fight with image URL
                    fight.ImageUrl = imageUrl;
                    await _fightService.UpdateAsync(fight.Id, fight);

                    _logger.LogInformation("Updated image for fight '{FightName}': {ImageUrl}", fight.Name, imageUrl);
                    updated++;

                    // Rate limiting
                    await Task.Delay(_options.Value.RequestDelayMs);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing fight: {FightName}", fight.Name);
                    failed++;
                }
            }

            _logger.LogInformation("Fight image synchronization complete. Updated: {Updated}, Failed: {Failed}",
                updated, failed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during fight image synchronization");
        }
    }

    private async Task<List<LodestoneDuty>> BuildDutyLookupAsync()
    {
        // Check if cache is valid
        if (_dutyCache != null && _cacheRefreshTime.HasValue &&
            DateTime.UtcNow - _cacheRefreshTime.Value < CacheLifetime)
        {
            _logger.LogDebug("Using cached Lodestone duty data");
            return _dutyCache.Values.SelectMany(x => x).ToList();
        }

        _logger.LogInformation("Building Lodestone duty lookup cache");
        _dutyCache = new Dictionary<string, List<LodestoneDuty>>();

        // Category mappings
        var categories = new Dictionary<FightType, int>
        {
            { FightType.Extreme, 4 },
            { FightType.Savage, 5 },
            { FightType.Ultimate, 28 }
        };

        // Expansion IDs (0 = ARR, 1 = HW, 2 = SB, 3 = ShB, 4 = EW, 5 = DT)
        var expansionIds = Enumerable.Range(0, 6).ToList();

        foreach (var category in categories)
        {
            foreach (var expansionId in expansionIds)
            {
                try
                {
                    var duties = await ScrapeListingPageAsync(expansionId, category.Value);

                    _logger.LogDebug("Scraped {Count} duties for {Type} expansion {ExpansionId}",
                        duties.Count, category.Key, expansionId);

                    // Populate boss names for each duty
                    foreach (var duty in duties)
                    {
                        try
                        {
                            await PopulateBossNamesAsync(duty);
                            await Task.Delay(_options.Value.RequestDelayMs); // Rate limiting
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to populate boss names for duty: {DutyName}", duty.Name);
                        }
                    }

                    var cacheKey = $"{category.Key}_{expansionId}";
                    _dutyCache[cacheKey] = duties;

                    _logger.LogDebug("Populated boss names for {Count} duties in {Type} expansion {ExpansionId}",
                        duties.Count, category.Key, expansionId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error scraping {Type} expansion {ExpansionId}",
                        category.Key, expansionId);
                }
            }
        }

        _cacheRefreshTime = DateTime.UtcNow;
        _logger.LogInformation("Lodestone duty cache built with {Count} total duties",
            _dutyCache.Values.SelectMany(x => x).Count());

        return _dutyCache.Values.SelectMany(x => x).ToList();
    }

    private async Task<List<LodestoneDuty>> ScrapeListingPageAsync(int expansionId, int categoryId)
    {
        var url = $"{_options.Value.BaseUrl}/lodestone/playguide/db/duty/?category2={categoryId}&ex_version={expansionId}";
        var duties = new List<LodestoneDuty>();

        try
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var html = await response.Content.ReadAsStringAsync();
            duties = ParseDutyListingHtml(html);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping listing page: {Url}", url);
        }

        return duties;
    }

    private List<LodestoneDuty> ParseDutyListingHtml(string html)
    {
        var duties = new List<LodestoneDuty>();

        try
        {
            var config = Configuration.Default;
            var context = BrowsingContext.New(config);
            var document = context.OpenAsync(req => req.Content(html)).Result;

            // Find all duty links - they're in anchors with specific class or pattern
            var dutyLinks = document.QuerySelectorAll("a[href*='/lodestone/playguide/db/duty/']");

            foreach (var link in dutyLinks)
            {
                var href = link.GetAttribute("href");
                if (string.IsNullOrEmpty(href)) continue;

                // Extract duty ID from URL
                var match = Regex.Match(href, @"/duty/(\w+)/");
                if (!match.Success) continue;

                var dutyId = match.Groups[1].Value;
                var name = link.TextContent?.Trim() ?? string.Empty;

                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(dutyId))
                {
                    duties.Add(new LodestoneDuty
                    {
                        Name = name,
                        LodestoneId = dutyId
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing duty listing HTML");
        }

        return duties;
    }

    private async Task<string?> ExtractImageFromDutyPageAsync(string lodestoneUrl, LodestoneDuty? duty = null)
    {
        try
        {
            var response = await _httpClient.GetAsync(lodestoneUrl);
            response.EnsureSuccessStatusCode();

            var html = await response.Content.ReadAsStringAsync();

            var config = Configuration.Default;
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync(req => req.Content(html));

            // Extract boss names while we're here (if duty object provided)
            if (duty != null)
            {
                duty.BossNames = ExtractBossNamesFromDocument(document);
                if (duty.BossNames.Any())
                {
                    _logger.LogDebug("Extracted boss names for '{DutyName}': {BossNames}",
                        duty.Name, string.Join(", ", duty.BossNames));
                }
            }

            // Look for the main duty image - typically in a specific div or img tag
            // Common selectors for Lodestone duty images
            var imageSelectors = new[]
            {
                "div.db__l_main__content img",
                "div.db-view__detail__visual img",
                "img.db-view__item__icon__item_image",
                "div.sys_duty_image img"
            };

            foreach (var selector in imageSelectors)
            {
                var imgElement = document.QuerySelector(selector);
                if (imgElement != null)
                {
                    var src = imgElement.GetAttribute("src");
                    if (!string.IsNullOrEmpty(src))
                    {
                        // Make absolute URL if relative
                        if (src.StartsWith("//"))
                            return "https:" + src;
                        if (src.StartsWith("/"))
                            return _options.Value.BaseUrl + src;
                        return src;
                    }
                }
            }

            _logger.LogWarning("No image found on duty page: {Url}", lodestoneUrl);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting image from duty page: {Url}", lodestoneUrl);
            return null;
        }
    }

    private List<string> ExtractBossNamesFromDocument(IDocument document)
    {
        var bossNames = new List<string>();

        try
        {
            // Boss names are in links to /lodestone/playguide/db/npc/enemy/
            var bossLinks = document.QuerySelectorAll("a[href*='/lodestone/playguide/db/npc/enemy/']");

            foreach (var link in bossLinks)
            {
                var bossName = link.TextContent?.Trim();
                if (!string.IsNullOrEmpty(bossName))
                {
                    bossNames.Add(bossName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting boss names from document");
        }

        return bossNames;
    }

    private async Task PopulateBossNamesAsync(LodestoneDuty duty)
    {
        // Skip if already populated
        if (duty.BossNames.Any())
            return;

        try
        {
            var response = await _httpClient.GetAsync(duty.Url);
            response.EnsureSuccessStatusCode();

            var html = await response.Content.ReadAsStringAsync();

            var config = Configuration.Default;
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync(req => req.Content(html));

            duty.BossNames = ExtractBossNamesFromDocument(document);

            if (duty.BossNames.Any())
            {
                _logger.LogDebug("Populated boss names for '{DutyName}': {BossNames}",
                    duty.Name, string.Join(", ", duty.BossNames));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error populating boss names for duty: {DutyUrl}", duty.Url);
            throw;
        }
    }

    private LodestoneDuty? FindBestMatch(Fight fight, List<LodestoneDuty> duties)
    {
        var normalizedFightName = NormalizeName(fight.Name);

        // Try exact match first
        var exactMatch = duties.FirstOrDefault(d =>
            NormalizeName(d.Name).Equals(normalizedFightName, StringComparison.OrdinalIgnoreCase));

        if (exactMatch != null)
            return exactMatch;

        // Try contains match
        var containsMatch = duties.FirstOrDefault(d =>
            NormalizeName(d.Name).Contains(normalizedFightName, StringComparison.OrdinalIgnoreCase) ||
            normalizedFightName.Contains(NormalizeName(d.Name), StringComparison.OrdinalIgnoreCase));

        if (containsMatch != null)
            return containsMatch;

        // Try matching by boss names (if available)
        var bossMatch = duties.FirstOrDefault(d =>
            d.BossNames.Any(boss =>
                NormalizeName(boss).Equals(normalizedFightName, StringComparison.OrdinalIgnoreCase) ||
                NormalizeName(boss).Contains(normalizedFightName, StringComparison.OrdinalIgnoreCase) ||
                normalizedFightName.Contains(NormalizeName(boss), StringComparison.OrdinalIgnoreCase)));

        if (bossMatch != null)
        {
            _logger.LogDebug("Matched '{FightName}' to '{DutyName}' by boss name",
                fight.Name, bossMatch.Name);
            return bossMatch;
        }

        // Try matching by significant words (for boss names)
        // Extract words that are likely boss/encounter names (3+ characters, not common words)
        var fightWords = ExtractSignificantWords(normalizedFightName);

        if (fightWords.Any())
        {
            var wordMatch = duties.FirstOrDefault(d =>
            {
                var dutyWords = ExtractSignificantWords(NormalizeName(d.Name));
                // Match if duties share significant words (like boss names)
                return fightWords.Intersect(dutyWords, StringComparer.OrdinalIgnoreCase).Any();
            });

            if (wordMatch != null)
            {
                _logger.LogDebug("Matched '{FightName}' to '{DutyName}' by word matching",
                    fight.Name, wordMatch.Name);
                return wordMatch;
            }
        }

        return null;
    }

    private List<string> ExtractSignificantWords(string text)
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

    private string NormalizeName(string name)
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