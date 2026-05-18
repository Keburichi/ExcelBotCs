using Discord;

namespace ExcelBotCs.Discord;

public enum MessageResponse
{
    NotValidUrl,
    NotFoundUrl,
    Success
}

public interface IMessageResponse
{
}

public record NotValidUrlMessageResponse : IMessageResponse;

public record NotFoundUrlMessageResponse : IMessageResponse;

public record SuccessMessageResponse(IMessage Message) : IMessageResponse;