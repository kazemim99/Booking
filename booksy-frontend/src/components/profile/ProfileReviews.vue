<template>
  <section class="profile-reviews" dir="rtl">
    <div class="reviews-header">
      <div class="header-content">
        <h2 class="section-title">نظرات مشتریان</h2>
        <p class="section-subtitle">
          تجربه واقعی مشتریان از خدمات ارائه شده
        </p>
      </div>

      <div class="rating-summary">
        <div class="overall-rating">
          <div class="rating-number">{{ overallRating }}</div>
          <div class="rating-stars">
            <svg
              v-for="star in 5"
              :key="star"
              xmlns="http://www.w3.org/2000/svg"
              viewBox="0 0 24 24"
              :fill="star <= parseFloat(overallRating) ? 'currentColor' : 'none'"
              :stroke="star <= parseFloat(overallRating) ? 'none' : 'currentColor'"
            >
              <path d="M11.48 3.499a.562.562 0 011.04 0l2.125 5.111a.563.563 0 00.475.345l5.518.442c.499.04.701.663.321.988l-4.204 3.602a.563.563 0 00-.182.557l1.285 5.385a.562.562 0 01-.84.61l-4.725-2.885a.563.563 0 00-.586 0L6.982 20.54a.562.562 0 01-.84-.61l1.285-5.386a.562.562 0 00-.182-.557l-4.204-3.602a.563.563 0 01.321-.988l5.518-.442a.563.563 0 00.475-.345L11.48 3.5z" />
            </svg>
          </div>
          <div class="rating-count">بر اساس {{ totalReviews }} نظر</div>
        </div>

        <div class="rating-breakdown">
          <div
            v-for="rating in [5, 4, 3, 2, 1]"
            :key="rating"
            class="rating-bar"
          >
            <span class="rating-label">{{ convertToPersianNumber(rating) }} ستاره</span>
            <div class="bar-container">
              <div class="bar-fill" :style="{ width: `${getRatingPercentage(rating)}%` }"></div>
            </div>
            <span class="rating-percentage">{{ convertToPersianNumber(getRatingPercentage(rating)) }}%</span>
          </div>
        </div>
      </div>
    </div>

    <!-- Reviews List -->
    <div v-if="reviews.length > 0" class="reviews-list">
      <div
        v-for="(review, index) in reviews"
        :key="review.id"
        class="review-card"
        :style="{ animationDelay: `${index * 0.1}s` }"
      >
        <div class="review-header">
          <div class="reviewer-info">
            <div class="reviewer-avatar">
              <span>{{ getInitials(review.customerName) }}</span>
            </div>
            <div class="reviewer-details">
              <h4 class="reviewer-name">{{ review.customerName }}</h4>
              <div class="review-meta">
                <div class="review-rating">
                  <svg
                    v-for="star in 5"
                    :key="star"
                    xmlns="http://www.w3.org/2000/svg"
                    viewBox="0 0 24 24"
                    :fill="star <= review.rating ? 'currentColor' : 'none'"
                    :stroke="star <= review.rating ? 'none' : 'currentColor'"
                  >
                    <path d="M11.48 3.499a.562.562 0 011.04 0l2.125 5.111a.563.563 0 00.475.345l5.518.442c.499.04.701.663.321.988l-4.204 3.602a.563.563 0 00-.182.557l1.285 5.385a.562.562 0 01-.84.61l-4.725-2.885a.563.563 0 00-.586 0L6.982 20.54a.562.562 0 01-.84-.61l1.285-5.386a.562.562 0 00-.182-.557l-4.204-3.602a.563.563 0 01.321-.988l5.518-.442a.563.563 0 00.475-.345L11.48 3.5z" />
                  </svg>
                </div>
                <span class="review-date">{{ review.date }}</span>
              </div>
            </div>
          </div>

          <div v-if="review.verified" class="verified-badge">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
              <path fill-rule="evenodd" d="M8.603 3.799A4.49 4.49 0 0112 2.25c1.357 0 2.573.6 3.397 1.549a4.49 4.49 0 013.498 1.307 4.491 4.491 0 011.307 3.497A4.49 4.49 0 0121.75 12a4.49 4.49 0 01-1.549 3.397 4.491 4.491 0 01-1.307 3.497 4.491 4.491 0 01-3.497 1.307A4.49 4.49 0 0112 21.75a4.49 4.49 0 01-3.397-1.549 4.49 4.49 0 01-3.498-1.306 4.491 4.491 0 01-1.307-3.498A4.49 4.49 0 012.25 12c0-1.357.6-2.573 1.549-3.397a4.49 4.49 0 011.307-3.497 4.49 4.49 0 013.497-1.307zm7.007 6.387a.75.75 0 10-1.22-.872l-3.236 4.53L9.53 12.22a.75.75 0 00-1.06 1.06l2.25 2.25a.75.75 0 001.14-.094l3.75-5.25z" clip-rule="evenodd" />
            </svg>
            خرید تایید شده
          </div>
        </div>

        <p class="review-text">{{ review.comment }}</p>

        <div v-if="review.serviceName" class="review-service">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
          </svg>
          <span>{{ review.serviceName }}</span>
        </div>

        <div class="review-actions">
          <button class="action-btn" @click="likeReview(review.id)">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14 10h4.764a2 2 0 011.789 2.894l-3.5 7A2 2 0 0115.263 21h-4.017c-.163 0-.326-.02-.485-.06L7 20m7-10V5a2 2 0 00-2-2h-.095c-.5 0-.905.405-.905.905 0 .714-.211 1.412-.608 2.006L7 11v9m7-10h-2M7 20H5a2 2 0 01-2-2v-6a2 2 0 012-2h2.5" />
            </svg>
            مفید بود ({{ convertToPersianNumber(review.helpful || 0) }})
          </button>
        </div>
      </div>
    </div>

    <!-- Empty State -->
    <div v-else class="empty-state">
      <div class="empty-icon">
        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
        </svg>
      </div>
      <h3>هنوز نظری ثبت نشده است</h3>
      <p>اولین نفری باشید که نظر خود را درباره این ارائه‌دهنده به اشتراک می‌گذارد.</p>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import type { Provider } from '@/modules/provider/types/provider.types'

