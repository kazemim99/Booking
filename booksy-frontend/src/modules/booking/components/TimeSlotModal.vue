<template>
  <Teleport to="body">
    <div class="modal-overlay" @click.self="close" dir="rtl">
      <div class="modal-container">
        <!-- Modal Header -->
        <div class="modal-header">
          <button class="close-button" @click="close">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>

          <div class="provider-header">
            <div class="provider-avatar">
              <img v-if="provider.photoUrl" :src="provider.photoUrl" :alt="provider.name" />
              <div v-else class="avatar-placeholder">
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
                </svg>
              </div>
            </div>
            <div>
              <h2>{{ provider.name }}</h2>
              <p v-if="provider.specialization">{{ provider.specialization }}</p>
            </div>
          </div>
        </div>

        <!-- Modal Body -->
        <div class="modal-body">
          <div class="content-grid">
            <!-- Calendar Section -->
            <div class="calendar-section">
              <h3>انتخاب تاریخ</h3>
              <VuePersianDatetimePicker
                v-model="selectedDateModel"
                display-format="jYYYY/jMM/jDD"
                format="YYYY-MM-DD"
                :min="minDate"
                :max="maxDate"
                :auto-submit="true"
                :clearable="false"
                :inline="true"
                :editable="false"
                :input-class="'hidden-input'"
                type="date"
                @change="handleDateChange"
              />
            </div>

            <!-- Time Slots Section -->
            <div ref="slotsSection" class="slots-section">
              <div v-if="!selectedDate" class="empty-state">
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                </svg>
                <h4>تاریخ را انتخاب کنید</h4>
                <p>لطفاً یک تاریخ از تقویم انتخاب کنید</p>
              </div>

              <div v-else class="slots-content">
                <div class="selected-date">
                  <h3>{{ formatSelectedDate(selectedDate) }}</h3>
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
                    <span class="time">{{ convertToPersianTime(slot.startTime) }}</span>
                  </button>
                </div>

                <!-- No Slots -->
                <div v-else class="empty-state">
                  <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                  <h4>زمانی موجود نیست</h4>
                  <p>برای این روز زمان خالی موجود نیست</p>
                  <div v-if="validationMessages.length" class="validation-messages">
                    <p v-for="(message, index) in validationMessages" :key="index">{{ message }}</p>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Modal Footer -->
        <div class="modal-footer">
          <button class="cancel-button" @click="close">انصراف</button>
          <button
            class="confirm-button"
            :disabled="!selectedSlot"
            @click="confirmSelection"
          >
            تأیید رزرو
          </button>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, computed, watch, nextTick } from 'vue'
import VuePersianDatetimePicker from 'vue3-persian-datetime-picker'
import { availabilityService } from '@/modules/booking/api/availability.service'

interface Provider {
  id: string
  name: string
  photoUrl?: string
  specialization?: string
}

interface TimeSlot {
  startTime: string
  endTime: string
  staffId: string
  staffName: string
  available: boolean
}

interface Props {
  provider: Provider
  serviceId: string | null
  providerId: string
}

const props = defineProps<Props>()

const emit = defineEmits<{
  (e: 'close'): void
  (e: 'slot-selected', slot: { date: string; startTime: string; endTime: string; staffId: string; staffName: string }): void
}>()

// Refs
const slotsSection = ref<HTMLElement | null>(null)

// State
const selectedDate = ref<string | null>(null)
const selectedDateModel = ref('')
const selectedSlot = ref<TimeSlot | null>(null)
const loadingSlots = ref(false)
const availableSlots = ref<TimeSlot[]>([])
const validationMessages = ref<string[]>([])

// Computed
const minDate = computed(() => {
  return new Date().toISOString().split('T')[0]
})

const maxDate = computed(() => {
  const maxDate = new Date()
  maxDate.setMonth(maxDate.getMonth() + 3)
  return maxDate.toISOString().split('T')[0]
})

// Check if mobile
const isMobile = computed(() => {
  return window.innerWidth < 768
})

// Watch for date changes
watch(selectedDateModel, (newValue) => {
  if (newValue) {
    handleDateChange(newValue)
  }
})

// Methods
const close = () => {
  emit('close')
}

