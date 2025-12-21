<template>
  <div class="booking-detail" dir="rtl">
    <!-- Loading State -->
    <div v-if="loading" class="loading-container">
      <div class="spinner"></div>
      <p>در حال بارگذاری جزئیات...</p>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="error-container">
      <div class="error-icon">⚠️</div>
      <p>{{ error }}</p>
      <button @click="loadBooking" class="btn-retry">تلاش مجدد</button>
      <button @click="router.back()" class="btn-back">بازگشت</button>
    </div>

    <!-- Booking Details -->
    <div v-else-if="booking" class="detail-card">
      <div class="detail-header">
        <div>
          <button @click="router.back()" class="btn-back-header">← بازگشت</button>
          <h2>جزئیات رزرو</h2>
        </div>
        <div class="booking-status" :class="booking.statusColor">
          {{ booking.statusLabel }}
        </div>
      </div>

      <div class="detail-content">
        <!-- Basic Information -->
        <div class="detail-section">
          <h3>🎫 اطلاعات رزرو</h3>
          <div class="info-grid">
            <div class="info-item">
              <span class="label">شناسه رزرو:</span>
              <span class="value">#{{ booking.bookingId.substring(0, 8) }}</span>
            </div>
            <div class="info-item">
              <span class="label">وضعیت:</span>
              <span class="value status-badge" :class="booking.statusColor">
                {{ booking.statusLabel }}
              </span>
            </div>
          </div>
        </div>

        <!-- Service Information -->
        <div class="detail-section">
          <h3>🛍️ اطلاعات خدمت</h3>
          <div class="info-grid">
            <div class="info-item">
              <span class="label">نام خدمت:</span>
              <span class="value">{{ booking.serviceName }}</span>
            </div>
            <div class="info-item">
              <span class="label">ارائه‌دهنده:</span>
              <span class="value">{{ booking.providerName }}</span>
            </div>
            <div class="info-item" v-if="booking.staffName">
              <span class="label">کارمند:</span>
              <span class="value">{{ booking.staffName }}</span>
            </div>
            <div class="info-item">
              <span class="label">مدت زمان:</span>
              <span class="value">{{ booking.formattedDuration }}</span>
            </div>
          </div>
        </div>

        <!-- Scheduling Information -->
        <div class="detail-section">
          <h3>📅 زمان‌بندی</h3>
          <div class="info-grid">
            <div class="info-item full-width">
              <span class="label">تاریخ:</span>
              <span class="value">{{ booking.formattedDate }}</span>
            </div>
            <div class="info-item">
              <span class="label">ساعت شروع:</span>
              <span class="value">{{ booking.formattedTime }}</span>
            </div>
            <div class="info-item">
              <span class="label">ساعت پایان:</span>
              <span class="value">{{ formatTime(booking.endTime) }}</span>
            </div>
            <div class="info-item">
              <span class="label">مدت:</span>
              <span class="value">{{ booking.formattedDuration }}</span>
            </div>
          </div>
        </div>

        <!-- Payment Information -->
        <div class="detail-section">
          <h3>💰 اطلاعات پرداخت</h3>
          <div class="info-grid">
            <div class="info-item">
              <span class="label">مبلغ کل:</span>
              <span class="value price-highlight">{{ booking.formattedPrice }}</span>
            </div>
            <div class="info-item">
              <span class="label">وضعیت پرداخت:</span>
              <span class="value">{{ getPaymentStatusLabel(booking.paymentStatus) }}</span>
            </div>
          </div>
        </div>

        <!-- Notes -->
        <div class="detail-section" v-if="booking.customerNotes">
          <h3>📝 یادداشت‌ها</h3>
          <p class="notes-text">{{ booking.customerNotes }}</p>
        </div>

        <!-- Timeline -->
        <div class="detail-section">
          <h3>⏱️ تاریخچه</h3>
          <div class="timeline">
            <div class="timeline-item">
              <span class="timeline-label">درخواست شده:</span>
              <span class="timeline-value">{{ formatDate(booking.requestedAt) }} - {{ formatTime(booking.requestedAt) }}</span>
            </div>
            <div class="timeline-item" v-if="booking.confirmedAt">
              <span class="timeline-label">تایید شده:</span>
              <span class="timeline-value">{{ formatDate(booking.confirmedAt) }} - {{ formatTime(booking.confirmedAt) }}</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Actions -->
      <div class="detail-actions">
        <button
          v-if="canCancel()"
          @click="cancelBooking"
          class="btn-cancel"
          :disabled="cancelling"
        >
          {{ cancelling ? 'در حال لغو...' : 'لغو رزرو' }}
        </button>
        <button @click="router.back()" class="btn-back">بازگشت به لیست</button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { bookingService } from '@/modules/booking/api/booking.service'
