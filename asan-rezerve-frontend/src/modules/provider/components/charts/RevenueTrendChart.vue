<template>
  <div class="revenue-trend-chart">
    <!-- Chart Header -->
    <div class="chart-header">
      <div class="header-content">
        <h3 class="chart-title">{{ title }}</h3>
        <p v-if="subtitle" class="chart-subtitle">{{ subtitle }}</p>
      </div>
      <div class="chart-controls">
        <div class="time-range-selector">
          <button
            v-for="range in timeRanges"
            :key="range.value"
            :class="['range-button', { active: selectedRange === range.value }]"
            @click="handleRangeChange(range.value)"
          >
            {{ range.label }}
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
        <span class="stat-label">{{ $t('dashboard.charts.total') }}</span>
        <span class="stat-value">{{ formatCurrency(totalRevenue) }}</span>
      </div>
      <div class="footer-stat">
        <span class="stat-label">{{ $t('dashboard.charts.average') }}</span>
        <span class="stat-value">{{ formatCurrency(averageRevenue) }}</span>
      </div>
      <div class="footer-stat">
        <span class="stat-label">{{ $t('dashboard.charts.trend') }}</span>
        <span :class="['stat-value', trendClass]">{{ trendPercentage }}</span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { use } from 'echarts/core'
import { CanvasRenderer } from 'echarts/renderers'
import { LineChart } from 'echarts/charts'
import {
  TitleComponent,
  TooltipComponent,
  GridComponent,
  LegendComponent,
} from 'echarts/components'
import VChart from 'vue-echarts'
import { Skeleton } from '@/shared/components'

// Register ECharts components
use([CanvasRenderer, LineChart, TitleComponent, TooltipComponent, GridComponent, LegendComponent])

interface RevenueDataPoint {
  date: string
  amount: number
}

interface Props {
  title?: string
  subtitle?: string
  data?: RevenueDataPoint[]
  loading?: boolean
  showFooterStats?: boolean
  currency?: string
}

const props = withDefaults(defineProps<Props>(), {
  title: 'Revenue Trend',
  subtitle: '',
  data: () => [],
  loading: false,
  showFooterStats: true,
  currency: 'USD',
})

const chartRef = ref()
const selectedRange = ref<'7d' | '30d' | '90d' | '1y'>('30d')

const timeRanges = [
  { label: '7D', value: '7d' as const },
  { label: '30D', value: '30d' as const },
  { label: '90D', value: '90d' as const },
  { label: '1Y', value: '1y' as const },
]

const emit = defineEmits<{
  (e: 'range-change', range: string): void
}>()

const handleRangeChange = (range: '7d' | '30d' | '90d' | '1y') => {
  selectedRange.value = range
  emit('range-change', range)
}

// Chart theme based on design tokens
const chartTheme = computed(() => ({
  color: ['#667eea', '#10b981', '#f59e0b'],
  backgroundColor: 'transparent',
}))

// Process chart data
const chartData = computed(() => {
  return props.data.map((point) => ({
    date: new Date(point.date).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
    }),
    value: point.amount,
  }))
})

const chartOption = computed(() => ({
  grid: {
    left: '3%',
    right: '4%',
    bottom: '10%',
    top: '10%',
    containLabel: true,
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
      const data = params[0]
      return `
        <div style="padding: 4px 0;">
          <div style="font-weight: 600; margin-bottom: 4px;">${data.name}</div>
          <div style="color: #667eea; font-weight: 600;">${formatCurrency(data.value)}</div>
        </div>
      `
    },
    axisPointer: {
      type: 'line',
      lineStyle: {
        color: '#667eea',
        width: 1,
        type: 'dashed',
      },
    },
  },
  xAxis: {
    type: 'category',
    data: chartData.value.map((d) => d.date),
    boundaryGap: false,
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
      formatter: (value: number) => {
        if (value >= 1000) {
          return `$${(value / 1000).toFixed(1)}k`
        }
        return `$${value}`
      },
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
      name: 'Revenue',
      type: 'line',
      smooth: true,
      symbol: 'circle',
      symbolSize: 8,
      sampling: 'lttb',
      itemStyle: {
        color: '#667eea',
        borderWidth: 2,
      },
      lineStyle: {
        width: 3,
        color: '#667eea',
      },
      areaStyle: {
        color: {
          type: 'linear',
          x: 0,
          y: 0,
          x2: 0,
          y2: 1,
          colorStops: [
            { offset: 0, color: 'rgba(102, 126, 234, 0.3)' },
            { offset: 1, color: 'rgba(102, 126, 234, 0.05)' },
          ],
        },
      },
      emphasis: {
        focus: 'series',
        itemStyle: {
          color: '#667eea',
          borderColor: '#fff',
          borderWidth: 3,
          shadowBlur: 10,
          shadowColor: 'rgba(102, 126, 234, 0.5)',
        },
      },
      data: chartData.value.map((d) => d.value),
    },
  ],
}))

