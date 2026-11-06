using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.Services.Lottery.Records;
using ExcelBotCs.Modules.Lottery;

namespace ExcelBotCs.Tests.Utils;

/// <summary>
///     A controllable clock. No lottery test may wait for a real Tuesday, so every schedule, retry and DST assertion
///     moves this clock instead of sleeping.
/// </summary>
public sealed class FakeTimeProvider : TimeProvider
{
	private DateTimeOffset _utcNow;

	public FakeTimeProvider(DateTimeOffset? utcNow = null)
	{
		_utcNow = utcNow ?? new DateTimeOffset(2026, 11, 10, 19, 0, 0, TimeSpan.Zero);
	}

	public override DateTimeOffset GetUtcNow()
	{
		return _utcNow;
	}

	public void Set(DateTimeOffset instant)
	{
		_utcNow = instant;
	}

	public void Advance(TimeSpan delta)
	{
		_utcNow += delta;
	}
}

/// <summary>The effective lottery settings, with the §7 defaults and a test-friendly constructor.</summary>
public static class TestLotterySettings
{
	public static LotteryEffectiveSettings Create(
		string runDay = "Tuesday",
		string runTime = "20:00",
		string runTimezone = "Europe/Berlin",
		bool automationEnabled = true,
		bool dryRun = false,
		bool pauseAutomation = false,
		long basePot = 1_000_000,
		long weeklyIncrement = 1_000_000,
		string winTemplate = "win {number}",
		string noWinTemplate = "no win {number}")
	{
		return new LotteryEffectiveSettings(runDay, runTime, runTimezone, "Monday", "18:00", "Europe/Berlin",
			false, pauseAutomation, winTemplate, noWinTemplate, 1, null, automationEnabled, dryRun, basePot,
			weeklyIncrement, []);
	}
}

/// <summary>An <see cref="ILotterySettingsService" /> that returns a fixed effective settings value.</summary>
public sealed class StubLotterySettingsService : ILotterySettingsService
{
	public StubLotterySettingsService(LotteryEffectiveSettings? settings = null)
	{
		Settings = settings ?? TestLotterySettings.Create();
	}

	public LotteryEffectiveSettings Settings { get; set; }

	public Task<LotteryEffectiveSettings> GetEffectiveAsync(CancellationToken cancellationToken = default)
	{
		return Task.FromResult(Settings);
	}

	public Task<LotterySettingsSaveResult> SaveAsync(LotterySettingsUpdate update, ulong? actorDiscordId,
		CancellationToken cancellationToken = default)
	{
		throw new NotSupportedException("The settings save is not part of this test double.");
	}
}

/// <summary>A deterministic draw; the lottery RNG itself is not exercised by the round tests.</summary>
public sealed class FakeLotteryNumberGenerator : ILotteryNumberGenerator
{
	private readonly Queue<int> _numbers = new();

	public int Calls { get; private set; }
	public int Draws { get; private set; }
	public int? DefaultNumber { get; set; } = 50;

	public FakeLotteryNumberGenerator(params int[] numbers)
	{
		foreach (var number in numbers) _numbers.Enqueue(number);
	}

	public int DrawWinningNumber()
	{
		Calls++;
		Draws++;
		return _numbers.Count > 0 ? _numbers.Dequeue() : DefaultNumber ?? 50;
	}
}

/// <summary>
///     The announcement seam: records what would have been posted and can fail on demand, which is how the
///     announcement-failure and resume paths are tested without Discord.
/// </summary>
public sealed class FakeLotteryAnnouncementPublisher : ILotteryAnnouncementPublisher
{
	public List<LotteryAnnouncement> Published { get; } = [];
	public List<string> VoidAnnotations { get; } = [];

	/// <summary>When set, <see cref="PublishAsync" /> throws this exception instead of recording a post.</summary>
	public Exception? PublishFailure { get; set; }

	/// <summary>When set, a resume finds this post id and adopts it instead of posting again.</summary>
	public string? ExistingPostId { get; set; }