import type { CustomerBookingDto } from '@/modules/booking/types/booking-api.types'
import { mapToEnrichedBookingView, type EnrichedBookingView } from '@/modules/booking/mappers/booking-dto.mapper'
import { formatDate } from '@/core/utils'

const route = useRoute()
const router = useRouter()

// State management
const booking = ref<EnrichedBookingView | null>(null)
const loading = ref(false)
const error = ref<string | null>(null)
const cancelling = ref(false)

// Computed properties
const bookingId = computed(() => route.params.id as string)

// Load booking details
async function loadBooking() {
  try {
    loading.value = true
    error.value = null

    const data = await bookingService.getBookingById(bookingId.value)
    // Map to enriched view model with formatted values
    booking.value = mapToEnrichedBookingView(data as any as CustomerBookingDto)

    console.log('[BookingDetailView] Loaded booking:', data)
  } catch (err) {
    console.error('[BookingDetailView] Error loading booking:', err)
    error.value = 'خطا در بارگذاری جزئیات رزرو. لطفا دوباره تلاش کنید.'
  } finally {
    loading.value = false
  }
}

// Payment status label mapping
function getPaymentStatusLabel(status: string) {
  const labels: Record<string, string> = {
    Pending: 'در انتظار پرداخت',
    PartiallyPaid: 'پرداخت جزئی',
    Paid: 'پرداخت شده',
    Refunded: 'مسترد شده',
    Failed: 'ناموفق',
  }
  return labels[status] || status
}

// Date and time formatting
function formatTime(dateString: string) {
  const date = new Date(dateString)
  return new Intl.DateTimeFormat('fa-IR', {
    hour: '2-digit',
    minute: '2-digit'
  }).format(date)
}

function formatPrice(price: number) {
  return new Intl.NumberFormat('fa-IR').format(price)
}

// Action handlers
function canCancel() {
  if (!booking.value) return false
  return booking.value.canCancel
}

async function cancelBooking() {
  if (!booking.value || !confirm('آیا از لغو این رزرو مطمئن هستید؟')) {
    return
  }

  try {
    cancelling.value = true

    await bookingService.cancelBooking(booking.value.bookingId, {
      reason: 'درخواست مشتری',
      notes: 'لغو شده توسط مشتری از صفحه جزئیات'
    })

    alert('رزرو با موفقیت لغو شد')
    router.push('/customer/my-bookings')
  } catch (err) {
    console.error('[BookingDetailView] Error cancelling booking:', err)
    alert('خطا در لغو رزرو. لطفا دوباره تلاش کنید.')
  } finally {
    cancelling.value = false
  }
}

// Load booking on mount
onMounted(() => {
  loadBooking()
})
</script>

<style scoped>
.booking-detail {
  max-width: 900px;
  margin: 0 auto;
  padding: 2rem;
}

/* Loading State */
.loading-container {
  text-align: center;
  padding: 4rem 2rem;
}

