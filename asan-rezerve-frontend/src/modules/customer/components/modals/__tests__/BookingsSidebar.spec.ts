import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { defineComponent, h } from 'vue'
import type { CustomerBookingDto } from '@/modules/booking/types/booking-api.types'

/**
 * The bookings sidebar (reviews-and-reschedule-round2): a moved booking waits for the salon's confirmation and the
 * customer is told so after the move; a salon review still inside its edit window is offered as «ویرایش نظر».
 */

const getUpcomingBookings = vi.fn()
const getPastBookings = vi.fn()
const rescheduleBooking = vi.fn()
vi.mock('@/modules/booking/api/booking.service', () => ({
  bookingService: {
    getUpcomingBookings: (...a: unknown[]) => getUpcomingBookings(...a),
    getPastBookings: (...a: unknown[]) => getPastBookings(...a),
    rescheduleBooking: (...a: unknown[]) => rescheduleBooking(...a),
  },
}))
const showSuccess = vi.fn()
vi.mock('@/core/composables/useNotification', () => ({ useNotification: () => ({ showSuccess, showError: vi.fn() }) }))
vi.mock('@/modules/booking/composables/useNameBeforeBooking', () => ({
  useNameBeforeBooking: () => ({ nameFormOpen: false, requireName: (f: () => void) => f(), onNameFormClosed: vi.fn() }),
}))

const RescheduleStub = defineComponent({
  props: ['isOpen', 'booking', 'mode'],
  emits: ['confirm', 'close'],
  setup: (props, { emit }) => () =>
    h('button', { 'data-test': `picker-${props.mode ?? 'reschedule'}`, onClick: () => emit('confirm', '2099-01-06T10:00:00') }),
})
const WriteReviewStub = defineComponent({
  props: ['isOpen', 'bookingId', 'reviewId', 'subject'],
  setup: (props) => () => h('div', { 'data-test': 'review-modal', 'data-review': props.reviewId ?? '' }),
})

import BookingsSidebar from '../BookingsSidebar.vue'

const booking = (extra: Partial<CustomerBookingDto>): CustomerBookingDto => ({
  bookingId: 'b1',
  customerId: 'c1',
  providerId: 'p1',
  serviceId: 's1',
  staffId: null,
  serviceName: 'کوتاهی مو',
  providerName: 'سالن نهال',
  staffName: null,
  startTime: '2099-01-05T10:00:00',
  endTime: '2099-01-05T10:45:00',
  durationMinutes: 45,
  status: 'Confirmed',
  totalPrice: 350000,
  currency: 'IRT',
  paymentStatus: 'Paid',
  requestedAt: '2098-12-01T10:00:00',
  confirmedAt: '2098-12-01T10:00:00',
  customerNotes: null,
  ...extra,
})

const mountSidebar = async () => {
  const wrapper = mount(BookingsSidebar, {
    props: { isOpen: true },
    global: {
      stubs: {
        RescheduleBookingModal: RescheduleStub,
        WriteReviewModal: WriteReviewStub,
        CancelBookingModal: true,
        ProfileEditModal: true,
        transition: false,
      },
    },
  })
  await flushPromises()
  return wrapper
}

describe('BookingsSidebar', () => {
  beforeEach(() => {
    getUpcomingBookings.mockReset()
    getPastBookings.mockReset()
    rescheduleBooking.mockReset()
    showSuccess.mockReset()
  })

  it('after a move, says the booking now waits for the salon to confirm it', async () => {
    getUpcomingBookings.mockResolvedValue([booking({})])
    getPastBookings.mockResolvedValue([])
    rescheduleBooking.mockResolvedValue({})
    const wrapper = await mountSidebar()

    await wrapper.get('[data-testid="booking-reschedule-button"]').trigger('click')
    await wrapper.get('[data-test="picker-reschedule"]').trigger('click')
    await flushPromises()

    expect(rescheduleBooking).toHaveBeenCalledWith(expect.objectContaining({ appointmentId: 'b1' }))
    expect(showSuccess).toHaveBeenCalledWith(expect.any(String), expect.stringContaining('در انتظار تأیید سالن'))
  })

  it('a past visit whose salon review may still be edited offers «ویرایش نظر» on that review', async () => {
    getUpcomingBookings.mockResolvedValue([])
    getPastBookings.mockResolvedValue([
      booking({
        startTime: '2020-01-05T10:00:00',
        endTime: '2020-01-05T10:45:00',
        status: 'Completed',
        canReview: false,
        reviewId: 'r3',
        reviewStatus: 'Published',
        reviewEditable: true,
      }),
    ])
    const wrapper = await mountSidebar()
    await wrapper.findAll('.tab')[1].trigger('click')
    await flushPromises()

    expect(wrapper.find('[data-testid="sidebar-review-button"]').exists()).toBe(false)
    await wrapper.get('[data-testid="sidebar-review-edit-button"]').trigger('click')

    expect(wrapper.get('[data-test="review-modal"]').attributes('data-review')).toBe('r3')
  })
})
