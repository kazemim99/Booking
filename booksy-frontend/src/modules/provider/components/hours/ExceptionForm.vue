<template>
  <div class="modal-overlay" @click.self="handleCancel">
    <div class="modal-dialog modal-dialog-centered">
      <div class="modal-content">
        <div class="modal-header">
          <h5 class="modal-title">
            {{ isEditing ? $t('provider.hours.exceptions.editException') : $t('provider.hours.exceptions.addException') }}
          </h5>
          <button type="button" class="btn-close" @click="handleCancel"></button>
        </div>

        <form @submit.prevent="handleSubmit">
          <div class="modal-body">
            <!-- Date Picker -->
            <div class="form-group">
              <label for="exception-date" class="form-label required">
                {{ $t('provider.hours.exceptions.date') }}
              </label>
              <input
                id="exception-date"
                v-model="formData.date"
                type="date"
                class="form-control"
                :class="{ 'is-invalid': errors.date }"
                :min="minDate"
                :disabled="isEditing"
                required
              />
              <div v-if="errors.date" class="invalid-feedback">
                {{ errors.date }}
              </div>
            </div>

            <!-- Closed Toggle -->
            <div class="form-group">
              <div class="form-check form-switch">
                <input
                  id="exception-closed"
                  v-model="formData.isClosed"
                  type="checkbox"
                  class="form-check-input"
                  role="switch"
                />
                <label for="exception-closed" class="form-check-label">
                  {{ $t('provider.hours.exceptions.markAsClosed') }}
                </label>
              </div>
              <small class="form-text text-muted">
                {{ $t('provider.hours.exceptions.closedHelp') }}
              </small>
            </div>

            <!-- Time Pickers (shown when not closed) -->
            <div v-if="!formData.isClosed" class="time-pickers">
              <div class="form-group">
                <label for="exception-open-time" class="form-label required">
                  {{ $t('provider.hours.exceptions.openTime') }}
                </label>
                <PersianTimePicker
                  v-model="formData.openTime"
                  :placeholder="$t('provider.hours.exceptions.openTime')"
                />
                <div v-if="errors.openTime" class="invalid-feedback d-block">
                  {{ errors.openTime }}
                </div>
              </div>

              <div class="form-group">
                <label for="exception-close-time" class="form-label required">
                  {{ $t('provider.hours.exceptions.closeTime') }}
                </label>
                <PersianTimePicker
                  v-model="formData.closeTime"
                  :placeholder="$t('provider.hours.exceptions.closeTime')"
                />
                <div v-if="errors.closeTime" class="invalid-feedback d-block">
                  {{ errors.closeTime }}
                </div>
              </div>
            </div>

            <!-- Reason -->
            <div class="form-group">
              <label for="exception-reason" class="form-label required">
                {{ $t('provider.hours.exceptions.reason') }}
              </label>
              <input
                id="exception-reason"
                v-model="formData.reason"
                type="text"
                class="form-control"
                :class="{ 'is-invalid': errors.reason }"
                :placeholder="$t('provider.hours.exceptions.reasonPlaceholder')"
                maxlength="200"
                required
              />
              <div v-if="errors.reason" class="invalid-feedback">
                {{ errors.reason }}
              </div>
              <small class="form-text text-muted">
                {{ formData.reason.length }}/200 {{ $t('common.characters') }}
              </small>
            </div>

            <!-- Preview Section -->
            <div v-if="!formData.isClosed && formData.openTime && formData.closeTime" class="preview-section">
              <h6 class="preview-title">
                <i class="bi bi-eye"></i>
                {{ $t('provider.hours.exceptions.preview') }}
              </h6>
              <div class="preview-content">
                <div class="preview-item">
                  <span class="preview-label">{{ $t('provider.hours.exceptions.date') }}:</span>
                  <span class="preview-value">{{ formatPreviewDate(formData.date) }}</span>
                </div>
                <div class="preview-item">
                  <span class="preview-label">{{ $t('provider.hours.exceptions.hours') }}:</span>
                  <span class="preview-value">{{ formatTime(formData.openTime) }} - {{ formatTime(formData.closeTime) }}</span>
                </div>
                <div class="preview-item">
                  <span class="preview-label">{{ $t('provider.hours.exceptions.duration') }}:</span>
                  <span class="preview-value">{{ calculateDuration() }}</span>
                </div>
              </div>
            </div>

            <!-- Conflict Warning -->
            <div v-if="hasConflict" class="alert alert-warning" role="alert">
              <i class="bi bi-exclamation-triangle"></i>
              {{ $t('provider.hours.exceptions.conflictWarning') }}
            </div>

            <!-- Error Message -->
            <div v-if="errorMessage" class="alert alert-danger" role="alert">
              <i class="bi bi-exclamation-triangle"></i>
              {{ errorMessage }}
            </div>
          </div>

          <div class="modal-footer">
            <button type="button" class="btn btn-secondary" @click="handleCancel">
              {{ $t('common.cancel') }}
            </button>
            <button type="submit" class="btn btn-primary" :disabled="isSaving || !isFormValid">
              <span v-if="isSaving" class="spinner-border spinner-border-sm" role="status"></span>
              {{ isEditing ? $t('common.update') : $t('common.add') }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { useHoursStore } from '../../stores/hours.store'
import type { ExceptionSchedule } from '../../types/hours.types'
import { useI18n } from 'vue-i18n'
import PersianTimePicker from '@/shared/components/calendar/PersianTimePicker.vue'

// Props & Emits
interface Props {
  providerId: string
  exception?: ExceptionSchedule | null
}

const props = defineProps<Props>()

const emit = defineEmits<{
  close: []
  saved: []
}>()

// Composables
const hoursStore = useHoursStore()
const { t } = useI18n()

// State
const formData = ref({
  date: '',
  openTime: '',
  closeTime: '',
  reason: '',
  isClosed: false,
})

const errors = ref({
  date: '',
  openTime: '',
  closeTime: '',
  reason: '',
})

const isSaving = ref(false)
const errorMessage = ref('')
const hasConflict = ref(false)

// Computed
const isEditing = computed(() => !!props.exception)

const minDate = computed(() => {
  const today = new Date()
  return today.toISOString().split('T')[0]
})

const isFormValid = computed(() => {
  if (!formData.value.date || !formData.value.reason.trim()) {
    return false
  }

  if (!formData.value.isClosed) {
    return formData.value.openTime && formData.value.closeTime
  }

  return true
})

// Methods
function validateForm(): boolean {
  errors.value = {
    date: '',
    openTime: '',
    closeTime: '',
    reason: '',
  }

  let isValid = true

  // Validate date
  if (!formData.value.date) {
    errors.value.date = t('provider.hours.exceptions.errors.dateRequired')
    isValid = false
  } else {
    const selectedDate = new Date(formData.value.date)
    const today = new Date()
    today.setHours(0, 0, 0, 0)

    if (selectedDate < today && !isEditing.value) {
      errors.value.date = t('provider.hours.exceptions.errors.pastDate')
      isValid = false
    }
  }

  // Validate times if not closed
  if (!formData.value.isClosed) {
    if (!formData.value.openTime) {
      errors.value.openTime = t('provider.hours.exceptions.errors.openTimeRequired')
      isValid = false
    }

    if (!formData.value.closeTime) {
      errors.value.closeTime = t('provider.hours.exceptions.errors.closeTimeRequired')
      isValid = false
    }

    // Validate open time is before close time
    if (formData.value.openTime && formData.value.closeTime) {
      if (formData.value.openTime >= formData.value.closeTime) {
        errors.value.closeTime = t('provider.hours.exceptions.errors.invalidTimeRange')
        isValid = false
      }
    }
  }

  // Validate reason
  if (!formData.value.reason.trim()) {
    errors.value.reason = t('provider.hours.exceptions.errors.reasonRequired')
    isValid = false
  } else if (formData.value.reason.length > 200) {
    errors.value.reason = t('provider.hours.exceptions.errors.reasonTooLong')
    isValid = false
  }

  return isValid
}

async function handleSubmit() {
  if (!validateForm()) return

  isSaving.value = true
  errorMessage.value = ''

  try {
    if (isEditing.value) {
      // For editing, we'd need an update endpoint
      // For now, we'll delete and recreate
      if (props.exception?.id) {
        await hoursStore.removeException(props.providerId, props.exception.id)
      }
    }

    await hoursStore.addException({
      providerId: props.providerId,
      exception: {
        date: formData.value.date,
        openTime: formData.value.isClosed ? undefined : formData.value.openTime,
        closeTime: formData.value.isClosed ? undefined : formData.value.closeTime,
        reason: formData.value.reason.trim(),
        isClosed: formData.value.isClosed,
      },
    })

    emit('saved')
  } catch (error) {
    console.error('Failed to save exception:', error)
    errorMessage.value = error instanceof Error ? error.message : t('provider.hours.exceptions.errors.saveFailed')
  } finally {
    isSaving.value = false
  }
}

function handleCancel() {
  emit('close')
}

function formatPreviewDate(dateString: string): string {
  if (!dateString) return ''
  const date = new Date(dateString)
  return date.toLocaleDateString(undefined, {
    weekday: 'long',
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  })
}

function formatTime(timeString: string): string {
  if (!timeString) return ''
  const [hours, minutes] = timeString.split(':')
  const hour = parseInt(hours)
  const ampm = hour >= 12 ? 'PM' : 'AM'
  const displayHour = hour % 12 || 12
  return `${displayHour}:${minutes} ${ampm}`
}

function calculateDuration(): string {
  if (!formData.value.openTime || !formData.value.closeTime) return ''

  const [openHours, openMinutes] = formData.value.openTime.split(':').map(Number)
  const [closeHours, closeMinutes] = formData.value.closeTime.split(':').map(Number)

  const openTotalMinutes = openHours * 60 + openMinutes
  const closeTotalMinutes = closeHours * 60 + closeMinutes
  const durationMinutes = closeTotalMinutes - openTotalMinutes

  const hours = Math.floor(durationMinutes / 60)
  const minutes = durationMinutes % 60

  if (hours === 0) {
    return `${minutes} ${t('common.minutes')}`
  } else if (minutes === 0) {
    return `${hours} ${t('common.hours')}`
  } else {
    return `${hours} ${t('common.hours')} ${minutes} ${t('common.minutes')}`
  }
}

function checkForConflicts() {
  if (!formData.value.date) {
    hasConflict.value = false
    return
  }

  // Check if there's already a holiday on this date
  const holiday = hoursStore.state.holidays.find(h => h.date === formData.value.date)
  if (holiday) {
    hasConflict.value = true
    return
  }

  // Check if there's already an exception on this date (excluding current if editing)
  const existingException = hoursStore.state.exceptions.find(
    e => e.date === formData.value.date && e.id !== props.exception?.id
  )
  if (existingException) {
    hasConflict.value = true
    return
  }

  hasConflict.value = false
}

// Watchers
watch(() => formData.value.isClosed, (isClosed) => {
  if (isClosed) {
    formData.value.openTime = ''
    formData.value.closeTime = ''
    errors.value.openTime = ''
    errors.value.closeTime = ''
  }
})

watch(() => formData.value.date, () => {
  checkForConflicts()
})

// Lifecycle
onMounted(() => {
  if (props.exception) {
    formData.value = {
      date: props.exception.date,
      openTime: props.exception.openTime || '',
      closeTime: props.exception.closeTime || '',
      reason: props.exception.reason,
      isClosed: props.exception.isClosed,
    }
  } else {
    // Set default date to today
    formData.value.date = new Date().toISOString().split('T')[0]
    // Set default times
    formData.value.openTime = '09:00'
    formData.value.closeTime = '17:00'
  }

  checkForConflicts()
})
</script>

<style scoped>
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
  padding: 1rem;
}

