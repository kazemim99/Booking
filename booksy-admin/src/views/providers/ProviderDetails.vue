<template>
  <div class="page-container">
    <a-page-header
      :title="provider?.businessName"
      :sub-title="t('provider.providerDetails')"
      @back="handleBack"
    >
      <template #extra>
        <a-space>
          <a-button v-if="provider?.status === 'Pending'" type="primary" @click="handleApprove">
            <check-outlined /> {{ t('provider.approve') }}
          </a-button>
          <a-button v-if="provider?.status === 'Pending'" danger @click="handleReject">
            <close-outlined /> {{ t('provider.reject') }}
          </a-button>
          <a-button v-if="provider?.status === 'Approved'" danger @click="handleSuspend">
            <stop-outlined /> {{ t('provider.suspend') }}
          </a-button>
        </a-space>
      </template>
    </a-page-header>

    <a-spin :spinning="loading">
      <a-row :gutter="[16, 16]">
        <a-col :xs="24" :lg="16">
          <a-card :title="t('provider.businessInformation')">
            <a-descriptions :column="2">
              <a-descriptions-item :label="t('provider.businessName')">
                {{ provider?.businessName }}
              </a-descriptions-item>
              <a-descriptions-item :label="t('common.status')">
                <a-tag :color="getStatusColor(provider?.status)">
                  {{ provider?.status }}
                </a-tag>
              </a-descriptions-item>
              <a-descriptions-item :label="t('provider.email')">
                {{ provider?.email }}
              </a-descriptions-item>
              <a-descriptions-item :label="t('provider.phone')">
                {{ provider?.phoneNumber }}
              </a-descriptions-item>
              <a-descriptions-item :label="t('provider.rating')" :span="2">
                <a-rate :value="provider?.rating || 0" disabled allow-half />
                <span style="margin-left: 8px">{{ provider?.rating?.toFixed(2) }}</span>
              </a-descriptions-item>
              <a-descriptions-item :label="t('provider.description')" :span="2">
                {{ provider?.description || t('provider.noDescriptionProvided') }}
              </a-descriptions-item>
              <a-descriptions-item :label="t('provider.address')" :span="2">
                {{ provider?.address || t('provider.noAddressProvided') }}
              </a-descriptions-item>
            </a-descriptions>
          </a-card>
        </a-col>

        <a-col :xs="24" :lg="8">
          <a-card :title="t('provider.statistics')">
            <a-statistic
              :title="t('provider.totalBookings')"
              :value="provider?.totalBookings || 0"
              style="margin-bottom: 16px"
            />
            <a-statistic
              :title="t('provider.registered')"
              :value="formatDate(provider?.createdAt)"
            />
            <a-divider />
            <a-statistic
              v-if="provider?.approvedAt"
              :title="t('provider.approvedOn')"
              :value="formatDate(provider?.approvedAt)"
            />
          </a-card>
        </a-col>
      </a-row>

      <!-- Gallery Section -->
      <a-row v-if="provider" :gutter="[16, 16]" style="margin-top: 16px">
        <a-col :span="24">
          <a-card :title="t('provider.gallery')">
            <template #extra>
              <a-tag :color="galleryStats.pending > 0 ? 'orange' : 'green'">
                {{ galleryStats.total }} {{ t('provider.images') }}
                <span v-if="galleryStats.pending > 0">
                  ({{ galleryStats.pending }} {{ t('provider.pendingReview') }})
                </span>
              </a-tag>
            </template>

            <a-spin :spinning="galleryLoading">
              <div v-if="galleryImages.length === 0" class="empty-gallery">
                <a-empty :description="t('provider.noImages')" />
              </div>

              <div v-else class="gallery-grid">
                <div
                  v-for="image in galleryImages"
                  :key="image.id"
                  class="gallery-item"
                >
                  <div class="image-wrapper">
                    <img :src="image.thumbnailUrl" :alt="image.altText || image.caption" />
                    <div v-if="image.isPrimary" class="primary-badge">
                      {{ t('provider.primaryImage') }}
                    </div>
                    <div class="image-overlay">
                      <a-space>
                        <a-button
                          v-if="image.status === 'Pending'"
                          type="primary"
                          size="small"
                          @click="handleApproveImage(image)"
                        >
                          {{ t('provider.approveImage') }}
                        </a-button>
                        <a-button
                          v-if="image.status === 'Pending'"
                          danger
                          size="small"
                          @click="handleRejectImage(image)"
                        >
                          {{ t('provider.rejectImage') }}
                        </a-button>
                        <a-button
                          danger
                          size="small"
                          @click="handleDeleteImage(image)"
                        >
                          {{ t('provider.deleteImage') }}
                        </a-button>
                      </a-space>
                    </div>
                  </div>
                  <div class="image-info">
                    <a-tag :color="getImageStatusColor(image.status)">
                      {{ image.status }}
                    </a-tag>
                    <div v-if="image.caption" class="image-caption">{{ image.caption }}</div>
                  </div>
                </div>
              </div>
            </a-spin>
          </a-card>
        </a-col>
      </a-row>
    </a-spin>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { message } from 'ant-design-vue'
