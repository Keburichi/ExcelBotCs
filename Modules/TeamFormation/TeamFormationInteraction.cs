using Discord;
using Discord.Interactions;

namespace ExcelBotCs.Modules.TeamFormation;

public class TeamFormationInteraction : InteractionModuleBase<SocketInteractionContext>
{
	private const string TankRoleEmote = "<:RoleTank:1380979172423499846>";
	private const string HealerRoleEmote = "<:RoleHealer:1380979170787721368>";
	private const string MeleeRoleEmote = "<:RoleMelee:873621778214318091>";
	private const string CasterRoleEmote = "<:RoleCaster:873621778566635540>";
	private const string RangedRoleEmote = "<:RoleRanged:873621778453368895>";

	private readonly List<(string Role, Emote Emote, int Minimum, int Maximum)> _groups =
	[
		("Tank", TankRoleEmote, 2, 2),
		("Healer", HealerRoleEmote, 2, 2),
		("Melee", MeleeRoleEmote, 1, 2),
		("Caster", CasterRoleEmote, 1, 2),
		("Ranged", RangedRoleEmote, 1, 1)
	];

	[SlashCommand("makegroup", "Pick a number and have a chance to win!")]
	public async Task MakeGroup(string postUrl)
	{
		await DeferAsync();

		switch (await Context.Client.GetMessageFromUrl(postUrl))
		{
			case Extensions.NotValidUrlMessageResponse:
				await FollowupAsync("The provided URL does not seem to be a valid Discord URL", ephemeral: true);
				break;

			case Extensions.NotFoundUrlMessageResponse:
				await FollowupAsync(
					"Could not find the Guild/Channel this message belongs to. Do I have permission to view it?",
					ephemeral: true);
				break;

			case Extensions.SuccessMessageResponse msg:

				var reactions = msg.Message.Reactions;
				var signups = new List<(string Role, ulong Id)>();

				// check that each role react is present
				foreach (var (role, emote, required, _) in _groups)
				{
					var reaction = reactions.TryGetValue(emote, out var metadata);
					if (!reaction || metadata.ReactionCount < required)
					{
						await RespondAsync($"There are not enough {role} signups to form a group.");
						return;
					}

					var msgReactions = await msg.Message.GetReactionUsersAsync(emote, 100).FlattenAsync();
					var toAdd = msgReactions.Select(user => (role, user.Id))
						.ToList();

					signups.AddRange(toAdd);
				}

				Dictionary<string, List<ulong>>? group = null;
				var cts = new CancellationTokenSource();
				var task = TryFormGroup(cts.Token, signups.ToList());

				if (await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(10), cts.Token)) == task)
				{
					await cts.CancelAsync();
					group = await task;
				}
				else
				{
					await cts.CancelAsync();
					await FollowupAsync("Trying to generate a group took too long, it's probably not possible to form one.", ephemeral: true);
				}

				await FollowupAsync($"### Group generated from {postUrl}\n{GenerateRoleText(TankRoleEmote, group, "Tank")}{GenerateRoleText(HealerRoleEmote, group, "Healer")}{GenerateRoleText(MeleeRoleEmote, group, "Melee")}{GenerateRoleText(CasterRoleEmote, group, "Caster")}{GenerateRoleText(RangedRoleEmote, group, "Ranged")}");
				break;
		}
	}

	private string GenerateRoleText(string emote, Dictionary<string, List<ulong>> group, string role) =>
		$"{emote}: {string.Join(' ', group[role].Select(id => $"<@{id}>"))}\n";

	private async Task<Dictionary<string, List<ulong>>?> TryFormGroup(CancellationToken token, List<(string Role, ulong Id)> signups)
	{
		while (true)
		{
			if (token.IsCancellationRequested)
				return null;

			var result = await FormGroup(token, signups.ToList());
			if (result != null)
				return result;
		}
	}

	private async Task<Dictionary<string, List<ulong>>?> FormGroup(CancellationToken token, List<(string Role, ulong Id)> signups)
	{
		var composedGroup = new Dictionary<string, List<ulong>>();

		while (composedGroup.Values.SelectMany(item => item).Count() != 8)
		{
			if (token.IsCancellationRequested)
				return null;

			// start with the role with the lowest signups for roles that haven't yet been selected
			var lowestReaction = signups
				.GroupBy(x => x.Role)
				.OrderBy(x => x.Count())
				.Select(x => x.Key)
				.FirstOrDefault(x => !composedGroup.TryGetValue(x, out var list) || list.Count < 1);

			// if no such case exists, flex
			lowestReaction ??= signups.GroupBy(x => x.Role)
				.OrderBy(x => x.Count())
				.Select(x => x.Key)
				.FirstOrDefault();

			if (lowestReaction == null)
				return null;

			var matchingSignups = signups.Where(x => x.Role == lowestReaction);

			var user = matchingSignups.Shuffle().First();

			if (!composedGroup.TryAdd(user.Role, [user.Id]))
				composedGroup[user.Role].Add(user.Id);

			// remove user from the pool
			signups.RemoveAll(signup => signup.Id == user.Id);

			// if we have enough, remove the role from the pool
			if (composedGroup[user.Role].Count >= _groups.First(group => group.Role == user.Role).Maximum)
				signups.RemoveAll(signup => signup.Role == user.Role);

			await Task.Yield();
		}

		return composedGroup;
	}
}