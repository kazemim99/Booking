<template>
  <div class="booking-wizard" dir="rtl">
    <div class="wizard-container">
      <!-- Progress Steps -->
      <div class="wizard-header">
        <div class="steps-indicator">
          <div
            v-for="(step, index) in steps"
            :key="step.id"
            class="step-item"
            :class="{
              active: currentStep === index + 1,
              completed: currentStep > index + 1
            }"
          >
            <div class="step-circle">
              <svg v-if="currentStep > index + 1" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                <path fill-rule="evenodd" d="M19.916 4.626a.75.75 0 01.208 1.04l-9 13.5a.75.75 0 01-1.154.114l-6-6a.75.75 0 011.06-1.06l5.353 5.353 8.493-12.739a.75.75 0 011.04-.208z" clip-rule="evenodd" />
              </svg>
              <span v-else>{{ convertToPersianNumber(index + 1) }}</span>
            </div>
            <div class="step-info">
              <div class="step-label">{{ step.label }}</div>
              <div class="step-description">{{ step.description }}</div>
            </div>
            <div v-if="index < steps.length - 1" class="step-connector"></div>
          </div>
        </div>
      </div>

      <!-- Wizard Content -->
      <div class="wizard-content">
        <!-- Step 1: Service Selection -->
        <ServiceSelection
          v-if="currentStep === 1"
          :provider-id="providerId"
          :selected-service-id="bookingData.serviceId"
          @service-selected="handleServiceSelected"
        />

        <!-- Step 2: Date & Time Selection -->
        <SlotSelection
          v-if="currentStep === 2"
          :provider-id="providerId"
          :service-id="bookingData.serviceId"
          :selected-date="bookingData.date"
          :selected-time="bookingData.startTime"
          :selected-staff-id="bookingData.staffId"
          @slot-selected="handleSlotSelected"
        />

        <!-- Step 3: Customer Information -->
        <CustomerInfo
          v-if="currentStep === 3"
          :customer-data="bookingData.customerInfo"
          @info-updated="handleCustomerInfoUpdated"
        />

        <!-- Step 4: Confirmation -->
        <BookingConfirmation
          v-if="currentStep === 4"
          :booking-data="bookingData"
          :provider-id="providerId"
        />
      </div>

      <!-- Navigation Buttons -->
      <div class="wizard-footer">
        <button
          v-if="currentStep > 1 && currentStep < 5"
          class="btn-secondary"
          @click="previousStep"
        >
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
          </svg>
          مرحله قبل
        </button>

        <div class="spacer"></div>

        <button
          v-if="currentStep < 3"
          class="btn-primary"
          :disabled="!canProceed"
          @click="nextStep"
        >
          مرحله بعد
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
          </svg>
        </button>

        <button
          v-if="currentStep === 3"
          class="btn-primary"
          :disabled="!canProceed"
          @click="reviewBooking"
        >
          بررسی و تایید
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
        </button>

        <button
          v-if="currentStep === 4"
          class="btn-primary"
          :disabled="isSubmitting"
          @click="submitBooking"
        >
          <span v-if="!isSubmitting">تایید و رزرو نهایی</span>
          <span v-else class="loading-text">
            <span class="spinner"></span>
            در حال ثبت رزرو...
          </span>
        </button>
      </div>
    </div>

    <!-- Success Modal -->
    <Teleport to="body">
      <div v-if="showSuccessModal" class="success-modal" @click="closeSuccessModal">
        <div class="modal-content" @click.stop>
          <div class="success-icon">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
              <path fill-rule="evenodd" d="M2.25 12c0-5.385 4.365-9.75 9.75-9.75s9.75 4.365 9.75 9.75-4.365 9.75-9.75 9.75S2.25 17.385 2.25 12zm13.36-1.814a.75.75 0 10-1.22-.872l-3.236 4.53L9.53 12.22a.75.75 0 00-1.06 1.06l2.25 2.25a.75.75 0 001.14-.094l3.75-5.25z" clip-rule="evenodd" />
            </svg>
          </div>
          <h2>رزرو شما با موفقیت ثبت شد!</h2>
          <p>کد رزرو: <strong>{{ convertToPersianNumber(bookingId) }}</strong></p>
          <p class="confirmation-text">
            اطلاعات رزرو به ایمیل و شماره تماس شما ارسال شد.
          </p>
          <div class="modal-actions">
            <button class="btn-primary" @click="goToMyBookings">
              مشاهده رزروهای من
            </button>
            <button class="btn-secondary" @click="closeSuccessModal">
              بستن
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import ServiceSelection from './ServiceSelection.vue'
import SlotSelection from './SlotSelection.vue'
import CustomerInfo from './CustomerInfo.vue'
import BookingConfirmation from './BookingConfirmation.vue'

