<template>
  <section class="profile-about" dir="rtl">
    <!-- About Section -->
    <div class="about-section">
      <div class="section-header">
        <h2 class="section-title">درباره {{ provider.profile.businessName }}</h2>
      </div>

      <div class="about-content">
        <p class="about-text">
          {{ provider.profile.description || 'توضیحاتی برای این کسب‌وکار ثبت نشده است.' }}
        </p>

        <div v-if="provider.tags && provider.tags.length > 0" class="tags-container">
          <h3 class="tags-title">تخصص‌ها</h3>
          <div class="tags-list">
            <span v-for="tag in provider.tags" :key="tag" class="tag">
              {{ tag }}
            </span>
          </div>
        </div>
      </div>
    </div>

    <!-- Organization Info Section (shown for organizations with staff) -->
    <div v-if="hasStaffMembers" class="organization-section">
      <div class="section-header">
        <h2 class="section-title">درباره این مجموعه</h2>
        <div class="org-badge">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
          </svg>
          <span>{{ getProviderCategoryLabel(provider.primaryCategory || provider.type) }}</span>
        </div>
      </div>

      <div class="organization-content">
        <!-- Organization Stats -->
        <div class="org-stats-grid">
          <div class="org-stat-card">
            <div class="stat-icon staff">
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
              </svg>
            </div>
            <div class="stat-info">
              <div class="stat-value">{{ convertToPersianNumber(activeStaffCount) }}</div>
              <div class="stat-label">متخصص فعال</div>
            </div>
          </div>

          <div class="org-stat-card">
            <div class="stat-icon services">
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4" />
              </svg>
            </div>
            <div class="stat-info">
              <div class="stat-value">{{ convertToPersianNumber(totalServices) }}</div>
              <div class="stat-label">خدمات ارائه شده</div>
            </div>
          </div>

          <div class="org-stat-card">
            <div class="stat-icon calendar">
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
              </svg>
            </div>
            <div class="stat-info">
              <div class="stat-value">{{ yearsActive }}</div>
              <div class="stat-label">سال فعالیت</div>
            </div>
          </div>

          <div v-if="provider.verifiedAt" class="org-stat-card verified">
            <div class="stat-icon verified">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                <path fill-rule="evenodd" d="M8.603 3.799A4.49 4.49 0 0112 2.25c1.357 0 2.573.6 3.397 1.549a4.49 4.49 0 013.498 1.307 4.491 4.491 0 011.307 3.497A4.49 4.49 0 0121.75 12a4.49 4.49 0 01-1.549 3.397 4.491 4.491 0 01-1.307 3.497 4.491 4.491 0 01-3.497 1.307A4.49 4.49 0 0112 21.75a4.49 4.49 0 01-3.397-1.549 4.49 4.49 0 01-3.498-1.306 4.491 4.491 0 01-1.307-3.498A4.49 4.49 0 012.25 12c0-1.357.6-2.573 1.549-3.397a4.49 4.49 0 011.307-3.497 4.49 4.49 0 013.497-1.307zm7.007 6.387a.75.75 0 10-1.22-.872l-3.236 4.53L9.53 12.22a.75.75 0 00-1.06 1.06l2.25 2.25a.75.75 0 001.14-.094l3.75-5.25z" clip-rule="evenodd" />
              </svg>
            </div>
            <div class="stat-info">
              <div class="stat-value">تایید شده</div>
              <div class="stat-label">هویت تایید شده</div>
            </div>
          </div>
        </div>

        <!-- Organization Features -->
        <div class="org-features">
          <h3 class="features-title">امکانات و ویژگی‌ها</h3>
          <div class="features-list">
            <div v-if="provider.allowOnlineBooking" class="feature-item">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd" />
              </svg>
              <span>امکان رزرو آنلاین</span>
            </div>
            <div v-if="provider.offersMobileServices" class="feature-item">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd" />
              </svg>
              <span>خدمات سیار</span>
            </div>
            <div v-if="hasStaffMembers" class="feature-item">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd" />
              </svg>
              <span>تیم متخصص با تجربه</span>
            </div>
            <div v-if="provider.requiresApproval" class="feature-item">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd" />
              </svg>
              <span>رزرو با تایید مدیر</span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Business Hours Section -->
    <div class="hours-section">
      <div class="section-header">
        <h2 class="section-title">ساعات کاری</h2>
        <div class="current-status" :class="{ open: isCurrentlyOpen }">
          <div class="status-dot"></div>
          <span>{{ isCurrentlyOpen ? 'اکنون باز است' : 'اکنون بسته است' }}</span>
        </div>
      </div>

      <div class="hours-list">
        <div
          v-for="hours in sortedBusinessHours"
          :key="hours.id"
          class="hours-item"
          :class="{ today: isToday(hours.dayOfWeek), closed: !hours.isOpen }"
        >
          <div class="day-name">
            <span class="day-label">{{ getDayName(hours.dayOfWeek) }}</span>
            <span v-if="isToday(hours.dayOfWeek)" class="today-badge">امروز</span>
          </div>
          <div class="time-range">
            <span v-if="hours.isOpen" class="time">
              {{ convertToPersianTime(hours.openTime) }} - {{ convertToPersianTime(hours.closeTime) }}
            </span>
            <span v-else class="closed-label">تعطیل</span>
          </div>
        </div>
      </div>
    </div>

    <!-- Contact Information -->
    <div class="contact-section">
      <div class="section-header">
        <h2 class="section-title">اطلاعات تماس</h2>
      </div>

      <div class="contact-grid">
        <div v-if="provider.contactInfo.email" class="contact-card">
          <div class="contact-icon email">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
            </svg>
          </div>
          <div class="contact-details">
            <div class="contact-label">ایمیل</div>
            <a :href="`mailto:${provider.contactInfo.email}`" class="contact-value">
              {{ provider.contactInfo.email }}
            </a>
          </div>
        </div>

        <div v-if="provider.contactInfo.phone" class="contact-card">
          <div class="contact-icon phone">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 5a2 2 0 012-2h3.28a1 1 0 01.948.684l1.498 4.493a1 1 0 01-.502 1.21l-2.257 1.13a11.042 11.042 0 005.516 5.516l1.13-2.257a1 1 0 011.21-.502l4.493 1.498a1 1 0 01.684.949V19a2 2 0 01-2 2h-1C9.716 21 3 14.284 3 6V5z" />
            </svg>
          </div>
          <div class="contact-details">
            <div class="contact-label">تلفن</div>
            <a :href="`tel:${provider.contactInfo.phone}`" class="contact-value">
              {{ convertToPersianNumber(provider.contactInfo.phone) }}
            </a>
          </div>
        </div>

        <div v-if="provider.contactInfo.secondaryPhone" class="contact-card">
          <div class="contact-icon phone">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 18h.01M8 21h8a2 2 0 002-2V5a2 2 0 00-2-2H8a2 2 0 00-2 2v14a2 2 0 002 2z" />
            </svg>
          </div>
          <div class="contact-details">
            <div class="contact-label">تلفن دوم</div>
            <a :href="`tel:${provider.contactInfo.secondaryPhone}`" class="contact-value">
              {{ convertToPersianNumber(provider.contactInfo.secondaryPhone) }}
            </a>
          </div>
        </div>

        <div v-if="provider.profile.websiteUrl" class="contact-card">
          <div class="contact-icon website">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 12a9 9 0 01-9 9m9-9a9 9 0 00-9-9m9 9H3m9 9a9 9 0 01-9-9m9 9c1.657 0 3-4.03 3-9s-1.343-9-3-9m0 18c-1.657 0-3-4.03-3-9s1.343-9 3-9m-9 9a9 9 0 019-9" />
            </svg>
          </div>
          <div class="contact-details">
            <div class="contact-label">وب‌سایت</div>
            <a :href="provider.profile.websiteUrl" target="_blank" rel="noopener" class="contact-value">
              مشاهده وب‌سایت
            </a>
          </div>
        </div>
      </div>
    </div>

    <!-- Location Section -->
    <div class="location-section">
      <div class="section-header">
        <h2 class="section-title">آدرس و موقعیت</h2>
      </div>

      <div class="location-content">
        <div class="address-card">
          <div class="address-icon">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
            </svg>
          </div>
          <div class="address-details">
            <p class="address-text">
              {{ provider.address.addressLine1 }}<br />
              <span v-if="provider.address.addressLine2">{{ provider.address.addressLine2 }}<br /></span>
              {{ provider.address.city }}، {{ provider.address.state }}<br />
              کد پستی: {{ convertToPersianNumber(provider.address.postalCode) }}
            </p>
            <button class="btn-get-directions" @click="getDirections">
              مسیریابی
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 20l-5.447-2.724A1 1 0 013 16.382V5.618a1 1 0 011.447-.894L9 7m0 13l6-3m-6 3V7m6 10l4.553 2.276A1 1 0 0021 18.382V7.618a1 1 0 00-.553-.894L15 4m0 13V4m0 0L9 7" />
              </svg>
            </button>
          </div>
        </div>

        <!-- Map Placeholder (integrate with Neshan Maps later) -->
        <div class="map-container">
          <div class="map-placeholder">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 20l-5.447-2.724A1 1 0 013 16.382V5.618a1 1 0 011.447-.894L9 7m0 13l6-3m-6 3V7m6 10l4.553 2.276A1 1 0 0021 18.382V7.618a1 1 0 00-.553-.894L15 4m0 13V4m0 0L9 7" />
            </svg>
            <p>نقشه به زودی افزوده خواهد شد</p>
          </div>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import type { Provider, DayOfWeek, BusinessHours } from '@/modules/provider/types/provider.types'
