<template>
  <div class="booking-wizard" dir="rtl">
    <div class="wizard-container">
      <div class="wizard-steps">
        <div v-for="(step, index) in steps" :key="index" 
             class="step" 
             :class="{ active: currentStep === index, completed: currentStep > index }">
          <div class="step-number">{{ index + 1 }}</div>
          <div class="step-label">{{ step }}</div>
        </div>
      </div>

      <div class="wizard-content">
        <div v-if="currentStep === 0" class="step-content">
          <h3>انتخاب خدمت</h3>
          <p>لطفاً خدمت مورد نظر خود را انتخاب کنید</p>
          <select v-model="selectedService" class="form-select">
            <option value="">انتخاب کنید...</option>
            <option value="1">کوتاهی مو</option>
            <option value="2">رنگ مو</option>
          </select>
        </div>

        <div v-if="currentStep === 1" class="step-content">
          <h3>انتخاب ارائه‌دهنده</h3>
          <p>ارائه‌دهنده مورد نظر خود را انتخاب کنید</p>
          <select v-model="selectedProvider" class="form-select">
            <option value="">انتخاب کنید...</option>
            <option value="1">آرایشگاه زیبا</option>
            <option value="2">اسپا رویا</option>
          </select>
        </div>

        <div v-if="currentStep === 2" class="step-content">
          <h3>انتخاب زمان</h3>
          <p>تاریخ و ساعت مورد نظر را انتخاب کنید</p>
          <input v-model="selectedDate" type="date" class="form-input" />
          <input v-model="selectedTime" type="time" class="form-input" />
        </div>

        <div v-if="currentStep === 3" class="step-content">
          <h3>تایید و پرداخت</h3>
          <div class="booking-summary">
            <p><strong>خدمت:</strong> کوتاهی مو</p>
            <p><strong>ارائه‌دهنده:</strong> آرایشگاه زیبا</p>
            <p><strong>زمان:</strong> {{ selectedDate }} - {{ selectedTime }}</p>
            <p class="total"><strong>مبلغ کل:</strong> 150,000 تومان</p>
          </div>
        </div>
      </div>

      <div class="wizard-actions">
        <button v-if="currentStep > 0" @click="prevStep" class="btn-prev">قبلی</button>
        <button v-if="currentStep < 3" @click="nextStep" class="btn-next">بعدی</button>
        <button v-if="currentStep === 3" @click="confirmBooking" class="btn-confirm">
          تایید و پرداخت
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useNotification } from '@/core/composables'

const router = useRouter()
const notification = useNotification()

const steps = ['انتخاب خدمت', 'انتخاب ارائه‌دهنده', 'انتخاب زمان', 'تایید و پرداخت']
const currentStep = ref(0)

const selectedService = ref('')
const selectedProvider = ref('')
const selectedDate = ref('')
const selectedTime = ref('')

function nextStep() {
  if (currentStep.value < 3) {
    currentStep.value++
  }
}

function prevStep() {
  if (currentStep.value > 0) {
    currentStep.value--
  }
}

function confirmBooking() {
  notification.success('موفق', 'رزرو شما با موفقیت ثبت شد')
  router.push('/customer/my-bookings')
}
</script>

<style scoped>
.wizard-container {
  background: white;
  padding: 2rem;
  border-radius: 0.75rem;
  max-width: 800px;
  margin: 0 auto;
}

.wizard-steps {
  display: flex;
  justify-content: space-between;
  margin-bottom: 3rem;
}

.step {
  display: flex;
  flex-direction: column;
  align-items: center;
  flex: 1;
  position: relative;
}

.step-number {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background: var(--color-gray-200);
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 600;
  margin-bottom: 0.5rem;
}

.step.active .step-number {
  background: var(--color-primary);
  color: white;
}

.step.completed .step-number {
  background: var(--color-green-500);
  color: white;
}

.step-content {
  min-height: 300px;
}

.step-content h3 {
  margin-top: 0;
}

.form-select,
.form-input {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid var(--color-gray-300);
  border-radius: 0.5rem;
  margin-top: 1rem;
}

.booking-summary {
  background: var(--color-gray-50);
  padding: 1.5rem;
  border-radius: 0.5rem;
}

.booking-summary p {
  margin: 0.75rem 0;
}

.total {
  font-size: 1.25rem;
  color: var(--color-primary);
  margin-top: 1.5rem;
  padding-top: 1rem;
  border-top: 2px solid var(--color-gray-200);
}

.wizard-actions {
  display: flex;
  justify-content: space-between;
  gap: 1rem;
  margin-top: 2rem;
}

.wizard-actions button {
  padding: 0.75rem 2rem;
  border: none;
  border-radius: 0.5rem;
  cursor: pointer;
}

.btn-prev {
  background: var(--color-gray-100);
}

.btn-next,
.btn-confirm {
  background: var(--color-primary);
  color: white;
}
</style>
