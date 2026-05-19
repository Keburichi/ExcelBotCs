using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.LodestoneClient;
using ExcelBotCs.Services.API.Interfaces;
using Microsoft.Extensions.Options;
using DbLodestoneDuty = ExcelBotCs.Models.Database.LodestoneDuty;

namespace ExcelBotCs.Services.Lodestone;

public class LodestoneService
{
    private readonly ILodestoneClient _lodestoneClient;
    private readonly IOptions<LodestoneOptions> _options;
    private readonly IFcMemberService _fcMemberService;
    private readonly IFightService _fightService;
    private readonly IBossService _bossService;
    private readonly ILogger<LodestoneService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IMemberService _memberService;
    private readonly ILodestoneDutyService _lodestoneDutyService;
    private readonly DutyMatchingService _dutyMatchingService;
    private readonly LodestoneDutyScraperService _scraperService;

    public LodestoneService(IOptions<LodestoneOptions> options, IFcMemberService fcMemberService,
        IFightService fightService, IBossService bossService, ILogger<LodestoneService> logger, HttpClient httpClient,
        IMemberService memberService, ILodestoneDutyService lodestoneDutyService,
        DutyMatchingService dutyMatchingService, LodestoneDutyScraperService scraperService,
        ILodestoneClient lodestoneClient)
    {
        _options = options;
        _fcMemberService = fcMemberService;
        _fightService = fightService;
        _bossService = bossService;
        _logger = logger;
        _httpClient = httpClient;
        _memberService = memberService;
        _lodestoneDutyService = lodestoneDutyService;
        _dutyMatchingService = dutyMatchingService;
        _scraperService = scraperService;
        _lodestoneClient = lodestoneClient;
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "ExcelBotCs/1.0");
    }

    public async Task<string> GetCharacterBioById(string characterId)
    {
        if (_lodestoneClient == null)
            throw new InvalidOperationException("Lodestone client not initialized");

        var character = await _lodestoneClient.GetCharacter(characterId);
        return character?.Bio ?? string.Empty;
    }

    public async Task<List<FcMemberEntry>> ImportMembers()
    {
        if (_lodestoneClient == null)
            return new List<FcMemberEntry>();

        var fcMembers = await _lodestoneClient.GetFreeCompanyMembers(_options.Value.FCId);

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
                fcMember.Id = dbFcMember.Id;

                await _fcMemberService.UpdateAsync(dbFcMember.Id, fcMember);

                // Also update the PlayerName property in the member entity
                var member = await _memberService.GetByLodestoneId(freeCompanyMembersEntry.Id);
                if (member == null)
                    continue;

                member.PlayerName = fcMember.Name;
                await _memberService.UpdateAsync(member.Id, member);
            }
        }

        // Fetch all fc members from database to check for people who have left
        var dbFcMembers = await _fcMemberService.GetAsync();

        foreach (var dbFcMember in dbFcMembers.Where(x => x.DateModified < DateTime.UtcNow.AddDays(-1)))
        {
            _logger.LogInformation($"Found {dbFcMember.Name} who left fc. Removing member");
            await _fcMemberService.DeleteAsync(dbFcMember.Id);
        }

        return fcMembers;
    }

    private async Task<FcMember> CreateFcMember(FcMemberEntry freeCompanyMembersEntry)
    {
        var lodestoneCharacter = await _lodestoneClient.GetCharacter(freeCompanyMembersEntry.Id);

        return new FcMember
        {
            CharacterId = freeCompanyMembersEntry.Id,
            Avatar = freeCompanyMembersEntry.Avatar?.ToString() ?? string.Empty,
            LastSynchronisation = DateTime.UtcNow,
            FcRank = freeCompanyMembersEntry.FreeCompanyRank,
            Name = freeCompanyMembersEntry.Name,
            Title = lodestoneCharacter?.Title ?? string.Empty,
            Bio = lodestoneCharacter?.Bio
        };
    }


    public async Task SyncBossImagesAsync()
    {
        try
        {
            _logger.LogInformation("Starting boss image synchronization from Lodestone");

            var allBosses = await _bossService.GetBossesAsync();
            var bossesNeedingImages = allBosses.Where(b => string.IsNullOrEmpty(b.ImageUrl)).ToList();

            if (!bossesNeedingImages.Any())
            {
                _logger.LogInformation("No bosses require image synchronization");
                return;
            }

            _logger.LogInformation("Found {Count} bosses needing images", bossesNeedingImages.Count);

            var dutyLookup = await BuildDutyLookupAsync();

            var updated = 0;
            var failed = 0;

            foreach (var boss in bossesNeedingImages)
                try
                {
                    _logger.LogDebug("Processing boss: {BossName}", boss.Name);

                    var matchedDuty = _dutyMatchingService.FindBestMatchForBoss(boss, dutyLookup);

                    if (matchedDuty == null)
                    {
                        _logger.LogWarning("No Lodestone match found for boss: {BossName}", boss.Name);
                        failed++;
                        continue;
                    }

                    if (string.IsNullOrEmpty(matchedDuty.ImageUrl))
                    {
                        _logger.LogWarning("No cached image URL for matched duty: {DutyName}", matchedDuty.Name);
                        failed++;
                        continue;
                    }

                    boss.ImageUrl = matchedDuty.ImageUrl;
                    boss.Description = matchedDuty.Description ?? boss.Description;
                    await _bossService.UpdateAsync(boss.Id, boss);

                    _logger.LogInformation(
                        "Updated metadata for boss '{BossName}': ImageUrl={ImageUrl}",
                        boss.Name, boss.ImageUrl);
                    updated++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing boss: {BossName}", boss.Name);
                    failed++;
                }

            _logger.LogInformation("Boss image synchronization complete. Updated: {Updated}, Failed: {Failed}",
                updated, failed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during boss image synchronization");
        }
    }

    private async Task<List<DbLodestoneDuty>> BuildDutyLookupAsync()
    {
        // Clear scraper cache at the start of the operation
        _scraperService.ClearCache();

        // Category mappings
        var categories = new Dictionary<FightType, int>
        {
            { FightType.Extreme, 4 },
            { FightType.Savage, 5 },
            { FightType.Ultimate, 28 }
        };

        // Expansion IDs (0 = ARR, 1 = HW, 2 = SB, 3 = ShB, 4 = EW, 5 = DT)
        var expansionIds = Enumerable.Range(0, 6).ToList();

        var newDuties = new List<DbLodestoneDuty>();
        var hasExistingData = await _lodestoneDutyService.HasDataAsync();

        if (hasExistingData)
            _logger.LogDebug("Checking for missing Lodestone duty data");
        else
            _logger.LogInformation("No cached Lodestone duty data found, performing initial scrape");

        // Check each expansion/category combination
        foreach (var category in categories)
        foreach (var expansionId in expansionIds)
        {
            // Check if we already have data for this combination
            var hasData =
                await _lodestoneDutyService.HasDataForExpansionAndCategoryAsync(expansionId, category.Value);

            if (hasData)
            {
                _logger.LogDebug("Skipping {Type} expansion {ExpansionId} - data already exists",
                    category.Key, expansionId);
                continue;
            }

            _logger.LogInformation("Scraping missing data for {Type} expansion {ExpansionId}",
                category.Key, expansionId);

            try
            {
                var duties =
                    await _scraperService.ScrapeListingPageAsync(expansionId, category.Value, category.Key);

                _logger.LogDebug("Scraped {Count} duties for {Type} expansion {ExpansionId}",
                    duties.Count, category.Key, expansionId);

                // Populate all metadata (boss names, description, image URL) for each duty
                foreach (var duty in duties)
                    try
                    {
                        await _scraperService.PopulateDutyMetadataAsync(duty);
                        await Task.Delay(_options.Value.RequestDelayMs); // Rate limiting
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to populate metadata for duty: {DutyName}", duty.Name);
                    }

                newDuties.AddRange(duties);

                _logger.LogDebug("Populated metadata for {Count} duties in {Type} expansion {ExpansionId}",
                    duties.Count, category.Key, expansionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scraping {Type} expansion {ExpansionId}",
                    category.Key, expansionId);
            }
        }

        // Persist new duties to database
        if (newDuties.Any())
        {
            _logger.LogInformation("Persisting {Count} new duties to database", newDuties.Count);
            await _lodestoneDutyService.BulkCreateAsync(newDuties);
        }

        // Clear scraper cache at the end of the operation
        _scraperService.ClearCache();

        // Return all duties from database
        return await _lodestoneDutyService.GetAsync();
    }
}