import { ProviderCategory } from '@/core/types/enums.types'
import { getCategoryPersianName } from '@/core/constants/provider-categories'

interface Props {
  provider: Provider
}

const props = defineProps<Props>()

// Computed
const sortedBusinessHours = computed(() => {
  if (!props.provider.businessHours || props.provider.businessHours.length === 0) {
    // Return default hours if none exist
    return getDefaultBusinessHours()
  }
  return [...props.provider.businessHours].sort((a, b) => a.dayOfWeek - b.dayOfWeek)
})

const isCurrentlyOpen = computed(() => {
  // TODO: Implement actual logic to check if business is currently open
  const now = new Date()
  const currentDay = (now.getDay() + 6) % 7 // Convert Sunday=0 to Monday=0
  const currentHours = sortedBusinessHours.value.find(h => h.dayOfWeek === currentDay)

  if (!currentHours || !currentHours.isOpen) {
    return false
  }

  // Simple check - can be enhanced later
  return true
})

// Organization computed properties
const hasStaffMembers = computed(() => {
  return props.provider.staff && props.provider.staff.length > 0
})

const activeStaffCount = computed(() => {
  if (!props.provider.staff) return 0
  return props.provider.staff.filter(s => s.isActive).length
})

const totalServices = computed(() => {
  return props.provider.services?.length || 0
})

