import { describe, it, expect } from 'vitest'
import type { PublicOffer } from '../../api/promotion.service'
import { discountFor, offerCondition, offerForService, formatToman } from '../offers'

const offer = (overrides: Partial<PublicOffer> = {}): PublicOffer => ({
  id: 'o1',
  title: 'تخفیف پاییزه',
  owner: 'Provider',
  discountKind: 'Percentage',
  discountValue: 20,
  newCustomersOnly: false,
  serviceIds: [],
  daysOfWeek: [],
  ...overrides,
})

describe('discountFor mirrors the server arithmetic', () => {
  it('takes a percentage, capped', () => {
    expect(discountFor(offer(), 200_000)).toBe(40_000)
    expect(discountFor(offer({ discountValue: 30, maxDiscountAmount: 100_000 }), 1_000_000)).toBe(100_000)
  })

  it('never takes more than 90% and rounds down to a whole Toman', () => {
    expect(discountFor(offer({ discountKind: 'FixedAmount', discountValue: 500_000 }), 300_000)).toBe(270_000)
    expect(discountFor(offer({ discountValue: 15 }), 33_333)).toBe(4_999)
  })
})

describe('offerForService', () => {
  it('shows a fact price for an offer with no conditions', () => {
    const result = offerForService('s1', 200_000, [offer()])

    expect(result?.badge).toBe('۲۰٪ تخفیف')
    expect(result?.discountedPrice).toBe(160_000)
    expect(result?.condition).toBeNull()
  })

  it('does not promise a price that depends on the day, time or customer', () => {
    const result = offerForService('s1', 200_000, [offer({ daysOfWeek: [6, 0], dailyStartTime: '10:00', dailyEndTime: '13:00' })])

    expect(result?.discountedPrice).toBeNull()
    expect(result?.condition).toBe('شنبه، یکشنبه · ساعت ۱۰:۰۰ تا ۱۳:۰۰')
  })

  it('skips offers for other services or below their minimum, and picks the largest', () => {
    const offers = [
      offer({ id: 'other', discountValue: 50, serviceIds: ['s2'] }),
      offer({ id: 'min', discountValue: 40, minimumSubtotal: 500_000 }),
      offer({ id: 'small', discountValue: 10 }),
      offer({ id: 'big', discountValue: 25 }),
    ]

    expect(offerForService('s1', 200_000, offers)?.offer.id).toBe('big')
  })

  it('returns null when nothing applies', () => {
    expect(offerForService('s1', 200_000, [])).toBeNull()
    expect(offerForService('s1', 200_000, [offer({ serviceIds: ['s2'] })])).toBeNull()
  })
})

describe('wording', () => {
  it('formats Toman with Persian digits', () => {
    expect(formatToman(1_250_000)).toBe('۱,۲۵۰,۰۰۰ تومان')
  })

  it('names new-customer and minimum conditions', () => {
    expect(offerCondition(offer({ newCustomersOnly: true, minimumSubtotal: 300_000 })))
      .toBe('برای اولین نوبت · از ۳۰۰,۰۰۰ تومان')
  })
})