.spinner {
  width: 48px;
  height: 48px;
  border: 4px solid var(--color-gray-200, #e5e7eb);
  border-top-color: var(--color-primary, #1976d2);
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
  margin: 0 auto 1rem;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

/* Error State */
.error-container {
  text-align: center;
  padding: 4rem 2rem;
  background: var(--color-red-50, #fef2f2);
  border-radius: 0.75rem;
}

.error-icon {
  font-size: 3rem;
  margin-bottom: 1rem;
}

.error-container p {
  color: var(--color-red-700, #b91c1c);
  margin-bottom: 1.5rem;
  font-size: 1.125rem;
}

.btn-retry {
  padding: 0.75rem 1.5rem;
  background: var(--color-red-600, #dc2626);
  color: white;
  border: none;
  border-radius: 0.5rem;
  cursor: pointer;
  font-weight: 500;
  margin-left: 0.5rem;
}

/* Detail Card */
.detail-card {
  background: white;
  padding: 2rem;
  border-radius: 0.75rem;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.detail-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 2rem;
  padding-bottom: 1rem;
  border-bottom: 2px solid var(--color-gray-200, #e5e7eb);
}

.detail-header > div:first-child {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.btn-back-header {
  background: none;
  border: none;
  color: var(--color-primary, #1976d2);
  cursor: pointer;
  font-size: 0.95rem;
  padding: 0;
  text-align: right;
}

.detail-header h2 {
  margin: 0;
  font-size: 1.75rem;
}

.booking-status {
  padding: 0.5rem 1rem;
  border-radius: 0.5rem;
  font-weight: 600;
  font-size: 0.95rem;
}

.booking-status.success {
  background: var(--color-green-100, #d1fae5);
  color: var(--color-green-700, #047857);
}

.booking-status.warning {
  background: var(--color-yellow-100, #fef3c7);
  color: var(--color-yellow-700, #b45309);
}

.booking-status.info {
  background: var(--color-blue-100, #dbeafe);
  color: var(--color-blue-700, #1d4ed8);
}

.booking-status.error {
  background: var(--color-red-100, #fee2e2);
  color: var(--color-red-700, #b91c1c);
}

.booking-status.default {
  background: var(--color-gray-100, #f3f4f6);
  color: var(--color-gray-700, #374151);
}

/* Detail Sections */
.detail-content {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.detail-section {
  padding: 1.5rem;
  background: var(--color-gray-50, #f9fafb);
  border-radius: 0.75rem;
  border: 1px solid var(--color-gray-200, #e5e7eb);
}

.detail-section h3 {
  margin: 0 0 1rem;
  font-size: 1.125rem;
  color: var(--color-gray-800, #1f2937);
}

/* Info Grid */
.info-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 1rem;
}

.info-item {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.info-item.full-width {
  grid-column: 1 / -1;
}

.label {
  font-size: 0.875rem;
  color: var(--color-gray-600, #4b5563);
  font-weight: 500;
}

.value {
  font-size: 1rem;
  color: var(--color-gray-900, #111827);
  font-weight: 600;
}

.price-highlight {
  color: var(--color-primary, #1976d2);
  font-size: 1.25rem;
}

.status-badge {
  display: inline-block;
  padding: 0.25rem 0.75rem;
  border-radius: 0.375rem;
  font-size: 0.875rem;
}

.status-badge.success {
  background: var(--color-green-100, #d1fae5);
  color: var(--color-green-700, #047857);
}

.status-badge.warning {
  background: var(--color-yellow-100, #fef3c7);
  color: var(--color-yellow-700, #b45309);
}

.status-badge.info {
  background: var(--color-blue-100, #dbeafe);
  color: var(--color-blue-700, #1d4ed8);
}

.status-badge.error {
  background: var(--color-red-100, #fee2e2);
  color: var(--color-red-700, #b91c1c);
}

.status-badge.default {
  background: var(--color-gray-100, #f3f4f6);
  color: var(--color-gray-700, #374151);
}

/* Notes */
.notes-text {
  margin: 0;
  padding: 1rem;
  background: white;
  border-radius: 0.5rem;
  border: 1px solid var(--color-gray-200, #e5e7eb);
  line-height: 1.6;
}

/* Timeline */
.timeline {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.timeline-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0.75rem;
  background: white;
  border-radius: 0.5rem;
  border: 1px solid var(--color-gray-200, #e5e7eb);
}

.timeline-label {
  font-weight: 600;
  color: var(--color-gray-700, #374151);
}

.timeline-value {
  color: var(--color-gray-600, #4b5563);
  font-size: 0.95rem;
}

/* Actions */
.detail-actions {
  display: flex;
  gap: 1rem;
  justify-content: flex-end;
  margin-top: 2rem;
  padding-top: 1.5rem;
  border-top: 2px solid var(--color-gray-200, #e5e7eb);
}

.detail-actions button {
  padding: 0.75rem 1.5rem;
  border: none;
  border-radius: 0.5rem;
  cursor: pointer;
  font-weight: 500;
  transition: all 0.2s;
}

.detail-actions button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn-cancel {
  background: var(--color-red-100, #fee2e2);
  color: var(--color-red-700, #b91c1c);
}

.btn-cancel:hover:not(:disabled) {
  background: var(--color-red-200, #fecaca);
}

.btn-back {
  background: var(--color-gray-100, #f3f4f6);
  color: var(--color-gray-700, #374151);
}

.btn-back:hover:not(:disabled) {
  background: var(--color-gray-200, #e5e7eb);
}

/* Responsive */
@media (max-width: 768px) {
  .booking-detail {
    padding: 1rem;
  }

  .detail-card {
    padding: 1.5rem;
  }

  .info-grid {
    grid-template-columns: 1fr;
  }

  .detail-actions {
    flex-direction: column-reverse;
  }

  .detail-actions button {
    width: 100%;
  }

  .timeline-item {
    flex-direction: column;
    align-items: flex-start;
    gap: 0.5rem;
  }
}
</style>
