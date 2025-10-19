<template>
  <StepContainer
    :title="$t('provider.registration.services.title')"
    :subtitle="$t('provider.registration.services.subtitle')"
  >
    <!-- Service Cards Grid -->
    <div v-if="services.length > 0" class="services-grid">
      <div v-for="service in services" :key="service.id" class="service-card">
        <div class="service-card-header">
          <div class="service-icon">
            <svg viewBox="0 0 20 20" fill="currentColor">
              <path
                fill-rule="evenodd"
                d="M6 2a2 2 0 00-2 2v12a2 2 0 002 2h8a2 2 0 002-2V7.414A2 2 0 0015.414 6L12 2.586A2 2 0 0010.586 2H6zm5 6a1 1 0 10-2 0v3.586l-1.293-1.293a1 1 0 10-1.414 1.414l3 3a1 1 0 001.414 0l3-3a1 1 0 00-1.414-1.414L11 11.586V8z"
                clip-rule="evenodd"
              />
            </svg>
          </div>
          <button type="button" class="btn-card-delete" @click="removeService(service.id)" title="Remove service">
            <svg viewBox="0 0 20 20" fill="currentColor">
              <path
                fill-rule="evenodd"
                d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z"
                clip-rule="evenodd"
              />
            </svg>
          </button>
        </div>
        <div class="service-card-body">
          <h4 class="service-card-name">{{ service.name }}</h4>
          <div class="service-card-details">
            <div class="detail-item">
              <svg class="detail-icon" viewBox="0 0 20 20" fill="currentColor">
                <path
                  fill-rule="evenodd"
                  d="M10 18a8 8 0 100-16 8 8 0 000 16zm1-12a1 1 0 10-2 0v4a1 1 0 00.293.707l2.828 2.829a1 1 0 101.415-1.415L11 9.586V6z"
                  clip-rule="evenodd"
                />
              </svg>
              <span>{{ formatDuration(service.durationHours, service.durationMinutes) }}</span>
            </div>
            <div class="detail-item">
              <svg class="detail-icon" viewBox="0 0 20 20" fill="currentColor">
                <path
                  d="M8.433 7.418c.155-.103.346-.196.567-.267v1.698a2.305 2.305 0 01-.567-.267C8.07 8.34 8 8.114 8 8c0-.114.07-.34.433-.582zM11 12.849v-1.698c.22.071.412.164.567.267.364.243.433.468.433.582 0 .114-.07.34-.433.582a2.305 2.305 0 01-.567.267z"
                />
                <path
                  fill-rule="evenodd"
                  d="M10 18a8 8 0 100-16 8 8 0 000 16zm1-13a1 1 0 10-2 0v.092a4.535 4.535 0 00-1.676.662C6.602 6.234 6 7.009 6 8c0 .99.602 1.765 1.324 2.246.48.32 1.054.545 1.676.662v1.941c-.391-.127-.68-.317-.843-.504a1 1 0 10-1.51 1.31c.562.649 1.413 1.076 2.353 1.253V15a1 1 0 102 0v-.092a4.535 4.535 0 001.676-.662C13.398 13.766 14 12.991 14 12c0-.99-.602-1.765-1.324-2.246A4.535 4.535 0 0011 9.092V7.151c.391.127.68.317.843.504a1 1 0 101.511-1.31c-.563-.649-1.413-1.076-2.354-1.253V5z"
                  clip-rule="evenodd"
                />
              </svg>
              <span class="price">{{ formatPrice(service.price) }}</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Add Service Card -->
      <div class="service-card service-card-add" @click="showAddModal = true">
        <div class="add-card-content">
          <div class="add-icon">
            <svg viewBox="0 0 20 20" fill="currentColor">
              <path
                fill-rule="evenodd"
                d="M10 5a1 1 0 011 1v3h3a1 1 0 110 2h-3v3a1 1 0 11-2 0v-3H6a1 1 0 110-2h3V6a1 1 0 011-1z"
                clip-rule="evenodd"
              />
            </svg>
          </div>
          <p class="add-text">{{ $t('provider.registration.services.addService') }}</p>
        </div>
      </div>
    </div>

    <!-- Empty State -->
    <div v-else class="empty-state">
      <div class="empty-icon">
        <svg viewBox="0 0 20 20" fill="currentColor">
          <path
            fill-rule="evenodd"
            d="M6 2a2 2 0 00-2 2v12a2 2 0 002 2h8a2 2 0 002-2V7.414A2 2 0 0015.414 6L12 2.586A2 2 0 0010.586 2H6zm1 8a1 1 0 100 2h6a1 1 0 100-2H7z"
            clip-rule="evenodd"
          />
        </svg>
      </div>
      <h3 class="empty-title">{{ $t('provider.registration.services.noServices') }}</h3>
      <p class="empty-subtitle">{{ $t('provider.registration.services.noServicesDesc') }}</p>
      <button type="button" class="btn-add-first" @click="showAddModal = true">
        + {{ $t('provider.registration.services.addFirstService') }}
      </button>
    </div>

    <!-- Validation Error -->
    <p v-if="services.length === 0 && showValidationError" class="error-message">
      {{ $t('provider.registration.services.minRequired') }}
    </p>

    <NavigationButtons
      :show-back="true"
      :can-continue="services.length > 0"
      @back="$emit('back')"
      @next="handleNext"
    />

    <!-- Enhanced Add Service Modal -->
    <div v-if="showAddModal" class="modal-overlay" @click="showAddModal = false">
      <div class="modal-content" @click.stop>
        <div class="modal-header">
          <h3 class="modal-title">{{ $t('provider.registration.services.addService') }}</h3>
          <button class="modal-close" @click="showAddModal = false">
            <svg viewBox="0 0 20 20" fill="currentColor">
              <path
                fill-rule="evenodd"
                d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z"
                clip-rule="evenodd"
              />
            </svg>
          </button>
        </div>
        <form @submit.prevent="handleAddService">
          <div class="form-group">
            <label class="form-label">Service Name *</label>
            <input
              v-model="newService.name"
              type="text"
              class="form-input"
              placeholder="e.g., Haircut, Massage, Consultation"
              required
            />
          </div>

          <div class="form-row">
            <div class="form-group">
              <label class="form-label">Duration (minutes) *</label>
              <input
                v-model.number="newService.durationMinutes"
                type="number"
                min="1"
                max="1440"
                class="form-input"
                placeholder="30"
                required
              />
            </div>
            <div class="form-group">
              <label class="form-label">Price (IRR) *</label>
              <input
                v-model.number="newService.price"
                type="number"
                min="0"
                step="1000"
                class="form-input"
                placeholder="50000"
                required
              />
            </div>
          </div>

          <div class="modal-actions">
            <button type="button" class="btn-modal-cancel" @click="showAddModal = false">
              Cancel
            </button>
            <button type="submit" class="btn-modal-submit">Add Service</button>
          </div>
        </form>
      </div>
    </div>
  </StepContainer>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import StepContainer from '../shared/StepContainer.vue'
