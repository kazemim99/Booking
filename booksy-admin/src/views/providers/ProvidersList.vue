<template>
  <div class="page-container">
    <a-page-header :title="$t('provider.management')" :sub-title="$t('provider.manageAndApprove')" />

    <a-tabs v-model:activeKey="activeTab">
      <a-tab-pane key="all" :tab="$t('provider.allProviders')">
        <provider-table :status="undefined" @view-details="handleViewDetails" />
      </a-tab-pane>

      <a-tab-pane v-for="status in PROVIDER_STATUS_TABS" :key="status">
        <template #tab>
          <a-badge
            v-if="status === 'PendingVerification'"
            :count="pendingCount"
            :overflow-count="99"
          >
            <span>{{ $t(statusLabelKey(status)) }}</span>
          </a-badge>
          <span v-else>{{ $t(statusLabelKey(status)) }}</span>
        </template>
        <provider-table :status="status" @view-details="handleViewDetails" />
      </a-tab-pane>
    </a-tabs>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import ProviderTable from './components/ProviderTable.vue'
import { providersApi } from '../../api/providers.api'
import { PROVIDER_STATUS_TABS, statusLabelKey } from '../../constants/provider-status'

const router = useRouter()
const activeTab = ref('all')
const pendingCount = ref(0)

const handleViewDetails = (id: string) => {
  router.push(`/providers/${id}`)
}

const loadPendingCount = async () => {
  try {
    pendingCount.value = await providersApi.getPendingVerificationCount()
  } catch (error) {
    console.error('Failed to load pending count:', error)
  }
}

onMounted(loadPendingCount)
</script>
