# Manual seed — historical winners and the opening pot

The application **never** seeds these documents. There is no startup seeding and no seed job: the history is
inserted by hand at cutover, following spec §6 "Manual seed shapes". Re-running the application, the index
migration or the archive backfill does not create them.

Two manual inserts are required:

1. the 35 historical winner rows in `lottery_winners`;
2. exactly one opening-balance entry in `lottery_pot_ledger`.

## What the application will not do for you

- It does not create historical winners from `lottery_results`.
- It does not create `lottery_entries` for historical rounds; the winner's guess lives in `lottery_winners`, and
  inserting only the winning guess would skew the most-guessed statistic.
- It does not replay the historical prizes as ledger entries. Those prizes were paid years ago; replaying them
  would double-count the current pot. The ledger starts at the opening balance.
- It does not set the current pot to any value — the pot is whatever the newest ledger entry's `BalanceAfter`
  says.

## Shape 1 — a historical winner

One document per source row. `DiscordId` is `null` for every historical row (they predate the Discord id record),
`PayoutStatus` is `paid`, and `PayoutNote` marks the row as a historical record.

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

Field rules for every row:

| Field | Value |
|---|---|
| `RoundKey` | the row's date, `DD.MM.YY` converted to `YYYY-MM-DD` |
| `DiscordId` | `null` |
| `DisplayName` | exactly as written, including apostrophes (`Ta'veren`, `H'molosi`) |
| `Guess` | the recorded number (row 21 is `30`) |
| `GilPrize` | the recorded Gil amount, `NumberLong` |
| `BonusItems` | `[]` when the row has no item, otherwise one element as written — `"Yan"`, `"Figmental Weapon Coffer"`, `"Fuax Mount"` (spelling preserved), `"Cosmic Mount"`, `"Chaotic Mount"` |
| `PayoutStatus` | `"paid"` |
| `PayoutActorDiscordId`, `PayoutAt` | `null` |
| `PayoutNote` | `"historical record"` |
| `DateCreated`, `DateModified` | the draw date, or the insert time |

Rows 25 and 26 are the same draw (two winners, guess 66, 2,500,000 Gil each), so they share `RoundKey`
`2026-01-01`. No unique index prevents that. No `lottery_rounds` documents are required for the historical rows:
the history page and the last-five query read `lottery_winners`.

### The 35 rows

| # | RoundKey | DisplayName | Guess | GilPrize | BonusItems |
|---|---|---|---|---|---|
| 1 | 2024-09-15 | Skor | 44 | 1500000 | |
| 2 | 2024-11-10 | Rhytar | 15 | 7000000 | |
| 3 | 2024-11-17 | Kei | 32 | 1000000 | |
| 4 | 2025-03-17 | Bonedaddy | 64 | 21000000 | |
| 5 | 2025-05-12 | Ta'veren | 28 | 18000000 | |
| 6 | 2025-05-18 | Mari | 82 | 9000000 | |
| 7 | 2025-06-09 | Aurora | 17 | 7500000 | |
| 8 | 2025-06-16 | Mari | 71 | 5000000 | |
| 9 | 2025-06-23 | Leon | 80 | 3000000 | |
| 10 | 2025-07-22 | H'molosi | 71 | 26000000 | |
| 11 | 2025-07-29 | H'molosi | 71 | 1000000 | |
| 12 | 2025-08-05 | Oriel | 26 | 1000000 | |
| 13 | 2025-08-12 | Nazha | 99 | 8000000 | |
| 14 | 2025-08-19 | Mina | 53 | 1000000 | |
| 15 | 2025-09-09 | Shivero | 55 | 8000000 | `["Yan"]` |
| 16 | 2025-09-16 | Keburichi | 21 | 1000000 | `["Figmental Weapon Coffer"]` |
| 17 | 2025-09-23 | Udoku | 61 | 1000000 | |
| 18 | 2025-09-30 | H'molosi | 43 | 1000000 | |
| 19 | 2025-11-04 | Amara | 35 | 30000000 | |
| 20 | 2025-11-18 | Aria | 35 | 2000000 | |
| 21 | 2025-11-25 | Mari | 30 | 2000000 | |
| 22 | 2025-12-02 | Siegward | 68 | 5000000 | |
| 23 | 2025-12-09 | Ta'veren | 82 | 6000000 | |
| 24 | 2025-12-23 | Mina | 56 | 18080000 | |
| 25 | 2026-01-01 | Karya | 66 | 2500000 | |
| 26 | 2026-01-01 | Uma | 66 | 2500000 | |
| 27 | 2026-02-10 | Ta'veren | 83 | 14300000 | `["Fuax Mount"]` |
| 28 | 2026-03-17 | Keburichi | 92 | 33000000 | `["Cosmic Mount"]` |
| 29 | 2026-03-24 | Oriana | 18 | 1000000 | |
| 30 | 2026-03-31 | Eva | 68 | 2450000 | |
| 31 | 2026-05-05 | Oriel | 24 | 24100000 | |
| 32 | 2026-05-12 | Skor | 90 | 17050000 | |
| 33 | 2026-05-26 | Elara | 94 | 1100000 | |
| 34 | 2026-06-17 | Elara | 99 | 6700000 | |
| 35 | 2026-07-21 | Siegward | 56 | 80000000 | `["Chaotic Mount"]` |

Every row above is inserted with `DiscordId: null`, `PayoutStatus: "paid"`, `PayoutActorDiscordId: null`,
`PayoutAt: null`, `PayoutNote: "historical record"`. Empty `BonusItems` means `[]`.

The source list (raw text) lives in [issue #102](https://github.com/Keburichi/ExcelBotCs/issues/102); this table
is its normalized form. After inserting, the history summary should show **35 winners / 34 draws** (rows 25–26
share a draw).

## Shape 2 — the opening pot

Exactly one document, at cutover. `RoundKey` is `null` because the balance belongs to no round.

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

`IdempotencyKey: "opening-balance"` is also protected by the unique index, so a second insert of the same
opening balance fails instead of silently doubling the pot. `Amount` and `BalanceAfter` are the same number:
the correction takes the pot from nothing to the current pot.

The historical 35 prizes are **not** replayed as `payout` ledger entries.

## Order of operations at cutover

1. Deploy with the automation disabled.
2. Run the index migration (see [operations](operations.md)).
3. Insert the 35 winner rows and the opening-pot entry (this page).
4. Run the one-time `lottery_results` backfill (see [operations](operations.md)).
5. Configure the schedule, reminders and templates, then dry-run one week.
