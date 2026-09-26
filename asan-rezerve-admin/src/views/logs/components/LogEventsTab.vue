<template>
  <div>
    <div class="filters">
      <a-select v-model:value="logs.range.value" class="filter" @change="logs.applyFilters">
        <a-select-option v-for="r in TIME_RANGES" :key="r" :value="r">{{ t(`logs.filters.ranges.${r}`) }}</a-select-option>
      </a-select>
      <a-select v-model:value="logs.minLevel.value" class="filter" @change="logs.applyFilters">
        <a-select-option value="all">{{ t('logs.filters.allLevels') }}</a-select-option>
        <a-select-option v-for="level in LOG_LEVELS" :key="level" :value="level">{{ t(`logs.levels.${level}`) }}</a-select-option>
      </a-select>
      <a-input-search
        v-model:value="logs.search.value"
        class="filter wide"
        allow-clear
        :placeholder="t('logs.filters.search')"
        @search="logs.applyFilters"
      />
      <a-input
        v-model:value="logs.source.value"
        class="filter"
        dir="ltr"
        allow-clear
        :placeholder="t('logs.filters.source')"
        @press-enter="logs.applyFilters"
      />
      <a-input
        v-model:value="logs.traceId.value"
        class="filter"
        dir="ltr"
        allow-clear
        :placeholder="t('logs.filters.traceId')"
        @press-enter="logs.applyFilters"
      />
      <a-input-number
        v-model:value="logs.statusCode.value"
        class="filter narrow"
        :min="100"
        :max="599"
        :placeholder="t('logs.filters.statusCode')"
        @press-enter="logs.applyFilters"
      />
      <a-button type="primary" :loading="logs.loading.value" @click="logs.applyFilters">
        <template #icon><search-outlined /></template>
        {{ t('common.search') }}
      </a-button>
      <a-space class="end">
        <span>{{ t('logs.filters.autoRefresh') }}</span>
        <a-switch :checked="logs.autoRefresh.value" @change="(on: boolean | string | number) => logs.setAutoRefresh(Boolean(on))" />
        <a-button :loading="exporting" @click="download">
          <template #icon><download-outlined /></template>
          {{ t('logs.filters.export') }}
        </a-button>
      </a-space>
    </div>

    <a-alert v-if="logs.error.value" type="error" show-icon :message="logs.error.value" class="alert">
      <template v-if="!logs.loaded.value" #action>
        <a-button size="small" @click="logs.load">{{ t('logs.retry') }}</a-button>
      </template>
    </a-alert>

    <a-table
      :columns="columns"
      :data-source="logs.items.value"
      :loading="logs.loading.value"
      :pagination="pagination"
      row-key="id"
      size="small"
      :scroll="{ x: 1100 }"
      @change="(pg: { current?: number }) => logs.goTo(pg.current ?? 1)"
    >
      <template #emptyText><a-empty :description="t('logs.empty')" /></template>
      <template #bodyCell="{ column, record }">
        <template v-if="column.key === 'timestamp'">
          <span class="nowrap">{{ formatTimestamp(record.timestamp) }}</span>
        </template>
        <template v-else-if="column.key === 'level'">
          <a-tag :color="LEVEL_COLORS[record.level as LogLevelName]">{{ t(`logs.levels.${record.level}`) }}</a-tag>
        </template>
        <template v-else-if="column.key === 'message'">
          <span class="message" dir="auto">
            <warning-outlined v-if="record.hasException" class="exception-mark" />
            {{ record.message }}
          </span>
        </template>
        <template v-else-if="column.key === 'source'">
          <span class="mono" dir="ltr">{{ shortSource(record.sourceContext) }}</span>
        </template>
        <template v-else-if="column.key === 'request'">
          <span v-if="record.requestPath" class="mono" dir="ltr">{{ record.requestPath }}</span>
        </template>
        <template v-else-if="column.key === 'status'">
          <a-tag v-if="record.statusCode" :color="record.statusCode >= 500 ? 'red' : record.statusCode >= 400 ? 'orange' : 'green'">
            {{ record.statusCode }}
          </a-tag>
        </template>
        <template v-else-if="column.key === 'actions'">
          <a-space>
            <a-button type="link" size="small" @click="logs.open(record.id)">{{ t('logs.actions.view') }}</a-button>
            <a-button v-if="record.traceId" type="link" size="small" @click="logs.openTrace(record.traceId)">
              {{ t('logs.actions.trace') }}
            </a-button>
          </a-space>
        </template>
      </template>
    </a-table>

    <a-drawer
      :open="!!logs.detail.value"
      :title="t('logs.detail.title')"
      width="720"
      @close="logs.closeDetail"
    >
      <template v-if="logs.detail.value">
        <a-descriptions :column="1" bordered size="small">
          <a-descriptions-item :label="t('logs.columns.timestamp')">{{ formatTimestamp(logs.detail.value.timestamp) }}</a-descriptions-item>
          <a-descriptions-item :label="t('logs.columns.level')">
            <a-tag :color="LEVEL_COLORS[logs.detail.value.level]">{{ t(`logs.levels.${logs.detail.value.level}`) }}</a-tag>
          </a-descriptions-item>
          <a-descriptions-item :label="t('logs.columns.message')"><span dir="auto">{{ logs.detail.value.message }}</span></a-descriptions-item>
          <a-descriptions-item :label="t('logs.detail.template')"><code dir="ltr">{{ logs.detail.value.messageTemplate }}</code></a-descriptions-item>
          <a-descriptions-item :label="t('logs.detail.source')"><code dir="ltr">{{ logs.detail.value.sourceContext }}</code></a-descriptions-item>
          <a-descriptions-item v-if="logs.detail.value.requestPath" :label="t('logs.columns.request')">
            <code dir="ltr">{{ logs.detail.value.requestPath }}</code>
            <span v-if="logs.detail.value.routeTemplate" class="muted" dir="ltr"> ({{ logs.detail.value.routeTemplate }})</span>
          </a-descriptions-item>
          <a-descriptions-item v-if="logs.detail.value.userId" :label="t('logs.detail.user')"><code dir="ltr">{{ logs.detail.value.userId }}</code></a-descriptions-item>
          <a-descriptions-item v-if="logs.detail.value.traceId" :label="t('logs.detail.trace')">
            <a-typography-text :copyable="{ text: logs.detail.value.traceId }" code>{{ logs.detail.value.traceId }}</a-typography-text>
            <a-button type="link" size="small" @click="logs.openTrace(logs.detail.value.traceId!)">{{ t('logs.detail.openTrace') }}</a-button>
          </a-descriptions-item>
        </a-descriptions>
        <template v-if="logs.detail.value.exception">
          <h4 class="section">{{ t('logs.detail.exception') }}</h4>
          <pre class="code" dir="ltr">{{ logs.detail.value.exception }}</pre>
        </template>
        <template v-if="logs.detail.value.properties">
          <h4 class="section">{{ t('logs.detail.properties') }}</h4>
          <pre class="code" dir="ltr">{{ JSON.stringify(logs.detail.value.properties, null, 2) }}</pre>
        </template>
      </template>
    </a-drawer>

    <a-modal
      :open="!!logs.traceOpen.value"
      :title="t('logs.trace.title')"
      width="960px"
      :footer="null"
      @cancel="logs.closeTrace"
    >
      <p class="mono muted" dir="ltr">{{ logs.traceOpen.value }}</p>
      <a-spin :spinning="logs.traceLoading.value">
        <a-empty v-if="!logs.traceLoading.value && logs.traceEvents.value.length === 0" :description="t('logs.trace.empty')" />
        <a-timeline v-else>
          <a-timeline-item
            v-for="event in logs.traceEvents.value"
            :key="event.id"
            :color="event.level === 'Error' || event.level === 'Fatal' ? 'red' : event.level === 'Warning' ? 'orange' : 'blue'"
          >
            <div class="nowrap muted">{{ formatTimestamp(event.timestamp) }} · <span dir="ltr">{{ shortSource(event.sourceContext) }}</span></div>
            <div dir="auto">{{ event.message }}</div>
            <pre v-if="event.exception" class="code small" dir="ltr">{{ event.exception }}</pre>
          </a-timeline-item>
        </a-timeline>
      </a-spin>
    </a-modal>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { DownloadOutlined, SearchOutlined, WarningOutlined } from '@ant-design/icons-vue'
