import { describe, it, expect } from 'vitest'
import { wallClockIso } from '../wall-clock'

describe('wallClockIso', () => {
  it('writes the salon clock as-is, with no timezone shift', () => {
    expect(wallClockIso('2026-10-03', '11:00')).toBe('2026-10-03T11:00:00.000Z')
  })

  it('reads Persian and Arabic digits', () => {
    expect(wallClockIso('۲۰۲۶-۱۰-۰۳', '۰۹:۳۰')).toBe('2026-10-03T09:30:00.000Z')
    expect(wallClockIso('٢٠٢٦-١٠-٠٣', '١٤:١٥')).toBe('2026-10-03T14:15:00.000Z')
  })

  it('is null when either part is missing or unreadable', () => {
    expect(wallClockIso(null, '10:00')).toBeNull()
    expect(wallClockIso('2026-10-03', null)).toBeNull()
    expect(wallClockIso('not-a-date', '10:00')).toBeNull()
  })
})
