<template>
  <div class="provider-stats">
    <!-- Loading State -->
    <div v-if="isLoading" class="stats-loading">
      <Spinner size="medium" />
      <p>Loading statistics...</p>
    </div>

    <!-- Error State -->
    <Alert v-else-if="error" type="error" :message="error" />

    <!-- Stats Grid -->
    <div v-else-if="stats" class="stats-grid">
      <!-- Services Stats -->
      <Card class="stat-card">
        <div class="stat-icon services">
          <svg
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M21 13.255A23.931 23.931 0 0112 15c-3.183 0-6.22-.62-9-1.745M16 6V4a2 2 0 00-2-2h-4a2 2 0 00-2 2v2m4 6h.01M5 20h14a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"
            />
          </svg>
        </div>
        <div class="stat-content">
          <div class="stat-value">{{ stats.totalServices }}</div>
          <div class="stat-label">Total Services</div>
          <div class="stat-sublabel">{{ stats.activeServices }} active</div>
        </div>
        <div v-if="showTrend" class="stat-trend positive">
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
            <path
              fill-rule="evenodd"
              d="M12 7a1 1 0 110-2h5a1 1 0 011 1v5a1 1 0 11-2 0V8.414l-4.293 4.293a1 1 0 01-1.414 0L8 10.414l-4.293 4.293a1 1 0 01-1.414-1.414l5-5a1 1 0 011.414 0L11 10.586 14.586 7H12z"
              clip-rule="evenodd"
            />
          </svg>
          <span>+12%</span>
        </div>
      </Card>

      <!-- Staff Stats -->
      <Card class="stat-card">
        <div class="stat-icon staff">
          <svg
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857m0 0a5.002 5.002 0 00-9.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z"
            />
          </svg>
        </div>
        <div class="stat-content">
          <div class="stat-value">{{ stats.totalStaff }}</div>
          <div class="stat-label">Team Members</div>
          <div class="stat-sublabel">{{ stats.activeStaff }} active</div>
        </div>
      </Card>

      <!-- Bookings Stats -->
      <Card class="stat-card">
        <div class="stat-icon bookings">
          <svg
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"
            />
          </svg>
        </div>
        <div class="stat-content">
          <div class="stat-value">{{ formatNumber(stats.totalBookings) }}</div>
          <div class="stat-label">Total Bookings</div>
          <div class="stat-sublabel">{{ stats.completedBookings }} completed</div>
        </div>
        <div v-if="showTrend" class="stat-trend positive">
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
            <path
              fill-rule="evenodd"
              d="M12 7a1 1 0 110-2h5a1 1 0 011 1v5a1 1 0 11-2 0V8.414l-4.293 4.293a1 1 0 01-1.414 0L8 10.414l-4.293 4.293a1 1 0 01-1.414-1.414l5-5a1 1 0 011.414 0L11 10.586 14.586 7H12z"
              clip-rule="evenodd"
            />
          </svg>
          <span>+8%</span>
        </div>
      </Card>

      <!-- Revenue Stats -->
      <Card class="stat-card">
        <div class="stat-icon revenue">
          <svg
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
            />
          </svg>
        </div>
        <div class="stat-content">
          <div class="stat-value">{{ formatCurrency(stats.totalRevenue) }}</div>
          <div class="stat-label">Total Revenue</div>
          <div class="stat-sublabel">This month</div>
        </div>
        <div v-if="showTrend" class="stat-trend positive">
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
            <path
              fill-rule="evenodd"
              d="M12 7a1 1 0 110-2h5a1 1 0 011 1v5a1 1 0 11-2 0V8.414l-4.293 4.293a1 1 0 01-1.414 0L8 10.414l-4.293 4.293a1 1 0 01-1.414-1.414l5-5a1 1 0 011.414 0L11 10.586 14.586 7H12z"
              clip-rule="evenodd"
            />
          </svg>
          <span>+15%</span>
        </div>
      </Card>

      <!-- Reviews Stats (if available) -->
      <Card v-if="stats.averageRating || stats.totalReviews > 0" class="stat-card">
        <div class="stat-icon reviews">
          <svg
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.197-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z"
            />
          </svg>
        </div>
        <div class="stat-content">
          <div class="stat-value">
            {{ stats.averageRating ? stats.averageRating.toFixed(1) : 'N/A' }}
            <span class="stat-unit">/ 5.0</span>
          </div>
          <div class="stat-label">Average Rating</div>
          <div class="stat-sublabel">{{ stats.totalReviews }} reviews</div>
        </div>
      </Card>

      <!-- Registration Date -->
      <Card class="stat-card">
        <div class="stat-icon info">
          <svg
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
            />
          </svg>
        </div>
        <div class="stat-content">
          <div class="stat-value">{{ formatDaysActive(stats.registeredAt) }}</div>
          <div class="stat-label">Days Active</div>
          <div class="stat-sublabel">Since {{ formatDate(stats.registeredAt) }}</div>
        </div>
      </Card>
    </div>

    <!-- Additional Metrics (Optional) -->
    <div v-if="showDetailedMetrics && stats" class="detailed-metrics">
      <Card>
        <h3 class="metrics-title">Performance Metrics</h3>
        <div class="metrics-grid">
          <div class="metric-item">
            <div class="metric-label">Completion Rate</div>
            <div class="metric-value">
              {{ calculateCompletionRate(stats.completedBookings, stats.totalBookings) }}%
            </div>
            <div class="metric-bar">
              <div
                class="metric-bar-fill"
                :style="{
                  width: `${calculateCompletionRate(stats.completedBookings, stats.totalBookings)}%`,
                }"
              ></div>
            </div>
          </div>

          <div class="metric-item">
            <div class="metric-label">Cancellation Rate</div>
            <div class="metric-value">
              {{ calculateCancellationRate(stats.cancelledBookings, stats.totalBookings) }}%
            </div>
            <div class="metric-bar">
              <div
                class="metric-bar-fill danger"
                :style="{
                  width: `${calculateCancellationRate(stats.cancelledBookings, stats.totalBookings)}%`,
                }"
              ></div>
            </div>
          </div>

          <div class="metric-item">
            <div class="metric-label">Service Utilization</div>
            <div class="metric-value">
              {{ calculateUtilization(stats.activeServices, stats.totalServices) }}%
            </div>
            <div class="metric-bar">
              <div
                class="metric-bar-fill"
                :style="{
                  width: `${calculateUtilization(stats.activeServices, stats.totalServices)}%`,
                }"
              ></div>
            </div>
          </div>

          <div class="metric-item">
            <div class="metric-label">Staff Utilization</div>
            <div class="metric-value">
              {{ calculateUtilization(stats.activeStaff, stats.totalStaff) }}%
            </div>
            <div class="metric-bar">
              <div
                class="metric-bar-fill"
                :style="{ width: `${calculateUtilization(stats.activeStaff, stats.totalStaff)}%` }"
              ></div>
            </div>
          </div>
        </div>
      </Card>
    </div>

    <!-- Quick Actions (Optional) -->
    <div v-if="showQuickActions" class="quick-actions">
      <button class="action-btn" @click="$emit('refresh')">
        <svg
          xmlns="http://www.w3.org/2000/svg"
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
        >
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"
          />
        </svg>
        Refresh Stats
      </button>
      <button class="action-btn" @click="$emit('view-details')">
        <svg
          xmlns="http://www.w3.org/2000/svg"
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
        >
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z"
          />
        </svg>
        View Analytics
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { Card, Alert, Spinner } from '@/shared/components'
import type { ProviderStatistics } from '../types/provider.types'

