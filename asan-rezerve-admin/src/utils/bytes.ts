const UNITS = ['KB', 'MB', 'GB', 'TB'] as const

/** A byte count for people: `512 B`, `1.5 KB`, `350.0 MB` (binary units, one decimal). */
export function formatBytes(bytes: number | null | undefined): string {
  if (bytes === null || bytes === undefined) return '—'
  if (bytes < 1024) return `${bytes} B`

  let value = bytes / 1024
  let unit = 0
  while (value >= 1024 && unit < UNITS.length - 1) {
    value /= 1024
    unit++
  }
  return `${value.toFixed(1)} ${UNITS[unit]}`
}
