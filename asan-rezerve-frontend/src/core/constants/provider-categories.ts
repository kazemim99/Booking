/**
 * Provider Category Metadata
 * Matches backend ServiceCategoryExtensions metadata
 */

import { ProviderCategory } from '@/core/types/enums.types'

export interface CategoryMetadata {
  id: ProviderCategory
  persianName: string
  englishName: string
  icon: string
  colorHex: string
  gradient: string
  slug: string
  description: string
}

/**
 * Complete metadata for all provider categories
 * Synchronized with backend ServiceCategoryExtensions.cs
 */
export const CATEGORY_METADATA: Record<ProviderCategory, CategoryMetadata> = {
  [ProviderCategory.HairSalon]: {
    id: ProviderCategory.HairSalon,
    persianName: 'آرایشگاه زنانه',
    englishName: "Women's Hair Salon",
    icon: '💇‍♀️',
    colorHex: '#8B5CF6',
    gradient: 'linear-gradient(135deg, #8B5CF6 0%, #A78BFA 100%)',
    slug: 'hair-salon',
    description: 'خدمات آرایشگری زنانه شامل کوتاهی، رنگ، مش و...',
  },
  [ProviderCategory.Barbershop]: {
    id: ProviderCategory.Barbershop,
    persianName: 'آرایشگاه مردانه',
    englishName: "Men's Barbershop",
    icon: '💇‍♂️',
    colorHex: '#3B82F6',
    gradient: 'linear-gradient(135deg, #3B82F6 0%, #60A5FA 100%)',
    slug: 'barbershop',
    description: 'خدمات آرایشگری مردانه شامل اصلاح، کوتاهی و...',
  },
  [ProviderCategory.BeautySalon]: {
    id: ProviderCategory.BeautySalon,
    persianName: 'سالن زیبایی',
    englishName: 'Beauty Salon',
    icon: '💅',
    colorHex: '#EC4899',
    gradient: 'linear-gradient(135deg, #EC4899 0%, #F472B6 100%)',
    slug: 'beauty-salon',
    description: 'خدمات زیبایی شامل آرایش، پاکسازی پوست و...',
  },
  [ProviderCategory.NailSalon]: {
    id: ProviderCategory.NailSalon,
    persianName: 'آرایش ناخن',
    englishName: 'Nail Salon',
    icon: '💅',
    colorHex: '#F472B6',
    gradient: 'linear-gradient(135deg, #F472B6 0%, #FB923C 100%)',
    slug: 'nail-salon',
    description: 'خدمات ناخن شامل مانیکور، پدیکور، طراحی ناخن',
  },
  [ProviderCategory.Spa]: {
    id: ProviderCategory.Spa,
    persianName: 'اسپا',
    englishName: 'Spa & Wellness',
    icon: '🧖',
    colorHex: '#06B6D4',
    gradient: 'linear-gradient(135deg, #06B6D4 0%, #22D3EE 100%)',
    slug: 'spa',
    description: 'خدمات اسپا و آرامش بخشی',
  },
  [ProviderCategory.Massage]: {
    id: ProviderCategory.Massage,
    persianName: 'ماساژ',
    englishName: 'Massage Therapy',
    icon: '💆',
    colorHex: '#10B981',
    gradient: 'linear-gradient(135deg, #10B981 0%, #34D399 100%)',
    slug: 'massage',
    description: 'خدمات ماساژ و رفع خستگی',
  },
  [ProviderCategory.Gym]: {
    id: ProviderCategory.Gym,
    persianName: 'باشگاه ورزشی',
    englishName: 'Gym & Fitness',
    icon: '🏋️',
    colorHex: '#F59E0B',
    gradient: 'linear-gradient(135deg, #F59E0B 0%, #FBBF24 100%)',
    slug: 'gym',
    description: 'خدمات ورزشی و تناسب اندام',
  },
  [ProviderCategory.Yoga]: {
    id: ProviderCategory.Yoga,
    persianName: 'یوگا و مدیتیشن',
    englishName: 'Yoga & Meditation',
    icon: '🧘',
    colorHex: '#A855F7',
    gradient: 'linear-gradient(135deg, #A855F7 0%, #C084FC 100%)',
    slug: 'yoga',
    description: 'خدمات یوگا و مدیتیشن',
  },
  [ProviderCategory.MedicalClinic]: {
    id: ProviderCategory.MedicalClinic,
    persianName: 'کلینیک پزشکی',
    englishName: 'Medical Clinic',
    icon: '🏥',
    colorHex: '#EF4444',
    gradient: 'linear-gradient(135deg, #EF4444 0%, #F87171 100%)',
    slug: 'medical-clinic',
    description: 'خدمات پزشکی و درمانی',
  },
  [ProviderCategory.Dental]: {
    id: ProviderCategory.Dental,
    persianName: 'دندانپزشکی',
    englishName: 'Dental Clinic',
    icon: '🦷',
    colorHex: '#22D3EE',
    gradient: 'linear-gradient(135deg, #22D3EE 0%, #67E8F9 100%)',
    slug: 'dental',
    description: 'خدمات دندانپزشکی',
  },
  [ProviderCategory.Physiotherapy]: {
    id: ProviderCategory.Physiotherapy,
    persianName: 'فیزیوتراپی',
    englishName: 'Physiotherapy',
    icon: '💆‍♀️',
    colorHex: '#14B8A6',
    gradient: 'linear-gradient(135deg, #14B8A6 0%, #2DD4BF 100%)',
    slug: 'physiotherapy',
    description: 'خدمات فیزیوتراپی و توانبخشی',
  },
  [ProviderCategory.Tutoring]: {
    id: ProviderCategory.Tutoring,
    persianName: 'آموزش خصوصی',
    englishName: 'Private Tutoring',
    icon: '📚',
    colorHex: '#6366F1',
    gradient: 'linear-gradient(135deg, #6366F1 0%, #818CF8 100%)',
    slug: 'tutoring',
    description: 'خدمات آموزشی و تدریس خصوصی',
  },
  [ProviderCategory.Automotive]: {
    id: ProviderCategory.Automotive,
    persianName: 'تعمیرات خودرو',
    englishName: 'Auto Repair & Service',
    icon: '🚗',
    colorHex: '#64748B',
    gradient: 'linear-gradient(135deg, #64748B 0%, #94A3B8 100%)',
    slug: 'automotive',
    description: 'خدمات تعمیرات و نگهداری خودرو',
  },
  [ProviderCategory.HomeServices]: {
    id: ProviderCategory.HomeServices,
    persianName: 'خدمات منزل',
    englishName: 'Home Services',
    icon: '🏠',
    colorHex: '#84CC16',
    gradient: 'linear-gradient(135deg, #84CC16 0%, #A3E635 100%)',
    slug: 'home-services',
    description: 'خدمات منزل و تعمیرات',
  },
  [ProviderCategory.PetCare]: {
    id: ProviderCategory.PetCare,
    persianName: 'مراقبت حیوانات',
    englishName: 'Pet Care',
    icon: '🐾',
    colorHex: '#FBBF24',
    gradient: 'linear-gradient(135deg, #FBBF24 0%, #FCD34D 100%)',
    slug: 'pet-care',
    description: 'خدمات مراقبت و نگهداری حیوانات',
  },
}

