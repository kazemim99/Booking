<template>
  <div class="booking-card">
    <!-- Provider Info -->
    <div class="booking-header">
      <img
        v-if="booking.providerLogoUrl"
        :src="booking.providerLogoUrl"
        :alt="booking.providerName"
        class="provider-logo"
      />
      <div v-else class="provider-logo-placeholder">
        {{ booking.providerName.charAt(0) }}
      </div>

      <div class="provider-info">
        <h3 class="provider-name">{{ booking.providerName }}</h3>
        <p class="service-name">{{ booking.serviceName }}</p>
      </div>
    </div>

    <!-- Booking Details -->
    <div class="booking-details">
      <div class="detail-row">
        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" class="icon">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
        </svg>
        <span>{{ formattedDate }}</span>
      </div>

      <div class="detail-row">
        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" class="icon">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
        <span>{{ formattedTime }}</span>
      </div>

      <div v-if="booking.totalPrice" class="detail-row">
        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" class="icon">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
        <span>{{ formattedPrice }}</span>
      </div>
    </div>

    <!-- Status Badge -->
    <div class="status-badge" :class="`status-${booking.status.toLowerCase()}`">
      {{ statusLabel }}
    </div>

    <!-- Actions -->
    <div class="booking-actions">
      <!-- Upcoming booking actions -->
      <template v-if="type === 'upcoming'">
        <button @click="handleReschedule" class="btn btn-secondary">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7h12m0 0l-4-4m4 4l-4 4m0 6H4m0 0l4 4m-4-4l4-4" />
          </svg>
          <span>تغییر زمان</span>
        </button>
        <button @click="handleCancel" class="btn btn-danger">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
          </svg>
          <span>لغو نوبت</span>
        </button>
      </template>

      <!-- Past booking actions -->
      <template v-if="type === 'past'">
        <button @click="handleRebook" class="btn btn-primary">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
          </svg>
          <span>رزرو مجدد</span>
        </button>
      </template>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import type { UpcomingBooking, BookingHistoryEntry } from '../../types/customer.types'
import { getBookingStatusLabel, formatPrice } from '../../types/customer.types'

interface Props {
  booking: UpcomingBooking | BookingHistoryEntry
  type: 'upcoming' | 'past'
}

const props = defineProps<Props>()

const emit = defineEmits<{
  cancel: [bookingId: string]
  reschedule: [bookingId: string]
  rebook: [providerId: string, serviceName: string]
}>()

const formattedDate = computed(() => {
  // TODO: Implement Jalali date formatting
  const date = new Date(props.booking.startTime)
  return date.toLocaleDateString('fa-IR', {
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  })
})

const formattedTime = computed(() => {
  const date = new Date(props.booking.startTime)
  return date.toLocaleTimeString('fa-IR', {
    hour: '2-digit',
    minute: '2-digit'
  })
})

const formattedPrice = computed(() => {
  if (!props.booking.totalPrice) return ''
  return formatPrice(props.booking.totalPrice)
})

const statusLabel = computed(() => {
  return getBookingStatusLabel(props.booking.status)
})

function handleCancel(): void {
  emit('cancel', props.booking.id)
}

function handleReschedule(): void {
  emit('reschedule', props.booking.id)
}

function handleRebook(): void {
  emit('rebook', props.booking.providerId, props.booking.serviceName)
}
</script>

<style scoped lang="scss">
.booking-card {
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: 12px;
  padding: 1rem;
  display: flex;
  flex-direction: column;
  gap: 1rem;
  transition: box-shadow 0.2s;

  &:hover {
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
  }
}

.booking-header {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.provider-logo {
  width: 48px;
  height: 48px;
  border-radius: 8px;
  object-fit: cover;
}

.provider-logo-placeholder {
  width: 48px;
  height: 48px;
  border-radius: 8px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 600;
  font-size: 1.25rem;
}

.provider-info {
  flex: 1;
  min-width: 0;
}

.provider-name {
  font-size: 1rem;
  font-weight: 600;
  color: #111827;
  margin: 0 0 0.25rem 0;
}

.service-name {
  font-size: 0.875rem;
  color: #6b7280;
  margin: 0;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.booking-details {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.detail-row {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.875rem;
  color: #374151;
}

.icon {
  width: 16px;
  height: 16px;
  color: #9ca3af;
  flex-shrink: 0;
}

.status-badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 0.375rem 0.75rem;
  border-radius: 6px;
  font-size: 0.75rem;
  font-weight: 600;
  width: fit-content;
}

.status-pending {
  background: #fef3c7;
  color: #92400e;
}

.status-confirmed {
  background: #dbeafe;
  color: #1e40af;
}

.status-completed {
  background: #d1fae5;
  color: #065f46;
}

.status-cancelled {
  background: #fee2e2;
  color: #991b1b;
}

.status-noshow {
  background: #f3f4f6;
  color: #374151;
}

.booking-actions {
  display: flex;
  gap: 0.5rem;
  margin-top: 0.5rem;
}

.btn {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 0.625rem 1rem;
  border-radius: 8px;
  font-size: 0.875rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
  border: none;

  svg {
    width: 16px;
    height: 16px;
  }
}

.btn-primary {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;

  &:hover {
    transform: translateY(-1px);
    box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
  }
}

.btn-secondary {
  background: white;
  color: #374151;
  border: 1px solid #d1d5db;

  &:hover {
    background: #f9fafb;
    border-color: #9ca3af;
  }
}

.btn-danger {
  background: white;
  color: #ef4444;
  border: 1px solid #fecaca;

  &:hover {
    background: #fef2f2;
    border-color: #ef4444;
  }
}
</style>
