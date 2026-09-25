<template>
  <div class="page-container">
    <a-page-header :title="$t('service.catalog')" :sub-title="$t('service.manageServicesCategories')" />
    <a-card>
      <a-empty :description="$t('service.comingSoon')">
        <template #image>
          <appstore-outlined style="font-size: 48px; color: #999" />
        </template>
      </a-empty>
    </a-card>
  </div>
</template>

<script setup lang="ts">
import { AppstoreOutlined } from '@ant-design/icons-vue'
import { useI18n } from 'vue-i18n'

useI18n()
</script>
