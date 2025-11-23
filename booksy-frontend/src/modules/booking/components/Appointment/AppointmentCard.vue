<template>
  <div class="appointment-card" :class="`status-${status.toLowerCase()}`">
    <!-- Header with Status Badge -->
    <div class="card-header">
      <div class="status-badge" :class="`badge-${status.toLowerCase()}`">
        {{ getStatusLabel(status) }}
      </div>
      <div class="appointment-date">
        {{ formattedDate }}
      </div>
    </div>

    <!-- Main Content -->
    <div class="card-body">
      <!-- Service Information -->
      <div class="info-section">
        <div class="section-icon service-icon">
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
            <path d="M11.7 2.805a.75.75 0 01.6 0A60.65 60.65 0 0122.83 8.72a.75.75 0 01-.231 1.337 49.949 49.949 0 00-9.902 3.912l-.003.002-.34.18a.75.75 0 01-.707 0A50.009 50.009 0 007.5 12.174v-.224c0-.131.067-.248.172-.311a54.614 54.614 0 014.653-2.52.75.75 0 00-.65-1.352 56.129 56.129 0 00-4.78 2.589 1.858 1.858 0 00-.859 1.228 49.803 49.803 0 00-4.634-1.527.75.75 0 01-.231-1.337A60.653 60.653 0 0111.7 2.805z" />
            <path d="M13.06 15.473a48.45 48.45 0 017.666-3.282c.134 1.414.22 2.843.255 4.285a.75.75 0 01-.46.71 47.878 47.878 0 00-8.105 4.342.75.75 0 01-.832 0 47.877 47.877 0 00-8.104-4.342.75.75 0 01-.461-.71c.035-1.442.121-2.87.255-4.286A48.4 48.4 0 016 13.18v1.27a1.5 1.5 0 00-.14 2.508c-.09.38-.222.753-.397 1.11.452.213.901.434 1.346.661a6.729 6.729 0 00.551-1.608 1.5 1.5 0 00.14-2.67v-.645a48.549 48.549 0 013.44 1.668 2.25 2.25 0 002.12 0z" />
          </svg>
        </div>
        <div class="section-content">
          <div class="service-name">{{ serviceName }}</div>
          <div class="service-details">
            <span class="detail-item">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm.75-13a.75.75 0 00-1.5 0v5c0 .414.336.75.75.75h4a.75.75 0 000-1.5h-3.25V5z" clip-rule="evenodd" />
              </svg>
              {{ formattedTime }}
            </span>
            <span class="detail-item">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                <path d="M10 2a.75.75 0 01.75.75v1.5a.75.75 0 01-1.5 0v-1.5A.75.75 0 0110 2zM10 15a.75.75 0 01.75.75v1.5a.75.75 0 01-1.5 0v-1.5A.75.75 0 0110 15zM10 7a3 3 0 100 6 3 3 0 000-6zM15.657 5.404a.75.75 0 10-1.06-1.06l-1.061 1.06a.75.75 0 001.06 1.06l1.06-1.06zM6.464 14.596a.75.75 0 10-1.06-1.06l-1.06 1.06a.75.75 0 001.06 1.06l1.06-1.06zM18 10a.75.75 0 01-.75.75h-1.5a.75.75 0 010-1.5h1.5A.75.75 0 0118 10zM5 10a.75.75 0 01-.75.75h-1.5a.75.75 0 010-1.5h1.5A.75.75 0 015 10zM14.596 15.657a.75.75 0 001.06-1.06l-1.06-1.061a.75.75 0 10-1.061 1.06l1.06 1.06zM5.404 6.464a.75.75 0 001.06-1.06l-1.06-1.06a.75.75 0 10-1.061 1.06l1.06 1.06z" />
              </svg>
              {{ convertToPersian(duration) }} دقیقه
            </span>
          </div>
        </div>
      </div>

      <!-- Staff Information (if available) -->
      <div v-if="staffName" class="info-section staff-section">
        <div class="section-icon staff-icon">
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
            <path fill-rule="evenodd" d="M7.5 6a4.5 4.5 0 119 0 4.5 4.5 0 01-9 0zM3.751 20.105a8.25 8.25 0 0116.498 0 .75.75 0 01-.437.695A18.683 18.683 0 0112 22.5c-2.786 0-5.433-.608-7.812-1.7a.75.75 0 01-.437-.695z" clip-rule="evenodd" />
          </svg>
        </div>
        <div class="section-content">
          <div class="staff-label">کارمند ارائه‌دهنده</div>
          <div class="staff-name">{{ staffName }}</div>
        </div>
      </div>

      <!-- Provider Information -->
      <div class="info-section">
        <div class="section-icon provider-icon">
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
            <path fill-rule="evenodd" d="M4.5 2.25a.75.75 0 000 1.5v16.5h-.75a.75.75 0 000 1.5h16.5a.75.75 0 000-1.5h-.75V3.75a.75.75 0 000-1.5h-15zM9 6a.75.75 0 000 1.5h1.5a.75.75 0 000-1.5H9zm-.75 3.75A.75.75 0 019 9h1.5a.75.75 0 010 1.5H9a.75.75 0 01-.75-.75zM9 12a.75.75 0 000 1.5h1.5a.75.75 0 000-1.5H9zm3.75-5.25A.75.75 0 0113.5 6H15a.75.75 0 010 1.5h-1.5a.75.75 0 01-.75-.75zM13.5 9a.75.75 0 000 1.5H15A.75.75 0 0015 9h-1.5zm-.75 3.75a.75.75 0 01.75-.75H15a.75.75 0 010 1.5h-1.5a.75.75 0 01-.75-.75zM9 19.5v-2.25a.75.75 0 01.75-.75h4.5a.75.75 0 01.75.75v2.25a.75.75 0 01-.75.75h-4.5A.75.75 0 019 19.5z" clip-rule="evenodd" />
          </svg>
        </div>
        <div class="section-content">
          <div class="provider-label">ارائه‌دهنده</div>
          <div class="provider-name">{{ providerName }}</div>
        </div>
      </div>

      <!-- Price -->
      <div class="price-section">
        <span class="price-label">هزینه:</span>
        <span class="price-value">{{ convertToPersian(totalPrice) }} تومان</span>
      </div>

      <!-- Notes (if available) -->
      <div v-if="bookingNotes" class="notes-section">
        <div class="notes-label">یادداشت:</div>
        <div class="notes-content">{{ bookingNotes }}</div>
      </div>
    </div>

    <!-- Actions Footer -->
    <div class="card-footer">
      <button
        v-if="canReschedule"
        class="action-btn btn-reschedule"
        @click="$emit('reschedule', appointmentId)"
      >
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
          <path fill-rule="evenodd" d="M15.312 11.424a5.5 5.5 0 01-9.201 2.466l-.312-.311h2.433a.75.75 0 000-1.5H3.989a.75.75 0 00-.75.75v4.242a.75.75 0 001.5 0v-2.43l.31.31a7 7 0 0011.712-3.138.75.75 0 00-1.449-.39zm1.23-3.723a.75.75 0 00.219-.53V2.929a.75.75 0 00-1.5 0V5.36l-.31-.31A7 7 0 003.239 8.188a.75.75 0 101.448.389A5.5 5.5 0 0113.89 6.11l.311.31h-2.432a.75.75 0 000 1.5h4.243a.75.75 0 00.53-.219z" clip-rule="evenodd" />
        </svg>
        تغییر زمان
      </button>
      <button
        v-if="canCancel"
        class="action-btn btn-cancel"
        @click="$emit('cancel', appointmentId)"
      >
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
          <path d="M6.28 5.22a.75.75 0 00-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 101.06 1.06L10 11.06l3.72 3.72a.75.75 0 101.06-1.06L11.06 10l3.72-3.72a.75.75 0 00-1.06-1.06L10 8.94 6.28 5.22z" />
        </svg>
        لغو رزرو
      </button>
      <button
        class="action-btn btn-details"
        @click="$emit('view-details', appointmentId)"
      >
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
          <path d="M10 12.5a2.5 2.5 0 100-5 2.5 2.5 0 000 5z" />
          <path fill-rule="evenodd" d="M.664 10.59a1.651 1.651 0 010-1.186A10.004 10.004 0 0110 3c4.257 0 7.893 2.66 9.336 6.41.147.381.146.804 0 1.186A10.004 10.004 0 0110 17c-4.257 0-7.893-2.66-9.336-6.41zM14 10a4 4 0 11-8 0 4 4 0 018 0z" clip-rule="evenodd" />
        </svg>
        جزئیات
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { AppointmentStatus } from '@/modules/booking/types/booking.types'
import { convertEnglishToPersianNumbers } from '@/shared/utils/date/jalali.utils'
import moment from 'moment-jalaali'