interface Review {
  id: string
  customerName: string
  rating: number
  comment: string
  date: string
  verified: boolean
  serviceName?: string
  helpful?: number
}

interface Props {
  provider: Provider
}

defineProps<Props>()

// Mock reviews (replace with real API data)
const mockReviews: Review[] = [
  {
    id: '1',
    customerName: 'سارا محمدی',
    rating: 5,
    comment: 'تجربه فوق‌العاده‌ای داشتم! کیفیت خدمات عالی بود و کارکنان بسیار حرفه‌ای و مهربان. قطعاً دوباره مراجعه خواهم کرد.',
    date: '۲ هفته پیش',
    verified: true,
    serviceName: 'رنگ و هایلایت مو',
    helpful: 12,
  },
  {
    id: '2',
    customerName: 'مینا رضایی',
    rating: 5,
    comment: 'بهترین آرایشگاهی که تا به حال رفتم. فضای بسیار تمیز و مدرن. نتیجه کار خیلی بهتر از انتظارم بود.',
    date: '۳ هفته پیش',
    verified: true,
    serviceName: 'کوتاهی و فشن مو',
    helpful: 8,
  },
  {
    id: '3',
    customerName: 'نگار احمدی',
    rating: 4,
    comment: 'خدمات خوبی ارائه می‌دهند. فقط زمان انتظار کمی طولانی بود ولی در کل راضی بودم.',
    date: '۱ ماه پیش',
    verified: true,
    serviceName: 'ماساژ صورت و پاکسازی پوست',
    helpful: 5,
  },
  {
    id: '4',
    customerName: 'الهام کریمی',
    rating: 5,
    comment: 'عالی! آرایشگر بسیار حرفه‌ای و با تجربه بود. به پیشنهادات من گوش داد و نتیجه کار عالی شد. حتماً پیشنهاد می‌کنم!',
    date: '۱ ماه پیش',
    verified: true,
    serviceName: 'آرایش عروس',
    helpful: 15,
  },
  {
    id: '5',
    customerName: 'زهرا فاطمی',
    rating: 5,
    comment: 'بهترین تجربه آرایشگاه! کارکنان فوق‌العاده و محصولات با کیفیت. حتماً دوباره میام.',
    date: '۲ ماه پیش',
    verified: true,
    serviceName: 'کراتینه و بوتاکس مو',
    helpful: 10,
  },
  {
    id: '6',
    customerName: 'لیلا نوری',
    rating: 4,
    comment: 'خدمات خوب و قیمت مناسب. فضای آرامش‌بخش و کارکنان مهربان.',
    date: '۲ ماه پیش',
    verified: true,
    serviceName: 'مانیکور و پدیکور',
    helpful: 7,
  },
]

// Rating distribution mock data
const ratingDistribution = {
  5: 4,
  4: 2,
  3: 0,
  2: 0,
  1: 0,
}

// Computed
const reviews = computed((): Review[] => {
  // TODO: Get from API when ready
  return mockReviews
})

const overallRating = computed(() => {
  return convertToPersianNumber(4.8)
})

const totalReviews = computed(() => {
  return convertToPersianNumber(127)
})

// Methods
const getRatingPercentage = (rating: number): number => {
  const total = Object.values(ratingDistribution).reduce((sum, count) => sum + count, 0)
  return total > 0 ? Math.round((ratingDistribution[rating as keyof typeof ratingDistribution] / total) * 100) : 0
}

const getInitials = (name: string): string => {
  return name
    .split(' ')
    .map((word) => word[0])
    .join('')
    .toUpperCase()
    .slice(0, 2)
}

const convertToPersianNumber = (num: number | string): string => {
  const persianDigits = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹']
  return num.toString().split('').map(char => {
    const digit = parseInt(char)
    return !isNaN(digit) ? persianDigits[digit] : char
  }).join('')
}

