using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Linq.Expressions;

namespace ExcelBotCs.Data;
public class Repository<T> where T : DatabaseObject
{
	private readonly IMongoCollection<T> _collection;

	public Repository(IMongoCollection<T> collection) => _collection = collection;

	public async Task Insert(T item) => await _collection.InsertOneAsync(item);

	public async Task<bool> Delete(T item)
	{
		var result = await _collection.DeleteOneAsync(MatchById(item));
		return result.DeletedCount == 1;
	}

	public async Task<int> Delete(Expression<Func<T, bool>> predicate)
	{
		var result = await _collection.DeleteManyAsync(predicate);
		return (int)result.DeletedCount;
	}

	public async Task DeleteAll() => await _collection.DeleteManyAsync(_ => true);

	public async Task Update(T item)
	{
		item.DateModified = DateTime.UtcNow;
		await _collection.ReplaceOneAsync(MatchById(item), item);
	}

	public async Task Upsert(T item)
	{
		item.DateModified = DateTime.UtcNow;
		await _collection.ReplaceOneAsync(MatchById(item), item, new ReplaceOptions() { IsUpsert = true });
	}

	public IQueryable<T> Where(Expression<Func<T, bool>> predicate) => _collection.AsQueryable().Where(predicate);

	private static FilterDefinition<T>? MatchById(T item) => Builders<T>.Filter.Eq(r => r.Id, item.Id);

	public IQueryable<T> Query => _collection.AsQueryable();
}