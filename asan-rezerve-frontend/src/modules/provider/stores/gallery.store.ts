import { defineStore } from 'pinia'
import { ref } from 'vue'
import galleryService from '../services/gallery.service'
import type {
  GalleryImage,
  UploadProgress,
  UpdateGalleryImageMetadataRequest,
  ReorderGalleryImagesRequest
} from '../types/gallery.types'

export const useGalleryStore = defineStore('gallery', () => {
  // State
  const galleryImages = ref<GalleryImage[]>([])
  const uploadProgress = ref<Map<string, UploadProgress>>(new Map())
  const isUploading = ref(false)
  const isLoading = ref(false)
  const error = ref<string | null>(null)

  // Actions
  /**
   * Fetch all gallery images for a provider
   */
  async function fetchGalleryImages(providerId: string): Promise<void> {
    isLoading.value = true
    error.value = null

    try {
      galleryImages.value = await galleryService.getGalleryImages(providerId)
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Failed to load gallery images'
      console.error('Error fetching gallery images:', err)
      throw err
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Upload images to the provider's gallery
   */
  async function uploadImages(
    providerId: string,
    files: File[]
  ): Promise<GalleryImage[]> {
    isUploading.value = true
    error.value = null

    // Initialize progress tracking for each file
    files.forEach((file) => {
      uploadProgress.value.set(file.name, {
        file,
        progress: 0,
        status: 'pending'
      })
    })

    try {
      const uploadedImages = await galleryService.uploadImages(
        providerId,
        files,
        (progressEvent: any) => {
          // Calculate overall progress
          const percentCompleted = Math.round(
            (progressEvent.loaded * 100) / progressEvent.total
          )

          // Update progress for all files (simplified - in a real app, track per file)
          files.forEach((file) => {
            const progress = uploadProgress.value.get(file.name)
            if (progress) {
              progress.progress = percentCompleted
              progress.status = percentCompleted === 100 ? 'success' : 'uploading'
            }
          })
        }
      )

      // Optimistically add uploaded images to the gallery
      galleryImages.value.push(...uploadedImages)

      // Sort by display order
      galleryImages.value.sort((a, b) => a.displayOrder - b.displayOrder)

      // Clear progress after successful upload
      setTimeout(() => {
        uploadProgress.value.clear()
      }, 1000)

      return uploadedImages
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Failed to upload images'
      console.error('Error uploading images:', err)

      // Mark all files as error
      files.forEach((file) => {
        const progress = uploadProgress.value.get(file.name)
        if (progress) {
          progress.status = 'error'
          progress.error = error.value || 'Upload failed'
        }
      })

      throw err
    } finally {
      isUploading.value = false
    }
  }

  /**
   * Update gallery image metadata
   */
  async function updateImageMetadata(
    providerId: string,
    imageId: string,
    metadata: UpdateGalleryImageMetadataRequest
  ): Promise<void> {
    error.value = null

    try {
      await galleryService.updateImageMetadata(providerId, imageId, metadata)

      // Optimistically update local state
      const image = galleryImages.value.find((img) => img.id === imageId)
      if (image) {
        if (metadata.caption !== undefined) {
          image.caption = metadata.caption
        }
        if (metadata.altText !== undefined) {
          image.altText = metadata.altText
        }
      }
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Failed to update image metadata'
      console.error('Error updating image metadata:', err)
      throw err
    }
  }

  /**
   * Delete a gallery image
   */
  async function deleteImage(providerId: string, imageId: string): Promise<void> {
    console.log('📦 Gallery Store: deleteImage called', { providerId, imageId })
    error.value = null

    // Store original images for rollback
    const originalImages = [...galleryImages.value]

    try {
      // Optimistically remove from UI
      galleryImages.value = galleryImages.value.filter((img) => img.id !== imageId)
      console.log('📦 Gallery Store: Optimistically removed image from store')

      console.log('📦 Gallery Store: Calling gallery service deleteImage...')
      await galleryService.deleteImage(providerId, imageId)
      console.log('📦 Gallery Store: Delete request successful')
    } catch (err: any) {
      // Rollback on error
      console.log('📦 Gallery Store: Delete failed, rolling back')
      galleryImages.value = originalImages

      error.value = err.response?.data?.message || 'Failed to delete image'
      console.error('📦 Gallery Store: Error deleting image:', err)
      throw err
    }
  }

  /**
   * Reorder gallery images and optionally set a primary image
   */
  async function reorderImages(
    providerId: string,
    imageOrders: Array<{ imageId: string; newOrder: number }>,
    primaryImageId?: string
  ): Promise<void> {
    error.value = null

    // Store original images for rollback
    const originalImages = [...galleryImages.value]

    try {
      // Optimistically update display order in UI
      imageOrders.forEach(({ imageId, newOrder }) => {
        const image = galleryImages.value.find((img) => img.id === imageId)
        if (image) {
          image.displayOrder = newOrder
        }
      })

      // Optimistically update primary image if specified
      if (primaryImageId) {
        galleryImages.value.forEach((img) => {
          img.isPrimary = img.id === primaryImageId
        })
      }

      // Sort by new display order
      galleryImages.value.sort((a, b) => a.displayOrder - b.displayOrder)

      // Build imageOrders dictionary for API
      const imageOrdersDict: Record<string, number> = {}
      imageOrders.forEach(({ imageId, newOrder }) => {
        imageOrdersDict[imageId] = newOrder
      })

      await galleryService.reorderImages(providerId, {
        imageOrders: imageOrdersDict,
        primaryImageId
      })
    } catch (err: any) {
      // Rollback on error
      galleryImages.value = originalImages

      error.value = err.response?.data?.message || 'Failed to reorder images'
      console.error('Error reordering images:', err)
      throw err
    }
  }

  /**
   * Clear error message
   */
  function clearError(): void {
    error.value = null
  }

  /**
   * Reset store state
   */
  function $reset(): void {
    galleryImages.value = []
    uploadProgress.value.clear()
    isUploading.value = false
    isLoading.value = false
    error.value = null
  }

  return {
    // State
    galleryImages,
    uploadProgress,
    isUploading,
    isLoading,
    error,

    // Actions
    fetchGalleryImages,
    uploadImages,
    updateImageMetadata,
    deleteImage,
    reorderImages,
    clearError,
    $reset
  }
})
