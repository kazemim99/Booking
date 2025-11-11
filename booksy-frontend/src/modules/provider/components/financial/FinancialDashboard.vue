<template>
  <div class="financial-dashboard" dir="rtl">
    <!-- Loading State -->
    <div v-if="loading" class="loading-state">
      <div class="spinner"></div>
      <p>در حال بارگذاری اطلاعات مالی...</p>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="error-state">
      <svg class="icon-error" viewBox="0 0 20 20" fill="currentColor">
        <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd" />
      </svg>
      <p>{{ error }}</p>
      <button @click="loadDashboard" class="btn btn-secondary">تلاش مجدد</button>
    </div>

    <!-- Dashboard Content -->
    <div v-else-if="dashboard" class="dashboard-content">
      <!-- Summary Cards -->
      <div class="summary-cards">
        <!-- Current Month Earnings -->
        <div class="summary-card card-primary">
          <div class="card-header">
            <h3>درآمد این ماه</h3>
            <svg viewBox="0 0 20 20" fill="currentColor">
              <path d="M8.433 7.418c.155-.103.346-.196.567-.267v1.698a2.305 2.305 0 01-.567-.267C8.07 8.34 8 8.114 8 8c0-.114.07-.34.433-.582zM11 12.849v-1.698c.22.071.412.164.567.267.364.243.433.468.433.582 0 .114-.07.34-.433.582a2.305 2.305 0 01-.567.267z" />
              <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm1-13a1 1 0 10-2 0v.092a4.535 4.535 0 00-1.676.662C6.602 6.234 6 7.009 6 8c0 .99.602 1.765 1.324 2.246.48.32 1.054.545 1.676.662v1.941c-.391-.127-.68-.317-.843-.504a1 1 0 10-1.51 1.31c.562.649 1.413 1.076 2.353 1.253V15a1 1 0 102 0v-.092a4.535 4.535 0 001.676-.662C13.398 13.766 14 12.991 14 12c0-.99-.602-1.765-1.324-2.246A4.535 4.535 0 0011 9.092V7.151c.391.127.68.317.843.504a1 1 0 101.511-1.31c-.563-.649-1.413-1.076-2.354-1.253V5z" clip-rule="evenodd" />
            </svg>
          </div>
          <div class="card-value">
            {{ formatCurrency(dashboard.currentMonthEarnings.netEarnings) }}
          </div>
          <div class="card-meta">
            <span class="growth" :class="{ positive: growthRate >= 0, negative: growthRate < 0 }">
              <svg v-if="growthRate >= 0" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M12 7a1 1 0 110-2h5a1 1 0 011 1v5a1 1 0 11-2 0V8.414l-4.293 4.293a1 1 0 01-1.414 0L8 10.414l-4.293 4.293a1 1 0 01-1.414-1.414l5-5a1 1 0 011.414 0L11 10.586 14.586 7H12z" clip-rule="evenodd" />
              </svg>
              <svg v-else viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M12 13a1 1 0 100 2h5a1 1 0 001-1v-5a1 1 0 10-2 0v2.586l-4.293-4.293a1 1 0 00-1.414 0L8 9.586 3.707 5.293a1 1 0 00-1.414 1.414l5 5a1 1 0 001.414 0L11 9.414 14.586 13H12z" clip-rule="evenodd" />
              </svg>
              {{ formatGrowthRate(growthRate) }}
            </span>
            <span class="text-muted">نسبت به ماه قبل</span>
          </div>
          <div class="card-footer">
            <div class="footer-item">
              <span class="label">درآمد ناخالص:</span>
              <span class="value">{{ formatCurrency(dashboard.currentMonthEarnings.totalRevenue) }}</span>
            </div>
            <div class="footer-item">
              <span class="label">کمیسیون:</span>
              <span class="value">{{ formatCurrency(dashboard.currentMonthEarnings.platformCommission) }}</span>
            </div>
          </div>
        </div>

        <!-- Total Bookings -->
        <div class="summary-card">
          <div class="card-header">
            <h3>رزروهای تکمیل شده</h3>
            <svg viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" />
            </svg>
          </div>
          <div class="card-value">
            {{ dashboard.currentMonthEarnings.completedBookings }}
          </div>
          <div class="card-meta">
            <span class="text-muted">از {{ dashboard.currentMonthEarnings.totalBookings }} رزرو کل</span>
          </div>
          <div class="card-footer">
            <div class="footer-item">
              <span class="label">لغو شده:</span>
              <span class="value">{{ dashboard.currentMonthEarnings.cancelledBookings }}</span>
            </div>
            <div class="footer-item">
              <span class="label">میانگین ارزش:</span>
              <span class="value">{{ formatCurrency(averageBookingValue) }}</span>
            </div>
          </div>
        </div>

        <!-- Pending Payouts -->
        <div class="summary-card card-warning">
          <div class="card-header">
            <h3>واریزهای در انتظار</h3>
            <svg viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm1-12a1 1 0 10-2 0v4a1 1 0 00.293.707l2.828 2.829a1 1 0 101.415-1.415L11 9.586V6z" clip-rule="evenodd" />
            </svg>
          </div>
          <div class="card-value">
            {{ formatCurrency(dashboard.pendingPayoutAmount) }}
          </div>
          <div class="card-meta">
            <span v-if="dashboard.nextPayoutDate" class="text-muted">
              تاریخ واریز: {{ formatRelativeDate(dashboard.nextPayoutDate) }}
            </span>
            <span v-else class="text-muted">واریز در انتظار نداشت</span>
          </div>
        </div>

        <!-- Previous Month -->
        <div class="summary-card">
          <div class="card-header">
            <h3>درآمد ماه قبل</h3>
            <svg viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M6 2a1 1 0 00-1 1v1H4a2 2 0 00-2 2v10a2 2 0 002 2h12a2 2 0 002-2V6a2 2 0 00-2-2h-1V3a1 1 0 10-2 0v1H7V3a1 1 0 00-1-1zm0 5a1 1 0 000 2h8a1 1 0 100-2H6z" clip-rule="evenodd" />
            </svg>
          </div>
          <div class="card-value">
            {{ formatCurrency(dashboard.previousMonthEarnings.netEarnings) }}
          </div>
          <div class="card-meta">
            <span class="text-muted">
              {{ dashboard.previousMonthEarnings.completedBookings }} رزرو تکمیل شده
            </span>
          </div>
        </div>
      </div>

      <!-- Quick Actions -->
      <div class="quick-actions">
        <button @click="handleRequestPayout" class="action-btn btn-primary">
          <svg viewBox="0 0 20 20" fill="currentColor">
            <path d="M8.433 7.418c.155-.103.346-.196.567-.267v1.698a2.305 2.305 0 01-.567-.267C8.07 8.34 8 8.114 8 8c0-.114.07-.34.433-.582zM11 12.849v-1.698c.22.071.412.164.567.267.364.243.433.468.433.582 0 .114-.07.34-.433.582a2.305 2.305 0 01-.567.267z" />
            <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm1-13a1 1 0 10-2 0v.092a4.535 4.535 0 00-1.676.662C6.602 6.234 6 7.009 6 8c0 .99.602 1.765 1.324 2.246.48.32 1.054.545 1.676.662v1.941c-.391-.127-.68-.317-.843-.504a1 1 0 10-1.51 1.31c.562.649 1.413 1.076 2.353 1.253V15a1 1 0 102 0v-.092a4.535 4.535 0 001.676-.662C13.398 13.766 14 12.991 14 12c0-.99-.602-1.765-1.324-2.246A4.535 4.535 0 0011 9.092V7.151c.391.127.68.317.843.504a1 1 0 101.511-1.31c-.563-.649-1.413-1.076-2.354-1.253V5z" clip-rule="evenodd" />
          </svg>
          درخواست واریز
        </button>
        <button @click="$emit('view-transactions')" class="action-btn">
          <svg viewBox="0 0 20 20" fill="currentColor">
            <path d="M9 2a1 1 0 000 2h2a1 1 0 100-2H9z" />
            <path fill-rule="evenodd" d="M4 5a2 2 0 012-2 3 3 0 003 3h2a3 3 0 003-3 2 2 0 012 2v11a2 2 0 01-2 2H6a2 2 0 01-2-2V5zm3 4a1 1 0 000 2h.01a1 1 0 100-2H7zm3 0a1 1 0 000 2h3a1 1 0 100-2h-3zm-3 4a1 1 0 100 2h.01a1 1 0 100-2H7zm3 0a1 1 0 100 2h3a1 1 0 100-2h-3z" clip-rule="evenodd" />
          </svg>
          مشاهده تراکنش‌ها
        </button>
        <button @click="$emit('view-payouts')" class="action-btn">
          <svg viewBox="0 0 20 20" fill="currentColor">
            <path d="M4 4a2 2 0 00-2 2v1h16V6a2 2 0 00-2-2H4z" />
            <path fill-rule="evenodd" d="M18 9H2v5a2 2 0 002 2h12a2 2 0 002-2V9zM4 13a1 1 0 011-1h1a1 1 0 110 2H5a1 1 0 01-1-1zm5-1a1 1 0 100 2h1a1 1 0 100-2H9z" clip-rule="evenodd" />
          </svg>
          واریزهای من
        </button>
      </div>

      <!-- Recent Transactions -->
      <div class="recent-section">
        <div class="section-header">
          <h2>آخرین تراکنش‌ها</h2>
          <button @click="$emit('view-transactions')" class="link-btn">
            مشاهده همه
            <svg viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M12.707 5.293a1 1 0 010 1.414L9.414 10l3.293 3.293a1 1 0 01-1.414 1.414l-4-4a1 1 0 010-1.414l4-4a1 1 0 011.414 0z" clip-rule="evenodd" />
            </svg>
          </button>
        </div>

        <div v-if="dashboard.recentTransactions.length > 0" class="transaction-list">
          <div
            v-for="transaction in dashboard.recentTransactions"
            :key="transaction.id"
            class="transaction-item"
          >
            <div class="transaction-icon" :class="getTransactionClass(transaction.type)">
              <svg v-if="transaction.type === 'Booking'" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm1-11a1 1 0 10-2 0v3.586L7.707 9.293a1 1 0 00-1.414 1.414l3 3a1 1 0 001.414 0l3-3a1 1 0 00-1.414-1.414L11 10.586V7z" clip-rule="evenodd" />
              </svg>
              <svg v-else-if="transaction.type === 'Refund'" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm1-11a1 1 0 10-2 0v3.586L7.707 9.293a1 1 0 00-1.414 1.414l3 3a1 1 0 001.414 0l3-3a1 1 0 00-1.414-1.414L11 10.586V7z" clip-rule="evenodd" />
              </svg>
              <svg v-else viewBox="0 0 20 20" fill="currentColor">
                <path d="M8.433 7.418c.155-.103.346-.196.567-.267v1.698a2.305 2.305 0 01-.567-.267C8.07 8.34 8 8.114 8 8c0-.114.07-.34.433-.582zM11 12.849v-1.698c.22.071.412.164.567.267.364.243.433.468.433.582 0 .114-.07.34-.433.582a2.305 2.305 0 01-.567.267z" />
              </svg>
            </div>
            <div class="transaction-info">
              <p class="transaction-desc">{{ transaction.description }}</p>
              <p class="transaction-date">{{ formatRelativeDate(transaction.createdAt) }}</p>
            </div>
            <div class="transaction-amount" :class="{ positive: isIncomeTransaction(transaction.type), negative: !isIncomeTransaction(transaction.type) }">
              {{ isIncomeTransaction(transaction.type) ? '+' : '-' }}{{ formatCurrency(transaction.amount) }}
            </div>
          </div>
        </div>

        <div v-else class="empty-state">
          <p>تراکنشی یافت نشد</p>
        </div>
      </div>

      <!-- Recent Payouts -->
      <div class="recent-section">
        <div class="section-header">
          <h2>آخرین واریزها</h2>
          <button @click="$emit('view-payouts')" class="link-btn">
            مشاهده همه
            <svg viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M12.707 5.293a1 1 0 010 1.414L9.414 10l3.293 3.293a1 1 0 01-1.414 1.414l-4-4a1 1 0 010-1.414l4-4a1 1 0 011.414 0z" clip-rule="evenodd" />
            </svg>
          </button>
        </div>

        <div v-if="dashboard.recentPayouts.length > 0" class="payout-list">
          <div
            v-for="payout in dashboard.recentPayouts"
            :key="payout.id"
            class="payout-item"
          >
            <div class="payout-info">
              <p class="payout-amount">{{ formatCurrency(payout.amount) }}</p>
              <p class="payout-date">{{ formatRelativeDate(payout.requestedAt) }}</p>
            </div>
            <div class="payout-status">
              <span class="status-badge" :style="{ backgroundColor: getPayoutStatusColor(payout.status) }">
                {{ getPayoutStatusLabel(payout.status) }}
              </span>
            </div>
          </div>
        </div>

        <div v-else class="empty-state">
          <p>واریزی یافت نشد</p>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useProviderStore } from '@/modules/provider/stores/provider.store'
