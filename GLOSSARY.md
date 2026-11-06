# ExcelBotCs

A companion platform for a Final Fantasy XIV Free Company: events, members, lottery, fights, and admin tools.

## Language

### Lottery

**Lottery**:
The weekly number-guessing game in which members submit guesses, one winning number is drawn, and winners share the pot.
_Avoid_: Raffle, draw (use "draw" for the act)

**Round**:
One lottery draw and its recorded outcome, identified by its scheduled run date. A skipped round has no draw and no pot change.
_Avoid_: Week, iteration

**Announcement**:
The weekly post in the lottery channel that reports the draw result, the pot, and the last five winners.
_Avoid_: Result post, winner post

**Winning number**:
The natural number from 1 to 99 drawn for a round.
_Avoid_: Roll, result, winning digit

**Guess**:
A member's chosen number for a round. A live guess lives in `lottery_guesses` (cleared when a round completes); when
the draw freezes the guess set it is copied to `lottery_entries`, one row per guess and round. That collection name is
fixed by the data model (spec §6), so code, specs and queries say `lottery_entries` — it is the persisted guess, not a
separate concept. Say "entry" only when naming the collection or the `LotteryEntry` type.
_Avoid_: Entry, ticket, pick

**Extra guess**:
An additional guess granted on top of the weekly guess, usually for event participation.
_Avoid_: Bonus guess, extra entry

**Reminder opt-in**:
A member's choice to receive the automatic pre-draw reminder. Default off; stored per Discord ID.
_Avoid_: Subscription, notification setting

**Reminder**:
The pre-draw post that pings opted-in members who still have unused guesses.
_Avoid_: Nudge, notification

**Pot**:
The Gil prize pool for the current round. Player-facing copy may call it the jackpot; code and specs use "pot".
_Avoid_: Jackpot (in code and specs), pool

**Base pot**:
The 1,000,000 Gil value the pot starts at and resets to after a win.
_Avoid_: Starting jackpot, minimum pot

**Weekly increment**:
The 1,000,000 Gil added to the pot after a round with no winner.
_Avoid_: Growth, accrual

**Riddle top-up**:
A manual credit to the pot from a riddle reward.
_Avoid_: Riddle bonus, riddle prize

**Donation top-up**:
A manual credit to the pot from a member donation.
_Avoid_: Donation (when referring to the ledger entry)

**Correction**:
A manual positive or negative adjustment to the pot, never below the base pot.
_Avoid_: Fix, adjustment

**Pot ledger**:
The append-only record of every change to the pot.
_Avoid_: Transactions, pot history

**Prize**:
A winner's share of the pot, plus any prize items assigned to that winner.
_Avoid_: Winnings, loot

**Split**:
The division of the pot among multiple winners in the same round.
_Avoid_: Share-out, division

**Winner**:
A member whose guess matches the round's winning number.
_Avoid_: Lucky member, jackpot winner

**Prize item**:
A non-Gil prize held in the prize pool, indivisible and assigned to a winner. The persisted winner field and template placeholder use the legacy name `BonusItems` / `{bonusItems}`.
_Avoid_: Bonus item (except for the field and placeholder), loot

**Prize pool**:
The additional non-Gil prizes available for the next draw; separate from the Gil pot.
_Avoid_: Bonus pool, side prizes

**Payout**:
The record of delivering a prize to a winner, with status pending, paid, or forfeited.
_Avoid_: Payment (when referring to the record)

**Previous winners**:
The historical list of past rounds' winners, guesses, prizes, and dates.
_Avoid_: Leaderboard, hall of fame
