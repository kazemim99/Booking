<template>
  <div class="profile-manager">
    <h2 class="page-title">پروفایل و تنظیمات</h2>

    <!-- Tab Navigation -->
    <div class="tabs-card">
      <div class="tabs-header">
        <button
          v-for="tab in tabs"
          :key="tab.id"
          @click="activeTab = tab.id"
          :class="['tab-button', { 'tab-active': activeTab === tab.id }]"
        >
          <component :is="tab.icon" class="tab-icon" />
          <span class="tab-label">{{ tab.label }}</span>
        </button>
      </div>
    </div>

    <!-- Tab Content -->
    <div class="content-card">
      <!-- Personal Tab -->
      <div v-if="activeTab === 'personal'" class="tab-content">
        <h3 class="section-title">اطلاعات شخصی</h3>
        <form @submit.prevent="savePersonalInfo" class="profile-form">
          <!-- Profile Image Upload -->
          <div class="form-group">
            <label class="form-label">تصویر پروفایل</label>
            <ImageUpload
              v-model="personalForm.profileImage"
              label="تصویر پروفایل"
              placeholder="تصویر پروفایل خود را انتخاب کنید"
              hint="فرمت‌های مجاز: JPG, PNG - حداکثر حجم: 5MB - ابعاد پیشنهادی: 400x400"
              :loading="uploadingProfileImage"
              @upload="handleProfileImageUpload"
            />
          </div>

          <div class="form-group">
            <label for="fullName" class="form-label">نام و نام خانوادگی</label>
            <input
              id="fullName"
              v-model="personalForm.fullName"
              type="text"
              class="form-input"
              placeholder="نام و نام خانوادگی خود را وارد کنید"
            />
          </div>

          <div class="form-row">
            <div class="form-group">
              <label for="email" class="form-label">ایمیل</label>
              <input
                id="email"
                v-model="personalForm.email"
                type="email"
                class="form-input"
                placeholder="example@email.com"
              />
            </div>

            <div class="form-group">
              <label for="phone" class="form-label">شماره موبایل</label>
              <input
                id="phone"
                v-model="personalForm.phone"
                type="text"
                class="form-input"
                disabled
              />
            </div>
          </div>

          <button type="submit" class="submit-btn" :disabled="savingProfile">
            {{ savingProfile ? 'در حال ذخیره...' : 'ذخیره تغییرات' }}
          </button>
        </form>
      </div>

      <!-- Business Tab -->
      <div v-if="activeTab === 'business'" class="tab-content">
        <h3 class="section-title">اطلاعات کسب‌وکار</h3>
        <form @submit.prevent="saveBusinessInfo" class="profile-form">
          <!-- Business Logo Upload -->
          <div class="form-group">
            <label class="form-label">لوگوی کسب‌وکار</label>
            <ImageUpload
              v-model="businessForm.logo"
              label="لوگوی کسب‌وکار"
              placeholder="لوگوی کسب‌وکار خود را انتخاب کنید"
              hint="فرمت‌های مجاز: JPG, PNG - حداکثر حجم: 5MB - ابعاد پیشنهادی: 400x400"
              :loading="uploadingBusinessLogo"
              @upload="handleBusinessLogoUpload"
            />
          </div>

          <div class="form-row">
            <div class="form-group">
              <label for="businessName" class="form-label">نام کسب‌وکار</label>
              <input
                id="businessName"
                v-model="businessForm.name"
                type="text"
                class="form-input"
                placeholder="نام کسب‌وکار را وارد کنید"
              />
            </div>

            <div class="form-group">
              <label for="category" class="form-label">دسته‌بندی</label>
              <input
                id="category"
                v-model="businessForm.category"
                type="text"
                class="form-input"
                disabled
              />
            </div>
          </div>

          <div class="form-group">
            <label for="description" class="form-label">توضیحات کسب‌وکار</label>
            <textarea
              id="description"
              v-model="businessForm.description"
              class="form-textarea"
              rows="4"
              placeholder="توضیحات کسب‌وکار خود را وارد کنید..."
            />
          </div>

          <button type="submit" class="submit-btn" :disabled="savingBusiness">
            {{ savingBusiness ? 'در حال ذخیره...' : 'ذخیره تغییرات' }}
          </button>
        </form>
      </div>

      <!-- Location Tab -->
      <div v-if="activeTab === 'location'" class="tab-content">
        <h3 class="section-title">موقعیت مکانی و آدرس</h3>
        <form @submit.prevent="saveLocation" class="profile-form">
          <div class="form-group">
            <label class="form-label">موقعیت روی نقشه</label>
            <p class="form-hint">روی نقشه کلیک کنید تا موقعیت کسب‌وکار خود را انتخاب کنید</p>
            <NeshanMapPicker
              v-model="locationForm.coordinates"
              :map-key="neshanMapKey"
              :service-key="neshanServiceKey"
              height="450px"
              @location-selected="handleLocationSelected"
            />
          </div>

          <!-- Province and City Selector -->
          <LocationSelector
            :province-id="locationForm.provinceId"
            :city-id="locationForm.cityId"
            @update:province-id="handleProvinceChange"
            @update:city-id="handleCityChange"
          />

          <div class="form-group">
            <label for="formattedAddress" class="form-label">آدرس <span class="required">*</span></label>
            <input
              id="formattedAddress"
              v-model="locationForm.formattedAddress"
              type="text"
              class="form-input"
              placeholder="آدرس از نقشه انتخاب شده"
              readonly
              required
            />
          </div>

          <button type="submit" class="submit-btn" :disabled="savingLocation">
            {{ savingLocation ? 'در حال ذخیره...' : 'ذخیره تغییرات' }}
          </button>
        </form>
      </div>

      <!-- Staff Tab -->
      <div v-if="activeTab === 'staff'" class="tab-content">
        <ProfileStaffSection />
      </div>

      <!-- Hours Tab - Complete Redesign -->
      <div v-if="activeTab === 'hours'" class="tab-content">
        <div class="hours-layout">
          <!-- Left Side: Hours Management -->
          <div class="hours-main">
            <div class="hours-header">
              <h3 class="section-title">ساعات کاری</h3>
              <div class="header-actions">
                <button type="button" class="action-btn-outline" @click="setStandardHours">
                  <svg class="btn-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect>
                    <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path>
                  </svg>
                  تنظیم استاندارد
                </button>
                <button type="button" class="action-btn-outline" @click="clearAllHours">
                  <svg class="btn-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <polyline points="3 6 5 6 21 6"></polyline>
                    <path
                      d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"
                    ></path>
                  </svg>
                  پاک کردن همه
                </button>
              </div>
            </div>

            <!-- Day Schedule Editor -->
            <DayScheduleEditor
              v-model="workingHours"
              :week-days="weekDays.map(d => d.persian)"
              start-time-label="ساعت شروع"
              end-time-label="ساعت پایان"
              break-time-label="استراحت"
              :show-break-time="true"
              :show-copy-button="true"
              copy-button-text="کپی"
              copy-button-label="کپی به همه روزها"
            />

            <!-- Save Button -->
            <button type="button" class="save-hours-btn" @click="saveHours">ذخیره تغییرات</button>

            <!-- Info Card -->
            <div class="hours-info-card">
              <div class="info-icon-wrapper">💡</div>
              <div class="info-content">
                <h4 class="info-title">راهنما</h4>
                <ul class="info-list">
                  <li>از تقویم برای تنظیم روزهای تعطیل و استثناها استفاده کنید</li>
                  <li>می‌توانید هر روز را جداگانه تنظیم کنید</li>
                  <li>با دکمه "کپی" تنظیمات یک روز را به همه روزها اعمال کنید</li>
                </ul>
              </div>
            </div>
          </div>

          <!-- Right Side: Calendar Widget -->
          <div class="calendar-widget">
            <div class="widget-header">
              <svg class="header-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect>
                <line x1="16" y1="2" x2="16" y2="6"></line>
                <line x1="8" y1="2" x2="8" y2="6"></line>
                <line x1="3" y1="10" x2="21" y2="10"></line>
              </svg>
              <h4>تقویم روزهای خاص</h4>
            </div>
            <div class="widget-body">
              <p class="widget-hint">برای تنظیم تعطیلات و روزهای استثنا روی تاریخ کلیک کنید</p>
              <PersianCalendar
                v-model="selectedDate"
                :custom-days="customDays"
                @day-click="handleDateSelect"
              />
              <div v-if="customDays.size > 0" class="exceptions-list">
                <h5 class="exceptions-title">روزهای تنظیم شده</h5>
                <div
                  v-for="[dateKey, data] in Array.from(customDays.entries()).slice(0, 3)"
                  :key="dateKey"
                  class="exception-item"
                >
                  <span class="exception-date">{{ formatPersianDateShort(dateKey) }}</span>
                  <span class="exception-type">{{ data.isClosed ? 'تعطیل' : 'ساعات ویژه' }}</span>
                </div>
                <button
                  v-if="customDays.size > 3"
                  type="button"
                  class="view-all-btn"
                  @click="$router.push({ name: 'ProviderBusinessHours' })"
                >
                  مشاهده همه ({{ convertEnglishToPersianNumbers(customDays.size.toString()) }})
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- Custom Day Modal -->
        <CustomDayModal
          :is-open="modalOpen"
          :selected-date="selectedDate"
          @close="modalOpen = false"
          @save="handleSaveCustomDay"
        />
      </div>

      <!-- Gallery Tab -->
      <div v-if="activeTab === 'gallery'" class="tab-content">
        <h3 class="section-title">گالری تصاویر</h3>
        <ProfileGallery />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, h, onMounted, watch } from 'vue'
