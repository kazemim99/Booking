/**
 * URL Service - Centralized utility for handling API URLs and asset paths
 *
 * This service provides a single source of truth for:
 * - API base URLs (from environment variables)
 * - Converting relative paths to absolute URLs
 * - Building complete URLs for images and assets
 */

/**
 * Get the Service Catalog API base URL from environment (for API calls)
 */
export function getServiceCatalogApiUrl(): string {
  return import.meta.env.VITE_SERVICE_CATALOG_API_URL || 'http://localhost:5010/api'
}

/**
 * Get the Service Catalog base URL without /api (for static files like uploads)
 */
export function getServiceCatalogBaseUrl(): string {
  const apiUrl = import.meta.env.VITE_SERVICE_CATALOG_API_URL || 'http://localhost:5010/api'
  // Remove '/api' suffix if present to get the base server URL
  return apiUrl.replace(/\/api\/?$/, '')
}


/**
 * Get the User Management API base URL from environment
 */
export function getUserManagementApiUrl(): string {
  return import.meta.env.VITE_USER_MANAGEMENT_API_URL || 'https://localhost:5021/api'
}

/**
 * Convert a relative or absolute URL to a full absolute URL
 *
 * @param path - The path (can be relative like "/uploads/..." or absolute like "https://...")
 * @param useBaseUrl - Use base URL without /api (for static files like uploads)
 * @returns Full absolute URL
 *
 * @example
 * toAbsoluteUrl('/uploads/providers/profile.jpg', true)
 * // Returns: 'http://localhost:5010/uploads/providers/profile.jpg'
 *
 * @example
 * toAbsoluteUrl('https://cdn.example.com/image.jpg')
 * // Returns: 'https://cdn.example.com/image.jpg' (unchanged)
 */
export function toAbsoluteUrl(path: string | null | undefined, useBaseUrl = true): string | null {
  if (!path) return null

  // If it's already an absolute URL (starts with http:// or https://), return as-is
  if (path.startsWith('http://') || path.startsWith('https://')) {
    return path
  }

  // If it's a relative path, prepend the appropriate base URL
  if (path.startsWith('/')) {
    // For static files (uploads), use base URL without /api
    const baseUrl = useBaseUrl ? getServiceCatalogBaseUrl() : getServiceCatalogApiUrl()
    return `${baseUrl}${path}`
  }

  // If it's a relative path without leading slash, add one
  const baseUrl = useBaseUrl ? getServiceCatalogBaseUrl() : getServiceCatalogApiUrl()
  return `${baseUrl}/${path}`
}

/**
 * Build a provider image URL (profile image or logo)
 *
 * @param profileImageUrl - Provider's profile image URL
 * @param logoUrl - Provider's business logo URL (fallback)
 * @returns Absolute URL for the image, or null if neither is available
 *
 * @example
 * buildProviderImageUrl('/uploads/profile.jpg', '/uploads/logo.jpg')
 * // Returns: 'http://localhost:5010/uploads/profile.jpg'
 *
 * @example
 * buildProviderImageUrl(null, '/uploads/logo.jpg')
 * // Returns: 'http://localhost:5010/uploads/logo.jpg' (fallback to logo)
 */
export function buildProviderImageUrl(
  profileImageUrl?: string | null,
  logoUrl?: string | null
): string | null {
  // Prefer profile image, fallback to logo
  const imageUrl = profileImageUrl || logoUrl
  // Use base URL (without /api) for uploads
  return toAbsoluteUrl(imageUrl, true)
}

/**
 * Build a gallery image URL
 *
 * @param imageUrl - Gallery image URL
 * @returns Absolute URL for the image
 */
export function buildGalleryImageUrl(imageUrl: string | null | undefined): string | null {
  return toAbsoluteUrl(imageUrl)
}

/**
 * Build a staff member photo URL
 *
 * @param photoUrl - Staff member's photo URL
 * @returns Absolute URL for the photo
 */
export function buildStaffPhotoUrl(photoUrl: string | null | undefined): string | null {
  return toAbsoluteUrl(photoUrl)
}

/**
 * Build a service image URL
 *
 * @param imageUrl - Service image URL
 * @returns Absolute URL for the image
 */
export function buildServiceImageUrl(imageUrl: string | null | undefined): string | null {
  return toAbsoluteUrl(imageUrl)
}
