<template>
  <div class="login-container">
    <a-card class="login-card" :title="$t('app.name')">
      <template #extra>
        <language-switcher />
      </template>
      <a-form :model="formData" @finish="handleLogin" layout="vertical">
        <a-form-item :label="$t('auth.email')" name="email" :rules="[{ required: true, type: 'email', message: $t('validation.email') }]">
          <a-input v-model:value="formData.email" size="large" placeholder="admin@booksy.com">
            <template #prefix><mail-outlined /></template>
          </a-input>
        </a-form-item>

        <a-form-item :label="$t('auth.password')" name="password" :rules="[{ required: true, message: $t('validation.required') }]">
          <a-input-password v-model:value="formData.password" size="large" :placeholder="$t('auth.password')">
            <template #prefix><lock-outlined /></template>
          </a-input-password>
        </a-form-item>

        <a-form-item>
          <a-button type="primary" html-type="submit" size="large" block :loading="loading">
            {{ $t('auth.signIn') }}
          </a-button>
        </a-form-item>
      </a-form>
    </a-card>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { message } from 'ant-design-vue'
import { useI18n } from 'vue-i18n'
import { MailOutlined, LockOutlined } from '@ant-design/icons-vue'
import { useAuthStore } from '../stores/auth.store'
import LanguageSwitcher from '../components/common/LanguageSwitcher.vue'

const { t } = useI18n()

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()
const loading = ref(false)

const formData = reactive({
  email: '',
  password: '',
})

const handleLogin = async () => {
  loading.value = true
  try {
    console.log('Attempting login...')
    const success = await authStore.login(formData)
    console.log('Login result:', success)
    console.log('Auth store state:', {
      isAuthenticated: authStore.isAuthenticated,
      isAdmin: authStore.isAdmin,
      user: authStore.user
    })

    if (success) {
      message.success(t('auth.loginSuccess'))

      // Get redirect path from query parameter, default to dashboard
      const redirect = (route.query.redirect as string) || '/dashboard'
      console.log('Attempting to redirect to:', redirect)

      // Use replace instead of push to avoid back button issues
      await router.replace(redirect)
      console.log('Router.replace called, current route:', router.currentRoute.value.path)
    } else {
      message.error(t('auth.invalidCredentials'))
    }
  } catch (error) {
    console.error('Login error:', error)
    message.error(t('messages.error.general'))
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.login-container {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.login-card {
  width: 100%;
  max-width: 400px;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.1);
}
</style>