const yearsActive = computed(() => {
  if (!props.provider.registeredAt) return convertToPersianNumber(1)
  const registeredDate = new Date(props.provider.registeredAt)
  const now = new Date()
  const years = Math.floor((now.getTime() - registeredDate.getTime()) / (365.25 * 24 * 60 * 60 * 1000))
  return convertToPersianNumber(Math.max(1, years))
})

// Methods
const getDayName = (day: DayOfWeek): string => {
  const days = ['دوشنبه', 'سه‌شنبه', 'چهارشنبه', 'پنج‌شنبه', 'جمعه', 'شنبه', 'یکشنبه']
  return days[day]
}

const isToday = (day: DayOfWeek): boolean => {
  const now = new Date()
  const currentDay = (now.getDay() + 6) % 7 // Convert Sunday=0 to Monday=0
  return day === currentDay
}

const convertToPersianTime = (time: string): string => {
  // Convert time format and numbers to Persian
  return convertToPersianNumber(time)
}

const convertToPersianNumber = (value: string | number): string => {
  const persianDigits = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹']
  return value.toString().split('').map(char => {
    const digit = parseInt(char)
    return !isNaN(digit) ? persianDigits[digit] : char
  }).join('')
}

const getDefaultBusinessHours = (): BusinessHours[] => {
  // Return default business hours if none are set
  return [
    { id: '1', dayOfWeek: 0, openTime: '09:00', closeTime: '18:00', isOpen: true },
    { id: '2', dayOfWeek: 1, openTime: '09:00', closeTime: '18:00', isOpen: true },
    { id: '3', dayOfWeek: 2, openTime: '09:00', closeTime: '18:00', isOpen: true },
    { id: '4', dayOfWeek: 3, openTime: '09:00', closeTime: '18:00', isOpen: true },
    { id: '5', dayOfWeek: 4, openTime: '09:00', closeTime: '14:00', isOpen: true },
    { id: '6', dayOfWeek: 5, openTime: '09:00', closeTime: '18:00', isOpen: true },
    { id: '7', dayOfWeek: 6, openTime: '00:00', closeTime: '00:00', isOpen: false },
  ]
}

const getDirections = () => {
  // TODO: Integrate with Neshan Maps or Google Maps
  const { latitude, longitude } = props.provider.address
  if (latitude && longitude) {
    window.open(`https://www.google.com/maps/dir/?api=1&destination=${latitude},${longitude}`, '_blank')
  } else {
    const address = `${props.provider.address.addressLine1}, ${props.provider.address.city}, ${props.provider.address.state}`
    window.open(`https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(address)}`, '_blank')
  }
}

