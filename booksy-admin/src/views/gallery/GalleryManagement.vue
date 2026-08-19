<template>
  <div class="page-container">
    <a-page-header
      :title="t('provider.galleryManagement')"
      :sub-title="t('provider.imageModeration')"
    />

    <a-card>
      <a-result status="info" :title="t('gallery.unavailableTitle')">
        <template #subTitle>
          {{ t('gallery.unavailableDescription') }}
        </template>
        <template #extra>
          <a-button type="primary" @click="router.push('/providers')">
            {{ t('gallery.browseProviders') }}
          </a-button>
        </template>
      </a-result>
    </a-card>
  </div>
</template>

<script setup lang="ts">
// Platform-wide gallery moderation has no backend behind it: there is no endpoint that lists
// images across providers, and no approve/reject capability on a gallery image at all. This
// page used to imply otherwise — it rendered pending/approved/rejected counters and moderation
// buttons over a hardcoded empty list, so it always showed "0 images" and its buttons did
// nothing. Until the backend exposes moderation (see GALLERY_BACKEND_REQUIREMENTS.md), the
// page states that plainly and points at the per-provider galleries, which are real.
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'

const { t } = useI18n()
const router = useRouter()
</script>