import { useProviderStore } from '../../stores/provider.store'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import PersianCalendar from '@/shared/components/calendar/PersianCalendar.vue'
import ProfileGallery from '../ProfileGallery.vue'
import ProfileStaffSection from '../ProfileStaffSection.vue'
import DayScheduleEditor from '@/shared/components/schedule/DayScheduleEditor.vue'
import CustomDayModal from '../../../provider/views/hours/CustomDayModal.vue'
import ImageUpload from '@/shared/components/ui/ImageUpload.vue'
import NeshanMapPicker from '@/shared/components/map/NeshanMapPicker.vue'
import LocationSelector from '@/shared/components/forms/LocationSelector.vue'
import { convertEnglishToPersianNumbers } from '@/shared/utils/date/jalali.utils'
import { providerProfileService } from '../../services/provider-profile.service'
import { useLocations } from '@/shared/composables/useLocations'

interface CustomDayData {
  date: Date
  startTime: string
  endTime: string
  breakTime: string
  isClosed: boolean
  closedReason: string
}

// Simple icon components
const UserIcon = () =>
  h('svg', { class: 'w-4 h-4', fill: 'none', stroke: 'currentColor', viewBox: '0 0 24 24' }, [
    h('path', {
      'stroke-linecap': 'round',
      'stroke-linejoin': 'round',
      'stroke-width': '2',
      d: 'M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z',
    }),
  ])

