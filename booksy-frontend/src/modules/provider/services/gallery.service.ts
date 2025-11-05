import { serviceCategoryClient } from '@/core/api/client/http-client'
import type { ApiResponse } from '@/core/api/client/api-response'
import type {
  GalleryImage,
  UpdateGalleryImageMetadataRequest,
  ReorderGalleryImagesRequest
} from '../types/gallery.types'

/**
 * Gallery API service for managing provider image galleries
 */
class GalleryService {
  private readonly baseUrl = '/v1/providers'

  /**
   * Upload one or more images to the provider's gallery
   */
  async uploadImages(
    providerId: string,
    files: File[],
    onUploadProgress?: (progressEvent: any) => void
  ): Promise<GalleryImage[]> {
    const formData = new FormData()

    files.forEach((file) => {
      formData.append('files', file)
    })

    const response = await serviceCategoryClient.post<ApiResponse<GalleryImage[]>>(
      `${this.baseUrl}/${providerId}/gallery`,
      formData,
      {
        headers: {
          'Content-Type': 'multipart/form-data'
        },
        onUploadProgress
      }
    )

    const images = response.data?.data || []
    return images.map(this.mapGalleryImageDates)
  }

  /**
   * Get all gallery images for a provider
   */
  async getGalleryImages(providerId: string): Promise<GalleryImage[]> {
    const response = await serviceCategoryClient.get<ApiResponse<GalleryImage[]>>(
      `${this.baseUrl}/${providerId}/gallery`
    )

    const images = response.data?.data || []
    return images.map(this.mapGalleryImageDates)
  }

  /**
   * Update gallery image metadata (caption, alt text)
   */
  async updateImageMetadata(
    providerId: string,
    imageId: string,
    metadata: UpdateGalleryImageMetadataRequest
  ): Promise<void> {
    await serviceCategoryClient.put(
      `${this.baseUrl}/${providerId}/gallery/${imageId}`,
      metadata
    )
  }

  /**
   * Delete a gallery image
   */
  async deleteImage(providerId: string, imageId: string): Promise<void> {
    await serviceCategoryClient.delete(`${this.baseUrl}/${providerId}/gallery/${imageId}`)
  }

  /**
   * Reorder gallery images
   */
  async reorderImages(
    providerId: string,
    request: ReorderGalleryImagesRequest
  ): Promise<void> {
    await serviceCategoryClient.put(
      `${this.baseUrl}/${providerId}/gallery/reorder`,
      request
    )
  }

  /**
   * Map gallery image dates from string to Date objects
   */
  private mapGalleryImageDates(image: any): GalleryImage {
    return {
      ...image,
      uploadedAt: new Date(image.uploadedAt)
    }
  }
}

export const galleryService = new GalleryService()
export default galleryService
