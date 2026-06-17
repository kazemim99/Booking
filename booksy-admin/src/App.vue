<template>
  <a-config-provider :direction="direction" :locale="antdLocale" :theme="antdTheme">
    <div id="app" :dir="direction" :lang="currentLocale">
      <router-view />
    </div>
  </a-config-provider>
</template>

<script setup lang="ts">
import { onMounted, computed } from 'vue'
import { ConfigProvider } from 'ant-design-vue'
import faIR from 'ant-design-vue/es/locale/fa_IR'
import enUS from 'ant-design-vue/es/locale/en_US'
import { useAuthStore } from './stores/auth.store'
import { useLocaleStore } from './stores/locale.store'
import { useRTL } from './composables/useRTL'
import { Language } from './types/locale.types'
import { antdTheme } from './config/antd-theme'

const AConfigProvider = ConfigProvider

const authStore = useAuthStore()
const localeStore = useLocaleStore()
const { direction } = useRTL()

const currentLocale = computed(() => localeStore.currentLocale)
const antdLocale = computed(() =>
  currentLocale.value === Language.Persian ? faIR : enUS
)

onMounted(async () => {
  await authStore.initialize()
})
</script>