/**
 * Helper functions for category metadata
 */

export const getCategoryMetadata = (category: ProviderCategory): CategoryMetadata => {
  return CATEGORY_METADATA[category]
}

export const getCategoryPersianName = (category: ProviderCategory): string => {
  return CATEGORY_METADATA[category]?.persianName ?? ''
}

export const getCategoryEnglishName = (category: ProviderCategory): string => {
  return CATEGORY_METADATA[category]?.englishName ?? ''
}

export const getCategoryIcon = (category: ProviderCategory): string => {
  return CATEGORY_METADATA[category]?.icon ?? '📋'
}

export const getCategoryColor = (category: ProviderCategory): string => {
  return CATEGORY_METADATA[category]?.colorHex ?? '#6B7280'
}

export const getCategoryGradient = (category: ProviderCategory): string => {
  return CATEGORY_METADATA[category]?.gradient ?? 'linear-gradient(135deg, #6B7280 0%, #9CA3AF 100%)'
}

export const getCategorySlug = (category: ProviderCategory): string => {
  return CATEGORY_METADATA[category]?.slug ?? ''
}

export const getCategoryDescription = (category: ProviderCategory): string => {
  return CATEGORY_METADATA[category]?.description ?? ''
}

/**
 * Parse slug to category
 */
export const parseCategorySlug = (slug: string): ProviderCategory | null => {
  const normalized = slug.toLowerCase().replace(/_/g, '-')

  const entry = Object.entries(CATEGORY_METADATA).find(
    ([_, metadata]) => metadata.slug === normalized
  )

  return entry ? (Number(entry[0]) as ProviderCategory) : null
}

/**
 * Every declared ProviderCategory, as numbers.
 * `Object.keys` on a numeric TS enum yields both the names and the values, so the
 * reverse-mapping keys are filtered out.
 */
const CATEGORY_VALUES: ProviderCategory[] = Object.keys(CATEGORY_METADATA).map(
  (key) => Number(key) as ProviderCategory
)

export const isProviderCategory = (value: unknown): value is ProviderCategory =>
  typeof value === 'number' && CATEGORY_VALUES.includes(value as ProviderCategory)

