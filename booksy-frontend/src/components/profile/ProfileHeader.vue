<template>
  <section class="profile-header" dir="rtl">
    <!-- Hero Section with Cover Image -->
    <div class="hero-section" :class="{ 'has-gradient': !heroCoverUrl }">
      <img
        v-if="heroCoverUrl"
        :src="heroCoverUrl"
        alt="Cover Image"
        class="hero-image"
      />
      <div class="hero-overlay"></div>
      <button class="btn-back" @click="goBack">
        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
        </svg>
        بازگشت
      </button>
    </div>

    <!-- Provider Info Card -->
    <div class="provider-info-card">
      <div class="card-content">
        <!-- Logo and Basic Info -->
        <div class="provider-main-info">
          <div class="provider-logo">
            <img
              :src="buildProviderImageUrl(provider.profileImageUrl, provider.profile.logoUrl)"
              :alt="provider.profile.businessName"
              @error="handleLogoError"
            />
          </div>

          <div class="provider-details">
            <div class="title-row">
              <h1 class="business-name">{{ provider.profile.businessName }}</h1>
              <div v-if="provider.verifiedAt" class="verified-badge">
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                  <path fill-rule="evenodd" d="M8.603 3.799A4.49 4.49 0 0112 2.25c1.357 0 2.573.6 3.397 1.549a4.49 4.49 0 013.498 1.307 4.491 4.491 0 011.307 3.497A4.49 4.49 0 0121.75 12a4.49 4.49 0 01-1.549 3.397 4.491 4.491 0 01-1.307 3.497 4.491 4.491 0 01-3.497 1.307A4.49 4.49 0 0112 21.75a4.49 4.49 0 01-3.397-1.549 4.49 4.49 0 01-3.498-1.306 4.491 4.491 0 01-1.307-3.498A4.49 4.49 0 012.25 12c0-1.357.6-2.573 1.549-3.397a4.49 4.49 0 011.307-3.497 4.49 4.49 0 013.497-1.307zm7.007 6.387a.75.75 0 10-1.22-.872l-3.236 4.53L9.53 12.22a.75.75 0 00-1.06 1.06l2.25 2.25a.75.75 0 001.14-.094l3.75-5.25z" clip-rule="evenodd" />
                </svg>
                تایید شده
              </div>
            </div>

            <p class="provider-description">{{ provider.profile.description || 'توضیحاتی برای این ارائه‌دهنده ثبت نشده است.' }}</p>

            <div class="provider-meta">
              <div class="meta-item">
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                </svg>
                <span>{{ provider.address.city }}، {{ provider.address.state }}</span>
              </div>

              <div class="meta-item">
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 7h.01M7 3h5c.512 0 1.024.195 1.414.586l7 7a2 2 0 010 2.828l-7 7a2 2 0 01-2.828 0l-7-7A1.994 1.994 0 013 12V7a4 4 0 014-4z" />
                </svg>
                <span>{{ getProviderTypeLabel(provider.type) }}</span>
              </div>
            </div>

            <div class="provider-features">
              <span v-if="provider.allowOnlineBooking" class="feature-tag">
                📅 رزرو آنلاین
              </span>
              <span v-if="provider.offersMobileServices" class="feature-tag">
                🚗 خدمات سیار
              </span>
              <span v-if="provider.tags && provider.tags.length > 0" class="feature-tag">
                ✨ {{ provider.tags.length }} تخصص
              </span>
            </div>
          </div>
        </div>

        <!-- Stats and Actions -->
        <div class="provider-actions">
          <div class="provider-stats">
            <div class="stat-item">
              <div class="stat-icon">⭐</div>
              <div class="stat-value">{{ averageRating }}</div>
              <div class="stat-label">امتیاز</div>
            </div>
            <div class="stat-item">
              <div class="stat-icon">💬</div>
              <div class="stat-value">{{ totalReviews }}</div>
              <div class="stat-label">نظر</div>
            </div>
            <div class="stat-item">
              <div class="stat-icon">✨</div>
              <div class="stat-value">{{ totalServices }}</div>
              <div class="stat-label">خدمات</div>
            </div>
          </div>

          <button
            v-if="provider.allowOnlineBooking"
            class="btn-book-now"
            @click="handleBookNow"
          >
            رزرو کنید
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
            </svg>
          </button>

          <div class="action-buttons">
            <button class="action-btn" @click="handleShare">
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8.684 13.342C8.886 12.938 9 12.482 9 12c0-.482-.114-.938-.316-1.342m0 2.684a3 3 0 110-2.684m0 2.684l6.632 3.316m-6.632-6l6.632-3.316m0 0a3 3 0 105.367-2.684 3 3 0 00-5.367 2.684zm0 9.316a3 3 0 105.368 2.684 3 3 0 00-5.368-2.684z" />
              </svg>
              اشتراک‌گذاری
            </button>
            <button class="action-btn" @click="handleFavorite">
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z" />
              </svg>
              علاقه‌مندی
            </button>
          </div>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import { buildProviderImageUrl, toAbsoluteUrl } from '@/core/utils/url.service'

