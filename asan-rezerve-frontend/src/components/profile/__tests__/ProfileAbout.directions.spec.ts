import { describe, it, expect, vi, afterEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { defineComponent, h } from 'vue'
import ProfileAbout from '../ProfileAbout.vue'
import type { Provider } from '@/modules/provider/types/provider.types'

/**
 * reviews-and-reschedule-round2 item 5: «مسیریابی» opened Google Maps only (a TODO said so). With coordinates it now
 * asks which app; without them it still searches the address text.
 */

const ChooserStub = defineComponent({
  props: ['isOpen', 'latitude', 'longitude'],
  setup: (props) => () =>
    h('div', { 'data-test': 'chooser', 'data-open': String(props.isOpen), 'data-at': `${props.latitude},${props.longitude}` }),
})

const salon = (address: Record<string, unknown>) =>
  ({
    id: 'p1',
    profile: { businessName: 'سالن نهال', description: '' },
    tags: [],
    staff: [],
    businessHours: [],
    contactInfo: { email: '', phone: '' },
    address: { addressLine1: 'خیابان ولیعصر', city: 'تهران', state: 'تهران', postalCode: '1234567890', ...address },
  }) as unknown as Provider

const mountAbout = (address: Record<string, unknown>) =>
  mount(ProfileAbout, { props: { provider: salon(address) }, global: { stubs: { DirectionsChooser: ChooserStub } } })

afterEach(() => vi.restoreAllMocks())

describe('ProfileAbout — directions', () => {
  it('with coordinates, asks which app instead of opening Google Maps', async () => {
    const open = vi.spyOn(window, 'open').mockImplementation(() => null)
    const wrapper = mountAbout({ latitude: 35.7, longitude: 51.4 })
    expect(wrapper.get('[data-test="chooser"]').attributes('data-open')).toBe('false')

    await wrapper.get('[data-test="get-directions"]').trigger('click')

    expect(wrapper.get('[data-test="chooser"]').attributes('data-open')).toBe('true')
    expect(wrapper.get('[data-test="chooser"]').attributes('data-at')).toBe('35.7,51.4')
    expect(open).not.toHaveBeenCalled()
  })

  it('without coordinates, searches the address text', async () => {
    const open = vi.spyOn(window, 'open').mockImplementation(() => null)
    const wrapper = mountAbout({})

    await wrapper.get('[data-test="get-directions"]').trigger('click')

    expect(wrapper.find('[data-test="chooser"]').exists()).toBe(false)
    expect(open).toHaveBeenCalledWith(expect.stringContaining('https://www.google.com/maps/search/?api=1&query='), '_blank')
  })
})
