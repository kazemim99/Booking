import type { DiscountKind, Promotion, PromotionActivation, PromotionState, PromotionTermsInput } from '../../api/promotions.api'

/**
 * The campaign form's rules and payload, kept out of the template so they are tested without mounting Ant Design.
 * They mirror the server's (the Promotion aggregate is the final word) so the admin hears about a mistake next to
 * the field, not after a round trip.
 */

export const MIN_PERCENT = 1
export const MAX_PERCENT = 90
export const CODE_PATTERN = /^[A-Z0-9-]{4,20}$/
export const TITLE_MAX = 80
export const DESCRIPTION_MAX = 500

/** The Persian week, Saturday first. Values are the server's day numbers (0 = Sunday … 6 = Saturday). */
export const PERSIAN_WEEK = [6, 0, 1, 2, 3, 4, 5] as const

export interface PromotionForm {
  title: string
  description: string
  activation: PromotionActivation
  code: string
  discountKind: DiscountKind
  discountValue: number | null
  maxDiscountAmount: number | null
  minimumSubtotal: number | null
  newCustomersOnly: boolean
  daysOfWeek: number[]
  useDailyWindow: boolean
  dailyStartTime: string
  dailyEndTime: string
  /** ISO instants; null start means "now". */
  startsAt: string | null
  endsAt: string | null
  totalUsageLimit: number | null
  perCustomerLimit: number | null
}

export type FormErrors = Partial<Record<keyof PromotionForm, string>>

export function emptyForm(): PromotionForm {
  return {
    title: '',
    description: '',
    activation: 'Automatic',
    code: '',
    discountKind: 'Percentage',
    discountValue: 10,
    maxDiscountAmount: null,
    minimumSubtotal: null,
    newCustomersOnly: false,
    daysOfWeek: [],
    useDailyWindow: false,
    dailyStartTime: '10:00',
    dailyEndTime: '14:00',
    startsAt: null,
    endsAt: null,
    totalUsageLimit: null,
    perCustomerLimit: null,
  }
}

export function formFromPromotion(p: Promotion): PromotionForm {
  return {
    title: p.title,
    description: p.description ?? '',
    activation: p.activation,
    code: p.code ?? '',
    discountKind: p.discountKind,
    discountValue: p.discountValue,
    maxDiscountAmount: p.maxDiscountAmount ?? null,
    minimumSubtotal: p.minimumSubtotal ?? null,
    newCustomersOnly: p.newCustomersOnly,
    daysOfWeek: [...p.daysOfWeek],
    useDailyWindow: !!(p.dailyStartTime && p.dailyEndTime),
    dailyStartTime: p.dailyStartTime ?? '10:00',
    dailyEndTime: p.dailyEndTime ?? '14:00',
    startsAt: p.startsAt,
    endsAt: p.endsAt ?? null,
    totalUsageLimit: p.totalUsageLimit ?? null,
    perCustomerLimit: p.perCustomerLimit ?? null,
  }
}

export function normalizeCode(code: string): string {
  return code.trim().toUpperCase()
}

