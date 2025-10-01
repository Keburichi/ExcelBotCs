using Discord;

namespace ExcelBotCs.Models;

public class Announcement
{
    public string Content { get; set; }
    public string Author { get; set; }
    public List<MessageAttachment> Attachments { get; set; }
    public DateTime Timestamp { get; set; }

    public Announcement()
    {
        
    }

    public Announcement(IMessage message)
    {
        Content = message.Content;
        Author = message.Author.Username;
        Timestamp = message.Timestamp.UtcDateTime;
        Attachments = message.Attachments.Select(a => new MessageAttachment(a)).ToList();
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