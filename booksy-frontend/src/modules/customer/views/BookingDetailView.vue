<template>
  <div class="booking-detail" dir="rtl">
    <div v-if="booking" class="detail-card">
      <div class="detail-header">
        <h2>جزئیات رزرو</h2>
        <div class="booking-status" :class="booking.status">
          {{ getStatusLabel(booking.status) }}
        </div>
      </div>
      
      <div class="detail-content">
        <div class="detail-section">
          <h3>اطلاعات خدمت</h3>
          <p><strong>خدمت:</strong> {{ booking.serviceName }}</p>
          <p><strong>ارائه‌دهنده:</strong> {{ booking.providerName }}</p>
          <p><strong>آدرس:</strong> {{ booking.address }}</p>
        </div>

        <div class="detail-section">
          <h3>زمان رزرو</h3>
          <p><strong>تاریخ:</strong> {{ formatDate(booking.startTime) }}</p>
          <p><strong>ساعت:</strong> {{ formatTime(booking.startTime) }}</p>
          <p><strong>مدت:</strong> {{ booking.duration }} دقیقه</p>
        </div>

        <div class="detail-section">
          <h3>اطلاعات پرداخت</h3>
          <p><strong>مبلغ کل:</strong> {{ formatPrice(booking.totalAmount) }} تومان</p>
          <p><strong>وضعیت پرداخت:</strong> {{ booking.paymentStatus }}</p>
        </div>
      </div>

      <div class="detail-actions">
        <button v-if="canCancel()" @click="cancelBooking" class="btn-cancel">
          لغو رزرو
        </button>
        <button @click="$router.back()" class="btn-back">بازگشت</button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'

const route = useRoute()
const router = useRouter()

const booking = ref({
  id: route.params.id,
  serviceName: 'کوتاهی مو',
  providerName: 'آرایشگاه زیبا',
  address: 'تهران، ونک',
  startTime: new Date(Date.now() + 86400000),
  duration: 45,
  totalAmount: 150000,
  status: 'confirmed',
  paymentStatus: 'پرداخت شده',
})

function getStatusLabel(status: string) {
  const labels: Record<string, string> = {
    confirmed: 'تایید شده',
    pending: 'در انتظار تایید',
    completed: 'تکمیل شده',
    cancelled: 'لغو شده',
  }
  return labels[status] || status
}

function formatDate(date: Date) {
  return new Intl.DateTimeFormat('fa-IR').format(new Date(date))
}

function formatTime(date: Date) {
  return new Intl.DateTimeFormat('fa-IR', { hour: '2-digit', minute: '2-digit' }).format(new Date(date))
}

function formatPrice(price: number) {
  return new Intl.NumberFormat('fa-IR').format(price)
}

function canCancel() {
  return booking.value.status === 'confirmed'
}

function cancelBooking() {
  if (confirm('آیا از لغو این رزرو مطمئن هستید؟')) {
    router.push('/customer/my-bookings')
  }
}
</script>

<style scoped>
.detail-card {
  background: white;
  padding: 2rem;
  border-radius: 0.75rem;
  max-width: 800px;
}

.detail-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 2rem;
  padding-bottom: 1rem;
  border-bottom: 1px solid var(--color-gray-200);
}

.booking-status {
  padding: 0.5rem 1rem;
  border-radius: 0.5rem;
  font-weight: 500;
}

.booking-status.confirmed {
  background: var(--color-green-100);
  color: var(--color-green-700);
}

.detail-section {
  margin-bottom: 2rem;
  padding: 1.5rem;
  background: var(--color-gray-50);
  border-radius: 0.5rem;
}

.detail-section h3 {
  margin-top: 0;
}

.detail-section p {
  margin: 0.5rem 0;
}

.detail-actions {
  display: flex;
  gap: 1rem;
  justify-content: flex-end;
}

.detail-actions button {
  padding: 0.75rem 1.5rem;
  border: none;
  border-radius: 0.5rem;
  cursor: pointer;
}

.btn-cancel {
  background: var(--color-red-100);
  color: var(--color-red-700);
}

.btn-back {
  background: var(--color-gray-100);
}
</style>
