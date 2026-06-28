using Discord;
using Discord.Interactions;
using ExcelBotCs.Services.API.Interfaces;

namespace ExcelBotCs.Modules.TeamFormation;

public class FightAutocompleteHandler : AutocompleteHandler
{
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context,
        IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
    {
        var fightService = services.GetRequiredService<IFightService>();
        var fights = await fightService.GetFightsAsync();

        var input = autocompleteInteraction.Data.Current.Value as string ?? string.Empty;

        var suggestions = fights
            .Where(fight => string.IsNullOrWhiteSpace(input) ||
                            fight.Name.Contains(input, StringComparison.OrdinalIgnoreCase))
            .Take(25)
            .Select(fight => new AutocompleteResult(fight.Name, fight.Id));

        return AutocompletionResult.FromSuccess(suggestions);
    }
}