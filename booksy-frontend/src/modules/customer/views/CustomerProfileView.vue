<template>
  <div class="customer-profile" dir="rtl">
    <div class="profile-card">
      <h2>پروفایل من</h2>
      <div class="profile-content">
        <div class="profile-avatar">
          <img :src="profile.avatar || '/placeholder.jpg'" alt="Avatar" />
          <button class="btn-change-avatar">تغییر عکس</button>
        </div>
        <div class="profile-form">
          <div class="form-group">
            <label>نام</label>
            <input v-model="profile.firstName" type="text" />
          </div>
          <div class="form-group">
            <label>نام خانوادگی</label>
            <input v-model="profile.lastName" type="text" />
          </div>
          <div class="form-group">
            <label>ایمیل</label>
            <input v-model="profile.email" type="email" />
          </div>
          <div class="form-group">
            <label>شماره موبایل</label>
            <input v-model="profile.phoneNumber" type="tel" />
          </div>
          <button @click="saveProfile" class="btn-save">ذخیره تغییرات</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useNotification } from '@/core/composables'

const notification = useNotification()

const profile = ref({
  firstName: 'علی',
  lastName: 'احمدی',
  email: 'ali@example.ir',
  phoneNumber: '09121234567',
  avatar: null,
})

function saveProfile() {
  // API call
  notification.success('موفق', 'پروفایل با موفقیت بهروزرسانی شد')
}
</script>

<style scoped>
.profile-card {
  background: white;
  padding: 2rem;
  border-radius: 0.75rem;
  max-width: 800px;
}

.profile-content {
  display: grid;
  grid-template-columns: auto 1fr;
  gap: 2rem;
  margin-top: 1.5rem;
}

.profile-avatar {
  text-align: center;
}

.profile-avatar img {
  width: 150px;
  height: 150px;
  border-radius: 50%;
  object-fit: cover;
  margin-bottom: 1rem;
}

.btn-change-avatar {
  padding: 0.5rem 1rem;
  background: var(--color-gray-100);
  border: none;
  border-radius: 0.375rem;
  cursor: pointer;
}

.form-group {
  margin-bottom: 1rem;
}

.form-group label {
  display: block;
  margin-bottom: 0.5rem;
  font-weight: 500;
}

.form-group input {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid var(--color-gray-300);
  border-radius: 0.5rem;
}

.btn-save {
  padding: 0.75rem 2rem;
  background: var(--color-primary);
  color: white;
  border: none;
  border-radius: 0.5rem;
  cursor: pointer;
  margin-top: 1rem;
}
</style>
