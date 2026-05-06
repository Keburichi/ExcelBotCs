export interface MentionData {
  Users: Record<string, string>
  Roles: Record<string, string>
  Channels: Record<string, string>
}

export interface Announcement {
  Content: string
  Author: string
  AuthorAvatarUrl: string | null
  Attachments: MessageAttachment[]
  Timestamp: string
  Mentions: MentionData
}

export interface MessageAttachment {
  Name: string
  Url: string
}