import NavigationButtons from '../shared/NavigationButtons.vue'
import type { Service } from '@/modules/provider/types/registration.types'

interface Props {
  modelValue?: Service[]
}

interface Emits {
  (e: 'update:modelValue', value: Service[]): void
  (e: 'next'): void
  (e: 'back'): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

const services = ref<Service[]>(props.modelValue || [])
const showAddModal = ref(false)
const showValidationError = ref(false)
const newService = ref({
  name: '',
  durationMinutes: 30,
  price: 50000,
})

const formatDuration = (hours: number, minutes: number): string => {
  const totalMinutes = hours * 60 + minutes
  if (totalMinutes < 60) {
    return `${totalMinutes}min`
  }
  const h = Math.floor(totalMinutes / 60)
  const m = totalMinutes % 60
  return m > 0 ? `${h}h ${m}min` : `${h}h`
}

const formatPrice = (price: number): string => {
  return new Intl.NumberFormat('fa-IR', {
    style: 'currency',
    currency: 'IRR',
    minimumFractionDigits: 0,
  }).format(price)
}

const handleAddService = () => {
  if (!newService.value.name.trim()) {
    return
  }

  services.value.push({
    id: Date.now().toString(),
    name: newService.value.name.trim(),
    durationHours: Math.floor(newService.value.durationMinutes / 60),
    durationMinutes: newService.value.durationMinutes % 60,
    price: newService.value.price,
    priceType: 'fixed',
  })

  // Reset form
  newService.value = {
    name: '',
    durationMinutes: 30,
    price: 50000,
  }
  showAddModal.value = false
  showValidationError.value = false
}

const removeService = (id: string) => {
  services.value = services.value.filter((s) => s.id !== id)
}

const handleNext = () => {
  if (services.value.length === 0) {
    showValidationError.value = true
    return
  }

  emit('update:modelValue', services.value)
  emit('next')
}
</script>

<style scoped>
/* Services Grid */
.services-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 1.25rem;
  margin-bottom: 2rem;
}

.service-card {
  background: white;
  border: 2px solid #e5e7eb;
  border-radius: 0.75rem;
  padding: 1.25rem;
  transition: all 0.2s ease;
  cursor: default;
}

.service-card:hover {
  border-color: #10b981;
  box-shadow: 0 4px 12px rgba(16, 185, 129, 0.1);
}

.service-card-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 1rem;
}

.service-icon {
  width: 2.5rem;
  height: 2.5rem;
  background: #f0fdf4;
  border-radius: 0.5rem;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #10b981;
}

.service-icon svg {
  width: 1.5rem;
  height: 1.5rem;
}

.btn-card-delete {
  width: 2rem;
  height: 2rem;
  background: none;
  border: none;
  color: #9ca3af;
  cursor: pointer;
  transition: color 0.2s;
  padding: 0.25rem;
  border-radius: 0.375rem;
}

.btn-card-delete:hover {
  color: #ef4444;
  background: #fef2f2;
}

