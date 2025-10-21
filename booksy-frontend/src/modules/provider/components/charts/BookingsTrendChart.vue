<template>
  <div class="bookings-trend-chart">
    <!-- Chart Header -->
    <div class="chart-header">
      <div class="header-content">
        <h3 class="chart-title">{{ title }}</h3>
        <p v-if="subtitle" class="chart-subtitle">{{ subtitle }}</p>
      </div>
      <div class="chart-controls">
        <div class="chart-type-selector">
          <button
            :class="['type-button', { active: chartType === 'bar' }]"
            @click="chartType = 'bar'"
            :title="$t('dashboard.charts.barChart')"
          >
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z"
              />
            </svg>
          </button>
          <button
            :class="['type-button', { active: chartType === 'line' }]"
            @click="chartType = 'line'"
            :title="$t('dashboard.charts.lineChart')"
          >
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M7 12l3-3 3 3 4-4"
              />
            </svg>
          </button>
        </div>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="chart-loading">
      <Skeleton height="300px" animation="wave" />
    </div>

    <!-- Chart Container -->
    <div v-else class="chart-container">
      <v-chart
        ref="chartRef"
        :option="chartOption"
        :theme="chartTheme"
        autoresize
        class="echart"
      />
    </div>

    <!-- Chart Footer Stats -->
    <div v-if="!loading && showFooterStats" class="chart-footer">
      <div class="footer-stat">
        <span class="stat-label">{{ $t('dashboard.charts.totalBookings') }}</span>
        <span class="stat-value">{{ totalBookings }}</span>
      </div>
      <div class="footer-stat">
        <span class="stat-label">{{ $t('dashboard.charts.completed') }}</span>
        <span class="stat-value stat-success">{{ completedBookings }}</span>
      </div>
      <div class="footer-stat">
        <span class="stat-label">{{ $t('dashboard.charts.cancelled') }}</span>
        <span class="stat-value stat-danger">{{ cancelledBookings }}</span>
      </div>
      <div class="footer-stat">
        <span class="stat-label">{{ $t('dashboard.charts.completionRate') }}</span>
        <span class="stat-value">{{ completionRate }}%</span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { use } from 'echarts/core'
import { CanvasRenderer } from 'echarts/renderers'
import { LineChart, BarChart } from 'echarts/charts'
import {
  TitleComponent,
  TooltipComponent,
  GridComponent,
  LegendComponent,
} from 'echarts/components'
import VChart from 'vue-echarts'
import { Skeleton } from '@/shared/components'

// Register ECharts components
use([
  CanvasRenderer,
  LineChart,
  BarChart,
  TitleComponent,
  TooltipComponent,
  GridComponent,
  LegendComponent,
])

interface BookingsDataPoint {
  date: string
  completed: number
  cancelled: number
  pending: number
}

interface Props {
  title?: string
  subtitle?: string
  data?: BookingsDataPoint[]
  loading?: boolean
  showFooterStats?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  title: 'Bookings Trend',
  subtitle: '',
  data: () => [],
  loading: false,
  showFooterStats: true,
})

const chartRef = ref()
const chartType = ref<'bar' | 'line'>('bar')

// Chart theme
const chartTheme = computed(() => ({
  color: ['#10b981', '#ef4444', '#f59e0b'],
  backgroundColor: 'transparent',
}))

// Process chart data
const chartData = computed(() => {
  return props.data.map((point) => ({
    date: new Date(point.date).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
    }),
    completed: point.completed,
    cancelled: point.cancelled,
    pending: point.pending,
  }))
})

