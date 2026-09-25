import { describe, it, expect } from 'vitest'

import { ProviderCategory } from '@/core/types/enums.types'
import {
  CATEGORY_METADATA,
  getAllCategories,
  getCategoryIcon,
  getCategoryMetadata,
  getCategoryPersianName,
  getCategorySlug,
  isProviderCategory,
  parseCategory,
  parseCategorySlug,
} from '@/core/constants/provider-categories'

/**
 * The category metadata table mirrors the backend's ServiceCategoryExtensions, and the numeric
 * ids are the shared contract with `Providers.PrimaryCategory`. These tests pin both.
 *
 * `parseCategory` gets the most attention because it is the fix for a live defect: the API
 * serialises enums with `JsonStringEnumConverter(JsonNamingPolicy.CamelCase)`, so
 * `primaryCategory` arrives as `"hairSalon"` while the frontend types it as a number. Reading it
 * directly produced `undefined` metadata and a blank category badge.
 */
describe('provider category metadata', () => {
  const ALL: ProviderCategory[] = [
    ProviderCategory.HairSalon,
    ProviderCategory.Barbershop,
    ProviderCategory.BeautySalon,
    ProviderCategory.NailSalon,
    ProviderCategory.Spa,
    ProviderCategory.Massage,
    ProviderCategory.Gym,
    ProviderCategory.Yoga,
    ProviderCategory.MedicalClinic,
    ProviderCategory.Dental,
    ProviderCategory.Physiotherapy,
    ProviderCategory.Tutoring,
    ProviderCategory.Automotive,
    ProviderCategory.HomeServices,
    ProviderCategory.PetCare,
  ]

  it('covers all 15 categories', () => {
    expect(getAllCategories()).toHaveLength(15)
    ALL.forEach((category) => {
      expect(CATEGORY_METADATA[category]).toBeDefined()
    })
  })

  it('keeps the integer ids the backend persists', () => {
    // Renumbering these silently re-labels every provider row.
    expect(ProviderCategory.HairSalon).toBe(1)
    expect(ProviderCategory.Barbershop).toBe(2)
    expect(ProviderCategory.Spa).toBe(5)
    expect(ProviderCategory.MedicalClinic).toBe(9)
    expect(ProviderCategory.PetCare).toBe(15)
  })

  it('gives every category complete, non-empty metadata', () => {
    ALL.forEach((category) => {
      const meta = getCategoryMetadata(category)
      expect(meta.id).toBe(category)
      expect(meta.persianName).toBeTruthy()
      expect(meta.englishName).toBeTruthy()
      expect(meta.icon).toBeTruthy()
      expect(meta.slug).toBeTruthy()
      expect(meta.colorHex).toMatch(/^#[0-9A-Fa-f]{6}$/)
      expect(meta.gradient).toContain('linear-gradient(')
    })
  })

  it('gives every category a unique url slug', () => {
    const slugs = ALL.map(getCategorySlug)
    expect(new Set(slugs).size).toBe(slugs.length)
  })

  it('matches the backend metadata for spot-checked categories', () => {
    expect(getCategoryPersianName(ProviderCategory.HairSalon)).toBe('آرایشگاه زنانه')
    expect(getCategoryPersianName(ProviderCategory.Barbershop)).toBe('آرایشگاه مردانه')
    expect(getCategoryIcon(ProviderCategory.HairSalon)).toBe('💇‍♀️')
    expect(getCategorySlug(ProviderCategory.MedicalClinic)).toBe('medical-clinic')
  })
})

describe('isProviderCategory', () => {
  it('accepts declared categories', () => {
    expect(isProviderCategory(1)).toBe(true)
    expect(isProviderCategory(15)).toBe(true)
  })

  it('rejects the unset 0 and out-of-range numbers', () => {
    expect(isProviderCategory(0)).toBe(false)
    expect(isProviderCategory(16)).toBe(false)
    expect(isProviderCategory(-1)).toBe(false)
    expect(isProviderCategory('1')).toBe(false)
  })
})

describe('parseCategory', () => {
  it('accepts the camelCase enum names the API actually sends', () => {
    // This is the shape that broke the category badge.
    expect(parseCategory('hairSalon')).toBe(ProviderCategory.HairSalon)
    expect(parseCategory('barbershop')).toBe(ProviderCategory.Barbershop)
    expect(parseCategory('medicalClinic')).toBe(ProviderCategory.MedicalClinic)
  })

  it('accepts the PascalCase names the draft endpoints return', () => {
    // GetDraftProvider / GetRegistrationProgress return `PrimaryCategory.ToString()`.
    expect(parseCategory('HairSalon')).toBe(ProviderCategory.HairSalon)
    expect(parseCategory('PetCare')).toBe(ProviderCategory.PetCare)
  })

  it('accepts numeric ids and numeric strings', () => {
    expect(parseCategory(1)).toBe(ProviderCategory.HairSalon)
    expect(parseCategory('5')).toBe(ProviderCategory.Spa)
  })

  it('accepts url slugs', () => {
    expect(parseCategory('hair-salon')).toBe(ProviderCategory.HairSalon)
    expect(parseCategory('hair_salon')).toBe(ProviderCategory.HairSalon)
    expect(parseCategory('medical-clinic')).toBe(ProviderCategory.MedicalClinic)
  })

  it('accepts the legacy wizard ids that saved drafts still carry', () => {
    // Mirrors ServiceCategoryResolver on the backend; `barber` in particular is what the old
    // registration step emitted for a men's barbershop.
    expect(parseCategory('barber')).toBe(ProviderCategory.Barbershop)
    expect(parseCategory('beauty_spa')).toBe(ProviderCategory.BeautySalon)
    expect(parseCategory('health_fitness')).toBe(ProviderCategory.Gym)
    expect(parseCategory('dental_orthodontics')).toBe(ProviderCategory.Dental)
  })

  it('round-trips every category through each accepted shape', () => {
    ALL_CATEGORIES.forEach((category) => {
      const meta = getCategoryMetadata(category)
      expect(parseCategory(category)).toBe(category)
      expect(parseCategory(String(category))).toBe(category)
      expect(parseCategory(meta.slug)).toBe(category)
      expect(parseCategory(ProviderCategory[category])).toBe(category)
    })
  })

  it('returns null rather than guessing for unrecognised input', () => {
    // Returning null keeps the caller in charge of the fallback; silently returning 0 would
    // produce a category that has no metadata at all.
    expect(parseCategory(null)).toBeNull()
    expect(parseCategory(undefined)).toBeNull()
    expect(parseCategory('')).toBeNull()
    expect(parseCategory('   ')).toBeNull()
    expect(parseCategory('not-a-category')).toBeNull()
    expect(parseCategory('0')).toBeNull()
    expect(parseCategory(0)).toBeNull()
    expect(parseCategory(99)).toBeNull()
    expect(parseCategory({})).toBeNull()
  })
})

describe('parseCategorySlug', () => {
  it('normalises underscores to hyphens', () => {
    expect(parseCategorySlug('home_services')).toBe(ProviderCategory.HomeServices)
    expect(parseCategorySlug('medical_clinic')).toBe(ProviderCategory.MedicalClinic)
  })

  it('returns null for unknown slugs', () => {
    expect(parseCategorySlug('nope')).toBeNull()
  })
})

const ALL_CATEGORIES: ProviderCategory[] = getAllCategories().map((m) => m.id)