import { financialService } from '@/modules/provider/services/financial.service'
import type { FinancialDashboard } from '@/modules/provider/types/financial.types'
import {
  formatCurrency,
  formatGrowthRate,
  formatRelativeDate,
  calculateGrowthRate,
  getPayoutStatusLabel,
  getPayoutStatusColor,
  isIncomeTransaction,
  TransactionType,
} from '@/modules/provider/types/financial.types'

// ============================================================================
// Emits
// ============================================================================

const emit = defineEmits<{
  (e: 'request-payout'): void
  (e: 'view-transactions'): void
  (e: 'view-payouts'): void
}>()

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
const dashboard = ref<FinancialDashboard | null>(null)

// ============================================================================
// Computed
// ============================================================================

const growthRate = computed(() => {
  if (!dashboard.value) return 0
  return calculateGrowthRate(
    dashboard.value.currentMonthEarnings.netEarnings,
    dashboard.value.previousMonthEarnings.netEarnings
  )
})

const averageBookingValue = computed(() => {
  if (!dashboard.value || dashboard.value.currentMonthEarnings.completedBookings === 0) return 0
  return dashboard.value.currentMonthEarnings.netEarnings / dashboard.value.currentMonthEarnings.completedBookings
})

// ============================================================================
// Lifecycle
// ============================================================================

