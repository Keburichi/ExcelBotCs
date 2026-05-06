using ExcelBotCs.Database;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.TestFramework.Database;

public class TestRepository : BaseRepository<TestDatabaseEntity>
{
    public TestRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions) : base(mongoClient,
        databaseOptions)
    {
    }
}

public class TestDatabaseEntity : BaseEntity
{
    public string Name { get; set; }
    public string Description { get; set; }
}