interface Props {
  appointmentId: string
  serviceName: string
  providerName: string
  staffName?: string
  scheduledStartTime: string
  scheduledEndTime: string
  duration: number
  status: AppointmentStatus
  totalPrice: number
  bookingNotes?: string
}

const props = defineProps<Props>()

defineEmits<{
  'reschedule': [appointmentId: string]
  'cancel': [appointmentId: string]
  'view-details': [appointmentId: string]
}>()

// Format date using Jalali calendar
const formattedDate = computed(() => {
  const date = moment(props.scheduledStartTime)
  const jDate = date.format('jYYYY/jMM/jDD')
  const dayName = date.format('dddd')
  return `${convertToPersian(jDate)} - ${dayName}`
})

// Format time
const formattedTime = computed(() => {
  const start = moment(props.scheduledStartTime).format('HH:mm')
  const end = moment(props.scheduledEndTime).format('HH:mm')
  return `${convertToPersian(start)} - ${convertToPersian(end)}`
})

// Persian status labels
const getStatusLabel = (status: AppointmentStatus): string => {
  const labels: Record<AppointmentStatus, string> = {
    [AppointmentStatus.Pending]: 'در انتظار تایید',
    [AppointmentStatus.Confirmed]: 'تایید شده',
    [AppointmentStatus.InProgress]: 'در حال انجام',
    [AppointmentStatus.Completed]: 'انجام شده',
    [AppointmentStatus.Cancelled]: 'لغو شده',
    [AppointmentStatus.NoShow]: 'عدم حضور',
    [AppointmentStatus.Rescheduled]: 'تغییر زمان داده شده',
  }
  return labels[status] || status
}

