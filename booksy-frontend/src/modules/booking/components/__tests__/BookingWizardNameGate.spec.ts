import { describe, it, expect, vi, beforeEach } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { defineComponent, h, reactive } from 'vue'

/**
 * A salon has to know who it is confirming (QA 2026-09-24), so a booking needs the customer's real first AND last
 * name. Signing up stays phone-only; the name is asked for when they confirm a booking, and only if they have none.
 * The customer app (Flutter) already did this; the web wizard booked with the sign-up placeholder.
 */

const createBooking = vi.fn()
vi.mock('@/modules/booking/api/booking.service', () => ({
  bookingService: { createBooking: (...args: unknown[]) => createBooking(...args) },
}))

const auth = reactive({
  user: null as null | { id: string; firstName: string; lastName: string; phoneNumber: string },
  customerId: 'c-1',
})
vi.mock('@/core/stores/modules/auth.store', () => ({ useAuthStore: () => auth }))
vi.mock('@/modules/provider/stores/provider.store', () => ({
  useProviderStore: () => ({ getProviderById: vi.fn(), currentProvider: null }),
}))
vi.mock('vue-router', () => ({
  useRoute: () => ({ params: { providerId: 'p-1' }, query: {} }),
  useRouter: () => ({ push: vi.fn() }),
}))

// The profile form the wizard reuses to take the name; saving it updates the signed-in user and closes.
const ProfileEditModalStub = defineComponent({
  name: 'ProfileEditModal',
  props: { isOpen: Boolean },
  emits: ['close'],
  setup: (props) => () => (props.isOpen ? h('div', { 'data-testid': 'name-form' }) : null),
})

async function wizardAtConfirm() {
  const BookingWizard = (await import('../BookingWizard.vue')).default
  const wrapper = mount(BookingWizard, {
    global: {
      stubs: {
        ServiceSelection: true,
        SlotSelection: true,
        BookingConfirmation: true,
        ProfileEditModal: ProfileEditModalStub,
        'router-link': true,
      },
    },
  })
  const vm = wrapper.vm as unknown as {
    currentStep: number
    bookingData: { services: unknown[]; date: string; startTime: string; endTime: string }
  }
  vm.bookingData.services = [{ id: 's-1', name: 'اصلاح سر', basePrice: 100000, duration: 30 }]
  vm.bookingData.date = '2026-09-26'
  vm.bookingData.startTime = '10:00'
  vm.bookingData.endTime = '10:30'
  vm.currentStep = 3
  await flushPromises()
  return wrapper
}

beforeEach(() => {
  vi.clearAllMocks()
  createBooking.mockResolvedValue({ id: 'b-1' })
})

describe('BookingWizard — a name before the booking', () => {
  it('a customer with only the sign-up placeholder is asked for their name, and nothing is booked yet', async () => {
    auth.user = { id: 'u-1', firstName: 'مشتری', lastName: '9384444636', phoneNumber: '09384444636' }
    const wrapper = await wizardAtConfirm()

    await wrapper.get('[data-testid="booking-confirm"]').trigger('click')
    await flushPromises()

    expect(wrapper.find('[data-testid="name-form"]').exists()).toBe(true)
    expect(createBooking).not.toHaveBeenCalled()
  })

  it('once they have given their name, the booking goes through', async () => {
    auth.user = { id: 'u-1', firstName: 'مشتری', lastName: '9384444636', phoneNumber: '09384444636' }
    const wrapper = await wizardAtConfirm()
    await wrapper.get('[data-testid="booking-confirm"]').trigger('click')
    await flushPromises()

    auth.user = { ...auth.user!, firstName: 'مصطفی', lastName: 'کاظمی' }
    wrapper.findComponent(ProfileEditModalStub).vm.$emit('close')
    await flushPromises()

    expect(createBooking).toHaveBeenCalledTimes(1)
  })

  it('closing the form without a name books nothing', async () => {
    auth.user = { id: 'u-1', firstName: 'مشتری', lastName: '9384444636', phoneNumber: '09384444636' }
    const wrapper = await wizardAtConfirm()
    await wrapper.get('[data-testid="booking-confirm"]').trigger('click')
    await flushPromises()

    wrapper.findComponent(ProfileEditModalStub).vm.$emit('close')
    await flushPromises()

    expect(createBooking).not.toHaveBeenCalled()
  })

  it('a customer who already has a real name books straight away', async () => {
    auth.user = { id: 'u-1', firstName: 'مصطفی', lastName: 'کاظمی', phoneNumber: '09384444636' }
    const wrapper = await wizardAtConfirm()

    await wrapper.get('[data-testid="booking-confirm"]').trigger('click')
    await flushPromises()

    expect(wrapper.find('[data-testid="name-form"]').exists()).toBe(false)
    expect(createBooking).toHaveBeenCalledTimes(1)
  })
})
