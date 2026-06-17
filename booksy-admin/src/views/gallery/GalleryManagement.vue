<template>
  <div class="page-container">
    <a-page-header
      :title="t('provider.galleryManagement')"
      :sub-title="t('provider.imageModeration')"
    />

    <a-card>
      <!-- Filters -->
      <div class="filters-section">
        <a-row :gutter="16">
          <a-col :xs="24" :sm="12" :md="8">
            <a-input
              v-model:value="filters.search"
              :placeholder="t('provider.searchPlaceholder')"
              allow-clear
              @pressEnter="handleSearch"
            >
              <template #prefix>
                <search-outlined />
              </template>
            </a-input>
          </a-col>
          <a-col :xs="24" :sm="12" :md="8">
            <a-select
              v-model:value="filters.status"
              :placeholder="t('provider.imageStatus')"
              style="width: 100%"
              allow-clear
              @change="handleSearch"
            >
              <a-select-option value="Pending">{{ t('provider.pendingReview') }}</a-select-option>
              <a-select-option value="Approved">{{ t('provider.approved') }}</a-select-option>
              <a-select-option value="Rejected">{{ t('provider.rejected') }}</a-select-option>
            </a-select>
          </a-col>
          <a-col :xs="24" :sm="24" :md="8">
            <a-space>
              <a-button type="primary" @click="handleSearch">
                <search-outlined /> {{ t('common.search') }}
              </a-button>
              <a-button @click="handleReset">
                {{ t('common.reset') }}
              </a-button>
            </a-space>
          </a-col>
        </a-row>
      </div>

      <!-- Statistics -->
      <div class="stats-section">
        <a-row :gutter="16">
          <a-col :xs="12" :sm="6">
            <a-statistic
              :title="t('common.total')"
              :value="stats.total"
              :value-style="{ color: '#1890ff' }"
            />
          </a-col>
          <a-col :xs="12" :sm="6">
            <a-statistic
              :title="t('provider.pendingReview')"
              :value="stats.pending"
              :value-style="{ color: '#faad14' }"
            />
          </a-col>
          <a-col :xs="12" :sm="6">
            <a-statistic
              :title="t('provider.approved')"
              :value="stats.approved"
              :value-style="{ color: '#52c41a' }"
            />
          </a-col>
          <a-col :xs="12" :sm="6">
            <a-statistic
              :title="t('provider.rejected')"
              :value="stats.rejected"
              :value-style="{ color: '#f5222d' }"
            />
          </a-col>
        </a-row>
      </div>

      <a-divider />

      <!-- Gallery Grid -->
      <a-spin :spinning="loading">
        <div v-if="images.length === 0" class="empty-state">
          <a-empty :description="t('provider.noImages')" />
        </div>

        <div v-else class="gallery-grid">
          <div
            v-for="image in images"
            :key="image.id"
            class="gallery-item"
          >
            <div class="image-wrapper">
              <img :src="image.thumbnailUrl" :alt="image.altText || image.caption" />

              <!-- Primary Badge -->
              <div v-if="image.isPrimary" class="primary-badge">
                {{ t('provider.primaryImage') }}
              </div>

              <!-- Image Overlay with Actions -->
              <div class="image-overlay">
                <a-space direction="vertical">
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
                  <a-button
                    size="small"
                    @click="handleViewProvider(image.providerId)"
                  >
                    {{ t('provider.viewGallery') }}
                  </a-button>
                </a-space>
              </div>
            </div>

            <!-- Image Info -->
            <div class="image-info">
              <div class="image-header">
                <a-tag :color="getImageStatusColor(image.status)">
                  {{ image.status }}
                </a-tag>
              </div>
              <div v-if="image.caption" class="image-caption">
                {{ image.caption }}
              </div>
              <div class="image-meta">
                <small>{{ t('provider.uploadedAt') }}: {{ formatDate(image.uploadedAt) }}</small>
              </div>
            </div>
          </div>
        </div>

        <!-- Pagination -->
        <div v-if="images.length > 0" class="pagination-section">
          <a-pagination
            v-model:current="pagination.current"
            v-model:pageSize="pagination.pageSize"
            :total="pagination.total"
            :show-size-changer="pagination.showSizeChanger"
            :show-total="(total: number) => `${t('common.total')} ${total} ${t('provider.images')}`"
            @change="handlePageChange"
          />
        </div>
      </a-spin>
    </a-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { message } from 'ant-design-vue'