// Footer stats calculations
const totalRevenue = computed(() => {
  return props.data.reduce((sum, point) => sum + point.amount, 0)
})

const averageRevenue = computed(() => {
  if (props.data.length === 0) return 0
  return totalRevenue.value / props.data.length
})

const trendPercentage = computed(() => {
  if (props.data.length < 2) return '0%'

  const halfLength = Math.floor(props.data.length / 2)
  const firstHalf = props.data.slice(0, halfLength)
  const secondHalf = props.data.slice(halfLength)

  const firstAvg = firstHalf.reduce((sum, p) => sum + p.amount, 0) / firstHalf.length
  const secondAvg = secondHalf.reduce((sum, p) => sum + p.amount, 0) / secondHalf.length

  const change = ((secondAvg - firstAvg) / firstAvg) * 100

  if (change > 0) return `+${change.toFixed(1)}%`
  return `${change.toFixed(1)}%`
})

const trendClass = computed(() => {
  const value = parseFloat(trendPercentage.value)
  if (value > 0) return 'trend-positive'
  if (value < 0) return 'trend-negative'
  return 'trend-neutral'
})

const formatCurrency = (amount: number): string => {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: props.currency,
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(amount)
}

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
.revenue-trend-chart {
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

.time-range-selector {
  display: flex;
  gap: var(--spacing-xs);
  padding: 4px;
  background: var(--color-gray-100);
  border-radius: var(--radius-md);
}

.range-button {
  padding: var(--spacing-xs) var(--spacing-sm);
  background: transparent;
  border: none;
  border-radius: var(--radius-sm);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-text-secondary);
  cursor: pointer;
  transition: all var(--transition-fast);
}

.range-button:hover {
  background: var(--color-gray-200);
  color: var(--color-text-primary);
}

.range-button.active {
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

.stat-value.trend-positive {
  color: var(--color-success-600);
}

.stat-value.trend-negative {
  color: var(--color-danger-600);
}

.stat-value.trend-neutral {
  color: var(--color-gray-600);
}

/* Mobile responsive */
@media (max-width: 767px) {
  .revenue-trend-chart {
    padding: var(--spacing-md) var(--spacing-lg);
  }

  .chart-header {
    flex-direction: column;
    align-items: stretch;
  }

  .chart-controls {
    justify-content: center;
  }

  .time-range-selector {
    flex-wrap: wrap;
  }

  .echart {
    height: 250px;
  }

  .chart-footer {
    flex-direction: column;
    gap: var(--spacing-md);
  }

  .footer-stat {
    width: 100%;
    flex-direction: row;
    justify-content: space-between;
    align-items: center;
  }

  .stat-label {
    text-align: start;
  }
}

/* Tablet */
@media (min-width: 768px) and (max-width: 1023px) {
  .revenue-trend-chart {
    padding: var(--spacing-md) var(--spacing-lg);
  }

  .echart {
    height: 280px;
  }
}

/* RTL Support */
[dir='rtl'] .chart-footer {
  flex-direction: row-reverse;
}

/* Print styles */
@media print {
  .revenue-trend-chart {
    border: 1px solid #000;
    box-shadow: none;
    page-break-inside: avoid;
  }

  .chart-controls {
    display: none;
  }
}
</style>
