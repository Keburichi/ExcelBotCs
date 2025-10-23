using Discord;
using Discord.Interactions;
using ExcelBotCs.Data;
using Sprache;
using System.Globalization;
using System.Text.RegularExpressions;

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

	private readonly Repository<EventDetails> _eventDetails;
	private readonly string _rootUrl;

	public TeamFormationInteraction(Prng rng, Database database)
	{
		_rng = rng;
		_eventDetails = database.GetCollection<EventDetails>("event_details");
		_rootUrl = Utils.GetEnvVar("EVENT_ENDPOINT_URL", nameof(TeamFormationInteraction));
	}

	private async Task<Dictionary<IEmote, HashSet<ulong>>> GetSignupsFromMessage(IEnumerable<IEmote> emotes, IMessage message)
	{
		var signups = new Dictionary<IEmote, HashSet<ulong>>();

		foreach (var emote in emotes)
		{
			signups.Add(emote, []);

			var users = await message.GetReactionUsersAsync(emote, 100).FlattenAsync();
			foreach (var user in users)
			{
				signups[emote].Add(user.Id);
			}
		}

		return signups;
	}
	private static IEnumerable<IEmote> ExtractEmotes(string input)
	{
		var matches = Regex.Matches(input, @"<a?:\w+:\d+>");
		foreach (Match match in matches)
		{
			if (Emote.TryParse(match.Value, out var emote))
				yield return emote;
		}

		var stripped = Regex.Replace(input, @"<a?:\w+:\d+>", "");
		var enumerator = StringInfo.GetTextElementEnumerator(stripped);
		while (enumerator.MoveNext())
		{
			var element = enumerator.GetTextElement();
			if (Emoji.TryParse(element, out var emoji))
				yield return emoji;
		}
	}

	[SlashCommand("list", "Get a list of signups from the post provided")]
	public async Task GetSignups(string postUrl, string? checkEmoji = null)
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
				var useEmoji = ExtractEmotes(checkEmoji ?? string.Empty).ToList();
				var emotes = useEmoji.Any()
					? msg.Message.Reactions.Keys.Where(useEmoji.Contains)
					: msg.Message.Reactions.Keys;
				var group = await GetSignupsFromMessage(emotes, msg.Message);
				var allSignups = group.Values.SelectMany(list => list.Select(id => id)).ToList();

				string GenerateInlineText(IEmote emote, HashSet<ulong> ids) =>
					$"{ToDisplay(emote)} ({ids.Count}): {ids.Select(id => allSignups.Count(signupId => signupId == id) == 1 ? $"⭐<@{id}>" : $"<@{id}>").ToList().PrettyJoin()}\n";

				await FollowupAsync($"### Reactions from {postUrl}");
				await Context.Channel.SendMessageAsync($"Total unique reactions: {allSignups.Distinct().Count()}");
				if (useEmoji.Any())
					await Context.Channel.SendMessageAsync($"Checking specified emotes: {string.Join(string.Empty, emotes.Select(ToDisplay) ?? [])}");

				foreach (var reaction in group.Select(kvp => GenerateInlineText(kvp.Key, kvp.Value)))
				{
					await Context.Channel.SendMessageAsync(reaction, allowedMentions: AllowedMentions.None);
				}
				break;
		}
	}

	private static string ToDisplay(IEmote emote) => emote switch
	{
		Emoji emoji => emoji.Name,
		Emote em => $"<:{em.Name}:{em.Id}>",
		_ => emote.Name
	};

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
		[MinValue(0)][MaxValue(23)] int endHourSt, string? tanks = null, string? healers = null, string? meleeDps = null, string? casterDps = null, string? rangedDps = null, [MinValue(0)][MaxValue(59)] int startMinuteSt = 0,
		[MinValue(0)][MaxValue(59)] int endMinuteSt = 0)
	{
		if (!Context.GuildUser().Roles.IsOfficer())
		{
			await RespondAsync("Only officers can use this command!", ephemeral: true);
			return;
		}

		IEnumerable<IUser> GetUsersFromString(string input)
		{
			var ids = Regex.Matches(input, @"\d+").Select(m => ulong.Parse(m.Value));
			return ids.Select(id => Context.Client.GetUser(id));
		}

		var tankIds = (string.IsNullOrWhiteSpace(tanks) ? [] : GetUsersFromString(tanks)).ToList();
		var healerIds = (string.IsNullOrWhiteSpace(healers) ? [] : GetUsersFromString(healers)).ToList();
		var meleeDpsIds = (string.IsNullOrWhiteSpace(meleeDps) ? [] : GetUsersFromString(meleeDps)).ToList();
		var casterDpsIds = (string.IsNullOrWhiteSpace(casterDps) ? [] : GetUsersFromString(casterDps)).ToList();
		var rangedDpsIds = (string.IsNullOrWhiteSpace(rangedDps) ? [] : GetUsersFromString(rangedDps)).ToList();

		var participants = new List<EventMemberDetails>();
		participants.AddRange(tankIds.Select(user => new EventMemberDetails() { DiscordId = user.Id, Role = Role.Tank }));
		participants.AddRange(healerIds.Select(user => new EventMemberDetails() { DiscordId = user.Id, Role = Role.Healer }));
		participants.AddRange(meleeDpsIds.Select(user => new EventMemberDetails() { DiscordId = user.Id, Role = Role.Melee }));
		participants.AddRange(casterDpsIds.Select(user => new EventMemberDetails() { DiscordId = user.Id, Role = Role.Caster }));
		participants.AddRange(rangedDpsIds.Select(user => new EventMemberDetails() { DiscordId = user.Id, Role = Role.Ranged }));

		if (participants.Count == 0)
		{
			await RespondAsync("No users were specified!", ephemeral: true);
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
			$"{(tankIds.Count > 0 ? $"<:RoleTank:1380979172423499846> {string.Join(" ", tankIds.Select(user => $"<@{user.Id}>"))}\r\n" : string.Empty)}" +
			$"{(healerIds.Count > 0 ? $"<:RoleHealer:1380979170787721368> {string.Join(" ", healerIds.Select(user => $"<@{user.Id}>"))}\r\n" : string.Empty)}" +
			$"{(meleeDpsIds.Count > 0 ? $"<:RoleMelee:873621778214318091> {string.Join(" ", meleeDpsIds.Select(user => $"<@{user.Id}>"))}\r\n" : string.Empty)}" +
			$"{(casterDpsIds.Count > 0 ? $"<:RoleCaster:873621778566635540> {string.Join(" ", casterDpsIds.Select(user => $"<@{user.Id}>"))}\r\n" : string.Empty)}" +
			$"{(rangedDpsIds.Count > 0 ? $"<:RoleRanged:873621778453368895> {string.Join(" ", rangedDpsIds.Select(user => $"<@{user.Id}>"))}\r\n" : string.Empty)}";
		output = output.Trim();

		var rosterChannel = Context.Guild.GetTextChannel(1411293182133665792);
		await rosterChannel.SendMessageAsync(output);

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
		var subLink = $"https://{_rootUrl}event/calendar/{Context.User.Id}";
		var downloadLink = $"https://{_rootUrl}event/retrieve/{Context.User.Id}.ics";

		await RespondAsync($"[Subscribe to an auto-updating Calendar]({subLink})\n-# This is a personalised calender that will automatically update with events you sign up to and can be added to iOS/Android notifications, Google Calendar, Apple Calendar and more \n\n-# [Download a single-use .ics calendar instead]({downloadLink})", ephemeral: true);
	}
}