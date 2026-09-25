<template>
  <div class="exception-manager">
    <!-- Header -->
    <div class="manager-header">
      <h3 class="manager-title">{{ $t('provider.hours.exceptions.title') }}</h3>
      <button class="btn btn-primary" @click="openAddModal">
        <i class="bi bi-plus-lg"></i>
        {{ $t('provider.hours.exceptions.addException') }}
      </button>
    </div>

    <!-- Info Banner -->
    <div class="info-banner">
      <i class="bi bi-info-circle"></i>
      <span>{{ $t('provider.hours.exceptions.infoText') }}</span>
    </div>

    <!-- Filters -->
    <div class="filters">
      <div class="filter-group">
        <label>
          <input
            v-model="showPast"
            type="checkbox"
            class="form-check-input"
          />
          {{ $t('provider.hours.exceptions.showPast') }}
        </label>
      </div>

      <div class="search-group">
        <input
          v-model="searchTerm"
          type="text"
          class="form-control"
          :placeholder="$t('provider.hours.exceptions.searchPlaceholder')"
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
    <div v-else-if="filteredExceptions.length === 0" class="empty-state">
      <i class="bi bi-calendar2-minus"></i>
      <h4>{{ $t('provider.hours.exceptions.emptyTitle') }}</h4>
      <p>{{ $t('provider.hours.exceptions.emptyMessage') }}</p>
      <button class="btn btn-primary" @click="openAddModal">
        {{ $t('provider.hours.exceptions.addFirstException') }}
      </button>
    </div>

    <!-- Exceptions List -->
    <div v-else class="exceptions-list">
      <div
        v-for="exception in filteredExceptions"
        :key="exception.id"
        class="exception-item"
        :class="{ 'is-past': isPast(exception.date), 'is-closed': exception.isClosed }"
      >
        <div class="exception-info">
          <div class="exception-date">
            <i class="bi bi-calendar2-event"></i>
            <span class="date-text">{{ formatDate(exception.date) }}</span>
            <span v-if="exception.isClosed" class="closed-badge">
              <i class="bi bi-x-circle"></i>
              {{ $t('provider.hours.exceptions.closed') }}
            </span>
            <span v-else class="modified-badge">
              <i class="bi bi-clock-history"></i>
              {{ $t('provider.hours.exceptions.modified') }}
            </span>
          </div>

          <div class="exception-hours">
            <template v-if="exception.isClosed">
              <span class="closed-text">{{ $t('provider.hours.exceptions.closedAllDay') }}</span>
            </template>
            <template v-else>
              <span class="hours-text">
                {{ formatTime(exception.openTime!) }} - {{ formatTime(exception.closeTime!) }}
              </span>
            </template>
          </div>

          <div class="exception-reason">{{ exception.reason }}</div>
        </div>

        <div class="exception-actions">
          <button
            class="btn btn-sm btn-outline-primary"
            @click="editException(exception)"
            :title="$t('common.edit')"
          >
            <i class="bi bi-pencil"></i>
          </button>
          <button
            class="btn btn-sm btn-outline-danger"
            @click="confirmDelete(exception)"
            :title="$t('common.delete')"
          >
            <i class="bi bi-trash"></i>
          </button>
        </div>
      </div>
    </div>

    <!-- Exception Form Modal -->
    <ExceptionForm
      v-if="showModal"
      :exception="editingException"
      :provider-id="providerId"
      @close="closeModal"
      @saved="handleSaved"
    />

    <!-- Delete Confirmation Modal -->
    <div v-if="showDeleteConfirm" class="modal-overlay" @click.self="cancelDelete">
      <div class="modal-dialog">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">{{ $t('provider.hours.exceptions.deleteConfirmTitle') }}</h5>
            <button type="button" class="btn-close" @click="cancelDelete"></button>
          </div>
          <div class="modal-body">
            <p>{{ $t('provider.hours.exceptions.deleteConfirmMessage', { reason: deletingException?.reason }) }}</p>
            <p v-if="affectedBookingsCount > 0" class="text-warning">
              <i class="bi bi-exclamation-triangle"></i>
              {{ $t('provider.hours.exceptions.affectedBookings', { count: affectedBookingsCount }) }}
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
import type { ExceptionSchedule } from '../../types/hours.types'
import ExceptionForm from './ExceptionForm.vue'
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
const showPast = ref(false)
const searchTerm = ref('')
const showModal = ref(false)
const editingException = ref<ExceptionSchedule | null>(null)
const showDeleteConfirm = ref(false)
const deletingException = ref<ExceptionSchedule | null>(null)
const affectedBookingsCount = ref(0)
const isDeleting = ref(false)

// Computed
const isLoading = computed(() => hoursStore.state.isLoading)

