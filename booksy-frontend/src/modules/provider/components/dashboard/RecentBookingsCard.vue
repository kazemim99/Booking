<template>
  <div class="recent-bookings-card">
    <!-- Loading State -->
    <div v-if="loading" class="loading-state">
      <Skeleton v-for="i in 3" :key="i" height="80px" class="booking-skeleton" />
    </div>

    <!-- Empty State -->
    <div v-else-if="!bookings || bookings.length === 0" class="empty-state">
      <div class="empty-icon">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"
          />
        </svg>
      </div>
      <h3 class="empty-title">{{ $t('dashboard.bookings.noBookings') }}</h3>
      <p class="empty-message">{{ $t('dashboard.bookings.noBookingsMessage') }}</p>
    </div>

    <!-- Bookings List -->
    <div v-else class="bookings-list">
      <div
        v-for="booking in bookings"
        :key="booking.id"
        class="booking-item"
        @click="handleBookingClick(booking)"
      >
        <!-- Status Indicator -->
        <div class="status-indicator" :class="`status-${booking.status.toLowerCase()}`"></div>

        <!-- Booking Info -->
        <div class="booking-content">
          <div class="booking-header">
            <div class="client-info">
              <h4 class="client-name">{{ getClientName(booking) }}</h4>
              <span class="service-name">{{ getServiceName(booking) }}</span>
            </div>
            <div class="booking-status">
              <span :class="statusBadgeClass(booking.status)">
                {{ formatStatus(booking.status) }}
              </span>
            </div>
          </div>

          <div class="booking-details">
            <div class="detail-item">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"
                />
              </svg>
              <span>{{ formatDate(booking.scheduledStartTime) }}</span>
            </div>

            <div class="detail-item">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"
                />
              </svg>
              <span>{{ formatTime(booking.scheduledStartTime) }}</span>
            </div>

            <div class="detail-item">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                />
              </svg>
              <span>{{ formatCurrency(booking.totalPrice) }}</span>
            </div>
          </div>
        </div>

        <!-- Action Arrow -->
        <div class="booking-action">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
          </svg>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { Skeleton } from '@/shared/components'
import type { Appointment, AppointmentStatus } from '@/modules/booking/types/booking.types'

interface Props {
  bookings?: Appointment[]
  loading?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  bookings: () => [],
  loading: false,
})

const emit = defineEmits<{
  (e: 'booking-click', booking: Appointment): void
}>()

const getClientName = (booking: Appointment): string => {
  // TODO: Fetch client name from clientId
  return `Client ${booking.clientId.substring(0, 8)}`
}

const getServiceName = (booking: Appointment): string => {
  // TODO: Fetch service name from serviceId
  return `Service ${booking.serviceId.substring(0, 8)}`
}

const formatStatus = (status: AppointmentStatus): string => {
  return status.replace(/([A-Z])/g, ' $1').trim()
}

const statusBadgeClass = (status: AppointmentStatus): string => {
  const baseClass = 'status-badge'
  const statusMap: Record<string, string> = {
    Pending: 'warning',
    Confirmed: 'info',
    InProgress: 'primary',
    Completed: 'success',
    Cancelled: 'danger',
    NoShow: 'danger',
    Rescheduled: 'warning',
  }
  return `${baseClass} ${baseClass}--${statusMap[status] || 'default'}`
}

const formatDate = (dateString: string): string => {
  const date = new Date(dateString)
  return new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  }).format(date)
}

const formatTime = (dateString: string): string => {
  const date = new Date(dateString)
  return new Intl.DateTimeFormat('en-US', {
    hour: 'numeric',
    minute: '2-digit',
    hour12: true,
  }).format(date)
}

const formatCurrency = (amount: number): string => {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
  }).format(amount)
}

const handleBookingClick = (booking: Appointment) => {
  emit('booking-click', booking)
}
</script>

<style scoped>
.recent-bookings-card {
  min-height: 200px;
}

/* Loading State */
.loading-state {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-md);
}

.booking-skeleton {
  border-radius: var(--radius-lg);
}

/* Empty State */
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: var(--spacing-2xl) var(--spacing-lg);
  text-align: center;
  min-height: 300px;
}

.empty-icon {
  width: 64px;
  height: 64px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: var(--spacing-md);
  border-radius: var(--radius-full);
  background: var(--color-gray-100);
  color: var(--color-gray-400);
}

