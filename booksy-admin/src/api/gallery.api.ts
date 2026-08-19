import apiClient from '../utils/axios'

/**
 * A provider gallery image, exactly as `GET /Providers/{id}/gallery` returns it.
 *
 * There is deliberately no moderation `status` here. The admin UI used to synthesise one
 * (`isActive ? 'Approved' : 'Rejected'`) and render it as if the backend had reviewed the
 * image; no such review exists. `isActive` only records whether the provider is showing
 * the image.
 */
export interface GalleryImage {
  id: string
  thumbnailUrl: string
  mediumUrl: string
  originalUrl: string
  caption?: string
  altText?: string
  displayOrder: number
  uploadedAt: string
  isActive: boolean
  isPrimary: boolean
}

class GalleryApi {
  async getProviderGallery(providerId: string): Promise<GalleryImage[]> {
    const response = await apiClient.get<GalleryImage[]>(`/Providers/${providerId}/gallery`)
    return response.data ?? []
  }

  async deleteImage(providerId: string, imageId: string): Promise<void> {
    await apiClient.delete(`/Providers/${providerId}/gallery/${imageId}`)
  }

  async updateImageMetadata(
    providerId: string,
    imageId: string,
    data: { caption?: string; altText?: string }
  ): Promise<void> {
    await apiClient.put(`/Providers/${providerId}/gallery/${imageId}`, data)
  }
}

export const galleryApi = new GalleryApi()
export default galleryApi