// Props
interface Props {
  stats: ProviderStatistics | null
  isLoading?: boolean
  error?: string | null
  showTrend?: boolean
  showDetailedMetrics?: boolean
  showQuickActions?: boolean
}

withDefaults(defineProps<Props>(), {
  isLoading: false,
  error: null,
  showTrend: false,
  showDetailedMetrics: false,
  showQuickActions: false,
})

// Emits
defineEmits<{
  (e: 'refresh'): void
  (e: 'view-details'): void
}>()

// Formatters
const formatNumber = (num: number): string => {
  return new Intl.NumberFormat('en-US').format(num)
}

const formatCurrency = (amount: number): string => {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(amount)
}

const formatDate = (dateString: string): string => {
  const date = new Date(dateString)
  return new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  }).format(date)
}

const formatDaysActive = (dateString: string): string => {
  const registered = new Date(dateString)
  const now = new Date()
  const diffTime = Math.abs(now.getTime() - registered.getTime())
  const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24))
  return formatNumber(diffDays)
}

// Calculations
const calculateCompletionRate = (completed: number, total: number): number => {
  if (total === 0) return 0
  return Math.round((completed / total) * 100)
}

const calculateCancellationRate = (cancelled: number, total: number): number => {
  if (total === 0) return 0
  return Math.round((cancelled / total) * 100)
}

