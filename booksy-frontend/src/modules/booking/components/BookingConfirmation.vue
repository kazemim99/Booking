<template>
  <div class="booking-confirmation" dir="rtl">
    <div class="step-header">
      <h2 class="step-title">بررسی و تایید نهایی</h2>
      <p class="step-description">
        لطفاً اطلاعات رزرو خود را بررسی کنید
      </p>
    </div>

    <div class="confirmation-content">
      <!-- Provider Info -->
      <div class="summary-card provider-card">
        <div class="card-header">
          <h3>اطلاعات ارائه‌دهنده</h3>
        </div>
        <div class="card-body">
          <div v-if="provider" class="provider-info">
            <div class="provider-logo">
              <img
                v-if="provider.profile.logoUrl"
                :src="provider.profile.logoUrl"
                :alt="provider.profile.businessName"
              />
              <div v-else class="logo-placeholder">
                {{ getInitials(provider.profile.businessName) }}
              </div>
            </div>
            <div class="provider-details">
              <h4>{{ provider.profile.businessName }}</h4>
              <p class="address">{{ provider.address.city }}، {{ provider.address.state }}</p>
            </div>
          </div>
        </div>
      </div>

      <!-- Booking Details -->
      <div class="summary-card booking-details-card">
        <div class="card-header">
          <h3>جزئیات رزرو</h3>
        </div>
        <div class="card-body">
          <div class="detail-row">
            <div class="detail-label">
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
              </svg>
              خدمت
            </div>
            <div class="detail-value">{{ bookingData.serviceName || 'انتخاب نشده' }}</div>
          </div>

          <div class="detail-row">
            <div class="detail-label">
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
              </svg>
              تاریخ
            </div>
            <div class="detail-value">{{ formatDate(bookingData.date) }}</div>
          </div>

          <div class="detail-row">
            <div class="detail-label">
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              ساعت
            </div>
            <div class="detail-value">{{ convertToPersianTime(bookingData.startTime) }}</div>
          </div>

          <div class="detail-row">
            <div class="detail-label">
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
              </svg>
              کارشناس
            </div>
            <div class="detail-value">{{ bookingData.staffName || 'انتخاب نشده' }}</div>
          </div>

          <div class="detail-row">
            <div class="detail-label">
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              مدت زمان
            </div>
            <div class="detail-value">{{ convertToPersianNumber(bookingData.serviceDuration) }} دقیقه</div>
          </div>
        </div>
      </div>

      <!-- Customer Info -->
      <div class="summary-card customer-card">
        <div class="card-header">
          <h3>اطلاعات مشتری</h3>
        </div>
        <div class="card-body">
          <div class="detail-row">
            <div class="detail-label">
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
              </svg>
              نام و نام خانوادگی
            </div>
            <div class="detail-value">{{ bookingData.customerInfo.firstName }} {{ bookingData.customerInfo.lastName }}</div>
          </div>

          <div class="detail-row">
            <div class="detail-label">
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 5a2 2 0 012-2h3.28a1 1 0 01.948.684l1.498 4.493a1 1 0 01-.502 1.21l-2.257 1.13a11.042 11.042 0 005.516 5.516l1.13-2.257a1 1 0 011.21-.502l4.493 1.498a1 1 0 01.684.949V19a2 2 0 01-2 2h-1C9.716 21 3 14.284 3 6V5z" />
              </svg>
              تلفن
            </div>
            <div class="detail-value" dir="ltr">{{ bookingData.customerInfo.phoneNumber }}</div>
          </div>

          <div v-if="bookingData.customerInfo.email" class="detail-row">
            <div class="detail-label">
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
              </svg>
              ایمیل
            </div>
            <div class="detail-value" dir="ltr">{{ bookingData.customerInfo.email }}</div>
          </div>

          <div class="detail-row notes-row full-width">
            <div class="detail-label">
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 8h10M7 12h4m1 8l-4-4H5a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-3l-4 4z" />
              </svg>
              یادداشت (اختیاری)
            </div>
            <textarea
              v-model="localNotes"
              class="notes-input"
              placeholder="یادداشت یا توضیحات اضافی برای ارائه‌دهنده..."
              rows="3"
              maxlength="500"
              @input="emitNotesUpdate"
            ></textarea>
            <div class="character-count">{{ localNotes.length }}/500 کاراکتر</div>
          </div>
        </div>
      </div>

      <!-- Price Summary -->
      <div class="summary-card price-card">
        <div class="card-header">
          <h3>مبلغ قابل پرداخت</h3>
        </div>
        <div class="card-body">
          <div class="price-row">
            <span>هزینه خدمت</span>
            <span>{{ formatPrice(bookingData.servicePrice) }} تومان</span>
          </div>
          <div class="price-row subtotal">
            <span>مالیات (۹٪)</span>
            <span>{{ formatPrice(Math.round(bookingData.servicePrice * 0.09)) }} تومان</span>
          </div>
          <div class="price-row total">
            <span>جمع کل</span>
            <span>{{ formatPrice(Math.round(bookingData.servicePrice * 1.09)) }} تومان</span>
          </div>
        </div>
      </div>

      <!-- Important Notice -->
      <div class="notice-card">
        <div class="notice-icon">
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
            <path fill-rule="evenodd" d="M2.25 12c0-5.385 4.365-9.75 9.75-9.75s9.75 4.365 9.75 9.75-4.365 9.75-9.75 9.75S2.25 17.385 2.25 12zm8.706-1.442c1.146-.573 2.437.463 2.126 1.706l-.709 2.836.042-.02a.75.75 0 01.67 1.34l-.04.022c-1.147.573-2.438-.463-2.127-1.706l.71-2.836-.042.02a.75.75 0 11-.671-1.34l.041-.022zM12 9a.75.75 0 100-1.5.75.75 0 000 1.5z" clip-rule="evenodd" />
          </svg>
        </div>
        <div class="notice-content">
          <h4>نکات مهم</h4>
          <ul>
            <li>لطفاً ۱۰ دقیقه زودتر از وقت رزرو حاضر شوید</li>
            <li>در صورت تأخیر بیش از ۱۵ دقیقه، رزرو شما لغو خواهد شد</li>
            <li>برای لغو رزرو، حداقل ۲۴ ساعت قبل اطلاع دهید</li>
          </ul>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { useProviderStore } from '@/modules/provider/stores/provider.store'
