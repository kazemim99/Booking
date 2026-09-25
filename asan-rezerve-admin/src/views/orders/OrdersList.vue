<template>
  <div class="page-container">
    <a-page-header :title="$t('order.title')" :sub-title="$t('order.subtitle')" />
    <a-card>
      <a-empty :description="$t('order.comingSoon')">
        <template #image>
          <shopping-cart-outlined style="font-size: 48px; color: #999" />
        </template>
      </a-empty>
    </a-card>
  </div>
</template>

<script setup lang="ts">
import { ShoppingCartOutlined } from '@ant-design/icons-vue'
import { useI18n } from 'vue-i18n'

useI18n()
</script>
