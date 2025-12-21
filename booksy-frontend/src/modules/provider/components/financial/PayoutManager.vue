<template>
  <div class="payout-manager" dir="rtl">
    <!-- Header with Create Button -->
    <div class="manager-header">
      <div>
        <h2>مدیریت واریزها</h2>
        <p>{{ totalPayouts }} واریز</p>
      </div>
      <button @click="showCreateModal = true" class="btn btn-primary">
        <svg viewBox="0 0 20 20" fill="currentColor">
          <path fill-rule="evenodd" d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z" clip-rule="evenodd" />
        </svg>
        درخواست واریز
      </button>
    </div>

    <!-- Status Filter -->
    <div class="filter-tabs">
      <button
        v-for="status in statusOptions"
        :key="status.value || 'all'"
        @click="filterByStatus(status.value as PayoutStatus | undefined)"
        :class="{ active: currentStatus === status.value }"
        class="filter-tab"
      >
        {{ status.label }}
      </button>
    </div>

    <!-- Loading/Error States -->
    <div v-if="loading" class="loading-state">
      <div class="spinner"></div>
      <p>در حال بارگذاری...</p>
    </div>

    <div v-else-if="error" class="error-state">
      <p>{{ error }}</p>
      <button @click="loadPayouts" class="btn btn-secondary">تلاش مجدد</button>
    </div>

    <!-- Payout List -->
    <div v-else-if="payouts.length > 0" class="payout-list">
      <div v-for="payout in payouts" :key="payout.id" class="payout-card">
        <div class="payout-info">
          <div class="payout-amount">{{ formatCurrency(payout.amount) }}</div>
          <div class="payout-method">{{ getPayoutMethodLabel(payout.method) }}</div>
          <div class="payout-date">درخواست: {{ formatRelativeDate(payout.requestedAt) }}</div>
        </div>
        <div class="payout-status">
          <span class="status-badge" :style="{ backgroundColor: getPayoutStatusColor(payout.status) }">
            {{ getPayoutStatusLabel(payout.status) }}
          </span>
          <div v-if="payout.completedAt" class="completed-date">
            تکمیل: {{ formatRelativeDate(payout.completedAt) }}
          </div>
        </div>
      </div>
    </div>

    <div v-else class="empty-state">
      <p>واریزی یافت نشد</p>
    </div>

    <!-- Create Payout Modal -->
    <Teleport to="body">
      <div v-if="showCreateModal" class="modal-overlay" @click="showCreateModal = false">
        <div class="modal-content" @click.stop>
          <div class="modal-header">
            <h3>درخواست واریز جدید</h3>
            <button @click="showCreateModal = false" class="btn-close">×</button>
          </div>

          <div class="modal-body">
            <div class="form-group">
              <label>مبلغ (تومان)</label>
              <input
                v-model.number="createForm.amount"
                type="number"
                class="form-control"
                placeholder="مبلغ مورد نظر را وارد کنید"
                min="10000"
              />
            </div>

            <div class="form-group">
              <label>روش پرداخت</label>
              <select v-model="createForm.method" class="form-control">
                <option value="BankTransfer">انتقال بانکی</option>
                <option value="ZarinPal">زرین‌پال</option>
              </select>
            </div>

            <div class="form-group">
              <label>یادداشت (اختیاری)</label>
              <textarea
                v-model="createForm.notes"
                class="form-control"
                placeholder="توضیحات اضافی..."
                rows="3"
              ></textarea>
            </div>
          </div>

          <div class="modal-footer">
            <button @click="handleCreatePayout" :disabled="creating" class="btn btn-primary">
              <span v-if="creating">در حال ثبت...</span>
              <span v-else>ثبت درخواست</span>
            </button>
            <button @click="showCreateModal = false" :disabled="creating" class="btn btn-secondary">
              انصراف
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useProviderStore } from '@/modules/provider/stores/provider.store'
import { financialService } from '@/modules/provider/services/financial.service'
import type { Payout, PayoutStatus, PayoutMethod, CreatePayoutRequest } from '@/modules/provider/types/financial.types'
import {
  formatCurrency,
  formatRelativeDate,
  getPayoutStatusLabel,
  getPayoutStatusColor,
  PAYOUT_METHOD_LABELS,
} from '@/modules/provider/types/financial.types'

const providerStore = useProviderStore()
const providerId = computed(() => providerStore.currentProvider?.id || '')

const loading = ref(false)
const error = ref<string | null>(null)
const payouts = ref<Payout[]>([])
const totalPayouts = ref(0)
const currentStatus = ref<PayoutStatus | undefined>(undefined)

const showCreateModal = ref(false)
const creating = ref(false)
const createForm = ref<CreatePayoutRequest>({
  providerId: '',
  amount: 0,
  method: 'BankTransfer' as PayoutMethod,
  notes: '',
})

const statusOptions = [
  { value: undefined, label: 'همه' },
  { value: 'Pending', label: 'در انتظار' },
  { value: 'Processing', label: 'در حال پردازش' },
  { value: 'Completed', label: 'تکمیل شده' },
]

onMounted(() => {
  loadPayouts()
})

