<template>
  <div class="registration-step">
    <ProgressIndicator :current-step="4" :total-steps="9" />

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

        <!-- Add/Edit Form -->
        <div v-if="isAdding" class="service-form">
          <div class="form-group">
            <label for="serviceName" class="form-label">نام خدمت</label>
            <input
              id="serviceName"
              v-model="formData.name"
              type="text"
              class="form-input"
              placeholder="مثال: اصلاح مو"
            />
          </div>

          <div class="form-row">
            <div class="form-group">
              <label for="price" class="form-label">قیمت (تومان)</label>
              <input
                id="price"
                v-model="formData.price"
                type="number"
                dir="ltr"
                class="form-input"
                placeholder="100000"
              />
            </div>

            <div class="form-group">
              <label for="duration" class="form-label">مدت زمان (دقیقه)</label>
              <input
                id="duration"
                v-model="formData.duration"
                type="number"
                dir="ltr"
                class="form-input"
                placeholder="30"
              />
            </div>
          </div>

          <div class="form-actions">
            <AppButton type="button" variant="primary" size="medium" @click="handleAddService">
              {{ editingId ? 'ویرایش' : 'افزودن' }}
            </AppButton>
            <AppButton type="button" variant="outline" size="medium" @click="handleCancelAdd">
              لغو
            </AppButton>
          </div>
        </div>

        <!-- Add Service Button -->
        <AppButton
          v-else
          type="button"
          variant="outline"
          size="large"
          class="btn-add-service"
          @click="isAdding = true"
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
          <AppButton type="button" variant="outline" size="large" @click="$emit('back')">
            قبلی
          </AppButton>
          <AppButton type="button" variant="primary" size="large" @click="handleNext">
            بعدی
          </AppButton>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import ProgressIndicator from '../shared/ProgressIndicator.vue'
import AppButton from '@/shared/components/ui/Button/AppButton.vue'
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

// State
const services = ref<Service[]>(props.modelValue || [])
const isAdding = ref(false)
const editingId = ref<string | null>(null)
const formData = ref({
  name: '',
  price: '',
  duration: '',
})
const error = ref('')

// Methods
const formatPrice = (price: number) => {
  return new Intl.NumberFormat('fa-IR').format(price)
}

const handleAddService = () => {
  if (!formData.value.name || !formData.value.price || !formData.value.duration) {
    error.value = 'لطفاً تمام فیلدها را پر کنید'
    return
  }

  const totalMinutes = parseInt(formData.value.duration)
  const newService: Service = {
    id: editingId.value || Date.now().toString(),
    name: formData.value.name,
    price: parseFloat(formData.value.price),
    durationHours: Math.floor(totalMinutes / 60),
    durationMinutes: totalMinutes % 60,
    priceType: 'fixed',
  }

  if (editingId.value) {
    // Edit existing service
    services.value = services.value.map((s) =>
      s.id === editingId.value ? newService : s
    )
    editingId.value = null
  } else {
    // Add new service
    services.value.push(newService)
  }

  // Reset form
  formData.value = { name: '', price: '', duration: '' }
  isAdding.value = false
  error.value = ''

  // Emit update
  emit('update:modelValue', services.value)
}

const handleEdit = (service: Service) => {
  const totalMinutes = service.durationHours * 60 + service.durationMinutes
  formData.value = {
    name: service.name,
    price: service.price.toString(),
    duration: totalMinutes.toString(),
  }
  editingId.value = service.id
  isAdding.value = true
}

const handleDelete = (id: string) => {
  services.value = services.value.filter((s) => s.id !== id)
  emit('update:modelValue', services.value)
}

const handleCancelAdd = () => {
  isAdding.value = false
  editingId.value = null
  formData.value = { name: '', price: '', duration: '' }
  error.value = ''
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

/* Service Form */
.service-form {
  padding: 1rem;
  background: rgba(139, 92, 246, 0.05);
  border: 1px solid rgba(139, 92, 246, 0.2);
  border-radius: 0.5rem;
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.form-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
}

.form-label {
  font-size: 0.875rem;
  font-weight: 500;
  color: #374151;
}

.form-input {
  width: 100%;
  padding: 0.75rem 1rem;
  font-size: 1rem;
  border: 1px solid #d1d5db;
  border-radius: 0.5rem;
  background: white;
  transition: all 0.2s ease;
}

.form-input:focus {
  outline: none;
  border-color: #8b5cf6;
  box-shadow: 0 0 0 3px rgba(139, 92, 246, 0.1);
}

.form-actions {
  display: flex;
  gap: 0.5rem;
}

.form-actions > * {
  flex: 1;
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
