# Automated weekly lottery — implementation spec

Status: handoff spec, assembled from the wayfinder map [#97](https://github.com/Keburichi/ExcelBotCs/issues/97).
Date: 2026-11-05.
Source decisions: #98–#106 (see the map's Decisions-so-far). The map's destination is this spec.

## 1. Summary

Replace the manual weekly lottery run and post with an automated flow:

- a scheduler runs the draw on a configurable day and time;
- the bot posts the weekly announcement, with editable winner/no-winner result templates;
- opted-in members get an automatic reminder with a one-click opt-out;
- the pot is an auditable Gil ledger, with a separate pool of non-Gil prizes;
- the full winner history is a deep-linkable web timeline;
- admins get a dedicated `/lottery/admin` page for the schedule, pot, prize pool, payouts, templates, and failure recovery;
- failures are retried, visible, and alerted; the live lottery is never double-run.

Payouts remain manual in-game. The system records status only.

## 2. Goals and non-goals

### Goals

- No admin has to run the draw or post the weekly message.
- The pot, winners, and prizes are persisted and auditable.
- Members can see the full history on the web; Discord keeps the last five.
- Members can opt in to reminders; only opted-in members are pinged.
- A failed run cannot double-draw, double-pay, or silently lose guesses.
- The rollout is safe: feature flag off, dry-run first, manual seed, one-time backfill, kill-switch rollback.

### Non-goals

- Automating the in-game Gil trade or item delivery. The admin marks payouts.
- Riddle-system integration or automatic pot credits from riddle rewards.
- Automatically awarding event guesses; the event flow already does that.
- Lottery statistics UI. The schema is ready; the view is a follow-up.
- Multi-guild or multi-tenant support.
- Changing the guessing UX, the RNG, or the 1–99 range.

## 3. Domain rules

Terms are in `GLOSSARY.md`. The short rules:

- Base pot: 1,000,000 Gil. Week 1 starts there.
- No winner: next pot = current pot + 1,000,000 Gil.
- Winner: each winner takes a share of the pot at the draw; the next pot resets to exactly 1,000,000 Gil. Nothing carries over.
- A winner never receives the post-increment pot.
- Top-ups/corrections credited before the draw are included; after the draw they apply to the next pot.
- Multiple winners split the pot evenly. Each share is floored to whole Gil; the remainder (at most winners−1 Gil) goes to the winner whose winning guess was recorded earliest. The shares sum to the pot.
- The pot ledger is append-only. Corrections cannot take the pot below 1,000,000 Gil.
- Only admins may add a top-up or correction. Officers keep the existing slash commands.
- A prize item is a non-Gil prize in the prize pool; it is indivisible, stays in the pool until assigned, and is assigned to a specific winner. It does not change the Gil balance.
- Payout status is `pending`, `paid`, or `forfeited`, with actor, time, and note. A forfeited prize may be returned to the pot or reassigned by an admin as an audited correction.

After a win, a bonus/prize item attached to the winner is shown next to the prize (`+ Chaotic Mount`). The pot summary may show `21,000,000 Gil + 2 prizes` while the pool has items.

## 4. Current code and constraints

- ASP.NET Core + Discord.Net + MongoDB + Vue 3; single replica (README).
- Lottery path: `Services/Lottery/LotteryService.cs`, `Modules/Lottery/LotteryInteraction.cs`, `Controllers/LotteryController.cs`, `Database/Lottery*Repository.cs`, `ClientApp/src/components/lottery/*`.
- Existing run: admin-only, draws 1–99 with `Prng.NextInt(0, 99)`, saves `LotteryResult`, posts a short message + participants, flushes guesses. No scheduler, pot, or winner history.
- Existing reminder: officer-gated, filters to previous participants and takes the three *oldest* results (bug); replaced.
- Existing web: `LotteryView.vue`, `LotteryAdmin.vue`; the admin section is embedded in the lottery page.
- `BaseEntity`: `_id` (ObjectId), `DateCreated`, `DateModified`. Enum-like values are stored as strings (`[BsonRepresentation(BsonType.String)]`).
- Test suite: xUnit + Testcontainers Mongo + in-memory TestServer; Discord mocked. `dotnet test` is unreliable on this machine; use `dotnet vstest ExcelBotCs.Tests/bin/Debug/net9.0/ExcelBotCs.Tests.dll` (see `AGENTS.md`).

## 5. Target behaviour

### 5.1 Schedule and round lifecycle

- Default run: **Tuesday 20:00 Europe/Berlin**. Day, time, and timezone are admin-editable in `/lottery/admin`.
- A round is identified by `RoundKey` = the scheduled run date in Europe/Berlin, `YYYY-MM-DD`. One round per date; a second claim returns "already running/run".
- No separate guess lock. Members may guess or change until the run starts at the scheduled time. The post states the configured time, not "end of Monday."
- The run works without an admin Discord user (system actor). The manual admin **Run now** is a live, confirmed escape hatch.
- Idempotent per round: unique `RoundKey`; one conditional claim; retries resume.
- Catch-up: on restart, run the most recent missed round once; mark older missed dates `skipped`; no pot growth for skipped/missed dates.
- Skip: `skipped`; no draw, no pot change, guesses carry to the next round; the base weekly allowance is not reset.
- Reschedule: move one round; guesses stay; the round key follows the new date; the pot is untouched until the run.
- Redo: retry a pending announcement with the same result; never silently re-draw. A completed/wrong round is voided (ledger reversed, announcement annotated) and a new round is run; audited.
- DST: use a timezone-aware clock; the run is a local time, the round key is the local date; test both transitions.

### 5.2 Guesses and extra guesses

- Existing `/lottery guess`, `/lottery luckydip`, `/lottery change`, `/lottery view`, `/lottery unused`, `/lottery whoguessed` and the web lottery page remain.
- One base guess per member per round; extra guesses from events or awards. A member cannot guess the same number twice in a round.
- `lottery_entries` stores the full guess set for a round. `lottery_awards` stores every extra guess awarded (Discord id, reason, awarded by, time).
- Live guesses are cleared only at `completed`, after the announcement was posted. On skip, they carry.
- The old `extra_lottery_guesses` records are flushed after the round; the new `lottery_awards` preserves the audit.

### 5.3 Draw and prize calculation

- Draw the winning number from 1–99 using the existing `Prng` (cryptographic). Store the winning number and run time on the round.
- Compute winners from the round's entries.
- No winner: write a `weekly_increment` ledger entry (+1M); round `PotAfter` = old + 1M.
- Winner(s): prize = the pot at the draw, including top-ups credited before the draw. Split per the rules. Write a `payout` ledger entry (−pot) and a `reset` ledger entry (+1M); round `PotAfter` = 1M. The winner's `lottery_winners` record stores their share.
- The announcement shows the next pot: post-increment after a no-win, post-reset after a win.
- No guesses: the draw still runs; no winner; pot +1M.
- A winner who left the FC still wins; the announcement uses their display name and mentions them only if still mentionable; payout pending.

### 5.4 Pot ledger

Append-only `lottery_pot_ledger` entries:

| Type | Sign | Meaning |
|---|---|---|
| `reset` | + | initial base credit and post-win return to base |
| `weekly_increment` | + | 1M after a no-win |
| `riddle` | + | riddle top-up |
| `donation` | + | donation top-up |
| `correction` | ± | audited manual adjustment, never below 1M |
| `payout` | − | prize debit at a draw |

Every entry stores amount, reason, actor, balance-after, and an idempotency key. The current pot is derived from the ledger.

### 5.5 Non-Gil prize pool

- Separate `lottery_prize_pool`: non-Gil prizes, independent of the Gil ledger.
- Admin can add/remove items; an item is indivisible.
- Items stay in the pool across rounds until assigned. On a win, the admin assigns items to specific winners (from the winner row or the prize row). Multiple winners/items are assigned individually.
- Unassigned items stay in the pool. Awarded items appear on the winner's payout row and the public history; the pool names/count appear in the pot summary and the announcement preview (`21,000,000 Gil + 2 prizes`).
- If a payout is forfeited or a winner leaves before receiving an item, an admin can reassign it or return it to the pool. Audited.

### 5.6 Announcement

- One **plain** message in the lottery channel, pinging the configured lottery member role.
- One logical post; no participants list; not pinned.
- The result block is editable in the admin Messages tab; the rest is generated.
- Placeholders: `{number}`, `{winners}`, `{prize}`, `{bonusItems}`, `{pot}`, `{nextPot}`, `{emoji}`.
- Default result templates:

```text
Winner:
the winning number was {number}! Congratulations to {winners}! 🎉
They take the {prize} pot{bonusItems}.
The jackpot resets to {nextPot} Gil for next week.

No winner:
the winning number was {number}! So no one won this week again {emoji}
The jackpot goes up to {pot}!!
```

- The fully assembled post:

```text
Hello <@&LOTTERY_MEMBER_ROLE>,

<filled result block>

Make sure to look out for new posts in <#EVENTS_CHANNEL> to earn extra lottery entries and if you wanna increase the jackpot even more, go and solve the riddle in <#RIDDLES_CHANNEL>!

You can make the guesses for this week now! Remember the rules are:
1) Number is a natural number between 1-99 (1 and 99 included)
2) For every FC event you participate in, you get an extra guess to your one weekly guess!

Check the pinned posts for an explanation on commands.
Submit your guesses by <RUN_DAY> <RUN_TIME>, when the draw runs.

Current pot: <POT>

Last 5 winners:
<LAST_FIVE>

Full history: <WEB_BASE>/lottery/history
```

- Visible text: ~928 characters for a no-win and ~1,003 for a win; raw Discord markup keeps it under ~1,100; the 2,000-character bot limit is not at risk.
- Multiple winners: `Congratulations to {winners}!`; `They split the {prize} pot.`; each winner gets a last-five row. A bonus/prize item is named next to its winner.
- The last five are the five most recent winner records, newest first; the current winner is included; a shared draw contributes multiple rows; historical names are plain text; the current winner is mentioned only in the headline.
- `:kekDoggo:` is posted as `<:kekDoggo:EMOJI_ID>`; fall back to the literal text if no id is configured.
- The role/channel/emoji IDs are deployment config, read-only in the admin page.
- Dry run posts the same message with a `[DRY RUN]` prefix to the log/test channel, with mentions suppressed.
- An invalid/over-limit template falls back to the default at run time and alerts admins.

### 5.7 Reminder and opt-in

- Member opt-in: web toggle on the lottery page, default **off**; mirrored by `/lottery reminders on|off`. The web record is the source of truth.
- Opt-in is by Discord id; FC members only. Retained through leave/rejoin; inactive while not a member; name changes are irrelevant; deleted accounts are skipped.
- Default reminder: **Monday 18:00 Europe/Berlin**, admin-editable. One post per week.
- One post in the lottery channel tagging only opted-in members with at least one unused guess, grouped by remaining count. No DMs.
- Copy:

```text
## Use your lottery guesses before <RUN_DAY> <RUN_TIME>
<2 guesses remaining: @A, @B
<1 guess remaining: @C
Only members who opted in are pinged. The draw runs <RUN_DAY> at <RUN_TIME>.
```

- Components: **Turn off reminders** (secondary, custom id `lottery_reminders:off`) and **Open lottery page** (link). The opt-out button sets the caller's opt-in false, replies ephemerally, is idempotent, affects only the caller, and works even after leaving the FC. The current post is not edited.
- Manual `/lottery remind` stays officer-gated; it sends the same opted-in-only post immediately; if nobody is eligible, it replies ephemerally.
- The global reminder toggle is admin-controlled; the env master `Enabled` flag wins.
- Silent when the toggle is off or nobody is opted in/eligible. No reminder for a skipped round; a rescheduled round uses the new time.
- Reminder failure is best-effort: 3 attempts / ~15 minutes; alert once; never block the draw.

### 5.8 History page

- Route: `/lottery/history` (History tab of the lottery page); `/lottery` is Play. The tab bar uses router links; deep links land on History.
- Members-only, same auth as the lottery page.
- Timeline of winner cards, newest first, one card per winner; shared draws show two cards with the same date and guess.
- Card fields: date (`DD.MM.YY`), winner (Discord avatar when known, otherwise initials), guess pill, Gil prize, prize item chip.
- Summary cards: current pot, last draw, **Winners recorded 35** (with "across 34 draws").
- Lazy loading: first batch of 10; scrolling near the bottom loads the next 10 via `IntersectionObserver`; a keyboard-accessible **Load more winners** fallback; a loading indicator; "All 35 winners loaded" at the end. No page numbers.
- Empty state: "No draws yet"; loading skeleton; cards stack on mobile.
- Payout status is admin-only and omitted; no statistics UI.
- The Discord post links to `<WEB_BASE>/lottery/history`.

### 5.9 Admin page

- Route: `/lottery/admin`, admin-only; linked from the lottery page. Member history stays at `/lottery/history`.
- Five tabs: **Overview**, **Schedule & reminders**, **Pot & payouts**, **Messages**, **Guesses**.
- **Overview:** this week status, run time, pot (Gil + prize count), reminder status; **Preview announcement**, **Run now** (confirm, live, irreversible), **Skip week** (confirm); **Recovery & danger** with **Retry announcement** (only when a run is pending) and **Void last draw**; **Needs attention** (pending payouts, pool prizes, opted-in count); last draw. A **Dry run now** action is available; a **Pause automation** toggle is shown with the status.
- **Schedule & reminders:** run day/time/timezone; reminder day/time/timezone; global reminder toggle; opted-in count; **Manage opt-ins**; **Send test reminder** (to the requesting admin only). Save is the only commit.
- **Pot & payouts:** pot adjustment (± amount, reason, 1M floor), recent ledger; additional prizes (add/remove/assign); winner payouts (**Mark paid**, **Forfeit**, **Assign prize**). This is the densest tab; split **Payouts** into a sixth tab if it grows.
- **Messages:** winner/no-winner result templates with the placeholder chips, character count, preview, reset-to-default, and save. The generated sections are shown read-only in the preview. Storage is `lottery_settings`; changes are audited.
- **Guesses:** existing award-guess flow.
- Permissions: the web admin page is admin-only. Officers keep `/lottery run`, `/lottery remind`, `/lottery award`.
- Confirmations: Run now, Skip, Void, Forfeit. No silent redo. Pot/payout/template/settings changes are audited.

### 5.10 Failure handling

State machine:

`scheduled → drawing → drawn → announced → completed`

with `skipped`, `failed`, and `voided` branches.

- Result, entries, winners, and ledger are written before the post.
- Live guesses are cleared only at `completed`.
- `posting` is written before the Discord send; on restart, search the lottery channel for the bot's recent message for this round and adopt the id; only post if none is found.
- Retries: draw/DB 5 attempts (30s/1m/2m/5m/10m); announcement same backoff up to 1 hour or until the next scheduled round; reminder 3 attempts / ~15 min. Retry the clock after a reconnect.
- A pending/failed round blocks the next scheduled round; after retry/void, #99 catch-up runs the latest missed round once.
- Bot offline at the scheduled time: draw and save anyway; post on reconnect.
- DB down: retry; if the slot is missed, catch up on restart; no pot growth for skipped/missed dates.
- Alerts: log channel; fallback lottery channel; admin ping only on `failed`/needs-admin; content includes round key, failed step, error, attempts, next retry, and a link to `/lottery/admin`.
- Admin Overview shows a failure badge, last error, and manual retry/void.
- `/health` exposes a `lottery` detail (healthy/degraded) without failing the whole container.

## 6. Data model

All collections derive from `BaseEntity` (`_id`, `DateCreated`, `DateModified`). Enum-like values are strings. `RoundKey` is the foreign key everywhere.

### `lottery_rounds`

| Field | Type | Notes |
|---|---|---|
| `RoundKey` | string | `YYYY-MM-DD` Europe/Berlin; unique |
| `ScheduledAt` | DateTime UTC | |
| `RunAt` | DateTime UTC? | |
| `Status` | string | `scheduled`, `drawing`, `drawn`, `announced`, `completed`, `skipped`, `failed`, `voided` |
| `FailedStep` | string? | `draw`, `announcement`, `cleanup` |
| `Attempts` | int | |
| `NextRetryAt` | DateTime? | |
| `LastError` | string? | |
| `WinningNumber` | int? | 1–99 |
| `PotBefore` | long? | null for historical/backfilled |
| `PotAfter` | long? | |
| `AnnouncementMessageId` | string? | |
| `PostingStartedAt` | DateTime? | reconciliation marker |
| `ClaimedAt` | DateTime? | claim/lock |
| `ClaimOwner` | string? | |

Indexes: unique `RoundKey`; `ScheduledAt`; `Status`.

### `lottery_entries`

| Field | Type | Notes |
|---|---|---|
| `RoundKey` | string | |
| `DiscordId` | ulong (Int64) | |
| `Number` | int | 1–99 |

Unique `(RoundKey, DiscordId, Number)`; index `Number` for stats. No per-entry source field; extra-guess awards are in `lottery_awards`.

### `lottery_winners`

| Field | Type | Notes |
|---|---|---|
| `RoundKey` | string | |
| `DiscordId` | ulong? | null for historical rows |
| `DisplayName` | string | |
| `Guess` | int | winning number |
| `GilPrize` | long | this winner's share |
| `BonusItems` | string[] | awarded non-Gil items (template placeholder `{bonusItems}`) |
| `PayoutStatus` | string | `pending`, `paid`, `forfeited` |
| `PayoutActorDiscordId` | ulong? | |
| `PayoutAt` | DateTime? | |
| `PayoutNote` | string? | |

Indexes: `RoundKey`, `DiscordId`, `Guess`. No unique index (historical rows share a round with null DiscordId).

### `lottery_pot_ledger`

| Field | Type | Notes |
|---|---|---|
| `RoundKey` | string? | null for the opening balance |
| `Type` | string | `reset`, `weekly_increment`, `riddle`, `donation`, `correction`, `payout` |
| `Amount` | long | signed |
| `Reason` | string | |
| `ActorDiscordId` | ulong? | |
| `BalanceAfter` | long | |
| `IdempotencyKey` | string | unique per entry; retries reuse it |

Indexes: `(RoundKey, DateCreated)`, `Type`, unique `IdempotencyKey`.

### `lottery_reminder_optins`

| Field | Type | Notes |
|---|---|---|
| `DiscordId` | ulong | unique |
| `Enabled` | bool | |

### `lottery_awards`

| Field | Type | Notes |
|---|---|---|
| `RoundKey` | string | |
| `DiscordId` | ulong | |
| `Reason` | string | |
| `AwardedByDiscordId` | ulong? | |

Index `(RoundKey, DiscordId)`.

### `lottery_prize_pool`

| Field | Type | Notes |
|---|---|---|
| `Name` | string | |
| `Status` | string | `in-pool`, `awarded` |
| `AwardedToDiscordId` | ulong? | |
| `AwardedToName` | string? | |
| `AwardedRoundKey` | string? | |
| `AddedByDiscordId` | ulong? | |
| `AddedAt` | DateTime | |

Indexes: `Status`, `AwardedRoundKey`.

### `lottery_settings` (singleton)

| Field | Type |
|---|---|
| `Key` | string (`"lottery"`, unique) |
| `RunDay` | string |
| `RunTime` | string (`HH:mm`) |
| `RunTimezone` | string (`Europe/Berlin`) |
| `ReminderDay` | string |
| `ReminderTime` | string |
| `ReminderTimezone` | string |
| `RemindersEnabled` | bool |
| `PauseAutomation` | bool |
| `WinTemplate` | string |
| `NoWinTemplate` | string |
| `Version` | int |
| `UpdatedByDiscordId` | ulong? |

### `lottery_settings_history`

| Field | Type |
|---|---|
| `SettingsKey` | string |
| `Version` | int |
| `ActorDiscordId` | ulong? |
| `ChangedAt` | DateTime |
| `OldValues` | object |
| `NewValues` | object |

Indexes: `(SettingsKey, Version)`.

### `lottery_results` archive

Keep as-is, read-only. The backfill migrates its rounds and entries; it does **not** synthesize winners. The 35 historical winners and the opening pot are manual inserts.

### Manual seed shapes

Historical winner (one document per source row; no auto seed):

```js
db.lottery_winners.insertOne({
  RoundKey: "2026-07-21",
  DiscordId: null,
  DisplayName: "Siegward",
  Guess: 56,
  GilPrize: NumberLong(80000000),
  BonusItems: ["Chaotic Mount"],
  PayoutStatus: "paid",
  PayoutActorDiscordId: null,
  PayoutAt: null,
  PayoutNote: "historical record",
  DateCreated: ISODate("2026-07-21T18:00:00Z"),
  DateModified: ISODate("2026-07-21T18:00:00Z")
});
```

Opening pot (exactly one document at cutover; no auto set):

```js
db.lottery_pot_ledger.insertOne({
  RoundKey: null,
  Type: "correction",
  Amount: NumberLong(21000000),
  Reason: "opening balance from manual history",
  ActorDiscordId: null,
  BalanceAfter: NumberLong(21000000),
  IdempotencyKey: "opening-balance",
  DateCreated: ISODate(),
  DateModified: ISODate()
});
```

Mapping rules: `RoundKey` = `DD.MM.YY` → `YYYY-MM-DD`; rows 25–26 share `2026-01-01`; row 21 guess 30; preserve names and prize-item spellings (including `Fuax Mount`); `BonusItems` empty when absent; `PayoutStatus: "paid"`; no `lottery_entries` for historical rows unless the full guess set is known; no historical `payout` ledger entries.

### Backfill

One-time, manually triggered, idempotent:

1. Read each `lottery_results` document.
2. Upsert `lottery_rounds` by `RoundKey` = the result's local date; `RunAt`, `Status: "completed"`, `WinningNumber`; leave pot fields and message id null.
3. Upsert `lottery_entries` from the embedded guesses; unique index makes it idempotent.
4. Do **not** create winners from old results; the manual 35-row seed is canonical.
5. Keep `lottery_results` read-only.

## 7. Configuration

### Deployment config (appsettings/env; read-only in the admin page)

| Key | Default | Notes |
|---|---|---|
| `DiscordBot:Token` | — | existing |
| `DiscordBot:GuildId` | — | existing |
| `DiscordBot:LotteryChannel` | — | existing |
| `DiscordBot:EventsChannel` | — | existing |
| `DiscordBot:RiddlesChannel` | — | new |
| `DiscordBot:LogChannel` | `0` | existing; alerts fall back to the lottery channel when 0 |
| `DiscordBot:LotteryMemberRoleId` | — | new; the role pinged by the announcement |
| `DiscordBot:LotteryCustomEmojiId` | — | new; `kekDoggo` id |
| `WebBaseUrl` (or `Jwt:Issuer`) | — | history link |
| `Lottery:Automation:Enabled` | `false` | deploy kill switch for the scheduler |
| `Lottery:Automation:DryRun` | `true` | first rollout |
| `Lottery:Automation:BasePot` | `1000000` | deployment config |
| `Lottery:Automation:WeeklyIncrement` | `1000000` | deployment config |
| Retry/alert limits | see #105 | 5 draw attempts, 1h announcement, 3 reminder attempts |

### Operational settings (`lottery_settings`, admin-editable)

Run day/time/timezone; reminder day/time/timezone; `RemindersEnabled`; `PauseAutomation`; `WinTemplate`; `NoWinTemplate`; version/updated-by.

### Precedence and validation

- env master flags > DB operational values > appsettings defaults.
- Missing DB settings fall back to defaults; invalid values are rejected on save and at startup.
- Base pot and weekly increment are not admin-editable.
- `Enabled=false` is the final override. The admin `PauseAutomation` stops the scheduler; the manual admin **Run now** remains live.
- Status: **Live** (Enabled, not DryRun, not Paused), **Dry run**, **Paused**, **Disabled**.

### Settings audit

Every settings change: admin-only; write `lottery_settings_history` (actor, time, old/new, version); post to the log channel; the admin page shows the last change.

## 8. API surface

Existing:

- `POST /api/lottery/guess/{number}` (member)
- `GET /api/lottery/unused` (member)
- `GET /api/lottery/view` (member)
- `POST /api/lottery/change` (member)
- `GET /api/lottery/who-guessed/{number}` (member)
- `GET /api/lottery/all-guesses` (member)
- `POST /api/lottery/run` (admin; now the idempotent new flow)
- `POST /api/lottery/award` (admin)

New member:

- `GET /api/lottery/history?cursor=&limit=10` — winner timeline, newest first; returns `{items, nextCursor, totals}`.
- `GET /api/lottery/reminders` / `PUT /api/lottery/reminders` — the member opt-in; or a toggle endpoint.

New admin:

- `GET /api/lottery/admin/status` — round status, next run, last error, counts, flags.
- `GET/PUT /api/lottery/admin/settings` — schedule, reminder, templates, pause; validation; audit.
- `POST /api/lottery/admin/pot` — `{amount, reason, direction}`; floor 1M; ledger.
- `GET /api/lottery/admin/ledger?cursor=` — ledger.
- `GET /api/lottery/admin/payouts?status=` — winners/payouts.
- `PATCH /api/lottery/admin/winners/{roundKey}/{winnerId}/payout` — mark paid/forfeit; note.
- `GET/POST/DELETE /api/lottery/admin/prizes` and `POST /api/lottery/admin/prizes/{id}/assign` — prize pool.
- `POST /api/lottery/admin/reminders/test` — send the test reminder to the admin.
- `GET/DELETE /api/lottery/admin/optins` — opt-in list/remove.
- `POST /api/lottery/admin/dry-run` — manual dry run to the log channel.
- `POST /api/lottery/admin/run` — live manual run (confirm in the UI; idempotent).
- `POST /api/lottery/admin/skip`, `.../void`, `.../retry` — schedule/recovery actions.
- `GET /api/lottery/admin/preview?result=win|nowin` — rendered announcement.

All admin routes use `[AdminAuth]`; member routes `[MemberAuth]`. Error responses are typed and user-facing; no raw exceptions.

DTOs: `LotteryHistoryItem`, `LotteryHistoryPage`, `LotteryAdminStatus`, `LotterySettingsDto`, `PotAdjustmentRequest`, `PrizePoolItem`, `PayoutUpdateRequest`, `AnnouncementTemplateDto`, `ReminderOptInDto`, `ManualActionResponse`.

## 9. Frontend

Routes:

- `/lottery` — Play tab; existing guessing UI + the reminder opt-in toggle + tabs to History (and Admin for admins).
- `/lottery/history` — History tab; timeline + summary + lazy loading.
- `/lottery/admin` — admin page; admin guard; five tabs.

Components:

- `LotteryView.vue`: tabs Play/History (Admin link for admins); opt-in toggle.
- `LotteryHistoryView.vue`: summary cards, timeline, infinite scroll, load-more fallback, empty/loading, mobile.
- `LotteryAdminView.vue`: five tabs; Overview, Schedule & reminders, Pot & payouts (pot/ledger/prize pool/payouts), Messages, Guesses.
- `LotteryAnnouncementPreview.vue`: modal/drawer.
- `LotteryPrizePool.vue`, `LotteryPayouts.vue`, `LotteryMessageTemplates.vue`, `LotteryScheduleSettings.vue`, `LotteryOptIns.vue`.
- API client `features/lottery/lottery.api.ts`; types.

The admin page shows deployment config read-only; the template editor has the placeholder chips, char count, preview/reset/save. The status pill shows Live / Dry run / Paused / Disabled.

Accessibility: semantic timeline list, keyboard load-more, focus-visible, reduced motion; responsive cards.

## 10. Discord bot and services

- New `LotterySchedulerService : BackgroundService`:
  - Uses injected `TimeProvider`; ticks (e.g., every minute) and computes due rounds from `lottery_settings`.
  - Honors `Enabled`, `DryRun`, `PauseAutomation`.
  - Claims a round via unique `RoundKey` + conditional status update.
  - Runs the draw, writes entries/winners/ledger, posts the announcement, completes; retries per #105; alerts.
  - Handles catch-up, skip, reschedule, void/retry.
  - Checks the bot connection before posting; waits/retries.
- `ILotteryService` additions/refactors: run round (system actor), dry run, pot adjust, prize pool, payouts, settings, templates, opt-ins, history, admin status, preview.
- Repositories: `LotteryRoundRepository`, `LotteryEntryRepository`, `LotteryWinnerRepository`, `LotteryPotLedgerRepository`, `LotteryReminderOptInRepository`, `LotteryAwardRepository`, `LotteryPrizePoolRepository`, `LotterySettingsRepository`, `LotterySettingsHistoryRepository`; keep `LotteryResultRepository` for archive.
- `IDiscordMessageService` additions: post lottery announcement, post reminder with components, post dry-run to log, post admin alert; mention suppression.
- `LotteryInteraction`:
  - `/lottery reminders on|off`.
  - `[ComponentInteraction("lottery_reminders:off")]` sets the caller's opt-in false and replies ephemerally.
  - Existing `/lottery run/remind/award` gates remain.
- `LotteryService.RunLotteryAsync` refactor to the new flow; admin/system actor; the old short messages/participants removed.
- `LotteryService.RemindAsync` replaced by the opt-in audience; the previous-participants/oldest-3 bug removed.
- `DiscordBotService`/`WorkerService` unchanged except registration; the scheduler is a new hosted service.
- Log channel alerts via the existing `IDiscordMessageService`/`DiscordLogger`; admin role ping.
- Health check: add a `lottery` check/detail to the existing `/health`.

## 11. Security and permissions

- Admin-only: settings, pot, payouts, prize pool, templates, skip/void/retry, dry-run, manual run. Member-only: guess/change/view/history/opt-in. Officers keep slash run/remind/award.
- Every settings/payout/pot/prize action is audited (actor, time, old/new or note).
- No secrets in the DB settings; channel/role/emoji ids are deployment config.
- Mentions: announcement role ping and reminder member mentions are intentional; dry-run and alerts suppress mentions as configured.
- Rate limits: one announcement/reminder per round; retries are bounded.
- The web app's existing auth/CSRF/forwarded headers apply.

## 12. Migration, cutover, rollback

1. Deploy with `Enabled=false`, `DryRun=true`.
2. Create collections and indexes (migration).
3. Manually insert the 35 winners and the opening pot (shapes above).
4. Run the one-time `lottery_results` backfill at a round boundary.
5. Set schedule/reminders/templates in the admin page.
6. Dry-run one scheduled week; review the `[DRY RUN]` post and logs.
7. Deploy `DryRun=false`, then `Enabled=true`. The first live run is the next scheduled round.

Rollback: set `Enabled=false` (or deploy the previous version). Collections are additive; `lottery_results` remains; no data is deleted. The manual `/lottery run` remains the live escape hatch.

Data retention: keep rounds, entries, winners, ledger, awards, opt-ins, prize pool, settings history indefinitely; no deletes except explicit admin removal of an in-pool prize or opt-in.

## 13. Testing and acceptance criteria

### Test areas

- Unit: pot/split/rounding; ledger signs/floor; prize-pool assignment; template fill/validation; opt-in logic; history mapping; settings validation; schedule/DST (TimeProvider); state machine transitions; idempotency keys.
- Integration: Mongo repositories/indexes; API auth; admin/member endpoints; the scheduler with a fake clock; Discord fake for announcement/reminder/alert; failure injection at draw/post/cleanup; restart/resume; dry-run no side effects; backfill idempotency.
- Frontend: history timeline/lazy loading/empty; admin tabs/preview/validation; opt-in toggle; mobile.
- Existing test suite must stay green; use `dotnet vstest` per `AGENTS.md`.

### Acceptance criteria

1. With automation disabled, the scheduler does nothing; the manual admin run still works.
2. The run executes on the configured day/time in Europe/Berlin, including DST transitions.
3. A round is unique per `RoundKey`; a double claim returns "already running/run".
4. No winner: pot +1M; a `weekly_increment` ledger entry; the post shows the next pot.
5. Winner(s): the pot is split per rules; floor/remainder correct; `payout` + `reset` ledger entries; the post shows the reset pot.
6. Multiple winners get correct shares and last-five rows; a shared draw shows two rows.
7. Corrections cannot take the pot below 1M; every change is ledgered and audited.
8. Non-Gil prizes can be added, removed, assigned; items are indivisible; awarded items appear on the winner and history; the Gil ledger is unaffected.
9. The announcement is a plain post under 2,000 chars with the configured role/channel/emoji; last five newest-first; no participants list; not pinned.
10. The win/no-win templates are editable; unknown placeholders are rejected; invalid/over-limit falls back and alerts; reset restores defaults.
11. The reminder posts only to opted-in members with unused guesses; no DMs; the group counts are correct; the one-click button turns off only the caller's opt-in and replies ephemerally.
12. The manual `/lottery remind` uses the same audience; no previous-participants/oldest-3 bug.
13. `/lottery/history` opens the History tab; the timeline is newest-first; infinite scroll loads batches of 10; the Load more fallback works; summary counts are correct (35 winners / 34 draws).
14. The admin page is admin-only; the five tabs work; Run/Skip/Void/Forfeit confirm; the preview renders the current templates/pool; the failure badge shows.
15. The failure state machine retries draw/post/reminder per policy; never re-draws; pending/failed rounds block the next; restart resumes; the `posting` reconciliation avoids duplicate posts.
16. Alerts go to the log channel (fallback lottery channel) with an admin ping only on terminal states.
17. `/health` exposes the lottery detail; a stuck round does not fail the container.
18. Settings changes are admin-only and audited with actor/time/old/new; the log channel gets a notice.
19. Bad/missing config starts degraded, disables the scheduler, and alerts; invalid DB settings fall back.
20. Dry run posts `[DRY RUN]` to the log channel with no ledger/winners/entries writes, no flush, no pot change, mentions suppressed.
21. The cutover/backfill/manual seed sequence works on a scratch DB; the backfill is idempotent; the archive remains.
22. Rollback via `Enabled=false` stops the scheduler; the manual run and data remain.
23. The old `lottery_results` archive and the new collections coexist; no duplicate rounds on backfill.
24. Members-only history/opt-in; admin-only settings/pot/payouts; officers keep slash commands.
25. Failure tests use `TimeProvider` and fakes; no test waits for a real Tuesday.

## 14. Rollout and monitoring

- Single replica; no new infrastructure.
- Deploy with `Enabled=false`, `DryRun=true`; one dry-run week; promote by deploy.
- Monitor: log channel alerts, `/health` lottery detail, admin Overview status, the audit history.
- Docs: README, appsettings examples, the manual seed/backfill runbook, the admin page guide.
- Rollback: `Enabled=false` or the previous image; no data deletion.

## 15. Suggested implementation slices

1. Data model/repositories + indexes; manual seed and backfill docs.
2. Settings/config + validation + audit.
3. Scheduler/state machine + pot/prize logic + idempotency.
4. Announcement + templates + posting/reconciliation + retry/alerts.
5. Reminder + opt-in + button + slash command.
6. History page (timeline/lazy loading).
7. Admin page (five tabs, preview, prize pool, payouts, templates).
8. Migration/cutover/dry-run/rollout + health.
9. Tests and docs.

## 16. Open questions and out of scope

- Statistics UI: deferred. The schema supports most-guessed (`lottery_entries` by `Number`) and most-winning (`lottery_winners` by `Guess`).
- Prize-pool edge cases: the approved default is admin assignment, indivisible items, unassigned items stay, forfeited/left-winner reassignment or return. The exact UI for assigning multiple items to multiple winners can be refined during implementation.
- Template validation: the approved default is the result block with the listed placeholders, unknown placeholders rejected, full message under 2,000, fallback to default and alert on run-time failure. The exact error copy can be refined.
- Payout actor/note: admin-only, audited; no automatic pot change except an explicit correction.
- In-game payout automation; riddle integration; event-guess automation; guessing UX; multi-guild; RNG/range changes; stats UI — out of scope.
- The spec path: `.scratch/lottery-automation/spec.md`; may be promoted to `docs/user-stories/` if the repo convention wins. The map is [#97](https://github.com/Keburichi/ExcelBotCs/issues/97).
