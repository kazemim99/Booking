/**
 * Image utility functions for handling image URLs from backend
 */

const SERVICE_CATEGORY_BASE_URL =
  import.meta.env.VITE_SERVICE_CATALOG_API_URL || 'http://localhost:5010/api'

/**
 * Converts a relative image path from the backend to a full URL
 * @param relativePath - The relative path returned from backend (e.g., "/uploads/providers/xxx/image.png")
 * @returns Full URL to access the image (e.g., "http://localhost:5010/uploads/providers/xxx/image.png")
 */
export function getImageUrl(relativePath: string | null | undefined): string | null {

  if (!relativePath) return null

  // If already a full URL (starts with http:// or https://), return as is
  if (relativePath.startsWith('http://') || relativePath.startsWith('https://')) {
    console.log(relativePath)
    return relativePath
  }

  // If it's a data URL (base64), return as is
  if (relativePath.startsWith('data:')) {
    console.log(relativePath)

    return relativePath
  }

  // Remove '/api' from base URL if present, since uploads are served from root
  const baseUrl = SERVICE_CATEGORY_BASE_URL.replace(/\/api$/, '')

  // Ensure path starts with /
  const path = relativePath.startsWith('/') ? relativePath : `/${relativePath}`
  console.log(path)

  return `${baseUrl}${path}`
}

/**
 * Gets the profile image URL for a provider
 * @param profileImageUrl - The profile image URL from provider data
 * @returns Full URL or null
 */
export function getProviderProfileImageUrl(
  profileImageUrl: string | null | undefined,
): string | null {
  return getImageUrl(profileImageUrl)
}

/**
 * Gets the business logo URL for a provider
 * @param logoUrl - The logo URL from provider data
 * @returns Full URL or null
 */
export function getProviderLogoUrl(logoUrl: string | null | undefined): string | null {
  return getImageUrl(logoUrl)
}

/**
 * Gets the gallery image URL
 * @param imageUrl - The image URL from gallery
 * @returns Full URL or null
 */
export function getGalleryImageUrl(imageUrl: string | null | undefined): string | null {
  return getImageUrl(imageUrl)
}
