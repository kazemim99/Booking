<template>
  <a-spin :spinning="system.loading.value">
    <div class="toolbar">
      <a-button @click="system.load">
        <template #icon><reload-outlined /></template>
        {{ t('common.refresh') }}
      </a-button>
    </div>
    <a-alert v-if="system.error.value" type="error" show-icon :message="system.error.value" class="block" closable />

    <template v-if="o">
      <a-row :gutter="[16, 16]">
        <a-col :xs="12" :md="6"><a-card size="small"><a-statistic :title="t('logs.overviewTab.requests')" :value="o.lastHour.requests" /></a-card></a-col>
        <a-col :xs="12" :md="6"><a-card size="small"><a-statistic :title="t('logs.overviewTab.errors')" :value="o.lastHour.serverErrors" :value-style="o.lastHour.serverErrors ? { color: '#cf1322' } : undefined" /></a-card></a-col>
        <a-col :xs="12" :md="6"><a-card size="small"><a-statistic :title="t('logs.overviewTab.errorRate')" :value="(o.lastHour.errorRate * 100).toFixed(2)" suffix="%" /></a-card></a-col>
        <a-col :xs="12" :md="6"><a-card size="small"><a-statistic :title="t('logs.overviewTab.p95')" :value="o.lastHour.p95Ms" /></a-card></a-col>
      </a-row>

      <a-card size="small" :title="t('logs.overviewTab.timeline')" class="block">
        <v-chart v-if="o.last24Hours.length" class="chart" :option="timelineOption" autoresize />
        <a-empty v-else />
      </a-card>

      <a-row :gutter="[16, 16]" class="block">
        <a-col :xs="24" :lg="12">
          <a-card size="small" :title="t('logs.overviewTab.slowRoutes')">
            <a-table :columns="routeColumns" :data-source="o.slowestRoutesLastHour" row-key="route" size="small" :pagination="false" :scroll="{ x: 560 }">
              <template #bodyCell="{ column, record }">
                <code v-if="column.key === 'route'" dir="ltr">{{ record.route }}</code>
              </template>
            </a-table>
          </a-card>
        </a-col>
        <a-col :xs="24" :lg="12">
          <a-card size="small" :title="t('logs.overviewTab.topErrors')">
            <a-table :columns="errorColumns" :data-source="o.topErrorsLastHour" :row-key="errorKey" size="small" :pagination="false" :scroll="{ x: 560 }">
              <template #bodyCell="{ column, record }">
                <template v-if="column.key === 'level'">
                  <a-tag :color="LEVEL_COLORS[record.level as LogLevelName]">{{ t(`logs.levels.${record.level}`) }}</a-tag>
                </template>
                <template v-else-if="column.key === 'template'">
                  <div dir="ltr" class="mono">{{ record.messageTemplate }}</div>
                  <div v-if="record.exceptionType" dir="ltr" class="mono muted">{{ record.exceptionType }}</div>
                </template>
                <template v-else-if="column.key === 'lastSeen'">{{ formatTimestamp(record.lastSeen) }}</template>
              </template>
            </a-table>
          </a-card>
        </a-col>
      </a-row>

      <a-card size="small" :title="t('logs.overviewTab.cache')" class="block">
        <a-row :gutter="[16, 16]">
          <a-col :xs="24" :md="12">
            <a-descriptions :title="t('logs.overviewTab.l2')" :column="1" size="small" bordered>
              <a-descriptions-item :label="t('logs.overviewTab.circuit')">
                <a-badge :status="o.cache.l2.circuit === 'Closed' ? 'success' : o.cache.l2.circuit === 'Open' ? 'error' : 'warning'" :text="t(`logs.overviewTab.circuits.${o.cache.l2.circuit}`)" />
              </a-descriptions-item>
              <a-descriptions-item :label="t('logs.overviewTab.store')"><code dir="ltr">{{ o.cache.l2.store }}</code></a-descriptions-item>
              <a-descriptions-item v-if="o.cache.l2.redisEndpoints" :label="t('logs.overviewTab.connected')">
                <span dir="ltr">{{ o.cache.l2.redisEndpoints }}</span> · {{ o.cache.l2.redisConnected ? '✓' : '✗' }}
              </a-descriptions-item>
              <a-descriptions-item v-if="o.cache.l2.lastError" :label="t('logs.overviewTab.lastError')"><span dir="ltr" class="mono">{{ o.cache.l2.lastError }}</span></a-descriptions-item>
            </a-descriptions>
          </a-col>
          <a-col v-if="o.cache.l1" :xs="24" :md="12">
            <a-descriptions :title="t('logs.overviewTab.l1')" :column="1" size="small" bordered>
              <a-descriptions-item :label="t('logs.overviewTab.entries')">{{ o.cache.l1.entries }}</a-descriptions-item>
              <a-descriptions-item :label="t('logs.overviewTab.hits')">{{ o.cache.l1.hits }}</a-descriptions-item>
              <a-descriptions-item :label="t('logs.overviewTab.misses')">{{ o.cache.l1.misses }}</a-descriptions-item>
            </a-descriptions>
          </a-col>
        </a-row>

        <a-table :columns="cacheColumns" :data-source="o.cache.regions" row-key="region" size="small" :pagination="false" class="block">
          <template #bodyCell="{ column, record }">
            <code v-if="column.key === 'region'" dir="ltr">{{ record.region }}</code>
            <a-progress v-else-if="column.key === 'hitRatio'" :percent="Math.round(record.hitRatio * 100)" size="small" />
          </template>
        </a-table>

        <div class="purge">
          <a-input v-model:value="purgeTag" dir="ltr" class="tag-input" :placeholder="t('logs.overviewTab.tagPlaceholder')" />
          <a-button :disabled="!purgeTag.trim()" :loading="system.purging.value" @click="purge(purgeTag)">{{ t('logs.overviewTab.purgeTag') }}</a-button>
          <a-popconfirm :title="t('logs.overviewTab.purgeConfirm')" @confirm="purge(null)">
            <a-button danger :loading="system.purging.value">{{ t('logs.overviewTab.purgeAll') }}</a-button>
          </a-popconfirm>
        </div>
      </a-card>

      <a-row :gutter="[16, 16]" class="block">
        <a-col :xs="24" :md="12">
          <a-card size="small" :title="t('logs.overviewTab.logStore')">
            <a-descriptions v-if="o.logStore" :column="2" size="small">
              <a-descriptions-item :label="t('logs.overviewTab.logStore')">
                <a-badge :status="o.logStore.ready ? 'success' : 'error'" :text="o.logStore.ready ? t('logs.overviewTab.ready') : t('logs.overviewTab.notReady')" />
              </a-descriptions-item>
              <a-descriptions-item :label="t('logs.overviewTab.written')">{{ o.logStore.written }}</a-descriptions-item>
              <a-descriptions-item :label="t('logs.overviewTab.dropped')">{{ o.logStore.dropped }}</a-descriptions-item>
              <a-descriptions-item :label="t('logs.overviewTab.failedBatches')">{{ o.logStore.failedBatches }}</a-descriptions-item>
              <a-descriptions-item :label="t('logs.overviewTab.queue')">{{ o.logStore.queueLength }}</a-descriptions-item>
              <template v-if="o.logStorage">
                <a-descriptions-item :label="t('logs.overviewTab.storageSize')">{{ formatBytes(o.logStorage.totalBytes) }}</a-descriptions-item>
                <a-descriptions-item :label="t('logs.overviewTab.storageDays')">
                  <span dir="ltr">{{ o.logStorage.oldestDay ?? '—' }} → {{ o.logStorage.newestDay ?? '—' }}</span>
                  ({{ o.logStorage.partitions }})
                </a-descriptions-item>
              </template>
              <a-descriptions-item v-if="o.logStore.lastError" :label="t('logs.overviewTab.lastError')" :span="2"><span dir="ltr" class="mono">{{ o.logStore.lastError }}</span></a-descriptions-item>
            </a-descriptions>
          </a-card>
        </a-col>
        <a-col :xs="24" :md="12">
          <a-card size="small" :title="t('logs.overviewTab.process')">
            <a-descriptions :column="2" size="small">
              <a-descriptions-item :label="t('logs.overviewTab.environment')">{{ o.environment }}</a-descriptions-item>
              <a-descriptions-item :label="t('logs.overviewTab.version')"><span dir="ltr">{{ o.version }}</span></a-descriptions-item>
              <a-descriptions-item :label="t('logs.overviewTab.startedAt')">{{ formatTimestamp(o.startedAt) }}</a-descriptions-item>
              <a-descriptions-item :label="t('logs.overviewTab.workingSet')">{{ o.process.workingSetMb }}</a-descriptions-item>
              <a-descriptions-item :label="t('logs.overviewTab.gcHeap')">{{ o.process.gcHeapMb }}</a-descriptions-item>
              <a-descriptions-item :label="t('logs.overviewTab.threads')">{{ o.process.threadPoolThreads }}</a-descriptions-item>
              <a-descriptions-item :label="t('logs.overviewTab.pending')">{{ o.process.pendingWorkItems }}</a-descriptions-item>
              <a-descriptions-item :label="t('logs.overviewTab.cpu')">{{ o.process.cpuSeconds }}</a-descriptions-item>
            </a-descriptions>
          </a-card>
        </a-col>
      </a-row>

      <a-card v-if="o.counters.length" size="small" :title="t('logs.overviewTab.counters')" class="block">
        <a-space wrap>
          <a-tag v-for="c in o.counters" :key="`${c.meter}/${c.name}`" dir="ltr">{{ c.name }}: {{ c.total }}</a-tag>
        </a-space>
      </a-card>
    </template>

    <a-card size="small" :title="t('logs.overviewTab.digest')" class="block">
      <p class="muted">{{ t('logs.overviewTab.digestHint') }}</p>
      <div class="purge">
        <a-input v-model:value="digestSource" dir="ltr" class="tag-input" :placeholder="t('logs.overviewTab.sourcePlaceholder')" />
        <a-button type="primary" :loading="system.digestLoading.value" @click="system.loadDigest(digestSource)">
          {{ t('logs.overviewTab.prepareDigest') }}
        </a-button>
        <a-button v-if="system.digest.value" @click="copyDigest">{{ t('logs.overviewTab.copyDigest') }}</a-button>
      </div>
      <pre v-if="system.digest.value" class="digest" dir="ltr">{{ system.digest.value }}</pre>
    </a-card>
  </a-spin>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { message } from 'ant-design-vue'