const router = useRouter()
const route = useRoute()

// Props
const providerId = ref(route.query.providerId as string || '')

// State
const currentStep = ref(1)
const isSubmitting = ref(false)
const showSuccessModal = ref(false)
const bookingId = ref('')

const steps = [
  { id: 1, label: 'انتخاب خدمت', description: 'خدمت مورد نظر را انتخاب کنید' },
  { id: 2, label: 'انتخاب زمان', description: 'تاریخ و ساعت مناسب را انتخاب کنید' },
  { id: 3, label: 'اطلاعات تماس', description: 'اطلاعات خود را وارد کنید' },
  { id: 4, label: 'تایید نهایی', description: 'بررسی و تایید رزرو' },
]

interface BookingData {
  serviceId: string | null
  serviceName: string
  servicePrice: number
  serviceDuration: number
  date: string | null
  startTime: string | null
  endTime: string | null
  staffId: string | null
  staffName: string
  customerInfo: {
    fullName: string
    phoneNumber: string
    email: string
    notes: string
  }
}

const bookingData = ref<BookingData>({
  serviceId: route.query.serviceId as string || null,
  serviceName: '',
  servicePrice: 0,
  serviceDuration: 0,
  date: null,
  startTime: null,
  endTime: null,
  staffId: null,
  staffName: '',
  customerInfo: {
    fullName: '',
    phoneNumber: '',
    email: '',
    notes: '',
  },
})

// Computed
const canProceed = computed(() => {
  switch (currentStep.value) {
    case 1:
      return !!bookingData.value.serviceId
    case 2:
      return !!bookingData.value.date && !!bookingData.value.startTime
    case 3:
      return !!(
        bookingData.value.customerInfo.fullName &&
        bookingData.value.customerInfo.phoneNumber
      )
    default:
      return true
  }
})

// Methods
const convertToPersianNumber = (num: number | string): string => {
  const persianDigits = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹']
  return num.toString().split('').map(char => {
    const digit = parseInt(char)
    return !isNaN(digit) ? persianDigits[digit] : char
  }).join('')
}

const handleServiceSelected = (service: any) => {
  bookingData.value.serviceId = service.id
  bookingData.value.serviceName = service.name
  bookingData.value.servicePrice = service.basePrice
  bookingData.value.serviceDuration = service.duration
}

const handleSlotSelected = (slot: any) => {
  bookingData.value.date = slot.date
  bookingData.value.startTime = slot.startTime
  bookingData.value.endTime = slot.endTime
  bookingData.value.staffId = slot.staffId
  bookingData.value.staffName = slot.staffName
}

const handleCustomerInfoUpdated = (info: any) => {
  bookingData.value.customerInfo = { ...info }
}

const nextStep = () => {
  if (canProceed.value && currentStep.value < 4) {
    currentStep.value++
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }
}

const previousStep = () => {
  if (currentStep.value > 1) {
    currentStep.value--
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }
}

const reviewBooking = () => {
  if (canProceed.value) {
    currentStep.value = 4
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }
}

const submitBooking = async () => {
  isSubmitting.value = true

  try {
    // TODO: Call actual API
    // const response = await bookingService.createBooking({
    //   providerId: providerId.value,
    //   serviceId: bookingData.value.serviceId,
    //   startTime: `${bookingData.value.date}T${bookingData.value.startTime}`,
    //   staffId: bookingData.value.staffId,
    //   customerNotes: bookingData.value.customerInfo.notes,
    // })

    // Mock success
    await new Promise(resolve => setTimeout(resolve, 2000))
    bookingId.value = 'BK' + Math.random().toString(36).substring(2, 9).toUpperCase()

    showSuccessModal.value = true
  } catch (error) {
    console.error('Booking failed:', error)
    alert('خطا در ثبت رزرو. لطفاً دوباره تلاش کنید.')
  } finally {
    isSubmitting.value = false
  }
}

const closeSuccessModal = () => {
  showSuccessModal.value = false
  router.push('/')
}

const goToMyBookings = () => {
  showSuccessModal.value = false
  router.push('/customer/my-bookings')
}
</script>

