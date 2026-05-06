using ExcelBotCs.Database;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Modules.TeamFormation;
using ExcelBotCs.TestFramework.Database;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Tests.Database;

[TestFixture]
public class EventDetailsRepositoryTests : MongoDbTest
{
    private IEventDetailsRepository _repository = null!;

    protected override void InitializeRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
    {
        _repository = new EventDetailsRepository(mongoClient, databaseOptions);
    }

    [Test]
    public async Task GetFutureByParticipantAsync_ReturnsEmpty_WhenNoEventsExist()
    {
        var discordId = ulong.Parse(GenerateRandomDiscordId());

        var result = await _repository.GetFutureByParticipantAsync(discordId);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetFutureByParticipantAsync_ReturnsFutureEvents_WhenParticipantIsRegistered()
    {
        var discordId = ulong.Parse(GenerateRandomDiscordId());
        var futureEvent = new EventDetails
        {
            Name = "Future Raid",
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime = DateTime.UtcNow.AddHours(3),
            Participants = [new EventMemberDetails { DiscordId = discordId, Role = Role.Tank }]
        };
        await _repository.CreateAsync(futureEvent);

        var result = await _repository.GetFutureByParticipantAsync(discordId);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Future Raid"));
    }

    [Test]
    public async Task GetFutureByParticipantAsync_ExcludesPastEvents()
    {
        var discordId = ulong.Parse(GenerateRandomDiscordId());
        var pastEvent = new EventDetails
        {
            Name = "Past Raid",
            StartTime = DateTime.UtcNow.AddHours(-3),
            EndTime = DateTime.UtcNow.AddHours(-1),
            Participants = [new EventMemberDetails { DiscordId = discordId, Role = Role.Healer }]
        };
        await _repository.CreateAsync(pastEvent);

        var result = await _repository.GetFutureByParticipantAsync(discordId);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetFutureByParticipantAsync_ExcludesEventsWhereParticipantIsNotRegistered()
    {
        var discordId = ulong.Parse(GenerateRandomDiscordId());
        var otherDiscordId = ulong.Parse(GenerateRandomDiscordId());
        var futureEvent = new EventDetails
        {
            Name = "Someone Else's Event",
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime = DateTime.UtcNow.AddHours(3),
            Participants = [new EventMemberDetails { DiscordId = otherDiscordId, Role = Role.Melee }]
        };
        await _repository.CreateAsync(futureEvent);

        var result = await _repository.GetFutureByParticipantAsync(discordId);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetFutureByParticipantAsync_ReturnsOnlyEventsWhereParticipantIsRegistered()
    {
        var discordId = ulong.Parse(GenerateRandomDiscordId());
        var otherDiscordId = ulong.Parse(GenerateRandomDiscordId());

        await _repository.CreateAsync(new EventDetails
        {
            Name = "My Event",
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime = DateTime.UtcNow.AddHours(3),
            Participants = [new EventMemberDetails { DiscordId = discordId, Role = Role.Caster }]
        });
        await _repository.CreateAsync(new EventDetails
        {
            Name = "Other Event",
            StartTime = DateTime.UtcNow.AddHours(2),
            EndTime = DateTime.UtcNow.AddHours(4),
            Participants = [new EventMemberDetails { DiscordId = otherDiscordId, Role = Role.Ranged }]
        });

        var result = await _repository.GetFutureByParticipantAsync(discordId);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("My Event"));
    }

    [Test]
    public async Task GetFutureByParticipantAsync_ReturnsMultipleEvents_WhenParticipantIsInSeveral()
    {
        var discordId = ulong.Parse(GenerateRandomDiscordId());

        for (var i = 1; i <= 3; i++)
            await _repository.CreateAsync(new EventDetails
            {
                Name = $"Event {i}",
                StartTime = DateTime.UtcNow.AddHours(i),
                EndTime = DateTime.UtcNow.AddHours(i + 2),
                Participants = [new EventMemberDetails { DiscordId = discordId, Role = Role.Tank }]
            });

        var result = await _repository.GetFutureByParticipantAsync(discordId);

        Assert.That(result, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task GetFutureByParticipantAsync_ExcludesEventWithNoParticipants()
    {
        var discordId = ulong.Parse(GenerateRandomDiscordId());
        await _repository.CreateAsync(new EventDetails
        {
            Name = "Empty Event",
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime = DateTime.UtcNow.AddHours(3),
            Participants = []
        });

        var result = await _repository.GetFutureByParticipantAsync(discordId);

        Assert.That(result, Is.Empty);
    }
}