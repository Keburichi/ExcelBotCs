# Operations — index migration and the one-time backfill

Two data steps belong to the cutover (spec §12 steps 2–4). Both are one-shot, idempotent, and safe to re-run.
They are exposed as command-line switches instead of HTTP endpoints: the application builds its service
container (so it reads the real database configuration) and then exits **without starting the web server or the
Discord gateway**.

The full operator sequence around them — deploy with the automation off, seed, backfill, dry-run week, promote,
monitor and roll back — is the [cutover runbook](cutover-runbook.md). This page is the reference for the two
commands themselves.

## Index migration

Collections are created by the repositories themselves; the indexes need an explicit step because the base
repository only creates collections. The migration creates the indexes listed in
[data-model](data-model.md#indexes) and touches nothing else — in particular it does not index the legacy
`lottery_results` archive.

```bash
dotnet run --project ExcelBotCs -- --lottery-ensure-indexes
```

`EnsureIndexesAsync` is idempotent: MongoDB treats creating an index that already exists with the same name and
key as a no-op, so the command can run on every deployment. It fails loudly if a collection already contains data
that violates a unique index (for example two `lottery_rounds` with the same `RoundKey`); fix the data first, then
re-run.

The same step is available in code as `ILotteryIndexInitializer.EnsureIndexesAsync()`.

## The one-time `lottery_results` backfill

Imports the legacy archive into the new collections, per spec §6 "Backfill":

1. reads every `lottery_results` document;
2. upserts one `lottery_rounds` document per archive result, keyed by the result's **local date in
   Europe/Berlin** (`2026-07-21T23:30Z` belongs to `2026-07-22`), with `ScheduledAt` and `RunAt` = the archive
   timestamp, `Status: "completed"` and the archived winning number; the pot fields and the announcement
   message id stay `null`;
3. upserts `lottery_entries` from the embedded guesses;
4. creates **no** winners — the manual 35-row seed is the canonical winner history;
5. never writes to, deletes from, or re-shapes `lottery_results`.

```bash
dotnet run --project ExcelBotCs -- --lottery-backfill
```

The two switches can be combined:

```bash
dotnet run --project ExcelBotCs -- --lottery-ensure-indexes --lottery-backfill
```

Run it once, at a round boundary, after a Mongo backup. Exit code `0` means the run finished (re-runs are
expected to report zero created rounds and entries); `1` means it threw and the log has the cause.

The result object (`LotteryBackfillResult`) reports `ResultsProcessed`, `RoundsCreated`, `RoundsRefreshed`,
`EntriesCreated`, `EntriesAlreadyPresent` and `WinnersCreated` (always `0`).

### Idempotency

Re-running is safe and leaves the data byte-identical:

- rounds are matched on the unique `RoundKey`; an existing round only has `RunAt`, `Status` and `WinningNumber`
  refreshed — `ScheduledAt`, `DateCreated`, the pot fields, the announcement message id and claim metadata are
  preserved;
- entries are matched on the unique `(RoundKey, DiscordId, Number)`;
- winners and the archive are never written.

At cutover no round created by the new flow exists yet, so the refresh only ever touches imported rounds. If the
backfill is run later, against a database that already has live rounds, note that it sets `Status` to `completed`
for the round keys present in the archive — that is the spec's rule, not an accident.

The test suite pins this: `LotteryArchiveBackfillServiceTests.BackfillAsync_RunTwice_YieldsIdenticalData`
compares whole-collection snapshots before and after the second run and asserts zero winners.

### What to check afterwards

- `db.lottery_rounds.countDocuments({ Status: "completed" })` matches the archive's result count (except for
  archive results that share a local date, which map to one round);
- every archive result's guesses are present in `lottery_entries` for its round key;
- `db.lottery_winners.countDocuments()` still equals the number of manually seeded rows (35 at cutover);
- `db.lottery_results` is unchanged.

## Rollback

The collections are additive. Rolling back is a deploy of the previous version plus disabling the automation;
nothing in this migration deletes data and `lottery_results` remains. If the backfill must be undone on a scratch
database, drop the new lottery collections — never `lottery_results` — and start the cutover again.

## Related

- [cutover, rollout and rollback runbook](cutover-runbook.md) — the sequence these commands sit in
- [data model and indexes](data-model.md)
- [manual seed](manual-seed.md) — the 35 historical winners and the opening pot
- [configuration](configuration.md) — the deployment flags, dry run and the health detail
- [the spec](../user-stories/automated-weekly-lottery.md) §6 and §12
