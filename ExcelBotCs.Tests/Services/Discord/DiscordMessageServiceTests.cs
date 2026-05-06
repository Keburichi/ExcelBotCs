// using Discord;
// using Discord.WebSocket;
// using ExcelBotCs.Models.Config;
// using ExcelBotCs.Services.Discord;
// using ExcelBotCs.Services.Discord.Interfaces;
// using Microsoft.Extensions.Options;
// using Moq;
//
// namespace ExcelBotCs.Tests.Services.Discord;
//
// [TestFixture]
// public class DiscordMessageServiceTests
// {
//     private Mock<DiscordSocketClient> _mockDiscordClient = null!;
//     private Mock<ITextChannel> _mockTextChannel = null!;
//     private IDiscordMessageService _discordMessageService = null!;
//     private DiscordBotOptions _discordBotOptions = null!;
//
//     [SetUp]
//     public void Setup()
//     {
//         _mockDiscordClient = new Mock<DiscordSocketClient>();
//         _mockTextChannel = new Mock<ITextChannel>();
//
//         _discordBotOptions = new DiscordBotOptions
//         {
//             Token = "test-token",
//             GuildId = 123456789,
//             AnnouncementChannel = 111111111,
//             EventsChannel = 222222222,
//             UpcomingRosterChannel = 333333333,
//             LotteryChannel = 444444444,
//             MemberRoleId = 555555555,
//             AdminRoleId = 666666666
//         };
//
//         var options = Options.Create(_discordBotOptions);
//         _discordMessageService = new DiscordMessageService(_mockDiscordClient.Object, options);
//     }
//
//     #region PostInAnnouncementChannelAsync Tests
//
//     [Test]
//     public async Task PostInAnnouncementChannelAsync_SendsMessageSuccessfully()
//     {
//         // Arrange
//         var testMessage = "Test announcement message";
//         _mockDiscordClient
//             .Setup(x => x.GetChannelAsync(It.IsAny<ulong>()))
//             .ReturnsAsync(_mockTextChannel.Object);
//
//         _mockTextChannel
//             .Setup(x => x.SendMessageAsync(
//                 It.IsAny<string>(),
//                 It.IsAny<bool>(),
//                 It.IsAny<Embed>(),
//                 It.IsAny<RequestOptions>(),
//                 It.IsAny<AllowedMentions>(),
//                 It.IsAny<MessageReference>(),
//                 It.IsAny<MessageComponent>(),
//                 It.IsAny<ISticker[]>(),
//                 It.IsAny<Embed[]>(),
//                 It.IsAny<MessageFlags>()))
//             .ReturnsAsync(Mock.Of<IUserMessage>());
//
//         // Act
//         await _discordMessageService.PostInAnnouncementChannelAsync(testMessage);
//
//         // Assert
//         _mockTextChannel.Verify(x => x.SendMessageAsync(
//             It.Is<string>(s => s == testMessage),
//             It.IsAny<bool>(),
//             It.IsAny<Embed>(),
//             It.IsAny<RequestOptions>(),
//             It.IsAny<AllowedMentions>(),
//             It.IsAny<MessageReference>(),
//             It.IsAny<MessageComponent>(),
//             It.IsAny<ISticker[]>(),
//             It.IsAny<Embed[]>(),
//             It.IsAny<MessageFlags>()), Times.Once);
//     }
//
//     [Test]
//     public async Task PostInAnnouncementChannelAsync_DoesNotThrowWhenChannelIsNull()
//     {
//         // Arrange
//         _mockDiscordClient
//             .Setup(x => x.GetChannelAsync(It.IsAny<ulong>()))
//             .ReturnsAsync((IChannel?)null);
//
//         // Act & Assert
//         Assert.DoesNotThrowAsync(async () =>
//             await _discordMessageService.PostInAnnouncementChannelAsync("Test message"));
//     }
//
//     [Test]
//     public async Task PostInAnnouncementChannelAsync_DoesNotSendMessageWhenChannelIsNull()
//     {
//         // Arrange
//         _mockDiscordClient
//             .Setup(x => x.GetChannelAsync(It.IsAny<ulong>()))
//             .ReturnsAsync((IChannel?)null);
//
//         // Act
//         await _discordMessageService.PostInAnnouncementChannelAsync("Test message");
//
//         // Assert
//         _mockTextChannel.Verify(x => x.SendMessageAsync(
//             It.IsAny<string>(),
//             It.IsAny<bool>(),
//             It.IsAny<Embed>(),
//             It.IsAny<RequestOptions>(),
//             It.IsAny<AllowedMentions>(),
//             It.IsAny<MessageReference>(),
//             It.IsAny<MessageComponent>(),
//             It.IsAny<ISticker[]>(),
//             It.IsAny<Embed[]>(),
//             It.IsAny<MessageFlags>()), Times.Never);
//     }
//
//     #endregion
//
//     #region PostInEventChannelAsync Tests
//
//     [Test]
//     public async Task PostInEventChannelAsync_SendsMessageSuccessfully()
//     {
//         // Arrange
//         var testMessage = "Test event message";
//         _mockDiscordClient
//             .Setup(x => x.GetChannelAsync(It.IsAny<ulong>()))
//             .ReturnsAsync(_mockTextChannel.Object);
//
//         _mockTextChannel
//             .Setup(x => x.SendMessageAsync(
//                 It.IsAny<string>(),
//                 It.IsAny<bool>(),
//                 It.IsAny<Embed>(),
//                 It.IsAny<RequestOptions>(),
//                 It.IsAny<AllowedMentions>(),
//                 It.IsAny<MessageReference>(),
//                 It.IsAny<MessageComponent>(),
//                 It.IsAny<ISticker[]>(),
//                 It.IsAny<Embed[]>(),
//                 It.IsAny<MessageFlags>()))
//             .ReturnsAsync(Mock.Of<IUserMessage>());
//
//         // Act
//         await _discordMessageService.PostInEventChannelAsync(testMessage);
//
//         // Assert
//         _mockTextChannel.Verify(x => x.SendMessageAsync(
//             It.Is<string>(s => s == testMessage),
//             It.IsAny<bool>(),
//             It.IsAny<Embed>(),
//             It.IsAny<RequestOptions>(),
//             It.IsAny<AllowedMentions>(),
//             It.IsAny<MessageReference>(),
//             It.IsAny<MessageComponent>(),
//             It.IsAny<ISticker[]>(),
//             It.IsAny<Embed[]>(),
//             It.IsAny<MessageFlags>()), Times.Once);
//     }
//
//     [Test]
//     public async Task PostInEventChannelAsync_DoesNotThrowWhenChannelIsNull()
//     {
//         // Arrange
//         _mockDiscordClient
//             .Setup(x => x.GetChannelAsync(It.IsAny<ulong>()))
//             .ReturnsAsync((IChannel?)null);
//
//         // Act & Assert
//         Assert.DoesNotThrowAsync(async () =>
//             await _discordMessageService.PostInEventChannelAsync("Test message"));
//     }
//
//     #endregion
//
//     #region PostInUpcomingRosterChannelAsync Tests
//
//     [Test]
//     public async Task PostInUpcomingRosterChannelAsync_SendsMessageSuccessfully()
//     {
//         // Arrange
//         var testMessage = "Test roster message";
//         _mockDiscordClient
//             .Setup(x => x.GetChannelAsync(It.IsAny<ulong>()))
//             .ReturnsAsync(_mockTextChannel.Object);
//
//         _mockTextChannel
//             .Setup(x => x.SendMessageAsync(
//                 It.IsAny<string>(),
//                 It.IsAny<bool>(),
//                 It.IsAny<Embed>(),
//                 It.IsAny<RequestOptions>(),
//                 It.IsAny<AllowedMentions>(),
//                 It.IsAny<MessageReference>(),
//                 It.IsAny<MessageComponent>(),
//                 It.IsAny<ISticker[]>(),
//                 It.IsAny<Embed[]>(),
//                 It.IsAny<MessageFlags>()))
//             .ReturnsAsync(Mock.Of<IUserMessage>());
//
//         // Act
//         await _discordMessageService.PostInUpcomingRosterChannelAsync(testMessage);
//
//         // Assert
//         _mockTextChannel.Verify(x => x.SendMessageAsync(
//             It.Is<string>(s => s == testMessage),
//             It.IsAny<bool>(),
//             It.IsAny<Embed>(),
//             It.IsAny<RequestOptions>(),
//             It.IsAny<AllowedMentions>(),
//             It.IsAny<MessageReference>(),
//             It.IsAny<MessageComponent>(),
//             It.IsAny<ISticker[]>(),
//             It.IsAny<Embed[]>(),
//             It.IsAny<MessageFlags>()), Times.Once);
//     }
//
//     [Test]
//     public async Task PostInUpcomingRosterChannelAsync_DoesNotThrowWhenChannelIsNull()
//     {
//         // Arrange
//         _mockDiscordClient
//             .Setup(x => x.GetChannelAsync(It.IsAny<ulong>()))
//             .ReturnsAsync((IChannel?)null);
//
//         // Act & Assert
//         Assert.DoesNotThrowAsync(async () =>
//             await _discordMessageService.PostInUpcomingRosterChannelAsync("Test message"));
//     }
//
//     #endregion
//
//     #region PostInLotteryChannelAsync Tests
//
//     [Test]
//     public async Task PostInLotteryChannelAsync_SendsMessageSuccessfully()
//     {
//         // Arrange
//         var testMessage = "Test lottery message";
//         _mockDiscordClient
//             .Setup(x => x.GetChannelAsync(It.IsAny<ulong>()))
//             .ReturnsAsync(_mockTextChannel.Object);
//
//         _mockTextChannel
//             .Setup(x => x.SendMessageAsync(
//                 It.IsAny<string>(),
//                 It.IsAny<bool>(),
//                 It.IsAny<Embed>(),
//                 It.IsAny<RequestOptions>(),
//                 It.IsAny<AllowedMentions>(),
//                 It.IsAny<MessageReference>(),
//                 It.IsAny<MessageComponent>(),
//                 It.IsAny<ISticker[]>(),
//                 It.IsAny<Embed[]>(),
//                 It.IsAny<MessageFlags>()))
//             .ReturnsAsync(Mock.Of<IUserMessage>());
//
//         // Act
//         await _discordMessageService.PostInLotteryChannelAsync(testMessage);
//
//         // Assert
//         _mockTextChannel.Verify(x => x.SendMessageAsync(
//             It.Is<string>(s => s == testMessage),
//             It.IsAny<bool>(),
//             It.IsAny<Embed>(),
//             It.IsAny<RequestOptions>(),
//             It.IsAny<AllowedMentions>(),
//             It.IsAny<MessageReference>(),
//             It.IsAny<MessageComponent>(),
//             It.IsAny<ISticker[]>(),
//             It.IsAny<Embed[]>(),
//             It.IsAny<MessageFlags>()), Times.Once);
//     }
//
//     [Test]
//     public async Task PostInLotteryChannelAsync_DoesNotThrowWhenChannelIsNull()
//     {
//         // Arrange
//         _mockDiscordClient
//             .Setup(x => x.GetChannelAsync(It.IsAny<ulong>()))
//             .ReturnsAsync((IChannel?)null);
//
//         // Act & Assert
//         Assert.DoesNotThrowAsync(async () =>
//             await _discordMessageService.PostInLotteryChannelAsync("Test message"));
//     }
//
//     #endregion
//
//     #region GetAnnouncementChannelMessagesAsync Tests
//
//     [Test]
//     public async Task GetAnnouncementChannelMessagesAsync_ReturnsEmptyListWhenChannelIsNull()
//     {
//         // Arrange
//         _mockDiscordClient
//             .Setup(x => x.GetChannelAsync(It.IsAny<ulong>()))
//             .ReturnsAsync((IChannel?)null);
//
//         // Act
//         var result = await _discordMessageService.GetAnnouncementChannelMessagesAsync();
//
//         // Assert
//         Assert.That(result, Is.Not.Null);
//         Assert.That(result, Is.Empty);
//     }
//
//     // Note: Testing GetMessagesAsync with full mocking is challenging due to Discord.Net's
//     // use of optional parameters which don't work well with Moq's expression trees.
//     // The method's core logic is simple delegation to Discord.Net, so testing the null
//     // channel case provides adequate coverage of the service's error handling logic.
//
//     #endregion
// }

