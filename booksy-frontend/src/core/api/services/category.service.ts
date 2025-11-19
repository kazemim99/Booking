/**
 * Category Service
 * Handles service category data
 */

// Future: Will use httpClient for API calls when backend endpoints are ready
// import { httpClient } from '@/core/api/client/http-client'
// import type { ApiResponse } from '@/core/api/client/api-response'

// const API_VERSION = 'v1'
// const API_BASE = `/${API_VERSION}/services`

// ==================== Types ====================

/**
 * Service Category
 */
export interface ServiceCategory {
  name: string
  description?: string
  iconUrl?: string
  color: string
  slug: string
}

/**
 * Popular category with counts
 */
export interface PopularCategory {
  name: string
  slug: string
  icon: string
  gradient: string
  providerCount: number
}

// ==================== Category Service Class ====================

class CategoryService {
  // Cache for categories to reduce API calls
  private categoriesCache: ServiceCategory[] | null = null
  private popularCategoriesCache: PopularCategory[] | null = null

  // ============================================
  // Categories
  // ============================================

  /**
   * Get all service categories
   * This returns predefined categories from the backend
   */
  async getCategories(useCache = true): Promise<ServiceCategory[]> {
    try {
      // Return cached data if available
      if (useCache && this.categoriesCache) {
        console.log('[CategoryService] Returning cached categories')
        return this.categoriesCache
      }

      console.log('[CategoryService] Fetching categories from API')

      // For now, return hardcoded categories that match backend ServiceCategory enum
      // In the future, this could be an API endpoint
      const categories: ServiceCategory[] = [
        { name: 'آرایشگاه مو', slug: 'haircut', color: '#667eea', description: 'کوتاهی و مدل مو' },
        { name: 'رنگ مو', slug: 'hair_coloring', color: '#f093fb', description: 'رنگ و هایلایت مو' },
        { name: 'حالت دهی مو', slug: 'hair_styling', color: '#4facfe', description: 'فر و صافی مو' },
        { name: 'درمان مو', slug: 'hair_treatment', color: '#fa709a', description: 'درمان‌های تخصصی مو' },
        { name: 'آرایش', slug: 'makeup', color: '#ffecd2', description: 'آرایش صورت و چشم' },
        { name: 'پاکسازی پوست', slug: 'facial', color: '#a8edea', description: 'فیشیال و پاکسازی' },
        { name: 'مراقبت پوست', slug: 'skincare', color: '#fed6e3', description: 'مراقبت‌های پوستی' },
        { name: 'اپیلاسیون', slug: 'waxing', color: '#ffd89b', description: 'اپیلاسیون بدن' },
        { name: 'بند انداختن', slug: 'threading', color: '#f857a6', description: 'بند ابرو و صورت' },
        { name: 'مانیکور', slug: 'manicure', color: '#ec4899', description: 'مانیکور و ناخن دست' },
        { name: 'پدیکور', slug: 'pedicure', color: '#10b981', description: 'پدیکور و ناخن پا' },
        { name: 'طراحی ناخن', slug: 'nail_art', color: '#f59e0b', description: 'طراحی و هنر ناخن' },
        { name: 'ماساژ', slug: 'massage', color: '#3b82f6', description: 'ماساژ درمانی و آرامش‌بخش' },
        { name: 'اسپا', slug: 'spa', color: '#6366f1', description: 'خدمات اسپا' },
        { name: 'آروماتراپی', slug: 'aromatherapy', color: '#8b5cf6', description: 'درمان با رایحه' },
        { name: 'لیزر', slug: 'laser', color: '#ef4444', description: 'لیزر موهای زائد' },
        { name: 'بوتاکس', slug: 'botox', color: '#06b6d4', description: 'تزریق بوتاکس' },
        { name: 'فیلر', slug: 'filler', color: '#84cc16', description: 'تزریق فیلر' },
        { name: 'مزوتراپی', slug: 'mesotherapy', color: '#f43f5e', description: 'تزریق مزوتراپی' },
        { name: 'پی آر پی', slug: 'prp', color: '#14b8a6', description: 'درمان با پلاسمای غنی از پلاکت' },
        { name: 'مربی شخصی', slug: 'personal_training', color: '#eab308', description: 'تمرینات ورزشی شخصی' },
        { name: 'یوگا', slug: 'yoga', color: '#a855f7', description: 'کلاس یوگا' },
        { name: 'پیلاتس', slug: 'pilates', color: '#ec4899', description: 'کلاس پیلاتس' },
        { name: 'زومبا', slug: 'zumba', color: '#f97316', description: 'کلاس زومبا' },
        { name: 'مشاوره', slug: 'consultation', color: '#64748b', description: 'مشاوره تخصصی' },
      ]

      // Cache the results
      this.categoriesCache = categories

      return categories
    } catch (error) {
      console.error('[CategoryService] Error fetching categories:', error)
      throw this.handleError(error)
    }
  }

