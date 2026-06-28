using Discord.Interactions;

namespace ExcelBotCs.Modules.TeamFormation;

public enum PartySize
{
    [ChoiceDisplay("Light (4)")] Light,
    [ChoiceDisplay("Full (8)")] Full,
    [ChoiceDisplay("Alliance (24)")] Alliance,
    [ChoiceDisplay("Any")] Any
}

public enum SignupPreset
{
    [ChoiceDisplay("Roles")] Roles,
    [ChoiceDisplay("Roles + Helper")] RolesHelper
}