const chartOption = computed(() => ({
  grid: {
    left: '3%',
    right: '4%',
    bottom: '10%',
    top: '15%',
    containLabel: true,
  },
  legend: {
    top: 0,
    left: 'center',
    itemWidth: 12,
    itemHeight: 12,
    textStyle: {
      color: '#6b7280',
      fontSize: 12,
      fontFamily: 'Inter, system-ui, sans-serif',
    },
    data: ['Completed', 'Cancelled', 'Pending'],
  },
  tooltip: {
    trigger: 'axis',
    backgroundColor: 'rgba(255, 255, 255, 0.95)',
    borderColor: '#e5e7eb',
    borderWidth: 1,
    textStyle: {
      color: '#1f2937',
      fontSize: 14,
    },
    formatter: (params: any) => {
      let result = `<div style="padding: 4px 0;"><div style="font-weight: 600; margin-bottom: 8px;">${params[0].name}</div>`
      params.forEach((item: any) => {
        result += `
          <div style="display: flex; align-items: center; justify-content: space-between; gap: 16px; margin-bottom: 4px;">
            <div style="display: flex; align-items: center; gap: 8px;">
              <span style="display: inline-block; width: 10px; height: 10px; background: ${item.color}; border-radius: 50%;"></span>
              <span>${item.seriesName}</span>
            </div>
            <span style="font-weight: 600;">${item.value}</span>
          </div>
        `
      })
      result += '</div>'
      return result
    },
    axisPointer: {
      type: chartType.value === 'bar' ? 'shadow' : 'line',
      lineStyle: {
        color: '#667eea',
        width: 1,
        type: 'dashed',
      },
      shadowStyle: {
        color: 'rgba(102, 126, 234, 0.1)',
      },
    },
  },
  xAxis: {
    type: 'category',
    data: chartData.value.map((d) => d.date),
    axisLine: {
      lineStyle: {
        color: '#e5e7eb',
      },
    },
    axisLabel: {
      color: '#6b7280',
      fontSize: 12,
      fontFamily: 'Inter, system-ui, sans-serif',
    },
    axisTick: {
      show: false,
    },
  },
  yAxis: {
    type: 'value',
    axisLine: {
      show: false,
    },
    axisLabel: {
      color: '#6b7280',
      fontSize: 12,
      fontFamily: 'Inter, system-ui, sans-serif',
    },
    splitLine: {
      lineStyle: {
        color: '#f3f4f6',
        type: 'dashed',
      },
    },
  },
  series: [
    {
      name: 'Completed',
      type: chartType.value,
      smooth: chartType.value === 'line',
      stack: chartType.value === 'bar' ? 'total' : undefined,
      itemStyle: {
        color: '#10b981',
        borderRadius: chartType.value === 'bar' ? [4, 4, 0, 0] : 0,
      },
      lineStyle: chartType.value === 'line' ? { width: 3 } : undefined,
      areaStyle:
        chartType.value === 'line'
          ? {
              color: {
                type: 'linear',
                x: 0,
                y: 0,
                x2: 0,
                y2: 1,
                colorStops: [
                  { offset: 0, color: 'rgba(16, 185, 129, 0.3)' },
                  { offset: 1, color: 'rgba(16, 185, 129, 0.05)' },
                ],
              },
            }
          : undefined,
      emphasis: {
        focus: 'series',
      },
      data: chartData.value.map((d) => d.completed),
    },
    {
      name: 'Cancelled',
      type: chartType.value,
      smooth: chartType.value === 'line',
      stack: chartType.value === 'bar' ? 'total' : undefined,
      itemStyle: {
        color: '#ef4444',
        borderRadius: chartType.value === 'bar' ? [4, 4, 0, 0] : 0,
      },
      lineStyle: chartType.value === 'line' ? { width: 3 } : undefined,
      areaStyle:
        chartType.value === 'line'
          ? {
              color: {
                type: 'linear',
                x: 0,
                y: 0,
                x2: 0,
                y2: 1,
                colorStops: [
                  { offset: 0, color: 'rgba(239, 68, 68, 0.3)' },
                  { offset: 1, color: 'rgba(239, 68, 68, 0.05)' },
                ],
              },
            }
          : undefined,
      emphasis: {
        focus: 'series',
      },
      data: chartData.value.map((d) => d.cancelled),
    },
    {
      name: 'Pending',
      type: chartType.value,
      smooth: chartType.value === 'line',
      stack: chartType.value === 'bar' ? 'total' : undefined,
      itemStyle: {
        color: '#f59e0b',
        borderRadius: chartType.value === 'bar' ? [4, 4, 0, 0] : 0,
      },
      lineStyle: chartType.value === 'line' ? { width: 3 } : undefined,
      areaStyle:
        chartType.value === 'line'
          ? {
              color: {
                type: 'linear',
                x: 0,
                y: 0,
                x2: 0,
                y2: 1,
                colorStops: [
                  { offset: 0, color: 'rgba(245, 158, 11, 0.3)' },
                  { offset: 1, color: 'rgba(245, 158, 11, 0.05)' },
                ],
              },
            }
          : undefined,
      emphasis: {
        focus: 'series',
      },
      data: chartData.value.map((d) => d.pending),
    },
  ],
}))

