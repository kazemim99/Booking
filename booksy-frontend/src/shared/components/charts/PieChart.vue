<template>
  <div class="pie-chart-container">
    <Pie :data="chartData" :options="chartOptions" />
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { Pie } from 'vue-chartjs'
import {
  Chart as ChartJS,
  ArcElement,
  Tooltip,
  Legend,
  type ChartOptions,
  type ChartData,
} from 'chart.js'
import { convertEnglishToPersianNumbers } from '@/shared/utils/date/jalali.utils'

// Register Chart.js components
ChartJS.register(ArcElement, Tooltip, Legend)

interface Props {
  data: ChartData<'pie'>
  options?: ChartOptions<'pie'>
  rtl?: boolean
  usePersianNumbers?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  rtl: true,
  usePersianNumbers: true,
})

const chartData = computed(() => props.data)

const chartOptions = computed<ChartOptions<'pie'>>(() => {
  const defaultOptions: ChartOptions<'pie'> = {
    responsive: true,
    maintainAspectRatio: true,
    plugins: {
      legend: {
        position: props.rtl ? 'right' : 'left',
        rtl: props.rtl,
        labels: {
          font: {
            family: 'IRANSans, sans-serif',
          },
          padding: 15,
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
            const label = context.label || ''
            const value = context.parsed || 0
            const persianValue = props.usePersianNumbers
              ? convertEnglishToPersianNumbers(value.toString())
              : value.toString()
            return `${label}: ${persianValue}`
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
  } as ChartOptions<'pie'>
})
</script>

<style scoped>
.pie-chart-container {
  position: relative;
  width: 100%;
  max-width: 400px;
  margin: 0 auto;
}
</style>
