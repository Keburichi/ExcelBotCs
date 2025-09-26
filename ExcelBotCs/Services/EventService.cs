using ExcelBotCs.Data;
using ExcelBotCs.Database.DTO;
using ExcelBotCs.Models.Config;
using Microsoft.Extensions.Options;

namespace ExcelBotCs.Services;

public class EventService : BaseDatabaseService<Event>
{
    public EventService(IOptions<DatabaseOptions> databaseConfig) : base(databaseConfig)
    {
    }
}