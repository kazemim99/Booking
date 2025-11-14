<template>
  <div class="pie-chart-container">
    <VChart :option="chartOption" :autoresize="true" />
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { use } from 'echarts/core'
import { CanvasRenderer } from 'echarts/renderers'
import { PieChart as EChartsPie } from 'echarts/charts'
import {
  TitleComponent,
  TooltipComponent,
  LegendComponent,
} from 'echarts/components'
import VChart from 'vue-echarts'
import { convertEnglishToPersianNumbers } from '@/shared/utils/date/jalali.utils'

use([
  CanvasRenderer,
  EChartsPie,
  TitleComponent,
  TooltipComponent,
  LegendComponent,
])

interface Dataset {
  data: number[]
  backgroundColor?: string[]
  borderColor?: string
  borderWidth?: number
  [key: string]: any
}

interface ChartData {
  labels: string[]
  datasets: Dataset[]
}

interface Props {
  data: ChartData
  options?: any
  rtl?: boolean
  usePersianNumbers?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  rtl: true,
  usePersianNumbers: true,
})

const chartOption = computed(() => {
  const { labels, datasets } = props.data
  const dataset = datasets[0]

  // Convert to ECharts pie series format
  const seriesData = labels.map((label, index) => ({
    name: label,
    value: dataset.data[index],
    itemStyle: {
      color: dataset.backgroundColor?.[index] || undefined,
      borderColor: dataset.borderColor || '#ffffff',
      borderWidth: dataset.borderWidth || 2,
    },
  }))

  const showLegend = props.options?.plugins?.legend?.display !== false

  return {
    tooltip: {
      trigger: 'item',
      formatter: (params: any) => {
        const value = props.usePersianNumbers
          ? convertEnglishToPersianNumbers(params.value.toString())
          : params.value
        const percent = props.usePersianNumbers
          ? convertEnglishToPersianNumbers(params.percent.toFixed(1))
          : params.percent.toFixed(1)
        return `${params.marker} ${params.name}: ${value} (${percent}%)`
      },
    },
    legend: showLegend
      ? {
          orient: 'vertical',
          right: props.rtl ? undefined : 10,
          left: props.rtl ? 10 : undefined,
          top: 'center',
          textStyle: {
            fontFamily: 'B Nazanin, IRANSans, sans-serif',
          },
        }
      : undefined,
    series: [
      {
        name: 'Data',
        type: 'pie',
        radius: '70%',
        center: ['50%', '50%'],
        data: seriesData,
        emphasis: {
          itemStyle: {
            shadowBlur: 10,
            shadowOffsetX: 0,
            shadowColor: 'rgba(0, 0, 0, 0.5)',
          },
        },
        label: {
          show: !showLegend,
          formatter: (params: any) => {
            const value = props.usePersianNumbers
              ? convertEnglishToPersianNumbers(params.value.toString())
              : params.value
            return `${params.name}\n${value}`
          },
          fontFamily: 'B Nazanin, IRANSans, sans-serif',
        },
      },
    ],
  }
})
</script>

<style scoped>
.pie-chart-container {
  position: relative;
  width: 100%;
  max-width: 400px;
  margin: 0 auto;
  min-height: 300px;
}
</style>
