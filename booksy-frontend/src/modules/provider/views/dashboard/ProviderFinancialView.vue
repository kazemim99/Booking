<template>
  <DashboardLayout>
    <!-- Financial Dashboard Overview -->
    <FinancialDashboard
      v-if="financialView === 'dashboard'"
      @request-payout="financialView = 'payouts'"
      @view-transactions="financialView = 'transactions'"
      @view-payouts="financialView = 'payouts'"
    />

    <!-- Transaction History View -->
    <div v-else-if="financialView === 'transactions'">
      <button @click="financialView = 'dashboard'" class="btn-back mb-4">
        ← بازگشت به داشبورد
      </button>
      <TransactionHistory />
    </div>

    <!-- Payout Manager View -->
    <div v-else-if="financialView === 'payouts'">
      <button @click="financialView = 'dashboard'" class="btn-back mb-4">
        ← بازگشت به داشبورد
      </button>
      <PayoutManager />
    </div>
  </DashboardLayout>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import DashboardLayout from '../../components/dashboard/DashboardLayout.vue'
import FinancialDashboard from '../../components/financial/FinancialDashboard.vue'
import TransactionHistory from '../../components/financial/TransactionHistory.vue'
import PayoutManager from '../../components/financial/PayoutManager.vue'

const financialView = ref<'dashboard' | 'transactions' | 'payouts'>('dashboard')
</script>

<style scoped>
.btn-back {
  padding: 0.5rem 1rem;
  background: var(--color-gray-100);
  border: 1px solid var(--color-gray-300);
  border-radius: 0.375rem;
  cursor: pointer;
  font-size: 0.875rem;
  color: var(--color-gray-800);
  transition: all 0.2s;
}

.btn-back:hover {
  background: var(--color-gray-300);
}

.mb-4 {
  margin-bottom: 1rem;
}
</style>
