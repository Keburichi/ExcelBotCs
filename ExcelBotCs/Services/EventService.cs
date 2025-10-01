using ExcelBotCs.Data;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database;
using Microsoft.Extensions.Options;

namespace ExcelBotCs.Services;

public class EventService : BaseDatabaseService<Event>
{
    public EventService(IOptions<DatabaseOptions> databaseConfig) : base(databaseConfig)
    {
    }
}