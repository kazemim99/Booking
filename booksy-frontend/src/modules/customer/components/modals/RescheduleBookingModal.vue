<template>
  <BaseModal
    :is-open="isOpen"
    @close="handleClose"
    title="تغییر زمان رزرو"
    size="md"
  >
    <div class="reschedule-modal-content">
      <!-- Current Booking Summary -->
      <div class="current-booking-section">
        <h4 class="section-title">رزرو فعلی</h4>
        <div v-if="booking" class="booking-summary">
          <div class="summary-row">
            <span class="label">خدمت:</span>
            <span class="value">{{ booking.serviceName }}</span>
          </div>
          <div class="summary-row">
            <span class="label">ارائه‌دهنده:</span>
            <span class="value">{{ booking.providerName }}</span>
          </div>
          <div v-if="booking.staffName" class="summary-row">
            <span class="label">کارمند:</span>
            <span class="value">{{ booking.staffName }}</span>
          </div>
          <div class="summary-row highlight">
            <span class="label">زمان فعلی:</span>
            <span class="value">{{ currentDateTime }}</span>
          </div>
        </div>
      </div>

      <!-- New Time Selection -->
      <div class="new-time-section">
        <h4 class="section-title">انتخاب زمان جدید</h4>

        <!-- Calendar Section -->
        <div class="calendar-section">
          <h5>انتخاب تاریخ</h5>
          <VuePersianDatetimePicker
            v-model="newDateModel"
            display-format="jYYYY/jMM/jDD"
            format="YYYY-MM-DD"
            :min="minDate"
            :auto-submit="true"
            :clearable="false"
            :inline="true"
            :editable="false"
            :input-class="'hidden-input'"
            type="date"
          />
        </div>

        <!-- Time Slots Section -->
        <div v-if="newDate" ref="slotsSection" class="slots-section">
          <div class="selected-date-header">
            <h5>{{ formatSelectedDate(newDate) }}</h5>
            <p>زمان‌های موجود</p>
          </div>

          <!-- Loading -->
          <div v-if="loadingSlots" class="loading-state">
            <div class="spinner"></div>
            <p>در حال بارگذاری...</p>
          </div>

          <!-- Time Slots -->
          <div v-else-if="availableSlots.length > 0" class="time-slots">
            <button
              v-for="slot in availableSlots"
              :key="slot.startTime"
              class="time-slot"
              :class="{ selected: selectedSlot?.startTime === slot.startTime }"
              @click="selectSlot(slot)"
            >
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm1-12a1 1 0 10-2 0v4a1 1 0 00.293.707l2.828 2.829a1 1 0 101.415-1.415L11 9.586V6z" clip-rule="evenodd" />
              </svg>
              <span class="time">{{ formatTimeSlot(slot.startTime) }}</span>
            </button>
          </div>

          <!-- No Slots -->
          <div v-else class="empty-state">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            <h6>زمانی موجود نیست</h6>
            <p>برای این روز زمان خالی موجود نیست</p>
          </div>
        </div>

        <!-- Reason (Optional) -->
        <div v-if="selectedSlot" class="form-group">
          <label for="reason" class="form-label">دلیل تغییر زمان (اختیاری)</label>
          <textarea
            id="reason"
            v-model="reason"
            class="form-textarea"
            rows="2"
            placeholder="مثال: تداخل زمانی با کار..."
          ></textarea>
        </div>
      </div>

      <!-- Summary of Changes (if slot selected) -->
      <div v-if="selectedSlot" class="change-summary">
        <div class="summary-header">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" class="info-icon">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          <span>خلاصه تغییرات</span>
        </div>
        <div class="change-details">
          <div class="change-row">
            <span class="old-value">{{ currentDateTime }}</span>
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" class="arrow-icon">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14 5l7 7m0 0l-7 7m7-7H3" />
            </svg>
            <span class="new-value">{{ newDateTime }}</span>
          </div>
        </div>
      </div>

      <!-- Actions -->
      <div class="modal-actions">
        <button
          @click="handleClose"
          class="btn btn-secondary"
          :disabled="loading"
        >
          انصراف
        </button>
        <button
          @click="handleConfirm"
          class="btn btn-primary"
          :disabled="loading || !selectedSlot"
        >
          <span v-if="loading" class="spinner-small"></span>
          <span v-else>تأیید تغییر زمان</span>
        </button>
      </div>
    </div>
  </BaseModal>
