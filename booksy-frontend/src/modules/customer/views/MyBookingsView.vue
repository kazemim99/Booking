<template>
  <div class="my-bookings" dir="rtl">
    <div class="bookings-header">
      <h2>رزروهای من</h2>
      <button @click="router.push('/customer/book')" class="btn-new-booking">
        + رزرو جدید
      </button>
    </div>

    <div class="bookings-tabs">
      <button @click="activeTab = 'upcoming'" :class="{ active: activeTab === 'upcoming' }">
        آینده ({{ upcomingCount }})
      </button>
      <button @click="activeTab = 'past'" :class="{ active: activeTab === 'past' }">
        گذشته ({{ pastCount }})
      </button>
      <button @click="activeTab = 'cancelled'" :class="{ active: activeTab === 'cancelled' }">
        لغو شده ({{ cancelledCount }})
      </button>
    </div>

    <div class="bookings-list">
      <div v-for="booking in filteredBookings" :key="booking.id" class="booking-card">
        <div class="booking-status" :class="booking.status">
          {{ getStatusLabel(booking.status) }}
        </div>
        <div class="booking-content">
          <div class="booking-date-time">
            <div class="date">{{ formatDate(booking.startTime) }}</div>
            <div class="time">{{ formatTime(booking.startTime) }}</div>
          </div>
          <div class="booking-details">
            <h3>{{ booking.serviceName }}</h3>
            <p class="provider">{{ booking.providerName }}</p>
            <p class="address">📍 {{ booking.address }}</p>
          </div>
          <div class="booking-actions">
            <button @click="viewDetails(booking.id)" class="btn-view">جزئیات</button>
            <button v-if="canCancel(booking)" @click="cancelBooking(booking.id)" class="btn-cancel">
              لغو رزرو
            </button>
            <button v-if="canRebook(booking)" @click="rebookService(booking)" class="btn-rebook">
              رزرو مجدد
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'

const router = useRouter()
const activeTab = ref<'upcoming' | 'past' | 'cancelled'>('upcoming')

const bookings = ref([
  {
    id: '1',
    serviceName: 'کوتاهی مو',
    providerName: 'آرایشگاه زیبا',
    address: 'تهران، ونک',
    startTime: new Date(Date.now() + 86400000),
    status: 'confirmed',
  },
  {
    id: '2',
    serviceName: 'ماساژ',
    providerName: 'اسپا رویا',
    address: 'تهران، نیاوران',
    startTime: new Date(Date.now() - 86400000),
    status: 'completed',
  },
])

const upcomingCount = computed(() => 
  bookings.value.filter(b => b.status === 'confirmed' || b.status === 'pending').length
)
const pastCount = computed(() => 
  bookings.value.filter(b => b.status === 'completed').length
)
const cancelledCount = computed(() => 
  bookings.value.filter(b => b.status === 'cancelled').length
)

const filteredBookings = computed(() => {
  if (activeTab.value === 'upcoming') {
    return bookings.value.filter(b => b.status === 'confirmed' || b.status === 'pending')
  } else if (activeTab.value === 'past') {
    return bookings.value.filter(b => b.status === 'completed')
  } else {
    return bookings.value.filter(b => b.status === 'cancelled')
  }
})

function getStatusLabel(status: string) {
  const labels: Record<string, string> = {
    pending: 'در انتظار تایید',
    confirmed: 'تایید شده',
    completed: 'تکمیل شده',
    cancelled: 'لغو شده',
  }
  return labels[status] || status
}

function formatDate(date: Date) {
  return new Intl.DateTimeFormat('fa-IR', { year: 'numeric', month: 'long', day: 'numeric' }).format(new Date(date))
}

function formatTime(date: Date) {
  return new Intl.DateTimeFormat('fa-IR', { hour: '2-digit', minute: '2-digit' }).format(new Date(date))
}

function canCancel(booking: any) {
  return booking.status === 'confirmed' && new Date(booking.startTime) > new Date()
}

function canRebook(booking: any) {
  return booking.status === 'completed' || booking.status === 'cancelled'
}

function viewDetails(id: string) {
  router.push(`/customer/booking/${id}`)
}

function cancelBooking(id: string) {
  if (confirm('آیا از لغو این رزرو مطمئن هستید؟')) {
    // API call to cancel
    console.log('Cancelling booking:', id)
  }
}

function rebookService(booking: any) {
  router.push({ name: 'NewBooking', query: { serviceId: booking.serviceId } })
}
</script>

<style scoped>
.bookings-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 2rem;
}

.btn-new-booking {
  padding: 0.75rem 1.5rem;
  background: var(--color-primary);
  color: white;
  border: none;
  border-radius: 0.5rem;
  cursor: pointer;
}

.bookings-tabs {
  display: flex;
  gap: 1rem;
  margin-bottom: 2rem;
  border-bottom: 2px solid var(--color-gray-200);
}

.bookings-tabs button {
  padding: 1rem 1.5rem;
  background: none;
  border: none;
  border-bottom: 2px solid transparent;
  cursor: pointer;
}

.bookings-tabs button.active {
  border-bottom-color: var(--color-primary);
  color: var(--color-primary);
}

.booking-card {
  background: white;
  border-radius: 0.75rem;
  padding: 1.5rem;
  margin-bottom: 1rem;
  position: relative;
}

.booking-status {
  position: absolute;
  top: 1rem;
  left: 1rem;
  padding: 0.375rem 0.75rem;
  border-radius: 0.375rem;
  font-size: 0.875rem;
  font-weight: 500;
}

.booking-status.confirmed {
  background: var(--color-green-100);
  color: var(--color-green-700);
}

.booking-status.pending {
  background: var(--color-yellow-100);
  color: var(--color-yellow-700);
}

.booking-status.completed {
  background: var(--color-blue-100);
  color: var(--color-blue-700);
}

.booking-status.cancelled {
  background: var(--color-red-100);
  color: var(--color-red-700);
}

.booking-content {
  display: grid;
  grid-template-columns: auto 1fr auto;
  gap: 1.5rem;
  align-items: center;
}

.booking-date-time {
  text-align: center;
  padding: 1rem;
  background: var(--color-primary-50);
  border-radius: 0.5rem;
}

.date {
  font-weight: 600;
  margin-bottom: 0.25rem;
}

.time {
  font-size: 0.875rem;
  color: var(--color-gray-600);
}

.booking-details h3 {
  margin: 0 0 0.5rem;
}

.booking-details p {
  margin: 0.25rem 0;
  color: var(--color-gray-600);
}

.booking-actions {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.booking-actions button {
  padding: 0.5rem 1rem;
  border: none;
  border-radius: 0.375rem;
  cursor: pointer;
  min-width: 100px;
}

.btn-view {
  background: var(--color-primary);
  color: white;
}

.btn-cancel {
  background: var(--color-red-100);
  color: var(--color-red-700);
}

.btn-rebook {
  background: var(--color-gray-100);
}
</style>