const BuildingIcon = () =>
  h('svg', { class: 'w-4 h-4', fill: 'none', stroke: 'currentColor', viewBox: '0 0 24 24' }, [
    h('path', {
      'stroke-linecap': 'round',
      'stroke-linejoin': 'round',
      'stroke-width': '2',
      d: 'M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4',
    }),
  ])

const MapPinIcon = () =>
  h('svg', { class: 'w-4 h-4', fill: 'none', stroke: 'currentColor', viewBox: '0 0 24 24' }, [
    h('path', {
      'stroke-linecap': 'round',
      'stroke-linejoin': 'round',
      'stroke-width': '2',
      d: 'M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z',
    }),
    h('path', {
      'stroke-linecap': 'round',
      'stroke-linejoin': 'round',
      'stroke-width': '2',
      d: 'M15 11a3 3 0 11-6 0 3 3 0 016 0z',
    }),
  ])

const UsersIcon = () =>
  h('svg', { class: 'w-4 h-4', fill: 'none', stroke: 'currentColor', viewBox: '0 0 24 24' }, [
    h('path', {
      'stroke-linecap': 'round',
      'stroke-linejoin': 'round',
      'stroke-width': '2',
      d: 'M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z',
    }),
  ])

const ClockIcon = () =>
  h('svg', { class: 'w-4 h-4', fill: 'none', stroke: 'currentColor', viewBox: '0 0 24 24' }, [
    h('path', {
      'stroke-linecap': 'round',
      'stroke-linejoin': 'round',
      'stroke-width': '2',
      d: 'M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z',
    }),
  ])

const ImageIcon = () =>
  h('svg', { class: 'w-4 h-4', fill: 'none', stroke: 'currentColor', viewBox: '0 0 24 24' }, [
    h('path', {
      'stroke-linecap': 'round',
      'stroke-linejoin': 'round',
      'stroke-width': '2',
      d: 'M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z',
    }),
  ])

const providerStore = useProviderStore()
const authStore = useAuthStore()
const { provinces, loadProvinces, loadCitiesByProvinceId, getCitiesByProvinceId, getProvinceByName } = useLocations()

const activeTab = ref('personal')

const currentProvider = computed(() => providerStore.currentProvider)

// Loading states
const uploadingProfileImage = ref(false)
const uploadingBusinessLogo = ref(false)
const savingProfile = ref(false)
const savingBusiness = ref(false)
const savingLocation = ref(false)

// Neshan Map API keys (get from environment variables or store in config)
const neshanMapKey = import.meta.env.VITE_NESHAN_MAP_KEY || 'web.2e852811b59d4733b08e16ec56311593'
const neshanServiceKey =
  import.meta.env.VITE_NESHAN_SERVICE_KEY || 'service.51a008d95d86499fab3c8496273db32f'

// Personal form
const personalForm = ref({
  fullName: '',
  email: '',
  phone: '',
  profileImage: null as string | null,
  profileImageUrl: '' as string, // Permanent URL from backend
})

