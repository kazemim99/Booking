<template>
  <div class="holiday-manager">
    <!-- Header -->
    <div class="manager-header">
      <h3 class="manager-title">{{ $t('provider.hours.holidays.title') }}</h3>
      <button class="btn btn-primary" @click="openAddModal">
        <i class="bi bi-plus-lg"></i>
        {{ $t('provider.hours.holidays.addHoliday') }}
      </button>
    </div>

    <!-- Filters -->
    <div class="filters">
      <div class="filter-group">
        <label>{{ $t('provider.hours.holidays.filterByYear') }}</label>
        <select v-model="selectedYear" class="form-select">
          <option :value="null">{{ $t('common.all') }}</option>
          <option v-for="year in availableYears" :key="year" :value="year">
            {{ year }}
          </option>
        </select>
      </div>

      <div class="filter-group">
        <label>
          <input
            v-model="showPast"
            type="checkbox"
            class="form-check-input"
          />
          {{ $t('provider.hours.holidays.showPast') }}
        </label>
      </div>

      <div class="search-group">
        <input
          v-model="searchTerm"
          type="text"
          class="form-control"
          :placeholder="$t('provider.hours.holidays.searchPlaceholder')"
        />
        <i class="bi bi-search"></i>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="isLoading" class="loading-state">
      <div class="spinner-border" role="status">
        <span class="visually-hidden">{{ $t('common.loading') }}</span>
      </div>
    </div>

    <!-- Empty State -->
    <div v-else-if="filteredHolidays.length === 0" class="empty-state">
      <i class="bi bi-calendar-x"></i>
      <h4>{{ $t('provider.hours.holidays.emptyTitle') }}</h4>
      <p>{{ $t('provider.hours.holidays.emptyMessage') }}</p>
      <button class="btn btn-primary" @click="openAddModal">
        {{ $t('provider.hours.holidays.addFirstHoliday') }}
      </button>
    </div>

    <!-- Holidays List -->
    <div v-else class="holidays-list">
      <div
        v-for="holiday in filteredHolidays"
        :key="holiday.id"
        class="holiday-item"
        :class="{ 'is-past': isPast(holiday.date) }"
      >
        <div class="holiday-info">
          <div class="holiday-date">
            <i class="bi bi-calendar-event"></i>
            <span class="date-text">{{ formatDate(holiday.date) }}</span>
            <span v-if="holiday.isRecurring" class="recurring-badge">
              <i class="bi bi-arrow-repeat"></i>
              {{ $t('provider.hours.holidays.recurring') }}
            </span>
          </div>
          <div class="holiday-reason">{{ holiday.reason }}</div>
          <div v-if="holiday.isRecurring" class="holiday-pattern">
            {{ formatRecurrencePattern(holiday.recurrencePattern) }}
          </div>
        </div>

        <div class="holiday-actions">
          <button
            class="btn btn-sm btn-outline-primary"
            @click="editHoliday(holiday)"
            :title="$t('common.edit')"
          >
            <i class="bi bi-pencil"></i>
          </button>
          <button
            class="btn btn-sm btn-outline-danger"
            @click="confirmDelete(holiday)"
            :title="$t('common.delete')"
          >
            <i class="bi bi-trash"></i>
          </button>
        </div>
      </div>
    </div>

    <!-- Holiday Form Modal -->
    <HolidayForm
      v-if="showModal"
      :holiday="editingHoliday"
      :provider-id="providerId"
      @close="closeModal"
      @saved="handleSaved"
    />

    <!-- Delete Confirmation Modal -->
    <div v-if="showDeleteConfirm" class="modal-overlay" @click.self="cancelDelete">
      <div class="modal-dialog">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">{{ $t('provider.hours.holidays.deleteConfirmTitle') }}</h5>
            <button type="button" class="btn-close" @click="cancelDelete"></button>
          </div>
          <div class="modal-body">
            <p>{{ $t('provider.hours.holidays.deleteConfirmMessage', { reason: deletingHoliday?.reason }) }}</p>
            <p v-if="affectedBookingsCount > 0" class="text-warning">
              <i class="bi bi-exclamation-triangle"></i>
              {{ $t('provider.hours.holidays.affectedBookings', { count: affectedBookingsCount }) }}
            </p>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-secondary" @click="cancelDelete">
              {{ $t('common.cancel') }}
            </button>
            <button type="button" class="btn btn-danger" @click="executeDelete" :disabled="isDeleting">
              <span v-if="isDeleting" class="spinner-border spinner-border-sm" role="status"></span>
              {{ $t('common.delete') }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useHoursStore } from '../../stores/hours.store'
import type { HolidaySchedule } from '../../types/hours.types'
import { RecurrencePattern } from '../../types/hours.types'
import HolidayForm from './HolidayForm.vue'
import { useI18n } from 'vue-i18n'
import { formatDate } from '@/core/utils'

// Props
interface Props {
  providerId: string
}

const props = defineProps<Props>()

// Composables
const hoursStore = useHoursStore()
const { t } = useI18n()

// State
const selectedYear = ref<number | null>(null)
const showPast = ref(false)
const searchTerm = ref('')
const showModal = ref(false)
const editingHoliday = ref<HolidaySchedule | null>(null)
const showDeleteConfirm = ref(false)
const deletingHoliday = ref<HolidaySchedule | null>(null)
const affectedBookingsCount = ref(0)
const isDeleting = ref(false)

// Computed
const isLoading = computed(() => hoursStore.state.isLoading)

const holidays = computed(() => hoursStore.state.holidays)

const availableYears = computed(() => {
  const years = new Set<number>()
  const currentYear = new Date().getFullYear()

  // Add current year and next year
  years.add(currentYear)
  years.add(currentYear + 1)

  // Add years from existing holidays
  holidays.value.forEach(h => {
    const year = new Date(h.date).getFullYear()
    years.add(year)
  })

  return Array.from(years).sort((a, b) => a - b)
})