const handleDateChange = async (dateValue: any) => {
  let dateString: string = ''

  if (typeof dateValue === 'string') {
    dateString = dateValue
  } else if (typeof dateValue === 'object') {
    if (dateValue._i) {
      if (typeof dateValue._i === 'string') {
        dateString = dateValue._i.split('T')[0]
      } else if (typeof dateValue._i === 'object' && dateValue._i.year) {
        const year = dateValue._i.year
        const month = String(dateValue._i.month + 1).padStart(2, '0')
        const day = String(dateValue._i.date || dateValue._i.day).padStart(2, '0')
        dateString = `${year}-${month}-${day}`
      }
    } else if (dateValue.format && typeof dateValue.format === 'function') {
      dateString = dateValue.format('YYYY-MM-DD')
    } else if (dateValue.toString) {
      const match = dateValue.toString().match(/\d{4}-\d{2}-\d{2}/)
      if (match) {
        dateString = match[0]
      }
    }
  }

  if (!dateString || !/^\d{4}-\d{2}-\d{2}$/.test(dateString)) {
    console.error('[TimeSlotModal] Invalid date string:', dateString)
    return
  }

  selectedDate.value = dateString
  selectedSlot.value = null // Reset selected slot
  await loadAvailableSlots(dateString)
}

const loadAvailableSlots = async (dateString: string) => {
  if (!props.serviceId) {
    return
  }

  loadingSlots.value = true
  validationMessages.value = []

  try {
    const response = await availabilityService.getAvailableSlots({
      providerId: props.providerId,
      serviceId: props.serviceId,
      date: dateString,
      staffMemberId: props.provider.id,
    })

    validationMessages.value = response.validationMessages || []

    availableSlots.value = response.slots
      .filter(slot => slot.isAvailable && slot.availableStaffId === props.provider.id)
      .map(slot => ({
        startTime: new Date(slot.startTime).toLocaleTimeString('fa-IR', { hour: '2-digit', minute: '2-digit', hour12: false }),
        endTime: new Date(slot.endTime).toLocaleTimeString('fa-IR', { hour: '2-digit', minute: '2-digit', hour12: false }),
        staffId: props.provider.id,
        staffName: props.provider.name,
        available: true,
      }))

    // On mobile, scroll to time slots section after slots are loaded
    if (isMobile.value && slotsSection.value) {
      await nextTick()
      slotsSection.value.scrollIntoView({ behavior: 'smooth', block: 'start' })
    }
  } catch (error) {
    console.error('[TimeSlotModal] Failed to load slots:', error)
    availableSlots.value = []
  } finally {
    loadingSlots.value = false
  }
}

const selectSlot = (slot: TimeSlot) => {
  selectedSlot.value = slot
}

const confirmSelection = () => {
  if (selectedSlot.value && selectedDate.value) {
    emit('slot-selected', {
      date: selectedDate.value,
      startTime: selectedSlot.value.startTime,
      endTime: selectedSlot.value.endTime,
      staffId: selectedSlot.value.staffId,
      staffName: selectedSlot.value.staffName,
    })
  }
}

const formatSelectedDate = (dateString: string): string => {
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

const gregorianToJalali = (gy: number, gm: number, gd: number): [number, number, number] => {
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

const convertToPersianTime = (time: string): string => {
  const persianDigits = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹']
  return time.split('').map(char => {
    const digit = parseInt(char)
    return !isNaN(digit) ? persianDigits[digit] : char
  }).join('')
}

const convertToPersianNumber = (num: number): string => {
  const persianDigits = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹']
  return num.toString().split('').map(char => {
    const digit = parseInt(char)
    return !isNaN(digit) ? persianDigits[digit] : char
  }).join('')
}
</script>

<style scoped>
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.6);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  padding: 1rem;
  animation: fadeIn 0.3s;
}

@keyframes fadeIn {
  from {
    opacity: 0;
  }
  to {
    opacity: 1;
  }
}

.modal-container {
  background: white;
  border-radius: 24px;
  max-width: 900px;
  width: 100%;
  max-height: 90vh;
  display: flex;
  flex-direction: column;
  animation: slideUp 0.3s;
  overflow: hidden;
}

@keyframes slideUp {
  from {
    transform: translateY(30px);
    opacity: 0;
  }
  to {
    transform: translateY(0);
    opacity: 1;
  }
}

.modal-header {
  padding: 2rem;
  border-bottom: 2px solid #e2e8f0;
  position: relative;
}

.close-button {
  position: absolute;
  top: 1.5rem;
  left: 1.5rem;
  width: 36px;
  height: 36px;
  border-radius: 50%;
  background: #f1f5f9;
  border: none;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s;
}

