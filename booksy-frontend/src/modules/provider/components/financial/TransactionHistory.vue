<template>
  <div class="transaction-history" dir="rtl">
    <!-- Header -->
    <div class="history-header">
      <div>
        <h2 class="history-title">تاریخچه تراکنش‌ها</h2>
        <p class="history-subtitle">{{ totalTransactions }} تراکنش</p>
      </div>

      <!-- Date Range Filter -->
      <div class="header-actions">
        <select v-model="selectedPeriod" @change="handlePeriodChange" class="select-input">
          <option value="today">امروز</option>
          <option value="week">هفته جاری</option>
          <option value="month">ماه جاری</option>
          <option value="lastMonth">ماه گذشته</option>
          <option value="custom">دوره سفارشی</option>
        </select>

        <button @click="showFilters = !showFilters" class="btn btn-secondary">
          <svg viewBox="0 0 20 20" fill="currentColor">
            <path fill-rule="evenodd" d="M3 3a1 1 0 011-1h12a1 1 0 011 1v3a1 1 0 01-.293.707L12 11.414V15a1 1 0 01-.293.707l-2 2A1 1 0 018 17v-5.586L3.293 6.707A1 1 0 013 6V3z" clip-rule="evenodd" />
          </svg>
          فیلترها
        </button>
      </div>
    </div>

    <!-- Filters Panel -->
    <div v-if="showFilters" class="filters-panel">
      <div class="filter-grid">
        <!-- Transaction Type Filter -->
        <div class="filter-group">
          <label>نوع تراکنش</label>
          <select v-model="filters.type" @change="applyFilters" class="select-input">
            <option :value="undefined">همه</option>
            <option value="Booking">رزرو</option>
            <option value="Refund">بازگشت وجه</option>
            <option value="Adjustment">تعدیل</option>
            <option value="Payout">واریز</option>
            <option value="Commission">کمیسیون</option>
          </select>
        </div>

        <!-- Custom Date Range -->
        <div v-if="selectedPeriod === 'custom'" class="filter-group">
          <label>از تاریخ</label>
          <input v-model="filters.startDate" type="date" @change="applyFilters" class="date-input" />
        </div>

        <div v-if="selectedPeriod === 'custom'" class="filter-group">
          <label>تا تاریخ</label>
          <input v-model="filters.endDate" type="date" @change="applyFilters" class="date-input" />
        </div>

        <!-- Sort Options -->
        <div class="filter-group">
          <label>مرتب‌سازی</label>
          <select v-model="filters.sortBy" @change="applyFilters" class="select-input">
            <option value="createdAt">تاریخ</option>
            <option value="amount">مبلغ</option>
          </select>
        </div>

        <div class="filter-group">
          <label>ترتیب</label>
          <select v-model="filters.sortOrder" @change="applyFilters" class="select-input">
            <option value="desc">نزولی</option>
            <option value="asc">صعودی</option>
          </select>
        </div>
      </div>

      <div class="filter-actions">
        <button @click="resetFilters" class="btn btn-secondary">پاک کردن فیلترها</button>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="loading-state">
      <div class="spinner"></div>
      <p>در حال بارگذاری تراکنش‌ها...</p>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="error-state">
      <svg class="icon-error" viewBox="0 0 20 20" fill="currentColor">
        <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd" />
      </svg>
      <p>{{ error }}</p>
      <button @click="loadTransactions" class="btn btn-secondary">تلاش مجدد</button>
    </div>

    <!-- Empty State -->
    <div v-else-if="transactions.length === 0" class="empty-state">
      <svg class="icon-empty" viewBox="0 0 20 20" fill="currentColor">
        <path d="M9 2a1 1 0 000 2h2a1 1 0 100-2H9z" />
        <path fill-rule="evenodd" d="M4 5a2 2 0 012-2 3 3 0 003 3h2a3 3 0 003-3 2 2 0 012 2v11a2 2 0 01-2 2H6a2 2 0 01-2-2V5zm3 4a1 1 0 000 2h.01a1 1 0 100-2H7zm3 0a1 1 0 000 2h3a1 1 0 100-2h-3zm-3 4a1 1 0 100 2h.01a1 1 0 100-2H7zm3 0a1 1 0 100 2h3a1 1 0 100-2h-3z" clip-rule="evenodd" />
      </svg>
      <p>تراکنشی یافت نشد</p>
    </div>

    <!-- Transaction Table -->
    <div v-else class="transaction-table-wrapper">
      <table class="transaction-table">
        <thead>
          <tr>
            <th>نوع</th>
            <th>توضیحات</th>
            <th>تاریخ</th>
            <th>مبلغ</th>
            <th>وضعیت</th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="transaction in transactions"
            :key="transaction.id"
            class="transaction-row"
          >
            <td>
              <div class="type-cell">
                <div class="type-icon" :class="getTransactionClass(transaction.type)">
                  <svg v-if="transaction.type === 'Booking'" viewBox="0 0 20 20" fill="currentColor">
                    <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm1-12a1 1 0 10-2 0v4a1 1 0 00.293.707l2.828 2.829a1 1 0 101.415-1.415L11 9.586V6z" clip-rule="evenodd" />
                  </svg>
                  <svg v-else-if="transaction.type === 'Refund'" viewBox="0 0 20 20" fill="currentColor">
                    <path fill-rule="evenodd" d="M4 2a1 1 0 011 1v2.101a7.002 7.002 0 0111.601 2.566 1 1 0 11-1.885.666A5.002 5.002 0 005.999 7H9a1 1 0 010 2H4a1 1 0 01-1-1V3a1 1 0 011-1zm.008 9.057a1 1 0 011.276.61A5.002 5.002 0 0014.001 13H11a1 1 0 110-2h5a1 1 0 011 1v5a1 1 0 11-2 0v-2.101a7.002 7.002 0 01-11.601-2.566 1 1 0 01.61-1.276z" clip-rule="evenodd" />
                  </svg>
                  <svg v-else viewBox="0 0 20 20" fill="currentColor">
                    <path d="M8.433 7.418c.155-.103.346-.196.567-.267v1.698a2.305 2.305 0 01-.567-.267C8.07 8.34 8 8.114 8 8c0-.114.07-.34.433-.582zM11 12.849v-1.698c.22.071.412.164.567.267.364.243.433.468.433.582 0 .114-.07.34-.433.582a2.305 2.305 0 01-.567.267z" />
                  </svg>
                </div>
                <span class="type-label">{{ getTransactionTypeLabel(transaction.type) }}</span>
              </div>
            </td>
            <td>
              <div class="description-cell">
                <p class="description-text">{{ transaction.description }}</p>
                <p v-if="transaction.bookingId" class="booking-id">رزرو #{{ transaction.bookingId.slice(-8) }}</p>
              </div>
            </td>
            <td>
              <div class="date-cell">
                <p class="date-full">{{ formatFullDate(transaction.createdAt) }}</p>
                <p class="date-relative">{{ formatRelativeDate(transaction.createdAt) }}</p>
              </div>
            </td>
            <td>
              <div class="amount-cell" :class="{ positive: isIncomeTransaction(transaction.type), negative: !isIncomeTransaction(transaction.type) }">
                {{ isIncomeTransaction(transaction.type) ? '+' : '-' }}{{ formatCurrency(transaction.amount) }}
              </div>
            </td>
            <td>
              <span class="status-badge" :class="`status-${transaction.status.toLowerCase()}`">
                {{ transaction.status }}
              </span>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Pagination -->
    <div v-if="totalPages > 1" class="pagination">
      <button
        @click="goToPage(currentPage - 1)"
        :disabled="currentPage === 1"
        class="pagination-btn"
      >
        <svg viewBox="0 0 20 20" fill="currentColor">
          <path fill-rule="evenodd" d="M12.707 5.293a1 1 0 010 1.414L9.414 10l3.293 3.293a1 1 0 01-1.414 1.414l-4-4a1 1 0 010-1.414l4-4a1 1 0 011.414 0z" clip-rule="evenodd" />
        </svg>
      </button>

      <div class="pagination-info">
        صفحه {{ currentPage }} از {{ totalPages }}
      </div>

      <button
        @click="goToPage(currentPage + 1)"
        :disabled="currentPage === totalPages"
        class="pagination-btn"
      >
        <svg viewBox="0 0 20 20" fill="currentColor">
          <path fill-rule="evenodd" d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z" clip-rule="evenodd" />
        </svg>
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useProviderStore } from '@/modules/provider/stores/provider.store'
import { financialService } from '@/modules/provider/services/financial.service'
import type { Transaction, TransactionType, GetTransactionsRequest } from '@/modules/provider/types/financial.types'
import { formatFullDate } from '@/core/utils'
import {
  formatCurrency,
  formatRelativeDate,
  getTransactionTypeLabel,
  isIncomeTransaction,
  getDateRangeForPeriod,
  EarningsPeriod,
} from '@/modules/provider/types/financial.types'

