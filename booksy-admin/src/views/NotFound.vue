<template>
  <a-result
    status="404"
    title="404"
    :sub-title="$t('notFound.subtitle')"
  >
    <template #extra>
      <a-button type="primary" @click="$router.push('/')">
        {{ $t('notFound.backHome') }}
      </a-button>
    </template>
  </a-result>
</template>

<script setup lang="ts">
import { useI18n } from 'vue-i18n'

useI18n()
</script>
