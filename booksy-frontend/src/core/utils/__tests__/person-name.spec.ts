import { describe, expect, it } from 'vitest'
import { hasFullName, personNameOrNull, realNameOrNull, userNames } from '../person-name'

// Production QA 2026-09-23: a person signed up by phone is «مشتری 9384444636» / «ارائه‌دهنده 9123135143» until they
// give a name, and the apps printed that — "the number must never be written anywhere". Mirrors the API's PersonName.
describe('realNameOrNull', () => {
  it('is first and last together; half a name is a name', () => {
    expect(realNameOrNull('سارا', 'احمدی')).toBe('سارا احمدی')
    expect(realNameOrNull(' سارا ', null)).toBe('سارا')
  })

  it('treats the sign-in placeholder and any phone number as no name', () => {
    expect(realNameOrNull('مشتری', '9384444636')).toBeNull()
    expect(realNameOrNull('ارائه‌دهنده', '۹۱۲۳۱۳۵۱۴۳')).toBeNull()
    expect(realNameOrNull('09123135143', undefined)).toBeNull()
    expect(realNameOrNull('ارائه‌دهنده 9123135143', '')).toBeNull()
    expect(realNameOrNull(undefined, undefined)).toBeNull()
  })

  it('never takes a number for a surname', () => {
    expect(realNameOrNull('سارا', '+989123135143')).toBe('سارا')
    expect(realNameOrNull('سارا', '0912 313 5143')).toBe('سارا')
  })
})

describe('personNameOrNull', () => {
  it('is null for a placeholder or a phone', () => {
    for (const name of ['ارائه‌دهنده 9123135143', 'مشتری', '09123135143', '+98 912 313 5143', '  ', null, undefined]) {
      expect(personNameOrNull(name)).toBeNull()
    }
  })

  it('keeps a real name, short numbers included, and drops a phone number', () => {
    expect(personNameOrNull('سالن نهال')).toBe('سالن نهال')
    expect(personNameOrNull('سالن ۲۴ ساعته')).toBe('سالن ۲۴ ساعته')
    expect(personNameOrNull('مریم 09123135143')).toBe('مریم')
  })
})

describe('userNames — what the header calls the signed-in person', () => {
  it('a real name: the first name for the button, the full name for the menu', () => {
    expect(userNames({ firstName: 'سارا', lastName: 'احمدی', fullName: 'سارا احمدی' })).toEqual({
      first: 'سارا',
      full: 'سارا احمدی',
    })
  })

  it('a phone sign-up that skipped the name has none — not «مشتری 9384444636»', () => {
    expect(userNames({ firstName: 'مشتری', lastName: '9384444636', fullName: 'مشتری 9384444636' })).toEqual({
      first: null,
      full: null,
    })
  })

  it('nobody signed in has none', () => {
    expect(userNames(null)).toEqual({ first: null, full: null })
  })
})

// QA 2026-09-24: a salon cannot tell who it is confirming. A booking needs a real first AND last name.
describe('hasFullName — enough of a name to book with', () => {
  it('a real first and last name is enough', () => {
    expect(hasFullName('مصطفی', 'کاظمی')).toBe(true)
  })

  it('the phone sign-up placeholder is not', () => {
    expect(hasFullName('مشتری', '9384444636')).toBe(false)
  })

  it('a first name alone, or a last name alone, is not', () => {
    expect(hasFullName('مصطفی', '')).toBe(false)
    expect(hasFullName(null, 'کاظمی')).toBe(false)
  })
})
