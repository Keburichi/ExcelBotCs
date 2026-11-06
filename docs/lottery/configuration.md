# Configuration, precedence and the settings audit

The implementation reference for #109 — the configuration surface of the automated weekly lottery. The product
contract remains [the spec](../user-stories/automated-weekly-lottery.md) §7, §5.1 and §5.10.

Two kinds of configuration exist and they must not be confused:

- **Deployment config** lives in `appsettings.json` / environment variables. It holds tokens, ids, the automation
  kill switch and the pot amounts. The admin page shows it read-only and the settings API never writes it.
- **Operational settings** live in the `lottery_settings` singleton and are edited on `/lottery/admin`. They hold
  the schedule, the reminder settings and the two result templates.

## Precedence

`env > stored lottery_settings > the defaults below`.

| Source | What it can set | Who wins |
|---|---|---|
| Environment variables / `appsettings.json` | deployment keys only (`Lottery:Automation:*`, ids, `WebBaseUrl`) | over everything in its own keys |
| `lottery_settings` document | schedule, reminders, pause, templates | over the built-in defaults |
| `LotterySettingsDefaults` (`Modules/Lottery/LotterySettingsDefaults.cs`) | every operational field | never — it is the floor |

There is no key that can be set both from a file and from the admin page, so "env beats stored" is not a
per-field merge: it is the rule that `Lottery:Automation:Enabled=false` (deploy kill switch) and the pot amounts
are final over anything stored, while `PauseAutomation` is the admin's own switch.

`IConfiguration` itself provides `environment > appsettings.json > class defaults`; that chain is covered by
`LotteryDeploymentConfigTests`.

## Deployment config (spec §7)

| Key | Default | Notes |
|---|---|---|
| `DiscordBot:RiddlesChannel` | `0` | channel linked in the announcement for riddle top-ups |
| `DiscordBot:LotteryMemberRoleId` | `0` | role the announcement pings |
| `DiscordBot:LotteryCustomEmojiId` | `0` | `kekDoggo` id; `0` means the template keeps the literal text |
| `DiscordBot:LogChannel` | `0` | audit notices and alerts; `0` falls back to `DiscordBot:LotteryChannel` |
| `WebBaseUrl` | falls back to `Jwt:Issuer` | **public** base URL for the `/lottery/history` and `/lottery/admin` links, no trailing slash; `appsettings.fc-server.json` ships a `localhost` placeholder |
| `Lottery:Automation:Enabled` | `false` | deploy kill switch — nothing is scheduled or posted |
| `Lottery:Automation:DryRun` | `true` | post a `[DRY RUN]` message with mentions suppressed |
| `Lottery:Automation:BasePot` | `1000000` | pot after a win / opening balance; **not** admin-editable |
| `Lottery:Automation:WeeklyIncrement` | `1000000` | pot growth after a round without a winner; **not** admin-editable |

`WebBaseUrl` is a top-level scalar, so it is registered specially in `ConfigExtensions.LoadSettings` instead of
through `[OptionsSection]` (`WebBaseUrlOptions.Resolve`). The three `DiscordBot` ids are `ulong` options without
`[Required]`: a deployment that has not filled them in still boots, and the startup check reports them.

The `localhost` values in `appsettings.json` (`https://localhost:8080`) and `appsettings.fc-server.json`
(`https://localhost:5032`) are **placeholders**. The startup check only rejects an empty or non-absolute value, so a
forgotten placeholder does not degrade the start — it just makes every announcement, void annotation and needs-admin
alert link point at the operator's machine. Set `WebBaseUrl` to the public site URL before the dry-run week.

## Operational settings and their defaults

`lottery_settings` has exactly one document, `Key = "lottery"`. A missing document, a missing field or a field
that fails validation all resolve to these defaults:

| Field | Default |
|---|---|
| `RunDay` / `RunTime` / `RunTimezone` | `Tuesday` / `20:00` / `Europe/Berlin` |
| `ReminderDay` / `ReminderTime` / `ReminderTimezone` | `Monday` / `18:00` / `Europe/Berlin` |
| `RemindersEnabled` | `false` |
| `PauseAutomation` | `false` |
| `WinTemplate` | spec §5.6 winner block (placeholders `{number}`, `{winners}`, `{prize}`, `{bonusItems}`, `{nextPot}`) |
| `NoWinTemplate` | spec §5.6 no-winner block (placeholders `{number}`, `{emoji}`, `{pot}`) |
| `Version` | `0` before the first save, bumped by one per accepted change |
| `UpdatedByDiscordId` | `null` for a system change |

