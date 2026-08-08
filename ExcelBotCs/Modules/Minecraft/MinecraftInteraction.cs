using Discord.Interactions;
using Discord.WebSocket;
using ExcelBotCs.Services.API.Interfaces;

namespace ExcelBotCs.Modules.Minecraft;

public class MinecraftInteraction : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IMemberService _memberService;

    public MinecraftInteraction(IMemberService memberService)
    {
        _memberService = memberService;
    }

    [SlashCommand("minecraft", "Set or update your whitelisted Minecraft username")]
    public async Task SetMinecraftUsername(
        [Summary("username", "Your exact Minecraft username")]
        string username)
    {
        await DeferAsync(true);

        var member = await _memberService.GetByDiscordId(Context.User.Id);
        if (member is null)
        {
            await FollowupAsync("Member not found for the current user.", ephemeral: true);
            return;
        }

        var (success, message) = await _memberService.SetMinecraftUsernameAsync(member.Id, username);
        await FollowupAsync(message, ephemeral: true);
    }
}
