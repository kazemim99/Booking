import { describe, it, expect, vi, beforeEach } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
// Static, so the large SFC compiles while the file is collected, not inside the first test's timeout.
// vi.mock calls below are hoisted above it.
import AcceptInvitationView from '../AcceptInvitationView.vue'

/**
 * The invitation screen shows the inviting salon's photo (openspec/changes/_inline/salon-images-load, G5).
 *
 * It never did: the summary's organizationLogo was dropped when the view copied it into its own state, and the
 * <img> bound its src to the salon's NAME. The backend now sends the salon's own photo as an absolute URL.
 */

const getInvitation = vi.fn()

vi.mock('../../../stores/membership.store', () => ({
  useMembershipStore: () => ({ getInvitation: (...args: unknown[]) => getInvitation(...args) }),
}))

vi.mock('@/core/stores/modules/auth.store', () => ({
  useAuthStore: () => ({ isAuthenticated: false, user: null }),
}))

vi.mock('@/core/composables/useNotification', () => ({
  useNotification: () => ({ success: vi.fn(), error: vi.fn() }),
}))

vi.mock('@/modules/auth/api/phoneVerification.api', () => ({ default: {} }))

vi.mock('vue-router', () => ({
  useRoute: () => ({ params: { id: 'inv-1' }, query: { org: 'org-1' } }),
  useRouter: () => ({ push: vi.fn() }),
}))

async function mountView() {
  const wrapper = mount(AcceptInvitationView, {
    global: { stubs: { AppButton: true, OTPInput: true, 'router-link': true } },
  })
  await flushPromises()
  return wrapper
}

const summary = {
  invitationId: 'inv-1',
  organizationId: 'org-1',
  organizationName: 'سالن نهال',
  maskedPhone: '0912***5143',
  status: 'Pending',
  expiresAt: new Date(Date.UTC(2099, 0, 1)),
}

beforeEach(() => {
  vi.clearAllMocks()
})

describe('AcceptInvitationView', () => {
  it("shows the salon's photo, not its name, as the image", async () => {
    const photo = 'https://back.nahalkmi.ir/uploads/providers/p/gallery/a_medium.webp'
    getInvitation.mockResolvedValue({ ...summary, organizationLogo: photo })

    const wrapper = await mountView()

    const img = wrapper.find('.organization-logo img')
    expect(img.exists()).toBe(true)
    expect(img.attributes('src')).toBe(photo)
    expect(img.attributes('alt')).toBe('سالن نهال')
  })

  it('falls back to the icon when the salon has no photo', async () => {
    getInvitation.mockResolvedValue({ ...summary, organizationLogo: null })

    const wrapper = await mountView()

    expect(wrapper.find('.organization-logo img').exists()).toBe(false)
    expect(wrapper.find('.organization-logo-fallback').exists()).toBe(true)
  })
})
