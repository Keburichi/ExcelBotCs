using Discord;
using Discord.WebSocket;

namespace ExcelBotCs.Models;

public class MentionData
{
    public Dictionary<string, string> Users { get; set; } = new();
    public Dictionary<string, string> Roles { get; set; } = new();
    public Dictionary<string, string> Channels { get; set; } = new();
}

public class Announcement
{
    public string Content { get; set; }
    public string Author { get; set; }
    public string? AuthorAvatarUrl { get; set; }
    public List<MessageAttachment> Attachments { get; set; }
    public DateTime Timestamp { get; set; }
    public MentionData Mentions { get; set; } = new();

    public Announcement()
    {
    }

    public Announcement(IMessage message)
    {
        Content = message.Content;

        // instead of the discord username, use the display name on the server
        var user = message.Author as SocketGuildUser;
        Author = user.DisplayName;
        AuthorAvatarUrl = (user as IUser).GetDisplayAvatarUrl(ImageFormat.WebP);
        Timestamp = message.Timestamp.UtcDateTime;
        Attachments = message.Attachments.Select(a => new MessageAttachment(a)).ToList();

        if (message is IUserMessage userMessage)
            foreach (var tag in userMessage.Tags)
                switch (tag.Type)
                {
                    case TagType.UserMention when tag.Value is IUser mentionedUser:
                    {
                        var name = mentionedUser is SocketGuildUser guildUser
                            ? guildUser.DisplayName
                            : mentionedUser.GlobalName ?? mentionedUser.Username;
                        Mentions.Users[tag.Key.ToString()] = name;
                        break;
                    }
                    case TagType.RoleMention when tag.Value is IRole role:
                        Mentions.Roles[tag.Key.ToString()] = role.Name;
                        break;
                    case TagType.ChannelMention when tag.Value is IChannel channel:
                        Mentions.Channels[tag.Key.ToString()] = channel.Name;
                        break;
                }
    }
}

public class MessageAttachment
{
    public string Name { get; set; }
    public string Url { get; set; }

    public MessageAttachment()
    {
    }

    public MessageAttachment(IAttachment attachment)
    {
        Name = attachment.Filename;
        Url = attachment.Url;
    }
}