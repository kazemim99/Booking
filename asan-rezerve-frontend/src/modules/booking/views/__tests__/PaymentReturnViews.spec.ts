import { describe, it, expect, vi, beforeEach } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'

/**
 * B4 — payment return landing pages.
 *
 * The security-critical property under test: these pages are reached via a gateway → backend → browser redirect, so
 * they must **never** treat the return URL as proof of payment. Success is announced only when the server confirms
 * it; when the server cannot confirm, the page must show a neutral "couldn't confirm" state that discourages paying
 * twice (an outstanding charge belongs to reconciliation, never to a second charge).
 */

// ---- mocks --------------------------------------------------------------------------------------

const getPaymentById = vi.fn()
const verifyZarinPalPayment = vi.fn()

vi.mock('@/core/api/services/payment.service', () => ({
  paymentService: {
    getPaymentById: (...args: unknown[]) => getPaymentById(...args),
    verifyZarinPalPayment: (...args: unknown[]) => verifyZarinPalPayment(...args),
  },
}))

const push = vi.fn()
let mockQuery: Record<string, string> = {}

vi.mock('vue-router', () => ({
  useRoute: () => ({ query: mockQuery }),
  useRouter: () => ({ push }),
}))

const routerLinkStub = { template: '<a><slot /></a>' }

async function mountSuccess() {
  const PaymentSuccessView = (await import('../PaymentSuccessView.vue')).default
  const wrapper = mount(PaymentSuccessView, { global: { stubs: { 'router-link': routerLinkStub } } })
  await flushPromises()
  return wrapper
}

async function mountFailure() {
  const PaymentFailureView = (await import('../PaymentFailureView.vue')).default
  return mount(PaymentFailureView, { global: { stubs: { 'router-link': routerLinkStub } } })
}

beforeEach(() => {
  vi.clearAllMocks()
  mockQuery = {}
})

// ---- success page -------------------------------------------------------------------------------

describe('PaymentSuccessView', () => {
  it('announces success only after the server confirms the payment is paid', async () => {
    mockQuery = { paymentId: 'pay-1', bookingId: 'book-1', refNumber: '12345' }
    getPaymentById.mockResolvedValue({ id: 'pay-1', bookingId: 'book-1', status: 'Paid' })

    const wrapper = await mountSuccess()

    expect(getPaymentById).toHaveBeenCalledWith('pay-1')
    expect(wrapper.find('[data-testid="payment-success"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="ref-number"]').text()).toBe('12345')
  })

  it('does NOT claim success when the server says the payment is not paid, even though the URL is /payment/success', async () => {
    mockQuery = { paymentId: 'pay-1', bookingId: 'book-1' }
    getPaymentById.mockResolvedValue({ id: 'pay-1', bookingId: 'book-1', status: 'Pending' })

    const wrapper = await mountSuccess()

    expect(wrapper.find('[data-testid="payment-success"]').exists()).toBe(false)
    expect(wrapper.find('[data-testid="payment-unconfirmed"]').exists()).toBe(true)
  })

  it('does NOT claim success when the confirmation request fails', async () => {
    mockQuery = { paymentId: 'pay-1' }
    getPaymentById.mockRejectedValue(new Error('network down'))

    const wrapper = await mountSuccess()

    expect(wrapper.find('[data-testid="payment-success"]').exists()).toBe(false)
    expect(wrapper.find('[data-testid="payment-unconfirmed"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="payment-error"]').text()).toContain('network down')
  })

  it('does NOT claim success when the URL carries no identifiers to confirm against', async () => {
    mockQuery = {}

    const wrapper = await mountSuccess()

    expect(getPaymentById).not.toHaveBeenCalled()
    expect(verifyZarinPalPayment).not.toHaveBeenCalled()
    expect(wrapper.find('[data-testid="payment-unconfirmed"]').exists()).toBe(true)
  })

  it('falls back to the idempotent verify endpoint when only an authority is present', async () => {
    mockQuery = { Authority: 'auth-xyz' }
    verifyZarinPalPayment.mockResolvedValue({ success: true, refId: '99887', message: 'ok' })

    const wrapper = await mountSuccess()

    expect(verifyZarinPalPayment).toHaveBeenCalledWith({ authority: 'auth-xyz', status: 'OK' })
    expect(wrapper.find('[data-testid="payment-success"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="ref-number"]').text()).toBe('99887')
  })

  it('surfaces the gateway message and stays unconfirmed when verify reports failure', async () => {
    mockQuery = { authority: 'auth-xyz' }
    verifyZarinPalPayment.mockResolvedValue({ success: false, message: 'Not paid' })

    const wrapper = await mountSuccess()

    expect(wrapper.find('[data-testid="payment-unconfirmed"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="payment-error"]').text()).toContain('Not paid')
  })

  it('can re-check on demand without initiating a payment', async () => {
    mockQuery = { paymentId: 'pay-1' }
    getPaymentById.mockResolvedValueOnce({ id: 'pay-1', status: 'Pending' })

    const wrapper = await mountSuccess()
    expect(wrapper.find('[data-testid="payment-unconfirmed"]').exists()).toBe(true)

    // Reconciliation has since settled it — re-checking is a pure read, never a new charge.
    getPaymentById.mockResolvedValueOnce({ id: 'pay-1', bookingId: 'book-9', status: 'Paid' })
    await wrapper.find('[data-testid="check-again"]').trigger('click')
    await flushPromises()

    expect(wrapper.find('[data-testid="payment-success"]').exists()).toBe(true)
    expect(verifyZarinPalPayment).not.toHaveBeenCalled()
    expect(getPaymentById).toHaveBeenCalledTimes(2)
  })
})

// ---- failure page ------------------------------------------------------------------------------

describe('PaymentFailureView', () => {
  it('reports the failure and shows the gateway error', async () => {
    mockQuery = { paymentId: 'pay-1', bookingId: 'book-1', error: 'User cancelled the payment' }

    const wrapper = await mountFailure()

    expect(wrapper.find('[data-testid="payment-failure"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="payment-error"]').text()).toContain('User cancelled the payment')
  })

  it('retry returns to the booking instead of charging from the failure page', async () => {
    mockQuery = { bookingId: 'book-1' }

    const wrapper = await mountFailure()
    await wrapper.find('[data-testid="retry-payment"]').trigger('click')

    expect(push).toHaveBeenCalledWith('/bookings/book-1')
    // No payment call may originate from this page.
    expect(verifyZarinPalPayment).not.toHaveBeenCalled()
    expect(getPaymentById).not.toHaveBeenCalled()
  })

  it('hides retry when there is no booking to return to', async () => {
    mockQuery = { paymentId: 'pay-1' }

    const wrapper = await mountFailure()

    expect(wrapper.find('[data-testid="retry-payment"]').exists()).toBe(false)
  })
})