// Business form
const businessForm = ref({
  name: '',
  category: '',
  description: '',
  logo: null as string | null,
  logoUrl: '' as string, // Permanent URL from backend
})

// Location form
const locationForm = ref({
  formattedAddress: '',
  provinceId: null as number | null,
  cityId: null as number | null,
  coordinates: null as { lat: number; lng: number } | null,
})

// Working hours data
const weekDays = [
  { persian: 'شنبه', english: 'Saturday' },
  { persian: 'یکشنبه', english: 'Sunday' },
  { persian: 'دوشنبه', english: 'Monday' },
  { persian: 'سه‌شنبه', english: 'Tuesday' },
  { persian: 'چهارشنبه', english: 'Wednesday' },
  { persian: 'پنجشنبه', english: 'Thursday' },
  { persian: 'جمعه', english: 'Friday' },
]

const workingHours = ref([
  { isOpen: true, startTime: '10:00', endTime: '22:00', breakTime: '' },
  { isOpen: true, startTime: '10:00', endTime: '22:00', breakTime: '' },
  { isOpen: true, startTime: '10:00', endTime: '22:00', breakTime: '' },
  { isOpen: true, startTime: '10:00', endTime: '22:00', breakTime: '' },
  { isOpen: true, startTime: '10:00', endTime: '22:00', breakTime: '' },
  { isOpen: true, startTime: '10:00', endTime: '22:00', breakTime: '' },
  { isOpen: false, startTime: '', endTime: '', breakTime: '' },
])

// Calendar and exceptions
const selectedDate = ref<Date | null>(null)
const modalOpen = ref(false)
const customDays = ref<Map<string, CustomDayData>>(new Map())

const tabs = [
  { id: 'personal', label: 'پروفایل من', icon: UserIcon },
  { id: 'business', label: 'کسب‌وکار', icon: BuildingIcon },
  { id: 'location', label: 'موقعیت', icon: MapPinIcon },
  { id: 'staff', label: 'پرسنل', icon: UsersIcon },
  { id: 'hours', label: 'ساعات کاری', icon: ClockIcon },
  { id: 'gallery', label: 'گالری', icon: ImageIcon },
]

// Function to load location data
const loadLocationData = () => {
  if (currentProvider.value) {
    locationForm.value = {
      formattedAddress: currentProvider.value.address?.formattedAddress || '',
      provinceId: currentProvider.value.address?.provinceId || null,
      cityId: currentProvider.value.address?.cityId || null,
      coordinates:
        currentProvider.value.address?.latitude && currentProvider.value.address?.longitude
          ? {
              lat: currentProvider.value.address.latitude,
              lng: currentProvider.value.address.longitude,
            }
          : null,
    }
  }
}

// Watch for location tab activation to reload data
watch(activeTab, async (newTab) => {
  if (newTab === 'location') {
    try {
      // Fetch fresh provider data from backend
      await providerStore.loadCurrentProvider(true) // Force refresh
      loadLocationData()
    } catch (error) {
      console.error('Error loading location data:', error)
    }
  }
})

// Load data on mount
onMounted(() => {
  if (currentProvider.value) {
    // Load personal info
    personalForm.value = {
      fullName: currentProvider.value.profile?.businessName || '',
      email: currentProvider.value.contactInfo?.email || authStore.user?.email || '',
      phone: currentProvider.value.contactInfo?.phone || authStore.user?.phoneNumber || '',
      profileImage: currentProvider.value.profileImageUrl || null,
      profileImageUrl: currentProvider.value.profileImageUrl || '',
    }

    // Load business info
    businessForm.value = {
      name: currentProvider.value.profile?.businessName || '',
      category: 'آرایشگاه', // Placeholder
      description: currentProvider.value.profile?.description || '',
      logo: currentProvider.value.profile?.logoUrl || null,
      logoUrl: currentProvider.value.profile?.logoUrl || '',
    }

    // Load location
    loadLocationData()
  }
})

// Location handlers
const handleProvinceChange = async (value: number | null) => {
  locationForm.value.provinceId = value
  // Reset city when province changes
  locationForm.value.cityId = null

  // Center map to province location
  if (value) {
    const province = provinces.value.find(p => p.id === value)
    if (province) {
      await centerMapToLocation(province.name)
    }
  }
}

const handleCityChange = async (value: number | null) => {
  locationForm.value.cityId = value

  // Center map to city location
  if (value && locationForm.value.provinceId) {
    const cities = getCitiesByProvinceId(locationForm.value.provinceId)
    const city = cities.find(c => c.id === value)
    const province = provinces.value.find(p => p.id === locationForm.value.provinceId)

    if (city && province) {
      // Search for "city, province" for better accuracy
      await centerMapToLocation(`${city.name}, ${province.name}`)
    }
  }
}