### Validation

The same rules run on save **and** when a stored document is read (spec §7, "rejected on save and at startup"):

| Field | Rule | Normalized to |
|---|---|---|
| day | an English day name, case-insensitive, trimmed | `Tuesday` |
| time | `HH:mm`, 24-hour, two-digit hour | `20:00` |
| timezone | an id `TimeZoneInfo` resolves | `Europe/Berlin` |

A save with any invalid field writes **nothing** and returns one error per field
(`LotterySettingsValidationError(Field, Message)`). A stored value that fails validation is replaced by its
default per field — the rest of the document is still honoured — and the field name is listed in
`LotteryEffectiveSettings.InvalidFields`, which the startup check turns into a degraded start.

A blank/whitespace template is stored as the default template. Template *rules* (placeholder allow-list,
character limit, fallback at run time) belong to #111; this ticket only owns the two strings.

## Status

`LotteryAutomationStatus` is derived, never stored: `Derive(enabled, dryRun, pauseAutomation)`.

| Enabled | PauseAutomation | DryRun | Kind | Label | Scheduler |
|---|---|---|---|---|---|
| `true` | `false` | `false` | `live` | Live | runs |
| `true` | `false` | `true` | `dry-run` | Dry run | runs |
| `true` | `true` | any | `paused` | Paused | stopped |
| `false` | any | any | `disabled` | Disabled | stopped |

`Enabled = false` is the final override. `SchedulerEnabled` is the flag the scheduler must read; `PauseAutomation`
stops only the scheduled path — the manual admin **Run now** and the officer's `/lottery remind` stay live.

