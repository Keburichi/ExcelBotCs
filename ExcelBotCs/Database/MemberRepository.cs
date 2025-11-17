using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ExcelBotCs.Database;

public class MemberRepository : BaseRepository<Member>, IMemberRepository
{
    public MemberRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
        : base(mongoClient, databaseOptions)
    {
    }

    private IAggregateFluent<BsonDocument> BuildAggregationPipeline()
    {
        // Define the pipeline stages as BsonDocuments for maximum reliability
        var lookupRolesStage = new BsonDocument("$lookup", new BsonDocument
        {
            { "from", "MemberRole" },
            { "localField", nameof(Member.RoleIds) },
            { "foreignField", nameof(MemberRole.DiscordId) },
            { "as", nameof(Member.Roles) }
        });

        var addFieldsStage = new BsonDocument("$addFields", new BsonDocument(
            "__ExperienceObjectIds", new BsonDocument(
                "$map", new BsonDocument
                {
                    { "input", new BsonDocument("$ifNull", new BsonArray { "$ExperienceIds", new BsonArray() }) },
                    { "as", "expId" },
                    { "in", new BsonDocument("$toObjectId", "$$expId") }
                }
            )
        ));

        var lookupFightsStage = new BsonDocument("$lookup", new BsonDocument
        {
            { "from", "Fight" },
            { "localField", "__ExperienceObjectIds" },
            { "foreignField", "_id" },
            { "as", nameof(Member.Experience) }
        });

        var projectStage = new BsonDocument("$project", new BsonDocument
        {
            { "__ExperienceObjectIds", 0 }
        });

        // Return as BsonDocument to preserve fields that have [BsonIgnore] attributes
        return Collection.Aggregate()
            .AppendStage<BsonDocument>(lookupRolesStage)
            .AppendStage<BsonDocument>(addFieldsStage)
            .AppendStage<BsonDocument>(lookupFightsStage)
            .AppendStage<BsonDocument>(projectStage);
    }

    private Member DeserializeMember(BsonDocument doc)
    {
        // Deserialize the base Member object (ignores Roles and Experience due to [BsonIgnore])
        var member = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<Member>(doc);

        // Manually deserialize the Roles array
        if (doc.Contains("Roles") && doc["Roles"].IsBsonArray)
        {
            member.Roles = doc["Roles"].AsBsonArray
                .Select(r => MongoDB.Bson.Serialization.BsonSerializer.Deserialize<MemberRole>(r.AsBsonDocument))
                .ToList();
        }

        // Manually deserialize the Experience array
        if (doc.Contains("Experience") && doc["Experience"].IsBsonArray)
        {
            member.Experience = doc["Experience"].AsBsonArray
                .Select(e => MongoDB.Bson.Serialization.BsonSerializer.Deserialize<Fight>(e.AsBsonDocument))
                .ToList();
        }

        return member;
    }

    public override async Task<List<Member>> GetAsync()
    {
        var pipeline = BuildAggregationPipeline();
        var documents = await pipeline.ToListAsync();
        return documents.Select(DeserializeMember).ToList();
    }

    public override async Task<Member?> GetAsync(string id)
    {
        var pipeline = BuildAggregationPipeline()
            .Match(Builders<BsonDocument>.Filter.Eq("_id", new ObjectId(id)));
        var document = await pipeline.FirstOrDefaultAsync();
        return document != null ? DeserializeMember(document) : null;
    }

    public async Task<Member> GetByDiscordId(string discordId)
    {
        var pipeline = BuildAggregationPipeline()
            .Match(Builders<BsonDocument>.Filter.Eq("DiscordId", discordId));
        var document = await pipeline.FirstOrDefaultAsync();
        return document != null ? DeserializeMember(document) : null;
    }

    public async Task<Member> GetByLodestoneId(string lodestoneId)
    {
        var pipeline = BuildAggregationPipeline()
            .Match(Builders<BsonDocument>.Filter.Eq("LodestoneId", lodestoneId));
        var document = await pipeline.FirstOrDefaultAsync();
        return document != null ? DeserializeMember(document) : null;
    }

    public async Task<Member> GetByDiscordId(ulong discordId)
        => await GetByDiscordId(discordId.ToString());
}