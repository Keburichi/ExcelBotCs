using Xunit;

namespace ExcelBotCs.TestFramework.Database;

[CollectionDefinition("MongoDB")]
public class MongoDbCollection : ICollectionFixture<MongoDbFixture>;