// ============================================================================
// Store
// ============================================================================

const providerStore = useProviderStore()
const providerId = computed(() => providerStore.currentProvider?.id || '')

// ============================================================================
// State
// ============================================================================

const loading = ref(false)
const error = ref<string | null>(null)
const transactions = ref<Transaction[]>([])
const currentPage = ref(1)
const totalPages = ref(1)
const totalTransactions = ref(0)
const showFilters = ref(false)
const selectedPeriod = ref<string>('month')

const filters = ref<GetTransactionsRequest>({
  providerId: '',
  type: undefined,
  startDate: undefined,
  endDate: undefined,
  page: 1,
  pageSize: 20,
  sortBy: 'createdAt',
  sortOrder: 'desc',
})

// ============================================================================
// Lifecycle
// ============================================================================

onMounted(() => {
  initializeFilters()
  loadTransactions()
})

watch(() => providerId.value, () => {
  if (providerId.value) {
    loadTransactions()
  }
})

// ============================================================================
// Methods
// ============================================================================

function initializeFilters(): void {
  const dateRange = getDateRangeForPeriod(EarningsPeriod.ThisMonth)
  filters.value.providerId = providerId.value
  filters.value.startDate = dateRange.startDate
  filters.value.endDate = dateRange.endDate
}

async function loadTransactions(): Promise<void> {
  if (!providerId.value) {
    error.value = 'شناسه ارائه‌دهنده یافت نشد'
    return
  }

  loading.value = true
  error.value = null

  try {
    filters.value.providerId = providerId.value
    filters.value.page = currentPage.value

    const response = await financialService.getTransactionHistory(filters.value)

    transactions.value = response.items
    totalPages.value = response.totalPages
    totalTransactions.value = response.totalCount

    console.log(`[TransactionHistory] Loaded ${transactions.value.length} transactions`)
  } catch (err) {
    console.error('[TransactionHistory] Error loading transactions:', err)
    error.value = 'خطا در بارگذاری تراکنش‌ها'
  } finally {
    loading.value = false
  }
}

