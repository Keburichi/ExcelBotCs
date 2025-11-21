import type {FCEvent} from '@/features/events/events.types'
import {RRule} from 'rrule'

export interface RecurrenceConfig {
    enabled: boolean
    frequency: 'DAILY' | 'WEEKLY' | 'MONTHLY' | 'YEARLY'
    interval: number
    endType: 'never' | 'count' | 'until'
    count?: number
    until?: Date
    byWeekday?: number[] // 0=MO, 1=TU, 2=WE, 3=TH, 4=FR, 5=SA, 6=SU (RRule format)
    byMonthDay?: number
}

/**
 * Generates an iCal string from event data and optional recurrence configuration
 */
export function generateICalString(event: Partial<FCEvent>, recurrence?: RecurrenceConfig): string {
    const startDate = event.StartDate ? new Date(event.StartDate) : new Date()
    // Default event duration changed to 30 minutes
    const endDate = new Date(startDate.getTime() + (event.Duration || 30) * 60 * 1000)

    // Format dates as iCal DTSTART/DTEND (UTC)
    const formatICalDateTime = (date: Date): string => {
        return `${date.toISOString().replace(/[-:]/g, '').split('.')[0]}Z`
    }

    const lines: string[] = [
        'BEGIN:VCALENDAR',
        'VERSION:2.0',
        'PRODID:-//ExcelBotCs//Event Calendar//EN',
        'BEGIN:VEVENT',
        `UID:${event.Id || generateUID()}`,
        `DTSTAMP:${formatICalDateTime(new Date())}`,
        `DTSTART:${formatICalDateTime(startDate)}`,
        `DTEND:${formatICalDateTime(endDate)}`,
        `SUMMARY:${escapeICalText(event.Name || '')}`,
    ]

    if (event.Description) {
        lines.push(`DESCRIPTION:${escapeICalText(event.Description)}`)
    }

    // Add recurrence rule if enabled
    if (recurrence?.enabled) {
        const rrule = buildRRule(startDate, recurrence)
        lines.push(`RRULE:${rrule}`)
    }

    lines.push('END:VEVENT', 'END:VCALENDAR')

    return lines.join('\r\n')
}

/**
 * Builds an RRULE string from recurrence configuration
 */
function buildRRule(startDate: Date, config: RecurrenceConfig): string {
    const parts: string[] = [`FREQ=${config.frequency}`]

    if (config.interval > 1) {
        parts.push(`INTERVAL=${config.interval}`)
    }

    // Handle end conditions
    if (config.endType === 'count' && config.count) {
        parts.push(`COUNT=${config.count}`)
    } else if (config.endType === 'until' && config.until) {
        const formatICalDate = (date: Date): string => {
            return `${date.toISOString().replace(/[-:]/g, '').split('.')[0]}Z`
        }
        parts.push(`UNTIL=${formatICalDate(config.until)}`)
    }

    // Weekly recurrence: specify days
    if (config.frequency === 'WEEKLY' && config.byWeekday && config.byWeekday.length > 0) {
        const dayAbbr = ['MO', 'TU', 'WE', 'TH', 'FR', 'SA', 'SU']
        const days = config.byWeekday.map(d => dayAbbr[d]).join(',')
        parts.push(`BYDAY=${days}`)
    }

    // Monthly recurrence: specify day of month
    if (config.frequency === 'MONTHLY' && config.byMonthDay) {
        parts.push(`BYMONTHDAY=${config.byMonthDay}`)
    }

    return parts.join(';')
}

/**
 * Parses an iCal string and extracts recurrence configuration
 */
export function parseICalString(iCalString: string): RecurrenceConfig | null {
    if (!iCalString)
        return null

    const rruleMatch = iCalString.match(/RRULE:([^\r\n]+)/)
    if (!rruleMatch)
        return null

    const rruleString = rruleMatch[1]
    const parts = rruleString.split(';').reduce((acc, part) => {
        const [key, value] = part.split('=')
        acc[key] = value
        return acc
    }, {} as Record<string, string>)

    const config: RecurrenceConfig = {
        enabled: true,
        frequency: (parts.FREQ as RecurrenceConfig['frequency']) || 'DAILY',
        interval: Number.parseInt(parts.INTERVAL || '1'),
        endType: 'never',
    }

    if (parts.COUNT) {
        config.endType = 'count'
        config.count = Number.parseInt(parts.COUNT)
    } else if (parts.UNTIL) {
        config.endType = 'until'
        config.until = new Date(
            parts.UNTIL.replace(/(\d{4})(\d{2})(\d{2})T(\d{2})(\d{2})(\d{2})Z/, '$1-$2-$3T$4:$5:$6Z'),
        )
    }

    if (parts.BYDAY) {
        const dayAbbr = ['MO', 'TU', 'WE', 'TH', 'FR', 'SA', 'SU']
        config.byWeekday = parts.BYDAY.split(',').map(day => dayAbbr.indexOf(day)).filter(i => i !== -1)
    }

    if (parts.BYMONTHDAY) {
        config.byMonthDay = Number.parseInt(parts.BYMONTHDAY)
    }

    return config
}

