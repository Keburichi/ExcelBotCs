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
    private readonly ILotteryReminderService _reminderService;
    private readonly IDiscordBotClient _discordClient;
    private readonly DiscordBotOptions _discordBotOptions;

    public LotteryInteraction(ILotteryService lotteryService, ILotteryReminderService reminderService,
        IDiscordBotClient discordClient, IOptions<DiscordBotOptions> discordBotOptions)
    {
        _lotteryService = lotteryService;
        _reminderService = reminderService;
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

        var result = await _lotteryService.RunLotteryAsync(Context.GuildUser().Id);
        await FollowupAsync(result.Message, ephemeral: true);
    }

    [SlashCommand("remind", "Reminds members with unused guesses who opted in")]
    public async Task Remind()
    {
        if (!Context.GuildUser().IsOfficer(_discordBotOptions))
        {
            await RespondAsync("Only officers can use this command!", ephemeral: true);
            return;
        }

        await DeferAsync(true);

        // The same opted-in audience and copy as the scheduled reminder; with nobody eligible nothing is posted and
        // the officer gets the reason ephemerally instead (spec §5.7).
        var outcome = await _reminderService.RemindNowAsync();
        await FollowupAsync(outcome.Message, ephemeral: true);
    }

    [SlashCommand("reminders", "Turn the pre-draw lottery reminder on or off")]
    public async Task Reminders(
        [Summary("state", "on or off")]
        [Choice("on", "on")]
        [Choice("off", "off")]
        string state)
    {
        var enabled = string.Equals(state, "on", StringComparison.OrdinalIgnoreCase);
        var preference = await _reminderService.SetPreferenceAsync(Context.User.Id, enabled);

        await RespondAsync(preference.Enabled
            ? "Lottery reminders are on. You will be pinged before the draw when you have guesses left."
            : "Lottery reminders are off. You can turn them back on from the lottery page.", ephemeral: true);
    }

    /// <summary>
    ///     The reminder's "Turn off reminders" button. It changes only the caller's opt-in, is idempotent, replies
    ///     ephemerally and never edits the post — so it also works for a member who has left the FC (spec §5.7).
    /// </summary>
    [ComponentInteraction(LotteryReminderComponents.OptOutCustomId)]
    public async Task TurnOffReminders()
    {
        await _reminderService.SetPreferenceAsync(Context.User.Id, false);
        await RespondAsync("Lottery reminders are off. You can turn them back on from the lottery page.",
            ephemeral: true);
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

                await _lotteryService.AwardUsersAsync(success, Context.GuildUser().Id);
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

        await _lotteryService.AwardUsersAsync(success, Context.GuildUser().Id);
        await socketMsg.UpdateAsync(msg =>
        {
            msg.Components = null;
            msg.Content = $"An extra lottery guess have been granted to: {success.PrettyUsersAwarded}";
        });
    }
}