<template>
  <div class="booking-confirmation" dir="rtl">
    <div class="confirmation-header">
      <div class="success-icon">
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
          <path fill-rule="evenodd" d="M2.25 12c0-5.385 4.365-9.75 9.75-9.75s9.75 4.365 9.75 9.75-4.365 9.75-9.75 9.75S2.25 17.385 2.25 12zm13.36-1.814a.75.75 0 10-1.22-.872l-3.236 4.53L9.53 12.22a.75.75 0 00-1.06 1.06l2.25 2.25a.75.75 0 001.14-.094l3.75-5.25z" clip-rule="evenodd" />
        </svg>
      </div>
      <h2 class="confirmation-title">تایید اطلاعات رزرو</h2>
      <p class="confirmation-description">لطفاً اطلاعات رزرو خود را بررسی کنید</p>
    </div>

    <div class="confirmation-content">
      <!-- Service Information -->
      <div class="info-section">
        <h3 class="section-title">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 13.255A23.931 23.931 0 0112 15c-3.183 0-6.22-.62-9-1.745M16 6V4a2 2 0 00-2-2h-4a2 2 0 00-2 2v2m4 6h.01M5 20h14a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
          </svg>
          خدمت
        </h3>
        <div class="info-card">
          <div class="info-item">
            <span class="info-label">نام خدمت:</span>
            <span class="info-value">{{ bookingData.serviceName }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">مدت زمان:</span>
            <span class="info-value">{{ convertToPersianNumber(bookingData.serviceDuration) }} دقیقه</span>
          </div>
          <div class="info-item">
            <span class="info-label">هزینه:</span>
            <span class="info-value price">{{ convertToPersianNumber(bookingData.servicePrice) }} تومان</span>
          </div>
        </div>
      </div>

      <!-- Staff Information (if available) -->
      <div v-if="bookingData.staffName" class="info-section">
        <h3 class="section-title">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
          </svg>
          کارمند ارائه‌دهنده خدمت
        </h3>
        <div class="info-card staff-card">
          <div class="staff-avatar">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
              <path fill-rule="evenodd" d="M7.5 6a4.5 4.5 0 119 0 4.5 4.5 0 01-9 0zM3.751 20.105a8.25 8.25 0 0116.498 0 .75.75 0 01-.437.695A18.683 18.683 0 0112 22.5c-2.786 0-5.433-.608-7.812-1.7a.75.75 0 01-.437-.695z" clip-rule="evenodd" />
            </svg>
          </div>
          <div class="staff-info">
            <div class="staff-name">{{ bookingData.staffName }}</div>
            <div class="staff-label">متخصص ارائه خدمت</div>
          </div>
        </div>
      </div>

      <!-- Date & Time Information -->
      <div class="info-section">
        <h3 class="section-title">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
          </svg>
          تاریخ و زمان
        </h3>
        <div class="info-card">
          <div class="info-item">
            <span class="info-label">تاریخ:</span>
            <span class="info-value">{{ formatDate(bookingData.date) }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">ساعت شروع:</span>
            <span class="info-value">{{ convertToPersianTime(bookingData.startTime) }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">ساعت پایان:</span>
            <span class="info-value">{{ convertToPersianTime(bookingData.endTime) }}</span>
          </div>
        </div>
      </div>

      <!-- Customer Information -->
      <div class="info-section">
        <h3 class="section-title">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5.121 17.804A13.937 13.937 0 0112 16c2.5 0 4.847.655 6.879 1.804M15 10a3 3 0 11-6 0 3 3 0 016 0zm6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          اطلاعات تماس
        </h3>
        <div class="info-card">
          <div class="info-item">
            <span class="info-label">نام و نام خانوادگی:</span>
            <span class="info-value">{{ bookingData.customerInfo.fullName }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">شماره تماس:</span>
            <span class="info-value">{{ convertToPersianNumber(bookingData.customerInfo.phoneNumber) }}</span>
          </div>
          <div v-if="bookingData.customerInfo.email" class="info-item">
            <span class="info-label">ایمیل:</span>
            <span class="info-value">{{ bookingData.customerInfo.email }}</span>
          </div>
          <div v-if="bookingData.customerInfo.notes" class="info-item full-width">
            <span class="info-label">یادداشت:</span>
            <span class="info-value notes">{{ bookingData.customerInfo.notes }}</span>
          </div>
        </div>
      </div>
    </div>

    <!-- Important Notice -->
    <div class="notice-box">
      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
        <path fill-rule="evenodd" d="M2.25 12c0-5.385 4.365-9.75 9.75-9.75s9.75 4.365 9.75 9.75-4.365 9.75-9.75 9.75S2.25 17.385 2.25 12zM12 8.25a.75.75 0 01.75.75v3.75a.75.75 0 01-1.5 0V9a.75.75 0 01.75-.75zm0 8.25a.75.75 0 100-1.5.75.75 0 000 1.5z" clip-rule="evenodd" />
      </svg>
      <div class="notice-content">
        <strong>توجه:</strong> پس از تایید رزرو، اطلاعات کامل به ایمیل و شماره تماس شما ارسال خواهد شد.
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
interface CustomerInfo {
  fullName: string
  phoneNumber: string
  email: string
  notes: string
}

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
  customerInfo: CustomerInfo
}

interface Props {
  bookingData: BookingData
  providerId: string
}

defineProps<Props>()

const convertToPersianNumber = (value: number | string): string => {
  const persianDigits = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹']
  return value.toString().split('').map(char => {
    const digit = parseInt(char)
    return !isNaN(digit) ? persianDigits[digit] : char
  }).join('')
}

const convertToPersianTime = (time: string | null): string => {
  if (!time) return ''
  return convertToPersianNumber(time)
}

const formatDate = (dateString: string | null): string => {
  if (!dateString) return ''

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
  const persianMonth = convertToPersianNumber(jalaliDate[1])
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
</script>

<style scoped lang="scss">
.booking-confirmation {
  padding: 0;
}

.confirmation-header {
  text-align: center;
  margin-bottom: 3rem;
}

.success-icon {
  width: 80px;
  height: 80px;
  margin: 0 auto 1.5rem;
  background: linear-gradient(135deg, #10b981 0%, #059669 100%);
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 8px 24px rgba(16, 185, 129, 0.3);

  svg {
    width: 48px;
    height: 48px;
    color: white;
  }
}

.confirmation-title {
  font-size: 2rem;
  font-weight: 800;
  color: #1e293b;
  margin: 0 0 0.75rem 0;
}

.confirmation-description {
  font-size: 1.05rem;
  color: #64748b;
  margin: 0;
}

.confirmation-content {
  display: flex;
  flex-direction: column;
  gap: 2rem;
}

.info-section {
  .section-title {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    font-size: 1.25rem;
    font-weight: 700;
    color: #1e293b;
    margin: 0 0 1rem 0;

    svg {
      width: 24px;
      height: 24px;
      color: #667eea;
    }
  }

  .info-card {
    background: white;
    border-radius: 16px;
    padding: 1.5rem;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);
    border: 1px solid #e2e8f0;
  }
}

.info-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0.75rem 0;

  &:not(:last-child) {
    border-bottom: 1px solid #f1f5f9;
  }

  &.full-width {
    flex-direction: column;
    align-items: flex-start;
    gap: 0.5rem;
  }

  .info-label {
    font-size: 0.95rem;
    color: #64748b;
    font-weight: 500;
  }

  .info-value {
    font-size: 1rem;
    color: #1e293b;
    font-weight: 600;

    &.price {
      color: #10b981;
      font-size: 1.125rem;
    }

    &.notes {
      color: #475569;
      font-weight: 400;
      line-height: 1.6;
    }
  }
}

.staff-card {
  display: flex;
  align-items: center;
  gap: 1rem;

  .staff-avatar {
    width: 56px;
    height: 56px;
    border-radius: 50%;
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;

    svg {
      width: 32px;
      height: 32px;
      color: white;
    }
  }

  .staff-info {
    flex: 1;

    .staff-name {
      font-size: 1.125rem;
      font-weight: 700;
      color: #1e293b;
      margin-bottom: 0.25rem;
    }

    .staff-label {
      font-size: 0.875rem;
      color: #64748b;
    }
  }
}

.notice-box {
  display: flex;
  align-items: flex-start;
  gap: 1rem;
  padding: 1.25rem;
  background: linear-gradient(135deg, #fef3c7 0%, #fde68a 100%);
  border: 2px solid #fbbf24;
  border-radius: 12px;
  margin-top: 1rem;

  svg {
    width: 24px;
    height: 24px;
    color: #d97706;
    flex-shrink: 0;
    margin-top: 0.125rem;
  }

  .notice-content {
    font-size: 0.95rem;
    color: #78350f;
    line-height: 1.6;

    strong {
      font-weight: 700;
    }
  }
}

@media (max-width: 768px) {
  .confirmation-title {
    font-size: 1.5rem;
  }

  .info-section {
    .section-title {
      font-size: 1.125rem;
    }

    .info-card {
      padding: 1.25rem;
    }
  }

  .info-item {
    flex-direction: column;
    align-items: flex-start;
    gap: 0.375rem;
  }
}
</style>
