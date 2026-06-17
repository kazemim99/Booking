<template>
  <div class="page-container">
    <a-page-header :title="$t('dashboard.title')" :sub-title="$t('dashboard.subTitle')" />

    <a-spin :spinning="loading">
      <a-row :gutter="[16, 16]">
        <a-col :xs="24" :sm="12" :lg="6">
          <a-card class="stats-card">
            <a-statistic
              :title="$t('dashboard.totalUsers')"
              :value="stats.totalUsers"
              :prefix="h(UserOutlined)"
              :value-style="{ color: '#3f8600' }"
            />
          </a-card>
        </a-col>

        <a-col :xs="24" :sm="12" :lg="6">
          <a-card class="stats-card">
            <a-statistic
              :title="$t('dashboard.totalProviders')"
              :value="stats.totalProviders"
              :prefix="h(ShopOutlined)"
              :value-style="{ color: '#1890ff' }"
            />
            <div v-if="stats.pendingProviders > 0" class="sub-stat">
              <a-badge :count="stats.pendingProviders" :overflow-count="99">
                <span>{{ $t('dashboard.pendingApproval') }}</span>
              </a-badge>
            </div>
          </a-card>
        </a-col>

        <a-col :xs="24" :sm="12" :lg="6">
          <a-card class="stats-card">
            <a-statistic
              :title="$t('dashboard.totalBookings')"
              :value="stats.totalBookings"
              :prefix="h(CalendarOutlined)"
              :value-style="{ color: '#cf1322' }"
            />
            <div class="sub-stat">
              {{ $t('dashboard.today') }}: {{ stats.todayBookings }}
            </div>
          </a-card>
        </a-col>

        <a-col :xs="24" :sm="12" :lg="6">
          <a-card class="stats-card">
            <a-statistic
              :title="$t('dashboard.totalRevenue')"
              :value="stats.totalRevenue"
              :precision="2"
              prefix="$"
              :value-style="{ color: '#faad14' }"
            />
            <div class="sub-stat">
              {{ $t('dashboard.monthlyGrowth') }}: {{ stats.monthlyGrowth }}%
            </div>
          </a-card>
        </a-col>
      </a-row>

      <a-row :gutter="[16, 16]" style="margin-top: 16px">
        <a-col :xs="24" :lg="12">
          <div class="chart-container">
            <h3>{{ $t('dashboard.userGrowth') }}</h3>
            <div style="height: 300px">
              <v-chart :option="userGrowthOption" autoresize />
            </div>
          </div>
        </a-col>

        <a-col :xs="24" :lg="12">
          <div class="chart-container">
            <h3>{{ $t('dashboard.bookingTrends') }}</h3>
            <div style="height: 300px">
              <v-chart :option="bookingTrendsOption" autoresize />
            </div>
          </div>
        </a-col>
      </a-row>

      <a-row :gutter="[16, 16]" style="margin-top: 16px">
        <a-col :xs="24" :lg="16">
          <div class="chart-container">
            <h3>{{ $t('dashboard.recentActivity') }}</h3>
            <a-table
              :columns="activityColumns"
              :data-source="recentActivity"
              :pagination="false"
              size="small"
            />
          </div>
        </a-col>

        <a-col :xs="24" :lg="8">
          <div class="chart-container">
            <h3>{{ $t('dashboard.quickActions') }}</h3>
            <a-space direction="vertical" style="width: 100%">
              <a-button type="primary" block @click="$router.push('/providers')">
                <team-outlined /> {{ $t('dashboard.manageProviders') }}
              </a-button>
              <a-button block @click="$router.push('/users')">
                <user-outlined /> {{ $t('dashboard.manageUsers') }}
              </a-button>
              <a-button block @click="$router.push('/analytics')">
                <bar-chart-outlined /> {{ $t('dashboard.viewAnalytics') }}
              </a-button>
              <a-button block @click="$router.push('/logs')">
                <file-text-outlined /> {{ $t('dashboard.systemLogs') }}
              </a-button>
            </a-space>
          </div>
        </a-col>
      </a-row>
    </a-spin>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, h, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { use } from 'echarts/core'
import { CanvasRenderer } from 'echarts/renderers'
import { LineChart, BarChart } from 'echarts/charts'
import {
  TitleComponent,
  TooltipComponent,
  LegendComponent,
  GridComponent,
} from 'echarts/components'
import VChart from 'vue-echarts'
import {
  UserOutlined,
  ShopOutlined,
  CalendarOutlined,
  TeamOutlined,
  BarChartOutlined,
  FileTextOutlined,
} from '@ant-design/icons-vue'
import { analyticsApi } from '../../api/analytics.api'
import type { DashboardStats } from '../../types'

const { t } = useI18n()

use([
  CanvasRenderer,
  LineChart,
  BarChart,
  TitleComponent,
  TooltipComponent,
  LegendComponent,
  GridComponent,
])

const loading = ref(false)
const stats = ref<DashboardStats>({
  totalUsers: 0,
  totalProviders: 0,
  totalBookings: 0,
  totalRevenue: 0,
  activeUsers: 0,
  pendingProviders: 0,
  todayBookings: 0,
  monthlyGrowth: 0,
})

const userGrowthOption = ref({
  tooltip: {
    trigger: 'axis',
  },
  xAxis: {
    type: 'category',
    data: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun'],
  },
  yAxis: {
    type: 'value',
  },
  series: [
    {
      name: 'Users',
      type: 'line',
      data: [120, 200, 350, 580, 750, 920],
      smooth: true,
      itemStyle: { color: '#1890ff' },
    },
  ],
})

const bookingTrendsOption = ref({
  tooltip: {
    trigger: 'axis',
  },
  xAxis: {
    type: 'category',
    data: ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'],
  },
  yAxis: {
    type: 'value',
  },
  series: [
    {
      name: 'Bookings',
      type: 'bar',
      data: [45, 62, 58, 73, 89, 95, 67],
      itemStyle: { color: '#52c41a' },
    },
  ],
})

const activityColumns = computed(() => [
  { title: t('order.customer'), dataIndex: 'user', key: 'user' },
  { title: t('logs.action'), dataIndex: 'action', key: 'action' },
  { title: t('logs.timestamp'), dataIndex: 'time', key: 'time' },
])

const recentActivity = ref([
  { key: '1', user: 'john@example.com', action: t('dashboard.createdAccount'), time: '2 mins ago' },
  { key: '2', user: 'Provider #12', action: t('dashboard.updatedServices'), time: '5 mins ago' },
  { key: '3', user: 'jane@example.com', action: t('dashboard.completedBooking'), time: '12 mins ago' },
  { key: '4', user: 'Provider #8', action: t('dashboard.requestedApproval'), time: '23 mins ago' },
  { key: '5', user: 'admin', action: t('dashboard.approvedProvider'), time: '34 mins ago' },
])

const loadDashboardData = async () => {
  loading.value = true
  try {
    stats.value = await analyticsApi.getDashboardStats()
  } catch (error) {
    console.error('Failed to load dashboard stats:', error)
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  loadDashboardData()
})
</script>

<style scoped>
.sub-stat {
  margin-top: 8px;
  font-size: 12px;
  color: #666;
}
</style>
