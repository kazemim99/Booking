<template>
  <div class="stats-grid">
    <!-- Total Bookings Card -->
    <div class="stat-card">
      <div class="card-header">
        <h3 class="card-title">مجموع رزروها این ماه</h3>
        <svg class="header-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
        </svg>
      </div>
      <div class="stat-value">{{ formatNumber(totalBookings) }}</div>
      <p class="stat-caption">
        <span class="stat-increase">↑ {{ formatNumber(12) }}٪</span> نسبت به ماه قبل
      </p>
    </div>

    <!-- Completion Ratio Card -->
    <div class="stat-card">
      <h3 class="card-title card-title-single">نسبت انجام رزروها</h3>
      <div class="chart-container">
        <PieChart
          :data="pieChartData"
          :options="pieChartOptions"
        />
      </div>
      <div class="legend">
        <div class="legend-item">
          <div class="legend-dot legend-completed" />
          <span>انجام‌شده</span>
        </div>
        <div class="legend-item">
          <div class="legend-dot legend-cancelled" />
          <span>لغوشده</span>
        </div>
        <div class="legend-item">
          <div class="legend-dot legend-scheduled" />
          <span>رزروشده</span>
        </div>
      </div>
    </div>

    <!-- Revenue Trend Card -->
    <div class="stat-card">
      <div class="card-header">
        <h3 class="card-title">روند درآمد (هزار تومان)</h3>
        <svg class="header-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 7h8m0 0v8m0-8l-8 8-4-4-6 6" />
        </svg>
      </div>
      <div class="chart-container">
        <LineChart
          :data="lineChartData"
          :options="lineChartOptions"
        />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, onMounted, watch } from 'vue'
import { PieChart, LineChart } from '@/shared/components/charts'
import { convertEnglishToPersianNumbers } from '@/shared/utils/date/jalali.utils'
import { bookingService } from '@/modules/booking/api/booking.service'
import type { ChartData, ChartOptions } from 'chart.js'

interface Props {
  providerId?: string
}

const props = defineProps<Props>()

const completedCount = ref(0)
const cancelledCount = ref(0)
const scheduledCount = ref(0)
const loading = ref(false)
const revenueData = ref([
  { month: 'فروردین', revenue: 0 },
  { month: 'اردیبهشت', revenue: 0 },
  { month: 'خرداد', revenue: 0 },
  { month: 'تیر', revenue: 0 },
  { month: 'مرداد', revenue: 0 },
  { month: 'شهریور', revenue: 0 }
])

// Fetch booking stats
const fetchStats = async () => {
  if (!props.providerId) return

  loading.value = true
  try {
    const stats = await bookingService.getProviderBookingStats(props.providerId)
    completedCount.value = stats.completed
    cancelledCount.value = stats.cancelled
    scheduledCount.value = stats.pending + stats.confirmed
  } catch (error) {
    console.error('Error fetching booking stats:', error)
  } finally {
    loading.value = false
  }
}

// Watch for providerId changes
watch(() => props.providerId, () => {
  if (props.providerId) {
    fetchStats()
  }
})

// Load data on mount
onMounted(() => {
  if (props.providerId) {
    fetchStats()
  }
})

const totalBookings = computed(() => {
  return completedCount.value + cancelledCount.value + scheduledCount.value
})

const formatNumber = (num: number) => {
  return convertEnglishToPersianNumbers(num.toString())
}

// Pie Chart Data
const pieChartData = computed<ChartData<'pie'>>(() => ({
  labels: ['انجام‌شده', 'لغوشده', 'رزروشده'],
  datasets: [
    {
      data: [completedCount.value, cancelledCount.value, scheduledCount.value],
      backgroundColor: ['#22c55e', '#ef4444', '#f59e0b'],
      borderWidth: 2,
      borderColor: '#ffffff'
    }
  ]
}))

const pieChartOptions = computed<ChartOptions<'pie'>>(() => ({
  responsive: true,
  maintainAspectRatio: true,
  plugins: {
    legend: {
      display: false
    },
    tooltip: {
      rtl: true,
      bodyFont: {
        family: 'IRANSans, Vazir, sans-serif'
      }
    }
  }
}))

// Line Chart Data
const lineChartData = computed<ChartData<'line'>>(() => ({
  labels: revenueData.value.map(d => d.month),
  datasets: [
    {
      label: 'درآمد',
      data: revenueData.value.map(d => d.revenue),
      borderColor: '#6366f1',
      backgroundColor: 'rgba(99, 102, 241, 0.1)',
      borderWidth: 2,
      tension: 0.4,
      pointRadius: 4,
      pointBackgroundColor: '#6366f1',
      pointBorderColor: '#ffffff',
      pointBorderWidth: 2,
      fill: true
    }
  ]
}))

const lineChartOptions = computed<ChartOptions<'line'>>(() => ({
  responsive: true,
  maintainAspectRatio: true,
  plugins: {
    legend: {
      display: false
    },
    tooltip: {
      rtl: true,
      bodyFont: {
        family: 'IRANSans, Vazir, sans-serif'
      }
    }
  },
  scales: {
    x: {
      grid: {
        display: false
      },
      ticks: {
        font: {
          family: 'IRANSans, Vazir, sans-serif',
          size: 10
        },
        color: '#6b7280'
      }
    },
    y: {
      beginAtZero: true,
      grid: {
        color: '#e5e7eb'
      },
      ticks: {
        font: {
          family: 'IRANSans, Vazir, sans-serif',
          size: 10
        },
        color: '#6b7280'
      }
    }
  }
}))
</script>

<style scoped lang="scss">
.stats-grid {
  display: grid;
  grid-template-columns: 1fr;
  gap: 24px;
  margin-bottom: 24px;
}

@media (min-width: 1024px) {
  .stats-grid {
    grid-template-columns: repeat(3, 1fr);
  }
}

.stat-card {
  background: white;
  border-radius: 8px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  padding: 24px;
}

.card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding-bottom: 8px;
}

.card-title {
  font-size: 14px;
  font-weight: 500;
  color: #6b7280;
  margin: 0;
}

.card-title-single {
  margin-bottom: 16px;
}

.header-icon {
  width: 16px;
  height: 16px;
  color: #9ca3af;
}

.stat-value {
  font-size: 30px;
  font-weight: 700;
  margin-bottom: 4px;
  color: #1f2937;
}

.stat-caption {
  font-size: 12px;
  color: #6b7280;
  margin: 0;
}

.stat-increase {
  color: #16a34a;
}

.chart-container {
  height: 150px;
  margin-bottom: 16px;
}

.legend {
  display: flex;
  justify-content: center;
  gap: 16px;
  font-size: 12px;
  margin-top: 16px;
}

.legend-item {
  display: flex;
  align-items: center;
  gap: 4px;
}

.legend-dot {
  width: 12px;
  height: 12px;
  border-radius: 50%;
}

.legend-completed {
  background: #22c55e;
}

.legend-cancelled {
  background: #ef4444;
}

.legend-scheduled {
  background: #f59e0b;
}
</style>