const calculateUtilization = (active: number, total: number): number => {
  if (total === 0) return 0
  return Math.round((active / total) * 100)
}
</script>

<style scoped>
.provider-stats {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.stats-loading {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 3rem;
  gap: 1rem;
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
  gap: 1.5rem;
}

.stat-card {
  position: relative;
  padding: 1.5rem;
  display: flex;
  gap: 1.25rem;
  transition: all 0.2s;
  cursor: default;
}

.stat-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 8px 16px rgba(0, 0, 0, 0.1);
}

.stat-icon {
  width: 60px;
  height: 60px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.stat-icon svg {
  width: 28px;
  height: 28px;
  color: white;
}

.stat-icon.services {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.stat-icon.staff {
  background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
}

.stat-icon.bookings {
  background: linear-gradient(135deg, #4facfe 0%, #00f2fe 100%);
}

.stat-icon.revenue {
  background: linear-gradient(135deg, #43e97b 0%, #38f9d7 100%);
}

.stat-icon.reviews {
  background: linear-gradient(135deg, #fa709a 0%, #fee140 100%);
}

.stat-icon.info {
  background: linear-gradient(135deg, #a8edea 0%, #fed6e3 100%);
}

.stat-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  justify-content: center;
}

.stat-value {
  font-size: 2rem;
  font-weight: 700;
  color: var(--color-text-primary);
  line-height: 1.2;
  margin-bottom: 0.25rem;
}

.stat-unit {
  font-size: 1rem;
  font-weight: 400;
  color: var(--color-text-tertiary);
}

.stat-label {
  font-size: 0.95rem;
  font-weight: 500;
  color: var(--color-text-secondary);
  margin-bottom: 0.125rem;
}

.stat-sublabel {
  font-size: 0.85rem;
  color: var(--color-text-tertiary);
}

.stat-trend {
  position: absolute;
  top: 1rem;
  right: 1rem;
  display: flex;
  align-items: center;
  gap: 0.25rem;
  padding: 0.375rem 0.625rem;
  border-radius: 6px;
  font-size: 0.85rem;
  font-weight: 600;
}

.stat-trend.positive {
  background: #d1fae5;
  color: #065f46;
}

.stat-trend.negative {
  background: #fee2e2;
  color: #991b1b;
}

.stat-trend svg {
  width: 14px;
  height: 14px;
}

/* Detailed Metrics */
.detailed-metrics {
  margin-top: 0.5rem;
}

.metrics-title {
  font-size: 1.25rem;
  font-weight: 600;
  margin: 0 0 1.5rem 0;
  color: var(--color-text-primary);
}

.metrics-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 2rem;
}

.metric-item {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.metric-label {
  font-size: 0.9rem;
  font-weight: 500;
  color: var(--color-text-secondary);
}

.metric-value {
  font-size: 1.5rem;
  font-weight: 700;
  color: var(--color-text-primary);
}

.metric-bar {
  height: 8px;
  background: var(--color-bg-tertiary);
  border-radius: 4px;
  overflow: hidden;
}

.metric-bar-fill {
  height: 100%;
  background: var(--color-primary);
  border-radius: 4px;
  transition: width 0.5s ease;
}

.metric-bar-fill.danger {
  background: var(--color-danger);
}

/* Quick Actions */
.quick-actions {
  display: flex;
  gap: 1rem;
  flex-wrap: wrap;
}

.action-btn {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.75rem 1.25rem;
  background: white;
  border: 1px solid var(--color-border);
  border-radius: 8px;
  font-size: 0.95rem;
  font-weight: 500;
  color: var(--color-text-primary);
  cursor: pointer;
  transition: all 0.2s;
}

.action-btn:hover {
  border-color: var(--color-primary);
  background: var(--color-bg-secondary);
}

.action-btn svg {
  width: 18px;
  height: 18px;
}

/* Responsive */
@media (max-width: 768px) {
  .stats-grid {
    grid-template-columns: 1fr;
  }

  .stat-card {
    padding: 1.25rem;
  }

  .stat-value {
    font-size: 1.75rem;
  }

  .metrics-grid {
    grid-template-columns: 1fr;
  }

  .quick-actions {
    flex-direction: column;
  }

  .action-btn {
    width: 100%;
    justify-content: center;
  }
}
</style>