import dayjs from 'dayjs'
import {
  CheckOutlined,
  CloseOutlined,
  StopOutlined,
} from '@ant-design/icons-vue'
import { providersApi } from '../../api/providers.api'
import { galleryApi, type GalleryImage } from '../../api/gallery.api'
import type { Provider } from '../../types'

const { t } = useI18n()

const route = useRoute()
const router = useRouter()
const loading = ref(false)
const provider = ref<Provider | null>(null)
const galleryLoading = ref(false)
const galleryImages = ref<GalleryImage[]>([])

const galleryStats = computed(() => {
  return {
    total: galleryImages.value.length,
    pending: galleryImages.value.filter(img => img.status === 'Pending').length,
    approved: galleryImages.value.filter(img => img.status === 'Approved').length,
    rejected: galleryImages.value.filter(img => img.status === 'Rejected').length,
  }
})

const getStatusColor = (status?: string) => {
  const colors: Record<string, string> = {
    Pending: 'orange',
    Approved: 'green',
    Rejected: 'red',
    Suspended: 'volcano',
  }
  return colors[status || ''] || 'default'
}

const formatDate = (date?: string) => {
  if (!date) return t('provider.notAvailable')
  return dayjs(date).format('MMM DD, YYYY')
}

const loadProvider = async () => {
  loading.value = true
  try {
    const id = route.params.id as string
    provider.value = await providersApi.getProviderById(id)
  } catch (error) {
    message.error(t('provider.failedToLoadProviders'))
    router.back()
  } finally {
    loading.value = false
  }
}

const handleBack = () => {
  // Check if there's a history entry to go back to
  if (window.history.length > 1) {
    router.back()
  } else {
    // If no history, navigate to providers list
    router.push('/providers')
  }
}

const handleApprove = async () => {
  try {
    await providersApi.approveProvider(provider.value!.id)
    message.success(t('provider.providerApprovedSuccessfully'))
    loadProvider()
  } catch (error) {
    message.error(t('provider.failedToApproveProvider'))
  }
}

const handleReject = () => {
  // Implementation with modal for reason
  message.info('Reject functionality - add modal for reason')
}

const handleSuspend = () => {
  // Implementation with modal for reason
  message.info('Suspend functionality - add modal for reason')
}

const getImageStatusColor = (status: string) => {
  const colors: Record<string, string> = {
    Pending: 'orange',
    Approved: 'green',
    Rejected: 'red',
  }
  return colors[status] || 'default'
}

const loadGallery = async () => {
  if (!provider.value) return

  galleryLoading.value = true
  try {
    galleryImages.value = await galleryApi.getProviderGallery(provider.value.id)
  } catch (error) {
    message.error(t('provider.failedToLoadGallery'))
  } finally {
    galleryLoading.value = false
  }
}

const handleApproveImage = async (image: GalleryImage) => {
  try {
    await galleryApi.approveImage(image.providerId, image.id)
    message.success(t('provider.imageApproved'))
    loadGallery()
  } catch (error) {
    message.error(t('provider.failedToApproveImage'))
  }
}

const handleRejectImage = async (image: GalleryImage) => {
  try {
    await galleryApi.rejectImage(image.providerId, image.id)
    message.success(t('provider.imageRejected'))
    loadGallery()
  } catch (error) {
    message.error(t('provider.failedToRejectImage'))
  }
}

const handleDeleteImage = async (image: GalleryImage) => {
  try {
    await galleryApi.deleteImage(image.providerId, image.id)
    message.success(t('provider.imageDeleted'))
    loadGallery()
  } catch (error) {
    message.error(t('provider.failedToDeleteImage'))
  }
}

onMounted(async () => {
  await loadProvider()
  await loadGallery()
})
</script>

<style scoped>
.gallery-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
  gap: 16px;
}

.gallery-item {
  border: 1px solid #f0f0f0;
  border-radius: 8px;
  overflow: hidden;
}

.image-wrapper {
  position: relative;
  width: 100%;
  padding-bottom: 100%;
  background-color: #f5f5f5;
  overflow: hidden;
}

.image-wrapper img {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.primary-badge {
  position: absolute;
  top: 8px;
  right: 8px;
  background-color: rgba(24, 144, 255, 0.9);
  color: white;
  padding: 4px 8px;
  border-radius: 4px;
  font-size: 12px;
  font-weight: 500;
}

.image-overlay {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(0, 0, 0, 0.7);
  display: flex;
  align-items: center;
  justify-content: center;
  opacity: 0;
  transition: opacity 0.3s;
}

.image-wrapper:hover .image-overlay {
  opacity: 1;
}

.image-info {
  padding: 8px;
}

.image-caption {
  margin-top: 4px;
  font-size: 12px;
  color: #666;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.empty-gallery {
  padding: 40px 0;
  text-align: center;
}
</style>
