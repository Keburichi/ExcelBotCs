namespace ExcelBotCs.Services.Minecraft;

public interface IMinecraftRconService
{
    Task<(bool Success, string Message)> WhitelistAddAsync(string username);
    Task<(bool Success, string Message)> WhitelistRemoveAsync(string username);
}
