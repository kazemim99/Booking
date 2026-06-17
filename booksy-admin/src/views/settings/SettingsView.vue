<template>
  <div class="page-container">
    <a-page-header :title="$t('settings.title')" :sub-title="$t('settings.subtitle')" />
    <a-card>
      <a-empty :description="$t('settings.comingSoon')">
        <template #image>
          <setting-outlined style="font-size: 48px; color: #999" />
        </template>
      </a-empty>
    </a-card>
  </div>
</template>

<script setup lang="ts">
import { SettingOutlined } from '@ant-design/icons-vue'
import { useI18n } from 'vue-i18n'

useI18n()
</script>
