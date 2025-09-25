using ExcelBotCs.Data;
using ExcelBotCs.Database.DTO;
using ExcelBotCs.Models.Config;
using Microsoft.Extensions.Options;

namespace ExcelBotCs.Services;

public class MemberNoteService : BaseDatabaseService<MemberNote>
{
    public MemberNoteService(IOptions<DatabaseOptions> databaseConfig) : base(databaseConfig)
    {
    }
}