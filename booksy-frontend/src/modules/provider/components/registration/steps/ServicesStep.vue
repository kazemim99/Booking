<template>
  <div class="registration-step">
    <div class="step-card">
      <div class="step-header">
        <h2 class="step-title">خدمات</h2>
        <p class="step-description">خدماتی که ارائه می‌دهید را اضافه کنید</p>
      </div>

      <div class="step-content">
        <!-- Service List -->
        <div v-if="services.length > 0" class="service-list">
          <div v-for="service in services" :key="service.id" class="service-item">
            <div class="service-info">
              <h4 class="service-name">{{ service.name }}</h4>
              <p class="service-details">
                قیمت: {{ formatPrice(service.price) }} تومان • مدت: {{ service.durationHours * 60 + service.durationMinutes }} دقیقه
              </p>
            </div>
            <div class="service-actions">
              <button
                type="button"
                class="btn-icon"
                @click="handleEdit(service)"
                title="ویرایش"
              >
                <svg class="icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    stroke-width="2"
                    d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"
                  />
                </svg>
              </button>
              <button
                type="button"
                class="btn-icon btn-delete"
                @click="handleDelete(service.id)"
                title="حذف"
              >
                <svg class="icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    stroke-width="2"
                    d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
                  />
                </svg>
              </button>
            </div>
          </div>
        </div>

        <!-- Add Service Button -->
        <AppButton
          type="button"
          variant="ghost"
          size="large"
          class="btn-add-service"
          @click="handleCreateService"
        >
          <svg class="icon-plus" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M12 4v16m8-8H4"
            />
          </svg>
          افزودن خدمت جدید
        </AppButton>

        <!-- Error Message -->
        <p v-if="error" class="error-message">{{ error }}</p>

        <!-- Navigation -->
        <div class="step-actions">
          <AppButton type="button" variant="ghost" size="large" @click="$emit('back')">
            قبلی
          </AppButton>
          <AppButton type="button" variant="primary" size="large" @click="handleNext">
            بعدی
          </AppButton>
        </div>
      </div>
    </div>

    <!-- Service Form Modal -->
    <ServiceFormModal
      v-model="isModalOpen"
      :service="editingService"
      @submit="handleModalSubmit"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'

import AppButton from '@/shared/components/ui/Button/AppButton.vue'
import ServiceFormModal from '../ServiceFormModal.vue'
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

// ==================== State ====================

const services = ref<Service[]>(props.modelValue || [])
const isModalOpen = ref(false)
const editingService = ref<Service | null>(null)
const error = ref('')

// ==================== Watchers ====================

// Watch for changes to props.modelValue to sync with parent
watch(
  () => props.modelValue,
  (newValue) => {
    if (newValue && newValue.length > 0) {
      services.value = newValue
      console.log('✅ ServicesStep: Synced services from props:', newValue.length)
    }
  },
  { immediate: true }
)

// ==================== Methods ====================

const formatPrice = (price: number) => {
  return new Intl.NumberFormat('fa-IR').format(price)
}

const handleCreateService = () => {
  editingService.value = null
  isModalOpen.value = true
}

const handleEdit = (service: Service) => {
  editingService.value = service
  isModalOpen.value = true
}

const handleDelete = (id: string) => {
  services.value = services.value.filter((s) => s.id !== id)
  emit('update:modelValue', services.value)
}

const handleModalSubmit = (service: Service) => {
  if (editingService.value) {
    // Edit existing service
    services.value = services.value.map((s) =>
      s.id === editingService.value!.id ? service : s
    )
  } else {
    // Add new service
    services.value.push(service)
  }

  // Emit update
  emit('update:modelValue', services.value)
}

const handleNext = () => {
  if (services.value.length === 0) {
    error.value = 'لطفاً حداقل یک خدمت اضافه کنید'
    return
  }
  error.value = ''
  emit('next')
}
</script>

<style scoped>
.registration-step {
  min-height: 100vh;
  padding: 2rem 1rem;
  background: #f9fafb;
  direction: rtl;
}

.step-card {
  max-width: 42rem;
  margin: 0 auto;
  background: white;
  border-radius: 1rem;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  padding: 2rem;
}

.step-header {
  margin-bottom: 2rem;
}

.step-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: #111827;
  margin-bottom: 0.5rem;
}

.step-description {
  font-size: 0.875rem;
  color: #6b7280;
}

.step-content {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

/* Service List */
.service-list {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.service-item {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 1rem;
  background: rgba(139, 92, 246, 0.05);
  border: 1px solid #e5e7eb;
  border-radius: 0.5rem;
}

.service-info {
  flex: 1;
}

.service-name {
  font-size: 1rem;
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
  gap: 0.5rem;
}

.btn-icon {
  padding: 0.5rem;
  background: none;
  border: none;
  border-radius: 0.375rem;
  cursor: pointer;
  transition: all 0.2s ease;
  display: flex;
  align-items: center;
  justify-content: center;
}

.btn-icon:hover {
  background: rgba(0, 0, 0, 0.05);
}

.btn-icon .icon {
  width: 1.25rem;
  height: 1.25rem;
  color: #6b7280;
}

.btn-delete .icon {
  color: #ef4444;
}

/* Add Service Button */
.btn-add-service {
  width: 100%;
  border-style: dashed !important;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
}

.icon-plus {
  width: 1.25rem;
  height: 1.25rem;
}

/* Error Message */
.error-message {
  font-size: 0.875rem;
  color: #ef4444;
  text-align: center;
}

/* Navigation */
.step-actions {
  display: flex;
  gap: 0.75rem;
  margin-top: 1rem;
  padding-top: 1.5rem;
  border-top: 1px solid #e5e7eb;
}

.step-actions > * {
  flex: 1;
}

@media (max-width: 640px) {
  .step-card {
    padding: 1.5rem;
  }

  .step-title {
    font-size: 1.25rem;
  }

  .form-row {
    grid-template-columns: 1fr;
  }
}
</style>
