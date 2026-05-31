using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Modules.Lottery;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Database;

public class BonusLotteryResultRepository : BaseRepository<BonusLotteryResult>, IBonusLotteryResultRepository
{
    public BonusLotteryResultRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
        : base(mongoClient, databaseOptions)
    {
    }

    protected override string GetCollectionName()
    {
        return "bonus_lottery_results";
    }
}