onMounted(() => {
  loadDashboard()
})

// ============================================================================
// Methods
// ============================================================================

async function loadDashboard(): Promise<void> {
  if (!providerId.value) {
    error.value = 'شناسه ارائه‌دهنده یافت نشد'
    return
  }

  loading.value = true
  error.value = null

  try {
    dashboard.value = await financialService.getFinancialDashboard(providerId.value)
    console.log('[FinancialDashboard] Dashboard loaded:', dashboard.value)
  } catch (err) {
    console.error('[FinancialDashboard] Error loading dashboard:', err)
    error.value = 'خطا در بارگذاری اطلاعات مالی'
  } finally {
    loading.value = false
  }
}

function getTransactionClass(type: TransactionType): string {
  switch (type) {
    case TransactionType.Booking:
      return 'income'
    case TransactionType.Refund:
    case TransactionType.Commission:
      return 'expense'
    default:
      return 'neutral'
  }
}

function handleRequestPayout(): void {
  emit('request-payout')
}
</script>

<style scoped>
.financial-dashboard {
  width: 100%;
}

/* Loading & Error States */
.loading-state,
.error-state {
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

.icon-error {
  width: 4rem;
  height: 4rem;
  color: #f56565;
  margin: 0 auto 1rem;
}

/* Summary Cards */
.summary-cards {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
  gap: 1.5rem;
  margin-bottom: 2rem;
}

.summary-card {
  background: white;
  border-radius: 0.75rem;
  padding: 1.5rem;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  transition: box-shadow 0.2s;
}

.summary-card:hover {
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
}

.summary-card.card-primary {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
}

.summary-card.card-warning {
  background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
  color: white;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
}

.card-header h3 {
  font-size: 0.875rem;
  font-weight: 500;
  margin: 0;
  opacity: 0.9;
}

.card-header svg {
  width: 1.5rem;
  height: 1.5rem;
  opacity: 0.7;
}

.card-value {
  font-size: 1.875rem;
  font-weight: 700;
  margin: 0.5rem 0;
}

.card-meta {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.75rem;
  margin-bottom: 1rem;
}

.growth {
  display: flex;
  align-items: center;
  gap: 0.25rem;
  padding: 0.25rem 0.5rem;
  border-radius: 0.25rem;
  font-weight: 600;
}

.growth.positive {
  background: rgba(16, 185, 129, 0.2);
  color: #10b981;
}

.growth.negative {
  background: rgba(239, 68, 68, 0.2);
  color: #ef4444;
}

.growth svg {
  width: 1rem;
  height: 1rem;
}

.text-muted {
  opacity: 0.7;
}

.card-footer {
  border-top: 1px solid rgba(255, 255, 255, 0.2);
  padding-top: 1rem;
  display: flex;
  justify-content: space-between;
}

.summary-card:not(.card-primary):not(.card-warning) .card-footer {
  border-top-color: #e2e8f0;
}

.footer-item {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.footer-item .label {
  font-size: 0.75rem;
  opacity: 0.7;
}

.footer-item .value {
  font-size: 0.875rem;
  font-weight: 600;
}

/* Quick Actions */
.quick-actions {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 1rem;
  margin-bottom: 2rem;
}

.action-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 0.875rem 1.5rem;
  background: white;
  border: 1px solid #e2e8f0;
  border-radius: 0.5rem;
  font-size: 0.875rem;
  font-weight: 500;
  color: #2d3748;
  cursor: pointer;
  transition: all 0.2s;
}

.action-btn:hover {
  background: #f7fafc;
  border-color: #cbd5e0;
}

.action-btn.btn-primary {
  background: #3182ce;
  border-color: #3182ce;
  color: white;
}

.action-btn.btn-primary:hover {
  background: #2c5aa0;
}

.action-btn svg {
  width: 1.25rem;
  height: 1.25rem;
}

/* Recent Sections */
.recent-section {
  background: white;
  border-radius: 0.75rem;
  padding: 1.5rem;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  margin-bottom: 1.5rem;
}

.section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1.5rem;
}

