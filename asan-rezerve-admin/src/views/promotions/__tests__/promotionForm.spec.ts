import { describe, it, expect } from 'vitest'
import type { Promotion } from '../../../api/promotions.api'
import {
  availableActions,
  benefitText,
  emptyForm,
  formFromPromotion,
  generateCode,
  toPayload,
  usageText,
  validateForm,
  type PromotionForm,
} from '../promotionForm'

const NOW = new Date('2026-10-01T06:30:00Z')

function form(overrides: Partial<PromotionForm> = {}): PromotionForm {
  return { ...emptyForm(), title: 'نوروز', ...overrides }
}

describe('validateForm mirrors the server rules', () => {
  it('accepts a plain automatic percentage', () => {
    expect(validateForm(form(), NOW)).toEqual({})
  })

  it('requires a title', () => {
    expect(validateForm(form({ title: '   ' }), NOW).title).toBe('promotions.errors.titleRequired')
  })

  it.each([0, 0.5, 91, 100, null])('refuses a percentage of %s', (value) => {
    expect(validateForm(form({ discountValue: value }), NOW).discountValue).toBe('promotions.errors.percentRange')
  })

  it('accepts 1 and 90 percent', () => {
    expect(validateForm(form({ discountValue: 1 }), NOW)).toEqual({})
    expect(validateForm(form({ discountValue: 90 }), NOW)).toEqual({})
  })

  it('wants a whole positive amount for a fixed discount', () => {
    expect(validateForm(form({ discountKind: 'FixedAmount', discountValue: 0 }), NOW).discountValue).toBeDefined()
    expect(validateForm(form({ discountKind: 'FixedAmount', discountValue: 1000.5 }), NOW).discountValue).toBeDefined()
    expect(validateForm(form({ discountKind: 'FixedAmount', discountValue: 50000 }), NOW)).toEqual({})
  })

  it.each(['', 'ab', 'نوروز', 'HAS SPACE', 'X'.repeat(21)])('refuses the code %j', (code) => {
    expect(validateForm(form({ activation: 'Code', code }), NOW).code).toBeDefined()
  })

  it('accepts a lower-case code (it is upper-cased on send)', () => {
    expect(validateForm(form({ activation: 'Code', code: 'yalda-1405' }), NOW)).toEqual({})
  })

  it('needs the end after the start and in the future', () => {
    expect(validateForm(form({ startsAt: '2026-10-05T00:00:00Z', endsAt: '2026-10-04T00:00:00Z' }), NOW).endsAt)
      .toBe('promotions.errors.endBeforeStart')
    expect(validateForm(form({ startsAt: '2026-09-01T00:00:00Z', endsAt: '2026-09-30T00:00:00Z' }), NOW).endsAt)
      .toBe('promotions.errors.endInPast')
  })

  it('needs a daily window that starts before it ends', () => {
    expect(validateForm(form({ useDailyWindow: true, dailyStartTime: '14:00', dailyEndTime: '10:00' }), NOW).dailyStartTime)
      .toBeDefined()
    expect(validateForm(form({ useDailyWindow: false, dailyStartTime: '14:00', dailyEndTime: '10:00' }), NOW)).toEqual({})
  })

  it('refuses limits below one', () => {
    const errors = validateForm(form({ totalUsageLimit: 0, perCustomerLimit: 0 }), NOW)
    expect(errors.totalUsageLimit).toBeDefined()
    expect(errors.perCustomerLimit).toBeDefined()
  })
})

describe('toPayload', () => {
  it('sends only what applies', () => {
    const payload = toPayload(form({
      activation: 'Code', code: ' spring ', discountKind: 'FixedAmount', discountValue: 50000,
      maxDiscountAmount: 999, daysOfWeek: [3, 6], useDailyWindow: false, description: '  ',
    }))

    expect(payload.code).toBe('SPRING')
    expect(payload.maxDiscountAmount).toBeNull() // a cap is only for percentages
    expect(payload.daysOfWeek).toEqual([3, 6])
    expect(payload.dailyStartTime).toBeNull()
    expect(payload.description).toBeNull()
    expect(payload.serviceIds).toBeNull() // campaigns never target services
  })

  it('an automatic promotion sends no code and empty days mean every day', () => {
    const payload = toPayload(form({ code: 'LEFTOVER' }))
    expect(payload.code).toBeNull()
    expect(payload.daysOfWeek).toBeNull()
  })

  it('round-trips a stored promotion through the form', () => {
    const stored = {
      title: 'یلدا', description: null, activation: 'Code', code: 'YALDA', discountKind: 'Percentage', discountValue: 15,
      maxDiscountAmount: 200000, minimumSubtotal: null, newCustomersOnly: true, serviceIds: [], daysOfWeek: [6],
      dailyStartTime: '10:00', dailyEndTime: '13:00', startsAt: '2026-12-21T20:30:00Z', endsAt: '2026-12-28T20:30:00Z',
      totalUsageLimit: 100, perCustomerLimit: 1,
    } as unknown as Promotion

    const payload = toPayload(formFromPromotion(stored))

    expect(payload).toMatchObject({
      title: 'یلدا', code: 'YALDA', discountValue: 15, maxDiscountAmount: 200000, newCustomersOnly: true,
      daysOfWeek: [6], dailyStartTime: '10:00', dailyEndTime: '13:00', totalUsageLimit: 100, perCustomerLimit: 1,
    })
  })
})

describe('display helpers', () => {
  it('describes the benefit in Persian', () => {
    expect(benefitText({ discountKind: 'Percentage', discountValue: 20, maxDiscountAmount: null })).toBe('۲۰٪ تخفیف')
    expect(benefitText({ discountKind: 'Percentage', discountValue: 20, maxDiscountAmount: 100000 }))
      .toBe('۲۰٪ تخفیف (تا سقف ۱۰۰٬۰۰۰ تومان)')
    expect(benefitText({ discountKind: 'FixedAmount', discountValue: 50000, maxDiscountAmount: null })).toBe('۵۰٬۰۰۰ تومان تخفیف')
  })

  it('shows usage against the limit', () => {
    expect(usageText({ uses: 3, totalUsageLimit: 10 })).toBe('۳ / ۱۰')
    expect(usageText({ uses: 3, totalUsageLimit: null })).toBe('۳')
  })

  it('offers only the lifecycle actions the state allows', () => {
    expect(availableActions({ status: 'Active' })).toEqual(['pause', 'end'])
    expect(availableActions({ status: 'Paused' })).toEqual(['resume', 'end'])
    expect(availableActions({ status: 'Ended' })).toEqual([])
  })

  it('generates readable codes that pass validation', () => {
    let seed = 0
    const code = generateCode(() => ((seed = (seed * 9301 + 49297) % 233280) / 233280))
    expect(code).toMatch(/^AR-[A-HJ-NP-Z2-9]{6}$/)
    expect(validateForm(form({ activation: 'Code', code }), NOW)).toEqual({})
  })
})
