using System.Text;
using Discord;
using ExcelBotCs.Discord;
using ExcelBotCs.Extensions;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.Database.Events;
using ExcelBotCs.Modules.TeamFormation;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.Services.Discord.Interfaces;

namespace ExcelBotCs.Services.Discord;

public class DiscordMessageCreator : IDiscordMessageCreator
{
    private readonly IDiscordBotClient _discordClient;
    private readonly IFightService _fightService;

    public DiscordMessageCreator(IDiscordBotClient discordClient, IFightService fightService)
    {
        _discordClient = discordClient;
        _fightService = fightService;
    }

    public async Task<ComponentBuilderV2> CreateSignupComponents(Event fcEvent)
    {
        ArgumentNullException.ThrowIfNull(fcEvent);

        if (fcEvent.SignupButtonConfigs is null)
            throw new ArgumentException("No signup button configs found for event");

        var componentBuilderV2 = new ComponentBuilderV2();

        componentBuilderV2.WithTextDisplay(new TextDisplayBuilder($"# {fcEvent.Name}"));

        Fight? fight = null;
        if (!string.IsNullOrWhiteSpace(fcEvent.FightId))
            fight = await _fightService.GetFightAsync(fcEvent.FightId);

        var subHeading = fight is null
            ? $"## {fcEvent.Type}"
            : $"## {fcEvent.Type} - {fight!.Name}";
        componentBuilderV2.WithTextDisplay(subHeading);

        if (!string.IsNullOrWhiteSpace(fcEvent.PictureUrl))
            componentBuilderV2.WithMediaGallery([fcEvent.PictureUrl]);

        componentBuilderV2.WithTextDisplay(
            $"**Date:** {fcEvent.StartDate.ToLongDiscordDateLongTime()} - {fcEvent.EndDate.ToLongDiscordDateLongTime()} ({fcEvent.StartDate.ToRelativeDiscordTime()})");

        if (fcEvent.Description != string.Empty)
            componentBuilderV2.WithTextDisplay($"{fcEvent.Description}");

        componentBuilderV2.WithSeparator();

        var signupsClosed = !fcEvent.AvailableForSignup || fcEvent.IsArchived;

        if (signupsClosed)
        {
            componentBuilderV2.WithTextDisplay("**Signups are closed for this event.**");
        }
        else
        {
            var buttons = new List<ButtonBuilder>();
            foreach (var config in fcEvent.SignupButtonConfigs)
            {
                var button = new ButtonBuilder(config.Label, $"{fcEvent.Id}-signup-{config.Slug}");
                if (config.EmojiId != null && ulong.TryParse(config.EmojiId, out var emojiId))
                {
                    var emote = _discordClient.GetEmoteById(emojiId);
                    if (emote != null)
                        button.WithEmote(emote);
                }

                buttons.Add(button);

                if (buttons.Count == 5)
                {
                    componentBuilderV2.WithActionRow(buttons);
                    buttons = new List<ButtonBuilder>();
                }
            }

            if (buttons.Count > 0)
                componentBuilderV2.WithActionRow(buttons);
        }

        componentBuilderV2.WithSeparator(SeparatorSpacingSize.Large);
        componentBuilderV2.WithTextDisplay("**Current Signups**");

        foreach (var config in fcEvent.SignupButtonConfigs)
        {
            var signUps = (fcEvent.Signups ?? Enumerable.Empty<EventSignup>())
                .Where(x => x.SignupSlugs != null && x.SignupSlugs.Contains(config.Slug));
            var members = signUps.Aggregate<EventSignup?, string>(null,
                (current, eventSignup) => current + $"<@{eventSignup.DiscordUserId}>, ");

            var emotePrefix = "";
            if (config.EmojiId != null && ulong.TryParse(config.EmojiId, out var emojiId))
            {
                var emote = _discordClient.GetEmoteById(emojiId);
                emotePrefix = emote != null ? $"{emote} " : "";
            }

            componentBuilderV2.WithTextDisplay($"{emotePrefix}**{config.Label}**: {members}");
        }

        return componentBuilderV2;
    }

    public async Task<string> CreateUpcomingRosterMessage(Event fcEvent)
    {
        if (fcEvent.Groups == null || fcEvent.Groups.Count == 0)
            throw new ArgumentException("No groups selected for event. Unable to post a roster");

        var messageBuilder = new StringBuilder();

        messageBuilder.AppendLine("# Upcoming roster for:");
        messageBuilder.AppendLine($"## {fcEvent.Type} - {fcEvent.Name}");
        // messageBuilder.AppendLine($"{_discordMessageService.GetEventSignupMessageUrl(fcEvent.DiscordMessageId)}");
        messageBuilder.AppendLine(
            $"**Date:** {fcEvent.StartDate.ToLongDiscordDateLongTime()} - {fcEvent.EndDate.ToLongDiscordDateLongTime()} ({fcEvent.StartDate.ToRelativeDiscordTime()})");
        messageBuilder.AppendLine();

        if (fcEvent.Groups.Count > 1)
        {
            foreach (var fcEventGroup in fcEvent.Groups)
            {
                var participants = fcEventGroup.Participants;

                messageBuilder.AppendLine($"{fcEventGroup.Name}:");
                AppendRoleMentions(messageBuilder, participants);
            }
        }
        else
        {
            var participants = fcEvent.Groups.First().Participants;
            AppendRoleMentions(messageBuilder, participants);
        }

        return messageBuilder.ToString();
    }

    private static void AppendRoleMentions(StringBuilder messageBuilder, List<EventParticipant> participants)
    {
        messageBuilder.AppendLine($"{Constants.TankRoleEmote} {RoleMentions(participants, Role.Tank)}");
        messageBuilder.AppendLine($"{Constants.HealerRoleEmote} {RoleMentions(participants, Role.Healer)}");
        messageBuilder.AppendLine($"{Constants.MeleeRoleEmote} {RoleMentions(participants, Role.Melee)}");
        messageBuilder.AppendLine($"{Constants.RangedRoleEmote} {RoleMentions(participants, Role.Ranged)}");
        messageBuilder.AppendLine($"{Constants.CasterRoleEmote} {RoleMentions(participants, Role.Caster)}");
    }

    private static string RoleMentions(List<EventParticipant> eventParticipants, Role role)
    {
        if (eventParticipants.IsNullOrEmpty())
            return string.Empty;

        var messageBuilder = new StringBuilder();

        var participants = eventParticipants.Where(p => p.Role == role).ToList();
        foreach (var participant in participants)
            messageBuilder.Append($"<@{participant.DiscordUserId}> ");

        return messageBuilder.ToString();
    }
}