import { LOG_LEVELS, type LogLevelName } from '../../../api/observability.api'
import { formatTimestamp } from '../../../utils/date'
import { TIME_RANGES, useLogExplorer } from '../useLogExplorer'
import { LEVEL_COLORS } from './levelColors'

const PAGE_SIZE = 50

const { t } = useI18n()
const logs = useLogExplorer(PAGE_SIZE)
const exporting = ref(false)

const columns = computed(() => [
  { title: t('logs.columns.timestamp'), key: 'timestamp', width: 150 },
  { title: t('logs.columns.level'), key: 'level', width: 110 },
  { title: t('logs.columns.message'), key: 'message' },
  { title: t('logs.columns.source'), key: 'source', width: 200 },
  { title: t('logs.columns.request'), key: 'request', width: 200 },
  { title: t('logs.columns.status'), key: 'status', width: 80 },
  { title: t('logs.columns.elapsed'), dataIndex: 'elapsedMs', key: 'elapsed', width: 100 },
  { title: t('logs.columns.actions'), key: 'actions', width: 170 },
])

const pagination = computed(() => ({
  current: logs.page.value,
  pageSize: PAGE_SIZE,
  total: logs.totalCount.value,
  showSizeChanger: false,
  showTotal: (total: number) => t('logs.totalCount', { n: total }),
}))

/** `AsanRezerve.ServiceCatalog.Application.Commands.CreateBookingHandler` → `…Commands.CreateBookingHandler`. */
function shortSource(source?: string | null): string {
  if (!source) return ''
  const parts = source.split('.')
  return parts.length <= 2 ? source : `…${parts.slice(-2).join('.')}`
}

async function download() {
  exporting.value = true
  try {
    const blob = await logs.exportLogs()
    if (!blob) return
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = `asanrezerve-logs-${new Date().toISOString().replace(/[:.]/g, '-')}.ndjson`
    link.click()
    URL.revokeObjectURL(url)
  } finally {
    exporting.value = false
  }
}

onMounted(logs.load)
</script>

<style scoped>
.filters {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-bottom: 16px;
  align-items: center;
}
.filter {
  width: 170px;
}
.filter.wide {
  width: 260px;
}
.filter.narrow {
  width: 120px;
}
.end {
  margin-inline-start: auto;
}
.alert {
  margin-bottom: 16px;
}
.nowrap {
  white-space: nowrap;
}
.message {
  word-break: break-word;
}
.exception-mark {
  color: #cf1322;
  margin-inline-end: 4px;
}
.mono,
.code {
  font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
  font-size: 12px;
}
.code {
  background: rgba(0, 0, 0, 0.04);
  padding: 12px;
  border-radius: 6px;
  white-space: pre-wrap;
  word-break: break-all;
  max-height: 360px;
  overflow: auto;
}
.code.small {
  max-height: 160px;
}
.muted {
  color: rgba(0, 0, 0, 0.45);
}
.section {
  margin-top: 16px;
}
</style>