import { ReloadOutlined } from '@ant-design/icons-vue'
import { use } from 'echarts/core'
import { CanvasRenderer } from 'echarts/renderers'
import { BarChart } from 'echarts/charts'
import { GridComponent, LegendComponent, TooltipComponent } from 'echarts/components'
import VChart from 'vue-echarts'
import type { ErrorGroup, LogLevelName } from '../../../api/observability.api'
import { formatBytes } from '../../../utils/bytes'
import { formatTimestamp } from '../../../utils/date'
import { useSystemOverview } from '../useSystemOverview'
import { LEVEL_COLORS } from './levelColors'

use([CanvasRenderer, BarChart, GridComponent, LegendComponent, TooltipComponent])

const { t } = useI18n()
const system = useSystemOverview()
const o = computed(() => system.overview.value)
const purgeTag = ref('')
const digestSource = ref('')

const routeColumns = computed(() => [
  { title: t('logs.overviewTab.route'), key: 'route' },
  { title: t('logs.overviewTab.requests'), dataIndex: 'requests', key: 'requests', width: 80 },
  { title: t('logs.overviewTab.errors'), dataIndex: 'serverErrors', key: 'serverErrors', width: 80 },
  { title: t('logs.overviewTab.p50'), dataIndex: 'p50Ms', key: 'p50', width: 90 },
  { title: t('logs.overviewTab.p95'), dataIndex: 'p95Ms', key: 'p95', width: 90 },
])

