using MongoDB.Driver;

namespace ExcelBotCs.Database;

/// <summary>
///     Recognises the duplicate-key write error the unique indexes turn into. The lottery has no multi-document
///     transactions, so "insert, and treat a duplicate key as success" is how the base-pot entry, the round creation
///     and the ledger entries stay idempotent.
/// </summary>
public static class MongoDuplicateKey
{
	public static bool IsDuplicate(Exception exception)
	{
		return exception switch
		{
			MongoWriteException write => write.WriteError?.Category == ServerErrorCategory.DuplicateKey,
			MongoBulkWriteException bulk => bulk.WriteErrors.Any(error =>
				error.Category == ServerErrorCategory.DuplicateKey),
			MongoCommandException command => command.Code == 11000,
			_ => false
		};
	}
}