</template>

<script setup lang="ts">
import { ref, computed, watch, nextTick } from 'vue'
import BaseModal from '@/shared/components/ui/BaseModal.vue'
import VuePersianDatetimePicker from 'vue3-persian-datetime-picker'
import { availabilityService } from '@/modules/booking/api/availability.service'
import type { EnrichedBookingView } from '@/modules/booking/mappers/booking-dto.mapper'
import { useNotification } from '@/core/composables/useNotification'

interface TimeSlot {
  startTime: string
  endTime: string
  isAvailable: boolean
  staffId?: string
}

interface Props {
  isOpen: boolean
  booking?: EnrichedBookingView | null
}

const props = defineProps<Props>()

const emit = defineEmits<{
  close: []
  confirm: [newStartTime: string, reason?: string]
}>()

// Composables
const { showError } = useNotification()

// Refs
const slotsSection = ref<HTMLElement | null>(null)

// State
const newDate = ref<string | null>(null)
const newDateModel = ref('')
const selectedSlot = ref<TimeSlot | null>(null)
const reason = ref('')
const loading = ref(false)
const loadingSlots = ref(false)
const availableSlots = ref<TimeSlot[]>([])

// Computed
const minDate = computed(() => {
  const tomorrow = new Date()
  tomorrow.setDate(tomorrow.getDate() + 1)
  return tomorrow.toISOString().split('T')[0]
})

