<template>
  <div class="payment-result">
    <div class="payment-result-container">
      <!-- Confirming: we do not announce success until the server confirms it -->
      <template v-if="state === 'checking'">
        <div class="spinner" role="status" aria-live="polite" data-testid="payment-checking" />
        <h1 class="result-title">Confirming your payment…</h1>
        <p class="result-message">Please wait while we confirm this with your bank.</p>
      </template>

      <!-- Server-confirmed success -->
      <template v-else-if="state === 'confirmed'">
        <div class="result-icon success" aria-hidden="true">✓</div>
        <h1 class="result-title" data-testid="payment-success">Payment confirmed</h1>
        <p class="result-message">Your booking is confirmed. A receipt is available in your appointments.</p>

        <dl class="result-details">
          <template v-if="refNumber">
            <dt>Reference number</dt>
            <dd data-testid="ref-number">{{ refNumber }}</dd>
          </template>
          <template v-if="paymentId">
            <dt>Payment ID</dt>
            <dd>{{ paymentId }}</dd>
          </template>
        </dl>

        <div class="result-actions">
          <button v-if="bookingId" class="btn btn-primary" @click="goToBooking">View booking</button>
          <router-link to="/my-appointments" class="btn btn-secondary">My appointments</router-link>
        </div>
      </template>

      <!-- Could not confirm. Never claim success from the URL alone. -->
      <template v-else>
        <div class="result-icon pending" aria-hidden="true">!</div>
        <h1 class="result-title" data-testid="payment-unconfirmed">We couldn't confirm your payment yet</h1>
        <p class="result-message">
          If your bank charged you, the payment is safe — our system reconciles automatically and your booking will
          update shortly. Please check your appointments in a few minutes before paying again.
        </p>
        <p v-if="errorMessage" class="result-error" data-testid="payment-error">{{ errorMessage }}</p>

        <div class="result-actions">
          <button class="btn btn-primary" data-testid="check-again" @click="confirm">Check again</button>
          <router-link to="/my-appointments" class="btn btn-secondary">My appointments</router-link>
        </div>
      </template>
    </div>
  </div>
</template>

<script setup lang="ts">
/**
 * Payment success landing page (gap B4).
 *
 * The customer reaches this page through a gateway → backend → browser redirect chain. The backend has already
 * verified the payment against ZarinPal before redirecting, so this page's job is to *display server-owned state*,
 * never to assert payment from the query string. It therefore re-reads the authoritative result from the API
 * (idempotent: re-verifying an already-paid payment returns the stored outcome) and only then shows success.
 *
 * If confirmation cannot be obtained we show a neutral "couldn't confirm yet" state that explicitly discourages
 * paying twice — the reconciler owns the outstanding case, so a duplicate charge is the one outcome to avoid.
 */
import { onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { paymentService } from '@/core/api/services/payment.service'

type ResultState = 'checking' | 'confirmed' | 'unconfirmed'

const route = useRoute()
const router = useRouter()

const state = ref<ResultState>('checking')
const errorMessage = ref<string | null>(null)

// Non-sensitive identifiers echoed by the backend redirect; used for display and for the confirmation read only.
const paymentId = ref<string | null>(null)
const bookingId = ref<string | null>(null)
const refNumber = ref<string | null>(null)
const authority = ref<string | null>(null)

function readQuery(): void {
  const q = route.query
  const str = (v: unknown): string | null =>
    typeof v === 'string' && v.length > 0 ? v : Array.isArray(v) && typeof v[0] === 'string' ? v[0] : null

  paymentId.value = str(q.paymentId)
  bookingId.value = str(q.bookingId)
  refNumber.value = str(q.refNumber)
  // ZarinPal also appends Authority/Status on some configurations; accept either casing.
  authority.value = str(q.Authority) ?? str(q.authority)
}

/**
 * Confirms the payment against the server. Prefers reading the payment record (a pure read); falls back to the
 * idempotent verify endpoint when only an authority is available. Success is asserted only on server confirmation.
 */
async function confirm(): Promise<void> {
  state.value = 'checking'
  errorMessage.value = null

  try {
    if (paymentId.value) {
      const payment = await paymentService.getPaymentById(paymentId.value)
      const paid = ['paid', 'partiallypaid', 'completed'].includes(String(payment?.status ?? '').toLowerCase())
      if (paid) {
        bookingId.value = bookingId.value ?? payment.bookingId ?? null
        state.value = 'confirmed'
        return
      }
      state.value = 'unconfirmed'
      return
    }

    if (authority.value) {
      // Idempotent on the server: an already-verified payment returns its stored result rather than re-charging.
      const result = await paymentService.verifyZarinPalPayment({ authority: authority.value, status: 'OK' })
      if (result?.success) {
        refNumber.value = refNumber.value ?? result.refId ?? null
        state.value = 'confirmed'
        return
      }
      errorMessage.value = result?.message ?? null
      state.value = 'unconfirmed'
      return
    }

    // Nothing to confirm against — never assume success.
    state.value = 'unconfirmed'
  } catch (error: unknown) {
    errorMessage.value = error instanceof Error ? error.message : 'Unable to reach the server.'
    state.value = 'unconfirmed'
  }
}

function goToBooking(): void {
  if (bookingId.value) router.push(`/bookings/${bookingId.value}`)
}

onMounted(() => {
  readQuery()
  void confirm()
})
</script>

<style scoped>
.payment-result {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 60vh;
  padding: 2rem 1rem;
}

.payment-result-container {
  max-width: 32rem;
  width: 100%;
  text-align: center;
}

.result-icon {
  width: 4rem;
  height: 4rem;
  line-height: 4rem;
  margin: 0 auto 1.25rem;
  border-radius: 50%;
  font-size: 2rem;
  font-weight: 700;
  color: #fff;
}

.result-icon.success {
  background-color: #16a34a;
}

.result-icon.pending {
  background-color: #d97706;
}

.spinner {
  width: 2.5rem;
  height: 2.5rem;
  margin: 0 auto 1.25rem;
  border: 3px solid #e5e7eb;
  border-top-color: #3777bf;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

@media (prefers-reduced-motion: reduce) {
  .spinner {
    animation-duration: 2s;
  }
}

.result-title {
  font-size: 1.5rem;
  font-weight: 700;
  margin-bottom: 0.5rem;
}

.result-message {
  color: #4b5563;
  margin-bottom: 1.5rem;
}

.result-error {
  color: #b91c1c;
  font-size: 0.875rem;
  margin-bottom: 1.5rem;
}

.result-details {
  display: grid;
  grid-template-columns: auto 1fr;
  gap: 0.375rem 1rem;
  text-align: left;
  margin: 0 auto 1.5rem;
  max-width: 24rem;
  font-size: 0.9375rem;
}

.result-details dt {
  color: #6b7280;
}

.result-details dd {
  margin: 0;
  font-family: ui-monospace, monospace;
  overflow-wrap: anywhere;
}

.result-actions {
  display: flex;
  gap: 0.75rem;
  justify-content: center;
  flex-wrap: wrap;
}

.btn {
  padding: 0.625rem 1.25rem;
  border-radius: 0.5rem;
  border: 1px solid transparent;
  cursor: pointer;
  font: inherit;
  text-decoration: none;
}

.btn-primary {
  background-color: #3777bf;
  color: #fff;
}

.btn-primary:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn-secondary {
  background-color: #f3f4f6;
  color: #111827;
  border-color: #e5e7eb;
}
</style>