	public Task<LotteryAnnouncementPost> PublishAsync(LotteryAnnouncement announcement,
		CancellationToken cancellationToken = default)
	{
		if (PublishFailure is { } failure) throw failure;

		Published.Add(announcement);
		return Task.FromResult(new LotteryAnnouncementPost($"message-{announcement.Round.RoundKey}"));
	}

	public Task<string?> FindExistingPostAsync(LotteryRound round, CancellationToken cancellationToken = default)
	{
		return Task.FromResult(ExistingPostId);
	}

	public Task AnnotateVoidedAsync(LotteryRound round, string reason,
		CancellationToken cancellationToken = default)
	{
		VoidAnnotations.Add($"{round.RoundKey}:{reason}");
		return Task.CompletedTask;
	}
}

/// <summary>
///     A controllable clock for the scheduler's own tests. Kept separate from <see cref="FakeTimeProvider" /> so a test
///     can move the clock without changing a runner's view of time.
/// </summary>
public static class TestInstants
{
	public static DateTimeOffset Utc(string instant)
	{
		return DateTimeOffset.Parse(instant + "Z", System.Globalization.CultureInfo.InvariantCulture);
	}

	public static DateTime UtcTime(string instant)
	{
		return Utc(instant).UtcDateTime;
	}
}

/// <summary>
///     Wraps the real entry repository and fails the next N writes, so the draw-failure path can be exercised without
///     a broken Mongo. The retry then runs against the same document store.
/// </summary>
public sealed class FlakyLotteryEntryRepository : ExcelBotCs.Database.Interfaces.ILotteryEntryRepository
{
	private readonly ExcelBotCs.Database.Interfaces.ILotteryEntryRepository _inner;

	public FlakyLotteryEntryRepository(ExcelBotCs.Database.Interfaces.ILotteryEntryRepository inner)
	{
		_inner = inner;
	}

	public int FailuresRemaining { get; set; }

	public Task<List<ExcelBotCs.Modules.Lottery.LotteryEntry>> GetAsync() => _inner.GetAsync();

	public Task<ExcelBotCs.Modules.Lottery.LotteryEntry?> GetAsync(string id) => _inner.GetAsync(id);

	public Task CreateAsync(ExcelBotCs.Modules.Lottery.LotteryEntry entity) => _inner.CreateAsync(entity);

	public Task UpdateAsync(string id, ExcelBotCs.Modules.Lottery.LotteryEntry updatedEntity)
	{
		return _inner.UpdateAsync(id, updatedEntity);
	}

	public Task DeleteAsync(string id) => _inner.DeleteAsync(id);

	public Task UpsertAsync(ExcelBotCs.Modules.Lottery.LotteryEntry entity) => _inner.UpsertAsync(entity);

	public IReadOnlyList<MongoDB.Driver.CreateIndexModel<ExcelBotCs.Modules.Lottery.LotteryEntry>> GetIndexModels()
	{
		return _inner.GetIndexModels();
	}

	public Task EnsureIndexesAsync() => _inner.EnsureIndexesAsync();

	public Task<List<ExcelBotCs.Modules.Lottery.LotteryEntry>> GetByRoundKeyAsync(string roundKey)
	{
		return _inner.GetByRoundKeyAsync(roundKey);
	}

	public Task<List<ExcelBotCs.Modules.Lottery.LotteryEntry>> GetByRoundKeyAndDiscordIdAsync(string roundKey,
		ulong discordId)
	{
		return _inner.GetByRoundKeyAndDiscordIdAsync(roundKey, discordId);
	}

	public Task<List<ExcelBotCs.Modules.Lottery.LotteryEntry>> GetByDiscordIdAsync(ulong discordId)
	{
		return _inner.GetByDiscordIdAsync(discordId);
	}

	public Task<bool> AddIfMissingAsync(ExcelBotCs.Modules.Lottery.LotteryEntry entry)
	{
		if (FailuresRemaining <= 0) return _inner.AddIfMissingAsync(entry);

		FailuresRemaining--;
		throw new InvalidOperationException("Simulated lottery entry write failure");
	}
}
