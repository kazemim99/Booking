import { describe, expect, it, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import ProfileStaff from '../ProfileStaff.vue'
import type { Provider } from '@/modules/provider/types/provider.types'

vi.mock('vue-router', () => ({ useRouter: () => ({ push: vi.fn() }) }))

// The salon page's team. Production QA 2026-09-23: a salon's owner who signed up with only a phone was named
// «ارائه‌دهنده 9123135143» wherever a customer saw them. The API now names such a member by the salon (fullName)
// and sends their first/last blank; this page read only first/last, so it must read fullName first.
const salon = (staff: Array<Record<string, unknown>>) =>
  ({ id: 'p1', allowOnlineBooking: false, staff }) as unknown as Provider

const member = (fields: Record<string, unknown>) => ({
  id: 'm1',
  providerId: 'p1',
  firstName: '',
  lastName: '',
  email: '',
  phone: '',
  isActive: true,
  specializations: [],
  ...fields,
})

describe('ProfileStaff', () => {
  it('names a member by the name the API gives them', () => {
    const wrapper = mount(ProfileStaff, {
      props: { provider: salon([member({ fullName: 'سالن نهال' })]) },
    })
    expect(wrapper.find('.staff-name').text()).toBe('سالن نهال')
  })

  it('still names a member from first and last when that is all there is', () => {
    const wrapper = mount(ProfileStaff, {
      props: { provider: salon([member({ firstName: 'مریم', lastName: 'احمدی' })]) },
    })
    expect(wrapper.find('.staff-name').text()).toBe('مریم احمدی')
  })

  it('never shows the sign-in placeholder or a phone number as a name', () => {
    const wrapper = mount(ProfileStaff, {
      props: {
        provider: salon([
          member({ firstName: 'ارائه‌دهنده', lastName: '9123135143', fullName: 'ارائه‌دهنده 9123135143' }),
        ]),
      },
    })
    expect(wrapper.text()).not.toContain('9123135143')
    expect(wrapper.find('.staff-name').text()).toBe('بدون نام')
  })
})