/**
 * Generates a human-readable description of recurrence pattern
 */
export function describeRecurrence(config: RecurrenceConfig, iCalString?: string): string {
    if (!config.enabled)
        return ''

    const freq = config.frequency.toLowerCase()
    const interval = config.interval

    let description = interval === 1 ? `Every ${freq.slice(0, -2)}` : `Every ${interval} ${freq.toLowerCase()}`

    if (config.frequency === 'WEEKLY' && config.byWeekday && config.byWeekday.length > 0) {
        const dayNames = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday']
        const days = config.byWeekday.map(d => dayNames[d]).join(', ')
        description += ` on ${days}`
    }

    if (config.frequency === 'MONTHLY' && config.byMonthDay) {
        description += ` on day ${config.byMonthDay}`
    }

    if (config.endType === 'count' && config.count) {
        description += `, ${config.count} times`
    } else if (config.endType === 'until' && config.until) {
        description += ` until ${config.until.toLocaleDateString()}`
    } else if (config.endType === 'never') {
        description += ' (ongoing)'
    }

    // Try to calculate and show last occurrence if iCal string is provided
    if (iCalString && config.endType !== 'never') {
        try {
            const lastOccurrence = calculateLastOccurrence(iCalString)
            if (lastOccurrence) {
                description += ` • Final occurrence: ${lastOccurrence.toLocaleDateString()}`
            }
        } catch {
            // Ignore errors in calculating last occurrence
        }
    }

    return description
}

/**
 * Calculates the last occurrence date from an iCal string
 */
function calculateLastOccurrence(iCalString: string): Date | null {
    try {
        const rruleMatch = iCalString.match(/RRULE:([^\r\n]+)/)
        const dtstartMatch = iCalString.match(/DTSTART:(\d{8}T\d{6}Z)/)

        if (!rruleMatch || !dtstartMatch)
            return null

        const rruleString = rruleMatch[1]
        const dtstartString = dtstartMatch[1]

        // Parse DTSTART
        const year = Number.parseInt(dtstartString.substring(0, 4))
        const month = Number.parseInt(dtstartString.substring(4, 6)) - 1
        const day = Number.parseInt(dtstartString.substring(6, 8))
        const hour = Number.parseInt(dtstartString.substring(9, 11))
        const minute = Number.parseInt(dtstartString.substring(11, 13))
        const second = Number.parseInt(dtstartString.substring(13, 15))
        const dtstart = new Date(Date.UTC(year, month, day, hour, minute, second))

        // Parse RRULE using rrule library
        const rrule = RRule.fromString(`DTSTART:${dtstartString}\nRRULE:${rruleString}`)

        // Get all occurrences (limited to reasonable timeframe)
        const occurrences = rrule.all((date, i) => {
            // Limit to 1000 occurrences or 10 years
            return i < 1000 && date < new Date(Date.now() + 10 * 365 * 24 * 60 * 60 * 1000)
        })

        if (occurrences.length > 0) {
            return occurrences[occurrences.length - 1]
        }
    } catch {
        // Return null if parsing fails
    }

    return null
}

/**
 * Checks if an iCal string contains recurrence rules
 */
export function isRecurring(iCalString: string): boolean {
    if (!iCalString)
        return false
    return iCalString.includes('RRULE:')
}

// Helper functions
function generateUID(): string {
    return `${Date.now()}-${Math.random().toString(36).substr(2, 9)}@excelbot`
}

function escapeICalText(text: string): string {
    return text
        .replace(/\\/g, '\\\\')
        .replace(/;/g, '\\;')
        .replace(/,/g, '\\,')
        .replace(/\n/g, '\\n')
}
