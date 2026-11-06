# Lottery persistence layer

The automated weekly lottery stores its data in the collections below. This page is the implementation reference
for the persistence layer (#108); the product contract remains
[the spec](../user-stories/automated-weekly-lottery.md) §6.

- Every collection derives from `BaseEntity`: `_id` (ObjectId), `DateCreated`, `DateModified`.
- `RoundKey` is `YYYY-MM-DD`, the round's scheduled run date in **Europe/Berlin**. It is the foreign key
  everywhere, including `lottery_winners` and `lottery_pot_ledger`.
- Enum-like fields are plain strings, not C# enums. The allowed values live as `public const string` members in
  classes under `ExcelBotCs/Modules/Lottery/` (`LotteryRoundStatus`, `LotteryFailedStep`,
  `LotteryPayoutStatus`, `LotteryPotLedgerType`, `LotteryPrizePoolStatus`). This keeps the database, the JSON API
  and the spec on the same vocabulary — including values that are not valid C# identifiers (`in-pool`,
  `weekly_increment`).
- `lottery_results` is the legacy archive. It is read-only for the application: no new writes, no shape changes,
  no deletes. The backfill only reads it.

## Collections

| Collection | Module | Repository | Notes |
|---|---|---|---|
| `lottery_rounds` | `LotteryRound` | `LotteryRoundRepository` | one document per round |
| `lottery_entries` | `LotteryEntry` | `LotteryEntryRepository` | full guess set per round |
| `lottery_winners` | `LotteryWinner` | `LotteryWinnerRepository` | winner rows, historical included |
| `lottery_pot_ledger` | `LotteryPotLedgerEntry` | `LotteryPotLedgerRepository` | append-only pot changes |
| `lottery_reminder_optins` | `LotteryReminderOptin` | `LotteryReminderOptinRepository` | one document per member |
| `lottery_awards` | `LotteryAward` | `LotteryAwardRepository` | extra-guess grants per round |
| `lottery_prize_pool` | `LotteryPrize` | `LotteryPrizePoolRepository` | non-Gil prize items |
| `lottery_settings` | `LotterySettings` | `LotterySettingsRepository` | singleton, `Key = "lottery"` |
| `lottery_settings_history` | `LotterySettingsHistory` | `LotterySettingsHistoryRepository` | settings audit trail |
| `lottery_results` | `LotteryResult` | `LotteryResultRepository` | legacy archive, read-only |

All repositories are registered in `Extensions/ServiceExtensions.cs` (`AddDatabaseRepositories`). The index
migration and the backfill are registered in `AddDomainServices`.

## Fields

`lottery_rounds` — `RoundKey`, `ScheduledAt` (UTC), `RunAt` (UTC, nullable), `Status`, `FailedStep` (nullable),
`Attempts`, `NextRetryAt` (nullable), `LastError` (nullable), `WinningNumber` (nullable), `PotBefore` /
`PotAfter` (nullable), `AnnouncementMessageId` (nullable), `PostingStartedAt` (nullable), `ClaimedAt` (nullable),
`ClaimOwner` (nullable).

`Status` is one of `scheduled`, `drawing`, `drawn`, `announced`, `completed`, `skipped`, `failed`, `voided`.
`FailedStep` is one of `draw`, `announcement`, `cleanup`.

`lottery_entries` — `RoundKey`, `DiscordId` (Int64), `Number` (1–99). There is no per-entry source field: whether
a guess was weekly or extra is derived from the award history in `lottery_awards`.

`lottery_winners` — `RoundKey`, `DiscordId` (nullable for historical rows), `DisplayName`, `Guess`, `GilPrize`,
`BonusItems` (string array, free text), `PayoutStatus` (`pending`, `paid`, `forfeited`),
`PayoutActorDiscordId` (nullable), `PayoutAt` (nullable), `PayoutNote` (nullable).

`lottery_pot_ledger` — `RoundKey` (nullable for the opening balance), `Type` (`reset`, `weekly_increment`,
`riddle`, `donation`, `correction`, `payout`), `Amount` (signed; `payout` is negative), `Reason`,
`ActorDiscordId` (nullable), `BalanceAfter`, `IdempotencyKey`. The current pot is `BalanceAfter` of the newest
entry; there is no separate balance document.

`lottery_reminder_optins` — `DiscordId`, `Enabled`.

`lottery_awards` — `RoundKey`, `DiscordId`, `Reason`, `AwardedByDiscordId` (nullable).

`lottery_prize_pool` — `Name`, `Status` (`in-pool`, `awarded`), `AwardedToDiscordId` (nullable),
`AwardedToName` (nullable), `AwardedRoundKey` (nullable), `AddedByDiscordId` (nullable), `AddedAt`.

`lottery_settings` — `Key` (`"lottery"`), `RunDay`, `RunTime` (`HH:mm`), `RunTimezone`, `ReminderDay`,
`ReminderTime`, `ReminderTimezone`, `RemindersEnabled`, `PauseAutomation`, `WinTemplate`, `NoWinTemplate`,
`Version`, `UpdatedByDiscordId` (nullable). Deployment config (token, channel ids) stays in appsettings/env.

