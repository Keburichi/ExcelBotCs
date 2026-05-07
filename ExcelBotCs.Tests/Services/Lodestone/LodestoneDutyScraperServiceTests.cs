using System.Net;
using AngleSharp;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Services.Lodestone;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace ExcelBotCs.Tests.Services.Lodestone;

/// <summary>
///     Unit tests for LodestoneDutyScraperService - tests HTML scraping and parsing logic with mocked HTTP responses.
/// </summary>
public class LodestoneDutyScraperServiceTests : IDisposable
{
    private readonly LodestoneDutyScraperService _service;
    private readonly Mock<ILogger<LodestoneDutyScraperService>> _loggerMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly IOptions<LodestoneOptions> _options;

    public LodestoneDutyScraperServiceTests()
    {
        _loggerMock = new Mock<ILogger<LodestoneDutyScraperService>>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _options = Options.Create(new LodestoneOptions
        {
            BaseUrl = "https://na.finalfantasyxiv.com",
            FCId = "test-fc-id",
            RequestDelayMs = 0
        });

        _service = new LodestoneDutyScraperService(_loggerMock.Object, _httpClient, _options);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }

    #region Helper Methods

    private void SetupHttpResponse(string url, string htmlContent, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == url),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(htmlContent)
            });
    }

    private string CreateDutyPageHtml(string dutyName, string description, string imageUrl, params string[] bossNames)
    {
        var bossLinksHtml = string.Join("\n", bossNames.Select(boss =>
            $"<li class=\"boss\"><a href=\"/lodestone/playguide/db/npc/enemy/{Guid.NewGuid()}/\"><strong>{boss}</strong></a></li>"));

        return $@"
<!DOCTYPE html>
<html>
<body>
    <div class=""db-view__data"">
        <h3 class=""db-view__data__head"">Data</h3>

        <div class=""db-view__data__inner db-view__data__inner--gold"">
            <div class=""db-view__data__inner__wrapper"">
                <h3 class=""db-view__data__title--gold"">Boss</h3>
                <ul class=""db-view__data__boss_list"">
                    {bossLinksHtml}
                </ul>
            </div>
        </div>

        <h4 class=""db-view__data__title_content_info"">Information</h4>
        <ul class=""db-view__data__content_info"">
            <li>Time Limit: 90m</li>
        </ul>

        <h4 class=""db-view__data__title_content_info"">Description</h4>
        <p class=""db-view__data__content_info"">{description}</p>

        <div class=""db__l_main__content"">
            <img src=""{imageUrl}"" alt=""{dutyName}"" />
        </div>
    </div>
</body>
</html>";
    }

    private string CreateListingPageHtml(params (string name, string lodestoneId)[] duties)
    {
        var dutyLinksHtml = string.Join("\n", duties.Select(duty =>
            $"<a href=\"/lodestone/playguide/db/duty/{duty.lodestoneId}/\" class=\"db-table__txt--detail_link\">{duty.name}</a>"));

        return $@"
<!DOCTYPE html>
<html>
<body>
    <div class=""db-table"">
        {dutyLinksHtml}
    </div>
</body>
</html>";
    }

    #endregion

    #region ClearCache Tests

    [Fact]
    public void ClearCache_ClearsInternalCache()
    {
        // Act
        _service.ClearCache();

        // Assert - Verify no exception thrown and logger was called
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Cleared duty page cache")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region ScrapeListingPageAsync Tests

    [Fact]
    public async Task ScrapeListingPageAsync_ValidPage_ReturnsDuties()
    {
        // Arrange
        var listingHtml = CreateListingPageHtml(
            ("The Minstrel's Ballad: Zodiark's Reign", "zodiark"),
            ("The Minstrel's Ballad: Hydaelyn's Call", "hydaelyn")
        );

        var url = "https://na.finalfantasyxiv.com/lodestone/playguide/db/duty/?category2=4&ex_version=4";
        SetupHttpResponse(url, listingHtml);

        // Act
        var result = await _service.ScrapeListingPageAsync(4, 4, FightType.Extreme);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result[0].Name.ShouldBe("The Minstrel's Ballad: Zodiark's Reign");
        result[0].LodestoneId.ShouldBe("zodiark");
        result[0].ExpansionId.ShouldBe(4);
        result[0].CategoryId.ShouldBe(4);
        result[0].FightType.ShouldBe(FightType.Extreme);
        result[1].Name.ShouldBe("The Minstrel's Ballad: Hydaelyn's Call");
    }

    [Fact]
    public async Task ScrapeListingPageAsync_EmptyPage_ReturnsEmptyList()
    {
        // Arrange
        var emptyHtml = CreateListingPageHtml();
        var url = "https://na.finalfantasyxiv.com/lodestone/playguide/db/duty/?category2=4&ex_version=0";
        SetupHttpResponse(url, emptyHtml);

        // Act
        var result = await _service.ScrapeListingPageAsync(0, 4, FightType.Extreme);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task ScrapeListingPageAsync_HttpError_ReturnsEmptyList()
    {
        // Arrange
        var url = "https://na.finalfantasyxiv.com/lodestone/playguide/db/duty/?category2=4&ex_version=5";
        SetupHttpResponse(url, "", HttpStatusCode.InternalServerError);

        // Act
        var result = await _service.ScrapeListingPageAsync(5, 4, FightType.Extreme);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    #endregion

    #region ParseDutyListingHtml Tests

    [Fact]
    public async Task ParseDutyListingHtml_ValidHtml_ExtractsAllDuties()
    {
        // Arrange
        var html = CreateListingPageHtml(
            ("Duty One", "duty1"),
            ("Duty Two", "duty2"),
            ("Duty Three", "duty3")
        );

        // Act
        var result = await _service.ParseDutyListingHtmlAsync(html, 5, 4, FightType.Extreme);

        // Assert
        result.Count.ShouldBe(3);
        result[0].Name.ShouldBe("Duty One");
        result[0].LodestoneId.ShouldBe("duty1");
        result[1].Name.ShouldBe("Duty Two");
        result[2].Name.ShouldBe("Duty Three");
    }

    [Fact]
    public async Task ParseDutyListingHtml_EmptyHtml_ReturnsEmptyList()
    {
        // Arrange
        var html = "<html><body></body></html>";

        // Act
        var result = await _service.ParseDutyListingHtmlAsync(html, 5, 4, FightType.Extreme);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task ParseDutyListingHtml_MalformedHtml_ReturnsEmptyList()
    {
        // Arrange
        var html = "<html><body>Invalid HTML without proper structure";

        // Act
        var result = await _service.ParseDutyListingHtmlAsync(html, 5, 4, FightType.Extreme);

        // Assert
        result.ShouldBeEmpty();
    }

    #endregion

    #region PopulateDutyMetadataAsync Tests

    [Fact]
    public async Task PopulateDutyMetadataAsync_EmptyDuty_PopulatesMetadata()
    {
        // Arrange
        var duty = new LodestoneDuty
        {
            Name = "Test Extreme",
            LodestoneId = "test123",
            ExpansionId = 5,
            CategoryId = 4,
            FightType = FightType.Extreme
        };

        var dutyHtml = CreateDutyPageHtml(
            "Test Extreme",
            "This is a test description.",
            "https://lds-img.finalfantasyxiv.com/test.png",
            "Test Boss"
        );

        var url = "https://na.finalfantasyxiv.com/lodestone/playguide/db/duty/test123/";
        SetupHttpResponse(url, dutyHtml);

        // Act
        await _service.PopulateDutyMetadataAsync(duty);

        // Assert
        duty.BossNames.Count.ShouldBe(1);
        duty.BossNames[0].ShouldBe("Test Boss");
        duty.Description.ShouldBe("This is a test description.");
        duty.ImageUrl.ShouldBe("https://lds-img.finalfantasyxiv.com/test.png");
    }

    [Fact]
    public async Task PopulateDutyMetadataAsync_AlreadyPopulated_SkipsRequest()
    {
        // Arrange
        var duty = new LodestoneDuty
        {
            Name = "Test Extreme",
            LodestoneId = "test123",
            ExpansionId = 5,
            CategoryId = 4,
            FightType = FightType.Extreme,
            BossNames = new List<string> { "Existing Boss" },
            Description = "Existing description",
            ImageUrl = "https://existing.com/image.png"
        };

        // Don't set up any HTTP response - should not be called

        // Act
        await _service.PopulateDutyMetadataAsync(duty);

        // Assert - Should not have made HTTP request, data unchanged
        duty.BossNames.Count.ShouldBe(1);
        duty.BossNames[0].ShouldBe("Existing Boss");
        duty.Description.ShouldBe("Existing description");
        duty.ImageUrl.ShouldBe("https://existing.com/image.png");
    }

    [Fact]
    public async Task PopulateDutyMetadataAsync_MultipleBosses_ExtractsAll()
    {
        // Arrange
        var duty = new LodestoneDuty
        {
            Name = "The Unending Coil of Bahamut (Ultimate)",
            LodestoneId = "ucob",
            ExpansionId = 2,
            CategoryId = 28,
            FightType = FightType.Ultimate
        };

        var dutyHtml = CreateDutyPageHtml(
            "UCOB",
            "Ultimate raid challenge.",
            "https://lds-img.finalfantasyxiv.com/ucob.png",
            "Twintania", "Nael deus Darnus", "Bahamut Prime"
        );

        var url = "https://na.finalfantasyxiv.com/lodestone/playguide/db/duty/ucob/";
        SetupHttpResponse(url, dutyHtml);

        // Act
        await _service.PopulateDutyMetadataAsync(duty);

        // Assert
        duty.BossNames.Count.ShouldBe(3);
        duty.BossNames.ShouldContain("Twintania");
        duty.BossNames.ShouldContain("Nael deus Darnus");
        duty.BossNames.ShouldContain("Bahamut Prime");
    }

    #endregion

    #region ExtractBossNamesFromDocument Tests

    [Fact]
    public async Task ExtractBossNamesFromDocument_ValidDocument_ExtractsBosses()
    {
        // Arrange
        var html = CreateDutyPageHtml(
            "Test Duty",
            "Description",
            "https://example.com/image.png",
            "Boss One", "Boss Two", "Boss Three"
        );

        var config = Configuration.Default;
        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(req => req.Content(html));

        // Act
        var result = _service.ExtractBossNamesFromDocument(document);

        // Assert
        result.Count.ShouldBe(3);
        result.ShouldContain("Boss One");
        result.ShouldContain("Boss Two");
        result.ShouldContain("Boss Three");
    }

    [Fact]
    public async Task ExtractBossNamesFromDocument_NoBosses_ReturnsEmptyList()
    {
        // Arrange
        var html = "<html><body><div>No boss links here</div></body></html>";

        var config = Configuration.Default;
        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(req => req.Content(html));

        // Act
        var result = _service.ExtractBossNamesFromDocument(document);

        // Assert
        result.ShouldBeEmpty();
    }

    #endregion

    #region ExtractImageUrlFromDocument Tests

    [Fact]
    public async Task ExtractImageUrlFromDocument_AbsoluteUrl_ReturnsUrl()
    {
        // Arrange
        var imageUrl = "https://lds-img.finalfantasyxiv.com/test.png";
        var html = CreateDutyPageHtml("Test", "Desc", imageUrl);

        var config = Configuration.Default;
        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(req => req.Content(html));

        // Act
        var result = _service.ExtractImageUrlFromDocument(document);

        // Assert
        result.ShouldBe(imageUrl);
    }

    [Fact]
    public async Task ExtractImageUrlFromDocument_ProtocolRelativeUrl_AddsHttps()
    {
        // Arrange
        var html = @"
<html>
<body>
    <div class=""db__l_main__content"">
        <img src=""//lds-img.finalfantasyxiv.com/test.png"" />
    </div>
</body>
</html>";

        var config = Configuration.Default;
        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(req => req.Content(html));

        // Act
        var result = _service.ExtractImageUrlFromDocument(document);

        // Assert
        result.ShouldBe("https://lds-img.finalfantasyxiv.com/test.png");
    }

    [Fact]
    public async Task ExtractImageUrlFromDocument_RelativeUrl_MakesAbsolute()
    {
        // Arrange
        var html = @"
<html>
<body>
    <div class=""db__l_main__content"">
        <img src=""/img/test.png"" />
    </div>
</body>
</html>";

        var config = Configuration.Default;
        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(req => req.Content(html));

        // Act
        var result = _service.ExtractImageUrlFromDocument(document);

        // Assert
        result.ShouldBe("https://na.finalfantasyxiv.com/img/test.png");
    }

    [Fact]
    public async Task ExtractImageUrlFromDocument_NoImage_ReturnsNull()
    {
        // Arrange
        var html = "<html><body><div>No image here</div></body></html>";

        var config = Configuration.Default;
        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(req => req.Content(html));

        // Act
        var result = _service.ExtractImageUrlFromDocument(document);

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region ExtractDescriptionFromDocument Tests

    [Fact]
    public async Task ExtractDescriptionFromDocument_ValidDescription_ExtractsCorrectly()
    {
        // Arrange
        var description = "This is a test description for the duty.";
        var html = CreateDutyPageHtml("Test", description, "https://example.com/image.png");

        var config = Configuration.Default;
        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(req => req.Content(html));

        // Act
        var result = _service.ExtractDescriptionFromDocument(document);

        // Assert
        result.ShouldBe(description);
    }

    [Fact]
    public async Task ExtractDescriptionFromDocument_LongDescription_Truncates()
    {
        // Arrange
        var longDescription = new string('A', 2500); // Exceeds 2000 char limit
        var html = CreateDutyPageHtml("Test", longDescription, "https://example.com/image.png");

        var config = Configuration.Default;
        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(req => req.Content(html));

        // Act
        var result = _service.ExtractDescriptionFromDocument(document);

        // Assert
        result.ShouldNotBeNull();
        result!.Length.ShouldBe(2000);
    }

    [Fact]
    public async Task ExtractDescriptionFromDocument_ShortDescription_Ignored()
    {
        // Arrange - Description too short (less than 10 chars)
        var html = CreateDutyPageHtml("Test", "Short", "https://example.com/image.png");

        var config = Configuration.Default;
        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(req => req.Content(html));

        // Act
        var result = _service.ExtractDescriptionFromDocument(document);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task ExtractDescriptionFromDocument_NoDescription_ReturnsNull()
    {
        // Arrange
        var html = @"
<html>
<body>
    <div class=""db-view__data"">
        <h4 class=""db-view__data__title_content_info"">Information</h4>
        <p>Some other info</p>
    </div>
</body>
</html>";

        var config = Configuration.Default;
        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(req => req.Content(html));

        // Act
        var result = _service.ExtractDescriptionFromDocument(document);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task ExtractDescriptionFromDocument_DescriptionHeadingWithoutParagraph_ReturnsNull()
    {
        // Arrange - Has "Description" heading but no following paragraph
        var html = @"
<html>
<body>
    <div class=""db-view__data"">
        <h4 class=""db-view__data__title_content_info"">Description</h4>
        <div>This is not a paragraph</div>
    </div>
</body>
</html>";

        var config = Configuration.Default;
        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(req => req.Content(html));

        // Act
        var result = _service.ExtractDescriptionFromDocument(document);

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region Caching Tests

    [Fact]
    public async Task FetchAndParseDutyPage_CalledTwice_UsesCacheOnSecondCall()
    {
        // Arrange
        var duty1 = new LodestoneDuty
        {
            Name = "Test Duty",
            LodestoneId = "cache-test",
            ExpansionId = 5,
            CategoryId = 4,
            FightType = FightType.Extreme
        };

        var duty2 = new LodestoneDuty
        {
            Name = "Test Duty",
            LodestoneId = "cache-test", // Same ID
            ExpansionId = 5,
            CategoryId = 4,
            FightType = FightType.Extreme
        };

        var dutyHtml = CreateDutyPageHtml(
            "Test Duty",
            "Test description",
            "https://example.com/test.png",
            "Test Boss"
        );

        var url = "https://na.finalfantasyxiv.com/lodestone/playguide/db/duty/cache-test/";
        var callCount = 0;

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == url),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(() =>
            {
                callCount++;
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(dutyHtml)
                };
            });

        // Act - First call
        await _service.PopulateDutyMetadataAsync(duty1);

        // Act - Second call with same URL (should use cache)
        await _service.PopulateDutyMetadataAsync(duty2);

        // Assert - Should only have called HTTP once
        callCount.ShouldBe(1);
        duty1.BossNames.Count.ShouldBe(1);
        duty2.BossNames.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ClearCache_ClearedBetweenCalls_MakesNewRequest()
    {
        // Arrange
        var duty1 = new LodestoneDuty
        {
            Name = "Test Duty",
            LodestoneId = "cache-clear-test",
            ExpansionId = 5,
            CategoryId = 4,
            FightType = FightType.Extreme
        };

        var duty2 = new LodestoneDuty
        {
            Name = "Test Duty",
            LodestoneId = "cache-clear-test", // Same ID
            ExpansionId = 5,
            CategoryId = 4,
            FightType = FightType.Extreme
        };

        var dutyHtml = CreateDutyPageHtml(
            "Test Duty",
            "Test description",
            "https://example.com/test.png",
            "Test Boss"
        );

        var url = "https://na.finalfantasyxiv.com/lodestone/playguide/db/duty/cache-clear-test/";
        var callCount = 0;

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == url),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(() =>
            {
                callCount++;
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(dutyHtml)
                };
            });

        // Act - First call
        await _service.PopulateDutyMetadataAsync(duty1);

        // Clear cache
        _service.ClearCache();

        // Act - Second call after cache clear
        await _service.PopulateDutyMetadataAsync(duty2);

        // Assert - Should have called HTTP twice (cache was cleared)
        callCount.ShouldBe(2);
    }

    #endregion
}