<template>
  <div class="page-container">
    <a-page-header :title="$t('provider.management')" :sub-title="$t('provider.manageAndApprove')" />

    <a-tabs v-model:activeKey="activeTab" @change="handleTabChange">
      <a-tab-pane key="all" :tab="$t('provider.allProviders')">
        <provider-table
          :status="undefined"
          @view-details="handleViewDetails"
        />
      </a-tab-pane>

      <a-tab-pane key="pending">
        <template #tab>
          <a-badge :count="pendingCount" :overflow-count="99">
            <span>{{ $t('provider.pendingApproval') }}</span>
          </a-badge>
        </template>
        <provider-table
          status="Pending"
          @view-details="handleViewDetails"
        />
      </a-tab-pane>

      <a-tab-pane key="approved" :tab="$t('provider.approved')">
        <provider-table
          status="Approved"
          @view-details="handleViewDetails"
        />
      </a-tab-pane>

      <a-tab-pane key="rejected" :tab="$t('provider.rejected')">
        <provider-table
          status="Rejected"
          @view-details="handleViewDetails"
        />
      </a-tab-pane>

      <a-tab-pane key="suspended" :tab="$t('provider.suspended')">
        <provider-table
          status="Suspended"
          @view-details="handleViewDetails"
        />
      </a-tab-pane>
    </a-tabs>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import ProviderTable from './components/ProviderTable.vue'
import { providersApi } from '../../api/providers.api'

useI18n()

const router = useRouter()
const activeTab = ref('all')
const pendingCount = ref(0)

const handleTabChange = () => {
  // Tab change handled by component
}

const handleViewDetails = (id: string) => {
  router.push(`/providers/${id}`)
}

const loadPendingCount = async () => {
  try {
    const response = await providersApi.getProviders({
      status: 'Pending',
      pageSize: 1,
    })
    pendingCount.value = response.totalCount
  } catch (error) {
    console.error('Failed to load pending count:', error)
  }
}

onMounted(() => {
  loadPendingCount()
})
</script>
