export interface Announcement {
    Content: string, 
    Author: string,
    Attachments: MessageAttachment[],
    Timestamp: string
}

export interface MessageAttachment {
    Name: string,
    Url: string
}