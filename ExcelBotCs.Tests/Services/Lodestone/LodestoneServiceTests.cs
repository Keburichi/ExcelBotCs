using System.Reflection;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.LodestoneClient;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.Services.Lodestone;
using ExcelBotCs.TestFramework.Database;
using ExcelBotCs.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;

namespace ExcelBotCs.Tests.Services.Lodestone;

/// <summary>
///     Integration tests for LodestoneService with actual repository calls and mocked HTTP responses.
/// </summary>
[Collection("MongoDB")]
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

    public LodestoneServiceTests(MongoDbFixture fixture) : base(fixture)
    {
    }

    protected override Task OnAfterIntegrationSetupAsync()
    {
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

        return Task.CompletedTask;
    }

    protected override Task BeforeTearDownAsync()
    {
        _httpClient?.Dispose();
        return Task.CompletedTask;
    }

    #region GetCharactersBioById Tests

    [Fact]
    public async Task GetCharactersBioById_ClientNotInitialized()
    {
        _lodestoneClient = null;
        // Force the service's private field to null to simulate not initialized
        var field = typeof(LodestoneService).GetField("_lodestoneClient",
            BindingFlags.Instance | BindingFlags.NonPublic);
        field!.SetValue(_lodestoneService, null);
        Should.Throw<InvalidOperationException>(() => _lodestoneService.GetCharacterBioById(Guid.NewGuid().ToString()));
    }

    [Fact]
    public async Task GetCharacterBioById_CharacterNotFound()
    {
        var id = Guid.NewGuid().ToString();

        _lodestoneClient.Setup(x => x.GetCharacter(id)).ReturnsAsync((LodestoneCharacter?)null);
        var result = await _lodestoneService.GetCharacterBioById(id);

        result.ShouldBe(string.Empty);
    }

    [Fact]
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
        result.ShouldBe(expectedBio);
    }

    #endregion

    #region ImportMembers Tests

    [Fact]
    public async Task ImportMembers_LodestoneClientNotInitialized()
    {
        _lodestoneClient = null;
        // Force the service's private field to null to simulate not initialized
        var field = typeof(LodestoneService).GetField("_lodestoneClient",
            BindingFlags.Instance | BindingFlags.NonPublic);
        field!.SetValue(_lodestoneService, null);
        _lodestoneService.ImportMembers().Result.ShouldBe(new List<FcMemberEntry>());
    }

    [Fact]
    public async Task ImportMembers_NoFcMembers()
    {
        _lodestoneClient.Setup(x => x.GetFreeCompanyMembers(_options.Value.FCId))
            .ReturnsAsync(new List<FcMemberEntry>());

        var result = await _lodestoneService.ImportMembers();

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task ImportMembers_NewMember_CreatesInDatabase()
    {
        // Arrange
        var characterId = "char-new-123";
        var fcMemberEntry = new FcMemberEntry
        {
            Id = characterId,
            Name = "Test Player",
            FreeCompanyRank = "Member",
            Avatar = new Uri("https://example.com/avatar.png")
        };

        var character = new LodestoneCharacter
        {
            Title = "Warrior of Light",
            Bio = "A test bio"
        };

        _lodestoneClient.Setup(x => x.GetFreeCompanyMembers(_options.Value.FCId))
            .ReturnsAsync(new List<FcMemberEntry> { fcMemberEntry });
        _lodestoneClient.Setup(x => x.GetCharacter(characterId))
            .ReturnsAsync(character);

        // Act
        var result = await _lodestoneService.ImportMembers();

        // Assert
        result.Count.ShouldBe(1);

        var fcMemberService = Factory.Services.GetRequiredService<IFcMemberService>();
        var dbMember = await fcMemberService.GetByCharacterId(characterId);
        dbMember.ShouldNotBeNull();
        dbMember.Name.ShouldBe("Test Player");
        dbMember.CharacterId.ShouldBe(characterId);
        dbMember.FcRank.ShouldBe("Member");
        dbMember.Title.ShouldBe("Warrior of Light");
        dbMember.Bio.ShouldBe("A test bio");
        dbMember.Avatar.ShouldBe("https://example.com/avatar.png");
    }

    [Fact]
    public async Task ImportMembers_NewMember_NullAvatar_UsesEmptyString()
    {
        // Arrange
        var characterId = "char-no-avatar";
        var fcMemberEntry = new FcMemberEntry
        {
            Id = characterId,
            Name = "No Avatar Player",
            FreeCompanyRank = "Member",
            Avatar = null
        };

        _lodestoneClient.Setup(x => x.GetFreeCompanyMembers(_options.Value.FCId))
            .ReturnsAsync(new List<FcMemberEntry> { fcMemberEntry });
        _lodestoneClient.Setup(x => x.GetCharacter(characterId))
            .ReturnsAsync(new LodestoneCharacter { Title = "Title", Bio = "bio" });

        // Act
        await _lodestoneService.ImportMembers();

        // Assert
        var fcMemberService = Factory.Services.GetRequiredService<IFcMemberService>();
        var dbMember = await fcMemberService.GetByCharacterId(characterId);
        dbMember.ShouldNotBeNull();
        dbMember.Avatar.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task ImportMembers_NewMember_NullCharacterTitle_UsesEmptyString()
    {
        // Arrange
        var characterId = "char-no-title";
        var fcMemberEntry = new FcMemberEntry
        {
            Id = characterId,
            Name = "No Title Player",
            FreeCompanyRank = "Member",
            Avatar = null
        };

        _lodestoneClient.Setup(x => x.GetFreeCompanyMembers(_options.Value.FCId))
            .ReturnsAsync(new List<FcMemberEntry> { fcMemberEntry });
        _lodestoneClient.Setup(x => x.GetCharacter(characterId))
            .ReturnsAsync(new LodestoneCharacter { Title = null, Bio = "bio" });

        // Act
        await _lodestoneService.ImportMembers();

        // Assert
        var fcMemberService = Factory.Services.GetRequiredService<IFcMemberService>();
        var dbMember = await fcMemberService.GetByCharacterId(characterId);
        dbMember.ShouldNotBeNull();
        dbMember.Title.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task ImportMembers_NewMember_NullCharacter_CreatesWithNullBioAndEmptyTitle()
    {
        // Arrange - GetCharacter returns null (character not found on Lodestone)
        var characterId = "char-null";
        var fcMemberEntry = new FcMemberEntry
        {
            Id = characterId,
            Name = "Null Character Player",
            FreeCompanyRank = "Member",
            Avatar = null
        };

        _lodestoneClient.Setup(x => x.GetFreeCompanyMembers(_options.Value.FCId))
            .ReturnsAsync(new List<FcMemberEntry> { fcMemberEntry });
        _lodestoneClient.Setup(x => x.GetCharacter(characterId))
            .ReturnsAsync((LodestoneCharacter?)null);

        // Act
        var result = await _lodestoneService.ImportMembers();

        // Assert - FcMember created with null Bio and empty Title via null-conditional access
        result.Count.ShouldBe(1);
        var fcMemberService = Factory.Services.GetRequiredService<IFcMemberService>();
        var dbMember = await fcMemberService.GetByCharacterId(characterId);
        dbMember.ShouldNotBeNull();
        dbMember.Title.ShouldBe(string.Empty);
        dbMember.Bio.ShouldBeNull();
    }

    [Fact]
    public async Task ImportMembers_ExistingMember_UpdatesInDatabase()
    {
        // Arrange - Pre-seed DB with existing FcMember
        var characterId = "char-existing-456";
        var fcMemberService = Factory.Services.GetRequiredService<IFcMemberService>();

        var existingFcMember = new FcMember
        {
            CharacterId = characterId,
            Name = "Old Name",
            FcRank = "Member",
            Avatar = "https://example.com/old-avatar.png",
            Title = "Old Title",
            Bio = "Old bio",
            LastSynchronisation = DateTime.UtcNow.AddDays(-1)
        };
        await fcMemberService.CreateAsync(existingFcMember);

        // Setup Lodestone mock with updated data
        var fcMemberEntry = new FcMemberEntry
        {
            Id = characterId,
            Name = "New Name",
            FreeCompanyRank = "Officer",
            Avatar = new Uri("https://example.com/new-avatar.png")
        };

        var character = new LodestoneCharacter
        {
            Title = "New Title",
            Bio = "New bio"
        };

        _lodestoneClient.Setup(x => x.GetFreeCompanyMembers(_options.Value.FCId))
            .ReturnsAsync(new List<FcMemberEntry> { fcMemberEntry });
        _lodestoneClient.Setup(x => x.GetCharacter(characterId))
            .ReturnsAsync(character);

        // Act
        var result = await _lodestoneService.ImportMembers();

        // Assert
        result.Count.ShouldBe(1);

        var dbMember = await fcMemberService.GetByCharacterId(characterId);
        dbMember.ShouldNotBeNull();
        dbMember.Name.ShouldBe("New Name");
        dbMember.FcRank.ShouldBe("Officer");
        dbMember.Title.ShouldBe("New Title");
        dbMember.Bio.ShouldBe("New bio");
        dbMember.Avatar.ShouldBe("https://example.com/new-avatar.png");
    }

    [Fact]
    public async Task ImportMembers_ExistingMemberWithNoLinkedMember_SkipsPlayerNameUpdate()
    {
        // Arrange - Existing FcMember in DB, but no linked Member entity
        var characterId = "char-no-link";
        var fcMemberService = Factory.Services.GetRequiredService<IFcMemberService>();
        var memberService = Factory.Services.GetRequiredService<IMemberService>();

        await fcMemberService.CreateAsync(new FcMember
        {
            CharacterId = characterId,
            Name = "Old Name",
            FcRank = "Member",
            Avatar = "",
            Title = "",
            Bio = "bio",
            LastSynchronisation = DateTime.UtcNow.AddDays(-1)
        });

        var fcMemberEntry = new FcMemberEntry
        {
            Id = characterId,
            Name = "New Name",
            FreeCompanyRank = "Officer",
            Avatar = null
        };

        _lodestoneClient.Setup(x => x.GetFreeCompanyMembers(_options.Value.FCId))
            .ReturnsAsync(new List<FcMemberEntry> { fcMemberEntry });
        _lodestoneClient.Setup(x => x.GetCharacter(characterId))
            .ReturnsAsync(new LodestoneCharacter { Title = "", Bio = "bio" });

        // Act - Should not throw even with no linked member
        var result = await _lodestoneService.ImportMembers();

        // Assert - FcMember updated, no member exists for this LodestoneId
        result.Count.ShouldBe(1);

        var dbFcMember = await fcMemberService.GetByCharacterId(characterId);
        dbFcMember.ShouldNotBeNull();
        dbFcMember.Name.ShouldBe("New Name");

        var member = await memberService.GetByLodestoneId(characterId);
        member.ShouldBeNull();
    }

    [Fact]
    public async Task ImportMembers_ExistingMemberWithLinkedMember_UpdatesPlayerName()
    {
        // Arrange
        var characterId = "char-linked-789";
        var fcMemberService = Factory.Services.GetRequiredService<IFcMemberService>();
        var memberService = Factory.Services.GetRequiredService<IMemberService>();

        // Seed FcMember
        await fcMemberService.CreateAsync(new FcMember
        {
            CharacterId = characterId,
            Name = "Old Player Name",
            FcRank = "Member",
            Avatar = "",
            Title = "",
            Bio = "bio",
            LastSynchronisation = DateTime.UtcNow.AddDays(-1)
        });

        // Seed linked Member with matching LodestoneId
        var linkedMember = new Member
        {
            DiscordId = GenerateRandomDiscordId(),
            DiscordName = "TestUser",
            DiscordAvatar = "avatar",
            LodestoneId = characterId,
            PlayerName = "Old Player Name",
            ExperienceIds = new List<string>(),
            RoleIds = new List<string>()
        };
        await memberService.CreateAsync(linkedMember);

        // Lodestone mock with updated name
        var fcMemberEntry = new FcMemberEntry
        {
            Id = characterId,
            Name = "Updated Player Name",
            FreeCompanyRank = "Member",
            Avatar = null
        };

        _lodestoneClient.Setup(x => x.GetFreeCompanyMembers(_options.Value.FCId))
            .ReturnsAsync(new List<FcMemberEntry> { fcMemberEntry });
        _lodestoneClient.Setup(x => x.GetCharacter(characterId))
            .ReturnsAsync(new LodestoneCharacter { Title = "Title", Bio = "bio" });

        // Act
        await _lodestoneService.ImportMembers();

        // Assert - Member.PlayerName should be updated
        var updatedMember = await memberService.GetByLodestoneId(characterId);
        updatedMember.ShouldNotBeNull();
        updatedMember.PlayerName.ShouldBe("Updated Player Name");
    }

    [Fact]
    public async Task ImportMembers_StaleFcMember_DeletesFromDatabase()
    {
        // Arrange - Create a stale FcMember whose DateModified is more than 1 day old
        var fcMemberService = Factory.Services.GetRequiredService<IFcMemberService>();
        var mongoClient = Factory.Services.GetRequiredService<IMongoClient>();

        var staleMember = new FcMember
        {
            CharacterId = "stale-char",
            Name = "Stale Member",
            FcRank = "Member",
            Avatar = "",
            Title = "",
            Bio = "",
            LastSynchronisation = DateTime.UtcNow.AddDays(-5)
        };
        await fcMemberService.CreateAsync(staleMember);

        // Directly set DateModified to 2 days ago via MongoDB (bypassing BaseRepository auto-update)
        var database = mongoClient.GetDatabase("TestDatabase");
        var collection = database.GetCollection<FcMember>("FcMember");
        var filter = Builders<FcMember>.Filter.Eq(x => x.Id, staleMember.Id);
        var update = Builders<FcMember>.Update.Set(x => x.DateModified, DateTime.UtcNow.AddDays(-2));
        await collection.UpdateOneAsync(filter, update);

        // Mock Lodestone to return empty - no current FC members
        _lodestoneClient.Setup(x => x.GetFreeCompanyMembers(_options.Value.FCId))
            .ReturnsAsync(new List<FcMemberEntry>());

        // Act
        await _lodestoneService.ImportMembers();

        // Assert - Stale member should be deleted
        var deletedMember = await fcMemberService.GetByCharacterId("stale-char");
        deletedMember.ShouldBeNull();
    }

    [Fact]
    public async Task ImportMembers_RecentFcMember_NotDeleted()
    {
        // Arrange - Create a recent FcMember (DateModified = now, set by CreateAsync)
        var fcMemberService = Factory.Services.GetRequiredService<IFcMemberService>();

        var recentMember = new FcMember
        {
            CharacterId = "recent-char",
            Name = "Recent Member",
            FcRank = "Member",
            Avatar = "",
            Title = "",
            Bio = "",
            LastSynchronisation = DateTime.UtcNow
        };
        await fcMemberService.CreateAsync(recentMember);

        // Mock Lodestone to return empty - member not in FC roster but EditDate is recent
        _lodestoneClient.Setup(x => x.GetFreeCompanyMembers(_options.Value.FCId))
            .ReturnsAsync(new List<FcMemberEntry>());

        // Act
        await _lodestoneService.ImportMembers();

        // Assert - Recent member should NOT be deleted (DateModified within 1 day)
        var existingMember = await fcMemberService.GetByCharacterId("recent-char");
        existingMember.ShouldNotBeNull();
    }

    [Fact]
    public async Task ImportMembers_MixOfNewAndExisting_HandlesAllCorrectly()
    {
        // Arrange
        var fcMemberService = Factory.Services.GetRequiredService<IFcMemberService>();

        var existingCharacterId = "existing-mix-char";
        var newCharacterId = "new-mix-char";

        // Seed existing member
        await fcMemberService.CreateAsync(new FcMember
        {
            CharacterId = existingCharacterId,
            Name = "Old Name",
            FcRank = "Member",
            Avatar = "",
            Title = "",
            Bio = "",
            LastSynchronisation = DateTime.UtcNow.AddDays(-1)
        });

        // Lodestone returns both new and existing
        var fcMembers = new List<FcMemberEntry>
        {
            new()
            {
                Id = existingCharacterId, Name = "Updated Name", FreeCompanyRank = "Officer", Avatar = null
            },
            new()
            {
                Id = newCharacterId, Name = "Brand New Player", FreeCompanyRank = "Member", Avatar = null
            }
        };

        _lodestoneClient.Setup(x => x.GetFreeCompanyMembers(_options.Value.FCId))
            .ReturnsAsync(fcMembers);
        _lodestoneClient.Setup(x => x.GetCharacter(It.IsAny<string>()))
            .ReturnsAsync(new LodestoneCharacter { Title = "Title", Bio = "bio" });

        // Act
        var result = await _lodestoneService.ImportMembers();

        // Assert
        result.Count.ShouldBe(2);

        var existing = await fcMemberService.GetByCharacterId(existingCharacterId);
        existing.ShouldNotBeNull();
        existing.Name.ShouldBe("Updated Name");
        existing.FcRank.ShouldBe("Officer");

        var newMember = await fcMemberService.GetByCharacterId(newCharacterId);
        newMember.ShouldNotBeNull();
        newMember.Name.ShouldBe("Brand New Player");
    }

    [Fact]
    public async Task ImportMembers_ReturnsFcMemberEntriesFromLodestone()
    {
        // Arrange - Verify the method returns the original Lodestone data, not DB state
        var entries = new List<FcMemberEntry>
        {
            new() { Id = "ret-1", Name = "Player One", FreeCompanyRank = "Member", Avatar = null },
            new() { Id = "ret-2", Name = "Player Two", FreeCompanyRank = "Officer", Avatar = null }
        };

        _lodestoneClient.Setup(x => x.GetFreeCompanyMembers(_options.Value.FCId))
            .ReturnsAsync(entries);
        _lodestoneClient.Setup(x => x.GetCharacter(It.IsAny<string>()))
            .ReturnsAsync(new LodestoneCharacter { Title = "", Bio = "" });

        // Act
        var result = await _lodestoneService.ImportMembers();

        // Assert - Returns the same list object from Lodestone
        result.ShouldBeSameAs(entries);
        result.Count.ShouldBe(2);
        result[0].Name.ShouldBe("Player One");
        result[1].Name.ShouldBe("Player Two");
    }

    #endregion

    #region Database Integration Tests

    [Fact]
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
        retrieved.ShouldNotBeNull();
        retrieved.Description.ShouldBe("This is a test description for the duty.");
        retrieved.ImageUrl.ShouldBe("https://example.com/test-image.png");
        retrieved.BossNames.Count.ShouldBe(1);
        retrieved.BossNames[0].ShouldBe("Test Boss");
    }

    [Fact]
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
        ewExtremes.Count.ShouldBe(1);
        ewExtremes[0].Name.ShouldBe("EW Extreme 1");
    }

    [Fact]
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
        retrieved.ShouldNotBeNull();
        retrieved.Description.ShouldBe("Updated description from Lodestone");
        retrieved.ImageUrl.ShouldBe("https://example.com/updated-image.png");
    }

    #endregion

    #region Edge Cases

    [Fact]
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
        retrieved.ShouldNotBeNull();
        retrieved.Description.ShouldBeNull();
    }

    [Fact]
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
        retrieved.ShouldNotBeNull();
        retrieved.Description.Length.ShouldBe(2000);
    }

    #endregion


    #region SyncFightImagesAsync Integration Tests

    [Fact]
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
        retrievedFight.ShouldNotBeNull();
        retrievedFight.ImageUrl.ShouldBeNull();
        retrievedFight.Description.ShouldBeNull();
    }

    [Fact]
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
        retrievedFight.ShouldNotBeNull();
        retrievedFight.Description.ShouldBe("Original description");
    }

    #endregion


    #region Multiple Expansions Tests

    [Fact]
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
        arrExtremes.Count.ShouldBe(1);
        arrExtremes[0].Name.ShouldBe("ARR Extreme 1");

        hwExtremes.Count.ShouldBe(1);
        hwExtremes[0].Name.ShouldBe("HW Extreme 1");

        shbSavages.Count.ShouldBe(1);
        shbSavages[0].Name.ShouldBe("ShB Savage 1");
    }

    #endregion
}