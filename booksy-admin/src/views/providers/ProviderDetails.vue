<template>
  <div class="page-container">
    <a-page-header
      :title="provider?.businessName"
      :sub-title="t('provider.providerDetails')"
      @back="handleBack"
    >
      <template #extra>
        <a-popconfirm
          v-if="provider && canActivate(provider.status)"
          :title="t('provider.confirmActivate')"
          :ok-text="t('common.confirm')"
          :cancel-text="t('common.cancel')"
          @confirm="handleActivate"
        >
          <a-button type="primary">
            <check-outlined /> {{ t('provider.activate') }}
          </a-button>
        </a-popconfirm>
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
                  {{ t(statusLabelKey(provider?.status)) }}
                </a-tag>
              </a-descriptions-item>
              <a-descriptions-item :label="t('provider.tableType')">
                {{ provider?.type }}
              </a-descriptions-item>
              <a-descriptions-item :label="t('provider.isApproved')">
                {{ provider?.isVerified ? t('common.yes') : t('common.no') }}
              </a-descriptions-item>
              <a-descriptions-item :label="t('provider.email')">
                {{ provider?.contactInfo?.email || t('provider.notAvailable') }}
              </a-descriptions-item>
              <a-descriptions-item :label="t('provider.phone')">
                {{ provider?.contactInfo?.primaryPhone || t('provider.notAvailable') }}
              </a-descriptions-item>
              <a-descriptions-item :label="t('provider.rating')" :span="2">
                <a-rate :value="provider?.averageRating || 0" disabled allow-half />
                <span style="margin-left: 8px">
                  {{ provider?.averageRating?.toFixed(2) ?? '—' }}
                  ({{ provider?.totalReviews ?? 0 }})
                </span>
              </a-descriptions-item>
              <a-descriptions-item :label="t('provider.description')" :span="2">
                {{ provider?.description || t('provider.noDescriptionProvided') }}
              </a-descriptions-item>
              <a-descriptions-item :label="t('provider.address')" :span="2">
                {{ formattedAddress || t('provider.noAddressProvided') }}
              </a-descriptions-item>
            </a-descriptions>
          </a-card>
        </a-col>

        <a-col :xs="24" :lg="8">
          <a-card :title="t('provider.statistics')">
            <a-statistic
              :title="t('provider.tableServices')"
              :value="provider?.serviceCount ?? 0"
              style="margin-bottom: 16px"
            />
            <a-statistic
              :title="t('provider.registered')"
              :value="formatDate(provider?.registeredAt) || '—'"
            />
          </a-card>
        </a-col>
      </a-row>

      <!-- Gallery Section -->
      <a-row v-if="provider" :gutter="[16, 16]" style="margin-top: 16px">
        <a-col :span="24">
          <a-card :title="t('provider.gallery')">
            <template #extra>
              <a-tag>{{ galleryImages.length }} {{ t('provider.images') }}</a-tag>
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
                      <a-space direction="vertical">
                        <a-button size="small" @click="openImage(image)">
                          {{ t('provider.viewGallery') }}
                        </a-button>
                        <a-popconfirm
                          :title="t('provider.confirmDeleteImage')"
                          :ok-text="t('common.confirm')"
                          :cancel-text="t('common.cancel')"
                          @confirm="handleDeleteImage(image)"
                        >
                          <a-button danger size="small">
                            {{ t('provider.deleteImage') }}
                          </a-button>
                        </a-popconfirm>
                      </a-space>
                    </div>
                  </div>
                  <div class="image-info">
                    <div v-if="image.caption" class="image-caption">{{ image.caption }}</div>
                    <small>{{ t('provider.uploadedAt') }}: {{ formatDate(image.uploadedAt) }}</small>
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
import { CheckOutlined } from '@ant-design/icons-vue'
import { providersApi } from '../../api/providers.api'
import { galleryApi, type GalleryImage } from '../../api/gallery.api'
import { getStatusColor, statusLabelKey, type ProviderStatus } from '../../constants/provider-status'
import { formatDate } from '../../utils/date'
import type { ProviderDetails } from '../../types'

const { t } = useI18n()

const route = useRoute()
const router = useRouter()
const loading = ref(false)
const provider = ref<ProviderDetails | null>(null)
const galleryLoading = ref(false)
const galleryImages = ref<GalleryImage[]>([])

const formattedAddress = computed(() => {
  const address = provider.value?.address
  if (!address) return ''
  return [address.formattedAddress, address.city, address.state].filter(Boolean).join('، ')
})

const canActivate = (status: ProviderStatus) =>
  status !== 'Active' && status !== 'Suspended' && status !== 'Archived'

const loadProvider = async () => {
  loading.value = true
  try {
    provider.value = await providersApi.getProviderById(route.params.id as string)
  } catch (error) {
    message.error(t('provider.failedToLoadProviders'))
    router.back()
  } finally {
    loading.value = false
  }
}

const handleBack = () => {
  if (window.history.length > 1) {
    router.back()
  } else {
    router.push('/providers')
  }
}

const handleActivate = async () => {
  try {
    await providersApi.activateProvider(provider.value!.id)
    message.success(t('provider.providerActivatedSuccessfully'))
    loadProvider()
  } catch (error) {
    message.error(t('provider.failedToActivateProvider'))
  }
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

const openImage = (image: GalleryImage) => {
  window.open(image.originalUrl, '_blank', 'noopener')
}

const handleDeleteImage = async (image: GalleryImage) => {
  try {
    await galleryApi.deleteImage(provider.value!.id, image.id)
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
