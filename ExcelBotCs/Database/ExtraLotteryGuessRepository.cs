using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Modules.Lottery;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Database;

public class ExtraLotteryGuessRepository : BaseRepository<ExtraLotteryGuess>, IExtraLotteryGuessRepository
{
    public ExtraLotteryGuessRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
        : base(mongoClient, databaseOptions)
    {
    }

    protected override string GetCollectionName()
    {
        return "extra_lottery_guesses";
    }

    public async Task<List<ExtraLotteryGuess>> GetByDiscordIdAsync(ulong discordId)
    {
        return await Collection.Find(g => g.DiscordId == discordId).ToListAsync();
    }

    public async Task DeleteAllAsync()
    {
        await Collection.DeleteManyAsync(_ => true);
    }
}