/** Get category label supporting both new ProviderCategory and legacy ProviderType */
const getProviderCategoryLabel = (category: ProviderCategory | string | undefined): string => {
  if (typeof category === 'number') {
    // New ProviderCategory enum
    return getCategoryPersianName(category as ProviderCategory)
  }
  // Legacy ProviderType fallback
  const labels: Record<string, string> = {
    salon: 'سالن زیبایی',
    barbershop: 'آرایشگاه',
    spa: 'اسپا',
    clinic: 'کلینیک',
    medical: 'پزشکی',
    gym_fitness: 'باشگاه',
    professional: 'حرفه‌ای',
    other: 'سایر',
  }
  return labels[category?.toLowerCase() || ''] || category || 'نامشخص'
}
</script>

<style scoped>
.profile-about {
  display: flex;
  flex-direction: column;
  gap: 2rem;
}

/* Common Section Styles */
.about-section,
.organization-section,
.hours-section,
.contact-section,
.location-section {
  background: white;
  border-radius: 24px;
  padding: 2.5rem;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.06);
}

.section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 2rem;
}

.section-title {
  font-size: 1.75rem;
  font-weight: 800;
  color: #1e293b;
  margin: 0;
}

/* About Section */
.about-text {
  font-size: 1.05rem;
  line-height: 1.8;
  color: #475569;
  margin: 0 0 2rem 0;
}

.tags-container {
  margin-top: 2rem;
}

.tags-title {
  font-size: 1.125rem;
  font-weight: 700;
  color: #1e293b;
  margin: 0 0 1rem 0;
}

.tags-list {
  display: flex;
  flex-wrap: wrap;
  gap: 0.75rem;
}

.tag {
  padding: 0.625rem 1.25rem;
  background: linear-gradient(135deg, rgba(102, 126, 234, 0.1) 0%, rgba(118, 75, 162, 0.1) 100%);
  color: #667eea;
  border-radius: 12px;
  font-size: 0.95rem;
  font-weight: 600;
  border: 1px solid rgba(102, 126, 234, 0.2);
}

/* Hours Section */
.current-status {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.625rem 1.25rem;
  background: #fef2f2;
  color: #991b1b;
  border-radius: 12px;
  font-size: 0.95rem;
  font-weight: 600;
}

.current-status.open {
  background: #f0fdf4;
  color: #166534;
}

.status-dot {
  width: 10px;
  height: 10px;
  border-radius: 50%;
  background: currentColor;
  animation: pulse 2s ease-in-out infinite;
}

@keyframes pulse {
  0%, 100% {
    opacity: 1;
  }
  50% {
    opacity: 0.5;
  }
}

.hours-list {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.hours-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1.25rem;
  background: #f8fafc;
  border-radius: 12px;
  transition: all 0.3s;
}

.hours-item.today {
  background: linear-gradient(135deg, rgba(102, 126, 234, 0.1) 0%, rgba(118, 75, 162, 0.1) 100%);
  border: 2px solid rgba(102, 126, 234, 0.3);
}

.hours-item.closed {
  opacity: 0.6;
}

.day-name {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.day-label {
  font-size: 1rem;
  font-weight: 700;
  color: #1e293b;
}

.today-badge {
  padding: 0.25rem 0.75rem;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  border-radius: 8px;
  font-size: 0.75rem;
  font-weight: 600;
}

.time-range {
  font-size: 1rem;
  font-weight: 600;
  color: #475569;
}

.closed-label {
  color: #94a3b8;
  font-style: italic;
}

/* Contact Section */
.contact-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
  gap: 1.5rem;
}

.contact-card {
  display: flex;
  gap: 1.25rem;
  padding: 1.5rem;
  background: #f8fafc;
  border-radius: 16px;
  transition: all 0.3s;
}

.contact-card:hover {
  background: #f1f5f9;
  transform: translateY(-2px);
}