/**
 * Category ids the registration wizard used before it moved to canonical slugs, plus the
 * narrower taxonomy ids that fold into a broader category.
 *
 * Mirrors `ServiceCategoryResolver.LegacyAliases` and `ServiceCategoryExtensions.TryParseSlug`
 * on the backend. Saved drafts still carry these, so dropping them would leave a returning
 * registrant with no category selected.
 */
const LEGACY_CATEGORY_ALIASES: Record<string, ProviderCategory> = {
  barber: ProviderCategory.Barbershop,
  beauty: ProviderCategory.BeautySalon,
  beauty_spa: ProviderCategory.BeautySalon,
  nails: ProviderCategory.NailSalon,
  fitness: ProviderCategory.Gym,
  clinic: ProviderCategory.MedicalClinic,
  physio: ProviderCategory.Physiotherapy,
  education: ProviderCategory.Tutoring,
  auto: ProviderCategory.Automotive,
  pet: ProviderCategory.PetCare,
  brows_lashes: ProviderCategory.BeautySalon,
  braids_locs: ProviderCategory.HairSalon,
  aesthetic_medicine: ProviderCategory.MedicalClinic,
  dental_orthodontics: ProviderCategory.Dental,
  hair_removal: ProviderCategory.Spa,
  health_fitness: ProviderCategory.Gym,
  other: ProviderCategory.BeautySalon,
}

/**
 * Normalises whatever the API sent into a `ProviderCategory`.
 *
 * The backend stores the category as an int, but the host serialises every enum with
 * `JsonStringEnumConverter(JsonNamingPolicy.CamelCase)` — so `primaryCategory` arrives on the
 * wire as `"hairSalon"`, not `1`. Reading it as a number produced `undefined` metadata and a
 * blank category badge. This accepts all the shapes the category can legitimately take:
 *
 *  - the enum number (`1`) or its numeric string (`"1"`), as sent by clients that post ids
 *  - the enum member name in any casing (`"hairSalon"`, `"HairSalon"`), as sent by the API
 *  - the URL slug (`"hair-salon"`), as used in category routes
 *
 * Returns `null` for anything unrecognised, so callers decide the fallback rather than
 * silently landing on category 0, which is not a real category.
 */
export const parseCategory = (value: unknown): ProviderCategory | null => {
  if (value === null || value === undefined || value === '') return null

  if (typeof value === 'number') {
    return isProviderCategory(value) ? value : null
  }

  if (typeof value !== 'string') return null

  const trimmed = value.trim()
  if (trimmed === '') return null

  // Numeric string, e.g. "1"
  if (/^\d+$/.test(trimmed)) {
    const asNumber = Number(trimmed)
    return isProviderCategory(asNumber) ? asNumber : null
  }

  // Enum member name in any casing, e.g. "hairSalon" / "HairSalon"
  const byName = CATEGORY_VALUES.find(
    (category) => ProviderCategory[category]?.toLowerCase() === trimmed.toLowerCase()
  )
  if (byName !== undefined) return byName

  // Slug form, e.g. "hair-salon"
  const bySlug = parseCategorySlug(trimmed)
  if (bySlug !== null) return bySlug

  // Finally the aliases carried by older drafts, e.g. "barber"
  return LEGACY_CATEGORY_ALIASES[trimmed.toLowerCase()] ?? null
}

/**
 * Get all categories as array
 */
export const getAllCategories = (): CategoryMetadata[] => {
  return Object.values(CATEGORY_METADATA)
}

/**
 * Get categories grouped by domain
 */
export const getCategoriesByDomain = () => {
  return {
    beautyAndPersonalCare: [
      CATEGORY_METADATA[ProviderCategory.HairSalon],
      CATEGORY_METADATA[ProviderCategory.Barbershop],
      CATEGORY_METADATA[ProviderCategory.BeautySalon],
      CATEGORY_METADATA[ProviderCategory.NailSalon],
      CATEGORY_METADATA[ProviderCategory.Spa],
    ],
    healthAndWellness: [
      CATEGORY_METADATA[ProviderCategory.Massage],
      CATEGORY_METADATA[ProviderCategory.Gym],
      CATEGORY_METADATA[ProviderCategory.Yoga],
    ],
    medical: [
      CATEGORY_METADATA[ProviderCategory.MedicalClinic],
      CATEGORY_METADATA[ProviderCategory.Dental],
      CATEGORY_METADATA[ProviderCategory.Physiotherapy],
    ],
    professionalServices: [
      CATEGORY_METADATA[ProviderCategory.Tutoring],
      CATEGORY_METADATA[ProviderCategory.Automotive],
      CATEGORY_METADATA[ProviderCategory.HomeServices],
      CATEGORY_METADATA[ProviderCategory.PetCare],
    ],
  }
}
