<template>
  <div class="line-chart-container">
    <Line :data="chartData" :options="chartOptions" />
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { Line } from 'vue-chartjs'
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
  Filler,
  type ChartOptions,
  type ChartData,
} from 'chart.js'
import { convertEnglishToPersianNumbers } from '@/shared/utils/date/jalali.utils'

// Register Chart.js components
ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
  Filler
)

interface Props {
  data: ChartData<'line'>
  options?: ChartOptions<'line'>
  rtl?: boolean
  usePersianNumbers?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  rtl: true,
  usePersianNumbers: true,
})

const chartData = computed(() => props.data)

const chartOptions = computed<ChartOptions<'line'>>(() => {
  const defaultOptions: ChartOptions<'line'> = {
    responsive: true,
    maintainAspectRatio: true,
    interaction: {
      mode: 'index',
      intersect: false,
    },
    plugins: {
      legend: {
        position: props.rtl ? 'right' : 'left',
        rtl: props.rtl,
        labels: {
          font: {
            family: 'IRANSans, sans-serif',
          },
          padding: 15,
          usePointStyle: true,
        },
      },
      tooltip: {
        rtl: props.rtl,
        titleFont: {
          family: 'IRANSans, sans-serif',
        },
        bodyFont: {
          family: 'IRANSans, sans-serif',
        },
        callbacks: {
          label: (context) => {
            const label = context.dataset.label || ''
            const value = context.parsed.y || 0
            const persianValue = props.usePersianNumbers
              ? convertEnglishToPersianNumbers(value.toString())
              : value.toString()
            return `${label}: ${persianValue}`
          },
          title: (tooltipItems) => {
            const title = tooltipItems[0]?.label || ''
            return props.usePersianNumbers
              ? convertEnglishToPersianNumbers(title)
              : title
          },
        },
      },
    },
    scales: {
      x: {
        grid: {
          display: false,
        },
        ticks: {
          font: {
            family: 'IRANSans, sans-serif',
          },
          callback: function (value) {
            const label = this.getLabelForValue(value as number)
            return props.usePersianNumbers
              ? convertEnglishToPersianNumbers(label)
              : label
          },
        },
      },
      y: {
        beginAtZero: true,
        ticks: {
          font: {
            family: 'IRANSans, sans-serif',
          },
          callback: function (value) {
            return props.usePersianNumbers
              ? convertEnglishToPersianNumbers(value.toString())
              : value.toString()
          },
        },
      },
    },
  }

  // Merge with custom options
  return {
    ...defaultOptions,
    ...props.options,
    plugins: {
      ...defaultOptions.plugins,
      ...props.options?.plugins,
    },
    scales: {
      ...defaultOptions.scales,
      ...props.options?.scales,
    },
  } as ChartOptions<'line'>
})
</script>

<style scoped>
.line-chart-container {
  position: relative;
  width: 100%;
  min-height: 300px;
}
</style>
