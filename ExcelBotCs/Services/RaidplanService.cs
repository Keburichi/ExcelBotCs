using ExcelBotCs.Data;
using ExcelBotCs.Database.DTO;
using ExcelBotCs.Models.Config;
using Microsoft.Extensions.Options;

namespace ExcelBotCs.Services;

public class RaidplanService : BaseDatabaseService<Raidplan>
{
    public RaidplanService(IOptions<DatabaseOptions> databaseConfig) : base(databaseConfig)
    {
    }
}