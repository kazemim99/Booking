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
 */
export interface ReorderGalleryImagesRequest {
  imageOrders: Array<{
    imageId: string
    newOrder: number
  }>
}