<style scoped>
.booking-wizard {
  min-height: 100vh;
  background: linear-gradient(180deg, #f8fafc 0%, #ffffff 100%);
  padding: 2rem 0 4rem;
}

.wizard-container {
  max-width: 1000px;
  margin: 0 auto;
  padding: 0 2rem;
}

.wizard-header {
  margin-bottom: 3rem;
}

.steps-indicator {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  position: relative;
}

.step-item {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  position: relative;
}

.step-circle {
  width: 56px;
  height: 56px;
  border-radius: 50%;
  background: white;
  border: 3px solid #e2e8f0;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.25rem;
  font-weight: 700;
  color: #94a3b8;
  transition: all 0.3s;
  position: relative;
  z-index: 2;
}

.step-item.active .step-circle {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  border-color: #667eea;
  color: white;
  box-shadow: 0 4px 16px rgba(102, 126, 234, 0.4);
}

.step-item.completed .step-circle {
  background: #10b981;
  border-color: #10b981;
  color: white;
}

.step-circle svg {
  width: 24px;
  height: 24px;
}

.step-info {
  text-align: center;
  margin-top: 1rem;
}

.step-label {
  font-size: 1rem;
  font-weight: 700;
  color: #1e293b;
  margin-bottom: 0.25rem;
}

.step-item.active .step-label {
  color: #667eea;
}

.step-description {
  font-size: 0.875rem;
  color: #64748b;
}

.step-connector {
  position: absolute;
  top: 28px;
  right: calc(50% + 28px);
  left: calc(-50% + 28px);
  height: 3px;
  background: #e2e8f0;
  z-index: 1;
}

.step-item.completed .step-connector {
  background: #10b981;
}

.wizard-content {
  background: white;
  border-radius: 24px;
  padding: 2.5rem;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.06);
  min-height: 500px;
  margin-bottom: 2rem;
}

.wizard-footer {
  display: flex;
  gap: 1rem;
  justify-content: space-between;
}

.spacer {
  flex: 1;
}

.btn-primary,
.btn-secondary {
  display: flex;
  align-items: center;
  gap: 0.625rem;
  padding: 1rem 2rem;
  border: none;
  border-radius: 12px;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s;
}

.btn-primary {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  box-shadow: 0 4px 12px rgba(102, 126, 234, 0.3);
}

.btn-primary:hover:not(:disabled) {
  transform: translateY(-2px);
  box-shadow: 0 6px 20px rgba(102, 126, 234, 0.4);
}

.btn-primary:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.btn-secondary {
  background: white;
  color: #475569;
  border: 2px solid #e2e8f0;
}

.btn-secondary:hover {
  background: #f8fafc;
  border-color: #cbd5e1;
}

.btn-primary svg,
.btn-secondary svg {
  width: 20px;
  height: 20px;
}

.loading-text {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.spinner {
  width: 20px;
  height: 20px;
  border: 3px solid rgba(255, 255, 255, 0.3);
  border-top-color: white;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

/* Success Modal */
.success-modal {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.7);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 9999;
  padding: 2rem;
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

.modal-content {
  background: white;
  border-radius: 24px;
  padding: 3rem;
  max-width: 500px;
  text-align: center;
  animation: slideUp 0.4s ease-out;
}

@keyframes slideUp {
  from {
    opacity: 0;
    transform: translateY(40px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.success-icon {
  width: 96px;
  height: 96px;
  margin: 0 auto 1.5rem;
  background: linear-gradient(135deg, #10b981 0%, #059669 100%);
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
}

.success-icon svg {
  width: 56px;
  height: 56px;
  color: white;
}

.modal-content h2 {
  font-size: 1.75rem;
  font-weight: 800;
  color: #1e293b;
  margin: 0 0 1rem 0;
}

.modal-content p {
  font-size: 1.05rem;
  color: #64748b;
  margin: 0.5rem 0;
}

.confirmation-text {
  margin-top: 1.5rem;
  padding-top: 1.5rem;
  border-top: 2px solid #f1f5f9;
}

.modal-actions {
  display: flex;
  gap: 1rem;
  margin-top: 2rem;
}

.modal-actions .btn-primary,
.modal-actions .btn-secondary {
  flex: 1;
}

/* Responsive */
@media (max-width: 768px) {
  .wizard-container {
    padding: 0 1rem;
  }

  .steps-indicator {
    flex-direction: column;
    align-items: stretch;
  }

  .step-item {
    flex-direction: row;
    align-items: center;
    margin-bottom: 1rem;
  }

  .step-circle {
    width: 48px;
    height: 48px;
    font-size: 1.125rem;
  }

  .step-info {
    text-align: right;
    margin-top: 0;
    margin-right: 1rem;
    flex: 1;
  }

  .step-connector {
    display: none;
  }

  .wizard-content {
    padding: 1.75rem;
  }

  .wizard-footer {
    flex-direction: column-reverse;
  }

  .wizard-footer .btn-primary,
  .wizard-footer .btn-secondary {
    width: 100%;
    justify-content: center;
  }

  .modal-content {
    padding: 2rem;
  }

  .modal-actions {
    flex-direction: column;
  }
}
</style>