// Helper function to center map using Neshan geocoding
const centerMapToLocation = async (locationName: string) => {
  try {
    const response = await fetch(
      `https://api.neshan.org/v1/search?term=${encodeURIComponent(locationName)}&lat=35.6892&lng=51.389`,
      {
        headers: {
          'Api-Key': neshanServiceKey,
        },
      }
    )

    if (!response.ok) {
      console.error('Geocoding failed:', response.statusText)
      return
    }

    const data = await response.json()

    if (data.items && data.items.length > 0) {
      const firstResult = data.items[0]
      const lat = firstResult.location.y
      const lng = firstResult.location.x

      // Update coordinates to trigger map centering
      locationForm.value.coordinates = { lat, lng }

      console.log('Map centered to:', locationName, { lat, lng })
    }
  } catch (error) {
    console.error('Error centering map:', error)
  }
}

// Image upload handlers
const handleProfileImageUpload = async (file: File) => {
  try {
    uploadingProfileImage.value = true
    console.log('Uploading profile image:', file.name)

    // Upload to backend
    const response = await providerProfileService.uploadProfileImage(file)

    // Store the permanent URL
    personalForm.value.profileImageUrl = response.imageUrl

    console.log('Profile image uploaded successfully:', response.imageUrl)
  } catch (error: any) {
    console.error('Error uploading profile image:', error)
    alert('خطا در آپلود تصویر پروفایل. لطفاً دوباره تلاش کنید.')

    // Clear the preview on error
    personalForm.value.profileImage = null
  } finally {
    uploadingProfileImage.value = false
  }
}

const handleBusinessLogoUpload = async (file: File) => {
  try {
    uploadingBusinessLogo.value = true
    console.log('Uploading business logo:', file.name)

    // Upload to backend
    const response = await providerProfileService.uploadBusinessLogo(file)

    // Store the permanent URL
    businessForm.value.logoUrl = response.imageUrl

    console.log('Business logo uploaded successfully:', response.imageUrl)
  } catch (error: any) {
    console.error('Error uploading business logo:', error)
    alert('خطا در آپلود لوگو. لطفاً دوباره تلاش کنید.')

    // Clear the preview on error
    businessForm.value.logo = null
  } finally {
    uploadingBusinessLogo.value = false
  }
}

// Form handlers
const savePersonalInfo = async () => {
  try {
    savingProfile.value = true
    console.log('Saving personal info:', personalForm.value)

    // Send to backend
    await providerProfileService.updateProfile({
      fullName: personalForm.value.fullName,
      email: personalForm.value.email,
      profileImageUrl: personalForm.value.profileImageUrl,
    })

    alert('اطلاعات شخصی با موفقیت ذخیره شد')

    // Optionally refresh provider data from store
    // await providerStore.fetchCurrentProvider()
  } catch (error: any) {
    console.error('Error saving personal info:', error)
    alert('خطا در ذخیره اطلاعات. لطفاً دوباره تلاش کنید.')
  } finally {
    savingProfile.value = false
  }
}

const saveBusinessInfo = async () => {
  try {
    savingBusiness.value = true
    console.log('Saving business info:', businessForm.value)

    // Send to backend
    await providerProfileService.updateBusinessInfo({
      businessName: businessForm.value.name,
      description: businessForm.value.description,
      logoUrl: businessForm.value.logoUrl,
    })

    alert('اطلاعات کسب‌وکار با موفقیت ذخیره شد')

    // Optionally refresh provider data from store
    // await providerStore.fetchCurrentProvider()
  } catch (error: any) {
    console.error('Error saving business info:', error)
    alert('خطا در ذخیره اطلاعات. لطفاً دوباره تلاش کنید.')
  } finally {
    savingBusiness.value = false
  }
}

