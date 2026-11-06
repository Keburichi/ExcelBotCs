/** Thousands-separated Gil, e.g. 80000000 -> "80,000,000". */
export function formatGil(value: number): string {
  return new Intl.NumberFormat('en-US', { maximumFractionDigits: 0 }).format(value)
}

/** The two-letter fallback the winner card shows when no Discord avatar is known. */
export function initials(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean)
  if (parts.length === 0)
    return '?'
  if (parts.length === 1)
    return parts[0].slice(0, 2).toUpperCase()
  return (parts[0][0] + parts[1][0]).toUpperCase()
}

/** A round's date as the admin tables show it: the key is already `YYYY-MM-DD`. */
export function formatRoundKey(roundKey: string | null): string {
  return roundKey ?? '—'
}

/**
 * An instant in a lottery timezone, so the page agrees with the configured run/reminder schedule rather than the
 * browser's locale. An unknown timezone falls back to the local rendering instead of throwing.
 */
export function formatInstant(iso: string | null, timezone?: string): string {
  if (!iso)
    return '—'

  const date = new Date(iso)
  if (Number.isNaN(date.getTime()))
    return iso

  try {
    return new Intl.DateTimeFormat('en-GB', {
      timeZone: timezone || undefined,
      dateStyle: 'medium',
      timeStyle: 'short',
    }).format(date)
  }
  catch {
    return date.toLocaleString()
  }
}

/** The day-and-time form the Overview shows for the next run. */
export function formatDayTime(day: string, time: string): string {
  return `${day} ${time}`
}
