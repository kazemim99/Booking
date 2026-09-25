import { describe, expect, it } from 'vitest'
import { formatPhone } from '../phone'

// Numbers are stored in E.164 (+989123135143). Shown raw inside the right-to-left admin layout, the
// "+" drifted to the far end and the number read as a foreign one; admins asked for the local form
// everyone in Iran reads and dials (2026-09-19).
const LRI = '⁦'
const PDI = '⁩'
const plain = (s: string) => s.replace(/[⁦⁩]/g, '')

describe('formatPhone', () => {
  it('shows an Iranian mobile in the local form, grouped for reading', () => {
    expect(plain(formatPhone('+989123135143'))).toBe('0912 313 5143')
  })

  it('accepts the 0098 and bare 98 spellings of the same number', () => {
    expect(plain(formatPhone('00989123135143'))).toBe('0912 313 5143')
    expect(plain(formatPhone('989123135143'))).toBe('0912 313 5143')
    expect(plain(formatPhone('09123135143'))).toBe('0912 313 5143')
  })

  it('keeps the digits in reading order inside right-to-left text', () => {
    const shown = formatPhone('+989123135143')
    expect(shown.startsWith(LRI)).toBe(true)
    expect(shown.endsWith(PDI)).toBe(true)
  })

  it('leaves a non-Iranian number as it was given', () => {
    expect(plain(formatPhone('+442071838750'))).toBe('+442071838750')
  })

  it('shows nothing for no number', () => {
    expect(formatPhone(undefined)).toBe('')
    expect(formatPhone('')).toBe('')
  })
})