import { SearchOutlined } from '@ant-design/icons-vue'
import dayjs from 'dayjs'
import { galleryApi, type GalleryImage } from '../../api/gallery.api'

const { t } = useI18n()
const router = useRouter()

const loading = ref(false)
const images = ref<GalleryImage[]>([])

const filters = reactive({
  search: '',
  status: undefined as 'Pending' | 'Approved' | 'Rejected' | undefined,
})

const pagination = reactive({
  current: 1,
  pageSize: 20,
  total: 0,
  showSizeChanger: true,
})

const stats = computed(() => {
  return {
    total: pagination.total,
    pending: images.value.filter(img => img.status === 'Pending').length,
    approved: images.value.filter(img => img.status === 'Approved').length,
    rejected: images.value.filter(img => img.status === 'Rejected').length,
  }
})

const getImageStatusColor = (status: string) => {
  const colors: Record<string, string> = {
    Pending: 'orange',
    Approved: 'green',
    Rejected: 'red',
  }
  return colors[status] || 'default'
}

const formatDate = (date: string) => {
  return dayjs(date).format('MMM DD, YYYY HH:mm')
}

const loadGalleryImages = async () => {
  loading.value = true
  try {
    const response = await galleryApi.getAllGalleryImages({
      pageNumber: pagination.current,
      pageSize: pagination.pageSize,
      status: filters.status,
      search: filters.search || undefined,
    })
    images.value = response.items
    pagination.total = response.totalCount
  } catch (error) {
    message.error(t('provider.failedToLoadGallery'))
  } finally {
    loading.value = false
  }
}

const handleSearch = () => {
  pagination.current = 1
  loadGalleryImages()
}

const handleReset = () => {
  filters.search = ''
  filters.status = undefined
  pagination.current = 1
  loadGalleryImages()
}

const handlePageChange = () => {
  loadGalleryImages()
}

const handleApproveImage = async (image: GalleryImage) => {
  try {
    await galleryApi.approveImage(image.providerId, image.id)
    message.success(t('provider.imageApproved'))
    loadGalleryImages()
  } catch (error) {
    message.error(t('provider.failedToApproveImage'))
  }
}

const handleRejectImage = async (image: GalleryImage) => {
  try {
    await galleryApi.rejectImage(image.providerId, image.id)
    message.success(t('provider.imageRejected'))
    loadGalleryImages()
  } catch (error) {
    message.error(t('provider.failedToRejectImage'))
  }
}

const handleDeleteImage = async (image: GalleryImage) => {
  try {
    await galleryApi.deleteImage(image.providerId, image.id)
    message.success(t('provider.imageDeleted'))
    loadGalleryImages()
  } catch (error) {
    message.error(t('provider.failedToDeleteImage'))
  }
}

const handleViewProvider = (providerId: string) => {
  router.push(`/providers/${providerId}`)
}

onMounted(() => {
  loadGalleryImages()
})
</script>

<style scoped>
.filters-section {
  margin-bottom: 24px;
}

.stats-section {
  margin: 24px 0;
}

.gallery-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
  gap: 16px;
  margin-top: 24px;
}

.gallery-item {
  border: 1px solid #f0f0f0;
  border-radius: 8px;
  overflow: hidden;
  transition: box-shadow 0.3s;
}

.gallery-item:hover {
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
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
  z-index: 1;
}

.image-overlay {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(0, 0, 0, 0.8);
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
  padding: 12px;
}

.image-header {
  margin-bottom: 8px;
}

.image-caption {
  font-size: 13px;
  color: #333;
  margin-bottom: 4px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.image-meta {
  color: #999;
  font-size: 11px;
}

.empty-state {
  padding: 60px 0;
  text-align: center;
}

.pagination-section {
  margin-top: 24px;
  display: flex;
  justify-content: center;
}
</style>