const errorColumns = computed(() => [
  { title: t('logs.columns.level'), key: 'level', width: 100 },
  { title: t('logs.detail.template'), key: 'template' },
  { title: t('logs.overviewTab.count'), dataIndex: 'count', key: 'count', width: 70 },
  { title: t('logs.overviewTab.lastSeen'), key: 'lastSeen', width: 140 },
])

const cacheColumns = computed(() => [
  { title: t('logs.overviewTab.region'), key: 'region' },
  { title: t('logs.overviewTab.requests'), dataIndex: 'requests', key: 'requests', width: 100 },
  { title: t('logs.overviewTab.hits'), dataIndex: 'hits', key: 'hits', width: 100 },
  { title: t('logs.overviewTab.misses'), dataIndex: 'misses', key: 'misses', width: 100 },
  { title: t('logs.overviewTab.hitRatio'), key: 'hitRatio', width: 200 },
])

const timelineOption = computed(() => {
  const buckets = o.value?.last24Hours ?? []
  return {
    tooltip: { trigger: 'axis' },
    legend: {},
    grid: { left: 40, right: 16, top: 32, bottom: 24 },
    xAxis: { type: 'category', data: buckets.map((b) => new Date(b.hour).getHours().toString().padStart(2, '0') + ':00') },
    yAxis: { type: 'value' },
    series: [
      { name: t('logs.overviewTab.information'), type: 'bar', stack: 'events', data: buckets.map((b) => b.information), itemStyle: { color: '#1677ff' } },
      { name: t('logs.overviewTab.warnings'), type: 'bar', stack: 'events', data: buckets.map((b) => b.warnings), itemStyle: { color: '#fa8c16' } },
      { name: t('logs.overviewTab.errorsSeries'), type: 'bar', stack: 'events', data: buckets.map((b) => b.errors), itemStyle: { color: '#cf1322' } },
    ],
  }
})

function errorKey(record: ErrorGroup): string {
  return `${record.level}|${record.sourceContext}|${record.messageTemplate}|${record.exceptionType}`
}

async function purge(tag: string | null) {
  if (await system.purge(tag)) {
    purgeTag.value = ''
    message.success(t('logs.overviewTab.purged'))
  }
}

async function copyDigest() {
  await navigator.clipboard.writeText(system.digest.value)
  message.success(t('logs.overviewTab.copied'))
}

onMounted(system.load)
</script>

<style scoped>
.toolbar {
  display: flex;
  justify-content: flex-end;
  margin-bottom: 16px;
}
.block {
  margin-top: 16px;
}
.chart {
  height: 260px;
}
.purge {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-top: 16px;
}
.tag-input {
  width: 360px;
}
.mono {
  font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
  font-size: 12px;
}
.muted {
  color: rgba(0, 0, 0, 0.45);
}
.digest {
  margin-top: 12px;
  background: rgba(0, 0, 0, 0.04);
  padding: 12px;
  border-radius: 6px;
  white-space: pre-wrap;
  max-height: 420px;
  overflow: auto;
  font-size: 12px;
}
</style>
