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
              <div style="font-size: 12px; color: #999">{{ record.description || $t('provider.description') }}</div>
              <div style="font-size: 12px; color: #999">{{ record.city }}, {{ record.state }}</div>
            </div>
          </template>

          <template v-if="column.key === 'status'">
            <a-tag :color="getStatusColor(record.status)">
              {{ record.status }}
            </a-tag>
          </template>

          <template v-if="column.key === 'rating'">
            <a-rate :value="record.averageRating || record.rating || 0" disabled allow-half />
            <span style="margin-left: 8px">{{ (record.averageRating || record.rating)?.toFixed(1) || 'N/A' }}</span>
          </template>

          <template v-if="column.key === 'bookings'">
            {{ record.totalBookings || 0 }}
          </template>

          <template v-if="column.key === 'createdAt'">
            {{ formatDate(record.registeredAt || record.createdAt) }}
          </template>

          <template v-if="column.key === 'actions'">
            <div class="table-actions">
              <a-button type="link" size="small" @click="$emit('viewDetails', record.id)">
                <eye-outlined /> {{ t('provider.view') }}
              </a-button>

              <template v-if="record.status === 'Pending' || record.status === 'PendingVerification'">
                <a-button type="link" size="small" @click="handleActivate(record)">
                  <check-outlined /> {{ t('provider.activate') }}
                </a-button>
                <a-button type="link" danger size="small" @click="handleReject(record)">
                  <close-outlined /> {{ t('provider.rejectProvider') }}
                </a-button>
              </template>

              <template v-if="record.status === 'Active' || record.status === 'Approved'">
                <a-button type="link" danger size="small" @click="handleSuspend(record)">
                  <stop-outlined /> {{ t('provider.suspendProvider') }}
                </a-button>
              </template>

              <template v-if="record.status === 'Suspended' || record.status === 'Inactive'">
                <a-button type="link" size="small" @click="handleActivate(record)">
                  <check-circle-outlined /> {{ t('provider.activate') }}
                </a-button>
              </template>
            </div>
          </template>
        </template>
      </a-table>
    </a-card>

    <a-modal
      v-model:open="rejectModalVisible"
      :title="t('provider.rejectProvider')"
      :confirm-loading="modalLoading"
      @ok="submitReject"
    >
      <a-form layout="vertical">
        <a-form-item :label="t('provider.reasonForRejection')" required>
          <a-textarea
            v-model:value="rejectReason"
            :rows="4"
            :placeholder="t('provider.provideReason')"
          />
        </a-form-item>
      </a-form>
    </a-modal>

    <a-modal
      v-model:open="suspendModalVisible"
      :title="t('provider.suspendProvider')"
      :confirm-loading="modalLoading"
      @ok="submitSuspend"
    >
      <a-form layout="vertical">
        <a-form-item :label="t('provider.reasonForSuspension')" required>
          <a-textarea
            v-model:value="suspendReason"
            :rows="4"
            :placeholder="t('provider.provideReason')"
          />
        </a-form-item>
      </a-form>
    </a-modal>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, watch, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { message } from 'ant-design-vue'
import dayjs from 'dayjs'
import {
  SearchOutlined,
  EyeOutlined,
  CheckOutlined,
  CloseOutlined,
  StopOutlined,
  CheckCircleOutlined,
  ShopOutlined,
} from '@ant-design/icons-vue'
import { providersApi } from '../../../api/providers.api'
import type { Provider } from '../../../types'

const { t } = useI18n()

interface Props {
  status?: 'Pending' | 'Approved' | 'Rejected' | 'Suspended'
}

const props = defineProps<Props>()
const emit = defineEmits<{
  viewDetails: [id: string]
}>()

const loading = ref(false)
const providers = ref<Provider[]>([])
const rejectModalVisible = ref(false)
const suspendModalVisible = ref(false)
const modalLoading = ref(false)
const rejectReason = ref('')
const suspendReason = ref('')
const selectedProvider = ref<Provider | null>(null)

const filters = reactive({
  search: '',
})

const pagination = reactive({
  current: 1,
  pageSize: 10,
  total: 0,
  showSizeChanger: true,
  showTotal: (total: number) => t('table.showing') + ' ' + total + ' ' + t('provider.title').toLowerCase(),
})

const columns = computed(() => [
  { title: t('provider.tableLogo'), key: 'logo', width: 80 },
  { title: t('provider.tableBusinessInfo'), key: 'business', width: 280 },
  { title: t('provider.tableType'), dataIndex: 'type', key: 'type', width: 100 },
  { title: t('provider.tableStatus'), key: 'status', width: 120 },
  { title: t('provider.tableRating'), key: 'rating', width: 150 },
  { title: t('provider.tableServices'), dataIndex: 'serviceCount', key: 'serviceCount', width: 100 },
  { title: t('provider.tableRegistered'), key: 'createdAt', width: 120 },
  { title: t('provider.tableActions'), key: 'actions', width: 300, fixed: 'right' },
])

const getStatusColor = (status: string) => {
  const colors: Record<string, string> = {
    Pending: 'orange',
    PendingVerification: 'orange',
    Active: 'green',
    Approved: 'green',
    Rejected: 'red',
    Suspended: 'volcano',
    Inactive: 'gray',
  }
  return colors[status] || 'default'
}

const formatDate = (date: string) => {
  return dayjs(date).format('MMM DD, YYYY')
}

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

const handleTableChange = (pag: any) => {
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
    await providersApi.approveProvider(provider.id)
    message.success(t('provider.providerActivatedSuccessfully'))
    loadProviders()
  } catch (error) {
    message.error(t('provider.failedToActivateProvider'))
  }
}

const handleReject = (provider: Provider) => {
  selectedProvider.value = provider
  rejectReason.value = ''
  rejectModalVisible.value = true
}

const submitReject = async () => {
  if (!rejectReason.value.trim()) {
    message.warning(t('provider.providePleasePleaseReason'))
    return
  }

  modalLoading.value = true
  try {
    await providersApi.rejectProvider(selectedProvider.value!.id, {
      reason: rejectReason.value,
    })
    message.success(t('provider.providerRejected'))
    rejectModalVisible.value = false
    loadProviders()
  } catch (error) {
    message.error(t('provider.failedToRejectProvider'))
  } finally {
    modalLoading.value = false
  }
}

const handleSuspend = (provider: Provider) => {
  selectedProvider.value = provider
  suspendReason.value = ''
  suspendModalVisible.value = true
}

const submitSuspend = async () => {
  if (!suspendReason.value.trim()) {
    message.warning(t('provider.providePleaseReasonSuspend'))
    return
  }

  modalLoading.value = true
  try {
    await providersApi.suspendProvider(selectedProvider.value!.id, suspendReason.value)
    message.success(t('provider.providerSuspended'))
    suspendModalVisible.value = false
    loadProviders()
  } catch (error) {
    message.error(t('provider.failedToSuspendProvider'))
  } finally {
    modalLoading.value = false
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
