<template>
  <StepContainer
    :title="$t('provider.registration.services.title')"
    :subtitle="$t('provider.registration.services.subtitle')"
  >
    <!-- Service List -->
    <div v-if="services.length > 0" class="services-list">
      <p class="list-header">{{ $t('provider.registration.services.yourServices') }} ({{ services.length }})</p>
      <div v-for="service in services" :key="service.id" class="service-item">
        <div class="service-info">
          <h4 class="service-name">{{ service.name }}</h4>
          <p class="service-details">{{ service.durationMinutes }}min</p>
        </div>
        <div class="service-actions">
          <span class="service-price">${{ service.price.toFixed(2) }}</span>
          <button type="button" class="btn-delete" @click="removeService(service.id)">
            <svg viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z" clip-rule="evenodd" />
            </svg>
          </button>
        </div>
      </div>
    </div>

    <!-- Add Service -->
    <button type="button" class="btn-add-service" @click="showAddModal = true">
      + {{ $t('provider.registration.services.addService') }}
    </button>

    <!-- Minimum requirement warning -->
    <p v-if="services.length === 0" class="warning-text">
      {{ $t('provider.registration.services.minRequired') }}
    </p>

    <NavigationButtons
      :show-back="true"
      :can-continue="services.length > 0"
      @back="$emit('back')"
      @next="handleNext"
    />

    <!-- Simple Add Service Modal -->
    <div v-if="showAddModal" class="modal-overlay" @click="showAddModal = false">
      <div class="modal-content" @click.stop>
        <h3 class="modal-title">{{ $t('provider.registration.services.addService') }}</h3>
        <form @submit.prevent="handleAddService">
          <input v-model="newService.name" type="text" class="form-input" placeholder="Service Name" required />
          <input v-model.number="newService.durationMinutes" type="number" class="form-input" placeholder="Duration (minutes)" required />
          <input v-model.number="newService.price" type="number" step="0.01" class="form-input" placeholder="Price" required />
          <div class="modal-actions">
            <button type="button" class="btn-cancel" @click="showAddModal = false">Cancel</button>
            <button type="submit" class="btn-submit">Add</button>
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
const newService = ref({ name: '', durationMinutes: 30, price: 0 })

const handleAddService = () => {
  services.value.push({
    id: Date.now().toString(),
    name: newService.value.name,
    durationHours: 0,
    durationMinutes: newService.value.durationMinutes,
    price: newService.value.price,
    priceType: 'fixed',
  })
  newService.value = { name: '', durationMinutes: 30, price: 0 }
  showAddModal.value = false
}

const removeService = (id: string) => {
  services.value = services.value.filter(s => s.id !== id)
}

const handleNext = () => {
  emit('update:modelValue', services.value)
  emit('next')
}
</script>

<style scoped>
.services-list {
  margin-bottom: 1.5rem;
}

.list-header {
  font-size: 0.875rem;
  color: #6b7280;
  margin-bottom: 1rem;
}

.service-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1rem;
  background: #ffffff;
  border: 1px solid #e5e7eb;
  border-radius: 0.5rem;
  margin-bottom: 0.75rem;
}

.service-info {
  flex: 1;
}

.service-name {
  font-weight: 600;
  color: #111827;
  margin-bottom: 0.25rem;
}

.service-details {
  font-size: 0.875rem;
  color: #6b7280;
}

.service-actions {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.service-price {
  font-weight: 600;
  color: #111827;
}

.btn-delete {
  width: 2rem;
  height: 2rem;
  background: none;
  border: none;
  color: #ef4444;
  cursor: pointer;
  padding: 0.25rem;
}

.btn-delete:hover {
  color: #dc2626;
}

.btn-add-service {
  width: 100%;
  padding: 1rem;
  background: #ffffff;
  border: 2px dashed #d1d5db;
  border-radius: 0.5rem;
  color: #6b7280;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
  margin-bottom: 1rem;
}

.btn-add-service:hover {
  border-color: #10b981;
  color: #10b981;
}

.warning-text {
  color: #ef4444;
  font-size: 0.875rem;
  text-align: center;
  margin: 1rem 0;
}

.modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.modal-content {
  background: white;
  padding: 2rem;
  border-radius: 0.75rem;
  width: 90%;
  max-width: 400px;
}

.modal-title {
  font-size: 1.25rem;
  font-weight: 700;
  margin-bottom: 1.5rem;
}

.form-input {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid #d1d5db;
  border-radius: 0.5rem;
  margin-bottom: 1rem;
}

.modal-actions {
  display: flex;
  gap: 1rem;
  margin-top: 1.5rem;
}

.btn-cancel, .btn-submit {
  flex: 1;
  padding: 0.75rem;
  border-radius: 0.5rem;
  font-weight: 600;
  cursor: pointer;
}

.btn-cancel {
  background: transparent;
  border: 1px solid #d1d5db;
  color: #6b7280;
}

.btn-submit {
  background: #111827;
  border: none;
  color: white;
}
</style>