/** Field → i18n key of what is wrong with it. Empty when the form can be sent. */
export function validateForm(form: PromotionForm, now: Date = new Date()): FormErrors {
  const errors: FormErrors = {}
  const title = form.title.trim()
  if (!title) errors.title = 'promotions.errors.titleRequired'
  else if (title.length > TITLE_MAX) errors.title = 'promotions.errors.titleTooLong'
  if (form.description.trim().length > DESCRIPTION_MAX) errors.description = 'promotions.errors.descriptionTooLong'

  if (form.activation === 'Code') {
    const code = normalizeCode(form.code)
    if (!code) errors.code = 'promotions.errors.codeRequired'
    else if (!CODE_PATTERN.test(code)) errors.code = 'promotions.errors.codeFormat'
  }

  const value = form.discountValue
  if (form.discountKind === 'Percentage') {
    if (value == null || value < MIN_PERCENT || value > MAX_PERCENT) errors.discountValue = 'promotions.errors.percentRange'
    if (form.maxDiscountAmount != null && form.maxDiscountAmount <= 0) errors.maxDiscountAmount = 'promotions.errors.positive'
  } else if (value == null || value <= 0 || !Number.isInteger(value)) {
    errors.discountValue = 'promotions.errors.amountPositive'
  }

  if (form.minimumSubtotal != null && form.minimumSubtotal < 0) errors.minimumSubtotal = 'promotions.errors.notNegative'

  if (form.useDailyWindow && !(form.dailyStartTime < form.dailyEndTime))
    errors.dailyStartTime = 'promotions.errors.dailyWindow'

  const start = form.startsAt ? new Date(form.startsAt) : now
  if (form.endsAt) {
    const end = new Date(form.endsAt)
    if (end <= start) errors.endsAt = 'promotions.errors.endBeforeStart'
    else if (end <= now) errors.endsAt = 'promotions.errors.endInPast'
  }

  if (form.totalUsageLimit != null && form.totalUsageLimit < 1) errors.totalUsageLimit = 'promotions.errors.atLeastOne'
  if (form.perCustomerLimit != null && form.perCustomerLimit < 1) errors.perCustomerLimit = 'promotions.errors.atLeastOne'
  return errors
}

export function toPayload(form: PromotionForm): PromotionTermsInput {
  const isPercent = form.discountKind === 'Percentage'
  return {
    title: form.title.trim(),
    description: form.description.trim() || null,
    activation: form.activation,
    code: form.activation === 'Code' ? normalizeCode(form.code) : null,
    discountKind: form.discountKind,
    discountValue: form.discountValue ?? 0,
    maxDiscountAmount: isPercent ? form.maxDiscountAmount : null,
    minimumSubtotal: form.minimumSubtotal || null,
    newCustomersOnly: form.newCustomersOnly,
    serviceIds: null,
    daysOfWeek: form.daysOfWeek.length ? [...form.daysOfWeek].sort() : null,
    dailyStartTime: form.useDailyWindow ? form.dailyStartTime : null,
    dailyEndTime: form.useDailyWindow ? form.dailyEndTime : null,
    startsAt: form.startsAt,
    endsAt: form.endsAt,
    totalUsageLimit: form.totalUsageLimit,
    perCustomerLimit: form.perCustomerLimit,
  }
}

const CODE_ALPHABET = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789' // no 0/O, 1/I: codes are read aloud and typed on phones

/** A readable random code, e.g. "AR-7KQ2MX". */
export function generateCode(random: () => number = Math.random, prefix = 'AR'): string {
  let body = ''
  for (let i = 0; i < 6; i++) body += CODE_ALPHABET[Math.floor(random() * CODE_ALPHABET.length)]
  return `${prefix}-${body}`
}

export const STATE_COLORS: Record<PromotionState, string> = {
  Scheduled: 'blue',
  Active: 'green',
  Paused: 'orange',
  Expired: 'default',
  Exhausted: 'purple',
  Ended: 'default',
}

/** Which lifecycle actions make sense in a state. */
export function availableActions(p: Pick<Promotion, 'status'>): Array<'pause' | 'resume' | 'end'> {
  if (p.status === 'Ended') return []
  return p.status === 'Paused' ? ['resume', 'end'] : ['pause', 'end']
}

const toman = new Intl.NumberFormat('fa-IR')

export function formatToman(amount: number): string {
  return `${toman.format(amount)} تومان`
}

/** The benefit in one line, as the customer will read it. */
export function benefitText(p: Pick<Promotion, 'discountKind' | 'discountValue' | 'maxDiscountAmount'>): string {
  if (p.discountKind === 'FixedAmount') return `${formatToman(p.discountValue)} تخفیف`
  const percent = `${toman.format(p.discountValue)}٪ تخفیف`
  return p.maxDiscountAmount ? `${percent} (تا سقف ${formatToman(p.maxDiscountAmount)})` : percent
}

export function usageText(p: Pick<Promotion, 'uses' | 'totalUsageLimit'>): string {
  return p.totalUsageLimit ? `${toman.format(p.uses)} / ${toman.format(p.totalUsageLimit)}` : toman.format(p.uses)
}