const exceptions = computed(() => hoursStore.state.exceptions)

const filteredExceptions = computed(() => {
  let filtered = exceptions.value

  // Filter past exceptions
  if (!showPast.value) {
    const today = new Date().toISOString().split('T')[0]
    filtered = filtered.filter(e => e.date >= today)
  }

  // Search filter
  if (searchTerm.value.trim()) {
    const term = searchTerm.value.toLowerCase()
    filtered = filtered.filter(e =>
      e.reason.toLowerCase().includes(term) ||
      formatDate(e.date).toLowerCase().includes(term)
    )
  }

  // Sort by date (ascending)
  return filtered.sort((a, b) => a.date.localeCompare(b.date))
})

// Methods
function openAddModal() {
  editingException.value = null
  showModal.value = true
}

function editException(exception: ExceptionSchedule) {
  editingException.value = exception
  showModal.value = true
}

function closeModal() {
  showModal.value = false
  editingException.value = null
}

function handleSaved() {
  closeModal()
}

async function confirmDelete(exception: ExceptionSchedule) {
  deletingException.value = exception
  // TODO: Fetch affected bookings count from API
  affectedBookingsCount.value = 0
  showDeleteConfirm.value = true
}

function cancelDelete() {
  showDeleteConfirm.value = false
  deletingException.value = null
  affectedBookingsCount.value = 0
}

async function executeDelete() {
  if (!deletingException.value?.id) return

  isDeleting.value = true
  try {
    await hoursStore.removeException(props.providerId, deletingException.value.id)
    showDeleteConfirm.value = false
    deletingException.value = null
  } catch (error) {
    console.error('Failed to delete exception:', error)
    // Error is already set in store
  } finally {
    isDeleting.value = false
  }
}

function isPast(date: string): boolean {
  const today = new Date().toISOString().split('T')[0]
  return date < today
}

function formatTime(timeString: string): string {
  // timeString is in HH:mm format
  const [hours, minutes] = timeString.split(':')
  const hour = parseInt(hours)
  const ampm = hour >= 12 ? 'PM' : 'AM'
  const displayHour = hour % 12 || 12
  return `${displayHour}:${minutes} ${ampm}`
}

// Lifecycle
onMounted(async () => {
  if (exceptions.value.length === 0) {
    await hoursStore.loadSchedule(props.providerId)
  }
})
</script>

<style scoped>
.exception-manager {
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

.info-banner {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.75rem 1rem;
  background-color: var(--bs-info-bg-subtle);
  border: 1px solid var(--bs-info-border-subtle);
  border-radius: 0.5rem;
  color: var(--bs-info-text-emphasis);
}

.info-banner i {
  font-size: 1.25rem;
  flex-shrink: 0;
}

.filters {
  display: flex;
  gap: 1rem;
  flex-wrap: wrap;
  padding: 1rem;
  background-color: var(--bs-light);
  border-radius: 0.5rem;
}

.filter-group label {
  display: flex;
  align-items: center;
  gap: 0.5rem;
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

.exceptions-list {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.exception-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1rem;
  border: 1px solid var(--bs-border-color);
  border-left: 4px solid var(--bs-warning);
  border-radius: 0.5rem;
  background-color: white;
  transition: all 0.2s;
}

.exception-item:hover {
  border-color: var(--bs-warning);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.exception-item.is-closed {
  border-left-color: var(--bs-danger);
}

.exception-item.is-past {
  opacity: 0.6;
  background-color: var(--bs-light);
}

.exception-info {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.exception-date {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-weight: 600;
  color: var(--bs-dark);
}

.exception-date i {
  color: var(--bs-warning);
}

.exception-item.is-closed .exception-date i {
  color: var(--bs-danger);
}

.date-text {
  flex: 1;
}

.closed-badge,
.modified-badge {
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
  padding: 0.125rem 0.5rem;
  border-radius: 1rem;
  font-size: 0.75rem;
  font-weight: 500;
}

.closed-badge {
  background-color: var(--bs-danger);
  color: white;
}

.modified-badge {
  background-color: var(--bs-warning);
  color: var(--bs-dark);
}

.exception-hours {
  font-size: 0.875rem;
  color: var(--bs-body-color);
}

.hours-text {
  font-weight: 500;
  font-family: monospace;
}

.closed-text {
  color: var(--bs-danger);
  font-weight: 500;
}

.exception-reason {
  color: var(--bs-secondary);
  font-size: 0.875rem;
}

.exception-actions {
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

  .exception-item {
    flex-direction: column;
    align-items: stretch;
    gap: 1rem;
  }

  .exception-actions {
    justify-content: flex-end;
  }
}
</style>
