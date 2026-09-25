/**
 * Gallery image interface
 */
export interface GalleryImage {
  id: string
  thumbnailUrl: string
  mediumUrl: string
  originalUrl: string
  displayOrder: number
  caption?: string
  altText?: string
  uploadedAt: Date
  isActive: boolean
  isPrimary?: boolean
}

/**
 * Upload progress tracking
 */
export interface UploadProgress {
  file: File
  progress: number
  status: 'pending' | 'uploading' | 'success' | 'error'
  error?: string
}

/**
 * Gallery image metadata update request
 */
export interface UpdateGalleryImageMetadataRequest {
  caption?: string
  altText?: string
}

/**
 * Reorder gallery images request
 * Maps image IDs to their new display order positions
 */
export interface ReorderGalleryImagesRequest {
  imageOrders: Record<string, number> // imageId -> displayOrder
  primaryImageId?: string // Optional: ID of image to set as primary
}
