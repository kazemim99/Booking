<template>
  <div class="modal-overlay" @click.self="handleCancel">
    <div class="modal-dialog modal-dialog-centered">
      <div class="modal-content">
        <div class="modal-header">
          <h5 class="modal-title">
            {{ isEditing ? $t('provider.hours.holidays.editHoliday') : $t('provider.hours.holidays.addHoliday') }}
          </h5>
          <button type="button" class="btn-close" @click="handleCancel"></button>
        </div>

        <form @submit.prevent="handleSubmit">
          <div class="modal-body">
            <!-- Date Picker -->
            <div class="form-group">
              <label for="holiday-date" class="form-label required">
                {{ $t('provider.hours.holidays.date') }}
              </label>
              <input
                id="holiday-date"
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

            <!-- Reason -->
            <div class="form-group">
              <label for="holiday-reason" class="form-label required">
                {{ $t('provider.hours.holidays.reason') }}
              </label>
              <input
                id="holiday-reason"
                v-model="formData.reason"
                type="text"
                class="form-control"
                :class="{ 'is-invalid': errors.reason }"
                :placeholder="$t('provider.hours.holidays.reasonPlaceholder')"
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

            <!-- Recurring Toggle -->
            <div class="form-group">
              <div class="form-check form-switch">
                <input
                  id="holiday-recurring"
                  v-model="formData.isRecurring"
                  type="checkbox"
                  class="form-check-input"
                  role="switch"
                />
                <label for="holiday-recurring" class="form-check-label">
                  {{ $t('provider.hours.holidays.makeRecurring') }}
                </label>
              </div>
              <small class="form-text text-muted">
                {{ $t('provider.hours.holidays.recurringHelp') }}
              </small>
            </div>

            <!-- Recurrence Pattern -->
            <div v-if="formData.isRecurring" class="form-group">
              <label for="recurrence-pattern" class="form-label required">
                {{ $t('provider.hours.holidays.recurrencePattern') }}
              </label>
              <select
                id="recurrence-pattern"
                v-model="formData.recurrencePattern"
                class="form-select"
                :class="{ 'is-invalid': errors.recurrencePattern }"
                required
              >
                <option :value="RecurrencePattern.None" disabled>
                  {{ $t('provider.hours.holidays.selectPattern') }}
                </option>
                <option :value="RecurrencePattern.Weekly">
                  {{ $t('provider.hours.holidays.patterns.weekly') }}
                </option>
                <option :value="RecurrencePattern.Monthly">
                  {{ $t('provider.hours.holidays.patterns.monthly') }}
                </option>
                <option :value="RecurrencePattern.Yearly">
                  {{ $t('provider.hours.holidays.patterns.yearly') }}
                </option>
              </select>
              <div v-if="errors.recurrencePattern" class="invalid-feedback">
                {{ errors.recurrencePattern }}
              </div>
            </div>

            <!-- Preview Recurring Dates -->
            <div v-if="formData.isRecurring && formData.recurrencePattern !== RecurrencePattern.None" class="preview-section">
              <h6 class="preview-title">
                <i class="bi bi-calendar2-event"></i>
                {{ $t('provider.hours.holidays.previewDates') }}
              </h6>
              <div class="preview-dates">
                <div v-for="(date, index) in previewDates" :key="index" class="preview-date-item">
                  {{ formatPreviewDate(date) }}
                </div>
              </div>
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
import type { HolidaySchedule } from '../../types/hours.types'
import { RecurrencePattern } from '../../types/hours.types'
import { useI18n } from 'vue-i18n'
import { formatDate } from '@/core/utils'

// Props & Emits
interface Props {
  providerId: string
  holiday?: HolidaySchedule | null
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
  reason: '',
  isRecurring: false,
  recurrencePattern: RecurrencePattern.None,
})

const errors = ref({
  date: '',
  reason: '',
  recurrencePattern: '',
})

const isSaving = ref(false)
const errorMessage = ref('')

// Computed
const isEditing = computed(() => !!props.holiday)

const minDate = computed(() => {
  const today = new Date()
  return today.toISOString().split('T')[0]
})

