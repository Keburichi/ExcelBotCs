using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database;
using Microsoft.Extensions.Options;

namespace ExcelBotCs.Services.Lodestone;

/// <summary>
///     Service responsible for scraping and parsing Lodestone duty pages.
///     Contains all HTML parsing logic with in-memory caching during operations.
/// </summary>
public class LodestoneDutyScraperService
{
    private readonly ILogger<LodestoneDutyScraperService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IOptions<LodestoneOptions> _options;

    // Cache parsed duty pages during current operation to avoid re-fetching
    private readonly Dictionary<string, DutyPageData> _dutyPageCache = new();

    private class DutyPageData
    {
        public List<string> BossNames { get; set; } = new();
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
    }

    public LodestoneDutyScraperService(
        ILogger<LodestoneDutyScraperService> logger,
        HttpClient httpClient,
        IOptions<LodestoneOptions> options)
    {
        _logger = logger;
        _httpClient = httpClient;
        _options = options;
    }

    /// <summary>
    ///     Clears the internal duty page cache.
    ///     Should be called at the start and end of scraping operations.
    /// </summary>
    public void ClearCache()
    {
        _dutyPageCache.Clear();
        _logger.LogDebug("Cleared duty page cache");
    }

    /// <summary>
    ///     Scrapes a Lodestone listing page for duties of a specific expansion and category.
    /// </summary>
    public async Task<List<LodestoneDuty>> ScrapeListingPageAsync(int expansionId, int categoryId,
        FightType fightType)
    {
        var url =
            $"{_options.Value.BaseUrl}/lodestone/playguide/db/duty/?category2={categoryId}&ex_version={expansionId}";
        var duties = new List<LodestoneDuty>();

        try
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var html = await response.Content.ReadAsStringAsync();
            duties = await ParseDutyListingHtmlAsync(html, expansionId, categoryId, fightType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping listing page: {Url}", url);
        }

        return duties;
    }

    /// <summary>
    ///     Parses HTML from a Lodestone listing page to extract duty information.
    /// </summary>
    public async Task<List<LodestoneDuty>> ParseDutyListingHtmlAsync(string html, int expansionId, int categoryId,
        FightType fightType)
    {
        var duties = new List<LodestoneDuty>();

        try
        {
            var config = Configuration.Default;
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync(req => req.Content(html));

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
                    duties.Add(new LodestoneDuty
                    {
                        Name = name,
                        LodestoneId = dutyId,
                        ExpansionId = expansionId,
                        CategoryId = categoryId,
                        FightType = fightType,
                        LastSyncTime = DateTime.UtcNow
                    });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing duty listing HTML");
        }

        return duties;
    }

    /// <summary>
    ///     Fetches and parses a single duty page to extract all metadata (boss names, description, image).
    ///     Uses in-memory caching to avoid redundant HTTP requests during a single operation.
    /// </summary>
    private async Task<DutyPageData> FetchAndParseDutyPageAsync(string lodestoneUrl)
    {
        // Check cache first
        if (_dutyPageCache.TryGetValue(lodestoneUrl, out var cachedData))
        {
            _logger.LogDebug("Using cached data for duty page: {Url}", lodestoneUrl);
            return cachedData;
        }

        try
        {
            // Fetch and parse
            var response = await _httpClient.GetAsync(lodestoneUrl);
            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync();

            var config = Configuration.Default;
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync(req => req.Content(html));

            var pageData = new DutyPageData
            {
                BossNames = ExtractBossNamesFromDocument(document),
                ImageUrl = ExtractImageUrlFromDocument(document),
                Description = ExtractDescriptionFromDocument(document)
            };

            // Cache for future use during this operation
            _dutyPageCache[lodestoneUrl] = pageData;

            _logger.LogDebug(
                "Fetched and cached duty page data: {Url} (Bosses: {BossCount}, HasImage: {HasImage}, HasDescription: {HasDescription})",
                lodestoneUrl, pageData.BossNames.Count, pageData.ImageUrl != null, pageData.Description != null);

            return pageData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching and parsing duty page: {Url}", lodestoneUrl);
            throw;
        }
    }

    /// <summary>
    ///     Populates boss names, description, and image URL for a duty by scraping its detail page.
    ///     Skips if boss names are already populated.
    /// </summary>
    public async Task PopulateDutyMetadataAsync(LodestoneDuty duty)
    {
        // Skip if already populated
        if (duty.BossNames.Any())
            return;

        var pageData = await FetchAndParseDutyPageAsync(duty.Url);
        duty.BossNames = pageData.BossNames;
        duty.Description = pageData.Description;
        duty.ImageUrl = pageData.ImageUrl;

        if (duty.BossNames.Any())
            _logger.LogDebug(
                "Populated metadata for '{DutyName}': {BossCount} bosses, HasImage: {HasImage}, HasDescription: {HasDescription}",
                duty.Name, duty.BossNames.Count, duty.ImageUrl != null, duty.Description != null);
    }

    /// <summary>
    ///     Extracts boss names from a parsed Lodestone duty page.
    /// </summary>
    public List<string> ExtractBossNamesFromDocument(IDocument document)
    {
        var bossNames = new List<string>();

        try
        {
            // Boss names are in links to /lodestone/playguide/db/npc/enemy/
            var bossLinks = document.QuerySelectorAll("a[href*='/lodestone/playguide/db/npc/enemy/']");

            foreach (var link in bossLinks)
            {
                var bossName = link.TextContent?.Trim();
                if (!string.IsNullOrEmpty(bossName)) bossNames.Add(bossName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting boss names from document");
        }

        return bossNames;
    }

    /// <summary>
    ///     Extracts the main duty image URL from a parsed Lodestone duty page.
    /// </summary>
    public string? ExtractImageUrlFromDocument(IDocument document)
    {
        try
        {
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

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting image URL from document");
            return null;
        }
    }

    /// <summary>
    ///     Extracts the duty description from a parsed Lodestone duty page.
    ///     Looks for the "Description" heading and extracts the following paragraph.
    /// </summary>
    public string? ExtractDescriptionFromDocument(IDocument document)
    {
        try
        {
            // Find the "Description" heading and get the next <p> element
            // Structure: <h4 class="db-view__data__title_content_info">Description</h4>
            //            <p class="db-view__data__content_info">The description text...</p>

            var headings = document.QuerySelectorAll("h4.db-view__data__title_content_info");
            foreach (var heading in headings)
            {
                var headingText = heading.TextContent?.Trim();
                if (headingText != null && headingText.Equals("Description", StringComparison.OrdinalIgnoreCase))
                {
                    // Get the next sibling <p> element
                    var descriptionParagraph = heading.NextElementSibling;
                    if (descriptionParagraph != null &&
                        descriptionParagraph.TagName.Equals("P", StringComparison.OrdinalIgnoreCase))
                    {
                        var description = descriptionParagraph.TextContent?.Trim();
                        if (!string.IsNullOrEmpty(description) && description.Length > 10)
                        {
                            // Truncate if too long (max 2000 characters)
                            _logger.LogDebug("Extracted description: {Description}",
                                description.Length > 100 ? description.Substring(0, 100) + "..." : description);
                            return description.Length > 2000 ? description.Substring(0, 2000) : description;
                        }
                    }
                }
            }

            _logger.LogWarning("No description found in document");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting description from document");
            return null;
        }
    }
}