const filteredHolidays = computed(() => {
  let filtered = holidays.value

  // Filter by year
  if (selectedYear.value !== null) {
    filtered = filtered.filter(h => {
      const year = new Date(h.date).getFullYear()
      return year === selectedYear.value
    })
  }

  // Filter past holidays
  if (!showPast.value) {
    const today = new Date().toISOString().split('T')[0]
    filtered = filtered.filter(h => h.date >= today)
  }

  // Search filter
  if (searchTerm.value.trim()) {
    const term = searchTerm.value.toLowerCase()
    filtered = filtered.filter(h =>
      h.reason.toLowerCase().includes(term) ||
      formatDate(h.date).toLowerCase().includes(term)
    )
  }

  // Sort by date
  return filtered.sort((a, b) => a.date.localeCompare(b.date))
})

// Methods
function openAddModal() {
  editingHoliday.value = null
  showModal.value = true
}

function editHoliday(holiday: HolidaySchedule) {
  editingHoliday.value = holiday
  showModal.value = true
}

function closeModal() {
  showModal.value = false
  editingHoliday.value = null
}

function handleSaved() {
  closeModal()
}

async function confirmDelete(holiday: HolidaySchedule) {
  deletingHoliday.value = holiday
  // TODO: Fetch affected bookings count from API
  affectedBookingsCount.value = 0
  showDeleteConfirm.value = true
}

function cancelDelete() {
  showDeleteConfirm.value = false
  deletingHoliday.value = null
  affectedBookingsCount.value = 0
}

async function executeDelete() {
  if (!deletingHoliday.value?.id) return

  isDeleting.value = true
  try {
    await hoursStore.removeHoliday(props.providerId, deletingHoliday.value.id)
    showDeleteConfirm.value = false
    deletingHoliday.value = null
  } catch (error) {
    console.error('Failed to delete holiday:', error)
    // Error is already set in store
  } finally {
    isDeleting.value = false
  }
}

function isPast(date: string): boolean {
  const today = new Date().toISOString().split('T')[0]
  return date < today
}

function formatRecurrencePattern(pattern?: RecurrencePattern): string {
  if (!pattern || pattern === RecurrencePattern.None) return ''

  switch (pattern) {
    case RecurrencePattern.Daily:
      return t('provider.hours.holidays.patterns.daily')
    case RecurrencePattern.Weekly:
      return t('provider.hours.holidays.patterns.weekly')
    case RecurrencePattern.Monthly:
      return t('provider.hours.holidays.patterns.monthly')
    case RecurrencePattern.Yearly:
      return t('provider.hours.holidays.patterns.yearly')
    default:
      return ''
  }
}

// Lifecycle
onMounted(async () => {
  if (holidays.value.length === 0) {
    await hoursStore.loadSchedule(props.providerId)
  }
})
</script>

<style scoped>
.holiday-manager {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.manager-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 1rem;
}

.manager-title {
  margin: 0;
  font-size: 1.25rem;
  font-weight: 600;
}

.filters {
  display: flex;
  gap: 1rem;
  flex-wrap: wrap;
  padding: 1rem;
  background-color: var(--bs-light);
  border-radius: 0.5rem;
}

.filter-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.filter-group label {
  font-size: 0.875rem;
  font-weight: 500;
  margin-bottom: 0;
}

.search-group {
  position: relative;
  flex: 1;
  min-width: 250px;
}

.search-group input {
  padding-right: 2.5rem;
}

.search-group i {
  position: absolute;
  right: 0.75rem;
  top: 50%;
  transform: translateY(-50%);
  color: var(--bs-secondary);
}

.loading-state {
  display: flex;
  justify-content: center;
  align-items: center;
  padding: 3rem;
}

.empty-state {
  text-align: center;
  padding: 3rem 1rem;
}

.empty-state i {
  font-size: 4rem;
  color: var(--bs-secondary);
  margin-bottom: 1rem;
}

.empty-state h4 {
  font-size: 1.25rem;
  font-weight: 600;
  margin-bottom: 0.5rem;
}

.empty-state p {
  color: var(--bs-secondary);
  margin-bottom: 1.5rem;
}

.holidays-list {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.holiday-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1rem;
  border: 1px solid var(--bs-border-color);
  border-radius: 0.5rem;
  background-color: white;
  transition: all 0.2s;
}

.holiday-item:hover {
  border-color: var(--bs-primary);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.holiday-item.is-past {
  opacity: 0.6;
  background-color: var(--bs-light);
}

.holiday-info {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.holiday-date {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-weight: 600;
  color: var(--bs-dark);
}

.holiday-date i {
  color: var(--bs-danger);
}

.date-text {
  flex: 1;
}

.recurring-badge {
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
  padding: 0.125rem 0.5rem;
  background-color: var(--bs-info);
  color: white;
  border-radius: 1rem;
  font-size: 0.75rem;
  font-weight: 500;
}

.holiday-reason {
  color: var(--bs-body-color);
}

.holiday-pattern {
  font-size: 0.875rem;
  color: var(--bs-secondary);
}

.holiday-actions {
  display: flex;
  gap: 0.5rem;
}

.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1050;
}

.modal-dialog {
  max-width: 500px;
  width: 90%;
}

.text-warning i {
  margin-right: 0.5rem;
}

/* Responsive */
@media (max-width: 768px) {
  .manager-header {
    flex-direction: column;
    align-items: stretch;
  }

  .holiday-item {
    flex-direction: column;
    align-items: stretch;
    gap: 1rem;
  }

  .holiday-actions {
    justify-content: flex-end;
  }
}
</style>
