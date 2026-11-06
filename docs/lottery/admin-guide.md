# Admin guide — `/lottery/admin`

The operator's page for the automated weekly lottery (spec §5.9). It is reachable from the lottery page's **Admin**
tab and is admin-only: a member who types the URL is sent back by the router guard, and every endpoint behind it is
admin-gated as well (`LotteryAdminController`). The member-facing pages stay `/lottery` (Play) and
`/lottery/history` (History).

Nothing here changes the deployment configuration. `Lottery:Automation:Enabled`, `DryRun` and the channel/role ids
are read-only on this page — they are a deploy decision
([configuration](configuration.md), [cutover runbook](cutover-runbook.md)). If the startup check is degraded, the
page can show the state, but only the deployment config can fix it.

## Layout

Five tabs, in order. The tab bar is keyboard-navigable (arrow keys).

| Tab | What it is for |
|---|---|
| **Overview** | this week at a glance, the four actions, the failure badge and the recovery buttons |
| **Schedule & reminders** | the run day/time/timezone, the reminder day/time/timezone, the reminder toggle, the opt-in list, the test send |
| **Pot & payouts** | the Gil pot and its ledger, the non-Gil prize pool, the winner payout statuses |
| **Messages** | the two result templates, the placeholder chips, the preview and the reset |
| **Guesses** | the existing award-guess flow |

The status pill on the Overview's **This week** card is derived, never stored: **Live** (`Enabled=true`, not a dry
run, not paused), **Dry run**, **Paused**, **Disabled** (`Lottery:Automation:Enabled=false`, the final override).

## Overview

- **This week** — the existing round for the upcoming slot: its status, the run instant in the configured timezone,
  the pot (Gil plus a prize count when the pool has items), the reminder status.
- **Preview winner post / Preview no-winner post** — renders the *current* stored templates against the live pot and
  prize pool. Read-only; it posts nothing.
- **Run now** — runs the due round **live**, immediately, whatever the flags say. It is the escape hatch and it is
  irreversible: the draw, the winners, the ledger entries and the announcement are the real thing. A second click in
  the same week answers "already run" instead of drawing again. On a **voided** due round it is also the recovery: it
  re-opens the round and draws the date again from the guesses that exist now (see **When something looks wrong**).
- **Dry run now** — the same announcement as a real run, prefixed with `[DRY RUN]`, posted to the log channel with
  mentions suppressed. Nothing is written: no round, entries, winners or ledger, and guesses are not flushed.
- **Skip week** — marks the upcoming round `skipped`. No draw, no pot change, guesses carry to the next round.
- **Pause automation** — stops the scheduler (draw *and* reminder). `Run now` stays live. This is the incident
  switch; it is a stored setting, so it survives a restart and is audited.
- **Recovery & danger** — **Retry announcement** (only while a round is unfinished: resumes the same result without
  re-drawing) and **Void last draw** (reverses the round's ledger entries and annotates the announcement; asks for a
  reason, audited).
- **Needs a decision** (only when a round is stuck) — the failure badge: the round key, the failed step, the attempt
  count, the next retry and the last error. This is the same information `/health` reports as `degraded`.
- **Needs attention** — pending payouts, prizes still in the pool, the opted-in count.
- **Last draw / Last settings change / Deployment config** — read-only summaries, including the version, actor and
  fields of the most recent settings change.

## Schedule & reminders

- Run day, run time, run timezone (default `Tuesday` / `20:00` / `Europe/Berlin`).
- Reminder day, reminder time, reminder timezone (default `Monday` / `18:00` / `Europe/Berlin`) and the global
  reminder toggle. The reminder only mentions members who opted in **and** still have an unused guess.
- **Save settings** is the only commit. A save that changes nothing writes no audit row; a save with an invalid field
  writes nothing at all and returns one error per field.
- **Opted-in members** — the opted-in rows with display name, current-membership and eligibility, and a **Remove**
  action per member (the admin-side opt-out).
- **Send test reminder** — delivers the current reminder copy to the requesting admin only, as a DM. It never posts
  to the channel and never pings the audience, and it ignores the time window and the toggles.

The scheduler will not post a reminder while the startup check is degraded or still running; the manual **Send test
reminder** and an officer's `/lottery remind` still work.

## Pot & payouts

- **Pot** — the current balance and an adjustment form (± amount, reason). A debit can never take the pot below the
  1,000,000 Gil floor; the response says how much was actually applied. Every change is one ledger entry with an
  idempotency key, and the actor is recorded.
- **Recent ledger** — newest first, paged with **Load more entries**. Every draw, correction and void appears here;
  the pot is always the newest entry's balance.
- **Prize pool** — add an item, remove an in-pool item, or assign it to a specific winner. Items are indivisible and
  are not Gil: adding, assigning or removing one does not touch the pot. An awarded item can only come back through
  a forfeit.
