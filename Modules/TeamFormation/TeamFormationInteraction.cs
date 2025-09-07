using Discord;
using Discord.Interactions;
using ExcelBotCs.Data;

namespace ExcelBotCs.Modules.TeamFormation;

[Group("event", "Event commands")]
public class TeamFormationInteraction : InteractionModuleBase<SocketInteractionContext>
{
	private readonly Prng _rng;

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

	private readonly Repository<EventDetails> _eventDetails;
	private readonly string _rootUrl;

	public TeamFormationInteraction(Prng rng, Database database)
	{
		_rng = rng;
		_eventDetails = database.GetCollection<EventDetails>("event_details");
		_rootUrl = Utils.GetEnvVar("EVENT_ENDPOINT_URL", nameof(TeamFormationInteraction));
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
		if (!Context.GuildUser().Roles.IsOfficer())
		{
			await RespondAsync("Only officers can use this command!", ephemeral: true);
			return;
		}

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

		var rosterChannel = Context.Guild.GetTextChannel(1411293182133665792);
		await rosterChannel.SendMessageAsync(output);

		var participants = new List<EventMemberDetails>
		{
			new() { DiscordId = tank1.Id, Role = Role.Tank },
			new() { DiscordId = tank2.Id, Role = Role.Tank },
			new() { DiscordId = healer1.Id, Role = Role.Healer },
			new() { DiscordId = healer2.Id, Role = Role.Healer },
			new() { DiscordId = melee1.Id, Role = Role.Melee },
			new() { DiscordId = caster1.Id, Role = Role.Caster },
			new() { DiscordId = ranged1.Id, Role = Role.Ranged }
		};

		if (melee2 != null)
			participants.Add(new EventMemberDetails() { DiscordId = melee2.Id, Role = Role.Melee });

		if (caster2 != null)
			participants.Add(new EventMemberDetails() { DiscordId = caster2.Id, Role = Role.Caster });

		if (ranged2 != null)
			participants.Add(new EventMemberDetails() { DiscordId = ranged2.Id, Role = Role.Ranged });

		await _eventDetails.Insert(new EventDetails()
		{
			StartTime = startTime,
			EndTime = endTime,
			Name = eventName,
			Participants = participants
		});
	}

	[SlashCommand("remind", "Get an auto-updating calendar link for keeping track")]
	public async Task RemindEvents()
	{
		await RespondAsync($"[Add to Calendar]({_rootUrl}event/retrieve/{Context.User.Id})\n-# This is a personalised calender that will automatically update with events you sign up to and can be added to iOS/Android notifications, Google Calendar, Apple Calendar and more", ephemeral: true);
	}
}