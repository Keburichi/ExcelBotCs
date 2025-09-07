using Discord;
using Discord.Interactions;
using System.Text;

namespace ExcelBotCs.Modules.TeamFormation;

[Group("team", "Team commands")]
public class TeamFormationInteraction : InteractionModuleBase<SocketInteractionContext>
{
	private readonly Prng _rng;

	public enum Role
	{
		Tank,
		Healer,
		Melee,
		Caster,
		Ranged
	}

	private const string TankRoleEmote = "<:RoleTank:1380979172423499846>";
	private const string HealerRoleEmote = "<:RoleHealer:1380979170787721368>";
	private const string MeleeRoleEmote = "<:RoleMelee:873621778214318091>";
	private const string CasterRoleEmote = "<:RoleCaster:873621778566635540>";
	private const string RangedRoleEmote = "<:RoleRanged:873621778453368895>";

	private readonly Dictionary<Role, (Emote Emote, int Minimum, int Maximum)> _groups =
		new()
		{
			{ Role.Tank, (TankRoleEmote, 2, 2) },
			{ Role.Healer, (HealerRoleEmote, 2, 2) },
			{ Role.Melee, (MeleeRoleEmote, 1, 2) },
			{ Role.Caster, (CasterRoleEmote, 1, 2) },
			{ Role.Ranged, (RangedRoleEmote, 1, 1) }
		};

	public TeamFormationInteraction(Prng rng)
	{
		_rng = rng;
	}

	private async Task<Dictionary<Role, HashSet<ulong>>> GetSignupsFromMessage(IMessage message)
	{
		var signups = new Dictionary<Role, HashSet<ulong>>()
		{
			{ Role.Tank, [] },
			{ Role.Healer, [] },
			{ Role.Melee, [] },
			{ Role.Caster, [] },
			{ Role.Ranged, [] }
		};

		foreach (var (role, (emote, _, _)) in _groups)
		{
			var users = await message.GetReactionUsersAsync(emote, 100).FlattenAsync();
			foreach (var user in users)
			{
				signups[role].Add(user.Id);
			}
		}

		return signups;
	}


	[SlashCommand("list", "Get a list of signups from the post provided")]
	public async Task GetSignups(string postUrl)
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
				var group = await GetSignupsFromMessage(msg.Message);
				var allSignups = group.Values.SelectMany(list => list.Select(id => id)).ToList();

				string GenerateInlineText(Dictionary<Role, HashSet<ulong>> group, Role role) =>
					$"{_groups[role].Emote} ({group[role].Count}): {group[role].Select(id => allSignups.Count(signupId => signupId == id) == 1 ? $"⭐<@{id}>" : $"<@{id}>").ToList().PrettyJoin()}\n";

