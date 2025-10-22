using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Database;

public class EventRepository : BaseRepository<Event>, IEventRepository
{
    public EventRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
        : base(mongoClient, databaseOptions)
    {
    }
}