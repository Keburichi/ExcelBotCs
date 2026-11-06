# Cutover, rollout and rollback runbook

The operator procedure for turning the automated weekly lottery on (spec §12 and §14), written for #115. It assumes
the deployment in [`configuration.md`](configuration.md), the manual history in [`manual-seed.md`](manual-seed.md)
and the two maintenance commands in [`operations.md`](operations.md).

Everything here is done **with the automation still off**. Nothing in this runbook deletes data, and every step is
re-runnable.

## The flags you will use

| Flag | Start | Promote to | Meaning |
|---|---|---|---|
| `Lottery:Automation:Enabled` | `false` | `true` (last) | deployment kill switch; the final override |
| `Lottery:Automation:DryRun` | `true` | `false` (before `Enabled`) | simulate and post `[DRY RUN]` to the log channel |

Both are environment variables / `appsettings*.json`, so each change is a **restart or redeploy**; there is no
"go live" button. The admin page's **Pause automation** toggle is the incident switch and only stops the scheduler —
the manual **Run now** stays live.

## 1. Deploy with the automation off

```bash
Lottery__Automation__Enabled=false
Lottery__Automation__DryRun=true
```

Check it took, through the two surfaces you will use for the rest of the runbook:

- `/health` → the `lottery` entry reports `state: "disabled"` and stays **Healthy**: nothing is pending, and the flag
  is reported in `automationNote` (see [Reading `/health`](#reading-health)).
- `/lottery/admin` → status pill **Disabled**, scheduler off.

While you are here, set `WebBaseUrl` to the **public** site URL (`https://lottery.example.com`, no trailing slash).
Every announcement carries a `/lottery/history` link, the void annotation carries a `/lottery/admin` link, and the
needs-admin alerts link the admin page too. The repository's `appsettings.fc-server.json` ships
`https://localhost:5032` as a **placeholder**: the startup check accepts it because it is an absolute URL, so a
forgotten value does not degrade the start — it just sends every operator and member to their own machine.

## 2. Create the collections and indexes

Back up MongoDB first, then:

```bash
dotnet run --project ExcelBotCs -- --lottery-ensure-indexes
```

Exit code `0` means the indexes are in place. The command is idempotent and can run on every deployment; it fails
loudly if existing data violates a unique index (fix the data, then re-run). Details:
[operations.md](operations.md#index-migration).

## 3. Insert the manual history and the opening pot

The application never seeds these and **never creates the opening pot**. Insert the 35 historical winner rows and
exactly one opening-balance ledger entry with the shapes in [`manual-seed.md`](manual-seed.md). Three rules matter at
cutover:

- the opening pot is **manual only** (§6 "no auto set"): the ledger starts empty, and the app derives the balance as
  the newest `BalanceAfter` — or the configured `Lottery:Automation:BasePot` while there is no entry. A deployment
  that skips this step therefore runs at 1,000,000 Gil, it does not silently acquire a seeded pot;
- the opening-balance entry is a `correction` with `IdempotencyKey: "opening-balance"` — the unique index makes a
  second insert fail instead of doubling the pot;
- no `lottery_rounds` documents are needed for the historical rows: the history page reads `lottery_winners`.

Verify: the admin pot equals the opening balance, `/lottery/history` shows **35 winners / 34 draws**.

## 4. Run the one-time `lottery_results` backfill at a round boundary

```bash
dotnet run --project ExcelBotCs -- --lottery-backfill
```

Run it **at a round boundary** (just after a draw date, before the next one) so the imported archive rounds and the
first live round do not interleave. Run it a second time and confirm it reports zero created rounds and entries — the
backfill is idempotent, and the archive is read-only.

`--lottery-ensure-indexes --lottery-backfill` runs both in one process if you prefer one step.

## 5. Set the schedule, reminders and templates in the admin page

On `/lottery/admin` → **Schedule & reminders** and **Messages**: run day/time/timezone, reminder day/time/timezone,
the reminder toggle, and the two result templates. Save once; every save writes one `lottery_settings_history` row and
one log-channel notice. The version number is the "what changed when" record.

The reminder is part of the automated path: pausing automation, the deployment kill switch, a degraded startup check
and the reminder toggle all stop the scheduled post. A member's opt-in is off by default, so expect the first real
reminder only after a few members have opted in on `/lottery`.

## 6. Dry-run one scheduled week

Leave `DryRun=true` and let a full scheduled week pass with members guessing. On the run date the scheduler draws,
simulates the round and posts the **same announcement, prefixed `[DRY RUN]`**, to the log channel with mentions
suppressed. Nothing is persisted: no round, entries, winners or ledger rows, no guess flush, and the pot does not move.

Review:

- the `[DRY RUN]` post in the log channel — role ping rendered as text, mention suppressed, history link, pot and the
  winner block as you expect from the templates;
- `/lottery/admin` → the pot is unchanged and no new round exists;
- `/health` → the `lottery` detail is **Healthy** with `state: "dry-run"` and the reason in `automationNote` (a dry
  run is an operator decision, not a fault);
- the log for `Dry run preview for round <key>`.

You can repeat the check on demand with **Dry run now** on the admin Overview; it uses the same preview path and never
writes. The dry-run tick only posts once per round key — a minute-by-minute scheduler does not spam the log channel.

The dry-run week is inert in two more ways worth confirming:

- **the reminder is suppressed.** The scheduled pre-draw reminder pings real opted-in members, so it is gated by
  `DryRun` like the rest of the automated path (`dry-run` disposition, `POST /lottery` reminders untouched). The
  officer's `/lottery remind` and the admin **Send test reminder** stay live — they are explicit human actions;
- **the previewed slot is remembered.** The tick stores the round key in `lottery_settings.LastDryRunRoundKey`, so
  promoting the deployment does not draw that date for real (see step 7).

## 7. Promote

1. Redeploy with `Lottery:Automation:DryRun=false` (`Enabled` still `false`). Nothing runs; this only removes the
   prefix for the next step.
2. Redeploy with `Lottery:Automation:Enabled=true`.

The first live run is the **next scheduled round** — the dry-run week is not replayed live. A dry run writes no round
row, so without `lottery_settings.LastDryRunRoundKey` the planner would treat the previewed date as a missed slot and
draw it on the first live tick; the marker makes that slot count as satisfied. Watch the first two live weeks against
the checklist in [Monitoring](#monitoring-checklist).

### Cutover gotcha: enabling mid-week runs a round immediately

Enabling the automation is not "wait quietly for next Tuesday": the scheduler runs the **most recent due slot** that
has no round yet (§5.10 catch-up), and marks the older missed dates `skipped`.

Concretely, with rounds already in the database (the backfill, the manual history does not count — winners are not
rounds):

- **enabled before the run time on the scheduled day** → that day's round runs at its slot;
- **enabled after the run time on the scheduled day** → that day's round runs on the first tick, within a minute;
- **enabled any other day** → the most recent scheduled date (the last run day) runs on the first tick, and every
  date between the newest existing round and that date is recorded `skipped`.

A caught-up round is a **real** round: it draws the live guesses and moves the pot (a win pays out; no winner grows
the pot by the increment). Skipped dates never grow the pot and never flush guesses.

So: enable **before** a run day if you want a quiet week first, or enable right after a draw if you want the next
round to be the first live one. Either way, do step 6 first, and if you need to stop after the catch-up round,
`Enabled=false` and **void** the round instead of letting the next one run.

A deployment with **no rounds at all** is the safe case: the first round only runs if the automation is switched on
**after** that day's run time (so the scheduler sees the due slot as missed today); otherwise it waits for the next
slot.

## Reading `/health`

`/health` returns one JSON report and the status code is the probe contract:

| Status code | When |
|---|---|
| `200` | every check healthy **or degraded** — the normal state, including a stuck lottery round |
| `503` | a **core** check is unhealthy: the Discord gateway or MongoDB |

The `lottery` entry never returns `Unhealthy`, so a stuck round, a paused scheduler or a slow database can never make
the container fail its probe. The accepted rule (#105) is that the **round state** decides the status — healthy while
no round is pending or failed, degraded when one is — and that the rollout flags are reported without flipping it:

| `data.state` | `status` | Why |
|---|---|---|
| `live` | Healthy | enabled, not a dry run, not paused, no unfinished round |
| `dry-run` | Healthy | draws are simulated and posted as `[DRY RUN]`; the reason is in `automationNote` |
| `paused` | Healthy | admin pause; manual **Run now** still works; the reason is in `automationNote` |
| `disabled` | Healthy | `Enabled=false`; nothing is scheduled; the reason is in `automationNote` |
| any | Degraded | a round is pending/failed: see `pendingRoundKey`, `pendingStatus`, `failedStep`, `attempts`, `nextRetryAtUtc`, `lastErrorReason` |
| any | Degraded | `startupCheck` is `degraded` (`problemCount`) or `pending` (the check has not finished) |
| `unknown` | Degraded | the status could not be read within the read budget |

A dry-run or paused week is therefore a **healthy** probe; only a stuck round or a broken startup check degrades the
detail. Read `automationNote` when you want to know which rollout state produced the detail.

`/health` is **anonymous**, so the report never carries raw exception text: the round's error appears as a coarse
`lastErrorReason` (for example `"a Discord error is recorded"`) and the startup problems as a `problemCount`. The full
`LastError` and the problem list are on the authenticated `GET /api/lottery/admin/status`, which is where you debug
from; the writer redacts `lastError`, `problems`, `exception`, `error` and `stackTrace` from any check's `data`.

Response shape (trimmed):

```json
{
  "status": "Degraded",
  "totalDurationMs": 11.4,
  "checks": {
    "discord-bot": { "status": "Healthy", "durationMs": 0.1 },
    "mongodb": { "status": "Healthy", "durationMs": 3.2 },
    "lottery": {
      "status": "Degraded",
      "description": "Lottery automation is live. Round 2026-11-10 is drawn and unfinished; ...",
      "durationMs": 4.7,
      "data": {
        "state": "live",
        "schedulerEnabled": true,
        "degraded": true,
        "startupCheck": "healthy",
        "pendingRoundKey": "2026-11-10",
        "pendingStatus": "drawn",
        "failedStep": "announcement",
        "attempts": 3,
        "nextRetryAtUtc": "2026-11-10T20:30:00.0000000Z",
        "lastErrorReason": "a Discord error is recorded",
        "nextRoundKey": "2026-11-17",
        "nextRunAtUtc": "2026-11-17T19:00:00.0000000Z"
      }
    }
  }
}
```

Probes should keep checking the status code only. `degraded: true` is the quick machine-readable signal; the
`description` is the operator sentence.

## Monitoring checklist

Weekly, after the run day (spec §14):

- [ ] the announcement post is in the lottery channel, one message, the expected winner block and pot;
- [ ] `/health` → `lottery` has no `pendingRoundKey` after the run, and the next round is scheduled;
- [ ] `/lottery/admin` → **Overview** shows the new round, the pot and no failure badge;
- [ ] the log channel has no needs-admin alert;
- [ ] `/lottery/history` shows the new winner(s) and the summary counts moved by one draw.

Daily during the first two weeks, or whenever something looks off:

- [ ] `lottery.status == Degraded` because of a **pending** round → open `/lottery/admin` and retry or void it; do not
  wait for the next date (a pending round blocks the schedule);
- [ ] `startupCheck == degraded` → a required id or the pot configuration is missing; fix the deployment config and
  restart, then confirm the detail reports `healthy`;
- [ ] the reminder post (Monday 18:00 by default) reached the opted-in members and the draw was not delayed;
- [ ] the audit history on the admin page matches the settings changes you made.

Never restart the container to "fix" a `Degraded` lottery detail: a restart re-runs the startup check but changes no
round state, and the round state is what an alert is about.

## Rollback

Rolling back is a flag, not a migration:

1. Redeploy with `Lottery:Automation:Enabled=false` (keep `DryRun` whatever it was). The scheduler stops on the next
   process start; whatever round is in flight is picked up again when you re-enable it, or can be finished by hand.
2. Confirm: `/health` → `lottery` `state: "disabled"`, `/lottery/admin` → **Disabled**, and no new rounds appear for
   the next date.
3. The manual escape hatch still works while rolled back: **Run now** on the admin page (or `POST
   /api/lottery/admin/run`) runs the due round live. Use it if a week must happen by hand.
4. Optionally deploy the previous image. Nothing else is required: the collections are additive, `lottery_results`
   remains, and no data is deleted.

What a rollback does **not** do: it does not delete rounds, winners, entries, ledger rows, opt-ins or settings, and
it does not roll the pot back. If a draw must be undone, use **Void last draw** on the admin page — that reverses the
round's ledger entries and annotates the post — and then **Run now** to draw the date again (see
[Recovering a wrong draw](#a-wrong-draw-void-then-run-the-date-again)).

Do not drop `lottery_*` collections or `lottery_results` as part of a rollback. On a **scratch** database where the
cutover must be restarted, drop only the new lottery collections and repeat this runbook from step 2.

## Recovering a wrong draw or a moved round

Two operator mistakes have a documented, audited recovery path. Neither is automatic: nothing redraws a date unless an
admin asks for it (§5.1 "Redo", "Reschedule").

### A wrong draw: void, then run the date again

1. `/lottery/admin` → **Overview** → **Void last draw**, with a reason. The reason is required: it is stored on the
   round and in the audit trail, the round becomes `voided`, the announcement is annotated as void, and the round's
   own ledger movements (`payout`/`reset`/`weekly_increment`) are reversed by audited `correction` entries.
2. Confirm `/health` shows the round as finished (no `pendingRoundKey`) — a `voided` round does not block the
   schedule — and that the pot is back where it was before the draw.
3. **Run now** on the same page draws the date again. It re-opens the voided round: `scheduled` again with the old
   result cleared, and `Attempt` incremented. The fresh draw uses the guesses that exist *now*, writes attempt-scoped
   ledger keys (`2026-11-10:payout:1`, `2026-11-10:reset:1`, …) and leaves the voided attempt's movements and their
   reversals untouched, so the ledger still balances and the audit still shows what was undone. The void reason, actor
   and time stay on the round.
4. Confirm the new announcement in the lottery channel, the new winners on `/lottery/history` and the pot on
   **Pot & payouts**.

Do not try to recover by running a *different* date: **Run now** targets the most recent due slot. Once a new week has
started, the voided date is history and is not re-opened — void early, while the date is still the due slot.

### A round on the wrong date: reschedule it

1. `/lottery/admin` → **Schedule & reminders** → **Move one round**.
2. Leave the round field blank to move the next round, or type its key (`YYYY-MM-DD`) to move a specific one.
3. Pick the new local date and time and confirm.

The round key follows the new **local** date (a 00:30 Berlin instant belongs to the next day), guesses stay with the
round, and the pot is untouched until the run. Moving onto a date that already has a round fails with a typed
conflict — nothing moves — and only a `scheduled` round can be moved: one that already ran, was skipped or was voided
must be recovered another way. The same action exists as `POST /api/lottery/admin/reschedule`.

## Afterwards

Keep rounds, entries, winners, the ledger, awards, opt-ins, the prize pool and the settings history indefinitely. The
only deletes are the explicit admin ones: an in-pool prize, a reminder opt-in.
