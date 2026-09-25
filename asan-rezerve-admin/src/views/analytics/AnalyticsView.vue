<template>
  <div class="page-container">
    <a-page-header :title="$t('analytics.title')" :sub-title="$t('analytics.subtitle')" />
    <a-card>
      <a-empty :description="$t('analytics.comingSoon')">
        <template #image>
          <bar-chart-outlined style="font-size: 48px; color: #999" />
        </template>
      </a-empty>
    </a-card>
  </div>
</template>

<script setup lang="ts">
import { BarChartOutlined } from '@ant-design/icons-vue'
import { useI18n } from 'vue-i18n'

useI18n()
</script>
