<template>
  <div class="page-container">
    <a-page-header :title="t('logs.title')" :sub-title="t('logs.subtitle')" />
    <a-card>
      <a-tabs v-model:active-key="tab" destroy-inactive-tab-pane @change="remember">
        <a-tab-pane key="events" :tab="t('logs.tabs.events')">
          <log-events-tab />
        </a-tab-pane>
        <a-tab-pane key="levels" :tab="t('logs.tabs.levels')">
          <log-levels-tab />
        </a-tab-pane>
        <a-tab-pane key="overview" :tab="t('logs.tabs.overview')">
          <system-overview-tab />
        </a-tab-pane>
      </a-tabs>
    </a-card>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import LogEventsTab from './components/LogEventsTab.vue'
import LogLevelsTab from './components/LogLevelsTab.vue'
import SystemOverviewTab from './components/SystemOverviewTab.vue'

/**
 * System logs (openspec/changes/add-observability-and-caching): stored events, runtime log levels, and the system
 * overview with the cache and the AI digest. The open tab is kept in the URL (?tab=) so it survives a reload.
 */
type Tab = 'events' | 'levels' | 'overview'
const TABS: Tab[] = ['events', 'levels', 'overview']

const { t } = useI18n()
const route = useRoute()
const router = useRouter()

const initial = route.query.tab as Tab | undefined
const tab = ref<Tab>(initial && TABS.includes(initial) ? initial : 'events')

function remember(key: string | number) {
  void router.replace({ query: { ...route.query, tab: String(key) } })
}
</script>
