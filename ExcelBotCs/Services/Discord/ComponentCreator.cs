using Discord;
using Discord.WebSocket;
using ExcelBotCs.Extensions;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Modules.TeamFormation;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.Services.Discord.Interfaces;

namespace ExcelBotCs.Services.Discord;

public class ComponentCreator : IComponentCreator
{
    private readonly DiscordSocketClient _discordSocketClient;
    private readonly IFightService _fightService;

    public ComponentCreator(DiscordSocketClient client, IFightService fightService)
    {
        _discordSocketClient = client;
        _fightService = fightService;
    }

    public async Task<ComponentBuilderV2> CreateSignupComponents(Event fcEvent, Fight? fight = null)
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

        var rangeButton = new ButtonBuilder("Range", $"{fcEvent.Id}-signup-range");

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

        if (fcEvent.PictureUrl != null)
            componentBuilderV2.WithMediaGallery([fcEvent.PictureUrl]);

        componentBuilderV2.WithTextDisplay(
            $"**Date:** {fcEvent.StartDate.ToLongDiscordDateLongTime()} - {fcEvent.EndDate.ToLongDiscordDateLongTime()} ({fcEvent.StartDate.ToRelativeDiscordTime()})");
        componentBuilderV2.WithTextDisplay($"{fcEvent.Description}");

        componentBuilderV2.WithSeparator();
        componentBuilderV2.WithActionRow(buttons);
        componentBuilderV2.WithSeparator(SeparatorSpacingSize.Large);
        componentBuilderV2.WithTextDisplay("**Current Signups**");

        foreach (Role role in Enum.GetValues(typeof(Role)))
        {
            var signUps = fcEvent.Occurrences.First().Signups.Where(x => x.Roles.Contains(role));
            var members = signUps.Aggregate<EventSignup?, string>(null,
                (current, eventSignup) => current + $"<@{eventSignup.DiscordUserId}>, ");

            componentBuilderV2.WithTextDisplay($"{_discordSocketClient.GetEmoteByRole(role)}: {members}");
        }

        return componentBuilderV2;
    }
}