import type { Provider } from '@/modules/provider/types/provider.types'

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
    firstName: string
    lastName: string
    phoneNumber: string
    email: string
    notes: string
  }
}

interface Props {
  bookingData: BookingData
  providerId: string
}

const props = defineProps<Props>()

const emit = defineEmits<{
  (e: 'notes-updated', notes: string): void
}>()

const providerStore = useProviderStore()
const provider = ref<Provider | null>(null)
const localNotes = ref('')

// Initialize notes from bookingData
watch(() => props.bookingData.customerInfo.notes, (newNotes) => {
  localNotes.value = newNotes || ''
}, { immediate: true })

const emitNotesUpdate = () => {
  emit('notes-updated', localNotes.value)
}

// Lifecycle
onMounted(async () => {
  if (props.providerId) {
    await providerStore.getProviderById(props.providerId, false, false)
    provider.value = providerStore.currentProvider
  }
})

// Methods
const getInitials = (name: string): string => {
  return name
    .split(' ')
    .map((word) => word[0])
    .join('')
    .toUpperCase()
    .slice(0, 2)
}

const formatDate = (dateString: string | null): string => {
  if (!dateString) return 'انتخاب نشده'

  const date = new Date(dateString)

  // Use Persian locale to format date
  const options: Intl.DateTimeFormatOptions = {
    weekday: 'long',
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    calendar: 'persian'
  }

  try {
    const formatter = new Intl.DateTimeFormat('fa-IR', options)
    return formatter.format(date)
  } catch (_error) {
    // Fallback to simple format if Intl fails
    const weekDays = ['یکشنبه', 'دوشنبه', 'سه‌شنبه', 'چهارشنبه', 'پنج‌شنبه', 'جمعه', 'شنبه']
    const dayName = weekDays[date.getDay()]
    return `${dayName}، ${convertToPersianNumber(date.getDate())} ${convertToPersianNumber(date.getMonth() + 1)} ${convertToPersianNumber(date.getFullYear())}`
  }
}

