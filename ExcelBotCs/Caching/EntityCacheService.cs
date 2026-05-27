using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO.Cache;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ExcelBotCs.Caching;

public class EntityCacheService : IEntityCacheService
{
    private readonly IEntityCacheStore<Boss> _bossStore;
    private readonly IEntityCacheStore<Fight> _fightStore;
    private readonly IEntityCacheStore<Member> _memberStore;
    private readonly IEntityCacheStore<MemberRole> _memberRoleStore;

    private readonly IBossRepository _bossRepository;
    private readonly IFightRepository _fightRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly IMemberRoleRepository _memberRoleRepository;

    private readonly IMongoClient _mongoClient;
    private readonly IOptions<DatabaseOptions> _dbOptions;
    private readonly ILogger<EntityCacheService> _logger;

    public IReadOnlyList<string> EntityTypes { get; } = [
        nameof(Boss), nameof(Fight), nameof(Member), nameof(MemberRole)
    ];

    public EntityCacheService(
        IEntityCacheStore<Boss> bossStore,
        IEntityCacheStore<Fight> fightStore,
        IEntityCacheStore<Member> memberStore,
        IEntityCacheStore<MemberRole> memberRoleStore,
        IBossRepository bossRepository,
        IFightRepository fightRepository,
        IMemberRepository memberRepository,
        IMemberRoleRepository memberRoleRepository,
        IMongoClient mongoClient,
        IOptions<DatabaseOptions> dbOptions,
        ILogger<EntityCacheService> logger)
    {
        _bossStore = bossStore;
        _fightStore = fightStore;
        _memberStore = memberStore;
        _memberRoleStore = memberRoleStore;
        _bossRepository = bossRepository;
        _fightRepository = fightRepository;
        _memberRepository = memberRepository;
        _memberRoleRepository = memberRoleRepository;
        _mongoClient = mongoClient;
        _dbOptions = dbOptions;
        _logger = logger;
    }

    public async Task WarmAllAsync()
    {
        _logger.LogInformation("Warming entity caches...");

        await FillBossesAsync();
        await FillFightsAsync();
        await FillMemberRolesAsync();
        await FillMembersAsync();

        _logger.LogInformation(
            "Cache warmed: Bosses={BossCount}, Fights={FightCount}, Members={MemberCount}, MemberRoles={RoleCount}",
            _bossStore.Count, _fightStore.Count, _memberStore.Count, _memberRoleStore.Count);
    }

    public async Task RefreshIfStaleAsync()
    {
        var db = _mongoClient.GetDatabase(_dbOptions.Value.DatabaseName);
        var refreshedMembers = false;

        if (await IsCollectionStale(db, "Boss", _bossStore.GetMaxDateModified()))
        {
            await FillBossesAsync();
            _logger.LogInformation("Cache refreshed: Bosses ({Count} items)", _bossStore.Count);
        }

        if (await IsCollectionStale(db, "Fight", _fightStore.GetMaxDateModified()))
        {
            await FillFightsAsync();
            _logger.LogInformation("Cache refreshed: Fights ({Count} items)", _fightStore.Count);
            // Cascade: Members reference Fights via Experience
            await FillMembersAsync();
            refreshedMembers = true;
        }

        if (await IsCollectionStale(db, "MemberRole", _memberRoleStore.GetMaxDateModified()))
        {
            await FillMemberRolesAsync();
            _logger.LogInformation("Cache refreshed: MemberRoles ({Count} items)", _memberRoleStore.Count);
            // Cascade: Members reference MemberRoles via Roles
            if (!refreshedMembers)
            {
                await FillMembersAsync();
                refreshedMembers = true;
            }
        }

        if (!refreshedMembers && await IsCollectionStale(db, "Member", _memberStore.GetMaxDateModified()))
        {
            await FillMembersAsync();
            _logger.LogInformation("Cache refreshed: Members ({Count} items)", _memberStore.Count);
        }
    }

    public CacheStatusResponse GetStatus()
    {
        return new CacheStatusResponse
        {
            Entities =
            [
                BuildStatus(nameof(Boss), _bossStore),
                BuildStatus(nameof(Fight), _fightStore),
                BuildStatus(nameof(Member), _memberStore),
                BuildStatus(nameof(MemberRole), _memberRoleStore)
            ]
        };
    }

    public async Task ClearAsync(string entityType)
    {
        switch (entityType)
        {
            case nameof(Boss):
                _bossStore.Clear();
                break;
            case nameof(Fight):
                _fightStore.Clear();
                break;
            case nameof(Member):
                _memberStore.Clear();
                break;
            case nameof(MemberRole):
                _memberRoleStore.Clear();
                break;
            default:
                throw new ArgumentException($"Unknown entity type: {entityType}");
        }

        await Task.CompletedTask;
    }

    public async Task FillAsync(string entityType)
    {
        switch (entityType)
        {
            case nameof(Boss):
                await FillBossesAsync();
                break;
            case nameof(Fight):
                await FillFightsAsync();
                break;
            case nameof(Member):
                await FillMembersAsync();
                break;
            case nameof(MemberRole):
                await FillMemberRolesAsync();
                break;
            default:
                throw new ArgumentException($"Unknown entity type: {entityType}");
        }
    }

    public async Task ClearAllAsync()
    {
        _bossStore.Clear();
        _fightStore.Clear();
        _memberStore.Clear();
        _memberRoleStore.Clear();
        await Task.CompletedTask;
    }

    public async Task FillAllAsync() => await WarmAllAsync();

    public object GetAllEntities(string entityType)
    {
        return entityType switch
        {
            nameof(Boss) => _bossStore.GetAll(),
            nameof(Fight) => _fightStore.GetAll(),
            nameof(Member) => _memberStore.GetAll(),
            nameof(MemberRole) => _memberRoleStore.GetAll(),
            _ => throw new ArgumentException($"Unknown entity type: {entityType}")
        };
    }

    private async Task FillBossesAsync()
    {
        var bosses = await _bossRepository.GetAsync();
        _bossStore.SetAll(bosses);
    }

    private async Task FillFightsAsync()
    {
        var fights = await _fightRepository.GetAsync();
        _fightStore.SetAll(fights);
    }

    private async Task FillMembersAsync()
    {
        var members = await _memberRepository.GetAsync();
        _memberStore.SetAll(members);
    }

    private async Task FillMemberRolesAsync()
    {
        var roles = await _memberRoleRepository.GetAsync();
        _memberRoleStore.SetAll(roles);
    }

    private async Task<bool> IsCollectionStale(IMongoDatabase db, string collectionName, DateTime? cachedMax)
    {
        var collection = db.GetCollection<BsonDocument>(collectionName);

        var pipeline = new[]
        {
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", BsonNull.Value },
                { "maxDate", new BsonDocument("$max", "$DateModified") }
            })
        };

        var result = await collection.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync();
        if (result == null) return false;

        var dbMax = result["maxDate"].ToUniversalTime();
        if (cachedMax == null) return true;

        return dbMax > cachedMax.Value;
    }

    private static CacheEntityStatus BuildStatus<T>(string name, IEntityCacheStore<T> store) where T : BaseEntity
    {
        return new CacheEntityStatus
        {
            EntityType = name,
            Count = store.Count,
            LastRefreshed = store.LastRefreshed,
            MaxDateModified = store.GetMaxDateModified(),
            IsPopulated = store.IsPopulated
        };
    }

}
