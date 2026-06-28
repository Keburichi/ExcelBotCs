using System.Globalization;
using System.Text.RegularExpressions;
using Discord;
using Discord.Interactions;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Discord;
using ExcelBotCs.Extensions;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database.Events;
using ExcelBotCs.Services.API.Interfaces;
using Microsoft.Extensions.Options;

namespace ExcelBotCs.Modules.TeamFormation;

[Group("event", "Event commands")]
public class TeamFormationInteraction : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IEventDetailsRepository _eventDetails;
    private readonly IDiscordBotClient _discordClient;
    private readonly IEventService _eventService;
    private readonly IMemberRepository _memberRepository;
    private readonly IFightService _fightService;
    private readonly IBossService _bossService;
    private readonly DiscordBotOptions _discordBotOptions;
    private readonly string _rootUrl;

    public TeamFormationInteraction(Prng rng, IEventDetailsRepository eventDetailsRepository,
        IDiscordBotClient discordClient, IOptions<DiscordBotOptions> discordBotOptions, IEventService eventService,
        IMemberRepository memberRepository, IFightService fightService, IBossService bossService)
    {
        _eventDetails = eventDetailsRepository;
        _discordClient = discordClient;
        _eventService = eventService;
        _memberRepository = memberRepository;
        _fightService = fightService;
        _bossService = bossService;

        _discordBotOptions = discordBotOptions.Value;
        _rootUrl = Utils.GetEnvVar("EVENT_ENDPOINT_URL", nameof(TeamFormationInteraction));
    }

    private async Task<Dictionary<IEmote, HashSet<ulong>>> GetSignupsFromMessage(IEnumerable<IEmote> emotes,
        IMessage message)
    {
        var signups = new Dictionary<IEmote, HashSet<ulong>>();

        foreach (var emote in emotes)
        {
            signups.Add(emote, []);

            var users = await message.GetReactionUsersAsync(emote, 100).FlattenAsync();
            foreach (var user in users) signups[emote].Add(user.Id);
        }

        return signups;
    }

    private static IEnumerable<IEmote> ExtractEmotes(string input)
    {
        var matches = Regex.Matches(input, @"<a?:\w+:\d+>");
        foreach (Match match in matches)
            if (Emote.TryParse(match.Value, out var emote))
                yield return emote;

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
        if (!Context.GuildUser().IsOfficer(_discordBotOptions))
        {
            await RespondAsync("Only officers can use this command!", ephemeral: true);
            return;
        }

        await DeferAsync();

        switch (await _discordClient.GetMessageFromUrl(postUrl))
        {
            case NotValidUrlMessageResponse:
                await FollowupAsync("The provided URL does not seem to be a valid Discord URL", ephemeral: true);
                break;

            case NotFoundUrlMessageResponse:
                await FollowupAsync(
                    "Could not find the Guild/Channel this message belongs to. Do I have permission to view it?",
                    ephemeral: true);
                break;

            case SuccessMessageResponse msg:
                // get the message id and check if we have any event that corresponds to it
                var messageId = msg.Message.Id;
                var eventDetails = await _eventService.GetAsync().ContinueWith(x =>
                    x.Result.FirstOrDefault(e => e.SignupPostId == messageId.ToString()));
                if (eventDetails != null)
                {
                    var uniqueSignups = eventDetails.Signups.Select(x => x.DiscordUserId).Distinct().Count();
                    await Context.Channel.SendMessageAsync($"Event found: {eventDetails.Name}");
                    await Context.Channel.SendMessageAsync($"Total unique signups: {uniqueSignups}");

                    var signupsMessage = "";

                    foreach (var eventDetailsSignupButtonConfig in eventDetails.SignupButtonConfigs)
                    {
                        // Check if signup config has an emoji
                        // If there is one, show it, otherwise use the label of the button
                        var signups = eventDetails.Signups.Where(x =>
                            x.SignupSlugs.Contains(eventDetailsSignupButtonConfig.Slug));

                        var signupMentions = signups.Select(x => x.DiscordUserId)
                            .Select(signupUserId => $"<@{signupUserId}> ").ToList();

                        if (!string.IsNullOrWhiteSpace(eventDetailsSignupButtonConfig.EmojiId))
                        {
                            var emote = _discordClient.GetEmoteById(
                                ulong.Parse(eventDetailsSignupButtonConfig.EmojiId));
                            if (emote != null)
                            {
                                signupsMessage += $"{emote} ({signups.Count()}): {signupMentions.PrettyJoin()}";
                                continue;
                            }
                        }

                        signupsMessage +=
                            $"{eventDetailsSignupButtonConfig.Label} ({signups.Count()}): {signupMentions.PrettyJoin()}{Environment.NewLine}";
                    }
                    
                    await Context.Channel.SendMessageAsync(signupsMessage);
                }
                else
                {
                    var useEmoji = ExtractEmotes(checkEmoji ?? string.Empty).ToList();
                    var emotes = useEmoji.Any()
                        ? msg.Message.Reactions.Keys.Where(useEmoji.Contains)
                        : msg.Message.Reactions.Keys;
                    var group = await GetSignupsFromMessage(emotes, msg.Message);
                    var allSignups = group.Values.SelectMany(list => list.Select(id => id)).ToList();

                    string GenerateInlineText(IEmote emote, HashSet<ulong> ids)
                    {
                        return
                            $"{ToDisplay(emote)} ({ids.Count}): {ids.Select(id => allSignups.Count(signupId => signupId == id) == 1 ? $"⭐<@{id}>" : $"<@{id}>").ToList().PrettyJoin()}\n";
                    }

                    await FollowupAsync($"### Reactions from {postUrl}");
                    await Context.Channel.SendMessageAsync($"Total unique reactions: {allSignups.Distinct().Count()}");
                    if (useEmoji.Any())
                        await Context.Channel.SendMessageAsync(
                            $"Checking specified emotes: {string.Join(string.Empty, emotes.Select(ToDisplay) ?? [])}");

                    foreach (var reaction in group.Select(kvp => GenerateInlineText(kvp.Key, kvp.Value)))
                        await Context.Channel.SendMessageAsync(reaction, allowedMentions: AllowedMentions.None);
                }

                await FollowupAsync("Done!", ephemeral: true);
                break;
        }
    }

    private static string ToDisplay(IEmote emote)
    {
        return emote switch
        {
            Emoji emoji => emoji.Name,
            Emote em => $"<:{em.Name}:{em.Id}>",
            _ => emote.Name
        };
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
    public async Task ScheduleGroup(string eventName, Month month, [MinValue(1)] [MaxValue(31)] int day,
        [MinValue(0)] [MaxValue(23)] int startHourSt,
        [MinValue(0)] [MaxValue(23)] int endHourSt, string? tanks = null, string? healers = null,
        string? meleeDps = null, string? casterDps = null, string? rangedDps = null,
        [MinValue(0)] [MaxValue(59)] int startMinuteSt = 0,
        [MinValue(0)] [MaxValue(59)] int endMinuteSt = 0)
    {
        if (!Context.GuildUser().IsOfficer(_discordBotOptions))
        {
            await RespondAsync("Only officers can use this command!", ephemeral: true);
            return;
        }

        IEnumerable<IUser> GetUsersFromString(string input)
        {
            var ids = Regex.Matches(input, @"\d+").Select(m => ulong.Parse(m.Value));
            return ids.Select(id => _discordClient.GetUser(id));
        }

        var tankIds = (string.IsNullOrWhiteSpace(tanks) ? [] : GetUsersFromString(tanks)).ToList();
        var healerIds = (string.IsNullOrWhiteSpace(healers) ? [] : GetUsersFromString(healers)).ToList();
        var meleeDpsIds = (string.IsNullOrWhiteSpace(meleeDps) ? [] : GetUsersFromString(meleeDps)).ToList();
        var casterDpsIds = (string.IsNullOrWhiteSpace(casterDps) ? [] : GetUsersFromString(casterDps)).ToList();
        var rangedDpsIds = (string.IsNullOrWhiteSpace(rangedDps) ? [] : GetUsersFromString(rangedDps)).ToList();

        var participants = new List<EventMemberDetails>();
        participants.AddRange(
            tankIds.Select(user => new EventMemberDetails { DiscordId = user.Id, Role = Role.Tank }));
        participants.AddRange(healerIds.Select(user => new EventMemberDetails
            { DiscordId = user.Id, Role = Role.Healer }));
        participants.AddRange(meleeDpsIds.Select(user => new EventMemberDetails
            { DiscordId = user.Id, Role = Role.Melee }));
        participants.AddRange(casterDpsIds.Select(user => new EventMemberDetails
            { DiscordId = user.Id, Role = Role.Caster }));
        participants.AddRange(rangedDpsIds.Select(user => new EventMemberDetails
            { DiscordId = user.Id, Role = Role.Ranged }));

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

        await RespondAsync("Forming group and posting schedule...", ephemeral: true);

        var output =
            $"## {eventName}\r\n" +
            $"<t:{startEpoch}:R>\r\n" +
            $"<t:{startEpoch}:F> - <t:{endEpoch}:F>\r\n\r\n" +
            $"{(tankIds.Count > 0 ? $"{Constants.TankRoleEmote} {string.Join(" ", tankIds.Select(user => $"<@{user.Id}>"))}\r\n" : string.Empty)}" +
            $"{(healerIds.Count > 0 ? $"{Constants.HealerRoleEmote} {string.Join(" ", healerIds.Select(user => $"<@{user.Id}>"))}\r\n" : string.Empty)}" +
            $"{(meleeDpsIds.Count > 0 ? $"{Constants.MeleeRoleEmote} {string.Join(" ", meleeDpsIds.Select(user => $"<@{user.Id}>"))}\r\n" : string.Empty)}" +
            $"{(casterDpsIds.Count > 0 ? $"{Constants.CasterRoleEmote}  {string.Join(" ", casterDpsIds.Select(user => $"<@{user.Id}>"))}\r\n" : string.Empty)}" +
            $"{(rangedDpsIds.Count > 0 ? $"{Constants.RangedRoleEmote} {string.Join(" ", rangedDpsIds.Select(user => $"<@{user.Id}>"))}\r\n" : string.Empty)}";
        output = output.Trim();

        var rosterChannel = Context.Guild.GetTextChannel(1411293182133665792);
        await rosterChannel.SendMessageAsync(output);

        await _eventDetails.CreateAsync(new EventDetails
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

        await RespondAsync(
            $"[Subscribe to an auto-updating Calendar]({subLink})\n-# This is a personalised calender that will automatically update with events you sign up to and can be added to iOS/Android notifications, Google Calendar, Apple Calendar and more \n\n-# [Download a single-use .ics calendar instead]({downloadLink})",
            ephemeral: true);
    }

    private static int RequiredParticipantsFor(PartySize partySize)
    {
        return partySize switch
        {
            PartySize.Light => 4,
            PartySize.Full => 8,
            PartySize.Alliance => 24,
            PartySize.Any => 0,
            _ => 0
        };
    }

    // Mirrors the `standard` / `roles-helper` presets in CreateEventView.vue (L196-216).
    private static List<SignupButtonConfig> BuildSignupButtonConfigs(SignupPreset preset)
    {
        var configs = new List<SignupButtonConfig>
        {
            new()
            {
                Slug = "tank", Label = "Tank", EmojiId = Constants.TankRoleEmoteId.ToString(), IsHelper = false,
                MappedRole = Role.Tank
            },
            new()
            {
                Slug = "healer", Label = "Healer", EmojiId = Constants.HealerRoleEmoteId.ToString(), IsHelper = false,
                MappedRole = Role.Healer
            },
            new()
            {
                Slug = "melee", Label = "Melee", EmojiId = Constants.MeleeRoleEmoteId.ToString(), IsHelper = false,
                MappedRole = Role.Melee
            },
            new()
            {
                Slug = "caster", Label = "Caster", EmojiId = Constants.CasterRoleEmoteId.ToString(), IsHelper = false,
                MappedRole = Role.Caster
            },
            new()
            {
                Slug = "ranged", Label = "Ranged", EmojiId = Constants.RangedRoleEmoteId.ToString(), IsHelper = false,
                MappedRole = Role.Ranged
            }
        };

        if (preset == SignupPreset.RolesHelper)
            configs.Add(new SignupButtonConfig
                { Slug = "helper", Label = "Helper", EmojiId = null, IsHelper = true, MappedRole = null });

        return configs;
    }

    [SlashCommand("create", "Create and post an event to the events channel")]
    public async Task CreateEvent(
        [Summary("name", "The event name")] string name,
        [Summary("type", "The type of event")] EventType type,
        [Summary("month", "Start month (ST/UTC)")]
        Month month,
        [Summary("day", "Start day of month (ST/UTC)")] [MinValue(1)] [MaxValue(31)]
        int day,
        [Summary("hour", "Start hour 0-23 (ST/UTC)")] [MinValue(0)] [MaxValue(23)]
        int hour,
        [Summary("duration", "Duration in minutes")] [MinValue(1)]
        int duration,
        [Summary("party-size", "Required party size")]
        PartySize partySize,
        [Summary("signups", "Signup buttons to show")]
        SignupPreset signups,
        [Summary("minute", "Start minute 0-59 (ST/UTC)")] [MinValue(0)] [MaxValue(59)]
        int minute = 0,
        [Summary("description", "Optional event description")]
        string? description = null,
        [Summary("fight", "Optional fight (auto-fills the event image)")]
        [Autocomplete(typeof(FightAutocompleteHandler))]
        string? fight = null)
    {
        if (!Context.GuildUser().IsOfficer(_discordBotOptions))
        {
            await RespondAsync("Only officers can use this command!", ephemeral: true);
            return;
        }

        await DeferAsync(true);

        var member = await _memberRepository.GetByDiscordId(Context.User.Id.ToString());
        if (member is null)
        {
            await FollowupAsync("Member not found for the current user.", ephemeral: true);
            return;
        }

        // start is parsed as UTC, which equals FFXIV server time (ST) - no timezone conversion needed.
        // Use the current year, rolling to next year if the chosen date has already passed.
        DateTime startTime;
        try
        {
            var year = DateTime.UtcNow.Year;
            startTime = new DateTime(year, (int)month, day, hour, minute, 0, DateTimeKind.Utc);
            if (startTime < DateTime.UtcNow)
                startTime = new DateTime(year + 1, (int)month, day, hour, minute, 0, DateTimeKind.Utc);
        }
        catch (ArgumentOutOfRangeException)
        {
            await FollowupAsync($"{month} {day} is not a valid date. Please check the day for the chosen month.",
                ephemeral: true);
            return;
        }

        string? fightId = null;
        string? pictureUrl = null;
        if (!string.IsNullOrWhiteSpace(fight))
        {
            var selectedFight = await _fightService.GetFightAsync(fight);
            if (selectedFight is not null)
            {
                fightId = selectedFight.Id;
                if (!string.IsNullOrWhiteSpace(selectedFight.BossId))
                    pictureUrl = (await _bossService.GetBossAsync(selectedFight.BossId))?.ImageUrl;
            }
        }

        var requiredParticipants = RequiredParticipantsFor(partySize);

        var newEvent = new Event
        {
            Name = name,
            Description = description ?? string.Empty,
            Type = type,
            StartDate = startTime,
            Duration = duration,
            SignupType = SignupType.SingleEvent,
            ICalString = string.Empty,
            RequiredParticipants = requiredParticipants,
            MaxNumberOfParticipants = requiredParticipants,
            SignupButtonConfigs = BuildSignupButtonConfigs(signups),
            FightId = fightId,
            PictureUrl = pictureUrl,
            AuthorId = member.Id,
            Organizer = member.PlayerName
        };

        await _eventService.CreateAsync(newEvent);

        var epoch = ((DateTimeOffset)startTime).ToUnixTimeSeconds();
        await FollowupAsync($"Created **{name}** starting <t:{epoch}:F> — posted to the events channel.",
            ephemeral: true);
    }
}