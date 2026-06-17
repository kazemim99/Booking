import apiClient from '../utils/axios'

// Backend response structure from GalleryImageResponse.cs
export interface GalleryImageBackend {
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

// Frontend interface with computed status
export interface GalleryImage extends GalleryImageBackend {
  providerId: string
  status: 'Pending' | 'Approved' | 'Rejected'
}

export interface GalleryImagesResponse {
  items: GalleryImage[]
  totalCount: number
}

class GalleryApi {
  /**
   * Get gallery images for a specific provider
   */
  async getProviderGallery(providerId: string): Promise<GalleryImage[]> {
    const response = await apiClient.get<GalleryImageBackend[]>(`/providers/${providerId}/gallery`)
    // Map backend response to frontend format with status
    return response.data.map(img => ({
      ...img,
      providerId,
      // For now, map isActive to status (can be enhanced when backend adds status field)
      status: img.isActive ? 'Approved' as const : 'Rejected' as const
    }))
  }

  /**
   * Get all gallery images (admin view)
   * NOTE: This endpoint doesn't exist in backend yet. Returns empty for now.
   */
  async getAllGalleryImages(_params?: {
    pageNumber?: number
    pageSize?: number
    status?: 'Pending' | 'Approved' | 'Rejected'
    search?: string
  }): Promise<GalleryImagesResponse> {
    // TODO: Backend needs to implement /admin/gallery endpoint
    // For now, return empty result
    console.warn('/admin/gallery endpoint not implemented in backend yet')
    return { items: [], totalCount: 0 }
  }

  /**
   * Activate/Approve a gallery image (reactivate if deactivated)
   * NOTE: Backend doesn't have /approve endpoint, using reactivation as workaround
   */
  async approveImage(_providerId: string, _imageId: string): Promise<void> {
    // TODO: Backend needs to implement approve endpoint or use existing reactivate
    // For now, update metadata to activate (this is a placeholder)
    console.warn('Approve endpoint not implemented. Backend needs to add this feature.')
  }

  /**
   * Deactivate/Reject a gallery image
   * NOTE: Backend doesn't have /reject endpoint
   */
  async rejectImage(_providerId: string, _imageId: string, _reason?: string): Promise<void> {
    // TODO: Backend needs to implement reject endpoint
    // Could potentially use the deactivate functionality
    console.warn('Reject endpoint not implemented. Backend needs to add this feature.')
  }

  /**
   * Delete a gallery image
   */
  async deleteImage(providerId: string, imageId: string): Promise<void> {
    await apiClient.delete(`/providers/${providerId}/gallery/${imageId}`)
  }

  /**
   * Update image metadata (caption, alt text)
   */
  async updateImageMetadata(
    providerId: string,
    imageId: string,
    data: { caption?: string; altText?: string }
  ): Promise<void> {
    await apiClient.put(`/providers/${providerId}/gallery/${imageId}`, data)
  }
}

export const galleryApi = new GalleryApi()
export default galleryApi
