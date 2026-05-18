using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ExcelBotCs.Discord;
using ExcelBotCs.Extensions;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Services.Lottery;
using ExcelBotCs.Services.Lottery.Enums;
using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.Services.Lottery.Records;
using Microsoft.Extensions.Options;

namespace ExcelBotCs.Modules.Lottery;

[Group("lottery", "Lottery commands")]
public class LotteryInteraction : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ILotteryService _lotteryService;
    private readonly IDiscordBotClient _discordClient;
    private readonly DiscordBotOptions _discordBotOptions;

    public LotteryInteraction(ILotteryService lotteryService, IDiscordBotClient discordClient,
        IOptions<DiscordBotOptions> discordBotOptions)
    {
        _lotteryService = lotteryService;
        _discordClient = discordClient;
        _discordBotOptions = discordBotOptions.Value;
    }

    [SlashCommand("guess", "Pick a number and have a chance to win!")]
    public async Task Guess(int number)
    {
        var guessResult = await _lotteryService.GuessAsync(Context.GuildUser().Id, number);
        await RespondAsync(LotteryResponseFormatter.FormatGuessResponse(guessResult), ephemeral: true);
    }

    [SlashCommand("unused", "Check what numbers have not yet been used.")]
    public async Task UnusedNumbers()
    {
        var unusedNumbers = await _lotteryService.GetUnusedNumbersAsync();
        await RespondAsync($"```{LotteryResponseFormatter.FormatUnusedNumbersResponse(unusedNumbers)}```",
            ephemeral: true);
    }


    [SlashCommand("luckydip", "Spin the wheel and maybe you'll win!")]
    public async Task RandomGuess(
        [Summary("number-pool", "Choose a set of numbers to use")]
        RandomGuessType numberPool =
            RandomGuessType.UnusedOnly)
    {
        await DeferAsync(true);

        var cts = new CancellationTokenSource();
        var result = await _lotteryService.RandomGuessAsync(Context.User.Id, cts, numberPool);
        await FollowupAsync(LotteryResponseFormatter.FormatGuessResponse(result), ephemeral: true);
    }

    [SlashCommand("change", "Change one of your current guesses")]
    public async Task Change(int old, int @new)
    {
        var result = await _lotteryService.ChangeGuessAsync(Context.GuildUser().Id, old, @new);
        await RespondAsync(LotteryResponseFormatter.FormatChangeGuessResponse(result, old, @new), ephemeral: true);
    }

    [SlashCommand("whoguessed", "Check who has guessed a certain number.")]
    public async Task WhoGuessed(int number)
    {
        var result = await _lotteryService.WhoGuessedAsync(number);
        await RespondAsync(LotteryResponseFormatter.FormatWhoGuessedResponse(result), ephemeral: true);
    }

    [SlashCommand("view", "Check your current guesses and see how many guesses you have left.")]
    public async Task View()
    {
        var result = await _lotteryService.ViewAsync(Context.GuildUser().Id);
        await RespondAsync(LotteryResponseFormatter.FormatViewResponse(result), ephemeral: true);
    }

    [SlashCommand("run", "Runs the lottery.")]
    public async Task Run()
    {
        if (!Context.GuildUser().IsOfficer(_discordBotOptions))
        {
            await RespondAsync("Only officers can use this command!", ephemeral: true);
            return;
        }

        await RespondAsync("Lottery running...", ephemeral: true);

        await _lotteryService.RunLotteryAsync(Context.GuildUser().Id);
    }

    [SlashCommand("remind", "Reminds users to use any remaining guesses")]
    public async Task Remind()
    {
        if (!Context.GuildUser().IsOfficer(_discordBotOptions))
        {
            await RespondAsync("Only officers can use this command!", ephemeral: true);
            return;
        }

        await DeferAsync(true);

        await _lotteryService.RemindAsync(Context.GuildUser().Id);
        await FollowupAsync("Reminders sent!", ephemeral: true);
    }

    [SlashCommand("award", "Grants extra guesses for the current lottery period")]
    public async Task Award(string reason, string? postUrl = null)
    {
        if (!Context.GuildUser().IsOfficer(_discordBotOptions))
        {
            await RespondAsync("Only officers can use this command!", ephemeral: true);
            return;
        }

        if (postUrl == null)
            await AwardByUi(reason);
        else
            await AwardByContents(reason, postUrl);
    }

    private async Task AwardByUi(string reason)
    {
        var awardSelection = new SelectMenuBuilder()
            .WithPlaceholder("Pick users")
            .WithCustomId($"award_selection:{reason}")
            .WithType(ComponentType.UserSelect)
            .WithMinValues(1)
            .WithMaxValues(24);

        var builder = new ComponentBuilder()
            .WithSelectMenu(awardSelection);

        await RespondAsync("Who should be awarded an extra guess for this lottery period?",
            components: builder.Build(), ephemeral: true);
    }

    private async Task AwardByContents(string reason, string postUrl)
    {
        await DeferAsync(true);

        switch (await _discordClient.GetMessageFromUrl(postUrl))
        {
            case NotValidUrlMessageResponse:
                await FollowupAsync("The provided URL does not seem to be a valid Discord URL", ephemeral: true);
                break;

            case NotFoundUrlMessageResponse:
                await FollowupAsync(
                    "Could not find the Guild/Channel this message belongs to. Do I have permission to view it?",
                    ephemeral: true);
                break;

            case SuccessMessageResponse msg:
            {
                var result = await _lotteryService.TryAwardUsersAsync(reason, msg.Message.MentionedUserIds.ToList());

                if (result is NoUsersAwardResponse)
                {
                    await FollowupAsync("No mentioned users could be found in the message.", ephemeral: true);
                    return;
                }

                if (result is not SuccessAwardResponse success)
                {
                    await FollowupAsync("Something went wrong. Tell Zahrymm.", ephemeral: true);
                    return;
                }

                await _lotteryService.AwardUsersAsync(success);
                await FollowupAsync($"An extra lottery guess have been granted to: {success.PrettyUsersAwarded}",
                    ephemeral: true);
                break;
            }
        }
    }


    [ComponentInteraction("award_selection:*", true)]
    public async Task HandleAward(string reason, IUser[] users)
    {
        if (Context.Interaction is not SocketMessageComponent socketMsg)
        {
            await RespondAsync("Something went wrong. Tell Zahrymm.");
            return;
        }

        await DeferAsync(true);

        var result = await _lotteryService.TryAwardUsersAsync(reason, users.Select(user => user.Id).ToList());

        if (result is NoUsersAwardResponse)
        {
            await FollowupAsync("You did not pick any users.", ephemeral: true);
            return;
        }

        if (result is not SuccessAwardResponse success)
        {
            await FollowupAsync("Something went wrong. Tell Zahrymm.", ephemeral: true);
            return;
        }

        await _lotteryService.AwardUsersAsync(success);
        await socketMsg.UpdateAsync(msg =>
        {
            msg.Components = null;
            msg.Content = $"An extra lottery guess have been granted to: {success.PrettyUsersAwarded}";
        });
    }
}