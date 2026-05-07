using ExcelBotCs.TestFramework.Database;
using Xunit;

namespace ExcelBotCs.Tests.Utils;

[CollectionDefinition("MongoDB")]
public class MongoDbCollection : ICollectionFixture<MongoDbFixture>;
