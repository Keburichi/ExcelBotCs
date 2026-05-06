using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Modules.TeamFormation;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Database;

public class EventDetailsRepository : BaseRepository<EventDetails>, IEventDetailsRepository
{
    public EventDetailsRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
        : base(mongoClient, databaseOptions)
    {
    }

    protected override string GetCollectionName()
    {
        return "event_details";
    }

    public async Task<List<EventDetails>> GetFutureByParticipantAsync(ulong discordId)
    {
        return await Collection
            .Find(e => e.EndTime > DateTime.UtcNow && e.Participants.Any(p => p.DiscordId == discordId))
            .ToListAsync();
    }
}