async function loadPayouts(): Promise<void> {
  if (!providerId.value) return

  loading.value = true
  error.value = null

  try {
    const response = await financialService.getProviderPayouts({
      providerId: providerId.value,
      status: currentStatus.value,
      page: 1,
      pageSize: 50,
    })

    payouts.value = response.items
    totalPayouts.value = response.totalCount
  } catch (err) {
    console.error('[PayoutManager] Error:', err)
    error.value = 'خطا در بارگذاری واریزها'
  } finally {
    loading.value = false
  }
}

function filterByStatus(status: PayoutStatus | undefined): void {
  currentStatus.value = status
  loadPayouts()
}

async function handleCreatePayout(): Promise<void> {
  if (!providerId.value || createForm.value.amount < 10000) {
    alert('مبلغ باید حداقل ۱۰۰۰۰ تومان باشد')
    return
  }

  creating.value = true

  try {
    createForm.value.providerId = providerId.value
    await financialService.createPayout(createForm.value)

    showCreateModal.value = false
    createForm.value = {
      providerId: '',
      amount: 0,
      method: 'BankTransfer' as PayoutMethod,
      notes: '',
    }

    await loadPayouts()
  } catch (err) {
    console.error('[PayoutManager] Create error:', err)
    alert('خطا در ثبت درخواست واریز')
  } finally {
    creating.value = false
  }
}

function getPayoutMethodLabel(method: PayoutMethod): string {
  return PAYOUT_METHOD_LABELS[method] || method
}
</script>

<style scoped>
.payout-manager {
  width: 100%;
}

.manager-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1.5rem;
}

.manager-header h2 {
  font-size: 1.5rem;
  font-weight: 700;
  color: #1a202c;
  margin: 0 0 0.25rem 0;
}

.manager-header p {
  color: #718096;
  font-size: 0.875rem;
  margin: 0;
}

.filter-tabs {
  display: flex;
  gap: 0.5rem;
  margin-bottom: 1.5rem;
  border-bottom: 2px solid #e2e8f0;
}

.filter-tab {
  padding: 0.75rem 1.5rem;
  background: none;
  border: none;
  border-bottom: 2px solid transparent;
  margin-bottom: -2px;
  font-size: 0.875rem;
  font-weight: 500;
  color: #718096;
  cursor: pointer;
  transition: all 0.2s;
}

.filter-tab.active {
  color: #3182ce;
  border-bottom-color: #3182ce;
}

.loading-state,
.error-state,
.empty-state {
  text-align: center;
  padding: 4rem 2rem;
  color: #718096;
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

.payout-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.payout-card {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1.5rem;
  background: white;
  border: 1px solid #e2e8f0;
  border-radius: 0.5rem;
  transition: box-shadow 0.2s;
}

.payout-card:hover {
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
}

.payout-info {
  flex: 1;
}

.payout-amount {
  font-size: 1.25rem;
  font-weight: 700;
  color: #2d3748;
  margin-bottom: 0.5rem;
}

.payout-method,
.payout-date,
.completed-date {
  font-size: 0.875rem;
  color: #718096;
  margin-bottom: 0.25rem;
}

.payout-status {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 0.5rem;
}

.status-badge {
  padding: 0.5rem 1rem;
  border-radius: 0.375rem;
  font-size: 0.75rem;
  font-weight: 600;
  color: white;
}

.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.modal-content {
  background: white;
  border-radius: 0.5rem;
  max-width: 500px;
  width: 90%;
  max-height: 90vh;
  overflow-y: auto;
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1.5rem;
  border-bottom: 1px solid #e2e8f0;
}

.modal-header h3 {
  font-size: 1.25rem;
  font-weight: 600;
  margin: 0;
}

.btn-close {
  width: 2rem;
  height: 2rem;
  border: none;
  background: transparent;
  font-size: 1.5rem;
  color: #718096;
  cursor: pointer;
}

.modal-body {
  padding: 1.5rem;
}

.form-group {
  margin-bottom: 1.25rem;
}

.form-group label {
  display: block;
  font-size: 0.875rem;
  font-weight: 500;
  color: #2d3748;
  margin-bottom: 0.5rem;
}

.form-control {
  width: 100%;
  padding: 0.625rem 0.875rem;
  font-size: 0.875rem;
  border: 1px solid #cbd5e0;
  border-radius: 0.375rem;
  transition: all 0.2s;
}

.form-control:focus {
  outline: none;
  border-color: #3182ce;
  box-shadow: 0 0 0 3px rgba(49, 130, 206, 0.1);
}

.modal-footer {
  display: flex;
  justify-content: flex-end;
  gap: 0.75rem;
  padding: 1.5rem;
  border-top: 1px solid #e2e8f0;
}

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

.btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn-primary {
  background: #3182ce;
  color: white;
}

.btn-primary:hover:not(:disabled) {
  background: #2c5aa0;
}

.btn-secondary {
  background: #e2e8f0;
  color: #2d3748;
}

.btn-secondary:hover:not(:disabled) {
  background: #cbd5e0;
}

.btn svg {
  width: 1.25rem;
  height: 1.25rem;
}
</style>