`lottery_settings_history` — `SettingsKey`, `Version`, `ActorDiscordId` (nullable), `ChangedAt`, `OldValues`,
`NewValues`. Old/new values are `Dictionary<string, object?>` keyed by settings property name (e.g.
`"RunTime": "19:00"`); the driver stores and returns the underlying primitives, and the JSON API can expose the
dictionary without a conversion layer.

## Indexes

Indexes are declared by each repository (`GetIndexModels`) and created by
`ILotteryIndexInitializer.EnsureIndexesAsync()` (see [operations](operations.md)). Names are explicit and
deterministic, so later tickets can rely on them.

| Collection | Index name | Key | Unique |
|---|---|---|---|
| `lottery_rounds` | `ux_lottery_rounds_RoundKey` | `RoundKey` | yes |
| `lottery_rounds` | `ix_lottery_rounds_ScheduledAt` | `ScheduledAt` | no |
| `lottery_rounds` | `ix_lottery_rounds_Status` | `Status` | no |
| `lottery_entries` | `ux_lottery_entries_RoundKey_DiscordId_Number` | `RoundKey, DiscordId, Number` | yes |
| `lottery_entries` | `ix_lottery_entries_Number` | `Number` | no |
| `lottery_winners` | `ix_lottery_winners_RoundKey` | `RoundKey` | no |
| `lottery_winners` | `ix_lottery_winners_DiscordId` | `DiscordId` | no |
| `lottery_winners` | `ix_lottery_winners_Guess` | `Guess` | no |
| `lottery_pot_ledger` | `ix_lottery_pot_ledger_RoundKey_DateCreated` | `RoundKey, DateCreated` (descending) | no |
| `lottery_pot_ledger` | `ix_lottery_pot_ledger_Type` | `Type` | no |
| `lottery_pot_ledger` | `ux_lottery_pot_ledger_IdempotencyKey` | `IdempotencyKey` | yes |
| `lottery_reminder_optins` | `ux_lottery_reminder_optins_DiscordId` | `DiscordId` | yes |
| `lottery_awards` | `ix_lottery_awards_RoundKey_DiscordId` | `RoundKey, DiscordId` | no |
| `lottery_prize_pool` | `ix_lottery_prize_pool_Status` | `Status` | no |
| `lottery_prize_pool` | `ix_lottery_prize_pool_AwardedRoundKey` | `AwardedRoundKey` | no |
| `lottery_settings` | `ux_lottery_settings_Key` | `Key` | yes |
| `lottery_settings_history` | `ix_lottery_settings_history_SettingsKey_Version` | `SettingsKey, Version` | no |

There is deliberately **no** unique index on `lottery_winners`: historical rows share a round with `DiscordId:
null`, and a shared draw puts two winner rows in one round. The `Guess` index keeps "most winning number"
aggregation cheap; `ix_lottery_entries_Number` does the same for "most guessed number".

`lottery_results` gets no index from this migration.

## Two uniqueness guarantees

1. **One round per `RoundKey`.** `ux_lottery_rounds_RoundKey` makes a second `lottery_rounds` document for the
   same date fail with a duplicate-key error. The scheduler claims a round through
   `ILotteryRoundRepository`, and `ImportCompletedRoundAsync` is an upsert, so the backfill and the live run
   agree on this key.
2. **One ledger entry per `IdempotencyKey`.** `ux_lottery_pot_ledger_IdempotencyKey` makes a retried pot change
   fail instead of double-counting, so a crash-and-retry cannot apply the same reset/payout twice. A retry should
   re-read with `GetByIdempotencyKeyAsync` before writing.

`lottery_entries` additionally has a unique `(RoundKey, DiscordId, Number)` index; it is what makes re-running
the backfill harmless.

## Repository method shapes worth knowing

- `ILotteryRoundRepository.ImportCompletedRoundAsync(roundKey, runAtUtc, winningNumber)` — creates the round as
  `completed` when missing; when it exists it only refreshes `RunAt`, `Status` and `WinningNumber`. It never
  touches `PotBefore` / `PotAfter` / `AnnouncementMessageId` / claim metadata, so the backfill cannot clobber
  pot or message data. Returns `true` when the round was created.
- `ILotteryEntryRepository.AddIfMissingAsync(entry)` — insert-if-absent on `(RoundKey, DiscordId, Number)`;
  returns `true` when inserted. Used by the backfill; the live flow should use `CreateAsync`.
- `ILotteryWinnerRepository.GetLatestAsync(limit)` — the last-N read path: `RoundKey` descending, then creation
  order, so a shared draw contributes both rows.
- `ILotterySettingsRepository.SaveAsync(settings)` and `GetSingletonAsync()` — keyed by `"lottery"`. `SaveAsync`
  keeps the stored document's `_id` and `DateCreated` when replacing.
- `ILotteryPotLedgerRepository.GetByIdempotencyKeyAsync(key)` / `GetLatestAsync(limit)` — retry lookup and
  current-balance read.
