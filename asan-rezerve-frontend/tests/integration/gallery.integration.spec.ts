/**
 * Integration Tests: Gallery Management (Priority 4)
 *
 * Tests the integration between Gallery components and ServiceCatalog API
 *
 * Prerequisites:
 * - ServiceCatalog API running on http://localhost:5010
 * - Valid JWT token for provider account
 * - Test provider ID available
 */

import { describe, it, expect, beforeAll } from 'vitest'
import { galleryService } from '@/modules/provider/services/gallery.service'

describe('Gallery Management Integration Tests', () => {
  let providerId: string
  let authToken: string
  let uploadedImageId: string

  beforeAll(async () => {
    // TODO: Replace with actual test credentials
    providerId = process.env.TEST_PROVIDER_ID || 'test-provider-id'
    authToken = process.env.TEST_AUTH_TOKEN || ''

    if (!authToken) {
      console.warn('⚠️  No auth token provided. Set TEST_AUTH_TOKEN environment variable.')
    }
  })

  describe('Upload Gallery Images', () => {
    it('should upload a single image successfully', async () => {
      // Create test image file
      const testImageBlob = new Blob(['test image content'], { type: 'image/jpeg' })
      const testFile = new File([testImageBlob], 'test-image.jpg', { type: 'image/jpeg' })

      const result = await galleryService.uploadImages(providerId, [testFile])

      expect(result).toHaveLength(1)
      expect(result[0]).toHaveProperty('id')
      expect(result[0]).toHaveProperty('imageUrl')
      expect(result[0]).toHaveProperty('providerId', providerId)

      uploadedImageId = result[0].id
    })

    it('should handle multiple image uploads', async () => {
      const testFiles = [
        new File([new Blob(['image 1'])], 'image1.jpg', { type: 'image/jpeg' }),
        new File([new Blob(['image 2'])], 'image2.jpg', { type: 'image/jpeg' }),
        new File([new Blob(['image 3'])], 'image3.jpg', { type: 'image/jpeg' }),
      ]

      const result = await galleryService.uploadImages(providerId, testFiles)

      expect(result).toHaveLength(3)
      result.forEach(image => {
        expect(image).toHaveProperty('id')
        expect(image).toHaveProperty('imageUrl')
      })
    })

    it('should reject files larger than 5MB', async () => {
      // Create 6MB file
      const largeBlob = new Blob([new ArrayBuffer(6 * 1024 * 1024)], { type: 'image/jpeg' })
      const largeFile = new File([largeBlob], 'large-image.jpg', { type: 'image/jpeg' })

      await expect(
        galleryService.uploadImages(providerId, [largeFile])
      ).rejects.toThrow()
    })

    it('should reject non-image files', async () => {
      const textFile = new File([new Blob(['text content'])], 'document.pdf', { type: 'application/pdf' })

      await expect(
        galleryService.uploadImages(providerId, [textFile])
      ).rejects.toThrow()
    })
  })

  describe('Get Gallery Images', () => {
    it('should retrieve all gallery images for a provider', async () => {
      const images = await galleryService.getGalleryImages(providerId)

      expect(Array.isArray(images)).toBe(true)
      if (images.length > 0) {
        expect(images[0]).toHaveProperty('id')
        expect(images[0]).toHaveProperty('imageUrl')
        expect(images[0]).toHaveProperty('caption')
        expect(images[0]).toHaveProperty('displayOrder')
      }
    })

    it('should return empty array for provider with no images', async () => {
      const emptyProviderId = 'provider-with-no-images'
      const images = await galleryService.getGalleryImages(emptyProviderId)

      expect(images).toEqual([])
    })
  })

  describe('Update Image Metadata', () => {
    it('should update image caption and alt text', async () => {
      if (!uploadedImageId) {
        console.warn('Skipping: No uploaded image available')
        return
      }

      await galleryService.updateImageMetadata(providerId, uploadedImageId, {
        caption: 'نمونه کار جدید',
        altText: 'New sample work',
      })

      const images = await galleryService.getGalleryImages(providerId)
      const updatedImage = images.find(img => img.id === uploadedImageId)

      expect(updatedImage).toBeDefined()
      expect(updatedImage?.caption).toBe('نمونه کار جدید')
      expect(updatedImage?.altText).toBe('New sample work')
    })
  })

  describe('Reorder Gallery Images', () => {
    it('should reorder images successfully', async () => {
      const images = await galleryService.getGalleryImages(providerId)

      if (images.length < 2) {
        console.warn('Skipping: Need at least 2 images to test reordering')
        return
      }

      // Reverse the order
      const reversedIds = images.map(img => img.id).reverse()

      await galleryService.reorderImages(providerId, {
        imageIds: reversedIds,
      })

      const reorderedImages = await galleryService.getGalleryImages(providerId)

      expect(reorderedImages[0].id).toBe(reversedIds[0])
      expect(reorderedImages[reorderedImages.length - 1].id).toBe(reversedIds[reversedIds.length - 1])
    })
  })

  describe('Set Primary Image', () => {
    it('should set an image as primary', async () => {
      if (!uploadedImageId) {
        console.warn('Skipping: No uploaded image available')
        return
      }

      await galleryService.setPrimaryImage(providerId, uploadedImageId)

      const images = await galleryService.getGalleryImages(providerId)
      const primaryImage = images.find(img => img.isPrimary)

      expect(primaryImage?.id).toBe(uploadedImageId)
    })

    it('should ensure only one primary image exists', async () => {
      const images = await galleryService.getGalleryImages(providerId)
      const primaryImages = images.filter(img => img.isPrimary)

      expect(primaryImages.length).toBeLessThanOrEqual(1)
    })
  })

  describe('Delete Gallery Image', () => {
    it('should delete an image successfully', async () => {
      if (!uploadedImageId) {
        console.warn('Skipping: No uploaded image available')
        return
      }

      await galleryService.deleteImage(providerId, uploadedImageId)

      const images = await galleryService.getGalleryImages(providerId)
      const deletedImage = images.find(img => img.id === uploadedImageId)

      expect(deletedImage).toBeUndefined()
    })

    it('should handle deleting non-existent image', async () => {
      const nonExistentId = 'non-existent-image-id'

      await expect(
        galleryService.deleteImage(providerId, nonExistentId)
      ).rejects.toThrow()
    })
  })

  describe('Error Handling', () => {
    it('should handle network errors gracefully', async () => {
      // Simulate network error by using invalid URL
      const invalidProviderId = ''

      await expect(
        galleryService.getGalleryImages(invalidProviderId)
      ).rejects.toThrow()
    })

    it('should handle unauthorized access', async () => {
      // TODO: Test without auth token
      // This requires modifying the service to accept optional tokens
    })
  })
})
