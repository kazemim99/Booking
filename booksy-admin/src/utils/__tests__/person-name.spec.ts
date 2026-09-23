import { describe, expect, it } from 'vitest'
import { realNameOrNull } from '../person-name'

// Production QA 2026-09-23: a person signed up by phone is stored as «مشتری 9384444636» / «ارائه‌دهنده 9123135143»
// until they give a name, and every app printed that as their name — "the number must never be written anywhere".
// The users list shows the real name, or says there is none; the phone has its own column. Mirrors the API's
// PersonName rule.
describe('realNameOrNull', () => {
  it('is first and last together', () => {
    expect(realNameOrNull('سارا', 'احمدی')).toBe('سارا احمدی')
    expect(realNameOrNull('سارا', null)).toBe('سارا')
  })

  it('treats the sign-in placeholder and any phone number as no name', () => {
    expect(realNameOrNull('مشتری', '9384444636')).toBeNull()
    expect(realNameOrNull('ارائه‌دهنده', '9123135143')).toBeNull()
    expect(realNameOrNull('09123135143', '')).toBeNull()
    expect(realNameOrNull('ارائه‌دهنده 9123135143', undefined)).toBeNull()
    expect(realNameOrNull(undefined, undefined)).toBeNull()
  })

  it('never takes a number for a surname, and keeps short numbers people choose', () => {
    expect(realNameOrNull('سارا', '+989123135143')).toBe('سارا')
    expect(realNameOrNull('Admin', 'User 2')).toBe('Admin User 2')
  })
})
