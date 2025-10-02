<!-- src/modules/provider/views/ProviderList.vue -->
<template>
  <div class="provider-list-page">
    <!-- Page Header -->
    <div class="page-header">
      <div class="header-content">
        <h1 class="page-title">Service Providers</h1>
        <p class="page-subtitle">Browse and manage service providers</p>
      </div>
      <div class="header-actions">
        <AppButton variant="primary" @click="handleAddProvider">
          <svg
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
            class="button-icon"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M12 4v16m8-8H4"
            />
          </svg>
          Add Provider
        </AppButton>
      </div>
    </div>

    <!-- Filters -->
    <Card class="filters-card">
      <div class="filters-container">
        <div class="filter-group">
          <TextInput
            v-model="searchTerm"
            placeholder="Search providers..."
            @update:model-value="debouncedSearch"
          >
            <template #prefix>
              <svg
                xmlns="http://www.w3.org/2000/svg"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
                class="input-icon"
              >
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"
                />
              </svg>
            </template>
          </TextInput>
        </div>

        <div class="filter-group">
          <Select
            v-model="selectedType"
            :options="typeOptions"
            placeholder="All Types"
            @change="handleTypeChange"
          />
        </div>

        <div class="filter-group">
          <Select
            v-model="selectedStatus"
            :options="statusOptions"
            placeholder="All Statuses"
            @change="handleStatusChange"
          />
        </div>

        <div class="filter-group">
          <AppButton v-if="hasFilters" variant="secondary" @click="clearFilters">
            Clear Filters
          </AppButton>
        </div>
      </div>
    </Card>

    <!-- Error Alert -->
    <Alert v-if="error" type="error" class="error-alert" :message="error" @dismiss="error = null" />

    <!-- Data Table -->
    <Card class="table-card">
      <DataTable
        :columns="columns"
        :data="providers"
        :loading="loading"
        :actions="tableActions"
        :striped="true"
        :hoverable="true"
        empty-message="No providers found"
        @sort="handleSort"
      />
    </Card>

    <!-- Pagination -->
    <div v-if="totalPages > 1" class="pagination-container">
      <Pagination
        :current-page="currentPage"
        :total-pages="totalPages"
        :page-size="pageSize"
        :total="total"
        @page-change="handlePageChange"
        @page-size-change="handlePageSizeChange"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { Card, DataTable, TextInput, Select, Alert, Pagination } from '@/shared/components'
import type { TableColumn, TableAction } from '@/shared/components/ui/Table/DataTable.types'
import type { ProviderStatus, ProviderSummary, ProviderType } from '../types/provider.types'
import { useDebounceFn } from '@/core/composables/useDebounce'
import { useProviderList } from '../composables/useProviderList'

const router = useRouter()

const {
  loading,
  error,
  providers,
  currentPage,
  pageSize,
  total,
  totalPages,
  hasFilters,
  fetchProviders,
  searchProviders,
  filterByType,
  filterByStatus,
  clearFilters,
  handleSort,
  handlePageChange,
  handlePageSizeChange,
} = useProviderList()

// Local state for filters
const searchTerm = ref('')
const selectedType = ref('')
const selectedStatus = ref('')

const statusOptions = [
  { label: 'All Statuses', value: '' },
  { label: 'Active', value: 'Active' },
  { label: 'Inactive', value: 'Inactive' },
  { label: 'Pending Verification', value: 'PendingVerification' },
  { label: 'Suspended', value: 'Suspended' },
]

