<template>
  <div>
    <div class="filter-section">
      <a-row :gutter="16">
        <a-col :xs="24" :sm="12" :md="10">
          <a-input
            v-model:value="filters.search"
            :placeholder="$t('provider.searchPlaceholder')"
            allow-clear
            @change="handleSearch"
          >
            <template #prefix>
              <search-outlined />
            </template>
          </a-input>
        </a-col>

        <a-col :xs="24" :sm="12" :md="6">
          <a-button block @click="resetFilters">{{ $t('common.reset') }}</a-button>
        </a-col>
      </a-row>
    </div>

    <a-card>
      <a-table
        :columns="columns"
        :data-source="providers"
        :loading="loading"
        :pagination="pagination"
        @change="handleTableChange"
        row-key="id"
      >
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'logo'">
            <a-avatar
              :size="48"
              :src="record.logoUrl || record.profileImageUrl"
              shape="square"
            >
              <template #icon>
                <shop-outlined />
              </template>
            </a-avatar>
          </template>

          <template v-if="column.key === 'business'">
            <div>
              <div style="font-weight: 500">{{ record.businessName }}</div>
              <div style="font-size: 12px; color: #999">{{ record.description }}</div>
              <div style="font-size: 12px; color: #999">{{ record.city }}, {{ record.state }}</div>
            </div>
          </template>

          <template v-if="column.key === 'status'">
            <a-tag :color="getStatusColor(record.status)">
              {{ t(statusLabelKey(record.status)) }}
            </a-tag>
          </template>

          <template v-if="column.key === 'rating'">
            <a-rate :value="record.averageRating || 0" disabled allow-half />
            <span style="margin-left: 8px">{{ record.averageRating?.toFixed(1) ?? '—' }}</span>
          </template>

          <template v-if="column.key === 'createdAt'">
            {{ formatDate(record.registeredAt) }}
          </template>

          <template v-if="column.key === 'actions'">
            <div class="table-actions">
              <a-button type="link" size="small" @click="$emit('viewDetails', record.id)">
                <eye-outlined /> {{ t('provider.view') }}
              </a-button>

              <a-popconfirm
                v-if="canActivate(record.status)"
                :title="t('provider.confirmActivate')"
                :ok-text="t('common.confirm')"
                :cancel-text="t('common.cancel')"
                @confirm="handleActivate(record)"
              >
                <a-button type="link" size="small">
                  <check-outlined /> {{ t('provider.activate') }}
                </a-button>
              </a-popconfirm>
            </div>
          </template>
        </template>
      </a-table>
    </a-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, watch, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { message } from 'ant-design-vue'
import {
  SearchOutlined,
  EyeOutlined,
  CheckOutlined,
  ShopOutlined,
} from '@ant-design/icons-vue'
import { providersApi } from '../../../api/providers.api'
import { getStatusColor, statusLabelKey, type ProviderStatus } from '../../../constants/provider-status'
import { formatDate } from '../../../utils/date'
import type { Provider } from '../../../types'

const { t } = useI18n()

interface Props {
  status?: ProviderStatus
}

const props = defineProps<Props>()
defineEmits<{
  viewDetails: [id: string]
}>()

const loading = ref(false)
const providers = ref<Provider[]>([])

const filters = reactive({
  search: '',
})

const pagination = reactive({
  current: 1,
  pageSize: 10,
  total: 0,
  showSizeChanger: true,
  showTotal: (total: number) => t('table.showing') + ' ' + total + ' ' + t('provider.title'),
})

const columns = computed(() => [
  { title: t('provider.tableLogo'), key: 'logo', width: 80 },
  { title: t('provider.tableBusinessInfo'), key: 'business', width: 280 },
  { title: t('provider.tableType'), dataIndex: 'type', key: 'type', width: 100 },
  { title: t('provider.tableStatus'), key: 'status', width: 140 },
  { title: t('provider.tableRating'), key: 'rating', width: 150 },
  { title: t('provider.tableServices'), dataIndex: 'serviceCount', key: 'serviceCount', width: 100 },
  { title: t('provider.tableRegistered'), key: 'createdAt', width: 140 },
  { title: t('provider.tableActions'), key: 'actions', width: 200, fixed: 'right' },
])

// Activate is the only lifecycle transition the backend exposes. The domain refuses it for a
// provider that is already Active or is Suspended, so it is not offered in those states.
const canActivate = (status: ProviderStatus) =>
  status !== 'Active' && status !== 'Suspended' && status !== 'Archived'

const loadProviders = async () => {
  loading.value = true
  try {
    const response = await providersApi.getProviders({
      pageNumber: pagination.current,
      pageSize: pagination.pageSize,
      search: filters.search || undefined,
      status: props.status,
    })
    providers.value = response.items
    pagination.total = response.totalCount
  } catch (error) {
    message.error(t('provider.failedToLoadProviders'))
  } finally {
    loading.value = false
  }
}

const handleTableChange = (pag: { current: number; pageSize: number }) => {
  pagination.current = pag.current
  pagination.pageSize = pag.pageSize
  loadProviders()
}

const handleSearch = () => {
  pagination.current = 1
  loadProviders()
}

const resetFilters = () => {
  filters.search = ''
  pagination.current = 1
  loadProviders()
}

const handleActivate = async (provider: Provider) => {
  try {
    await providersApi.activateProvider(provider.id)
    message.success(t('provider.providerActivatedSuccessfully'))
    loadProviders()
  } catch (error) {
    message.error(t('provider.failedToActivateProvider'))
  }
}

watch(() => props.status, () => {
  pagination.current = 1
  loadProviders()
})

onMounted(() => {
  loadProviders()
})
</script>