The status is also what the `lottery` health detail reports: a non-live status is an operator decision, so it appears
there as an informational `automationNote` and does not degrade the probe (see
[the health detail](#the-lottery-health-detail) below).

## Audit

Every accepted change to `lottery_settings`:

1. bumps `Version` and stores `UpdatedByDiscordId` on the singleton;
2. writes exactly one `lottery_settings_history` row — `Version`, `ActorDiscordId`, `ChangedAt` (UTC), and
   `OldValues` / `NewValues` holding only the fields that actually changed;
3. posts one concise notice, `Lottery settings v3 updated by <@id>: PauseAutomation`, through
   `IDiscordMessageService.PostInLogChannelAsync` (log channel, lottery-channel fallback).

A save that would not change an effective value writes nothing and writes no history row, so the admin page can
save an untouched form without polluting the audit trail. Normalization happens before that comparison, so
`tuesday` → `Tuesday` is not a change. A failed Discord post is logged as a warning and never fails the save —
the history row is the record of truth.

## Degraded start

`LotteryStartupCheck` runs once at startup (`LotteryStartupCheckService`) and never throws. It reports degraded
when any of these is wrong:

- `DiscordBot:GuildId`, `LotteryChannel`, `LotteryMemberRoleId` or `RiddlesChannel` is `0`;
- `Lottery:Automation:BasePot` is not positive, or `WeeklyIncrement` is negative;
- `WebBaseUrl` (after the `Jwt:Issuer` fallback) is empty or not an absolute `http(s)` URL;
- `lottery_settings` cannot be read, or contains fields that had to be repaired.

All problems go out as **one** error log line, which `DiscordLogger` mirrors into the log channel — one alert, no
second mechanism. With `DiscordBot:Enabled=false` the check is skipped (info log): a botless checkout has no
automation and nobody to alert.

A degraded (or still-running) check disables **all** automation, not just the draw:

- the round tick does nothing (`LotteryTickAction.Nothing`);
- the reminder tick does nothing and returns `startup-pending` or `startup-degraded` — it deliberately logs at debug
  level, because the startup check already owns the single alert and an error log per tick would be alert spam;
- the manual escape hatches stay live: the admin **Run now** / **Dry run now** and the officer's `/lottery remind`.

The `lottery` health detail reports the verdict as `startupCheck: "pending"` or `"degraded"` (with the `problems`
array) and is itself degraded.

## Dry run (spec §5.6, §13 criterion 20)

`Lottery:Automation:DryRun=true` — the shipped default, and the first rollout week — keeps the scheduler running but
turns every draw into a rehearsal:

- the scheduler reads the live guesses, draws a number and builds the announcement exactly as the live path does;
- the **same assembled message** is posted with a `[DRY RUN] ` prefix to `DiscordBot:LogChannel` (falling back to
  `DiscordBot:LotteryChannel` when it is `0`) with `AllowedMentions.None`, so no role and no member is pinged;
- **nothing** is written: no `lottery_rounds`, `lottery_entries`, `lottery_winners` or `lottery_pot_ledger` row, no
  guess flush, no pot change. The live guesses carry to the next round;
- the preview is posted once per round key, so a minute-by-minute tick does not repeat it. A failed preview is retried
  on the next tick.

The admin **Dry run now** action uses the same preview path (`ILotteryRoundRunner.PreviewAsync`) and the same
guarantees, whatever the flags say. Because the dry run uses the same renderer as the live post, the templates, role
fallback, emoji and history link are all reviewed under real conditions before promotion.

Promotion is a deploy, not a button: `DryRun=false` first, then `Enabled=true`
([cutover runbook](cutover-runbook.md#7-promote)).

## Rollback

`Lottery:Automation:Enabled=false` is the rollback. It is the final override (see the status table above): the
scheduler stops on the next process start, and the manual admin **Run now** stays live. Nothing else changes — the
`lottery_*` collections and `lottery_results` are additive, and no data is deleted. The step-by-step procedure,
including the mid-week catch-up gotcha, is in the [cutover runbook](cutover-runbook.md#rollback).

## Automatic mid-week catch-up

Enabling the automation does not wait for the next run day. The scheduler runs the most recent due slot that has no
round yet and records the older missed dates as `skipped` (spec §5.10). A deployment with no rounds at all waits for
the next slot unless it is switched on after that day's run time. The operator-facing consequences are documented in
the [cutover runbook](cutover-runbook.md#cutover-gotcha-enabling-mid-week-runs-a-round-immediately).

## The `lottery` health detail

`/health` registers a third check, `lottery` (`LotteryHealthCheck`), next to `discord-bot` and `mongodb`. It reuses
the round engine's `ILotteryRoundRunner.GetStatusAsync()` snapshot and the startup verdict, with a bounded read
(3 seconds, like the Mongo check's 5) so a slow database cannot hang a probe.

- `Healthy` when no round is pending or failed (the last round completed or was skipped and the next is scheduled)
  and the startup check passed;
- `Degraded` for a pending/failed round (with the round key, failed step, attempts, next retry and last error) and for
  a degraded or still-pending startup check;
- a `dry-run`, `paused` or `disabled` scheduler does **not** degrade the detail: those are operator decisions, so they
  are reported in the informational `automationNote` field and in the description (spec §5.10 as clarified by #105);
- **never** `Unhealthy`. `Program` maps `Degraded` to `200` and only an `Unhealthy` **core** check (Discord, MongoDB)
  to `503`, so the lottery detail can never fail the container.

The response body is the JSON report written by `HealthResponseWriter`; see
[Reading `/health`](cutover-runbook.md#reading-health) for the shape and the data keys. The status code remains the
probe contract.

## Contracts for the later tickets

- Read settings only through `ILotterySettingsService.GetEffectiveAsync()` — never through
  `ILotterySettingsRepository`. It returns `LotteryEffectiveSettings` (schedule + templates + `Version` + the
  deployment `AutomationEnabled` / `DryRun` / `BasePot` / `WeeklyIncrement` + `Status` + `InvalidFields`).
- The admin API saves with `ILotterySettingsService.SaveAsync(LotterySettingsUpdate, ulong? actorDiscordId)`.
  `LotterySettingsUpdate` is exactly the admin-editable field set; `LotterySettingsUpdate.FromDefaults()` and
  `effective.ToUpdate()` are the "reset to default" and "prefill the form" values.
- Constants: `LotterySettingsDefaults.*` (operational) and `LotteryAutomationOptions.*`
  (`Lottery:Automation:*`, deployment). Do not re-declare `1_000_000` or a day name anywhere else.
- Alerts and notices use `IDiscordMessageService.PostInLogChannelAsync` (log channel, lottery-channel fallback).
- The scheduled path (the round tick **and** the reminder tick) starts only when
  `LotteryStartupResult.IsDegraded == false` and `LotteryAutomationStatus.SchedulerEnabled == true`. The manual
  actions are exempt by design.
