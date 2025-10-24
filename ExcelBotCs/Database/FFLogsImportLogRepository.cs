using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Database;

public class FFLogsImportLogRepository : BaseRepository<FFLogsImportLog>, IFFLogsImportLogRepository
{
    public FFLogsImportLogRepository(IMongoClient client, IOptions<DatabaseOptions> options)
        : base(client, options)
    {
    }
}