.service-card-body {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.service-card-name {
  font-size: 1.125rem;
  font-weight: 600;
  color: #111827;
  line-height: 1.4;
}

.service-card-details {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.detail-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.875rem;
  color: #6b7280;
}

.detail-icon {
  width: 1.125rem;
  height: 1.125rem;
  flex-shrink: 0;
}

.price {
  font-weight: 600;
  color: #10b981;
}

/* Add Service Card */
.service-card-add {
  border-style: dashed;
  border-width: 2px;
  border-color: #d1d5db;
  background: #f9fafb;
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 180px;
  cursor: pointer;
  transition: all 0.2s ease;
}

.service-card-add:hover {
  border-color: #10b981;
  background: #f0fdf4;
}

.add-card-content {
  text-align: center;
  color: #6b7280;
}

.add-icon {
  width: 3rem;
  height: 3rem;
  background: white;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  margin: 0 auto 0.75rem;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
}

.add-icon svg {
  width: 1.5rem;
  height: 1.5rem;
}

.service-card-add:hover .add-icon {
  background: #10b981;
  color: white;
}

.add-text {
  font-weight: 600;
  font-size: 0.875rem;
}

/* Empty State */
.empty-state {
  text-align: center;
  padding: 3rem 2rem;
  background: #f9fafb;
  border-radius: 0.75rem;
  margin-bottom: 2rem;
}

.empty-icon {
  width: 4rem;
  height: 4rem;
  background: white;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  margin: 0 auto 1.5rem;
  color: #9ca3af;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
}

.empty-icon svg {
  width: 2rem;
  height: 2rem;
}

.empty-title {
  font-size: 1.25rem;
  font-weight: 700;
  color: #111827;
  margin-bottom: 0.5rem;
}

.empty-subtitle {
  font-size: 0.875rem;
  color: #6b7280;
  margin-bottom: 1.5rem;
}

.btn-add-first {
  padding: 0.75rem 1.5rem;
  background: #10b981;
  color: white;
  border: none;
  border-radius: 0.5rem;
  font-weight: 600;
  cursor: pointer;
  transition: background 0.2s;
}

.btn-add-first:hover {
  background: #059669;
}

/* Error Message */
.error-message {
  color: #ef4444;
  font-size: 0.875rem;
  text-align: center;
  margin: -1rem 0 1.5rem;
  font-weight: 500;
}

/* Modal */
.modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.5);
  backdrop-filter: blur(4px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  animation: fadeIn 0.2s ease;
}

@keyframes fadeIn {
  from {
    opacity: 0;
  }
  to {
    opacity: 1;
  }
}

.modal-content {
  background: white;
  padding: 0;
  border-radius: 1rem;
  width: 90%;
  max-width: 480px;
  box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1);
  animation: slideUp 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}

@keyframes slideUp {
  from {
    opacity: 0;
    transform: translateY(1rem);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1.5rem;
  border-bottom: 1px solid #e5e7eb;
}

.modal-title {
  font-size: 1.25rem;
  font-weight: 700;
  color: #111827;
  margin: 0;
}

.modal-close {
  width: 2rem;
  height: 2rem;
  background: none;
  border: none;
  color: #9ca3af;
  cursor: pointer;
  padding: 0.25rem;
  border-radius: 0.375rem;
  transition: all 0.2s;
}

.modal-close:hover {
  background: #f3f4f6;
  color: #111827;
}

form {
  padding: 1.5rem;
}

.form-group {
  margin-bottom: 1.25rem;
}

.form-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
}

.form-label {
  display: block;
  font-size: 0.875rem;
  font-weight: 600;
  color: #374151;
  margin-bottom: 0.5rem;
}

.form-input {
  width: 100%;
  padding: 0.75rem 1rem;
  font-size: 0.9375rem;
  border: 1px solid #d1d5db;
  border-radius: 0.5rem;
  outline: none;
  transition: all 0.2s ease;
}

.form-input:focus {
  border-color: #10b981;
  box-shadow: 0 0 0 3px rgba(16, 185, 129, 0.1);
}

.form-input::placeholder {
  color: #9ca3af;
}

.modal-actions {
  display: flex;
  gap: 0.75rem;
  margin-top: 1.5rem;
  padding-top: 1.5rem;
  border-top: 1px solid #e5e7eb;
}

.btn-modal-cancel,
.btn-modal-submit {
  flex: 1;
  padding: 0.75rem 1.5rem;
  border-radius: 0.5rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
  font-size: 0.9375rem;
}

.btn-modal-cancel {
  background: transparent;
  border: 1px solid #d1d5db;
  color: #6b7280;
}

.btn-modal-cancel:hover {
  background: #f9fafb;
  border-color: #9ca3af;
}

.btn-modal-submit {
  background: #10b981;
  border: none;
  color: white;
}

.btn-modal-submit:hover {
  background: #059669;
}

@media (max-width: 640px) {
  .services-grid {
    grid-template-columns: 1fr;
  }

  .form-row {
    grid-template-columns: 1fr;
  }
}
</style>
