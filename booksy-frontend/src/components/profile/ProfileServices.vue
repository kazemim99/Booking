<template>
  <section class="profile-services" dir="rtl">
    <div class="services-header">
      <h2 class="section-title">خدمات ارائه شده</h2>
      <p class="section-subtitle">
        لیست کامل خدماتی که توسط این ارائه‌دهنده ارائه می‌شود
      </p>
    </div>

    <!-- Services List -->
    <div v-if="services && services.length > 0" class="services-grid">
      <div
        v-for="(service, index) in services"
        :key="service.id"
        class="service-card"
        :style="{ animationDelay: `${index * 0.1}s` }"
      >
        <div class="service-image">
          <img
            v-if="service.imageUrl"
            :src="service.imageUrl"
            :alt="service.name"
            @error="(e) => handleImageError(e, index)"
          />
          <div v-else class="image-placeholder" :style="{ background: getServiceGradient(service.category) }">
            <span class="service-icon">{{ getServiceIcon(service.category) }}</span>
          </div>

          <div v-if="service.status === 'Active'" class="status-badge active">
            فعال
          </div>
          <div v-else class="status-badge inactive">
            غیرفعال
          </div>
        </div>

        <div class="service-content">
          <div class="service-header">
            <h3 class="service-name">{{ service.name }}</h3>
            <div class="service-category">
              {{ getCategoryLabel(service.category) }}
            </div>
          </div>

          <p class="service-description">{{ truncateText(service.description, 100) }}</p>

          <div class="service-details">
            <div class="detail-row">
              <div class="detail-item">
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                <span>{{ convertToPersianNumber(service.duration) }} دقیقه</span>
              </div>

              <div class="detail-item price">
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                <span class="price-value">{{ formatPrice(service.basePrice) }} تومان</span>
              </div>
            </div>

            <div v-if="service.tags && service.tags.length > 0" class="service-tags">
              <span v-for="tag in service.tags.slice(0, 3)" :key="tag" class="tag">
                {{ tag }}
              </span>
            </div>
          </div>

          <button
            v-if="provider.allowOnlineBooking"
            class="btn-book-service"
            @click="handleBookService(service)"
          >
            رزرو این خدمت
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
            </svg>
          </button>
        </div>
      </div>
    </div>

    <!-- Empty State -->
    <div v-else class="empty-state">
      <div class="empty-icon">
        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4" />
        </svg>
      </div>
      <h3>هنوز خدماتی ثبت نشده است</h3>
      <p>این ارائه‌دهنده هنوز خدماتی را ثبت نکرده است. لطفاً بعداً مراجعه کنید.</p>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import type { Provider, ServiceSummary } from '@/modules/provider/types/provider.types'

interface Props {
  provider: Provider
}

const props = defineProps<Props>()
const router = useRouter()

// Computed
const services = computed(() => props.provider.services || [])

// Methods
const getCategoryLabel = (category: string): string => {
  const labels: Record<string, string> = {
    haircut: 'آرایش مو',
    styling: 'استایل مو',
    coloring: 'رنگ مو',
    facial: 'مراقبت پوست',
    massage: 'ماساژ',
    manicure: 'مانیکور',
    pedicure: 'پدیکور',
    makeup: 'آرایش صورت',
    waxing: 'اپیلاسیون',
    threading: 'بند انداختن',
    other: 'سایر',
  }
  return labels[category.toLowerCase()] || category
}

const getServiceIcon = (category: string): string => {
  const icons: Record<string, string> = {
    haircut: '✂️',
    styling: '💇',
    coloring: '🎨',
    facial: '🧖',
    massage: '💆',
    manicure: '💅',
    pedicure: '🦶',
    makeup: '💄',
    waxing: '✨',
    threading: '🧵',
    other: '⭐',
  }
  return icons[category.toLowerCase()] || '⭐'
}

const getServiceGradient = (category: string): string => {
  const gradients: Record<string, string> = {
    haircut: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
    styling: 'linear-gradient(135deg, #f093fb 0%, #f5576c 100%)',
    coloring: 'linear-gradient(135deg, #4facfe 0%, #00f2fe 100%)',
    facial: 'linear-gradient(135deg, #43e97b 0%, #38f9d7 100%)',
    massage: 'linear-gradient(135deg, #fa709a 0%, #fee140 100%)',
    manicure: 'linear-gradient(135deg, #30cfd0 0%, #330867 100%)',
    pedicure: 'linear-gradient(135deg, #a8edea 0%, #fed6e3 100%)',
    makeup: 'linear-gradient(135deg, #ff9a9e 0%, #fecfef 100%)',
    waxing: 'linear-gradient(135deg, #ffecd2 0%, #fcb69f 100%)',
    threading: 'linear-gradient(135deg, #ff6e7f 0%, #bfe9ff 100%)',
    other: 'linear-gradient(135deg, #e0c3fc 0%, #8ec5fc 100%)',
  }
  return gradients[category.toLowerCase()] || gradients.other
}

const formatPrice = (price: number): string => {
  return convertToPersianNumber(price.toLocaleString('fa-IR'))
}

const convertToPersianNumber = (num: number | string): string => {
  const persianDigits = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹']
  return num.toString().split('').map(char => {
    const digit = parseInt(char)
    return !isNaN(digit) ? persianDigits[digit] : char
  }).join('')
}