// Action button visibility
const canReschedule = computed(() => {
  return [
    AppointmentStatus.Pending,
    AppointmentStatus.Confirmed,
  ].includes(props.status)
})

const canCancel = computed(() => {
  return [
    AppointmentStatus.Pending,
    AppointmentStatus.Confirmed,
  ].includes(props.status)
})

// Convert numbers to Persian
const convertToPersian = (value: number | string): string => {
  return convertEnglishToPersianNumbers(value.toString())
}
</script>

<style scoped lang="scss">
.appointment-card {
  background: white;
  border-radius: 12px;
  border: 1px solid #e5e7eb;
  overflow: hidden;
  transition: all 0.3s ease;

  &:hover {
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
    transform: translateY(-2px);
  }

  // Status-specific border colors
  &.status-confirmed {
    border-right: 4px solid #10b981;
  }

  &.status-pending {
    border-right: 4px solid #f59e0b;
  }

  &.status-inprogress {
    border-right: 4px solid #3b82f6;
  }

  &.status-completed {
    border-right: 4px solid #6b7280;
  }

  &.status-cancelled,
  &.status-noshow {
    border-right: 4px solid #ef4444;
    opacity: 0.8;
  }

  &.status-rescheduled {
    border-right: 4px solid #8b5cf6;
  }
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px;
  background: #f9fafb;
  border-bottom: 1px solid #e5e7eb;
}

