using ExcelBotCs.Data;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database;
using Microsoft.Extensions.Options;

namespace ExcelBotCs.Services;

public class RaidplanService : BaseDatabaseService<Raidplan>
{
    public RaidplanService(IOptions<DatabaseOptions> databaseConfig) : base(databaseConfig)
    {
    }
}