function handlePeriodChange(): void {
  if (selectedPeriod.value !== 'custom') {
    const periodMap: Record<string, EarningsPeriod> = {
      today: EarningsPeriod.Today,
      week: EarningsPeriod.ThisWeek,
      month: EarningsPeriod.ThisMonth,
      lastMonth: EarningsPeriod.LastMonth,
    }

    const period = periodMap[selectedPeriod.value]
    if (period) {
      const dateRange = getDateRangeForPeriod(period)
      filters.value.startDate = dateRange.startDate
      filters.value.endDate = dateRange.endDate
      applyFilters()
    }
  }
}

function applyFilters(): void {
  currentPage.value = 1
  loadTransactions()
}

function resetFilters(): void {
  selectedPeriod.value = 'month'
  filters.value = {
    providerId: providerId.value,
    type: undefined,
    startDate: undefined,
    endDate: undefined,
    page: 1,
    pageSize: 20,
    sortBy: 'createdAt',
    sortOrder: 'desc',
  }
  initializeFilters()
  applyFilters()
}

function goToPage(page: number): void {
  if (page >= 1 && page <= totalPages.value) {
    currentPage.value = page
    loadTransactions()
  }
}

function getTransactionClass(type: TransactionType): string {
  switch (type) {
    case 'Booking':
      return 'income'
    case 'Refund':
    case 'Commission':
      return 'expense'
    default:
      return 'neutral'
  }
}

</script>

<style scoped>
.transaction-history {
  width: 100%;
}

/* Header */
.history-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1.5rem;
  flex-wrap: wrap;
  gap: 1rem;
}

.history-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: #1a202c;
  margin: 0;
}

.history-subtitle {
  color: #718096;
  font-size: 0.875rem;
  margin: 0.25rem 0 0 0;
}

.header-actions {
  display: flex;
  gap: 0.75rem;
}

/* Filters */
.filters-panel {
  background: #f7fafc;
  border: 1px solid #e2e8f0;
  border-radius: 0.5rem;
  padding: 1.5rem;
  margin-bottom: 1.5rem;
}

.filter-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 1rem;
  margin-bottom: 1rem;
}

.filter-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.filter-group label {
  font-size: 0.875rem;
  font-weight: 500;
  color: #2d3748;
}

.select-input,
.date-input {
  padding: 0.625rem 0.875rem;
  font-size: 0.875rem;
  border: 1px solid #cbd5e0;
  border-radius: 0.375rem;
  background: white;
  transition: all 0.2s;
}

