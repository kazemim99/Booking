<template>
  <div class="appointment-list">
    <!-- Header with Filters -->
    <div class="list-header">
      <h2 class="list-title">{{ title }}</h2>

      <!-- Tab Filters -->
      <div class="tab-filters">
        <button
          v-for="tab in tabs"
          :key="tab.value"
          class="tab-button"
          :class="{ active: activeTab === tab.value }"
          @click="activeTab = tab.value"
        >
          <span class="tab-label">{{ tab.label }}</span>
          <span v-if="tab.count !== undefined" class="tab-count">
            {{ convertToPersian(tab.count) }}
          </span>
        </button>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="isLoading" class="loading-state">
      <div class="spinner"></div>
      <p>در حال بارگذاری...</p>
    </div>

    <!-- Empty State -->
    <div v-else-if="filteredAppointments.length === 0" class="empty-state">
      <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
      </svg>
      <h3>{{ emptyStateMessage }}</h3>
      <p>{{ emptyStateDescription }}</p>
    </div>

    <!-- Appointments List -->
    <div v-else class="appointments-grid">
      <AppointmentCard
        v-for="appointment in filteredAppointments"
        :key="appointment.id"
        :appointment-id="appointment.id"
        :service-name="appointment.serviceName"
        :provider-name="appointment.providerName"
        :staff-name="appointment.staffName"
        :scheduled-start-time="appointment.scheduledStartTime"
        :scheduled-end-time="appointment.scheduledEndTime"
        :duration="appointment.duration"
        :status="appointment.status"
        :total-price="appointment.totalPrice"
        :booking-notes="appointment.bookingNotes"
        @reschedule="handleReschedule"
        @cancel="handleCancel"
        @view-details="handleViewDetails"
      />
    </div>

    <!-- Pagination -->
    <div v-if="totalPages > 1" class="pagination">
      <button
        class="pagination-btn"
        :disabled="currentPage === 1"
        @click="goToPage(currentPage - 1)"
      >
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
          <path fill-rule="evenodd" d="M12.79 5.23a.75.75 0 01-.02 1.06L8.832 10l3.938 3.71a.75.75 0 11-1.04 1.08l-4.5-4.25a.75.75 0 010-1.08l4.5-4.25a.75.75 0 011.06.02z" clip-rule="evenodd" />
        </svg>
        قبلی
      </button>

      <div class="pagination-info">
        صفحه {{ convertToPersian(currentPage) }} از {{ convertToPersian(totalPages) }}
      </div>

      <button
        class="pagination-btn"
        :disabled="currentPage === totalPages"
        @click="goToPage(currentPage + 1)"
      >
        بعدی
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
          <path fill-rule="evenodd" d="M7.21 14.77a.75.75 0 01.02-1.06L11.168 10 7.23 6.29a.75.75 0 111.04-1.08l4.5 4.25a.75.75 0 010 1.08l-4.5 4.25a.75.75 0 01-1.06-.02z" clip-rule="evenodd" />
        </svg>
      </button>
    </div>

    <!-- Reschedule Modal -->
    <RescheduleModal
      v-if="showRescheduleModal"
      v-model="showRescheduleModal"
      :appointment-id="selectedAppointmentId"
      @reschedule="onRescheduleConfirm"
    />

    <!-- Cancel Modal -->
    <CancelModal
      v-if="showCancelModal"
      v-model="showCancelModal"
      :appointment-id="selectedAppointmentId"
      @cancel="onCancelConfirm"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import AppointmentCard from './AppointmentCard.vue'
import RescheduleModal from './RescheduleModal.vue'
import CancelModal from './CancelModal.vue'
import { convertEnglishToPersianNumbers } from '@/shared/utils/date/jalali.utils'
import { AppointmentStatus } from '@/modules/booking/types/booking.types'

// Extended appointment type with populated names
export interface AppointmentWithDetails {
  id: string
  providerId: string
  clientId: string
  serviceId: string
  staffMemberId?: string

  // Populated names
  serviceName: string
  providerName: string
  staffName?: string

  // Scheduling
  scheduledStartTime: string
  scheduledEndTime: string
  duration: number

  // Status & Info
  status: AppointmentStatus
  bookingNotes?: string

  // Pricing
  totalPrice: number
  currency: string
}

type TabFilter = 'all' | 'upcoming' | 'past' | 'pending' | 'confirmed' | 'cancelled'

