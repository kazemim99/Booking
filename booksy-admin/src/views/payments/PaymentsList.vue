<template>
  <div class="page-container">
    <a-page-header :title="$t('payment.title')" :sub-title="$t('payment.subtitle')" />
    <a-card>
      <a-empty :description="$t('payment.comingSoon')">
        <template #image>
          <dollar-outlined style="font-size: 48px; color: #999" />
        </template>
      </a-empty>
    </a-card>
  </div>
</template>

<script setup lang="ts">
import { DollarOutlined } from '@ant-design/icons-vue'
import { useI18n } from 'vue-i18n'

useI18n()
</script>