.select-input:focus,
.date-input:focus {
  outline: none;
  border-color: #3182ce;
  box-shadow: 0 0 0 3px rgba(49, 130, 206, 0.1);
}

.filter-actions {
  display: flex;
  justify-content: flex-end;
}

/* States */
.loading-state,
.error-state,
.empty-state {
  text-align: center;
  padding: 4rem 2rem;
}

.spinner {
  width: 3rem;
  height: 3rem;
  border: 4px solid #e2e8f0;
  border-top-color: #3182ce;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
  margin: 0 auto 1rem;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.icon-error,
.icon-empty {
  width: 4rem;
  height: 4rem;
  margin: 0 auto 1rem;
  color: #cbd5e0;
}

.icon-error {
  color: #f56565;
}

/* Table */
.transaction-table-wrapper {
  overflow-x: auto;
  background: white;
  border-radius: 0.5rem;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.transaction-table {
  width: 100%;
  border-collapse: collapse;
}

.transaction-table thead {
  background: #f7fafc;
  border-bottom: 2px solid #e2e8f0;
}

.transaction-table th {
  padding: 1rem;
  text-align: right;
  font-size: 0.75rem;
  font-weight: 600;
  color: #4a5568;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.transaction-row {
  border-bottom: 1px solid #e2e8f0;
  transition: background 0.2s;
}

.transaction-row:hover {
  background: #f7fafc;
}

.transaction-table td {
  padding: 1rem;
}

/* Table Cells */
.type-cell {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.type-icon {
  width: 2rem;
  height: 2rem;
  border-radius: 0.375rem;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.type-icon.income {
  background: rgba(16, 185, 129, 0.1);
  color: #10b981;
}

.type-icon.expense {
  background: rgba(239, 68, 68, 0.1);
  color: #ef4444;
}

.type-icon.neutral {
  background: rgba(107, 114, 128, 0.1);
  color: #6b7280;
}

.type-icon svg {
  width: 1rem;
  height: 1rem;
}

.type-label {
  font-size: 0.875rem;
  font-weight: 500;
  color: #2d3748;
}

.description-cell {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.description-text {
  font-size: 0.875rem;
  color: #2d3748;
  margin: 0;
}

.booking-id {
  font-size: 0.75rem;
  color: #718096;
  margin: 0;
}

.date-cell {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.date-full {
  font-size: 0.875rem;
  color: #2d3748;
  margin: 0;
}

.date-relative {
  font-size: 0.75rem;
  color: #718096;
  margin: 0;
}

.amount-cell {
  font-size: 1rem;
  font-weight: 600;
}

.amount-cell.positive {
  color: #10b981;
}

.amount-cell.negative {
  color: #ef4444;
}

.status-badge {
  padding: 0.375rem 0.75rem;
  border-radius: 0.375rem;
  font-size: 0.75rem;
  font-weight: 500;
  display: inline-block;
}

.status-badge.status-pending {
  background: #fef3c7;
  color: #92400e;
}

.status-badge.status-completed {
  background: #d1fae5;
  color: #065f46;
}

.status-badge.status-failed {
  background: #fee2e2;
  color: #991b1b;
}

/* Pagination */
.pagination {
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 1rem;
  margin-top: 1.5rem;
}

.pagination-btn {
  width: 2.5rem;
  height: 2.5rem;
  border: 1px solid #e2e8f0;
  background: white;
  border-radius: 0.375rem;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.2s;
}

.pagination-btn:hover:not(:disabled) {
  background: #f7fafc;
  border-color: #cbd5e0;
}

.pagination-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.pagination-btn svg {
  width: 1.25rem;
  height: 1.25rem;
  color: #4a5568;
}

.pagination-info {
  font-size: 0.875rem;
  color: #4a5568;
}

/* Buttons */
.btn {
  padding: 0.625rem 1.25rem;
  font-size: 0.875rem;
  font-weight: 500;
  border: none;
  border-radius: 0.375rem;
  cursor: pointer;
  transition: all 0.2s;
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
}

.btn-secondary {
  background: #e2e8f0;
  color: #2d3748;
}

.btn-secondary:hover {
  background: #cbd5e0;
}

.btn svg {
  width: 1.25rem;
  height: 1.25rem;
}

/* Responsive */
@media (max-width: 768px) {
  .history-header {
    flex-direction: column;
    align-items: flex-start;
  }

  .filter-grid {
    grid-template-columns: 1fr;
  }

  .transaction-table {
    font-size: 0.875rem;
  }

  .transaction-table th,
  .transaction-table td {
    padding: 0.75rem 0.5rem;
  }
}
</style>