				await FollowupAsync($"### Signups from {postUrl}\nTotal unique signups: {allSignups.Distinct().Count()}\n" +
									$"{GenerateInlineText(group, Role.Tank)}" +
									$"{GenerateInlineText(group, Role.Healer)}" +
									$"{GenerateInlineText(group, Role.Melee)}" +
									$"{GenerateInlineText(group, Role.Caster)}" +
									$"{GenerateInlineText(group, Role.Ranged)}");
				break;
		}
	}


	[SlashCommand("make", "Randomly assemble a group from the post provided")]
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
				var signups = await GetSignupsFromMessage(msg.Message);

				if (signups.Values.Sum(set => set.Count) < 8)
				{
					await RespondAsync("There are not enough signups to form a group.");
					return;
				}

				Dictionary<Role, HashSet<ulong>>? group = null;
				var cts = new CancellationTokenSource();
				var task = TryFormGroup(cts.Token, signups);

				if (await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(5), cts.Token)) == task)
				{
					await cts.CancelAsync();
					group = await task;
				}
				else
				{
					await cts.CancelAsync();
					await FollowupAsync("Trying to generate a group took too long, it's probably not possible to form one.", ephemeral: true);
				}

				await FollowupAsync($"### Group generated from {postUrl}\n{GenerateRoleText(group, Role.Tank)}{GenerateRoleText(group, Role.Healer)}{GenerateRoleText(group, Role.Melee)}{GenerateRoleText(group, Role.Caster)}{GenerateRoleText(group, Role.Ranged)}");
				break;
		}
	}

	private string GenerateRoleText(Dictionary<Role, HashSet<ulong>> group, Role role) =>
		$"{_groups[role].Emote}: {string.Join(' ', group[role].Select(id => $"<@{id}>"))}\n";

	private async Task<Dictionary<Role, HashSet<ulong>>?> TryFormGroup(CancellationToken token, Dictionary<Role, HashSet<ulong>> signups)
	{
		while (true)
		{
			if (token.IsCancellationRequested)
				return null;

			var result = await FormGroup(token, signups);
			if (result != null)
				return result;
		}
	}

	private async Task<Dictionary<Role, HashSet<ulong>>?> FormGroup(CancellationToken token, Dictionary<Role, HashSet<ulong>> signups)
	{
		var clone = signups.ToDictionary();
		var composedGroup = new Dictionary<Role, HashSet<ulong>>();

		foreach (var (role, _) in _groups)
		{
			composedGroup.Add(role, []);
		}

		while (composedGroup.Values.SelectMany(item => item).Count() != 8)
		{
			if (token.IsCancellationRequested)
				return null;

			var min = clone.Min(kvp => kvp.Value.Count);
			var nextPriority = clone
				.Where(kvp => kvp.Value.Count == min)
				.SelectMany(kvp => kvp.Value.Select(id => (kvp.Key, id)))
				.ToList();

			if (nextPriority.Count == 0)
				return null;

			var user = _rng.Pick(nextPriority).First();

			if (!composedGroup.TryAdd(user.Key, [user.id]))
				composedGroup[user.Key].Add(user.id);

			// remove user from the pool
			clone[user.Key].Remove(user.id);

			// if we have enough, remove the role from the pool
			if (composedGroup[user.Key].Count >= _groups[user.Key].Maximum)
				clone.Remove(user.Key);

			// if we have enough dps, remove them all from the pool to prevent 5 dps nonsense
			if (composedGroup[Role.Melee].Count + composedGroup[Role.Caster].Count + composedGroup[Role.Ranged].Count >= 4)
			{
				clone.Remove(Role.Melee);
				clone.Remove(Role.Caster);
				clone.Remove(Role.Ranged);
			}

			await Task.Yield();
		}

		return composedGroup;
	}

	public enum Month
	{
		January = 1,
		February,
		March,
		April,
		May,
		June,
		July,
		August,
		September,
		October,
		November,
		December
	}


	[SlashCommand("schedule", "Creates the group to run")]
	public async Task ScheduleGroup(string eventName, Month month, [MinValue(1)][MaxValue(31)] int day, [MinValue(0)][MaxValue(23)] int startHourSt,
		[MinValue(0)][MaxValue(23)] int endHourSt, IUser tank1, IUser tank2, IUser healer1, IUser healer2, IUser melee1, IUser caster1, IUser ranged1,
		IUser? melee2 = null, IUser? caster2 = null, IUser? ranged2 = null, [MinValue(0)][MaxValue(59)] int startMinuteSt = 0,
		[MinValue(0)][MaxValue(59)] int endMinuteSt = 0)
	{
		if (!Context.GuildUser().Roles.IsOfficer())
		{
			await RespondAsync("Only officers can use this command!", ephemeral: true);
			return;
		}

		var sanityCheck = new List<IUser?>() { melee2, caster2, ranged2 }.Count(user => user != null);
		if (sanityCheck != 1)
		{
			// we have too few or too many members, reject it
			await RespondAsync("An incorrect number of members was specified, fix your input", ephemeral: true);
			return;
		}

		var year = DateTime.Now.Year;
		var startTime = new DateTime(year, (int)month, day, startHourSt, startMinuteSt, 0, DateTimeKind.Utc);
		var endTime = new DateTime(year, (int)month, day, endHourSt, endMinuteSt, 0, DateTimeKind.Utc);
		var startEpoch = ((DateTimeOffset)startTime).ToUnixTimeSeconds();
		var endEpoch = ((DateTimeOffset)endTime).ToUnixTimeSeconds();

		await RespondAsync($"Forming group and posting schedule...", ephemeral: true);

		var output =
			$"## {eventName}\r\n" +
			$"<t:{startEpoch}:R>\r\n" +
			$"<t:{startEpoch}:F> - <t:{endEpoch}:F>\r\n\r\n" +
			$"<:RoleTank:1380979172423499846> <@{tank1.Id}> <@{tank2.Id}>\r\n" +
			$"<:RoleHealer:1380979170787721368> <@{healer1.Id}> <@{healer2.Id}>\r\n" +
			$"<:RoleMelee:873621778214318091> <@{melee1.Id}> {(melee2 != null ? $"<@{melee2.Id}>" : string.Empty)}\r\n" +
			$"<:RoleCaster:873621778566635540> <@{caster1.Id}> {(caster2 != null ? $"<@{caster2.Id}>" : string.Empty)}\r\n" +
			$"<:RoleRanged:873621778453368895> <@{ranged1.Id}> {(ranged2 != null ? $"<@{ranged2.Id}>" : string.Empty)}";

		var ics =
			"BEGIN:VCALENDAR\n" +
			"VERSION:2.0\n" +
			"PRODID:Excelsior Events\n" +
			"CALSCALE:GREGORIAN\n" +
			"METHOD:PUBLISH\n" +
			"BEGIN:VTIMEZONE\n" +
			"TZID:Etc/UTC\n" +
			"END:VTIMEZONE\n" +
			"BEGIN:VEVENT\n" +
			$"DTSTART:{startTime:yyyyMMddTHHmm00}\n" +
			$"DTEND:{endTime:yyyyMMddTHHmm00}\n" +
			$"SUMMARY:{eventName}\n" +
			"LOCATION:Final Fantasy XIV Online\n" +
			"END:VEVENT\n" +
			"END:VCALENDAR";

		var bytes = Encoding.UTF8.GetBytes(ics);
		using var stream = new MemoryStream(bytes);

		var rosterChannel = Context.Guild.GetTextChannel(1411293182133665792);
		await rosterChannel.SendMessageAsync(output);
		await rosterChannel.SendFileAsync(stream, "event.ics", "Import to Calendar");
	}
}