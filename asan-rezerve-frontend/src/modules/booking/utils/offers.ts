import type { PublicOffer } from '../api/promotion.service'

/**
 * Which of a salon's automatic offers to show on a service, and whether its discounted price can be shown as a
 * fact. Mirrors the server's arithmetic (percentage with an optional cap, or a fixed amount; never more than 90% of
 * the price; rounded down to a whole Toman) — but the server's quote at checkout is what is charged.
 */

export const MAX_DISCOUNT_SHARE = 0.9

const PERSIAN_WEEK = [6, 0, 1, 2, 3, 4, 5]
const DAY_NAMES: Record<number, string> = {
  6: 'شنبه', 0: 'یکشنبه', 1: 'دوشنبه', 2: 'سه‌شنبه', 3: 'چهارشنبه', 4: 'پنجشنبه', 5: 'جمعه',
}

export function toPersianDigits(value: string | number): string {
  return String(value).replace(/\d/g, (d) => '۰۱۲۳۴۵۶۷۸۹'[Number(d)] ?? d)
}

export function formatToman(amount: number): string {
  return `${toPersianDigits(Math.round(amount).toLocaleString('en-US'))} تومان`
}

export function discountFor(offer: PublicOffer, price: number): number {
  const raw = offer.discountKind === 'Percentage' ? (price * offer.discountValue) / 100 : offer.discountValue
  const capped = offer.discountKind === 'Percentage' && offer.maxDiscountAmount
    ? Math.min(raw, offer.maxDiscountAmount)
    : raw
  return Math.floor(Math.min(capped, price * MAX_DISCOUNT_SHARE))
}

/** True when the offer applies to this service whatever the day, time or customer — its price is then a fact. */
export function isUnconditional(offer: PublicOffer): boolean {
  return !offer.newCustomersOnly
    && (offer.daysOfWeek.length === 0 || offer.daysOfWeek.length === 7)
    && !(offer.dailyStartTime && offer.dailyEndTime)
}

export interface ServiceOffer {
  offer: PublicOffer
  discount: number
  /** Shown struck-through-and-replaced only when nothing about the day, time or customer can change it. */
  discountedPrice: number | null
  badge: string
  condition: string | null
}

/** The best automatic offer that can apply to a service at [price], or null. */
export function offerForService(serviceId: string, price: number, offers: PublicOffer[]): ServiceOffer | null {
  let best: ServiceOffer | null = null
  for (const offer of offers) {
    if (offer.serviceIds.length > 0 && !offer.serviceIds.includes(serviceId)) continue
    if (offer.minimumSubtotal && price < offer.minimumSubtotal) continue
    const discount = discountFor(offer, price)
    if (discount <= 0) continue
    if (best && best.discount >= discount) continue
    best = {
      offer,
      discount,
      discountedPrice: isUnconditional(offer) ? price - discount : null,
      badge: offerBadge(offer),
      condition: offerCondition(offer),
    }
  }
  return best
}

export function offerBadge(offer: PublicOffer): string {
  return offer.discountKind === 'Percentage'
    ? `${toPersianDigits(offer.discountValue)}٪ تخفیف`
    : `${formatToman(offer.discountValue)} تخفیف`
}

/** The conditions in one line, or null when there are none. */
export function offerCondition(offer: PublicOffer): string | null {
  const parts: string[] = []
  if (offer.newCustomersOnly) parts.push('برای اولین نوبت')
  if (offer.daysOfWeek.length > 0 && offer.daysOfWeek.length < 7)
    parts.push(PERSIAN_WEEK.filter((d) => offer.daysOfWeek.includes(d)).map((d) => DAY_NAMES[d]).join('، '))
  if (offer.dailyStartTime && offer.dailyEndTime)
    parts.push(`ساعت ${toPersianDigits(offer.dailyStartTime)} تا ${toPersianDigits(offer.dailyEndTime)}`)
  if (offer.minimumSubtotal) parts.push(`از ${formatToman(offer.minimumSubtotal)}`)
  if (offer.maxDiscountAmount && offer.discountKind === 'Percentage')
    parts.push(`تا سقف ${formatToman(offer.maxDiscountAmount)}`)
  return parts.length ? parts.join(' · ') : null
}
