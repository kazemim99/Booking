<template>
  <div class="page-container">
    <a-page-header :title="$t('logs.title')" :sub-title="$t('logs.subtitle')" />
    <a-card>
      <a-empty :description="$t('logs.comingSoon')">
        <template #image>
          <file-text-outlined style="font-size: 48px; color: #999" />
        </template>
      </a-empty>
    </a-card>
  </div>
</template>

<script setup lang="ts">
import { FileTextOutlined } from '@ant-design/icons-vue'
import { useI18n } from 'vue-i18n'

useI18n()
</script>
