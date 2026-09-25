/**
 * The appointment's start as the API expects it: the salon's wall-clock date and time written as an ISO string
 * with no conversion (the server reads a booking time as the salon's clock, never as an instant). Persian and
 * Arabic digits are accepted. Null when the date or time cannot be read.
 */
export function wallClockIso(date: string | null | undefined, time: string | null | undefined): string | null {
  if (!date || !time) return null
  const ascii = (value: string) =>
    value
      .replace(/[۰-۹]/g, (d) => String('۰۱۲۳۴۵۶۷۸۹'.indexOf(d)))
      .replace(/[٠-٩]/g, (d) => String('٠١٢٣٤٥٦٧٨٩'.indexOf(d)))
  const [year, month, day] = ascii(date).split('-').map(Number)
  const [hour, minute] = ascii(time).split(':').map(Number)
  const at = new Date(Date.UTC(year, (month ?? 1) - 1, day, hour, minute, 0))
  return Number.isNaN(at.getTime()) ? null : at.toISOString()
}