// Footer stats calculations
const totalBookings = computed(() => {
  return props.data.reduce((sum, point) => sum + point.completed + point.cancelled + point.pending, 0)
})

const completedBookings = computed(() => {
  return props.data.reduce((sum, point) => sum + point.completed, 0)
})

const cancelledBookings = computed(() => {
  return props.data.reduce((sum, point) => sum + point.cancelled, 0)
})

const completionRate = computed(() => {
  if (totalBookings.value === 0) return 0
  return ((completedBookings.value / totalBookings.value) * 100).toFixed(1)
})

// Resize chart on window resize
onMounted(() => {
  const handleResize = () => {
    chartRef.value?.resize()
  }
  window.addEventListener('resize', handleResize)
  return () => window.removeEventListener('resize', handleResize)
})
</script>

<style scoped>
.bookings-trend-chart {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-lg);
  padding: var(--spacing-lg) var(--spacing-xl);
  background: var(--color-background);
  border: 1px solid var(--color-gray-200);
  border-radius: var(--radius-xl);
  box-shadow: var(--shadow-sm);
}

.chart-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: var(--spacing-md);
  flex-wrap: wrap;
}

.header-content {
  flex: 1;
  min-width: 200px;
}

.chart-title {
  margin: 0 0 var(--spacing-xs) 0;
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
}

.chart-subtitle {
  margin: 0;
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}

.chart-controls {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
}

.chart-type-selector {
  display: flex;
  gap: var(--spacing-xs);
  padding: 4px;
  background: var(--color-gray-100);
  border-radius: var(--radius-md);
}

.type-button {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 36px;
  padding: 0;
  background: transparent;
  border: none;
  border-radius: var(--radius-sm);
  color: var(--color-text-secondary);
  cursor: pointer;
  transition: all var(--transition-fast);
}

.type-button svg {
  width: 20px;
  height: 20px;
  stroke-width: 2;
}

.type-button:hover {
  background: var(--color-gray-200);
  color: var(--color-text-primary);
}

.type-button.active {
  background: white;
  color: var(--color-primary-600);
  box-shadow: var(--shadow-sm);
}

.chart-loading {
  width: 100%;
  min-height: 300px;
}

.chart-container {
  width: 100%;
  min-height: 300px;
  position: relative;
}

.echart {
  width: 100%;
  height: 300px;
}

.chart-footer {
  display: flex;
  align-items: center;
  justify-content: space-around;
  gap: var(--spacing-lg);
  padding-top: var(--spacing-md);
  border-top: 1px solid var(--color-gray-200);
}

.footer-stat {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-xs);
  text-align: center;
}

.stat-label {
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-medium);
  color: var(--color-text-secondary);
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.stat-value {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-bold);
  color: var(--color-text-primary);
}

.stat-value.stat-success {
  color: var(--color-success-600);
}

.stat-value.stat-danger {
  color: var(--color-danger-600);
}

/* Mobile responsive */
@media (max-width: 767px) {
  .bookings-trend-chart {
    padding: var(--spacing-md) var(--spacing-lg);
  }

  .chart-header {
    flex-direction: column;
    align-items: stretch;
  }

  .chart-controls {
    justify-content: center;
  }

  .echart {
    height: 250px;
  }

  .chart-footer {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: var(--spacing-md);
  }

  .footer-stat {
    text-align: start;
  }
}

/* Tablet */
@media (min-width: 768px) and (max-width: 1023px) {
  .bookings-trend-chart {
    padding: var(--spacing-md) var(--spacing-lg);
  }

  .echart {
    height: 280px;
  }

  .chart-footer {
    flex-wrap: wrap;
  }
}

/* RTL Support */
[dir='rtl'] .chart-footer {
  flex-direction: row-reverse;
}

/* Print styles */
@media print {
  .bookings-trend-chart {
    border: 1px solid #000;
    box-shadow: none;
    page-break-inside: avoid;
  }

  .chart-controls {
    display: none;
  }
}
</style>
