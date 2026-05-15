using System.Text;
using Discord;
using Discord.WebSocket;
using ExcelBotCs.Extensions;
using ExcelBotCs.Models.Database.Events;
using ExcelBotCs.Modules.TeamFormation;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.Services.Discord.Interfaces;

namespace ExcelBotCs.Services.Discord;

public class DiscordMessageCreator : IDiscordMessageCreator
{
    private readonly DiscordSocketClient _discordSocketClient;
    private readonly IFightService _fightService;

    public DiscordMessageCreator(DiscordSocketClient client, IFightService fightService)
    {
        _discordSocketClient = client;
        _fightService = fightService;
    }

    public async Task<ComponentBuilderV2> CreateSignupComponents(Event fcEvent)
    {
        var buttons = new List<ButtonBuilder>();

        var tankEmote = _discordSocketClient.GetTankEmote();
        var healEmote = _discordSocketClient.GetHealerEmote();
        var meleeEmote = _discordSocketClient.GetMeleeEmote();
        var rangeEmote = _discordSocketClient.GetRangedEmote();
        var casterEmote = _discordSocketClient.GetCasterEmote();

        var tankButton = new ButtonBuilder("Tank", $"{fcEvent.Id}-signup-tank");

        if (tankEmote != null)
            tankButton.WithEmote(tankEmote);

        var healerButton = new ButtonBuilder("Healer", $"{fcEvent.Id}-signup-healer");

        if (healEmote != null)
            healerButton.WithEmote(healEmote);

        var meleeButton = new ButtonBuilder("Melee", $"{fcEvent.Id}-signup-melee");

        if (meleeEmote != null)
            meleeButton.WithEmote(meleeEmote);

        var rangeButton = new ButtonBuilder("Range", $"{fcEvent.Id}-signup-ranged");

        if (rangeEmote != null)
            rangeButton.WithEmote(rangeEmote);

        var casterButton = new ButtonBuilder("Caster", $"{fcEvent.Id}-signup-caster");

        if (casterEmote != null)
            casterButton.WithEmote(casterEmote);

        buttons.Add(tankButton);
        buttons.Add(healerButton);
        buttons.Add(meleeButton);
        buttons.Add(rangeButton);
        buttons.Add(casterButton);

        var componentBuilderV2 = new ComponentBuilderV2();

        componentBuilderV2.WithTextDisplay(new TextDisplayBuilder($"# {fcEvent.Name}"));

        var subHeading = fcEvent.FightId.IsNullOrEmpty()
            ? $"## {fcEvent.Type}"
            : $"## {fcEvent.Type} - {await _fightService.GetAsync(fcEvent.FightId)}";
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
            componentBuilderV2.WithActionRow(buttons);
        }

        componentBuilderV2.WithSeparator(SeparatorSpacingSize.Large);
        componentBuilderV2.WithTextDisplay("**Current Signups**");

        foreach (Role role in Enum.GetValues(typeof(Role)))
        {
            var signUps = (fcEvent.Signups ?? Enumerable.Empty<EventSignup>())
                .Where(x => x.Roles.Contains(role));
            var members = signUps.Aggregate<EventSignup?, string>(null,
                (current, eventSignup) => current + $"<@{eventSignup.DiscordUserId}>, ");
            componentBuilderV2.WithTextDisplay($"{_discordSocketClient.GetEmoteByRole(role)}: {members}");
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