using Discord.Interactions;

namespace ExcelBotCs.Services.Lottery.Enums;

public enum RandomGuessType
{
    [ChoiceDisplay("Use any random number from 1-99")] Any,
    [ChoiceDisplay("Use only numbers that have not been guessed")] UnusedOnly,
    [ChoiceDisplay("Use only numbers that have been guessed")] UsedOnly,
}