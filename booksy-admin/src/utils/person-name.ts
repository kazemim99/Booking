/**
 * What counts as a person's real name (the same rule as booksy-frontend's core/utils/person-name).
 *
 * A person signed up by phone is stored as «مشتری 9384444636» / «ارائه‌دهنده 9123135143» — the word for their side
 * of the marketplace, then their phone's national number — until they give a name. Production QA 2026-09-23 found
 * the apps printing that as a name: "the number must never be written anywhere". Mirrors the API's `PersonName`:
 * a run of 7+ digits (any spacing, Latin or Persian digits) is a phone and never part of a name; a digits-only
 * surname is not a surname; the bare placeholder word is not a name.
 */

const PLACEHOLDER_WORDS = new Set(['مشتری', 'ارائه‌دهنده', 'ارائه دهنده'])

/** A run of digits this long is a phone number, however it is spaced; shorter ones can be part of a name. */
const PHONE_DIGITS = 7

const DIGIT = /[0-9۰-۹٠-٩]/g
const DIGIT_GROUP = /^[0-9۰-۹٠-٩+\-().\/]+$/

const digitCount = (token: string): number => token.match(DIGIT)?.length ?? 0
const isDigitGroup = (token: string): boolean => DIGIT_GROUP.test(token) && digitCount(token) > 0

/** A name held as one string with any phone number taken out; null when nothing real is left. */
export function personNameOrNull(name?: string | null): string | null {
  if (!name || !name.trim()) return null

  // Split on real whitespace only: the ZWNJ inside «ارائه‌دهنده» is part of the word.
  const kept: string[] = []
  let run: string[] = []
  const endRun = () => {
    // Consecutive digit groups ("0912 313 5143") are one number; drop it when it is a phone.
    if (run.reduce((n, t) => n + digitCount(t), 0) < PHONE_DIGITS) kept.push(...run)
    run = []
  }

  for (const token of name.trim().split(/\s+/)) {
    if (isDigitGroup(token)) {
      run.push(token)
      continue
    }
    endRun()
    kept.push(token)
  }
  endRun()

  const result = kept.join(' ')
  return result === '' || PLACEHOLDER_WORDS.has(result) ? null : result
}

/** The person's real name from their two parts, or null when they have none. */
export function realNameOrNull(firstName?: string | null, lastName?: string | null): string | null {
  const first = personNameOrNull(firstName) ?? ''
  const rawLast = lastName?.trim() ?? ''
  // The placeholder's surname is the phone's national number: a surname made of digits is not one.
  const last = isDigitGroup(rawLast) ? '' : (personNameOrNull(rawLast) ?? '')
  const full = `${first} ${last}`.trim()
  return full === '' ? null : full
}
