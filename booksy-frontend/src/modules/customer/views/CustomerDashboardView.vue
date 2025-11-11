<template>
  <div class="customer-dashboard" dir="rtl">
    <div class="welcome-section">
      <h2>خوش آمدید، {{ userName }}!</h2>
      <p>به داشبورد خود خوش آمدید</p>
    </div>

    <div class="stats-grid">
      <div class="stat-card">
        <div class="stat-icon">📅</div>
        <div class="stat-info">
          <h3>{{ upcomingBookings }}</h3>
          <p>رزروهای آینده</p>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon">✅</div>
        <div class="stat-info">
          <h3>{{ completedBookings }}</h3>
          <p>رزروهای تکمیل شده</p>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon">❤️</div>
        <div class="stat-info">
          <h3>{{ favoritesCount }}</h3>
          <p>علاقه‌مندی‌ها</p>
        </div>
      </div>
    </div>

    <div class="quick-actions">
      <h3>دسترسی سریع</h3>
      <div class="actions-grid">
        <button @click="router.push('/customer/browse')" class="action-card">
          <span class="action-icon">🔍</span>
          <span class="action-label">جستجوی خدمات</span>
        </button>
        <button @click="router.push('/customer/book')" class="action-card">
          <span class="action-icon">➕</span>
          <span class="action-label">رزرو جدید</span>
        </button>
        <button @click="router.push('/customer/my-bookings')" class="action-card">
          <span class="action-icon">📋</span>
          <span class="action-label">رزروهای من</span>
        </button>
        <button @click="router.push('/customer/favorites')" class="action-card">
          <span class="action-icon">⭐</span>
          <span class="action-label">علاقه‌مندی‌ها</span>
        </button>
      </div>
    </div>

    <div class="upcoming-appointments">
      <h3>رزروهای آینده</h3>
      <div v-if="upcomingList.length > 0" class="appointments-list">
        <div v-for="booking in upcomingList" :key="booking.id" class="appointment-item">
          <div class="appointment-date">
            <span class="day">{{ formatDay(booking.startTime) }}</span>
            <span class="time">{{ formatTime(booking.startTime) }}</span>
          </div>
          <div class="appointment-details">
            <h4>{{ booking.serviceName }}</h4>
            <p>{{ booking.providerName }}</p>
          </div>
          <button class="btn-view" @click="viewBooking(booking.id)">مشاهده</button>
        </div>
      </div>
      <div v-else class="empty-state">
        <p>رزرو آینده‌ای ندارید</p>
        <button @click="router.push('/customer/book')" class="btn-primary">رزرو جدید</button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/core/stores'

const router = useRouter()
const authStore = useAuthStore()

const userName = computed(() => authStore.user?.firstName || 'کاربر')

// Mock data - replace with real API calls
const upcomingBookings = ref(3)
const completedBookings = ref(12)
const favoritesCount = ref(5)

const upcomingList = ref([
  {
    id: '1',
    serviceName: 'کوتاهی مو',
    providerName: 'آرایشگاه زیبا',
    startTime: new Date(Date.now() + 86400000),
  },
  {
    id: '2',
    serviceName: 'ماساژ',
    providerName: 'اسپا رویا',
    startTime: new Date(Date.now() + 172800000),
  },
])

function formatDay(date: Date) {
  return new Intl.DateTimeFormat('fa-IR', { day: 'numeric', month: 'short' }).format(new Date(date))
}

function formatTime(date: Date) {
  return new Intl.DateTimeFormat('fa-IR', { hour: '2-digit', minute: '2-digit' }).format(new Date(date))
}

function viewBooking(id: string) {
  router.push(`/customer/booking/${id}`)
}
</script>

<style scoped>
.customer-dashboard {
  max-width: 1200px;
  margin: 0 auto;
}

.welcome-section {
  margin-bottom: 2rem;
}

.welcome-section h2 {
  font-size: 2rem;
  margin-bottom: 0.5rem;
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 1.5rem;
  margin-bottom: 2rem;
}

.stat-card {
  background: white;
  padding: 1.5rem;
  border-radius: 0.75rem;
  display: flex;
  align-items: center;
  gap: 1rem;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.stat-icon {
  font-size: 2.5rem;
}

.stat-info h3 {
  font-size: 2rem;
  margin: 0;
  color: var(--color-primary);
}

.stat-info p {
  margin: 0.25rem 0 0;
  color: var(--color-gray-600);
}

.quick-actions {
  background: white;
  padding: 1.5rem;
  border-radius: 0.75rem;
  margin-bottom: 2rem;
}

.quick-actions h3 {
  margin-top: 0;
}

.actions-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 1rem;
}

.action-card {
  background: var(--color-gray-50);
  border: 2px solid var(--color-gray-200);
  border-radius: 0.5rem;
  padding: 1.5rem;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
  cursor: pointer;
  transition: all 0.2s;
}

.action-card:hover {
  background: var(--color-primary-50);
  border-color: var(--color-primary);
}

.action-icon {
  font-size: 2rem;
}

.upcoming-appointments {
  background: white;
  padding: 1.5rem;
  border-radius: 0.75rem;
}

.upcoming-appointments h3 {
  margin-top: 0;
}

.appointment-item {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 1rem;
  border: 1px solid var(--color-gray-200);
  border-radius: 0.5rem;
  margin-bottom: 1rem;
}

.appointment-date {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 0.75rem;
  background: var(--color-primary-50);
  border-radius: 0.5rem;
  min-width: 80px;
}

.appointment-details {
  flex: 1;
}

.appointment-details h4 {
  margin: 0 0 0.25rem;
}

.appointment-details p {
  margin: 0;
  color: var(--color-gray-600);
}

.btn-view {
  padding: 0.5rem 1rem;
  background: var(--color-primary);
  color: white;
  border: none;
  border-radius: 0.375rem;
  cursor: pointer;
}

.empty-state {
  text-align: center;
  padding: 3rem;
  color: var(--color-gray-500);
}

.btn-primary {
  padding: 0.75rem 1.5rem;
  background: var(--color-primary);
  color: white;
  border: none;
  border-radius: 0.5rem;
  cursor: pointer;
  margin-top: 1rem;
}
</style>
