using ExcelBotCs.Models.Database.Events;
using ExcelBotCs.Modules.TeamFormation;

namespace ExcelBotCs.TestFramework.Extensions;

public static class EventExtensions
{
    public static List<SignupButtonConfig> WithRoleButtons(this List<SignupButtonConfig> @event)
    {
        return new List<SignupButtonConfig>
        {
            new() { MappedRole = Role.Tank, Slug = "tank", Label = "Tank" },
            new() { MappedRole = Role.Healer, Slug = "healer", Label = "Healer" },
            new() { MappedRole = Role.Melee, Slug = "melee", Label = "Melee" },
            new() { MappedRole = Role.Ranged, Slug = "ranged", Label = "Ranged" },
            new() { MappedRole = Role.Caster, Slug = "caster", Label = "Caster" }
        };
    }

    public static List<SignupButtonConfig> WithEmoteRoleButtons(this List<SignupButtonConfig> @event)
    {
        return new List<SignupButtonConfig>
        {
            new() { MappedRole = Role.Tank, Slug = "tank", Label = "Tank", EmojiId = "1234567890" },
            new() { MappedRole = Role.Healer, Slug = "healer", Label = "Healer", EmojiId = "1234567890" },
            new() { MappedRole = Role.Melee, Slug = "melee", Label = "Melee", EmojiId = "1234567890" },
            new() { MappedRole = Role.Ranged, Slug = "ranged", Label = "Ranged", EmojiId = "1234567890" },
            new() { MappedRole = Role.Caster, Slug = "caster", Label = "Caster", EmojiId = "1234567890" }
        };
    }

    public static List<SignupButtonConfig> WithRoleAndHelperButtons(this List<SignupButtonConfig> @event)
    {
        return new List<SignupButtonConfig>
        {
            new() { MappedRole = Role.Tank, Slug = "tank", Label = "Tank" },
            new() { MappedRole = Role.Healer, Slug = "healer", Label = "Healer" },
            new() { MappedRole = Role.Melee, Slug = "melee", Label = "Melee" },
            new() { MappedRole = Role.Ranged, Slug = "ranged", Label = "Ranged" },
            new() { MappedRole = Role.Caster, Slug = "caster", Label = "Caster" },
            new() { MappedRole = null, Slug = "helper", Label = "Helper", IsHelper = true }
        };
    }
}