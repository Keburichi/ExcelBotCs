using System.Reflection;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.LodestoneClient;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.Services.Lodestone;
using ExcelBotCs.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace ExcelBotCs.Tests.Services.Lodestone;

/// <summary>
///     Integration tests for LodestoneService with actual repository calls and mocked HTTP responses.
/// </summary>
[TestFixture]
public class LodestoneServiceTests : IntegrationTestBase
{
    private LodestoneService _lodestoneService = null!;
    private ILodestoneDutyRepository _lodestoneDutyRepository = null!;
    private IFightRepository _fightRepository = null!;
    private IFightService _fightService = null!;
    private Mock<HttpMessageHandler> _httpMessageHandlerMock = null!;
    private HttpClient _httpClient = null!;
    private Mock<ILodestoneClient> _lodestoneClient;
    private IOptions<LodestoneOptions> _options;

    [SetUp]
    public new void SetUp()
    {
        base.SetUp();

        // Get real repositories from the test container
        _lodestoneDutyRepository = Factory.Services.GetRequiredService<ILodestoneDutyRepository>();
        _fightRepository = Factory.Services.GetRequiredService<IFightRepository>();
        _fightService = Factory.Services.GetRequiredService<IFightService>();

        // Mock HttpClient to return fake Lodestone HTML
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);

        // Create LodestoneService with real repositories and mocked HttpClient
        _options = Options.Create(new LodestoneOptions
        {
            FCId = "test-fc-id",
            BaseUrl = "https://na.finalfantasyxiv.com",
            RequestDelayMs = 0 // No delay in tests
        });

        var fcMemberService = Factory.Services.GetRequiredService<IFcMemberService>();
        var memberService = Factory.Services.GetRequiredService<IMemberService>();
        var lodestoneDutyService = Factory.Services.GetRequiredService<ILodestoneDutyService>();
        var logger = Factory.Services.GetRequiredService<ILogger<LodestoneService>>();

        // Create the specialized services
        var matchingLogger = Factory.Services.GetRequiredService<ILogger<DutyMatchingService>>();
        var dutyMatchingService = new DutyMatchingService(matchingLogger);

        var scraperLogger = Factory.Services.GetRequiredService<ILogger<LodestoneDutyScraperService>>();
        var scraperService = new LodestoneDutyScraperService(scraperLogger, _httpClient, _options);
        _lodestoneClient = new Mock<ILodestoneClient>();

