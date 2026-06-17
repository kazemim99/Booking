<template>
  <a-dropdown placement="bottomRight">
    <a-button class="language-switcher">
      <GlobalOutlined />
      <span class="language-name">{{ localeName }}</span>
    </a-button>
    <template #overlay>
      <a-menu @click="handleLocaleChange" :selectedKeys="[currentLocale]">
        <a-menu-item key="fa">
          <CheckOutlined v-if="currentLocale === 'fa'" class="check-icon" />
          <span :class="{ 'selected-lang': currentLocale === 'fa' }">فارسی</span>
        </a-menu-item>
        <a-menu-item key="en">
          <CheckOutlined v-if="currentLocale === 'en'" class="check-icon" />
          <span :class="{ 'selected-lang': currentLocale === 'en' }">English</span>
        </a-menu-item>
      </a-menu>
    </template>
  </a-dropdown>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { GlobalOutlined, CheckOutlined } from '@ant-design/icons-vue'
import { useLocaleStore } from '../../stores/locale.store'
import { Language } from '../../types/locale.types'
import { useI18n } from 'vue-i18n'

const localeStore = useLocaleStore()
const { locale } = useI18n()

const currentLocale = computed(() => localeStore.currentLocale)
const localeName = computed(() => localeStore.localeName)

const handleLocaleChange = ({ key }: { key: string }) => {
  const newLocale = key as Language
  localeStore.setLocale(newLocale)
  locale.value = newLocale
}
</script>

<style scoped>
.language-switcher {
  display: flex;
  align-items: center;
  gap: 8px;
}

.language-name {
  font-weight: 500;
}

.check-icon {
  margin-inline-end: 8px;
  color: #1890ff;
}

.selected-lang {
  font-weight: 600;
  color: #1890ff;
}
</style>
