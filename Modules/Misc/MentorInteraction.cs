using Discord.Interactions;

namespace ExcelBotCs.Modules.Misc;

[Group("mentor", "Mentor commands")]
public class MentorInteraction : InteractionModuleBase<SocketInteractionContext>
{
	private const ulong TankSpecialistRole = 1404817680984178709;
	private const ulong HealerSpecialistRole = 1404817727612387471;
	private const ulong MeleeSpecialistRole = 1404817754401275925;
	private const ulong CasterSpecialistRole = 1404817824827838534;
	private const ulong RangedPhysSpecialistRole = 1404817896554631301;
	private const ulong CrafterSpecialistRole = 1447967209086521488;

	private async Task ToggleMentorRole(string name, ulong role)
	{
		var user = Context.GuildUser();

		if (user.Roles.Any(r => r.Id == role))
		{
			await user.RemoveRoleAsync(role);
			await RespondAsync($"{name} specialist role was removed.", ephemeral: true);
		}
		else
		{
			await user.AddRoleAsync(role);
			await RespondAsync($"{name} specialist role was added.", ephemeral: true);
		}
	}


	[SlashCommand("tank", "Toggle tank specialist role")]
	private Task ToggleTankMentorRole() => ToggleMentorRole("Tank", TankSpecialistRole);

	[SlashCommand("healer", "Toggle healer specialist role")]
	private Task ToggleHealerMentorRole() => ToggleMentorRole("Healer", HealerSpecialistRole);

	[SlashCommand("melee", "Toggle melee specialist role")]
	private Task ToggleMeleeMentorRole() => ToggleMentorRole("Melee DPS", MeleeSpecialistRole);


	[SlashCommand("caster", "Toggle caster specialist role")]
	private Task ToggleCasterMentorRole() => ToggleMentorRole("Caster DPS", CasterSpecialistRole);

	[SlashCommand("ranged", "Toggle ranged physical specialist role")]
	private Task ToggleRangedMentorRole() => ToggleMentorRole("Physical Ranged DPS", RangedPhysSpecialistRole);

	[SlashCommand("crafter", "Toggle crafter specialist role")]
	private Task ToggleCrafterMentorRole() => ToggleMentorRole("Crafter", CrafterSpecialistRole);
}