interface Tab {
  value: TabFilter
  label: string
  count?: number
}

interface Props {
  appointments?: AppointmentWithDetails[]
  title?: string
  isLoading?: boolean
  showTabs?: boolean
  defaultTab?: TabFilter
  pageSize?: number
}

const props = withDefaults(defineProps<Props>(), {
  appointments: () => [],
  title: 'رزروهای من',
  isLoading: false,
  showTabs: true,
  defaultTab: 'all',
  pageSize: 10,
})

const emit = defineEmits<{
  'reschedule': [appointmentId: string, newTime: string]
  'cancel': [appointmentId: string, reason: string]
  'view-details': [appointmentId: string]
  'page-change': [page: number]
}>()

// State
const activeTab = ref<TabFilter>(props.defaultTab)
const currentPage = ref(1)
const showRescheduleModal = ref(false)
const showCancelModal = ref(false)
const selectedAppointmentId = ref('')

// Tabs configuration
const tabs = computed<Tab[]>(() => {
  const now = new Date()

  return [
    {
      value: 'all' as TabFilter,
      label: 'همه',
      count: props.appointments.length,
    },
    {
      value: 'upcoming' as TabFilter,
      label: 'آینده',
      count: props.appointments.filter(a =>
        new Date(a.scheduledStartTime) > now &&
        [AppointmentStatus.Confirmed, AppointmentStatus.Pending].includes(a.status)
      ).length,
    },
    {
      value: 'past' as TabFilter,
      label: 'گذشته',
      count: props.appointments.filter(a =>
        new Date(a.scheduledStartTime) < now ||
        [AppointmentStatus.Completed, AppointmentStatus.Cancelled].includes(a.status)
      ).length,
    },
    {
      value: 'pending' as TabFilter,
      label: 'در انتظار تایید',
      count: props.appointments.filter(a => a.status === AppointmentStatus.Pending).length,
    },
    {
      value: 'confirmed' as TabFilter,
      label: 'تایید شده',
      count: props.appointments.filter(a => a.status === AppointmentStatus.Confirmed).length,
    },
  ]
})

// Filtered appointments based on active tab
const filteredAppointments = computed(() => {
  const now = new Date()

  let filtered = props.appointments

  switch (activeTab.value) {
    case 'upcoming':
      filtered = props.appointments.filter(a =>
        new Date(a.scheduledStartTime) > now &&
        [AppointmentStatus.Confirmed, AppointmentStatus.Pending].includes(a.status)
      )
      break
    case 'past':
      filtered = props.appointments.filter(a =>
        new Date(a.scheduledStartTime) < now ||
        [AppointmentStatus.Completed, AppointmentStatus.Cancelled].includes(a.status)
      )
      break
    case 'pending':
      filtered = props.appointments.filter(a => a.status === AppointmentStatus.Pending)
      break
    case 'confirmed':
      filtered = props.appointments.filter(a => a.status === AppointmentStatus.Confirmed)
      break
    case 'cancelled':
      filtered = props.appointments.filter(a => a.status === AppointmentStatus.Cancelled)
      break
    default:
      filtered = props.appointments
  }

  // Sort by date (upcoming first, then by nearest date)
  return filtered.sort((a, b) => {
    const dateA = new Date(a.scheduledStartTime)
    const dateB = new Date(b.scheduledStartTime)
    return dateA.getTime() - dateB.getTime()
  })
})

// Pagination
const totalPages = computed(() => {
  return Math.ceil(filteredAppointments.value.length / props.pageSize)
})

const paginatedAppointments = computed(() => {
  const start = (currentPage.value - 1) * props.pageSize
  const end = start + props.pageSize
  return filteredAppointments.value.slice(start, end)
})

// Empty state messages
const emptyStateMessage = computed(() => {
  switch (activeTab.value) {
    case 'upcoming':
      return 'رزرو آینده‌ای ندارید'
    case 'past':
      return 'رزرو گذشته‌ای ندارید'
    case 'pending':
      return 'رزرو در انتظار تاییدی ندارید'
    case 'confirmed':
      return 'رزرو تایید شده‌ای ندارید'
    case 'cancelled':
      return 'رزرو لغو شده‌ای ندارید'
    default:
      return 'رزروی یافت نشد'
  }
})