const typeOptions = [
  { label: 'All Types', value: '' },
  { label: 'Individual', value: 'Individual' },
  { label: 'Company', value: 'Company' },
  { label: 'Freelancer', value: 'Freelancer' },
]
// Table Columns
const columns: TableColumn<ProviderSummary>[] = [
  {
    key: 'businessName',
    label: 'Business Name',
    sortable: true,
    width: '250px',
  },
  {
    key: 'type',
    label: 'Type',
    sortable: true,
    width: '120px',
  },
  {
    key: 'status',
    label: 'Status',
    sortable: true,
    width: '150px',
    formatter: (value) => {
      const statusMap: Record<string, string> = {
        Active: '✓ Active',
        Inactive: '○ Inactive',
        PendingVerification: '⋯ Pending',
        Suspended: '⊗ Suspended',
      }
      return statusMap[value as string] || String(value)
    },
  },
  {
    key: 'city',
    label: 'Location',
    sortable: true,
    width: '150px',
    formatter: (value, row) => {
      return row.state ? `${String(value)}, ${row.state}` : String(value)
    },
  },
  {
    key: 'averageRating',
    label: 'Rating',
    sortable: true,
    width: '100px',
    align: 'center',
    formatter: (value) => {
      return typeof value === 'number' ? `⭐ ${value.toFixed(1)}` : 'N/A'
    },
  },
  {
    key: 'serviceCount',
    label: 'Services',
    sortable: true,
    width: '100px',
    align: 'center',
  },
  {
    key: 'registeredAt',
    label: 'Registered',
    sortable: true,
    width: '120px',
    formatter: (value) => {
      return new Date(value as string | number | Date).toLocaleDateString()
    },
  },
]

// Table Actions
const tableActions: TableAction<ProviderSummary>[] = [
  {
    label: 'View',
    icon: '<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" /><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" /></svg>',
    variant: 'secondary',
    onClick: (row) => handleViewProvider(row.id),
  },
  {
    label: 'Edit',
    icon: '<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" /></svg>',
    variant: 'primary',
    onClick: (row) => handleEditProvider(row.id),
    visible: (row) => row.status !== 'Suspended',
  },
  {
    label: 'Delete',
    icon: '<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>',
    variant: 'danger',
    onClick: (row) => handleDeleteProvider(row.id),
  },
]

// Methods
const debouncedSearch = useDebounceFn((value: string | number) => {
  searchProviders(String(value))
}, 500)

function handleTypeChange() {
  filterByType((selectedType.value as ProviderType) || undefined)
}

function handleStatusChange() {
  filterByStatus((selectedStatus.value as ProviderStatus) || undefined)
}

function handleAddProvider() {
  router.push('/providers/register')
}

function handleViewProvider(id: string) {
  router.push(`/providers/${id}`)
}

function handleEditProvider(id: string) {
  router.push(`/providers/${id}/edit`)
}

function handleDeleteProvider(id: string) {
  // Implement delete logic with confirmation modal
  console.log('Delete provider:', id)
}

// Lifecycle
onMounted(() => {
  fetchProviders()
})
</script>

<style scoped lang="scss">
.provider-list-page {
  padding: 24px;
  max-width: 1400px;
  margin: 0 auto;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 24px;

  @media (max-width: 768px) {
    flex-direction: column;
    gap: 16px;
  }
}

.header-content {
  .page-title {
    font-size: 28px;
    font-weight: 700;
    color: #1f2937;
    margin: 0 0 8px 0;
  }

  .page-subtitle {
    font-size: 14px;
    color: #6b7280;
    margin: 0;
  }
}

.header-actions {
  display: flex;
  gap: 12px;

  .button-icon {
    width: 20px;
    height: 20px;
  }
}

.filters-card {
  margin-bottom: 24px;
}

.filters-container {
  display: grid;
  grid-template-columns: 2fr 1fr 1fr auto;
  gap: 16px;

  @media (max-width: 1024px) {
    grid-template-columns: 1fr 1fr;
  }

  @media (max-width: 640px) {
    grid-template-columns: 1fr;
  }
}

.filter-group {
  display: flex;
  align-items: center;

  .input-icon {
    width: 20px;
    height: 20px;
    color: #9ca3af;
  }
}

.error-alert {
  margin-bottom: 24px;
}

.table-card {
  margin-bottom: 24px;
  overflow: visible;
}

.pagination-container {
  display: flex;
  justify-content: center;
}
</style>
