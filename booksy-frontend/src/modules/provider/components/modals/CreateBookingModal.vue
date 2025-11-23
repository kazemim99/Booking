<template>
  <Modal v-model="isOpen" title="رزرو جدید" size="large">
    <form @submit.prevent="handleSubmit" class="booking-form">
      <!-- Customer Selection -->
      <div class="form-group">
        <label class="form-label required">مشتری</label>
        <div class="customer-search">
          <input
            v-model="searchCustomer"
            type="text"
            class="form-input"
            placeholder="جستجوی مشتری (نام یا شماره تلفن)"
            @input="filterCustomers"
            @focus="showCustomerDropdown = true"
          />
          <div v-if="showCustomerDropdown && filteredCustomers.length > 0" class="customers-dropdown">
            <div
              v-for="customer in filteredCustomers"
              :key="customer.id"
              class="customer-option"
              @click="selectCustomer(customer)"
            >
              <div class="customer-name">{{ customer.name }}</div>
              <div class="customer-phone">{{ customer.phone }}</div>
            </div>
          </div>
        </div>
        <div v-if="formData.customerId" class="selected-customer">
          <span class="selected-label">مشتری انتخاب شده:</span>
          <span class="selected-value">{{ selectedCustomerName }}</span>
        </div>
      </div>

      <!-- Service Selection -->
      <div class="form-group">
        <label class="form-label required">خدمت</label>
        <select v-model="formData.serviceId" class="form-select" required @change="handleServiceChange">
          <option value="">انتخاب کنید</option>
          <option v-for="service in services" :key="service.id" :value="service.id">
            {{ service.name }} - {{ convertToPersian(service.duration) }} دقیقه - {{ convertToPersian(service.price) }} تومان
          </option>
        </select>
      </div>

      <!-- Staff Selection (only for organizations with staff) -->
      <div v-if="shouldShowStaffSelector" class="form-group">
        <label class="form-label" :class="{ required: requiresStaffSelection }">
          انتخاب کارمند
        </label>
        <StaffSelector
          v-if="providerId"
          :organization-id="providerId"
          v-model="formData.staffId"
          @select="handleStaffSelect"
        />
      </div>

      <!-- Date & Time -->
      <div class="form-group">
        <label class="form-label required">تاریخ و زمان</label>
        <VuePersianDatetimePicker
          v-model="formData.dateTime"
          placeholder="تاریخ و زمان را انتخاب کنید"
          format="YYYY-MM-DD HH:mm"
          display-format="jYYYY/jMM/jDD - HH:mm"
          type="datetime"
          :min="new Date().toISOString()"
          auto-submit
          color="#1976d2"
          input-class="persian-datepicker-input"
        />
      </div>

      <!-- Notes -->
      <div class="form-group">
        <label class="form-label">یادداشت</label>
        <textarea
          v-model="formData.notes"
          class="form-textarea"
          rows="3"
          placeholder="یادداشت‌های اضافی (اختیاری)"
        ></textarea>
      </div>

      <!-- Summary -->
      <div v-if="selectedService" class="booking-summary">
        <h4 class="summary-title">خلاصه رزرو</h4>
        <div class="summary-item">
          <span class="summary-label">مدت زمان:</span>
          <span class="summary-value">{{ convertToPersian(selectedService.duration) }} دقیقه</span>
        </div>
        <div class="summary-item">
          <span class="summary-label">هزینه:</span>
          <span class="summary-value">{{ convertToPersian(selectedService.price) }} تومان</span>
        </div>
        <div v-if="selectedStaffMember" class="summary-item">
          <span class="summary-label">کارمند:</span>
          <span class="summary-value">{{ selectedStaffMember.fullName }}</span>
        </div>
      </div>
    </form>

    <template #footer>
      <button type="button" class="btn-secondary" @click="handleCancel">انصراف</button>
      <button
        type="button"
        class="btn-primary"
        @click="handleSubmit"
        :disabled="!isFormValid"
      >
        ثبت رزرو
      </button>
    </template>
  </Modal>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import Modal from '@/shared/components/Modal.vue'