- **Winner payouts** — every winner with guess, Gil prize, item chips and payout status. **Mark paid** and
  **Forfeit** are the manual records of the in-game trade; a forfeit returns the winner's prize items to the pool.
  Payout status is admin-only and never appears on the member history page.

## Messages

The winner and no-winner result blocks only — the rest of the announcement (role ping, events/riddles nudge, rules,
deadline, pot summary, last five, history link) is generated and shown read-only.

- The placeholder chips list what may appear in each template; an unknown placeholder, a missing required one, or a
  message that would exceed Discord's 2,000 characters is rejected on save.
- **Preview winner / Preview no-winner** renders the form's current text against the live data.
- **Reset to default** restores the accepted copy from `LotterySettingsDefaults` (audited).
- The character count is visible while typing. A stored template that stops satisfying the contract at run time falls
  back to the default and raises one needs-admin alert — but a save should never get you there.

## Guesses

The existing award-guess flow: pick members, give a reason, and each gets one extra guess for the current round. The
same operation stays available to officers as `/lottery award` in Discord.

## Dangerous actions, in one list

| Action | What it does | Reversible? |
|---|---|---|
| **Run now** | draws, pays, posts and completes the due round live; on a voided due round it draws that date again | no — undo it with **Void last draw**, which is audited and leaves the post annotated |
| **Dry run now** | posts `[DRY RUN]` to the log channel, writes nothing | yes — nothing changed |
| **Void last draw** | reverses the round's ledger entries, marks the round void, annotates the announcement | no — audited, and the pot moves back; **Run now** then re-draws the same date |
| **Move one round** | moves a scheduled round to a new date/time; the round key follows the new local date, guesses stay | yes — move it again while it is still `scheduled` |
| **Retry announcement** | resumes an unfinished round with the same result | yes — it never re-draws |
| **Skip week** | marks the upcoming round skipped, guesses carry | not directly; the round stays `skipped` |
| **Pause automation** | stops the scheduler (draw and reminder) until unpaused | yes — toggle back |
| **Forfeit** | marks a winner's prizes forfeited and returns items to the pool | the payout status cannot be un-forfeited |
| **Pot adjustment** | moves the pot, floored at 1,000,000 Gil | only by the opposite adjustment, which is also audited |
| **Remove prize / Remove opt-in** | deletes an in-pool prize or an opt-in | no — the only deletes the lottery performs |

**Confirmations**: Run now, Skip, Void and Forfeit all confirm first. There is no silent redo anywhere: a completed
round is voided before a new one is run, and a drawn round always retries the same result.

## When something looks wrong

1. **Failure badge / `/health` shows `degraded` with a `pendingRoundKey`** — open **Overview**, read the failed step
   and the last error, then **Retry announcement** (or **Void last draw**). A pending round blocks the next
   scheduled round, so do not wait for the next date.
2. **`/health` shows `startupCheck: degraded`** — a required id or the pot configuration is missing. Fix the
   deployment config and restart; the admin page cannot change it. While it is degraded, neither the draw nor the
   scheduled reminder runs (the manual actions still do).
3. **A wrong draw was completed** — **Void last draw** with a reason. The ledger is reversed and the announcement is
   annotated so nobody pays out the old result; then **Run now** draws the same date again (the round is re-opened with
   a new attempt, and the ledger keys of the two attempts never collide).
4. **A round is on the wrong date** — **Schedule & reminders** → **Move one round**: leave the round blank for the next
   one or type its key, pick the new local date and time, confirm. Guesses stay and the pot is untouched until the run.
   Moving onto a date that already has a round is refused with a conflict, and only a `scheduled` round can be moved.
5. **A payout went wrong** — **Forfeit** returns the prize items to the pool; the Gil ledger is untouched by payout
   statuses and can only be corrected with an explicit pot adjustment.

Never restart the container to "fix" a degraded lottery detail: a restart re-runs the startup check but changes no
round state, and the round state is what the alert is about. See
[reading `/health`](cutover-runbook.md#reading-health).

## Permissions in one line

Admin-only: this page, `api/lottery/admin/*`, the pot, payouts, prizes, templates, settings, skip/void/retry,
dry-run and the manual run. Member-only: guess/change/view, the history page and the reminder opt-in. Officers keep
the Discord slash commands `/lottery run`, `/lottery remind` and `/lottery award`. Every settings, pot, payout and
prize action is audited with actor, time and old/new value or note.

## Related

- [acceptance map](acceptance.md) — which test proves each behaviour on this page
- [configuration](configuration.md) — the deployment flags, precedence and the `lottery` health detail
- [cutover runbook](cutover-runbook.md) — promotion, monitoring and rollback
- [operations](operations.md) — the index migration and the one-time backfill
- [the spec](../user-stories/automated-weekly-lottery.md) §5.9
