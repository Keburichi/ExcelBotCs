using System.Net;
using System.Net.Http.Json;
using Discord;
using ExcelBotCs.Models;
using ExcelBotCs.Services.Discord.Interfaces;
using ExcelBotCs.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace ExcelBotCs.Tests.Controllers;

[TestFixture]
public class HomeControllerIntegrationTests : IntegrationTestBase
{
    #region Permission Tests

    [Test]
    public async Task GetAnnouncements_CheckPermissions()
    {
        // No Auth = Unauthorized
        SetUnauthenticated();
        var response = await Client.GetAsync("api/Home/announcements");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        // No Member = Forbidden
        SetAuthenticatedUser("12355");
        response = await Client.GetAsync("api/Home/announcements");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));

        // Member = Ok
        await AuthenticateAsMember();
        response = await Client.GetAsync("api/Home/announcements");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Admin = Ok
        await AuthenticateAsAdmin();
        response = await Client.GetAsync("api/Home/announcements");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    #endregion

    #region Functional Tests

    [Test]
    public async Task GetAnnouncements_NoMessages_ReturnsEmptyList()
    {
        // Arrange
        await AuthenticateAsMember();

        // The default mock returns an empty list, so no additional setup needed

        // Act
        var response = await Client.GetAsync("api/Home/announcements");

        // Assert
        response.EnsureSuccessStatusCode();
        var announcements = await response.Content.ReadFromJsonAsync<List<Announcement>>();
        Assert.That(announcements, Is.Not.Null);
        Assert.That(announcements, Is.Empty);
    }

    [Test]
    public async Task GetAnnouncements_WithMessages_ReturnsAnnouncementList()
    {
        // Arrange
        // Create mock Discord messages
        var mockMessages = new List<IMessage>
        {
            CreateMockDiscordMessage(
                123456789,
                "Test announcement 1",
                "TestUser1",
                DateTimeOffset.UtcNow.AddDays(-1),
                new List<IAttachment>()
            ),
            CreateMockDiscordMessage(
                987654321,
                "Test announcement 2",
                "TestUser2",
                DateTimeOffset.UtcNow.AddHours(-2),
                new List<IAttachment>
                {
                    CreateMockAttachment("image1.png", "https://example.com/image1.png")
                }
            )
        };

        // Configure the mock to return our test messages
        var mockDiscordService = new Mock<IDiscordMessageService>();
        mockDiscordService.Setup(x => x.GetAnnouncementChannelMessagesAsync())
            .ReturnsAsync(mockMessages);

        // Create a new factory with the updated services
        using var customFactory = Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDiscordMessageService>();
                services.AddSingleton(mockDiscordService.Object);
            });
        });

        using var customClient = customFactory.CreateClient();

        // Authenticate after creating the custom client
        var member = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());

        // Act
        var response = await customClient.GetAsync("api/Home/announcements");

        // Assert
        response.EnsureSuccessStatusCode();
        var announcements = await response.Content.ReadFromJsonAsync<List<Announcement>>();
        Assert.That(announcements, Is.Not.Null);
        Assert.That(announcements, Has.Count.EqualTo(2));

        // Verify first announcement
        var announcement1 = announcements[0];
        Assert.That(announcement1.Content, Is.EqualTo("Test announcement 1"));
        Assert.That(announcement1.Author, Is.EqualTo("TestUser1"));
        Assert.That(announcement1.Attachments, Is.Empty);

        // Verify second announcement
        var announcement2 = announcements[1];
        Assert.That(announcement2.Content, Is.EqualTo("Test announcement 2"));
        Assert.That(announcement2.Author, Is.EqualTo("TestUser2"));
        Assert.That(announcement2.Attachments, Has.Count.EqualTo(1));
        Assert.That(announcement2.Attachments[0].Name, Is.EqualTo("image1.png"));
        Assert.That(announcement2.Attachments[0].Url, Is.EqualTo("https://example.com/image1.png"));
    }

    [Test]
    public async Task GetAnnouncements_WithMessagesWithMultipleAttachments_ReturnsCorrectData()
    {
        // Arrange
        var mockMessages = new List<IMessage>
        {
            CreateMockDiscordMessage(
                111222333,
                "Announcement with multiple images",
                "AdminUser",
                DateTimeOffset.UtcNow,
                new List<IAttachment>
                {
                    CreateMockAttachment("screenshot1.png", "https://example.com/screenshot1.png"),
                    CreateMockAttachment("screenshot2.png", "https://example.com/screenshot2.png"),
                    CreateMockAttachment("document.pdf", "https://example.com/document.pdf")
                }
            )
        };

        var mockDiscordService = new Mock<IDiscordMessageService>();
        mockDiscordService.Setup(x => x.GetAnnouncementChannelMessagesAsync())
            .ReturnsAsync(mockMessages);

        using var customFactory = Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDiscordMessageService>();
                services.AddSingleton(mockDiscordService.Object);
            });
        });

        using var customClient = customFactory.CreateClient();

        // Authenticate after creating the custom client
        await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());

        // Act
        var response = await customClient.GetAsync("api/Home/announcements");

        // Assert
        response.EnsureSuccessStatusCode();
        var announcements = await response.Content.ReadFromJsonAsync<List<Announcement>>();
        Assert.That(announcements, Is.Not.Null);
        Assert.That(announcements, Has.Count.EqualTo(1));

        var announcement = announcements[0];
        Assert.That(announcement.Attachments, Has.Count.EqualTo(3));
        Assert.That(announcement.Attachments[0].Name, Is.EqualTo("screenshot1.png"));
        Assert.That(announcement.Attachments[1].Name, Is.EqualTo("screenshot2.png"));
        Assert.That(announcement.Attachments[2].Name, Is.EqualTo("document.pdf"));
    }

    #endregion

    #region Helper Methods

    /// <summary>
    ///     Creates a mock Discord IMessage for testing purposes
    /// </summary>
    private static IMessage CreateMockDiscordMessage(
        ulong id,
        string content,
        string authorUsername,
        DateTimeOffset timestamp,
        IReadOnlyCollection<IAttachment> attachments)
    {
        var mockAuthor = new Mock<IGuildUser>();
        mockAuthor.Setup(x => x.Username).Returns(authorUsername);
        mockAuthor.Setup(x => x.DisplayName).Returns(authorUsername);
        mockAuthor.Setup(x => x.GetDisplayAvatarUrl(ImageFormat.WebP, 128)).Returns("https://example.com/image1.png");

        var mockMessage = new Mock<IMessage>();
        mockMessage.Setup(x => x.Id).Returns(id);
        mockMessage.Setup(x => x.Content).Returns(content);
        mockMessage.Setup(x => x.Author).Returns(mockAuthor.Object);
        mockMessage.Setup(x => x.Timestamp).Returns(timestamp);
        mockMessage.Setup(x => x.Attachments).Returns(attachments);

        return mockMessage.Object;
    }

    /// <summary>
    ///     Creates a mock Discord IAttachment for testing purposes
    /// </summary>
    private static IAttachment CreateMockAttachment(string filename, string url)
    {
        var mockAttachment = new Mock<IAttachment>();
        mockAttachment.Setup(x => x.Filename).Returns(filename);
        mockAttachment.Setup(x => x.Url).Returns(url);

        return mockAttachment.Object;
    }

    #endregion
}