const emptyStateDescription = computed(() => {
  switch (activeTab.value) {
    case 'upcoming':
      return 'هنوز رزروی برای آینده ثبت نکرده‌اید'
    case 'past':
      return 'هیچ رزرو گذشته‌ای در سیستم ثبت نشده است'
    default:
      return 'می‌توانید از بخش جستجو، خدمت مورد نظر خود را رزرو کنید'
  }
})

// Event Handlers
const handleReschedule = (appointmentId: string) => {
  selectedAppointmentId.value = appointmentId
  showRescheduleModal.value = true
}

const handleCancel = (appointmentId: string) => {
  selectedAppointmentId.value = appointmentId
  showCancelModal.value = true
}

const handleViewDetails = (appointmentId: string) => {
  emit('view-details', appointmentId)
}

const onRescheduleConfirm = (newTime: string) => {
  emit('reschedule', selectedAppointmentId.value, newTime)
  showRescheduleModal.value = false
}

const onCancelConfirm = (reason: string) => {
  emit('cancel', selectedAppointmentId.value, reason)
  showCancelModal.value = false
}

const goToPage = (page: number) => {
  if (page >= 1 && page <= totalPages.value) {
    currentPage.value = page
    emit('page-change', page)
  }
}

// Convert numbers to Persian
const convertToPersian = (value: number | string): string => {
  return convertEnglishToPersianNumbers(value.toString())
}

// Reset to page 1 when filter changes
watch(activeTab, () => {
  currentPage.value = 1
})
</script>

<style scoped lang="scss">
.appointment-list {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.list-header {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.list-title {
  font-size: 24px;
  font-weight: 700;
  color: #1f2937;
  margin: 0;
}

.tab-filters {
  display: flex;
  gap: 8px;
  overflow-x: auto;
  padding-bottom: 4px;

  &::-webkit-scrollbar {
    height: 4px;
  }

  &::-webkit-scrollbar-thumb {
    background: #d1d5db;
    border-radius: 2px;
  }
}

.tab-button {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 16px;
  background: #f3f4f6;
  border: 1px solid transparent;
  border-radius: 8px;
  font-size: 14px;
  font-weight: 600;
  color: #6b7280;
  cursor: pointer;
  transition: all 0.2s;
  white-space: nowrap;
  font-family: 'B Nazanin', sans-serif;

  &:hover {
    background: #e5e7eb;
  }

  &.active {
    background: #dbeafe;
    color: #1e40af;
    border-color: #3b82f6;
  }
}

.tab-label {
  font-weight: 600;
}

.tab-count {
  display: flex;
  align-items: center;
  justify-content: center;
  min-width: 24px;
  height: 24px;
  padding: 0 8px;
  background: rgba(255, 255, 255, 0.8);
  border-radius: 12px;
  font-size: 12px;
  font-weight: 700;
}

.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 60px 20px;
  color: #6b7280;

  .spinner {
    width: 48px;
    height: 48px;
    border: 4px solid #e5e7eb;
    border-top-color: #3b82f6;
    border-radius: 50%;
    animation: spin 1s linear infinite;
    margin-bottom: 16px;
  }

  p {
    font-size: 14px;
    margin: 0;
  }
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 60px 20px;
  text-align: center;

  svg {
    width: 64px;
    height: 64px;
    color: #9ca3af;
    margin-bottom: 16px;
  }

  h3 {
    font-size: 18px;
    font-weight: 600;
    color: #374151;
    margin: 0 0 8px 0;
  }

  p {
    font-size: 14px;
    color: #6b7280;
    margin: 0;
  }
}

.appointments-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(min(100%, 400px), 1fr));
  gap: 20px;
}

.pagination {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 16px;
  padding: 20px;
}

.pagination-btn {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 16px;
  background: #f3f4f6;
  border: 1px solid #d1d5db;
  border-radius: 8px;
  font-size: 14px;
  font-weight: 600;
  color: #374151;
  cursor: pointer;
  transition: all 0.2s;
  font-family: 'B Nazanin', sans-serif;

  svg {
    width: 20px;
    height: 20px;
  }

  &:hover:not(:disabled) {
    background: #e5e7eb;
  }

  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
}

.pagination-info {
  font-size: 14px;
  color: #6b7280;
  font-weight: 500;
}

// Responsive design
@media (max-width: 768px) {
  .appointments-grid {
    grid-template-columns: 1fr;
  }

  .tab-filters {
    justify-content: flex-start;
  }
}
</style>
