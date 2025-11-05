<template>
  <div class="gallery-grid-container">
    <!-- Empty state -->
    <div v-if="images.length === 0 && !loading" class="empty-state">
      <div class="empty-icon">
        <svg
          xmlns="http://www.w3.org/2000/svg"
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
        >
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z"
          />
        </svg>
      </div>
      <h3 class="empty-title">No Images Yet</h3>
      <p class="empty-description">{{ emptyMessage }}</p>
    </div>

    <!-- Loading skeleton -->
    <div v-if="loading" class="gallery-grid">
      <div v-for="n in skeletonCount" :key="`skeleton-${n}`" class="skeleton-item">
        <div class="skeleton-image"></div>
        <div v-if="showCaptions" class="skeleton-caption"></div>
      </div>
    </div>

    <!-- Gallery grid -->
    <div v-if="images.length > 0 && !loading" class="gallery-grid" :class="gridClass">
      <GalleryImageCard
        v-for="image in images"
        :key="image.id"
        :image="image"
        :selectable="selectable"
        :selected="selectedImages.includes(image.id)"
        :draggable="reorderable"
        :show-caption="showCaptions"
        @edit="$emit('edit', $event)"
        @delete="$emit('delete', $event)"
        @toggle-select="toggleSelection"
        @dragstart="handleDragStart($event, image.id)"
        @dragover.prevent="handleDragOver"
        @drop="handleDrop($event, image.id)"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import GalleryImageCard from './GalleryImageCard.vue'
import type { GalleryImage } from '../../types/gallery.types'

// Props
interface Props {
  images: GalleryImage[]
  loading?: boolean
  selectable?: boolean
  reorderable?: boolean
  showCaptions?: boolean
  columns?: 2 | 3 | 4
  emptyMessage?: string
}

const props = withDefaults(defineProps<Props>(), {
  loading: false,
  selectable: false,
  reorderable: false,
  showCaptions: true,
  columns: 3,
  emptyMessage: 'Upload photos to showcase your work and attract more customers'
})

// Emits
const emit = defineEmits<{
  edit: [imageId: string]
  delete: [imageId: string]
  reorder: [imageOrders: Array<{ imageId: string; newOrder: number }>]
  'selection-change': [selectedIds: string[]]
}>()

// State
const selectedImages = ref<string[]>([])
const draggedImageId = ref<string | null>(null)
const skeletonCount = ref(6)

// Computed
const gridClass = computed(() => {
  return `grid-cols-${props.columns}`
})

// Methods
function toggleSelection(imageId: string) {
  const index = selectedImages.value.indexOf(imageId)
  if (index > -1) {
    selectedImages.value.splice(index, 1)
  } else {
    selectedImages.value.push(imageId)
  }
  emit('selection-change', selectedImages.value)
}

function handleDragStart(event: DragEvent, imageId: string) {
  draggedImageId.value = imageId
}

function handleDragOver(event: DragEvent) {
  event.preventDefault()
}

function handleDrop(event: DragEvent, targetImageId: string) {
  event.preventDefault()

  if (!draggedImageId.value || draggedImageId.value === targetImageId) {
    return
  }

  const draggedIndex = props.images.findIndex((img) => img.id === draggedImageId.value)
  const targetIndex = props.images.findIndex((img) => img.id === targetImageId)

  if (draggedIndex === -1 || targetIndex === -1) {
    return
  }

  // Calculate new display orders
  const imageOrders: Array<{ imageId: string; newOrder: number }> = []

  // Reorder logic: move dragged item to target position
  const newImages = [...props.images]
  const [draggedImage] = newImages.splice(draggedIndex, 1)
  newImages.splice(targetIndex, 0, draggedImage)

  // Update display orders
  newImages.forEach((image, index) => {
    if (image.displayOrder !== index) {
      imageOrders.push({
        imageId: image.id,
        newOrder: index
      })
    }
  })

  if (imageOrders.length > 0) {
    emit('reorder', imageOrders)
  }

  draggedImageId.value = null
}

// Expose methods
defineExpose({
  clearSelection: () => {
    selectedImages.value = []
    emit('selection-change', [])
  },
  selectAll: () => {
    selectedImages.value = props.images.map((img) => img.id)
    emit('selection-change', selectedImages.value)
  }
})
</script>

<script lang="ts">
import { computed } from 'vue'
</script>

<style scoped>
.gallery-grid-container {
  width: 100%;
}

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 4rem 2rem;
  background-color: #f9fafb;
  border: 2px dashed #d1d5db;
  border-radius: 0.75rem;
  text-align: center;
}

.empty-icon {
  width: 4rem;
  height: 4rem;
  color: #9ca3af;
  margin-bottom: 1rem;
}

.empty-title {
  font-size: 1.25rem;
  font-weight: 600;
  color: #111827;
  margin: 0 0 0.5rem 0;
}

.empty-description {
  font-size: 0.875rem;
  color: #6b7280;
  margin: 0;
  max-width: 400px;
}

.gallery-grid {
  display: grid;
  gap: 1rem;
}

.grid-cols-2 {
  grid-template-columns: repeat(2, 1fr);
}

.grid-cols-3 {
  grid-template-columns: repeat(3, 1fr);
}

.grid-cols-4 {
  grid-template-columns: repeat(4, 1fr);
}

.skeleton-item {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.skeleton-image {
  aspect-ratio: 1 / 1;
  background: linear-gradient(90deg, #f3f4f6 25%, #e5e7eb 50%, #f3f4f6 75%);
  background-size: 200% 100%;
  animation: shimmer 1.5s infinite;
  border-radius: 0.5rem;
}

.skeleton-caption {
  height: 1.25rem;
  background: linear-gradient(90deg, #f3f4f6 25%, #e5e7eb 50%, #f3f4f6 75%);
  background-size: 200% 100%;
  animation: shimmer 1.5s infinite;
  border-radius: 0.25rem;
  width: 70%;
}

@keyframes shimmer {
  0% {
    background-position: 200% 0;
  }
  100% {
    background-position: -200% 0;
  }
}

/* Responsive */
@media (max-width: 1024px) {
  .grid-cols-4 {
    grid-template-columns: repeat(3, 1fr);
  }
}

@media (max-width: 768px) {
  .grid-cols-3,
  .grid-cols-4 {
    grid-template-columns: repeat(2, 1fr);
  }
}

@media (max-width: 480px) {
  .grid-cols-2,
  .grid-cols-3,
  .grid-cols-4 {
    grid-template-columns: 1fr;
  }
}
</style>
