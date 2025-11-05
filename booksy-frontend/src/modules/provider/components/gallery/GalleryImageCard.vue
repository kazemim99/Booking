<template>
  <div class="gallery-image-card" :draggable="draggable" @dragstart="handleDragStart">
    <div class="image-container">
      <img
        :src="image.thumbnailUrl"
        :alt="image.altText || `Gallery image ${image.displayOrder + 1}`"
        class="gallery-image"
        loading="lazy"
      />

      <div class="image-overlay">
        <div class="action-buttons">
          <button
            type="button"
            class="action-btn edit-btn"
            @click="$emit('edit', image.id)"
            title="Edit image details"
            aria-label="Edit image details"
          >
            <svg
              xmlns="http://www.w3.org/2000/svg"
              viewBox="0 0 20 20"
              fill="currentColor"
              width="16"
              height="16"
            >
              <path
                d="M2.695 14.763l-1.262 3.154a.5.5 0 00.65.65l3.155-1.262a4 4 0 001.343-.885L17.5 5.5a2.121 2.121 0 00-3-3L3.58 13.42a4 4 0 00-.885 1.343z"
              />
            </svg>
          </button>

          <button
            type="button"
            class="action-btn delete-btn"
            @click="$emit('delete', image.id)"
            title="Delete image"
            aria-label="Delete image"
          >
            <svg
              xmlns="http://www.w3.org/2000/svg"
              viewBox="0 0 20 20"
              fill="currentColor"
              width="16"
              height="16"
            >
              <path
                fill-rule="evenodd"
                d="M8.75 1A2.75 2.75 0 006 3.75v.443c-.795.077-1.584.176-2.365.298a.75.75 0 10.23 1.482l.149-.022.841 10.518A2.75 2.75 0 007.596 19h4.807a2.75 2.75 0 002.742-2.53l.841-10.52.149.023a.75.75 0 00.23-1.482A41.03 41.03 0 0014 4.193V3.75A2.75 2.75 0 0011.25 1h-2.5zM10 4c.84 0 1.673.025 2.5.075V3.75c0-.69-.56-1.25-1.25-1.25h-2.5c-.69 0-1.25.56-1.25 1.25v.325C8.327 4.025 9.16 4 10 4zM8.58 7.72a.75.75 0 00-1.5.06l.3 7.5a.75.75 0 101.5-.06l-.3-7.5zm4.34.06a.75.75 0 10-1.5-.06l-.3 7.5a.75.75 0 101.5.06l.3-7.5z"
                clip-rule="evenodd"
              />
            </svg>
          </button>
        </div>

        <div v-if="selectable" class="selection-checkbox">
          <input
            type="checkbox"
            :checked="selected"
            @change="$emit('toggle-select', image.id)"
            :aria-label="`Select image ${image.displayOrder + 1}`"
          />
        </div>
      </div>

      <div v-if="draggable" class="drag-handle" title="Drag to reorder">
        <svg
          xmlns="http://www.w3.org/2000/svg"
          viewBox="0 0 20 20"
          fill="currentColor"
          width="16"
          height="16"
        >
          <path
            d="M10 3a1.5 1.5 0 110 3 1.5 1.5 0 010-3zM10 8.5a1.5 1.5 0 110 3 1.5 1.5 0 010-3zM11.5 15.5a1.5 1.5 0 10-3 0 1.5 1.5 0 003 0z"
          />
        </svg>
      </div>
    </div>

    <div v-if="showCaption && image.caption" class="image-caption">
      {{ image.caption }}
    </div>
  </div>
</template>

<script setup lang="ts">
import type { GalleryImage } from '../../types/gallery.types'

// Props
interface Props {
  image: GalleryImage
  selectable?: boolean
  selected?: boolean
  draggable?: boolean
  showCaption?: boolean
}

withDefaults(defineProps<Props>(), {
  selectable: false,
  selected: false,
  draggable: false,
  showCaption: true
})

// Emits
const emit = defineEmits<{
  edit: [imageId: string]
  delete: [imageId: string]
  'toggle-select': [imageId: string]
  dragstart: [event: DragEvent, imageId: string]
}>()

// Methods
function handleDragStart(event: DragEvent) {
  if (event.dataTransfer) {
    event.dataTransfer.effectAllowed = 'move'
    event.dataTransfer.setData('text/plain', '')
  }
  emit('dragstart', event, (event.currentTarget as HTMLElement).dataset.imageId || '')
}
</script>

<style scoped>
.gallery-image-card {
  position: relative;
  border-radius: 0.5rem;
  overflow: hidden;
  background-color: #fff;
  transition: transform 0.2s ease, box-shadow 0.2s ease;
}

.gallery-image-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06);
}

.gallery-image-card[draggable='true'] {
  cursor: grab;
}

.gallery-image-card[draggable='true']:active {
  cursor: grabbing;
}

.image-container {
  position: relative;
  aspect-ratio: 1 / 1;
  overflow: hidden;
  background-color: #f3f4f6;
}

.gallery-image {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
}

.image-overlay {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background-color: rgba(0, 0, 0, 0.4);
  display: flex;
  align-items: center;
  justify-content: center;
  opacity: 0;
  transition: opacity 0.2s ease;
}

.image-container:hover .image-overlay {
  opacity: 1;
}

.action-buttons {
  display: flex;
  gap: 0.5rem;
}

.action-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 2rem;
  height: 2rem;
  border: none;
  border-radius: 0.375rem;
  cursor: pointer;
  transition: all 0.2s ease;
  color: #fff;
}

.edit-btn {
  background-color: rgba(59, 130, 246, 0.9);
}

.edit-btn:hover {
  background-color: rgba(59, 130, 246, 1);
}

.delete-btn {
  background-color: rgba(239, 68, 68, 0.9);
}

.delete-btn:hover {
  background-color: rgba(239, 68, 68, 1);
}

.selection-checkbox {
  position: absolute;
  top: 0.5rem;
  left: 0.5rem;
}

.selection-checkbox input[type='checkbox'] {
  width: 1.25rem;
  height: 1.25rem;
  cursor: pointer;
}

.drag-handle {
  position: absolute;
  top: 0.5rem;
  right: 0.5rem;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 2rem;
  height: 2rem;
  background-color: rgba(0, 0, 0, 0.6);
  border-radius: 0.375rem;
  color: #fff;
  opacity: 0;
  transition: opacity 0.2s ease;
}

.image-container:hover .drag-handle {
  opacity: 1;
}

.image-caption {
  padding: 0.75rem;
  font-size: 0.875rem;
  color: #4b5563;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  background-color: #fff;
}
</style>