const handleLocationSelected = async (data: {
  lat: number
  lng: number
  address?: string
  addressDetails?: {
    formattedAddress: string
    neighbourhood: string
    city: string
    state: string
    address: string
    route: string
    district: string
    village: string
    county: string
    postalCode: string
  } | null
}) => {
  console.log('Location selected:', data)
  locationForm.value.coordinates = { lat: data.lat, lng: data.lng }

  // Populate formatted address from map selection
  if (data.addressDetails) {
    const details = data.addressDetails
    // Use formattedAddress from geocoding response
    locationForm.value.formattedAddress = details.formattedAddress || ''

    // Auto-detect province and city from map coordinates
    if (details.state) {
      // Ensure provinces are loaded
      await loadProvinces()

      // Clean province name by removing "استان" prefix if present
      let provinceName = details.state.trim()
      if (provinceName.startsWith('استان ')) {
        provinceName = provinceName.replace('استان ', '')
      }

      // Find matching province by name
      let province = getProvinceByName(provinceName)

      // If not found, try with original name
      if (!province) {
        province = getProvinceByName(details.state)
      }

      if (province) {
        locationForm.value.provinceId = province.id

        // Load cities for this province
        await loadCitiesByProvinceId(province.id)

        // Find matching city by name
        if (details.city) {
          const cities = getCitiesByProvinceId(province.id)
          const city = cities.find(c => c.name === details.city)
          if (city) {
            locationForm.value.cityId = city.id
            console.log('Auto-detected location:', {
              province: province.name,
              city: city.name,
              provinceId: province.id,
              cityId: city.id
            })
          } else {
            console.warn('City not found:', details.city, 'in province:', province.name)
          }
        }
      } else {
        console.warn('Province not found:', details.state, 'cleaned:', provinceName)
      }
    }

    console.log('Address fields populated:', {
      formattedAddress: locationForm.value.formattedAddress,
      provinceId: locationForm.value.provinceId,
      cityId: locationForm.value.cityId
    })
  } else if (data.address) {
    // Fallback: If only address string is provided from search
    locationForm.value.formattedAddress = data.address
  }
}

const saveLocation = async () => {
  try {
    savingLocation.value = true
    console.log('Saving location:', locationForm.value)

    // Validate required fields
    if (
      !locationForm.value.formattedAddress ||
      !locationForm.value.provinceId ||
      !locationForm.value.cityId ||
      !locationForm.value.coordinates?.lat ||
      !locationForm.value.coordinates?.lng
    ) {
      alert('لطفاً تمام فیلدهای الزامی را پر کنید (استان، شهر، آدرس از نقشه و موقعیت روی نقشه)')
      return
    }

    // Check provider ID
    if (!currentProvider.value?.id) {
      alert('خطا: شناسه ارائه‌دهنده یافت نشد')
      return
    }

    // Call API to update location
    await providerProfileService.updateLocation(currentProvider.value.id, {
      formattedAddress: locationForm.value.formattedAddress,
      country: 'Iran',
      provinceId: locationForm.value.provinceId,
      cityId: locationForm.value.cityId,
      latitude: locationForm.value.coordinates.lat,
      longitude: locationForm.value.coordinates.lng,
    })

    // Refresh provider data from backend
    await providerStore.loadCurrentProvider(true)

    // Reload location form with fresh data
    loadLocationData()

    alert('موقعیت مکانی با موفقیت ذخیره شد')
  } catch (error) {
    console.error('Error saving location:', error)
    alert('خطا در ذخیره موقعیت مکانی. لطفاً دوباره تلاش کنید.')
  } finally {
    savingLocation.value = false
  }
}

// Hours management methods
const setStandardHours = () => {
  workingHours.value = workingHours.value.map(() => ({
    isOpen: true,
    startTime: '10:00',
    endTime: '22:00',
    breakTime: '',
  }))
}

const clearAllHours = () => {
  workingHours.value = workingHours.value.map(() => ({
    isOpen: false,
    startTime: '',
    endTime: '',
    breakTime: '',
  }))
}

const saveHours = () => {
  console.log('Saving hours:', workingHours.value)
  alert('ساعات کاری با موفقیت ذخیره شد')
}

// Calendar methods
const handleDateSelect = (date: Date) => {
  // PersianCalendar emits Date object directly
  selectedDate.value = date
  modalOpen.value = true
}

const handleSaveCustomDay = (data: CustomDayData) => {
  const dateKey = data.date.toISOString().split('T')[0]
  customDays.value.set(dateKey, data)
  modalOpen.value = false
}

const formatPersianDateShort = (dateKey: string) => {
  const date = new Date(dateKey)
  return `${convertEnglishToPersianNumbers(date.getDate().toString())}/${convertEnglishToPersianNumbers((date.getMonth() + 1).toString())}`
}
</script>

<style scoped>
/* Main Container - matching registration steps */
.profile-manager {
  max-width: 80rem;
  margin: 0 auto;
  padding: 0 1rem;
}

.page-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: #111827;
  margin: 0 0 2rem 0;
}

/* Tabs Navigation */
.tabs-card {
  background: #f3f4f6;
  border-radius: 0.75rem;
  padding: 0.25rem;
  margin-bottom: 1rem;
  display: inline-flex;
  width: fit-content;
  min-width: 100%;
}

.tabs-header {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  width: 100%;
  gap: 0;
}

@media (min-width: 768px) {
  .tabs-header {
    grid-template-columns: repeat(6, 1fr);
  }
}

