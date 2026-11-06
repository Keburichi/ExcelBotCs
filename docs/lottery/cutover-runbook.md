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

- `/health` → the `lottery` entry reports `state: "disabled"` (see [Reading `/health`](#reading-health)).
- `/lottery/admin` → status pill **Disabled**, scheduler off.

## 2. Create the collections and indexes

Back up MongoDB first, then:

```bash
dotnet run --project ExcelBotCs -- --lottery-ensure-indexes
```

Exit code `0` means the indexes are in place. The command is idempotent and can run on every deployment; it fails
loudly if existing data violates a unique index (fix the data, then re-run). Details:
[operations.md](operations.md#index-migration).

## 3. Insert the manual history and the opening pot

The application never seeds these. Insert the 35 historical winner rows and exactly one opening-balance ledger entry
with the shapes in [`manual-seed.md`](manual-seed.md). Two rules matter at cutover:

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

## 6. Dry-run one scheduled week

Leave `DryRun=true` and let a full scheduled week pass with members guessing. On the run date the scheduler draws,
simulates the round and posts the **same announcement, prefixed `[DRY RUN]`**, to the log channel with mentions
suppressed. Nothing is persisted: no round, entries, winners or ledger rows, no guess flush, and the pot does not move.

Review:

- the `[DRY RUN]` post in the log channel — role ping rendered as text, mention suppressed, history link, pot and the
  winner block as you expect from the templates;
- `/lottery/admin` → the pot is unchanged and no new round exists;
- the log for `Dry run preview for round <key>`.

You can repeat the check on demand with **Dry run now** on the admin Overview; it uses the same preview path and never
writes. The dry-run tick only posts once per round key — a minute-by-minute scheduler does not spam the log channel.

## 7. Promote

1. Redeploy with `Lottery:Automation:DryRun=false` (`Enabled` still `false`). Nothing runs; this only removes the
   prefix for the next step.
2. Redeploy with `Lottery:Automation:Enabled=true`.

The first live run is the **next scheduled round**. Watch the first two live weeks against the checklist in
[Monitoring](#monitoring-checklist).

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
the container fail its probe. Its `status` is `Healthy` only while the automation is **live**, nothing is pending and
the startup check passed; every other state is `Degraded`:

| `data.state` | `status` | Why |
|---|---|---|
| `live` | Healthy | enabled, not a dry run, not paused, no unfinished round |
| `dry-run` | Degraded | draws are simulated and posted as `[DRY RUN]` |
| `paused` | Degraded | admin pause; manual **Run now** still works |
| `disabled` | Degraded | `Enabled=false`; nothing is scheduled |
| any | Degraded | a round is pending/failed: see `pendingRoundKey`, `pendingStatus`, `failedStep`, `attempts`, `nextRetryAtUtc`, `lastError` |
| any | Degraded | `startupCheck` is `degraded` (`problems`) or `pending` (the check has not finished) |
| `unknown` | Degraded | the status could not be read within the read budget |

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
        "lastError": "Discord is unreachable",
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
round's ledger entries and annotates the post.

Do not drop `lottery_*` collections or `lottery_results` as part of a rollback. On a **scratch** database where the
cutover must be restarted, drop only the new lottery collections and repeat this runbook from step 2.

## Afterwards

Keep rounds, entries, winners, the ledger, awards, opt-ins, the prize pool and the settings history indefinitely. The
only deletes are the explicit admin ones: an in-pool prize, a reminder opt-in.