  /**
   * Get category by slug
   */
  async getCategoryBySlug(slug: string): Promise<ServiceCategory | null> {
    const categories = await this.getCategories()
    return categories.find(c => c.slug === slug) || null
  }

  /**
   * Search categories by name
   */
  async searchCategories(query: string): Promise<ServiceCategory[]> {
    const categories = await this.getCategories()
    const lowerQuery = query.toLowerCase()

    return categories.filter(category =>
      category.name.includes(query) ||
      category.slug.toLowerCase().includes(lowerQuery) ||
      category.description?.includes(query)
    )
  }

  /**
   * Get popular categories
   * This would ideally come from analytics, but for now we'll use predefined popular ones
   */
  async getPopularCategories(limit = 8): Promise<PopularCategory[]> {
    try {
      // Return cached data if available
      if (this.popularCategoriesCache) {
        console.log('[CategoryService] Returning cached popular categories')
        return this.popularCategoriesCache.slice(0, limit)
      }

      console.log('[CategoryService] Fetching popular categories')

      // For now, return hardcoded popular categories
      // In the future, this should come from analytics API
      const popularCategories: PopularCategory[] = [
        {
          name: 'آرایشگاه مو',
          slug: 'haircut',
          icon: '💇',
          gradient: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
          providerCount: 2500,
        },
        {
          name: 'ماساژ و اسپا',
          slug: 'massage',
          icon: '💆',
          gradient: 'linear-gradient(135deg, #f093fb 0%, #f5576c 100%)',
          providerCount: 1800,
        },
        {
          name: 'پاکسازی پوست',
          slug: 'facial',
          icon: '✨',
          gradient: 'linear-gradient(135deg, #4facfe 0%, #00f2fe 100%)',
          providerCount: 1200,
        },
        {
          name: 'مانیکور و پدیکور',
          slug: 'manicure',
          icon: '💅',
          gradient: 'linear-gradient(135deg, #fa709a 0%, #fee140 100%)',
          providerCount: 1500,
        },
        {
          name: 'آرایش',
          slug: 'makeup',
          icon: '💄',
          gradient: 'linear-gradient(135deg, #ffecd2 0%, #fcb69f 100%)',
          providerCount: 900,
        },
        {
          name: 'اپیلاسیون',
          slug: 'waxing',
          icon: '🌿',
          gradient: 'linear-gradient(135deg, #a8edea 0%, #fed6e3 100%)',
          providerCount: 800,
        },
        {
          name: 'آرایشگاه مردانه',
          slug: 'barbering',
          icon: '💈',
          gradient: 'linear-gradient(135deg, #ffd89b 0%, #19547b 100%)',
          providerCount: 1100,
        },
        {
          name: 'خالکوبی و پیرسینگ',
          slug: 'tattoo',
          icon: '🎨',
          gradient: 'linear-gradient(135deg, #f857a6 0%, #ff5858 100%)',
          providerCount: 600,
        },
      ]

      // Cache the results
      this.popularCategoriesCache = popularCategories

      return popularCategories.slice(0, limit)
    } catch (error) {
      console.error('[CategoryService] Error fetching popular categories:', error)
      throw this.handleError(error)
    }
  }

  // ============================================
  // Utility Methods
  // ============================================

  /**
   * Clear cache
   */
  clearCache(): void {
    this.categoriesCache = null
    this.popularCategoriesCache = null
    console.log('[CategoryService] Cache cleared')
  }

  // ============================================
  // Error Handling
  // ============================================

  /**
   * Centralized error handling
   */
  private handleError(error: unknown): Error {
    if (error instanceof Error) {
      return error
    }
    return new Error('خطا در بارگذاری اطلاعات دسته‌بندی')
  }
}

// Export singleton instance
export const categoryService = new CategoryService()
export default categoryService
