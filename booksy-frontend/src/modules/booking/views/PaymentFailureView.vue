<template>
  <div class="payment-result">
    <div class="payment-result-container">
      <div class="result-icon failure" aria-hidden="true">×</div>
      <h1 class="result-title" data-testid="payment-failure">Payment not completed</h1>
      <p class="result-message">
        Your payment was not completed, so you have not been charged. Your booking is still held for a short while —
        you can try paying again.
      </p>
      <p v-if="errorMessage" class="result-error" data-testid="payment-error">{{ errorMessage }}</p>

      <dl v-if="paymentId" class="result-details">
        <dt>Payment ID</dt>
        <dd>{{ paymentId }}</dd>
      </dl>

      <div class="result-actions">
        <button v-if="bookingId" class="btn btn-primary" data-testid="retry-payment" @click="retry">
          Try again
        </button>
        <router-link to="/my-appointments" class="btn btn-secondary">My appointments</router-link>
      </div>

      <p class="result-note">
        If you believe you were charged, do not pay again — our system reconciles with the bank automatically and your
        booking will update.
      </p>
    </div>
  </div>
</template>

<script setup lang="ts">
/**
 * Payment failure landing page (gap B4).
 *
 * Reached when the backend callback could not verify the payment (customer cancelled, or the gateway reported the
 * payment as not completed). The backend has already settled the payment as Failed before redirecting here, so this
 * page only reports that outcome and offers a retry.
 *
 * "Try again" returns the customer to the booking, from which a new payment attempt can be started — it deliberately
 * does not initiate a charge from this page, and the copy warns against paying twice if the customer suspects they
 * were charged (that case belongs to reconciliation, not to a second charge).
 */
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'

const route = useRoute()
const router = useRouter()

const str = (v: unknown): string | null =>
  typeof v === 'string' && v.length > 0 ? v : Array.isArray(v) && typeof v[0] === 'string' ? v[0] : null

const paymentId = computed(() => str(route.query.paymentId))
const bookingId = computed(() => str(route.query.bookingId))
const errorMessage = computed(() => str(route.query.error))

function retry(): void {
  if (bookingId.value) router.push(`/bookings/${bookingId.value}`)
}
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

.result-icon.failure {
  background-color: #dc2626;
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
  margin-bottom: 1.25rem;
}

.result-note {
  color: #6b7280;
  font-size: 0.8125rem;
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

.btn-secondary {
  background-color: #f3f4f6;
  color: #111827;
  border-color: #e5e7eb;
}
</style>