.tab-button {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.375rem;
  padding: 0.5rem 0.75rem;
  font-size: 0.875rem;
  font-weight: 500;
  border: 1px solid transparent;
  border-radius: 0.5rem;
  background: transparent;
  cursor: pointer;
  transition: all 0.2s ease;
  color: #6b7280;
  white-space: nowrap;
  min-height: 2.25rem;

  &:hover:not(.tab-active) {
    color: #111827;
  }

  &.tab-active {
    background: white;
    color: #8b5cf6;
    border-color: #e5e7eb;
    box-shadow: 0 1px 2px rgba(0, 0, 0, 0.05);
  }

  .tab-icon {
    width: 1rem;
    height: 1rem;
    flex-shrink: 0;
  }

  .tab-label {
    display: none;
  }
}

@media (min-width: 640px) {
  .tab-button .tab-label {
    display: inline;
  }
}

/* Content Card - matching step-card */
.content-card {
  background: white;
  border-radius: 1rem;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  padding: 2rem;
  overflow: hidden;
}

@media (max-width: 640px) {
  .content-card {
    padding: 1.5rem;
  }
}

.tab-content {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.section-title {
  font-size: 1.125rem;
  font-weight: 600;
  margin: 0 0 0.5rem 0;
  color: #111827;
}

.section-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 0.75rem;
  margin-bottom: 1rem;
}

.action-btn {
  padding: 0.5rem 1rem;
  background: #8b5cf6;
  color: white;
  border: none;
  border-radius: 0.5rem;
  font-size: 0.875rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;
  white-space: nowrap;
  display: flex;
  align-items: center;
  gap: 0.375rem;

  &:hover {
    background: #7c3aed;
  }

  &:active {
    background: #6d28d9;
  }
}

.info-text {
  color: #6b7280;
  font-size: 0.875rem;
  line-height: 1.6;
}

/* Empty State */
.empty-state {
  text-align: center;
  color: #9ca3af;
  padding: 3rem 1.5rem;
  background: #fafafa;
  border-radius: 0.5rem;
  border: 1px dashed #e5e7eb;
  font-size: 0.875rem;
}

/* Form Styles - matching registration */
.profile-form {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.form-row {
  display: grid;
  grid-template-columns: 1fr;
  gap: 1rem;
}

@media (min-width: 768px) {
  .form-row {
    grid-template-columns: 1fr 1fr;
  }
}

.form-label {
  font-size: 0.875rem;
  font-weight: 500;
  color: #374151;
  display: block;
  margin: 0;
}

.form-hint {
  font-size: 0.8125rem;
  color: #6b7280;
  margin-top: 0.25rem;
  margin-bottom: 0.75rem;
}

.form-input {
  width: 100%;
  padding: 0.75rem 1rem;
  font-size: 1rem;
  border: 1px solid #d1d5db;
  border-radius: 0.5rem;
  background: white;
  transition: all 0.2s ease;

  &::placeholder {
    color: #9ca3af;
  }

  &:focus {
    outline: none;
    border-color: #8b5cf6;
    box-shadow: 0 0 0 3px rgba(139, 92, 246, 0.1);
  }

  &:disabled {
    background: #f3f4f6;
    color: #6b7280;
    cursor: not-allowed;
    opacity: 0.6;
  }
}

.form-textarea {
  width: 100%;
  padding: 0.75rem 1rem;
  font-size: 1rem;
  border: 1px solid #d1d5db;
  border-radius: 0.5rem;
  background: white;
  transition: all 0.2s ease;
  resize: vertical;
  min-height: 6rem;

  &::placeholder {
    color: #9ca3af;
  }

  &:focus {
    outline: none;
    border-color: #8b5cf6;
    box-shadow: 0 0 0 3px rgba(139, 92, 246, 0.1);
  }
}

.submit-btn {
  width: fit-content;
  padding: 0.75rem 1.5rem;
  background: #8b5cf6;
  color: white;
  border: none;
  border-radius: 0.5rem;
  font-size: 1rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;
  align-self: flex-start;

  &:hover {
    background: #7c3aed;
  }

  &:active {
    background: #6d28d9;
  }

  &.submit-btn-full {
    width: 100%;
  }
}

/* Map Placeholder - matching registration */
.map-placeholder {
  position: relative;
  height: 16rem;
  background: #f3f4f6;
  border-radius: 0.75rem;
  overflow: hidden;
  border: 1px solid #e5e7eb;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 0.75rem;
  padding: 3rem 1.5rem;
}

@media (max-width: 640px) {
  .map-placeholder {
    height: 12rem;
  }
}

.map-icon {
  width: 3rem;
  height: 3rem;
  color: #8b5cf6;
}

.map-text {
  margin: 0;
  font-size: 0.875rem;
  color: #6b7280;
  text-align: center;
}

.map-change-btn {
  padding: 0.5rem 1rem;
  background: white;
  color: #8b5cf6;
  border: 1px solid #8b5cf6;
  border-radius: 0.375rem;
  font-size: 0.875rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;

  &:hover {
    background: #8b5cf6;
    color: white;
  }
}

.btn-icon {
  width: 1rem;
  height: 1rem;
  flex-shrink: 0;
}

/* Hours Tab - Complete Redesign */
.hours-layout {
  display: grid;
  grid-template-columns: 1fr 400px;
  gap: 2rem;
  align-items: start;
}

@media (max-width: 1024px) {
  .hours-layout {
    grid-template-columns: 1fr;
  }
}

/* Left Side - Hours Main */
.hours-main {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.hours-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 0.5rem;
}

.header-actions {
  display: flex;
  gap: 0.75rem;
}

.action-btn-outline {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 1rem;
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: 0.5rem;
  font-size: 0.875rem;
  color: #374151;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    background: #f9fafb;
    border-color: #8b5cf6;
    color: #8b5cf6;
  }
}