const truncateText = (text: string, maxLength: number): string => {
  if (!text) return 'توضیحاتی برای این خدمت ثبت نشده است.'
  return text.length > maxLength ? text.substring(0, maxLength) + '...' : text
}

const handleImageError = (event: Event, index: number) => {
  const img = event.target as HTMLImageElement
  img.style.display = 'none'
}

const handleBookService = (service: ServiceSummary) => {
  router.push({
    name: 'NewBooking',
    query: {
      providerId: props.provider.id,
      serviceId: service.id,
    },
  })
}
</script>

<style scoped>
.profile-services {
  padding: 2rem 0;
}

.services-header {
  text-align: center;
  margin-bottom: 3rem;
}

.section-title {
  font-size: 2rem;
  font-weight: 800;
  color: #1e293b;
  margin: 0 0 0.75rem 0;
}

.section-subtitle {
  font-size: 1.05rem;
  color: #64748b;
  margin: 0;
}

.services-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(340px, 1fr));
  gap: 2rem;
}

.service-card {
  background: white;
  border-radius: 20px;
  overflow: hidden;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08);
  transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1);
  animation: fadeInScale 0.5s ease-out;
  animation-fill-mode: both;
}

@keyframes fadeInScale {
  from {
    opacity: 0;
    transform: scale(0.95);
  }
  to {
    opacity: 1;
    transform: scale(1);
  }
}

.service-card:hover {
  transform: translateY(-8px);
  box-shadow: 0 12px 32px rgba(102, 126, 234, 0.2);
}

.service-image {
  position: relative;
  height: 200px;
  overflow: hidden;
}

.service-image img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  transition: transform 0.4s;
}

.service-card:hover .service-image img {
  transform: scale(1.1);
}

.image-placeholder {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
}

.service-icon {
  font-size: 4rem;
}

.status-badge {
  position: absolute;
  top: 1rem;
  left: 1rem;
  padding: 0.5rem 1rem;
  border-radius: 12px;
  font-size: 0.75rem;
  font-weight: 700;
  backdrop-filter: blur(8px);
}

.status-badge.active {
  background: rgba(16, 185, 129, 0.9);
  color: white;
}

.status-badge.inactive {
  background: rgba(148, 163, 184, 0.9);
  color: white;
}

.service-content {
  padding: 1.75rem;
}

.service-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 1rem;
  margin-bottom: 0.75rem;
}

.service-name {
  font-size: 1.375rem;
  font-weight: 700;
  color: #1e293b;
  margin: 0;
  flex: 1;
}

.service-category {
  padding: 0.375rem 0.875rem;
  background: #f1f5f9;
  color: #475569;
  border-radius: 8px;
  font-size: 0.75rem;
  font-weight: 600;
  white-space: nowrap;
}

.service-description {
  font-size: 0.95rem;
  line-height: 1.6;
  color: #64748b;
  margin: 0 0 1.25rem 0;
}

.service-details {
  margin-bottom: 1.5rem;
}

.detail-row {
  display: flex;
  justify-content: space-between;
  margin-bottom: 1rem;
  padding: 1rem;
  background: #f8fafc;
  border-radius: 12px;
}

.detail-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.95rem;
  color: #475569;
  font-weight: 600;
}

.detail-item svg {
  width: 18px;
  height: 18px;
  color: #94a3b8;
}

.detail-item.price {
  color: #10b981;
}

.detail-item.price svg {
  color: #10b981;
}

.price-value {
  font-weight: 700;
  font-size: 1.05rem;
}

.service-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.tag {
  padding: 0.375rem 0.875rem;
  background: linear-gradient(135deg, rgba(102, 126, 234, 0.1) 0%, rgba(118, 75, 162, 0.1) 100%);
  color: #667eea;
  border-radius: 8px;
  font-size: 0.8rem;
  font-weight: 600;
  border: 1px solid rgba(102, 126, 234, 0.2);
}

.btn-book-service {
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.625rem;
  padding: 1rem;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  border: none;
  border-radius: 12px;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s;
}

.btn-book-service svg {
  width: 18px;
  height: 18px;
}

.btn-book-service:hover {
  transform: scale(1.02);
  box-shadow: 0 6px 20px rgba(102, 126, 234, 0.4);
}

.empty-state {
  text-align: center;
  padding: 5rem 2rem;
  background: white;
  border-radius: 24px;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.06);
}

.empty-icon {
  margin: 0 auto 1.5rem;
  width: 80px;
  height: 80px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, rgba(102, 126, 234, 0.1) 0%, rgba(118, 75, 162, 0.1) 100%);
  border-radius: 50%;
}

.empty-icon svg {
  width: 40px;
  height: 40px;
  color: #667eea;
}

.empty-state h3 {
  font-size: 1.5rem;
  font-weight: 700;
  color: #1e293b;
  margin: 0 0 0.75rem 0;
}

.empty-state p {
  font-size: 1rem;
  color: #64748b;
  margin: 0;
}

/* Responsive */
@media (max-width: 768px) {
  .services-grid {
    grid-template-columns: 1fr;
  }

  .section-title {
    font-size: 1.75rem;
  }

  .service-image {
    height: 180px;
  }

  .detail-row {
    flex-direction: column;
    gap: 0.75rem;
  }
}
</style>