.section-header h2 {
  font-size: 1.25rem;
  font-weight: 600;
  color: #1a202c;
  margin: 0;
}

.link-btn {
  display: flex;
  align-items: center;
  gap: 0.25rem;
  background: none;
  border: none;
  color: #3182ce;
  font-size: 0.875rem;
  font-weight: 500;
  cursor: pointer;
  transition: color 0.2s;
}

.link-btn:hover {
  color: #2c5aa0;
}

.link-btn svg {
  width: 1rem;
  height: 1rem;
}

/* Transaction List */
.transaction-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.transaction-item {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 1rem;
  background: #f7fafc;
  border-radius: 0.5rem;
  transition: background 0.2s;
}

.transaction-item:hover {
  background: #edf2f7;
}

.transaction-icon {
  width: 2.5rem;
  height: 2.5rem;
  border-radius: 0.5rem;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.transaction-icon.income {
  background: rgba(16, 185, 129, 0.1);
  color: #10b981;
}

.transaction-icon.expense {
  background: rgba(239, 68, 68, 0.1);
  color: #ef4444;
}

.transaction-icon.neutral {
  background: rgba(107, 114, 128, 0.1);
  color: #6b7280;
}

.transaction-icon svg {
  width: 1.25rem;
  height: 1.25rem;
}

.transaction-info {
  flex: 1;
}

.transaction-desc {
  font-size: 0.875rem;
  font-weight: 500;
  color: #2d3748;
  margin: 0 0 0.25rem 0;
}

.transaction-date {
  font-size: 0.75rem;
  color: #718096;
  margin: 0;
}

.transaction-amount {
  font-size: 1rem;
  font-weight: 600;
}

.transaction-amount.positive {
  color: #10b981;
}

.transaction-amount.negative {
  color: #ef4444;
}

/* Payout List */
.payout-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.payout-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1rem;
  background: #f7fafc;
  border-radius: 0.5rem;
}

.payout-info {
  flex: 1;
}

.payout-amount {
  font-size: 1rem;
  font-weight: 600;
  color: #2d3748;
  margin: 0 0 0.25rem 0;
}

.payout-date {
  font-size: 0.75rem;
  color: #718096;
  margin: 0;
}

.status-badge {
  padding: 0.375rem 0.75rem;
  border-radius: 0.375rem;
  font-size: 0.75rem;
  font-weight: 500;
  color: white;
}

/* Empty State */
.empty-state {
  text-align: center;
  padding: 3rem 1rem;
  color: #718096;
}

.empty-state p {
  margin: 0;
  font-size: 0.875rem;
}

/* Responsive */
@media (max-width: 768px) {
  .summary-cards {
    grid-template-columns: 1fr;
  }

  .quick-actions {
    grid-template-columns: 1fr;
  }

  .card-value {
    font-size: 1.5rem;
  }
}
</style>
