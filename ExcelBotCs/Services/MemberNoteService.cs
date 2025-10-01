using ExcelBotCs.Data;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database;
using Microsoft.Extensions.Options;

namespace ExcelBotCs.Services;

public class MemberNoteService : BaseDatabaseService<MemberNote>
{
    public MemberNoteService(IOptions<DatabaseOptions> databaseConfig) : base(databaseConfig)
    {
    }
}