import VuePersianDatetimePicker from 'vue3-persian-datetime-picker'
import StaffSelector from '@/modules/booking/components/StaffSelector.vue'
import { convertEnglishToPersianNumbers } from '@/shared/utils/date/jalali.utils'
import { useHierarchyStore } from '@/modules/provider/stores/hierarchy.store'
import { ProviderHierarchyType } from '@/modules/provider/types/hierarchy.types'
import type { StaffMember } from '@/modules/provider/types/hierarchy.types'

interface Customer {
  id: string
  name: string
  phone: string
}

interface Service {
  id: string
  name: string
  duration: number
  price: number
}

interface BookingFormData {
  customerId: string
  serviceId: string
  staffId: string | null
  dateTime: string
  notes: string
}

interface Props {
  modelValue: boolean
  providerId?: string
  customers?: Customer[]
  services?: Service[]
}

const props = withDefaults(defineProps<Props>(), {
  customers: () => [],
  services: () => [],
  providerId: ''
})

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  'submit': [data: BookingFormData]
}>()

const hierarchyStore = useHierarchyStore()

const isOpen = computed({
  get: () => props.modelValue,
  set: (value) => emit('update:modelValue', value)
})

const searchCustomer = ref('')
const showCustomerDropdown = ref(false)
const filteredCustomers = ref<Customer[]>([])
const selectedCustomerName = ref('')
const selectedStaffMember = ref<StaffMember | null>(null)

const formData = ref<BookingFormData>({
  customerId: '',
  serviceId: '',
  staffId: null,
  dateTime: '',
  notes: ''
})

const selectedService = computed(() => {
  return props.services.find(s => s.id === formData.value.serviceId)
})

// Check if provider is an organization with staff
const providerHierarchy = computed(() => hierarchyStore.currentHierarchy)
const isOrganizationWithStaff = computed(() => {
  return providerHierarchy.value?.provider?.hierarchyType === ProviderHierarchyType.Organization &&
         (providerHierarchy.value?.provider?.staffCount ?? 0) > 0
})

// Show staff selector if organization has staff
const shouldShowStaffSelector = computed(() => isOrganizationWithStaff.value)

// Require staff selection for organizations with staff
const requiresStaffSelection = computed(() => isOrganizationWithStaff.value)

const isFormValid = computed(() => {
  const basicValid = formData.value.customerId &&
         formData.value.serviceId &&
         formData.value.dateTime

  // If staff selection is required, validate it too
  if (requiresStaffSelection.value) {
    return basicValid && !!formData.value.staffId
  }

  return basicValid
})

const filterCustomers = () => {
  const search = searchCustomer.value.toLowerCase().trim()
  if (!search) {
    filteredCustomers.value = props.customers.slice(0, 5)
  } else {
    filteredCustomers.value = props.customers
      .filter(c =>
        c.name.toLowerCase().includes(search) ||
        c.phone.includes(search)
      )
      .slice(0, 5)
  }
}

const selectCustomer = (customer: Customer) => {
  formData.value.customerId = customer.id
  selectedCustomerName.value = customer.name
  searchCustomer.value = customer.name
  showCustomerDropdown.value = false
}

const handleServiceChange = () => {
  // Reset staff selection when service changes
  // (different services may have different staff availability)
  formData.value.staffId = null
  selectedStaffMember.value = null
}

const handleStaffSelect = (staff: StaffMember) => {
  selectedStaffMember.value = staff
}

const handleSubmit = () => {
  if (!isFormValid.value) return

  emit('submit', { ...formData.value })
  resetForm()
  isOpen.value = false
}

const handleCancel = () => {
  resetForm()
  isOpen.value = false
}

const resetForm = () => {
  formData.value = {
    customerId: '',
    serviceId: '',
    staffId: null,
    dateTime: '',
    notes: ''
  }
  searchCustomer.value = ''
  selectedCustomerName.value = ''
  selectedStaffMember.value = null
  showCustomerDropdown.value = false
}

const convertToPersian = (num: number) => {
  return convertEnglishToPersianNumbers(num.toString())
}