const likeReview = (reviewId: string) => {
  // TODO: Implement like functionality
  console.log('Liked review:', reviewId)
}
</script>

<style scoped>
.profile-reviews {
  padding: 2rem 0;
}

.reviews-header {
  background: white;
  border-radius: 24px;
  padding: 2.5rem;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.06);
  margin-bottom: 2rem;
}

.header-content {
  text-align: center;
  margin-bottom: 2.5rem;
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

.rating-summary {
  display: grid;
  grid-template-columns: auto 1fr;
  gap: 3rem;
  align-items: center;
}

.overall-rating {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.75rem;
  padding: 2rem;
  background: linear-gradient(135deg, rgba(102, 126, 234, 0.1) 0%, rgba(118, 75, 162, 0.1) 100%);
  border-radius: 20px;
}

.rating-number {
  font-size: 4rem;
  font-weight: 800;
  color: #667eea;
  line-height: 1;
}

.rating-stars {
  display: flex;
  gap: 0.25rem;
}

.rating-stars svg {
  width: 28px;
  height: 28px;
  color: #fbbf24;
  stroke-width: 1;
}

.rating-count {
  font-size: 0.95rem;
  color: #64748b;
  font-weight: 600;
}

.rating-breakdown {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.rating-bar {
  display: grid;
  grid-template-columns: 80px 1fr 60px;
  gap: 1rem;
  align-items: center;
}

.rating-label {
  font-size: 0.9rem;
  color: #475569;
  font-weight: 600;
}

.bar-container {
  height: 10px;
  background: #e2e8f0;
  border-radius: 5px;
  overflow: hidden;
}

.bar-fill {
  height: 100%;
  background: linear-gradient(90deg, #667eea 0%, #764ba2 100%);
  border-radius: 5px;
  transition: width 0.3s;
}

.rating-percentage {
  font-size: 0.875rem;
  color: #64748b;
  font-weight: 600;
  text-align: left;
}

.reviews-list {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.review-card {
  background: white;
  border-radius: 20px;
  padding: 2rem;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.06);
  animation: fadeInUp 0.5s ease-out;
  animation-fill-mode: both;
  transition: all 0.3s;
}

.review-card:hover {
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.1);
  transform: translateY(-2px);
}

@keyframes fadeInUp {
  from {
    opacity: 0;
    transform: translateY(20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.review-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 1.25rem;
}

.reviewer-info {
  display: flex;
  gap: 1rem;
}

.reviewer-avatar {
  width: 56px;
  height: 56px;
  border-radius: 50%;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.reviewer-avatar span {
  font-size: 1.25rem;
  font-weight: 700;
  color: white;
}

.reviewer-details {
  flex: 1;
}

.reviewer-name {
  font-size: 1.125rem;
  font-weight: 700;
  color: #1e293b;
  margin: 0 0 0.5rem 0;
}

.review-meta {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.review-rating {
  display: flex;
  gap: 0.125rem;
}

.review-rating svg {
  width: 16px;
  height: 16px;
  color: #fbbf24;
  stroke-width: 1;
}

.review-date {
  font-size: 0.875rem;
  color: #94a3b8;
}

.verified-badge {
  display: flex;
  align-items: center;
  gap: 0.375rem;
  padding: 0.5rem 1rem;
  background: linear-gradient(135deg, #10b981 0%, #059669 100%);
  color: white;
  border-radius: 12px;
  font-size: 0.75rem;
  font-weight: 600;
}

.verified-badge svg {
  width: 14px;
  height: 14px;
}

.review-text {
  font-size: 1.05rem;
  line-height: 1.7;
  color: #475569;
  margin: 0 0 1.25rem 0;
}

.review-service {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.75rem 1.25rem;
  background: #f8fafc;
  border-radius: 12px;
  margin-bottom: 1.25rem;
  font-size: 0.95rem;
  color: #475569;
  font-weight: 600;
}

.review-service svg {
  width: 18px;
  height: 18px;
  color: #10b981;
}

.review-actions {
  display: flex;
  gap: 1rem;
}

.action-btn {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.625rem 1.25rem;
  background: #f1f5f9;
  color: #64748b;
  border: none;
  border-radius: 10px;
  font-size: 0.9rem;
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
  color: #475569;
  transform: translateY(-1px);
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
@media (max-width: 1024px) {
  .rating-summary {
    grid-template-columns: 1fr;
    gap: 2rem;
  }

  .overall-rating {
    width: 100%;
  }
}

@media (max-width: 768px) {
  .reviews-header {
    padding: 1.75rem;
  }

  .section-title {
    font-size: 1.75rem;
  }

  .rating-number {
    font-size: 3rem;
  }

  .rating-bar {
    grid-template-columns: 70px 1fr 50px;
    gap: 0.75rem;
  }

  .review-card {
    padding: 1.5rem;
  }

  .review-header {
    flex-direction: column;
    gap: 1rem;
  }

  .verified-badge {
    align-self: flex-start;
  }

  .reviewer-avatar {
    width: 48px;
    height: 48px;
  }

  .reviewer-avatar span {
    font-size: 1.125rem;
  }
}
</style>