.contact-icon {
  width: 56px;
  height: 56px;
  flex-shrink: 0;
  border-radius: 14px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.contact-icon svg {
  width: 28px;
  height: 28px;
  color: white;
}

.contact-icon.email {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.contact-icon.phone {
  background: linear-gradient(135deg, #10b981 0%, #059669 100%);
}

.contact-icon.website {
  background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%);
}

.contact-details {
  flex: 1;
}

.contact-label {
  font-size: 0.875rem;
  color: #64748b;
  font-weight: 600;
  margin-bottom: 0.375rem;
}

.contact-value {
  font-size: 1.05rem;
  color: #1e293b;
  font-weight: 700;
  text-decoration: none;
  display: block;
}

.contact-value:hover {
  color: #667eea;
}

/* Organization Section */
.org-badge {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.625rem 1.25rem;
  background: linear-gradient(135deg, rgba(102, 126, 234, 0.1) 0%, rgba(118, 75, 162, 0.1) 100%);
  color: #667eea;
  border-radius: 12px;
  font-size: 0.95rem;
  font-weight: 600;
  border: 1px solid rgba(102, 126, 234, 0.2);
}

.org-badge svg {
  width: 20px;
  height: 20px;
}

.organization-content {
  display: flex;
  flex-direction: column;
  gap: 2rem;
}

.org-stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 1.5rem;
}

.org-stat-card {
  display: flex;
  align-items: center;
  gap: 1.25rem;
  padding: 1.5rem;
  background: #f8fafc;
  border-radius: 16px;
  transition: all 0.3s;
}

.org-stat-card:hover {
  background: #f1f5f9;
  transform: translateY(-2px);
}

.org-stat-card.verified {
  background: linear-gradient(135deg, rgba(16, 185, 129, 0.05) 0%, rgba(5, 150, 105, 0.05) 100%);
  border: 1px solid rgba(16, 185, 129, 0.2);
}

.stat-icon {
  width: 56px;
  height: 56px;
  flex-shrink: 0;
  border-radius: 14px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.stat-icon svg {
  width: 28px;
  height: 28px;
  color: white;
}

.stat-icon.staff {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.stat-icon.services {
  background: linear-gradient(135deg, #10b981 0%, #059669 100%);
}

.stat-icon.calendar {
  background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%);
}

.stat-icon.verified {
  background: linear-gradient(135deg, #10b981 0%, #059669 100%);
}

.stat-info {
  flex: 1;
}

.stat-value {
  font-size: 1.75rem;
  font-weight: 800;
  color: #1e293b;
  margin-bottom: 0.25rem;
}

.stat-label {
  font-size: 0.875rem;
  color: #64748b;
  font-weight: 500;
}

.org-features {
  padding: 1.5rem;
  background: #f8fafc;
  border-radius: 16px;
}

.features-title {
  font-size: 1.125rem;
  font-weight: 700;
  color: #1e293b;
  margin: 0 0 1.25rem 0;
}

.features-list {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 1rem;
}

.feature-item {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.75rem 1rem;
  background: white;
  border-radius: 12px;
  font-size: 0.95rem;
  font-weight: 500;
  color: #475569;
  transition: all 0.2s;
}

.feature-item:hover {
  background: #f1f5f9;
}

.feature-item svg {
  width: 20px;
  height: 20px;
  color: #10b981;
  flex-shrink: 0;
}

/* Location Section */
.location-content {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 2rem;
}

.address-card {
  display: flex;
  gap: 1.5rem;
}

.address-icon {
  width: 64px;
  height: 64px;
  flex-shrink: 0;
  border-radius: 16px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  display: flex;
  align-items: center;
  justify-content: center;
}

.address-icon svg {
  width: 32px;
  height: 32px;
  color: white;
}

.address-details {
  flex: 1;
}

.address-text {
  font-size: 1.05rem;
  line-height: 1.8;
  color: #475569;
  margin: 0 0 1.5rem 0;
}

.btn-get-directions {
  display: flex;
  align-items: center;
  gap: 0.625rem;
  padding: 0.875rem 1.75rem;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  border: none;
  border-radius: 12px;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s;
}

.btn-get-directions svg {
  width: 20px;
  height: 20px;
}

.btn-get-directions:hover {
  transform: translateY(-2px);
  box-shadow: 0 8px 20px rgba(102, 126, 234, 0.4);
}

.map-container {
  border-radius: 16px;
  overflow: hidden;
}

.map-placeholder {
  height: 100%;
  min-height: 300px;
  background: linear-gradient(135deg, #f1f5f9 0%, #e2e8f0 100%);
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 1rem;
  color: #94a3b8;
}

.map-placeholder svg {
  width: 64px;
  height: 64px;
}

.map-placeholder p {
  font-size: 1rem;
  font-weight: 600;
  margin: 0;
}

/* Responsive */
@media (max-width: 1024px) {
  .location-content {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 768px) {
  .about-section,
  .organization-section,
  .hours-section,
  .contact-section,
  .location-section {
    padding: 1.75rem;
  }

  .section-header {
    flex-direction: column;
    align-items: flex-start;
    gap: 1rem;
  }

  .section-title {
    font-size: 1.5rem;
  }

  .contact-grid {
    grid-template-columns: 1fr;
  }

  .org-stats-grid {
    grid-template-columns: 1fr;
  }

  .features-list {
    grid-template-columns: 1fr;
  }

  .hours-item {
    flex-direction: column;
    align-items: flex-start;
    gap: 0.5rem;
  }

  .address-card {
    flex-direction: column;
  }
}
</style>