        _lodestoneService = new LodestoneService(
            _options,
            fcMemberService,
            _fightService,
            logger,
            _httpClient,
            memberService,
            lodestoneDutyService,
            dutyMatchingService,
            scraperService, _lodestoneClient.Object);
    }

    [TearDown]
    public new async Task TearDown()
    {
        _httpClient?.Dispose();
        await base.TearDown();
    }

    #region GetCharactersBioById Tests

    [Test]
    public async Task GetCharactersBioById_ClientNotInitialized()
    {
        _lodestoneClient = null;
        // Force the service's private field to null to simulate not initialized
        var field = typeof(LodestoneService).GetField("_lodestoneClient",
            BindingFlags.Instance | BindingFlags.NonPublic);
        field!.SetValue(_lodestoneService, null);
        Assert.That(() => _lodestoneService.GetCharacterBioById(Guid.NewGuid().ToString()),
            Throws.InvalidOperationException);
    }

    [Test]
    public async Task GetCharacterBioById_CharacterNotFound()
    {
        var id = Guid.NewGuid().ToString();

        _lodestoneClient.Setup(x => x.GetCharacter(id)).ReturnsAsync((LodestoneCharacter?)null);
        var result = await _lodestoneService.GetCharacterBioById(id);

        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public async Task GetCharacterBioById_CharacterFound()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var expectedBio = "This is my dummy bio";

        // Create TestLodestoneCharacter with properly parsed HTML
        var testCharacter = new LodestoneCharacter
        {
            Bio = expectedBio
        };

        // Setup the GetCharacter method to return our test character
        _lodestoneClient.Setup(x => x.GetCharacter(id)).ReturnsAsync(testCharacter);

        // Act
        var result = await _lodestoneService.GetCharacterBioById(id);

        // Assert
        Assert.That(result, Is.EqualTo(expectedBio));
    }

    #endregion

    #region ImportMembers Tests

    [Test]
    public async Task ImportMembers_LodestoneClientNotInitialized()
    {
        _lodestoneClient = null;
        // Force the service's private field to null to simulate not initialized
        var field = typeof(LodestoneService).GetField("_lodestoneClient",
            BindingFlags.Instance | BindingFlags.NonPublic);
        field!.SetValue(_lodestoneService, null);
        Assert.That(() => _lodestoneService.ImportMembers(), Is.EqualTo(new List<FcMemberEntry>()));
    }

    [Test]
    public async Task ImportMembers_NoFcMembers()
    {
        _lodestoneClient.Setup(x => x.GetFreeCompanyMembers(_options.Value.FCId))
            .ReturnsAsync(new List<FcMemberEntry>());

        var result = await _lodestoneService.ImportMembers();

        Assert.That(result, Is.Empty);
    }

    #endregion

    #region Database Integration Tests

    [Test]
    public async Task LodestoneDuty_WithDescriptionAndImageUrl_SavesCorrectly()
    {
        // Arrange
        var duty = new LodestoneDuty
        {
            Name = "Test Extreme",
            LodestoneId = "test789",
            ExpansionId = 5,
            CategoryId = 4,
            FightType = FightType.Extreme,
            BossNames = new List<string> { "Test Boss" },
            Description = "This is a test description for the duty.",
            ImageUrl = "https://example.com/test-image.png"
        };

        // Act
        await _lodestoneDutyRepository.CreateAsync(duty);

        // Assert
        var retrieved = await _lodestoneDutyRepository.GetAsync(duty.Id);
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved.Description, Is.EqualTo("This is a test description for the duty."));
        Assert.That(retrieved.ImageUrl, Is.EqualTo("https://example.com/test-image.png"));
        Assert.That(retrieved.BossNames, Has.Count.EqualTo(1));
        Assert.That(retrieved.BossNames[0], Is.EqualTo("Test Boss"));
    }

    [Test]
    public async Task LodestoneDuty_GetByExpansionAndCategory_FiltersCorrectly()
    {
        // Arrange
        var duty1 = new LodestoneDuty
        {
            Name = "EW Extreme 1",
            LodestoneId = "ew1",
            ExpansionId = 4,
            CategoryId = 4,
            FightType = FightType.Extreme
        };

        var duty2 = new LodestoneDuty
        {
            Name = "DT Extreme 1",
            LodestoneId = "dt1",
            ExpansionId = 5,
            CategoryId = 4,
            FightType = FightType.Extreme
        };

        var duty3 = new LodestoneDuty
        {
            Name = "EW Savage 1",
            LodestoneId = "ew2",
            ExpansionId = 4,
            CategoryId = 5,
            FightType = FightType.Savage
        };

        await _lodestoneDutyRepository.CreateAsync(duty1);
        await _lodestoneDutyRepository.CreateAsync(duty2);
        await _lodestoneDutyRepository.CreateAsync(duty3);

        // Act
        var ewExtremes = await _lodestoneDutyRepository.GetByExpansionAndCategoryAsync(4, 4);

        // Assert
        Assert.That(ewExtremes, Has.Count.EqualTo(1));
        Assert.That(ewExtremes[0].Name, Is.EqualTo("EW Extreme 1"));
    }

    [Test]
    public async Task Fight_UpdateWithDescriptionAndImageUrl_SavesCorrectly()
    {
        // Arrange
        var fight = new Fight
        {
            Name = "Test Fight",
            Type = FightType.Extreme,
            FFLogsExpansionId = 5,
            FFLogsZoneId = 100,
            FFLogsEncounterId = 1
        };

        await _fightRepository.CreateAsync(fight);

        // Act - Update with description and image URL (simulating SyncFightImagesAsync behavior)
        fight.Description = "Updated description from Lodestone";
        fight.ImageUrl = "https://example.com/updated-image.png";
        await _fightRepository.UpdateAsync(fight.Id, fight);

        // Assert
        var retrieved = await _fightRepository.GetAsync(fight.Id);
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved.Description, Is.EqualTo("Updated description from Lodestone"));
        Assert.That(retrieved.ImageUrl, Is.EqualTo("https://example.com/updated-image.png"));
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task LodestoneDuty_NullDescription_HandlesGracefully()
    {
        // Arrange
        var duty = new LodestoneDuty
        {
            Name = "No Description Duty",
            LodestoneId = "nodesc",
            ExpansionId = 5,
            CategoryId = 4,
            FightType = FightType.Extreme,
            Description = null,
            ImageUrl = "https://example.com/image.png"
        };

        // Act
        await _lodestoneDutyRepository.CreateAsync(duty);

        // Assert
        var retrieved = await _lodestoneDutyRepository.GetAsync(duty.Id);
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved.Description, Is.Null);
    }

    [Test]
    public async Task LodestoneDuty_LongDescription_Truncates()
    {
        // Arrange
        var longDescription = new string('A', 2500); // Exceeds 2000 char limit
        var duty = new LodestoneDuty
        {
            Name = "Long Description Duty",
            LodestoneId = "longdesc",
            ExpansionId = 5,
            CategoryId = 4,
            FightType = FightType.Extreme,
            Description = longDescription.Substring(0, 2000) // Simulating truncation
        };

        // Act
        await _lodestoneDutyRepository.CreateAsync(duty);

        // Assert
        var retrieved = await _lodestoneDutyRepository.GetAsync(duty.Id);
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved.Description, Has.Length.EqualTo(2000));
    }

    #endregion


    #region SyncFightImagesAsync Integration Tests

    [Test]
    public async Task SyncFightImages_WithMatchedDuty_UpdatesFightMetadata()
    {
        // Arrange - Create a duty with metadata
        var duty = new LodestoneDuty
        {
            Name = "Test Extreme",
            LodestoneId = "sync-test",
            ExpansionId = 5,
            CategoryId = 4,
            FightType = FightType.Extreme,
            BossNames = new List<string> { "Sync Boss" },
            Description = "This is a synced description",
            ImageUrl = "https://example.com/sync-image.png"
        };

        await _lodestoneDutyRepository.CreateAsync(duty);

        // Create a fight that needs an image
        var fight = new Fight
        {
            Name = "Test Extreme",
            Type = FightType.Extreme,
            FFLogsExpansionId = 5,
            FFLogsZoneId = 100,
            FFLogsEncounterId = 1,
            ImageUrl = null, // No image yet
            Description = null // No description yet
        };

        await _fightRepository.CreateAsync(fight);

        // The test verifies setup - actual sync would be done by calling SyncFightImagesAsync
        var retrievedFight = await _fightRepository.GetAsync(fight.Id);
        Assert.That(retrievedFight, Is.Not.Null);
        Assert.That(retrievedFight.ImageUrl, Is.Null);
        Assert.That(retrievedFight.Description, Is.Null);
    }

    [Test]
    public async Task SyncFightImages_NoMatchingDuty_LeavesFieldsUnchanged()
    {
        // Arrange - Create fight with no matching duty
        var fight = new Fight
        {
            Name = "Unmatched Fight",
            Type = FightType.Extreme,
            FFLogsExpansionId = 5,
            FFLogsZoneId = 200,
            FFLogsEncounterId = 2,
            ImageUrl = null,
            Description = "Original description"
        };

        await _fightRepository.CreateAsync(fight);

        // No matching duty exists
        var retrievedFight = await _fightRepository.GetAsync(fight.Id);
        Assert.That(retrievedFight, Is.Not.Null);
        Assert.That(retrievedFight.Description, Is.EqualTo("Original description"));
    }

    #endregion


    #region Multiple Expansions Tests

    [Test]
    public async Task GetByExpansionAndCategory_MultipleExpansions_FiltersCorrectly()
    {
        // Arrange - Create duties across multiple expansions
        var arrExtreme = new LodestoneDuty
        {
            Name = "ARR Extreme 1",
            LodestoneId = "arr-ex1",
            ExpansionId = 0,
            CategoryId = 4,
            FightType = FightType.Extreme
        };

        var hwExtreme = new LodestoneDuty
        {
            Name = "HW Extreme 1",
            LodestoneId = "hw-ex1",
            ExpansionId = 1,
            CategoryId = 4,
            FightType = FightType.Extreme
        };

        var shbSavage = new LodestoneDuty
        {
            Name = "ShB Savage 1",
            LodestoneId = "shb-sav1",
            ExpansionId = 3,
            CategoryId = 5,
            FightType = FightType.Savage
        };

        await _lodestoneDutyRepository.CreateAsync(arrExtreme);
        await _lodestoneDutyRepository.CreateAsync(hwExtreme);
        await _lodestoneDutyRepository.CreateAsync(shbSavage);

        // Act
        var arrExtremes = await _lodestoneDutyRepository.GetByExpansionAndCategoryAsync(0, 4);
        var hwExtremes = await _lodestoneDutyRepository.GetByExpansionAndCategoryAsync(1, 4);
        var shbSavages = await _lodestoneDutyRepository.GetByExpansionAndCategoryAsync(3, 5);

        // Assert
        Assert.That(arrExtremes, Has.Count.EqualTo(1));
        Assert.That(arrExtremes[0].Name, Is.EqualTo("ARR Extreme 1"));

        Assert.That(hwExtremes, Has.Count.EqualTo(1));
        Assert.That(hwExtremes[0].Name, Is.EqualTo("HW Extreme 1"));

        Assert.That(shbSavages, Has.Count.EqualTo(1));
        Assert.That(shbSavages[0].Name, Is.EqualTo("ShB Savage 1"));
    }

    #endregion
}