.status-badge {
  padding: 6px 12px;
  border-radius: 16px;
  font-size: 12px;
  font-weight: 600;
  display: inline-flex;
  align-items: center;

  &.badge-confirmed {
    background: #d1fae5;
    color: #065f46;
  }

  &.badge-pending {
    background: #fef3c7;
    color: #92400e;
  }

  &.badge-inprogress {
    background: #dbeafe;
    color: #1e40af;
  }

  &.badge-completed {
    background: #e5e7eb;
    color: #374151;
  }

  &.badge-cancelled,
  &.badge-noshow {
    background: #fee2e2;
    color: #991b1b;
  }

  &.badge-rescheduled {
    background: #ede9fe;
    color: #5b21b6;
  }
}

.appointment-date {
  font-size: 14px;
  color: #6b7280;
  font-weight: 500;
}

.card-body {
  padding: 20px;
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.info-section {
  display: flex;
  align-items: flex-start;
  gap: 12px;

  &.staff-section {
    padding: 12px;
    background: #f0f9ff;
    border-radius: 8px;
    border-right: 3px solid #3b82f6;
  }
}

.section-icon {
  width: 40px;
  height: 40px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;

  svg {
    width: 20px;
    height: 20px;
  }

  &.service-icon {
    background: #dbeafe;
    color: #1e40af;
  }

  &.staff-icon {
    background: #e0e7ff;
    color: #4338ca;
  }

  &.provider-icon {
    background: #fce7f3;
    color: #9f1239;
  }
}

.section-content {
  flex: 1;
  min-width: 0;
}

.service-name {
  font-size: 16px;
  font-weight: 700;
  color: #1f2937;
  margin-bottom: 8px;
}

.service-details {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
}

.detail-item {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 13px;
  color: #6b7280;

  svg {
    width: 16px;
    height: 16px;
  }
}

.staff-label {
  font-size: 12px;
  color: #6b7280;
  margin-bottom: 4px;
}

.staff-name {
  font-size: 15px;
  font-weight: 600;
  color: #1f2937;
}

.provider-label {
  font-size: 12px;
  color: #6b7280;
  margin-bottom: 4px;
}

.provider-name {
  font-size: 14px;
  font-weight: 600;
  color: #1f2937;
}

.price-section {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 12px;
  background: #f9fafb;
  border-radius: 8px;
}

.price-label {
  font-size: 14px;
  color: #6b7280;
  font-weight: 500;
}

.price-value {
  font-size: 16px;
  font-weight: 700;
  color: #1976d2;
}

.notes-section {
  padding: 12px;
  background: #fefce8;
  border-radius: 8px;
  border-right: 3px solid #facc15;
}

.notes-label {
  font-size: 12px;
  color: #854d0e;
  font-weight: 600;
  margin-bottom: 4px;
}

.notes-content {
  font-size: 13px;
  color: #713f12;
  line-height: 1.5;
}

.card-footer {
  display: flex;
  gap: 8px;
  padding: 16px;
  background: #f9fafb;
  border-top: 1px solid #e5e7eb;
}

.action-btn {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  padding: 10px 16px;
  border: none;
  border-radius: 8px;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
  font-family: 'B Nazanin', sans-serif;

  svg {
    width: 16px;
    height: 16px;
  }

  &.btn-reschedule {
    background: #dbeafe;
    color: #1e40af;

    &:hover {
      background: #bfdbfe;
    }
  }

  &.btn-cancel {
    background: #fee2e2;
    color: #991b1b;

    &:hover {
      background: #fecaca;
    }
  }

  &.btn-details {
    background: #f3f4f6;
    color: #374151;

    &:hover {
      background: #e5e7eb;
    }
  }
}

// Responsive design
@media (max-width: 640px) {
  .card-footer {
    flex-direction: column;
  }

  .service-details {
    flex-direction: column;
    gap: 8px;
  }
}
</style>