const isFormValid = computed(() => {
  return (
    formData.value.date &&
    formData.value.reason.trim() &&
    (!formData.value.isRecurring || formData.value.recurrencePattern !== RecurrencePattern.None)
  )
})

const previewDates = computed(() => {
  if (!formData.value.date || !formData.value.isRecurring || formData.value.recurrencePattern === RecurrencePattern.None) {
    return []
  }

  const baseDate = new Date(formData.value.date)
  const dates: Date[] = []
  const maxPreview = 5

  switch (formData.value.recurrencePattern) {
    case RecurrencePattern.Weekly:
      for (let i = 0; i < maxPreview; i++) {
        const date = new Date(baseDate)
        date.setDate(baseDate.getDate() + (i * 7))
        dates.push(date)
      }
      break

    case RecurrencePattern.Monthly:
      for (let i = 0; i < maxPreview; i++) {
        const date = new Date(baseDate)
        date.setMonth(baseDate.getMonth() + i)
        dates.push(date)
      }
      break

    case RecurrencePattern.Yearly:
      for (let i = 0; i < maxPreview; i++) {
        const date = new Date(baseDate)
        date.setFullYear(baseDate.getFullYear() + i)
        dates.push(date)
      }
      break
  }

  return dates
})

// Methods
function validateForm(): boolean {
  errors.value = {
    date: '',
    reason: '',
    recurrencePattern: '',
  }

  let isValid = true

  // Validate date
  if (!formData.value.date) {
    errors.value.date = t('provider.hours.holidays.errors.dateRequired')
    isValid = false
  } else {
    const selectedDate = new Date(formData.value.date)
    const today = new Date()
    today.setHours(0, 0, 0, 0)

    if (selectedDate < today && !isEditing.value) {
      errors.value.date = t('provider.hours.holidays.errors.pastDate')
      isValid = false
    }
  }

  // Validate reason
  if (!formData.value.reason.trim()) {
    errors.value.reason = t('provider.hours.holidays.errors.reasonRequired')
    isValid = false
  } else if (formData.value.reason.length > 200) {
    errors.value.reason = t('provider.hours.holidays.errors.reasonTooLong')
    isValid = false
  }

  // Validate recurrence pattern
  if (formData.value.isRecurring && formData.value.recurrencePattern === RecurrencePattern.None) {
    errors.value.recurrencePattern = t('provider.hours.holidays.errors.patternRequired')
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
      if (props.holiday?.id) {
        await hoursStore.removeHoliday(props.providerId, props.holiday.id)
      }
    }

    await hoursStore.addHoliday({
      providerId: props.providerId,
      holiday: {
        date: formData.value.date,
        reason: formData.value.reason.trim(),
        isRecurring: formData.value.isRecurring,
        recurrencePattern: formData.value.isRecurring ? formData.value.recurrencePattern : undefined,
      },
    })

    emit('saved')
  } catch (error) {
    console.error('Failed to save holiday:', error)
    errorMessage.value = error instanceof Error ? error.message : t('provider.hours.holidays.errors.saveFailed')
  } finally {
    isSaving.value = false
  }
}

function handleCancel() {
  emit('close')
}

function formatPreviewDate(date: Date): string {
  return date.toLocaleDateString(undefined, {
    weekday: 'short',
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  })
}

// Watchers
watch(() => formData.value.isRecurring, (isRecurring) => {
  if (!isRecurring) {
    formData.value.recurrencePattern = RecurrencePattern.None
  } else if (formData.value.recurrencePattern === RecurrencePattern.None) {
    formData.value.recurrencePattern = RecurrencePattern.Yearly
  }
})

// Lifecycle
onMounted(() => {
  if (props.holiday) {
    formData.value = {
      date: props.holiday.date,
      reason: props.holiday.reason,
      isRecurring: props.holiday.isRecurring,
      recurrencePattern: props.holiday.recurrencePattern || RecurrencePattern.None,
    }
  } else {
    // Set default date to today
    formData.value.date = new Date().toISOString().split('T')[0]
  }
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

.preview-dates {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.preview-date-item {
  padding: 0.5rem;
  background-color: white;
  border-radius: 0.25rem;
  font-size: 0.875rem;
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
}
</style>