.modal-dialog {
  max-width: 600px;
  width: 100%;
}

.modal-content {
  border-radius: 0.5rem;
  box-shadow: 0 0.5rem 1rem rgba(0, 0, 0, 0.15);
}

.modal-header {
  border-bottom: 1px solid var(--bs-border-color);
}

.modal-title {
  font-size: 1.25rem;
  font-weight: 600;
}

.modal-body {
  padding: 1.5rem;
}

.form-group {
  margin-bottom: 1.5rem;
}

.form-label {
  font-weight: 500;
  margin-bottom: 0.5rem;
}

.form-label.required::after {
  content: ' *';
  color: var(--bs-danger);
}

.form-control:focus,
.form-select:focus {
  border-color: var(--bs-primary);
  box-shadow: 0 0 0 0.2rem rgba(var(--bs-primary-rgb), 0.25);
}

.form-check-label {
  font-weight: 500;
}

.time-pickers {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
}

.preview-section {
  margin-top: 1rem;
  padding: 1rem;
  background-color: var(--bs-light);
  border-radius: 0.5rem;
}

.preview-title {
  font-size: 0.875rem;
  font-weight: 600;
  margin-bottom: 0.75rem;
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.preview-content {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.preview-item {
  display: flex;
  justify-content: space-between;
  font-size: 0.875rem;
}

.preview-label {
  color: var(--bs-secondary);
  font-weight: 500;
}

.preview-value {
  color: var(--bs-body-color);
  font-weight: 600;
}

.alert {
  margin-top: 1rem;
  margin-bottom: 0;
}

.alert i {
  margin-right: 0.5rem;
}

.modal-footer {
  border-top: 1px solid var(--bs-border-color);
  padding: 1rem 1.5rem;
}

/* Responsive */
@media (max-width: 576px) {
  .modal-dialog {
    max-width: 100%;
    margin: 0;
  }

  .modal-content {
    border-radius: 0;
  }

  .time-pickers {
    grid-template-columns: 1fr;
  }
}
</style>