const convertToPersianTime = (time: string | null): string => {
  if (!time) return 'انتخاب نشده'

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

const formatPrice = (price: number): string => {
  const formatted = price.toLocaleString('fa-IR')
  const persianDigits = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹']
  return formatted.split('').map(char => {
    const digit = parseInt(char)
    return !isNaN(digit) ? persianDigits[digit] : char
  }).join('')
}
</script>

<style scoped>
.booking-confirmation {
  padding: 0;
}

.step-header {
  text-align: center;
  margin-bottom: 3rem;
}

.step-title {
  font-size: 2rem;
  font-weight: 800;
  color: #1e293b;
  margin: 0 0 0.75rem 0;
}

.step-description {
  font-size: 1.05rem;
  color: #64748b;
  margin: 0;
}

.confirmation-content {
  display: grid;
  gap: 1.5rem;
}

.summary-card {
  background: white;
  border: 2px solid #e2e8f0;
  border-radius: 20px;
  overflow: hidden;
}

.card-header {
  background: linear-gradient(135deg, rgba(102, 126, 234, 0.1) 0%, rgba(118, 75, 162, 0.1) 100%);
  padding: 1.25rem 1.5rem;
  border-bottom: 2px solid #e2e8f0;
}

.card-header h3 {
  font-size: 1.125rem;
  font-weight: 700;
  color: #1e293b;
  margin: 0;
}

.card-body {
  padding: 1.5rem;
}

.provider-info {
  display: flex;
  gap: 1.25rem;
  align-items: center;
}

.provider-logo {
  width: 64px;
  height: 64px;
  border-radius: 12px;
  overflow: hidden;
  flex-shrink: 0;
}

.provider-logo img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.logo-placeholder {
  width: 100%;
  height: 100%;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
  font-size: 1.5rem;
  font-weight: 700;
}

.provider-details h4 {
  font-size: 1.25rem;
  font-weight: 700;
  color: #1e293b;
  margin: 0 0 0.375rem 0;
}

.provider-details .address {
  font-size: 0.95rem;
  color: #64748b;
  margin: 0;
}

.detail-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1rem 0;
  border-bottom: 1px solid #f1f5f9;
}

.detail-row:last-child {
  border-bottom: none;
  padding-bottom: 0;
}

.detail-row:first-child {
  padding-top: 0;
}

.detail-label {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.95rem;
  color: #64748b;
  font-weight: 600;
}

.detail-label svg {
  width: 18px;
  height: 18px;
  color: #94a3b8;
}

.detail-value {
  font-size: 1rem;
  color: #1e293b;
  font-weight: 600;
}

.notes-row {
  flex-direction: column;
  align-items: flex-start;
  gap: 0.75rem;
}

.notes-row.full-width {
  width: 100%;
}

.notes-input {
  width: 100%;
  padding: 1rem;
  background: #f8fafc;
  border: 2px solid #e2e8f0;
  border-radius: 12px;
  font-size: 0.95rem;
  font-family: inherit;
  line-height: 1.6;
  color: #475569;
  resize: vertical;
  transition: all 0.3s;
}

.notes-input:focus {
  outline: none;
  border-color: #667eea;
  background: white;
  box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
}

.notes-input::placeholder {
  color: #94a3b8;
}

.character-count {
  align-self: flex-end;
  font-size: 0.75rem;
  color: #94a3b8;
  margin-top: -0.5rem;
}

.notes-value {
  width: 100%;
  padding: 1rem;
  background: #f8fafc;
  border-radius: 12px;
  line-height: 1.6;
  color: #475569;
}

.price-row {
  display: flex;
  justify-content: space-between;
  padding: 0.875rem 0;
  font-size: 1rem;
  color: #475569;
}

.price-row.subtotal {
  border-top: 1px solid #f1f5f9;
}

.price-row.total {
  border-top: 2px solid #e2e8f0;
  padding: 1.25rem 0 0;
  margin-top: 0.5rem;
  font-size: 1.25rem;
  font-weight: 700;
  color: #1e293b;
}

.notice-card {
  display: flex;
  gap: 1.25rem;
  padding: 1.75rem;
  background: linear-gradient(135deg, rgba(59, 130, 246, 0.1) 0%, rgba(37, 99, 235, 0.1) 100%);
  border-radius: 20px;
  border: 2px solid rgba(59, 130, 246, 0.2);
}

.notice-icon {
  width: 48px;
  height: 48px;
  flex-shrink: 0;
  background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%);
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.notice-icon svg {
  width: 28px;
  height: 28px;
  color: white;
}

.notice-content h4 {
  font-size: 1.125rem;
  font-weight: 700;
  color: #1e293b;
  margin: 0 0 0.75rem 0;
}

.notice-content ul {
  margin: 0;
  padding: 0 0 0 1.5rem;
  list-style: disc;
}

.notice-content li {
  font-size: 0.95rem;
  color: #475569;
  line-height: 1.8;
  margin-bottom: 0.375rem;
}

.notice-content li:last-child {
  margin-bottom: 0;
}

/* Responsive */
@media (max-width: 768px) {
  .step-title {
    font-size: 1.75rem;
  }

  .provider-info {
    flex-direction: column;
    text-align: center;
  }

  .detail-row {
    flex-direction: column;
    align-items: flex-start;
    gap: 0.5rem;
  }

  .detail-value {
    width: 100%;
    text-align: right;
  }

  .notice-card {
    flex-direction: column;
  }
}
</style>
