using ExcelBotCs.Database;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.TestFramework.Database;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Tests.Database;

public class FFLogsImportLogRepositoryTests : MongoDbTest
{
    private IFFLogsImportLogRepository _repository;

    protected override void InitializeRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
    {
        _repository = new FFLogsImportLogRepository(mongoClient, databaseOptions);
    }
}