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
