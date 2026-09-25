import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { defineComponent, h } from 'vue'
import type { CustomerBookingDto } from '@/modules/booking/types/booking-api.types'

/**
 * «چرا نمیتونم بعنوان مشتری کامنت بذارم؟» on the web (openspec/changes/_inline/customer-reviews-and-nahal-seed):
 * My Bookings had cancel and rebook, and no way at all to review a visit.
 */

const getMyBookings = vi.fn()
vi.mock('@/modules/booking/api/booking.service', () => ({
  bookingService: { getMyBookings: (...a: unknown[]) => getMyBookings(...a) },
}))
vi.mock('vue-router', () => ({ useRouter: () => ({ push: vi.fn() }) }))

const WriteReviewStub = defineComponent({
  props: ['isOpen', 'bookingId', 'subject'],
  emits: ['saved', 'close'],
  setup: (props, { emit }) => () =>
    h('button', { 'data-test': 'modal-save', 'data-booking': props.bookingId, onClick: () => emit('saved', 'r1') }, props.subject),
})

import MyBookingsView from '../MyBookingsView.vue'

const visit = (extra: Partial<CustomerBookingDto>): CustomerBookingDto => ({
  bookingId: 'b1',
  customerId: 'c1',
  providerId: 'p1',
  serviceId: 's1',
  staffId: null,
  serviceName: 'کوتاهی مو',
  providerName: 'سالن نهال',
  staffName: null,
  startTime: '2020-01-05T10:00:00',
  endTime: '2020-01-05T10:45:00',
  durationMinutes: 45,
  status: 'Completed',
  totalPrice: 350000,
  currency: 'IRT',
  paymentStatus: 'Paid',
  requestedAt: '2020-01-01T10:00:00',
  confirmedAt: '2020-01-01T10:00:00',
  customerNotes: null,
  ...extra,
})

async function openPast(items: CustomerBookingDto[]) {
  getMyBookings.mockResolvedValue({
    items, pageNumber: 1, pageSize: 20, totalPages: 1, totalCount: items.length,
    hasPreviousPage: false, hasNextPage: false, previousPageNumber: null, nextPageNumber: null, itemRange: '',
  })
  const wrapper = mount(MyBookingsView, { global: { stubs: { WriteReviewModal: WriteReviewStub } } })
  await flushPromises()
  await wrapper.get('[data-testid="bookings-tab-past"]').trigger('click')
  await flushPromises()
  return wrapper
}

describe('My Bookings — reviewing a visit', () => {
  beforeEach(() => getMyBookings.mockReset())

  it('a completed visit offers «ثبت نظر», for that visit, and once saved says it awaits approval', async () => {
    const wrapper = await openPast([visit({ canReview: true })])

    await wrapper.get('[data-testid="booking-review-button"]').trigger('click')
    const modal = wrapper.get('[data-test="modal-save"]')
    expect(modal.attributes('data-booking')).toBe('b1')
    expect(modal.text()).toBe('سالن نهال · کوتاهی مو')

    await modal.trigger('click')
    await flushPromises()

    expect(wrapper.find('[data-testid="booking-review-button"]').exists()).toBe(false)
    expect(wrapper.get('[data-testid="booking-review-state"]').text()).toContain('در انتظار تأیید')
  })

  it('a visit waiting for the salon says why, with nothing to press', async () => {
    const reason = 'پس از اینکه سالن این نوبت را «انجام‌شده» ثبت کند، می‌توانید برایش نظر بنویسید.'
    const wrapper = await openPast([visit({ status: 'Confirmed', canReview: false, reviewBlockedReason: reason })])

    expect(wrapper.find('[data-testid="booking-review-button"]').exists()).toBe(false)
    expect(wrapper.get('[data-testid="booking-review-waiting"]').text()).toContain(reason)
  })

  it('a reviewed visit shows its review\'s state', async () => {
    const wrapper = await openPast([visit({ canReview: false, reviewId: 'r1', reviewStatus: 'Published' })])

    expect(wrapper.get('[data-testid="booking-review-state"]').text()).toContain('منتشر شده')
  })
})
