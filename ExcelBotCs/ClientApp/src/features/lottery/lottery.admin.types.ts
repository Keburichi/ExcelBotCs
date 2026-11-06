/// <summary>
/// The admin page's API contract (#114, spec §5.9 and §8). These mirror the `Models/DTO/LotteryAdminDto.cs` records
/// one-for-one, so a drift in the payload shows up as a type error here rather than a blank card at runtime.
/// </summary>

/** The status pill inputs: "live" | "dry-run" | "paused" | "disabled". */
export type LotteryAutomationKind = 'live' | 'dry-run' | 'paused' | 'disabled'

export type LotteryPayoutStatus = 'pending' | 'paid' | 'forfeited'
export type LotteryPrizeStatus = 'in-pool' | 'awarded'

/** The Overview payload. Every Discord id is a string: a snowflake loses precision as a JavaScript number. */
export interface LotteryAdminStatus {
  AutomationStatus: LotteryAutomationKind
  AutomationLabel: string
  SchedulerEnabled: boolean
  AutomationEnabled: boolean
  DryRun: boolean
  PauseAutomation: boolean
  Degraded: boolean
  Blocked: boolean
  Pot: number
  PendingPayouts: number
  PoolPrizes: number
  OptedIn: number
  EligibleReminders: number
  RemindersEnabled: boolean
  LatestRound: LotteryAdminRound | null
  PendingRound: LotteryAdminRound | null
  NextRunAtUtc: string | null
  NextRoundKey: string | null
  LastError: string | null
  Deployment: LotteryAdminDeployment
  LastSettingsChange: LotteryAdminSettingsChange | null
  InvalidSettingsFields: string[]
}

export interface LotteryAdminRound {
  RoundKey: string
  Status: string
  ScheduledAt: string
  RunAt: string | null
  WinningNumber: number | null
  PotBefore: number | null
  PotAfter: number | null
  FailedStep: string | null
  Attempts: number
  NextRetryAt: string | null
  LastError: string | null
  AnnouncementMessageId: string | null
}

/** Deployment config, shown read-only: role/channel/emoji ids and the web base URL. */
export interface LotteryAdminDeployment {
  LotteryChannelId: string
  LotteryMemberRoleId: string
  LotteryCustomEmojiId: string
  WebBaseUrl: string
}

export interface LotteryAdminSettingsChange {
  Version: number
  ChangedAtUtc: string
  ActorDiscordId: string | null
  Fields: string[]
}

/** The settings form: the admin-editable set plus the read-only flags and the reset defaults. */
export interface LotterySettings {
  RunDay: string
  RunTime: string
  RunTimezone: string
  ReminderDay: string
  ReminderTime: string
  ReminderTimezone: string
  RemindersEnabled: boolean
  PauseAutomation: boolean
  WinTemplate: string
  NoWinTemplate: string
  Version: number
  UpdatedByDiscordId: string | null
  AutomationEnabled: boolean
  DryRun: boolean
  AutomationStatus: LotteryAutomationKind
  AutomationLabel: string
  BasePot: number
  WeeklyIncrement: number
  InvalidFields: string[]
  Defaults: LotterySettingsDefaults
}

export interface LotterySettingsDefaults {
  RunDay: string
  RunTime: string
  RunTimezone: string
  ReminderDay: string
  ReminderTime: string
  ReminderTimezone: string
  WinTemplate: string
  NoWinTemplate: string
}

/** The whole editable set, because save is the only commit. `ResetTemplates` restores the default result copy. */
export interface LotterySettingsSaveRequest {
  RunDay: string
  RunTime: string
  RunTimezone: string
  ReminderDay: string
  ReminderTime: string
  ReminderTimezone: string
  RemindersEnabled: boolean
  PauseAutomation: boolean
  WinTemplate: string
  NoWinTemplate: string
  ResetTemplates: boolean
}

export interface LotterySettingsSaveResponse {
  Success: boolean
  Changed: boolean
  Settings: LotterySettings | null
  Errors: LotteryFieldError[]
}

/** One rejected form field; `Field` is the DTO property name the editor binds to. */
export interface LotteryFieldError {
  Field: string
  Message: string
}

export interface PotAdjustmentRequest {
  Amount: number
  Reason: string
  Direction: 'increase' | 'decrease'
  IdempotencyKey: string | null
}

export interface PotAdjustmentResponse {
  Success: boolean
  Replayed: boolean
  Balance: number
  RequestedAmount: number
  AppliedAmount: number
  Entry: LotteryLedgerEntry | null
  Errors: LotteryFieldError[]
}

export interface LotteryLedgerEntry {
  Id: string
  RoundKey: string | null
  Type: string
  Amount: number
  Reason: string
  ActorDiscordId: string | null
  BalanceAfter: number
  IdempotencyKey: string
  CreatedAtUtc: string
}

export interface LotteryLedgerPage {
  Items: LotteryLedgerEntry[]
  NextCursor: string | null
}

export interface LotteryPayout {
  Id: string
  RoundKey: string
  DiscordId: string | null
  DisplayName: string
  Guess: number
  GilPrize: number
  Items: string[]
  PayoutStatus: LotteryPayoutStatus
  PayoutActorDiscordId: string | null
  PayoutAt: string | null
  PayoutNote: string | null
}

export interface LotteryPayoutPage {
  Items: LotteryPayout[]
  NextCursor: string | null
  PendingCount: number
}

export interface PayoutUpdateRequest {
  Status: LotteryPayoutStatus
  Note: string | null
}

export interface PayoutUpdateResponse {
  Success: boolean
  Code: string
  Message: string
  Payout: LotteryPayout | null
  ReturnedPrizes: string[]
}

export interface PrizePoolItem {
  Id: string
  Name: string
  Status: LotteryPrizeStatus
  AwardedToDiscordId: string | null
  AwardedToName: string | null
  AwardedRoundKey: string | null
  AddedByDiscordId: string | null
  AddedAt: string
}

export interface LotteryPrizePool {
  Items: PrizePoolItem[]
  InPoolCount: number
  AwardedCount: number
}

export interface AssignPrizeRequest {
  RoundKey: string
  WinnerId: string
}

export interface PrizeResponse {
  Success: boolean
  Code: string
  Message: string
  Prize: PrizePoolItem | null
  Winner: LotteryPayout | null
  Errors: LotteryFieldError[]
}

export interface ReminderOptIn {
  DiscordId: string
  DisplayName: string | null
  IsCurrentMember: boolean
  IsEligible: boolean
  OptedInAtUtc: string
}

export interface LotteryOptIns {
  Items: ReminderOptIn[]
  OptedIn: number
  Eligible: number
}

export interface ReminderTestResponse {
  Posted: boolean
  Disposition: string
  Message: string
  RoundKey: string | null
  Recipients: number
}

export interface LotteryAnnouncementPreview {
  Variant: string
  Text: string
  VisibleLength: number
  RawLength: number
}

export interface ManualActionRequest {
  Reason: string | null
  RoundKey: string | null
}

/** The engine's typed disposition, so the page never parses prose to decide what happened. */
export interface ManualActionResponse {
  Disposition: string
  Success: boolean
  Message: string
  RoundKey: string | null
  Pot: number | null
}