.close-button:hover {
  background: #e2e8f0;
}

.close-button svg {
  width: 20px;
  height: 20px;
  color: #64748b;
}

.provider-header {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.provider-avatar {
  width: 60px;
  height: 60px;
  border-radius: 50%;
  overflow: hidden;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.provider-avatar img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.avatar-placeholder {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
}

.avatar-placeholder svg {
  width: 32px;
  height: 32px;
}

.provider-header h2 {
  font-size: 1.5rem;
  font-weight: 700;
  color: #1e293b;
  margin: 0 0 0.25rem 0;
}

.provider-header p {
  font-size: 0.95rem;
  color: #64748b;
  margin: 0;
}

.modal-body {
  flex: 1;
  overflow-y: auto;
  padding: 2rem;
  scroll-behavior: smooth;
}

.content-grid {
  display: grid;
  grid-template-columns: 1.2fr 1fr;
  gap: 2rem;
}

.calendar-section h3,
.selected-date h3 {
  font-size: 1.125rem;
  font-weight: 700;
  color: #1e293b;
  margin: 0 0 1rem 0;
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
  box-shadow: none;
  border: 2px solid #e2e8f0;
  border-radius: 16px;
}

.calendar-section :deep(.vpd-container) {
  width: 100%;
}

.calendar-section :deep(.vpd-header) {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.calendar-section :deep(.vpd-day-effect) {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.calendar-section :deep(.vpd-selected) {
  background: linear-gradient(135deg, #10b981 0%, #059669 100%);
}

.slots-section {
  background: #f8fafc;
  border-radius: 16px;
  padding: 1.5rem;
  display: flex;
  flex-direction: column;
}

.selected-date {
  margin-bottom: 1.5rem;
  padding-bottom: 1rem;
  border-bottom: 2px solid #e2e8f0;
}

.selected-date p {
  font-size: 0.9rem;
  color: #64748b;
  margin: 0.25rem 0 0 0;
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

.empty-state h4 {
  font-size: 1.125rem;
  font-weight: 600;
  color: #1e293b;
  margin: 0 0 0.5rem 0;
}

.empty-state p {
  font-size: 0.9rem;
  color: #64748b;
  margin: 0;
}

.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 2rem;
  gap: 1rem;
}

.spinner {
  width: 36px;
  height: 36px;
  border: 3px solid #e2e8f0;
  border-top-color: #667eea;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.loading-state p {
  font-size: 0.9rem;
  color: #64748b;
}

.time-slots {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 0.75rem;
  max-height: 400px;
  overflow-y: auto;
  padding: 0.25rem;
}

.time-slot {
  background: white;
  border: 2px solid #e2e8f0;
  border-radius: 12px;
  padding: 1rem;
  cursor: pointer;
  transition: all 0.3s;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  font-size: 1.05rem;
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
}

.time-slot.selected svg {
  color: white;
}

.validation-messages {
  margin-top: 1rem;
}

.validation-messages p {
  background: #fee2e2;
  color: #dc2626;
  padding: 0.75rem;
  border-radius: 8px;
  margin: 0.5rem 0;
  font-size: 0.85rem;
  border-right: 3px solid #dc2626;
}

.modal-footer {
  padding: 1.5rem 2rem;
  border-top: 2px solid #e2e8f0;
  display: flex;
  gap: 1rem;
  justify-content: flex-end;
}

.cancel-button,
.confirm-button {
  padding: 0.875rem 2rem;
  border-radius: 12px;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s;
  border: none;
}

.cancel-button {
  background: #f1f5f9;
  color: #64748b;
}

.cancel-button:hover {
  background: #e2e8f0;
}

.confirm-button {
  background: linear-gradient(135deg, #10b981 0%, #059669 100%);
  color: white;
}

.confirm-button:hover:not(:disabled) {
  transform: scale(1.02);
  box-shadow: 0 4px 12px rgba(16, 185, 129, 0.4);
}

.confirm-button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

@media (max-width: 768px) {
  .content-grid {
    grid-template-columns: 1fr;
  }

  .modal-body {
    padding: 1.5rem;
  }

  .modal-footer {
    flex-direction: column-reverse;
  }

  .cancel-button,
  .confirm-button {
    width: 100%;
  }

  .time-slots {
    grid-template-columns: 1fr;
  }
}
</style>