// Load provider hierarchy when modal opens
watch(() => isOpen.value, async (newValue) => {
  if (newValue && props.providerId) {
    try {
      await hierarchyStore.loadProviderHierarchy(props.providerId)
    } catch (error) {
      console.error('Error loading provider hierarchy:', error)
    }
  }
  if (!newValue) {
    resetForm()
  }
})

// Initialize filtered customers
watch(() => props.customers, () => {
  if (props.customers.length > 0) {
    filteredCustomers.value = props.customers.slice(0, 5)
  }
}, { immediate: true })

onMounted(async () => {
  // Load provider hierarchy if providerId is available
  if (props.providerId) {
    try {
      await hierarchyStore.loadProviderHierarchy(props.providerId)
    } catch (error) {
      console.error('Error loading provider hierarchy:', error)
    }
  }
})
</script>

<style scoped lang="scss">
.booking-form {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 8px;
  position: relative;
}

.form-label {
  font-size: 14px;
  font-weight: 600;
  color: #374151;

  &.required::after {
    content: ' *';
    color: #ef4444;
  }
}

.form-input,
.form-select,
.form-textarea {
  width: 100%;
  padding: 12px 16px;
  border: 1px solid #d1d5db;
  border-radius: 8px;
  font-size: 14px;
  font-family: 'B Nazanin', sans-serif;
  transition: all 0.2s;

  &:focus {
    outline: none;
    border-color: #1976d2;
    box-shadow: 0 0 0 3px rgba(25, 118, 210, 0.1);
  }

  &::placeholder {
    color: #9ca3af;
  }
}

.form-textarea {
  resize: vertical;
  min-height: 80px;
}

.customer-search {
  position: relative;
}

.customers-dropdown {
  position: absolute;
  top: 100%;
  left: 0;
  right: 0;
  max-height: 200px;
  overflow-y: auto;
  background: white;
  border: 1px solid #d1d5db;
  border-radius: 8px;
  margin-top: 4px;
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
  z-index: 10;
}

.customer-option {
  padding: 12px 16px;
  cursor: pointer;
  transition: background 0.2s;

  &:hover {
    background: #f3f4f6;
  }

  &:not(:last-child) {
    border-bottom: 1px solid #e5e7eb;
  }
}

.customer-name {
  font-size: 14px;
  font-weight: 600;
  color: #1f2937;
  margin-bottom: 4px;
}

.customer-phone {
  font-size: 12px;
  color: #6b7280;
}

.selected-customer {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 12px;
  background: #e3f2fd;
  border-radius: 8px;
  font-size: 14px;
}

.selected-label {
  color: #1976d2;
  font-weight: 600;
}

.selected-value {
  color: #1f2937;
  font-weight: 500;
}

.booking-summary {
  padding: 16px;
  background: #f9fafb;
  border-radius: 8px;
  border-right: 4px solid #1976d2;
}

.summary-title {
  font-size: 16px;
  font-weight: 700;
  color: #1f2937;
  margin: 0 0 12px 0;
}

.summary-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 8px 0;

  &:not(:last-child) {
    border-bottom: 1px solid #e5e7eb;
  }
}

.summary-label {
  font-size: 14px;
  color: #6b7280;
}

.summary-value {
  font-size: 14px;
  font-weight: 600;
  color: #1f2937;
}

.btn-secondary,
.btn-primary {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  padding: 10px 24px;
  border-radius: 8px;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
  border: none;
  min-width: 100px;
}

.btn-secondary {
  background: #f3f4f6;
  color: #374151;

  &:hover {
    background: #e5e7eb;
  }
}

.btn-primary {
  background: #1976d2;
  color: white;

  &:hover:not(:disabled) {
    background: #1565c0;
    box-shadow: 0 4px 8px rgba(25, 118, 210, 0.3);
  }

  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
}

:deep(.persian-datepicker-input) {
  width: 100%;
  padding: 12px 16px;
  border: 1px solid #d1d5db;
  border-radius: 8px;
  font-size: 14px;
  font-family: 'B Nazanin', sans-serif;
  transition: all 0.2s;

  &:focus {
    outline: none;
    border-color: #1976d2;
    box-shadow: 0 0 0 3px rgba(25, 118, 210, 0.1);
  }
}
</style>