.empty-icon svg {
  width: 32px;
  height: 32px;
  stroke-width: 2;
}

.empty-title {
  margin: 0 0 var(--spacing-xs) 0;
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
}

.empty-message {
  margin: 0;
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
  max-width: 300px;
}

/* Bookings List */
.bookings-list {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
}

.booking-item {
  position: relative;
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
  padding: var(--spacing-md) var(--spacing-lg);
  background: var(--color-background);
  border: 1px solid var(--color-gray-200);
  border-radius: var(--radius-lg);
  transition: all var(--transition-fast);
  cursor: pointer;
  overflow: hidden;
}

.booking-item:hover {
  border-color: var(--color-primary-300);
  background: var(--color-gray-50);
  box-shadow: var(--shadow-md);
  transform: translateX(4px);
}

.status-indicator {
  position: absolute;
  inset-inline-start: 0;
  top: 0;
  bottom: 0;
  width: 4px;
  border-radius: var(--radius-lg) 0 0 var(--radius-lg);
}

.status-pending {
  background: var(--color-warning-500);
}

.status-confirmed {
  background: var(--color-info-500);
}

.status-inprogress {
  background: var(--color-primary-500);
}

.status-completed {
  background: var(--color-success-500);
}

.status-cancelled,
.status-noshow {
  background: var(--color-danger-500);
}

.status-rescheduled {
  background: var(--color-warning-500);
}

.booking-content {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
  padding-inline-start: var(--spacing-xs);
}

.booking-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: var(--spacing-md);
}

.client-info {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.client-name {
  margin: 0;
  font-size: var(--font-size-base);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.service-name {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.booking-status {
  flex-shrink: 0;
}

.status-badge {
  display: inline-flex;
  align-items: center;
  padding: 2px var(--spacing-xs);
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
  border-radius: var(--radius-sm);
  white-space: nowrap;
}

.status-badge--warning {
  color: var(--color-warning-700);
  background: var(--color-warning-50);
}

.status-badge--info {
  color: var(--color-info-700);
  background: var(--color-info-50);
}

.status-badge--primary {
  color: var(--color-primary-700);
  background: var(--color-primary-50);
}

.status-badge--success {
  color: var(--color-success-700);
  background: var(--color-success-50);
}

.status-badge--danger {
  color: var(--color-danger-700);
  background: var(--color-danger-50);
}

.status-badge--default {
  color: var(--color-gray-700);
  background: var(--color-gray-100);
}

.booking-details {
  display: flex;
  align-items: center;
  gap: var(--spacing-lg);
  flex-wrap: wrap;
}

.detail-item {
  display: flex;
  align-items: center;
  gap: var(--spacing-xs);
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}

.detail-item svg {
  width: 16px;
  height: 16px;
  stroke-width: 2;
  color: var(--color-gray-400);
}

.booking-action {
  flex-shrink: 0;
  width: 24px;
  height: 24px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--color-gray-400);
  transition: all var(--transition-fast);
}

.booking-action svg {
  width: 20px;
  height: 20px;
  stroke-width: 2;
}

.booking-item:hover .booking-action {
  color: var(--color-primary-600);
  transform: translateX(2px);
}

/* Mobile responsive */
@media (max-width: 767px) {
  .booking-item {
    padding: var(--spacing-sm) var(--spacing-md);
  }

  .booking-header {
    flex-direction: column;
    align-items: flex-start;
  }

  .booking-status {
    align-self: flex-start;
  }

  .booking-details {
    flex-direction: column;
    align-items: flex-start;
    gap: var(--spacing-xs);
  }

  .booking-action {
    display: none;
  }

  .booking-item:hover {
    transform: none;
  }
}

/* Tablet */
@media (min-width: 768px) and (max-width: 1023px) {
  .booking-details {
    gap: var(--spacing-md);
  }
}

/* RTL Support - Already handled by logical properties */

/* Reduced motion support */
@media (prefers-reduced-motion: reduce) {
  .booking-item:hover {
    transform: none;
  }

  .booking-item:hover .booking-action {
    transform: none;
  }
}

/* Print styles */
@media print {
  .booking-item {
    border: 1px solid #000;
    box-shadow: none;
    page-break-inside: avoid;
  }

  .booking-action {
    display: none;
  }
}
</style>