/* Save Button */
.save-hours-btn {
  width: 100%;
  padding: 0.875rem 1.5rem;
  background: #8b5cf6;
  color: white;
  border: none;
  border-radius: 0.75rem;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    background: #7c3aed;
    transform: translateY(-1px);
    box-shadow: 0 4px 12px rgba(139, 92, 246, 0.3);
  }

  &:active {
    transform: translateY(0);
  }
}

/* Info Card */
.hours-info-card {
  display: flex;
  gap: 1rem;
  padding: 1.25rem;
  background: rgba(139, 92, 246, 0.05);
  border: 1px solid rgba(139, 92, 246, 0.2);
  border-radius: 0.75rem;
}

.info-icon-wrapper {
  font-size: 1.5rem;
  flex-shrink: 0;
}

.info-content {
  flex: 1;
}

.info-title {
  font-size: 0.9375rem;
  font-weight: 600;
  color: #1a1a1a;
  margin-bottom: 0.5rem;
}

.info-list {
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: 0.375rem;
}

.info-list li {
  font-size: 0.8125rem;
  color: #6b7280;
  position: relative;
  padding-right: 1rem;

  &:before {
    content: '•';
    position: absolute;
    right: 0;
    color: #8b5cf6;
    font-weight: bold;
  }
}

/* Right Side - Calendar Widget */
.calendar-widget {
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: 0.75rem;
  overflow: hidden;
  position: sticky;
  top: 1rem;
}

.widget-header {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 1rem 1.25rem;
  border-bottom: 1px solid #e5e7eb;
  background: #f9fafb;
}

.header-icon {
  width: 1.25rem;
  height: 1.25rem;
  stroke-width: 2;
  color: #8b5cf6;
}

.widget-header h4 {
  font-size: 1rem;
  font-weight: 600;
  color: #1a1a1a;
}

.widget-body {
  padding: 1.25rem;
}

.widget-hint {
  font-size: 0.8125rem;
  color: #6b7280;
  margin-bottom: 1rem;
  line-height: 1.5;
}

/* Exceptions List */
.exceptions-list {
  margin-top: 1rem;
  padding-top: 1rem;
  border-top: 1px solid #e5e7eb;
}

.exceptions-title {
  font-size: 0.875rem;
  font-weight: 600;
  color: #1a1a1a;
  margin-bottom: 0.75rem;
}

.exception-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0.625rem;
  background: #f9fafb;
  border-radius: 0.5rem;
  margin-bottom: 0.5rem;
}

.exception-date {
  font-size: 0.8125rem;
  font-weight: 500;
  color: #374151;
}

.exception-type {
  font-size: 0.75rem;
  color: #6b7280;
  background: white;
  padding: 0.25rem 0.625rem;
  border-radius: 999px;
  border: 1px solid #e5e7eb;
}

.view-all-btn {
  width: 100%;
  padding: 0.5rem;
  margin-top: 0.5rem;
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: 0.5rem;
  font-size: 0.8125rem;
  color: #8b5cf6;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    background: #f9fafb;
    border-color: #8b5cf6;
  }
}

.info-note {
  display: flex;
  align-items: flex-start;
  gap: 0.75rem;
  padding: 1rem;
  background: #eff6ff;
  border: 1px solid #bfdbfe;
  border-radius: 0.5rem;
  font-size: 0.875rem;
  color: #1e40af;
  line-height: 1.5;
}

.info-icon {
  width: 1.25rem;
  height: 1.25rem;
  flex-shrink: 0;
  color: #3b82f6;
  margin-top: 0.125rem;
}

/* Required field indicator */
.required {
  color: #ef4444;
  margin-right: 0.25rem;
}
</style>
