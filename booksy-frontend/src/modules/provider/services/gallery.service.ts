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

    let imageList: GalleryImage[] = []
    if (response.data) {
      if (Array.isArray(response.data)) {
        imageList = response.data
      } else if ('data' in response.data) {
        imageList = (response.data as ApiResponse<GalleryImage[]>).data || []
      }
    }
    return imageList.map(this.mapGalleryImageDates)
  }

  /**
   * Get all gallery images for a provider
   */
  async getGalleryImages(providerId: string): Promise<GalleryImage[]> {
    const response = await serviceCategoryClient.get<GalleryImage[]>(
      `${this.baseUrl}/${providerId}/gallery`
    )

    let imageList: GalleryImage[] = []
    if (response.data) {
      if (Array.isArray(response.data)) {
        imageList = response.data
      } else if ('data' in response.data) {
        imageList = (response.data as ApiResponse<GalleryImage[]>).data || []
      }
    }
    return imageList.map(this.mapGalleryImageDates)
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
    await serviceCategoryClient.delete(
      `${this.baseUrl}/${providerId}/gallery/${imageId}`
    )
  }

  /**
   * Reorder gallery images and optionally set a primary image
   * @param providerId - Provider ID
   * @param request - Reorder request with optional primary image ID
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
   * Set an image as primary (main image for the provider)
   */
  async setPrimaryImage(providerId: string, imageId: string): Promise<void> {
    await serviceCategoryClient.put(
      `${this.baseUrl}/${providerId}/gallery/${imageId}/set-primary`
    )
  }

  /**
   * Map gallery image dates from string to Date objects
   * Backend returns properly formatted URLs, use them as-is (no wrapping/normalization)
   */
  private mapGalleryImageDates = (image: any): GalleryImage => {
    return {
      id: image.id,
      thumbnailUrl: image.thumbnailUrl,
      mediumUrl: image.mediumUrl,
      originalUrl: image.originalUrl,
      displayOrder: image.displayOrder,
      caption: image.caption,
      altText: image.altText,
      uploadedAt: new Date(image.uploadedAt),
      isActive: image.isActive ?? true,
      isPrimary: image.isPrimary ?? false,
    }
  }
}

export const galleryService = new GalleryService()
export default galleryService
