<template>
  <div class="service-selection" dir="rtl">
    <div class="step-header">
      <h2 class="step-title">انتخاب خدمات</h2>
      <p class="step-description">
        خدمات مورد نظر خود را از لیست زیر انتخاب کنید (چند انتخابی)
      </p>
      <div v-if="selectedServices.length > 0" class="selected-summary">
        <span class="selected-count">{{ convertToPersianNumber(selectedServices.length) }} خدمت انتخاب شده</span>
        <span class="total-price">جمع: {{ formatPrice(totalPrice) }} تومان</span>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="loading-state">
      <div class="loading-spinner"></div>
      <p>در حال بارگذاری خدمات...</p>
    </div>

    <!-- Services Grid -->
    <div v-else-if="services.length > 0" class="services-grid">
      <div
        v-for="service in services"
        :key="service.id"
        class="service-card"
        :class="{ selected: isSelected(service.id) }"
        @click="toggleService(service)"
      >
        <div class="service-image">
          <img
            v-if="service.imageUrl"
            :src="service.imageUrl"
            :alt="service.name"
          />
          <div v-else class="image-placeholder" :style="{ background: getServiceGradient(service.category) }">
            <span class="service-icon">{{ getServiceIcon(service.category) }}</span>
          </div>

          <div v-if="isSelected(service.id)" class="selected-badge">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
              <path fill-rule="evenodd" d="M2.25 12c0-5.385 4.365-9.75 9.75-9.75s9.75 4.365 9.75 9.75-4.365 9.75-9.75 9.75S2.25 17.385 2.25 12zm13.36-1.814a.75.75 0 10-1.22-.872l-3.236 4.53L9.53 12.22a.75.75 0 00-1.06 1.06l2.25 2.25a.75.75 0 001.14-.094l3.75-5.25z" clip-rule="evenodd" />
            </svg>
          </div>
        </div>

        <div class="service-content">
          <h3 class="service-name">{{ service.name }}</h3>
          <p class="service-description">{{ truncate(service.description, 80) }}</p>

          <div class="service-details">
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
              <span>{{ formatPrice(service.basePrice) }} تومان</span>
            </div>
          </div>

          <div class="service-category">
            {{ getCategoryLabel(service.category) }}
          </div>
        </div>
      </div>
    </div>

    <!-- Empty State -->
    <div v-else class="empty-state">
      <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4" />
      </svg>
      <h3>خدماتی یافت نشد</h3>
      <p>متأسفانه این ارائه‌دهنده هنوز خدماتی ثبت نکرده است.</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useProviderStore } from '@/modules/provider/stores/provider.store'
import type { ServiceSummary } from '@/modules/provider/types/provider.types'

interface Props {
  providerId: string
  selectedServiceIds: string[]
}

const props = defineProps<Props>()

const emit = defineEmits<{
  (e: 'services-selected', services: ServiceSummary[]): void
}>()

const providerStore = useProviderStore()

// State
const loading = ref(false)
const services = ref<ServiceSummary[]>([])
const selectedServices = ref<ServiceSummary[]>([])

// Computed
const totalPrice = computed(() => {
  return selectedServices.value.reduce((sum, service) => sum + service.basePrice, 0)
})

// Lifecycle
onMounted(async () => {
  loading.value = true
  try {
    await providerStore.getProviderById(props.providerId, true, false)
    services.value = providerStore.currentProvider?.services || []

    // Initialize selected services if passed via props
    if (props.selectedServiceIds.length > 0) {
      selectedServices.value = services.value.filter(s =>
        props.selectedServiceIds.includes(s.id)
      )
    }
  } catch (error) {
    console.error('Failed to load services:', error)
  } finally {
    loading.value = false
  }
})

// Methods
const isSelected = (serviceId: string): boolean => {
  return selectedServices.value.some(s => s.id === serviceId)
}

const toggleService = (service: ServiceSummary) => {
  const index = selectedServices.value.findIndex(s => s.id === service.id)

  if (index > -1) {
    // Remove service
    selectedServices.value.splice(index, 1)
  } else {
    // Add service
    selectedServices.value.push(service)
  }

  emit('services-selected', selectedServices.value)
}

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

const truncate = (text: string, length: number): string => {
  if (!text) return 'توضیحاتی برای این خدمت ثبت نشده است.'
  return text.length > length ? text.substring(0, length) + '...' : text
}
</script>

<style scoped>
.service-selection {
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
  margin: 0 0 1rem 0;
}

.selected-summary {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 2rem;
  padding: 1rem 2rem;
  background: linear-gradient(135deg, rgba(16, 185, 129, 0.1) 0%, rgba(5, 150, 105, 0.1) 100%);
  border-radius: 12px;
  margin-top: 1rem;
}

.selected-count {
  font-size: 1rem;
  font-weight: 600;
  color: #059669;
}

.total-price {
  font-size: 1.125rem;
  font-weight: 700;
  color: #10b981;
}

.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 4rem 2rem;
  gap: 1.5rem;
}

.loading-spinner {
  width: 48px;
  height: 48px;
  border: 4px solid #e2e8f0;
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
  font-size: 1.05rem;
  color: #64748b;
}

.services-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 1.5rem;
}

.service-card {
  background: white;
  border: 3px solid #e2e8f0;
  border-radius: 20px;
  overflow: hidden;
  cursor: pointer;
  transition: all 0.3s;
}

.service-card:hover {
  border-color: #cbd5e1;
  transform: translateY(-4px);
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.1);
}

.service-card.selected {
  border-color: #667eea;
  box-shadow: 0 8px 32px rgba(102, 126, 234, 0.3);
}

.service-image {
  position: relative;
  height: 180px;
  overflow: hidden;
}

.service-image img {
  width: 100%;
  height: 100%;
  object-fit: cover;
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

.selected-badge {
  position: absolute;
  top: 1rem;
  left: 1rem;
  width: 48px;
  height: 48px;
  background: linear-gradient(135deg, #10b981 0%, #059669 100%);
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 4px 12px rgba(16, 185, 129, 0.4);
  animation: bounceIn 0.4s ease-out;
}

@keyframes bounceIn {
  0% {
    opacity: 0;
    transform: scale(0.3);
  }
  50% {
    transform: scale(1.1);
  }
  100% {
    opacity: 1;
    transform: scale(1);
  }
}

.selected-badge svg {
  width: 28px;
  height: 28px;
  color: white;
}

.service-content {
  padding: 1.5rem;
}

.service-name {
  font-size: 1.25rem;
  font-weight: 700;
  color: #1e293b;
  margin: 0 0 0.75rem 0;
}

.service-description {
  font-size: 0.95rem;
  line-height: 1.6;
  color: #64748b;
  margin: 0 0 1.25rem 0;
}

.service-details {
  display: flex;
  justify-content: space-between;
  gap: 1rem;
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

.service-category {
  display: inline-block;
  padding: 0.5rem 1rem;
  background: linear-gradient(135deg, rgba(102, 126, 234, 0.1) 0%, rgba(118, 75, 162, 0.1) 100%);
  color: #667eea;
  border-radius: 10px;
  font-size: 0.875rem;
  font-weight: 600;
}

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 5rem 2rem;
  text-align: center;
}

.empty-state svg {
  width: 80px;
  height: 80px;
  color: #cbd5e1;
  margin-bottom: 1.5rem;
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

  .step-title {
    font-size: 1.75rem;
  }
}
</style>