import type { Provider } from '@/modules/provider/types/provider.types'

interface Props {
  provider: Provider
}

const props = defineProps<Props>()
const router = useRouter()

// Computed
const heroCoverUrl = computed(() => {
  return toAbsoluteUrl(props.provider.profileImageUrl, true)
})

const averageRating = computed(() => {
  // TODO: Get from API - for now using mock data
  return convertToPersianNumber(4.8)
})

const totalReviews = computed(() => {
  // TODO: Get from API - for now using mock data
  return convertToPersianNumber(127)
})

const totalServices = computed(() => {
  return convertToPersianNumber(props.provider.services?.length || 0)
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

const getProviderTypeLabel = (type: string): string => {
  const labels: Record<string, string> = {
    Salon: 'سالن زیبایی',
    Spa: 'اسپا',
    Clinic: 'کلینیک',
    Studio: 'استودیو',
    Individual: 'فردی',
    Professional: 'حرفه‌ای',
  }
  return labels[type] || type
}

const convertToPersianNumber = (num: number): string => {
  const persianDigits = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹']
  return num.toString().split('').map(digit => {
    const parsedDigit = parseInt(digit)
    return !isNaN(parsedDigit) ? persianDigits[parsedDigit] : digit
  }).join('')
}

const handleLogoError = (event: Event) => {
  // Fallback if logo fails to load
  const img = event.target as HTMLImageElement
  img.style.display = 'none'
}

const goBack = () => {
  router.back()
}

const handleBookNow = () => {
  router.push({
    name: 'NewBooking',
    query: { providerId: props.provider.id },
  })
}

const handleShare = () => {
  if (navigator.share) {
    navigator.share({
      title: props.provider.profile.businessName,
      text: props.provider.profile.description,
      url: window.location.href,
    })
  } else {
    // Fallback: copy to clipboard
    navigator.clipboard.writeText(window.location.href)
    alert('لینک کپی شد!')
  }
}

const handleFavorite = () => {
  // TODO: Implement favorite functionality
  alert('به علاقه‌مندی‌ها اضافه شد!')
}
</script>

<style scoped>
.profile-header {
  margin-bottom: 3rem;
}

.hero-section {
  height: 320px;
  position: relative;
  border-radius: 0 0 32px 32px;
  overflow: hidden;
}

.hero-section.has-gradient {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.hero-image {
  width: 100%;
  height: 100%;
  object-fit: cover;
  position: absolute;
  top: 0;
  left: 0;
}

.hero-overlay {
  position: absolute;
  inset: 0;
  background: linear-gradient(to bottom, rgba(0, 0, 0, 0.4), rgba(0, 0, 0, 0.2));
}

.btn-back {
  position: absolute;
  top: 2rem;
  right: 2rem;
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.75rem 1.5rem;
  background: rgba(255, 255, 255, 0.95);
  backdrop-filter: blur(10px);
  border: none;
  border-radius: 12px;
  font-size: 1rem;
  font-weight: 600;
  color: #1e293b;
  cursor: pointer;
  transition: all 0.3s;
  z-index: 10;
}

.btn-back svg {
  width: 20px;
  height: 20px;
}

.btn-back:hover {
  background: white;
  transform: translateX(4px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
}

.provider-info-card {
  max-width: 1200px;
  margin: -100px auto 0;
  padding: 0 2rem;
  position: relative;
  z-index: 5;
}

.card-content {
  background: white;
  border-radius: 24px;
  padding: 2.5rem;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.12);
  display: grid;
  grid-template-columns: 1fr auto;
  gap: 3rem;
  align-items: start;
}

.provider-main-info {
  display: flex;
  gap: 2rem;
}

.provider-logo {
  width: 140px;
  height: 140px;
  flex-shrink: 0;
  border-radius: 20px;
  overflow: hidden;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.12);
  border: 4px solid white;
}

.provider-logo img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.logo-placeholder {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.logo-initials {
  font-size: 3rem;
  font-weight: 800;
  color: white;
}

.provider-details {
  flex: 1;
}

.title-row {
  display: flex;
  align-items: center;
  gap: 1rem;
  margin-bottom: 1rem;
}

.business-name {
  font-size: 2.25rem;
  font-weight: 800;
  color: #1e293b;
  margin: 0;
}

.verified-badge {
  display: flex;
  align-items: center;
  gap: 0.375rem;
  padding: 0.5rem 1rem;
  background: linear-gradient(135deg, #10b981 0%, #059669 100%);
  color: white;
  border-radius: 20px;
  font-size: 0.875rem;
  font-weight: 600;
  box-shadow: 0 4px 12px rgba(16, 185, 129, 0.3);
}

.verified-badge svg {
  width: 16px;
  height: 16px;
}

.provider-description {
  font-size: 1.05rem;
  line-height: 1.7;
  color: #64748b;
  margin: 0 0 1.25rem 0;
}

.provider-meta {
  display: flex;
  flex-wrap: wrap;
  gap: 1.5rem;
  margin-bottom: 1.25rem;
}

.meta-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  color: #475569;
  font-size: 1rem;
  font-weight: 500;
}

.meta-item svg {
  width: 20px;
  height: 20px;
  color: #94a3b8;
}

.provider-features {
  display: flex;
  flex-wrap: wrap;
  gap: 0.75rem;
}

.feature-tag {
  padding: 0.5rem 1.25rem;
  background: linear-gradient(135deg, rgba(102, 126, 234, 0.1) 0%, rgba(118, 75, 162, 0.1) 100%);
  color: #667eea;
  border-radius: 12px;
  font-size: 0.9rem;
  font-weight: 600;
  border: 1px solid rgba(102, 126, 234, 0.2);
}

.provider-actions {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
  align-items: center;
}

.provider-stats {
  display: flex;
  gap: 2rem;
}

.stat-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.375rem;
}

.stat-icon {
  font-size: 2rem;
}

.stat-value {
  font-size: 1.75rem;
  font-weight: 800;
  color: #1e293b;
}

.stat-label {
  font-size: 0.875rem;
  color: #64748b;
  font-weight: 500;
}

.btn-book-now {
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.75rem;
  padding: 1.125rem 2.5rem;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  border: none;
  border-radius: 16px;
  font-size: 1.125rem;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.3s;
  box-shadow: 0 4px 16px rgba(102, 126, 234, 0.4);
}

.btn-book-now svg {
  width: 22px;
  height: 22px;
}

.btn-book-now:hover {
  transform: translateY(-2px);
  box-shadow: 0 8px 24px rgba(102, 126, 234, 0.5);
}

.action-buttons {
  display: flex;
  gap: 1rem;
  width: 100%;
}

.action-btn {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 0.875rem;
  background: #f1f5f9;
  color: #475569;
  border: none;
  border-radius: 12px;
  font-size: 0.95rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s;
}

.action-btn svg {
  width: 18px;
  height: 18px;
}

.action-btn:hover {
  background: #e2e8f0;
  transform: translateY(-2px);
}

/* Responsive */
@media (max-width: 1024px) {
  .card-content {
    grid-template-columns: 1fr;
    gap: 2rem;
  }

  .provider-actions {
    width: 100%;
  }

  .provider-stats {
    width: 100%;
    justify-content: space-around;
  }
}

@media (max-width: 768px) {
  .hero-section {
    height: 240px;
    border-radius: 0 0 24px 24px;
  }

  .btn-back {
    top: 1rem;
    right: 1rem;
    padding: 0.625rem 1.25rem;
    font-size: 0.95rem;
  }

  .provider-info-card {
    margin-top: -80px;
    padding: 0 1rem;
  }

  .card-content {
    padding: 1.75rem;
  }

  .provider-main-info {
    flex-direction: column;
    align-items: center;
    text-align: center;
  }

  .provider-logo {
    width: 120px;
    height: 120px;
  }

  .business-name {
    font-size: 1.75rem;
  }

  .title-row {
    flex-direction: column;
    gap: 0.75rem;
  }

  .provider-meta {
    justify-content: center;
  }

  .provider-features {
    justify-content: center;
  }

  .provider-stats {
    gap: 1.5rem;
  }

  .stat-icon {
    font-size: 1.5rem;
  }

  .stat-value {
    font-size: 1.5rem;
  }
}
</style>
