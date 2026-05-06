using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Modules.Lottery;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Database;

public class LotteryGuessRepository : BaseRepository<LotteryGuess>, ILotteryGuessRepository
{
    public LotteryGuessRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
        : base(mongoClient, databaseOptions)
    {
    }

    protected override string GetCollectionName()
    {
        return "lottery_guesses";
    }

    public async Task<List<LotteryGuess>> GetByDiscordIdAsync(ulong discordId)
    {
        return await Collection.Find(g => g.DiscordId == discordId).ToListAsync();
    }

    public async Task DeleteByDiscordIdAndNumberAsync(ulong discordId, int number)
    {
        await Collection.DeleteOneAsync(g => g.DiscordId == discordId && g.Number == number);
    }

    public async Task DeleteAllAsync()
    {
        await Collection.DeleteManyAsync(_ => true);
    }
}