const currentDateTime = computed(() => {
  if (!props.booking) return ''
  const date = new Date(props.booking.startTime)
  return date.toLocaleString('fa-IR', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
})

const newDateTime = computed(() => {
  if (!selectedSlot.value) return ''
  const date = new Date(selectedSlot.value.startTime)
  return date.toLocaleString('fa-IR', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
})

// Methods
async function handleDateChange(dateValue: any): Promise<void> {
  console.log('[RescheduleModal] handleDateChange called with:', dateValue, 'Type:', typeof dateValue)

  // Skip if no value
  if (!dateValue) {
    console.log('[RescheduleModal] No date value, skipping')
    return
  }

  let dateString: string = ''

  if (typeof dateValue === 'string') {
    dateString = dateValue
    console.log('[RescheduleModal] String value:', dateString)
  } else if (typeof dateValue === 'object') {
    console.log('[RescheduleModal] Object value structure:', JSON.stringify(dateValue, null, 2))

    if (dateValue._i) {
      if (typeof dateValue._i === 'string') {
        dateString = dateValue._i.split('T')[0]
        console.log('[RescheduleModal] Extracted from _i string:', dateString)
      } else if (typeof dateValue._i === 'object' && dateValue._i.year) {
        const year = dateValue._i.year
        const month = String(dateValue._i.month + 1).padStart(2, '0')
        const day = String(dateValue._i.date || dateValue._i.day).padStart(2, '0')
        dateString = `${year}-${month}-${day}`
        console.log('[RescheduleModal] Constructed from _i object:', dateString)
      }
    } else if (dateValue.format && typeof dateValue.format === 'function') {
      dateString = dateValue.format('YYYY-MM-DD')
      console.log('[RescheduleModal] Formatted using .format():', dateString)
    } else if (dateValue.toString) {
      const str = dateValue.toString()
      console.log('[RescheduleModal] toString value:', str)
      const match = str.match(/\d{4}-\d{2}-\d{2}/)
      if (match) {
        dateString = match[0]
        console.log('[RescheduleModal] Extracted from toString:', dateString)
      }
    }
  }

  if (!dateString || !/^\d{4}-\d{2}-\d{2}$/.test(dateString)) {
    console.error('[RescheduleModal] Invalid date string:', dateString, 'from value:', dateValue)
    return
  }

  console.log('[RescheduleModal] Final date string:', dateString)
  newDate.value = dateString
  selectedSlot.value = null
  await loadAvailableSlots(dateString)
}

async function loadAvailableSlots(dateString: string): Promise<void> {
  if (!props.booking) return

  loadingSlots.value = true
  availableSlots.value = []

  try {
    const response = await availabilityService.getAvailableSlots({
      providerId: props.booking.providerId,
      serviceId: props.booking.serviceId,
      date: dateString,
      staffMemberId: props.booking.staffId || undefined
    })

    // Filter available slots and ensure unique times
    let filteredSlots = response.slots.filter(slot => slot.isAvailable)

    // If we have a specific staff ID, filter to only show that staff's slots
    const bookingStaffId = props.booking?.staffId
    if (bookingStaffId) {
      filteredSlots = filteredSlots.filter(slot => {
        const slotStaffId = slot.availableStaffId || slot.staffMemberId
        return slotStaffId === bookingStaffId
      })
    }

    // Deduplicate by start time (in case backend returns multiple slots for the same time)
    const uniqueSlots = new Map<string, TimeSlot>()
    filteredSlots.forEach(slot => {
      const key = slot.startTime
      if (!uniqueSlots.has(key)) {
        uniqueSlots.set(key, slot)
      }
    })

    availableSlots.value = Array.from(uniqueSlots.values())

    console.log('[RescheduleModal] Loaded', availableSlots.value.length, 'available slots for', dateString,
                'Staff:', props.booking.staffId || 'any')

    // Scroll to time slots section smoothly after slots are loaded
    await nextTick()
    if (slotsSection.value) {
      slotsSection.value.scrollIntoView({ behavior: 'smooth', block: 'start' })
    }
  } catch (error) {
    console.error('[RescheduleModal] Error loading slots:', error)
    showErrorMessage('خطا در بارگذاری زمان‌های خالی')
  } finally {
    loadingSlots.value = false
  }
}

function selectSlot(slot: TimeSlot): void {
  selectedSlot.value = slot
}

function formatTimeSlot(dateTime: string): string {
  const date = new Date(dateTime)
  return date.toLocaleTimeString('fa-IR', {
    hour: '2-digit',
    minute: '2-digit'
  })
}

function handleClose(): void {
  if (!loading.value) {
    resetForm()
    emit('close')
  }
}

function handleConfirm(): void {
  if (!selectedSlot.value) return

  emit('confirm', selectedSlot.value.startTime, reason.value || undefined)
}

function resetForm(): void {
  newDate.value = null
  newDateModel.value = ''
  selectedSlot.value = null
  reason.value = ''
  availableSlots.value = []
  loading.value = false
}

function formatSelectedDate(dateString: string): string {
  const weekDays = ['یکشنبه', 'دوشنبه', 'سه‌شنبه', 'چهارشنبه', 'پنج‌شنبه', 'جمعه', 'شنبه']
  const persianMonths = [
    'فروردین', 'اردیبهشت', 'خرداد', 'تیر', 'مرداد', 'شهریور',
    'مهر', 'آبان', 'آذر', 'دی', 'بهمن', 'اسفند'
  ]

  const [year, month, day] = dateString.split('-').map(Number)
  const date = new Date(year, month - 1, day)
  const dayName = weekDays[date.getDay()]

  const jalaliDate = gregorianToJalali(year, month, day)
  const persianYear = convertToPersianNumber(jalaliDate[0])
  const persianDay = convertToPersianNumber(jalaliDate[2])
  const monthName = persianMonths[jalaliDate[1] - 1]

  return `${dayName}، ${persianDay} ${monthName} ${persianYear}`
}

function gregorianToJalali(gy: number, gm: number, gd: number): [number, number, number] {
  const g_d_m = [0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334]

  let jy = gy <= 1600 ? 0 : 979
  gy -= gy <= 1600 ? 621 : 1600

  const gy2 = gm > 2 ? gy + 1 : gy
  let days = 365 * gy + Math.floor((gy2 + 3) / 4) - Math.floor((gy2 + 99) / 100) +
             Math.floor((gy2 + 399) / 400) - 80 + gd + g_d_m[gm - 1]

  jy += 33 * Math.floor(days / 12053)
  days %= 12053
  jy += 4 * Math.floor(days / 1461)
  days %= 1461

  if (days > 365) {
    jy += Math.floor((days - 1) / 365)
    days = (days - 1) % 365
  }

  const jm = days < 186 ? 1 + Math.floor(days / 31) : 7 + Math.floor((days - 186) / 30)
  const jd = 1 + (days < 186 ? days % 31 : (days - 186) % 30)

  return [jy, jm, jd]
}

function convertToPersianNumber(num: number): string {
  const persianDigits = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹']
  return num.toString().split('').map(char => {
    const digit = parseInt(char)
    return !isNaN(digit) ? persianDigits[digit] : char
  }).join('')
}

// Watch for modal open/close
watch(() => props.isOpen, (isOpen) => {
  if (!isOpen) {
    resetForm()
  }
})

// Watch for date model changes and trigger slot loading
watch(newDateModel, async (value) => {
  console.log('[RescheduleModal] newDateModel changed to:', value, 'Type:', typeof value)

  if (!value) {
    console.log('[RescheduleModal] Empty value, skipping')
    return
  }

  await handleDateChange(value)
})

// Notification helpers
function showErrorMessage(message: string): void {
  showError('خطا', message)
}

defineExpose({ resetForm })
</script>

<style scoped lang="scss">
.reschedule-modal-content {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.current-booking-section,
.new-time-section {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.section-title {
  font-size: 1rem;
  font-weight: 600;
  color: #111827;
  margin: 0;
  padding-bottom: 0.5rem;
  border-bottom: 2px solid #e5e7eb;
}

.booking-summary {
  background: #f9fafb;
  border-radius: 8px;
  padding: 1rem;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.summary-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 0.875rem;

  .label {
    color: #6b7280;
    font-weight: 500;
  }

  .value {
    color: #111827;
    font-weight: 600;
  }

  &.highlight {
    background: #fef3c7;
    padding: 0.5rem;
    border-radius: 6px;
    margin-top: 0.25rem;

    .label {
      color: #92400e;
    }

    .value {
      color: #78350f;
    }
  }
}

/* Calendar Section */
.calendar-section {
  margin-top: 1rem;
  display: flex;
  flex-direction: column;
  align-items: center;
  position: relative;
  width: 100%;
}

.calendar-section h5 {
  font-size: 1rem;
  font-weight: 600;
  color: #1e293b;
  margin: 0 0 1rem 0;
  text-align: center;
  width: 100%;
}

/* Hide the input field completely */
.calendar-section :deep(.vpd-input-group) {
  display: none !important;
}

.calendar-section :deep(.hidden-input) {
  display: none !important;
}

/* Calendar Styling */
.calendar-section :deep(.vpd-main) {
  width: 100%;
  max-width: 320px;
  margin: 0 auto;
  box-shadow: none;
  border-radius: 12px;
  position: static !important;
}

.calendar-section :deep(.vpd-container) {
  width: 320px !important;
  position: static !important;
  left: auto !important;
  right: auto !important;
  top: auto !important;
  bottom: auto !important;
  transform: none !important;
  margin: 0 auto;
}

.calendar-section :deep(.vpd-content) {
  width: 100% !important;
  border: 2px solid #e2e8f0;
  border-radius: 12px;
  overflow: hidden;
  position: static !important;
  left: auto !important;
  right: auto !important;
  top: auto !important;
  bottom: auto !important;
}

.calendar-section :deep(.vpd-header) {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  border-radius: 10px 10px 0 0;
}

.calendar-section :deep(.vpd-day-effect) {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

/* Slots Section - Below Calendar */
.slots-section {
  background: #f8fafc;
  border-radius: 12px;
  padding: 1.5rem;
  margin-top: 1.5rem;
  scroll-margin-top: 20px; /* For smooth scroll offset */
}

.selected-date-header {
  margin-bottom: 1.5rem;
  padding-bottom: 0.75rem;
  border-bottom: 2px solid #e2e8f0;
}

.selected-date-header h5 {
  font-size: 1.125rem;
  font-weight: 600;
  color: #1e293b;
  margin: 0 0 0.5rem 0;
}

.selected-date-header p {
  font-size: 0.875rem;
  color: #64748b;
  margin: 0;
}

/* Time Slots Grid */
.time-slots {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(120px, 1fr));
  gap: 0.75rem;
  max-height: 400px;
  overflow-y: auto;
  padding: 0.25rem;
}

.time-slot {
  background: white;
  border: 2px solid #e2e8f0;
  border-radius: 10px;
  padding: 1rem;
  cursor: pointer;
  transition: all 0.3s;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  font-size: 1rem;
  font-weight: 600;
  color: #1e293b;
}

.time-slot svg {
  width: 18px;
  height: 18px;
  color: #64748b;
}

.time-slot:hover {
  border-color: #cbd5e1;
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
}

.time-slot.selected {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  border-color: #667eea;
  color: white;
  box-shadow: 0 4px 16px rgba(102, 126, 234, 0.3);
}

.time-slot.selected svg {
  color: white;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  margin-top: 1rem;
}

.form-label {
  font-size: 0.875rem;
  font-weight: 500;
  color: #374151;
}

.form-textarea {
  padding: 0.75rem;
  border: 1px solid #d1d5db;
  border-radius: 8px;
  font-size: 0.875rem;
  font-family: inherit;
  transition: border-color 0.2s;
  resize: vertical;
  min-height: 60px;

  &:focus {
    outline: none;
    border-color: #667eea;
    box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
  }
}

.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 2rem;
  gap: 1rem;
  color: #6b7280;
}

.spinner {
  width: 40px;
  height: 40px;
  border: 3px solid #e5e7eb;
  border-top-color: #667eea;
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

.spinner-small {
  display: inline-block;
  width: 16px;
  height: 16px;
  border: 2px solid rgba(255, 255, 255, 0.3);
  border-top-color: white;
  border-radius: 50%;
  animation: spin 0.6s linear infinite;
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
  text-align: center;
  padding: 2rem;
  flex: 1;
}

.empty-state svg {
  width: 48px;
  height: 48px;
  color: #cbd5e1;
  margin-bottom: 1rem;
}

.empty-state h6 {
  font-size: 1rem;
  font-weight: 600;
  color: #1e293b;
  margin: 0 0 0.5rem;
}

.empty-state p {
  font-size: 0.85rem;
  color: #64748b;
  margin: 0;
}

.change-summary {
  background: #dbeafe;
  border: 1px solid #93c5fd;
  border-radius: 8px;
  padding: 1rem;
}

.summary-header {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-weight: 600;
  color: #1e40af;
  margin-bottom: 0.75rem;
}

.info-icon {
  width: 20px;
  height: 20px;
  color: #3b82f6;
}

.change-details {
  padding-top: 0.5rem;
  border-top: 1px solid #93c5fd;
}

.change-row {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 1rem;
  font-size: 0.875rem;
}

.old-value {
  color: #991b1b;
  font-weight: 500;
  text-decoration: line-through;
  opacity: 0.7;
}

.new-value {
  color: #065f46;
  font-weight: 600;
}

.arrow-icon {
  width: 20px;
  height: 20px;
  color: #6b7280;
}

.modal-actions {
  display: flex;
  gap: 0.75rem;
  margin-top: 0.5rem;
}

.btn {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 0.75rem 1.5rem;
  border-radius: 8px;
  font-size: 0.875rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
  border: none;

  &:disabled {
    opacity: 0.6;
    cursor: not-allowed;
  }
}

.btn-secondary {
  background: white;
  color: #374151;
  border: 1px solid #d1d5db;

  &:hover:not(:disabled) {
    background: #f9fafb;
    border-color: #9ca3af;
  }
}

.btn-primary {
  background: #667eea;
  color: white;

  &:hover:not(:disabled) {
    background: #5568d3;
    transform: translateY(-1px);
    box-shadow: 0 4px 12px rgba(102, 126, 234, 0.3);
  }

  &:active:not(:disabled) {
    transform: translateY(0);
  }
}

// RTL Support
[dir='rtl'] {
  .reschedule-modal-content,
  .booking-summary,
  .form-group,
  .change-summary {
    direction: rtl;
  }

  .arrow-icon {
    transform: scaleX(-1);
  }
}

// Responsive
@media (max-width: 768px) {
  .calendar-section :deep(.vpd-main) {
    max-width: 100%;
  }

  .time-slots {
    grid-template-columns: repeat(auto-fill, minmax(100px, 1fr));
  }

  .slots-section {
    padding: 1rem;
  }
}
</style>
