import { describe, it, expect, vi, beforeEach } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'

/**
 * The confirm step prices the visit through the server (openspec/changes/add-discounts-and-campaigns): subtotal,
 * the one discount applied, the total — and what happened to a code the customer typed. It used to add a 9% tax the
 * server never charged.
 */

const quote = vi.hoisted(() => vi.fn())
vi.mock('@/modules/booking/api/promotion.service', () => ({ promotionService: { quote } }))
vi.mock('@/modules/provider/stores/provider.store', () => ({
  useProviderStore: () => ({ getProviderById: vi.fn(), currentProvider: null }),
}))

const bookingData = {
  serviceId: 's-1',
  serviceName: 'کوتاهی مو',
  servicePrice: 200000,
  serviceDuration: 30,
  date: '2026-10-03',
  startTime: '11:00',
  endTime: '11:30',
  staffId: null,
  staffName: '',
  customerInfo: { firstName: '', lastName: '', phoneNumber: '', email: '', notes: '' },
}
const quoteRequest = { providerId: 'p-1', serviceIds: ['s-1'], startTime: '2026-10-03T11:00:00.000Z' }

const noDiscount = { subtotal: 200000, discount: 0, total: 200000, currency: 'IRT', appliedDiscount: null, codeOutcome: 'None' }
const withAuto = {
  subtotal: 200000, discount: 40000, total: 160000, currency: 'IRT', codeOutcome: 'None',
  appliedDiscount: { promotionId: 'o1', title: 'تخفیف پاییزه', owner: 'Provider', amount: 40000 },
}

async function mountCard() {
  const BookingConfirmation = (await import('../BookingConfirmation.vue')).default
  const wrapper = mount(BookingConfirmation, { props: { bookingData, providerId: 'p-1', quoteRequest } })
  await flushPromises()
  return wrapper
}

const text = (wrapper: Awaited<ReturnType<typeof mountCard>>, id: string) =>
  wrapper.find(`[data-testid="${id}"]`).text()

describe('BookingConfirmation price card', () => {
  beforeEach(() => {
    quote.mockReset()
  })

  it('shows the server total and no invented tax', async () => {
    quote.mockResolvedValue(noDiscount)

    const wrapper = await mountCard()

    expect(quote).toHaveBeenCalledWith({ ...quoteRequest, promotionCode: null })
    expect(text(wrapper, 'price-total')).toContain('۲۰۰,۰۰۰')
    expect(wrapper.text()).not.toContain('مالیات')
    expect(wrapper.find('[data-testid="price-discount"]').exists()).toBe(false)
  })

  it('shows an automatic discount with its name and the saving', async () => {
    quote.mockResolvedValue(withAuto)

    const wrapper = await mountCard()

    expect(text(wrapper, 'price-discount')).toContain('تخفیف پاییزه')
    expect(text(wrapper, 'price-total')).toContain('۱۶۰,۰۰۰')
    expect(text(wrapper, 'price-saving')).toContain('۴۰,۰۰۰')
  })

  it('an applied code is reported and handed to the booking', async () => {
    quote.mockResolvedValueOnce(noDiscount).mockResolvedValueOnce({
      ...withAuto, discount: 60000, total: 140000, codeOutcome: 'Applied', codeMessage: 'کد تخفیف اعمال شد.',
      appliedDiscount: { promotionId: 'c1', title: 'کد وفاداری', code: 'LOYAL', owner: 'Provider', amount: 60000 },
    })
    const wrapper = await mountCard()

    await wrapper.find('[data-testid="promo-toggle"]').trigger('click')
    await wrapper.find('[data-testid="promo-input"]').setValue('loyal')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(quote).toHaveBeenLastCalledWith({ ...quoteRequest, promotionCode: 'loyal' })
    expect(text(wrapper, 'promo-message')).toBe('کد تخفیف اعمال شد.')
    expect(text(wrapper, 'price-total')).toContain('۱۴۰,۰۰۰')
    expect(wrapper.emitted('promotion-code')?.at(-1)).toEqual(['loyal'])
  })

  it('a refused code says why and is not sent with the booking', async () => {
    quote.mockResolvedValueOnce(noDiscount).mockResolvedValueOnce({
      ...noDiscount, codeOutcome: 'NotEligible', codeMessage: 'این تخفیف فقط برای اولین نوبت در این سالن است.',
    })
    const wrapper = await mountCard()

    await wrapper.find('[data-testid="promo-toggle"]').trigger('click')
    await wrapper.find('[data-testid="promo-input"]').setValue('FIRST')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(wrapper.find('[data-testid="promo-message"]').classes()).toContain('promo-message--error')
    expect(text(wrapper, 'promo-message')).toContain('اولین نوبت')
    expect(wrapper.emitted('promotion-code')?.at(-1)).toEqual([null])
  })

  it('a code worse than the automatic offer keeps the offer and says so', async () => {
    quote.mockResolvedValueOnce(withAuto).mockResolvedValueOnce({
      ...withAuto, codeOutcome: 'BetterOfferApplied', codeMessage: 'تخفیف بهتری روی این نوبت اعمال شده است.',
    })
    const wrapper = await mountCard()

    await wrapper.find('[data-testid="promo-toggle"]').trigger('click')
    await wrapper.find('[data-testid="promo-input"]').setValue('SMALL')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(text(wrapper, 'promo-message')).toContain('تخفیف بهتری')
    expect(text(wrapper, 'price-total')).toContain('۱۶۰,۰۰۰')
    expect(wrapper.emitted('promotion-code')?.at(-1)).toEqual([null])
  })

  it('a failed quote falls back to the list price with a note, never a blank card', async () => {
    quote.mockRejectedValue(new Error('offline'))

    const wrapper = await mountCard()

    expect(text(wrapper, 'price-total')).toContain('۲۰۰,۰۰۰')
    expect(wrapper.text()).toContain('قیمت نهایی هنگام ثبت نوبت محاسبه می‌شود')
  })
})
