<template>
  <div class="line-chart-container">
    <VChart :option="chartOption" :autoresize="true" />
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { use } from 'echarts/core'
import { CanvasRenderer } from 'echarts/renderers'
import { LineChart as EChartsLine } from 'echarts/charts'
import {
  TitleComponent,
  TooltipComponent,
  GridComponent,
  LegendComponent,
} from 'echarts/components'
import VChart from 'vue-echarts'
import { convertEnglishToPersianNumbers } from '@/shared/utils/date/jalali.utils'

use([
  CanvasRenderer,
  EChartsLine,
  TitleComponent,
  TooltipComponent,
  GridComponent,
  LegendComponent,
])

interface Dataset {
  label: string
  data: number[]
  borderColor?: string
  backgroundColor?: string
  borderWidth?: number
  tension?: number
  fill?: boolean
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

  // Convert series data
  const series = datasets.map((dataset) => ({
    name: dataset.label,
    type: 'line',
    data: dataset.data,
    smooth: dataset.tension ? true : false,
    smoothMonotonicity: 'x',
    lineStyle: {
      color: dataset.borderColor || '#1976d2',
      width: dataset.borderWidth || 2,
    },
    itemStyle: {
      color: dataset.borderColor || '#1976d2',
    },
    areaStyle: dataset.fill
      ? {
          color: dataset.backgroundColor || 'rgba(25, 118, 210, 0.1)',
        }
      : undefined,
    symbol: 'circle',
    symbolSize: 6,
  }))

  return {
    tooltip: {
      trigger: 'axis',
      axisPointer: {
        type: 'cross',
        label: {
          backgroundColor: '#6a7985',
        },
      },
      formatter: (params: any) => {
        if (!Array.isArray(params)) params = [params]
        let result = params[0].axisValueLabel
        if (props.usePersianNumbers) {
          result = convertEnglishToPersianNumbers(result)
        }
        result += '<br/>'
        params.forEach((param: any) => {
          const value = props.usePersianNumbers
            ? convertEnglishToPersianNumbers(param.value.toString())
            : param.value
          result += `${param.marker} ${param.seriesName}: ${value}<br/>`
        })
        return result
      },
    },
    legend: {
      data: datasets.map((d) => d.label),
      right: props.rtl ? undefined : 10,
      left: props.rtl ? 10 : undefined,
      textStyle: {
        fontFamily: 'B Nazanin, IRANSans, sans-serif',
      },
    },
    grid: {
      left: '3%',
      right: '4%',
      bottom: '3%',
      top: '10%',
      containLabel: true,
    },
    xAxis: {
      type: 'category',
      boundaryGap: false,
      data: labels,
      axisLabel: {
        formatter: (value: string) =>
          props.usePersianNumbers ? convertEnglishToPersianNumbers(value) : value,
        fontFamily: 'B Nazanin, IRANSans, sans-serif',
      },
      axisLine: {
        lineStyle: {
          color: 'rgba(0, 0, 0, 0.12)',
        },
      },
    },
    yAxis: {
      type: 'value',
      axisLabel: {
        formatter: (value: number) =>
          props.usePersianNumbers
            ? convertEnglishToPersianNumbers(value.toString())
            : value.toString(),
        fontFamily: 'B Nazanin, IRANSans, sans-serif',
      },
      splitLine: {
        lineStyle: {
          color: 'rgba(0, 0, 0, 0.06)',
        },
      },
    },
    series,
  }
})
</script>

<style scoped>
.line-chart-container {
  position: relative;
  width: 100%;
  min-height: 300px;
}
</style>
