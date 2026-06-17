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
              <div class="phone-input-wrapper">
                <input
                  id="phone"
                  v-model="personalForm.phone"
                  type="text"
                  class="form-input"
                  dir="ltr"
                  placeholder="09123456789"
                  maxlength="11"
                  @input="handlePhoneInput"
                />
                <span v-if="isPhoneChanged" class="phone-changed-badge">
                  نیاز به تأیید
                </span>
              </div>
              <span v-if="phoneError" class="form-error">{{ phoneError }}</span>
              <span v-else-if="isPhoneChanged" class="form-hint">
                بعد از ذخیره، کد تأیید به شماره جدید ارسال می‌شود
              </span>
            </div>
          </div>

          <button type="submit" class="submit-btn" :disabled="savingProfile">
            {{ savingProfile ? 'در حال ذخیره...' : 'ذخیره تغییرات' }}
          </button>
        </form>
      </div>

      <!-- Phone Verification Modal -->
      <PhoneVerificationModal
        v-model="showPhoneVerification"
        :phone-number="pendingPhoneNumber"
        :user-id="authStore.user?.id || ''"
        @verified="handlePhoneVerified"
        @cancel="handlePhoneVerificationCancel"
      />

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

        <form @submit.prevent="saveLocation" class="location-form">
          <!-- City Search -->
          <div class="form-group">
            <label class="form-label-small required">شهر</label>
            <SearchableSelect
              v-model="locationForm.cityId"
              :options="allCityOptions"
              label=""
              placeholder="جستجوی شهر..."
              :required="true"
              @update:model-value="handleCityChange"
            />
            <span class="form-hint">برای یافتن شهر مورد نظر تایپ کنید</span>
          </div>

          <!-- Neshan Map Picker -->
          <div class="form-group">
            <label class="form-label-small">موقعیت روی نقشه</label>
            <p class="form-hint">روی نقشه کلیک کنید تا موقعیت دقیق کسب‌وکار خود را انتخاب کنید</p>
            <NeshanMapPicker
              v-model="locationForm.coordinates"
              :map-key="neshanMapKey"
              :service-key="neshanServiceKey"
              height="450px"
              @location-selected="handleLocationSelected"
            />
          </div>

          <!-- Address Field -->
          <div class="form-group">
            <label for="formattedAddress" class="form-label-small required">آدرس دقیق</label>
            <input
              id="formattedAddress"
              v-model="locationForm.formattedAddress"
              type="text"
              class="form-input"
              placeholder="مثال: خیابان ولیعصر، کوچه پنجم، پلاک ۱۲"
              required
            />
          </div>

          <!-- Postal Code -->
          <div class="form-group">
            <label for="postalCode" class="form-label-small">کد پستی (اختیاری)</label>
            <input
              id="postalCode"
              v-model="locationForm.postalCode"
              type="text"
              dir="ltr"
              class="form-input"
              placeholder="1234567890"
              maxlength="10"
            />
          </div>

          <button type="submit" class="submit-btn" :disabled="savingLocation">
            {{ savingLocation ? 'در حال ذخیره...' : 'ذخیره تغییرات' }}
          </button>
        </form>
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
              v-model="scheduleDays"
              :week-days="weekDays.map(d => d.persian)"
              start-time-label="ساعت شروع"
              end-time-label="ساعت پایان"
              :show-breaks="true"
              breaks-label="استراحت‌ها"
              break-start-label="شروع استراحت"
              break-end-label="پایان استراحت"
              add-break-text="افزودن استراحت"
              remove-break-label="حذف استراحت"
              no-breaks-text="استراحتی تعریف نشده است"
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

      <!-- Services Tab -->
      <div v-if="activeTab === 'services'" class="tab-content">
        <h3 class="section-title">خدمات</h3>

        <!-- Loading State -->
        <div v-if="loadingServices" class="loading-state">
          <div class="spinner"></div>
          <p>در حال بارگذاری خدمات...</p>
        </div>

        <!-- Service List -->
        <div v-else-if="services.length > 0" class="service-list">
          <template v-for="service in services" :key="service.id">
            <div class="service-item">
              <div class="service-info">
                <h4 class="service-name">{{ service.name }}</h4>
                <p class="service-details">
                  قیمت: {{ formatPrice(service.basePrice) }} تومان • مدت: {{ service.duration }} دقیقه
                </p>
              </div>
              <div class="service-actions">
                <button
                  type="button"
                  class="btn-icon"
                  @click="handleEditService(service)"
                  title="ویرایش"
                >
                  <svg class="icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      stroke-width="2"
                      d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"
                    />
                  </svg>
                </button>
                <button
                  type="button"
                  class="btn-icon btn-delete"
                  @click="handleDeleteService(service.id)"
                  title="حذف"
                >
                  <svg class="icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      stroke-width="2"
                      d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
                    />
                  </svg>
                </button>
              </div>
            </div>
          </template>
        </div>

        <!-- Add Service Button -->
        <button
          v-if="!loadingServices"
          type="button"
          class="btn-add-service"
          @click="handleAddNewService"
        >
          <svg class="icon-plus" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M12 4v16m8-8H4"
            />
          </svg>
          افزودن خدمت جدید
        </button>

        <!-- Error Message -->
        <div v-if="serviceError" class="error-message">
          <svg class="error-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
            />
          </svg>
          <div class="error-text">{{ serviceError }}</div>
        </div>
      </div>

      <!-- Gallery Tab -->
      <div v-if="activeTab === 'gallery'" class="tab-content">
        <h3 class="section-title">گالری تصاویر</h3>
        <ProfileGallery />
      </div>
    </div>

    <!-- Service Edit/Add Modal -->
    <Modal
      v-model="showServiceModal"
      :title="editingServiceId ? 'ویرایش خدمت' : 'افزودن خدمت جدید'"
      size="medium"
    >
      <div class="service-modal-form">
        <div class="form-group">
          <label for="modal-serviceName" class="form-label">نام خدمت</label>
          <input
            id="modal-serviceName"
            v-model="serviceFormData.name"
            type="text"
            class="form-input"
            placeholder="مثال: اصلاح مو"
          />
        </div>

        <div class="form-row">
          <div class="form-group">
            <label for="modal-price" class="form-label">قیمت (تومان)</label>
            <input
              id="modal-price"
              v-model="serviceFormData.price"
              type="number"
              dir="ltr"
              class="form-input"
              placeholder="100000"
            />
          </div>

          <div class="form-group">
            <label for="modal-duration" class="form-label">مدت زمان</label>
            <select
              id="modal-duration"
              v-model.number="serviceFormData.durationMinutes"
              class="form-input"
              :class="{ 'form-input-error': fieldErrors.duration }"
            >
              <option
                v-for="option in durationOptions"
                :key="option.value"
                :value="option.value"
              >
                {{ option.label }}
              </option>
            </select>
            <span v-if="fieldErrors.duration" class="field-error">
              {{ fieldErrors.duration.join(', ') }}
            </span>
          </div>
        </div>

        <!-- Field Errors Display -->
        <div v-if="serviceError" class="modal-error-message">
          <svg class="error-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
            />
          </svg>
          <div class="error-text">{{ serviceError }}</div>
        </div>
      </div>

      <template #footer>
        <button type="button" class="btn-modal-outline" @click="handleCancelServiceEdit" :disabled="savingService">
          لغو
        </button>
        <button type="button" class="btn-modal-primary" @click="handleSaveService" :disabled="savingService">
          {{ savingService ? 'در حال ذخیره...' : (editingServiceId ? 'ذخیره تغییرات' : 'افزودن خدمت') }}
        </button>
      </template>
    </Modal>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, h, onMounted, watch } from 'vue'
import { useProviderStore } from '../../stores/provider.store'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import PersianCalendar from '@/shared/components/calendar/PersianCalendar.vue'
import ProfileGallery from '../ProfileGallery.vue'
import ProfileStaffSection from '../ProfileStaffSection.vue'
import DayScheduleEditor, { type DayScheduleItem } from '@/shared/components/schedule/DayScheduleEditor.vue'
import CustomDayModal from '../../../provider/views/hours/CustomDayModal.vue'
import ImageUpload from '@/shared/components/ui/ImageUpload.vue'
import NeshanMapPicker from '@/shared/components/map/NeshanMapPicker.vue'
import SearchableSelect, { type SelectOption } from '@/shared/components/forms/SearchableSelect.vue'
import PhoneVerificationModal from './PhoneVerificationModal.vue'
import Modal from '@/shared/components/Modal.vue'
import { convertEnglishToPersianNumbers } from '@/shared/utils/date/jalali.utils'
import { providerProfileService } from '../../services/provider-profile.service'
import { useLocations } from '@/shared/composables/useLocations'
import type { DayHoursString } from '@/shared/types/business-hours.types'
import { PERSIAN_WEEKDAYS, PERSIAN_TO_BACKEND_DAY_MAP } from '@/shared/types/business-hours.types'
import { serviceService } from '../../services/service.service'
import type { Service } from '../../types/service.types'

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
const { provinces, loadProvinces, loadCitiesByProvinceId, getCitiesByProvinceId, getProvinceByName, getCityByName, getLocationById } = useLocations()

const activeTab = ref('personal')

const currentProvider = computed(() => providerStore.currentProvider)

// Get all cities from all provinces for searchable dropdown
const allCityOptions = computed<SelectOption[]>(() => {
  const allCities: SelectOption[] = []
  const provincesList = provinces.value

  provincesList.forEach(province => {
    const cities = getCitiesByProvinceId(province.id)
    cities.forEach(city => {
      allCities.push({
        label: `${city.name} (${province.name})`,
        value: city.id,
      })
    })
  })

  return allCities
})

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

// Phone verification state
const originalPhone = ref('')
const pendingPhoneNumber = ref('')
const showPhoneVerification = ref(false)
const phoneError = ref('')

const isPhoneChanged = computed(() => {
  return personalForm.value.phone !== originalPhone.value && personalForm.value.phone.length > 0
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
  city: '',
  provinceId: null as number | null,
  cityId: null as number | null,
  coordinates: null as { lat: number; lng: number } | null,
  postalCode: '',
})

// Working hours data - now using centralized types
const weekDays = [
  { persian: PERSIAN_WEEKDAYS[0], english: 'Saturday', backendDayOfWeek: PERSIAN_TO_BACKEND_DAY_MAP[0] },
  { persian: PERSIAN_WEEKDAYS[1], english: 'Sunday', backendDayOfWeek: PERSIAN_TO_BACKEND_DAY_MAP[1] },
  { persian: PERSIAN_WEEKDAYS[2], english: 'Monday', backendDayOfWeek: PERSIAN_TO_BACKEND_DAY_MAP[2] },
  { persian: PERSIAN_WEEKDAYS[3], english: 'Tuesday', backendDayOfWeek: PERSIAN_TO_BACKEND_DAY_MAP[3] },
  { persian: PERSIAN_WEEKDAYS[4], english: 'Wednesday', backendDayOfWeek: PERSIAN_TO_BACKEND_DAY_MAP[4] },
  { persian: PERSIAN_WEEKDAYS[5], english: 'Thursday', backendDayOfWeek: PERSIAN_TO_BACKEND_DAY_MAP[5] },
  { persian: PERSIAN_WEEKDAYS[6], english: 'Friday', backendDayOfWeek: PERSIAN_TO_BACKEND_DAY_MAP[6] },
]

// Initialize with default values - will be populated from backend on mount
const workingHours = ref<DayHoursString[]>([
  { isOpen: true, startTime: '10:00', endTime: '22:00', breaks: [] },
  { isOpen: true, startTime: '10:00', endTime: '22:00', breaks: [] },
  { isOpen: true, startTime: '10:00', endTime: '22:00', breaks: [] },
  { isOpen: true, startTime: '10:00', endTime: '22:00', breaks: [] },
  { isOpen: true, startTime: '10:00', endTime: '22:00', breaks: [] },
  { isOpen: true, startTime: '10:00', endTime: '22:00', breaks: [] },
  { isOpen: false, startTime: '', endTime: '', breaks: [] },
])

// Cast workingHours to DayScheduleItem[] for DayScheduleEditor component
const scheduleDays = computed<DayScheduleItem[]>({
  get: () => workingHours.value as unknown as DayScheduleItem[],
  set: (value) => {
    workingHours.value = value as unknown as DayHoursString[]
  }
})

// Calendar and exceptions
const selectedDate = ref<Date | null>(null)
const modalOpen = ref(false)
const customDays = ref<Map<string, CustomDayData>>(new Map())

// Services state
const services = ref<Service[]>([])
const showServiceModal = ref(false)
const editingServiceId = ref<string | null>(null)
const serviceFormData = ref({
  name: '',
  price: '',
  durationMinutes: 30, // Total duration in minutes (default 30 minutes)
})
const serviceError = ref('')
const fieldErrors = ref<Record<string, string[]>>({})
const loadingServices = ref(false)
const savingService = ref(false)

// Duration options in minutes (15-minute increments up to 8 hours)
const durationOptions = computed(() => {
  const options = []
  for (let i = 15; i <= 480; i += 15) {
    const hours = Math.floor(i / 60)
    const mins = i % 60
    let label = ''
    if (hours > 0 && mins > 0) {
      label = `${hours} ساعت و ${mins} دقیقه`
    } else if (hours > 0) {
      label = `${hours} ساعت`
    } else {
      label = `${mins} دقیقه`
    }
    options.push({ value: i, label })
  }
  return options
})

const BriefcaseIcon = () =>
  h('svg', { class: 'w-4 h-4', fill: 'none', stroke: 'currentColor', viewBox: '0 0 24 24' }, [
    h('path', {
      'stroke-linecap': 'round',
      'stroke-linejoin': 'round',
      'stroke-width': '2',
      d: 'M21 13.255A23.931 23.931 0 0112 15c-3.183 0-6.22-.62-9-1.745M16 6V4a2 2 0 00-2-2h-4a2 2 0 00-2 2v2m4 6h.01M5 20h14a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z',
    }),
  ])

const tabs = [
  { id: 'personal', label: 'پروفایل من', icon: UserIcon },
  { id: 'business', label: 'کسب‌وکار', icon: BuildingIcon },
  { id: 'location', label: 'موقعیت', icon: MapPinIcon },
  { id: 'services', label: 'خدمات', icon: BriefcaseIcon },
  { id: 'hours', label: 'ساعات کاری', icon: ClockIcon },
  { id: 'gallery', label: 'گالری', icon: ImageIcon },
]

// Function to load location data
const loadLocationData = async () => {
  // ✅ ALWAYS load provinces/cities when location tab opens (in 1 API call)
  await loadProvinces()

  if (currentProvider.value) {
    const address = currentProvider.value.address
    let provinceId = address?.provinceId || null
    let cityId = address?.cityId || null

    // If IDs are not available but names are, resolve IDs from names
    if (!provinceId && address?.state) {
      const province = getProvinceByName(address.state)
      if (province) {
        provinceId = province.id

        // Now try to resolve city if we have province and city name
        if (!cityId && address?.city) {
          const city = getCityByName(province.id, address.city)
          if (city) {
            cityId = city.id
          }
        }
      }
    }

    locationForm.value = {
      formattedAddress: address?.formattedAddress || '',
      city: address?.city || '',
      provinceId,
      cityId,
      coordinates:
        address?.latitude && address?.longitude
          ? {
              lat: address.latitude,
              lng: address.longitude,
            }
          : null,
      postalCode: address?.postalCode || '',
    }
  }
}

// Helper function to format time from hours and minutes
const formatTimeFromHoursMinutes = (hours?: number, minutes?: number): string => {
  if (hours === undefined || hours === null) return '10:00'
  const h = hours.toString().padStart(2, '0')
  const m = (minutes || 0).toString().padStart(2, '0')
  return `${h}:${m}`
}

// Function to load business hours data
const loadBusinessHoursData = () => {
  if (currentProvider.value?.businessHours && currentProvider.value.businessHours.length > 0) {
    console.log('📋 ProfileManager: Loading business hours from backend:', currentProvider.value.businessHours)

    // Create a map of backend dayOfWeek to business hours
    const hoursMap = new Map<number, any>()
    currentProvider.value.businessHours.forEach(bh => {
      hoursMap.set(bh.dayOfWeek, bh)
    })

    // Map to our working hours array (Persian week order)
    workingHours.value = weekDays.map((day) => {
      const backendHours = hoursMap.get(day.backendDayOfWeek)

      if (backendHours && backendHours.isOpen) {
        return {
          isOpen: true,
          startTime: formatTimeFromHoursMinutes(backendHours.openTimeHours, backendHours.openTimeMinutes),
          endTime: formatTimeFromHoursMinutes(backendHours.closeTimeHours, backendHours.closeTimeMinutes),
          breaks: backendHours.breaks?.map((b: any) => ({
            id: `break_${Date.now()}_${Math.random().toString(36).substring(2, 9)}`,
            start: formatTimeFromHoursMinutes(b.startTimeHours, b.startTimeMinutes),
            end: formatTimeFromHoursMinutes(b.endTimeHours, b.endTimeMinutes),
          })) || [],
        }
      } else {
        return {
          isOpen: false,
          startTime: '',
          endTime: '',
          breaks: [],
        }
      }
    })

    console.log('📋 ProfileManager: Business hours loaded successfully:', workingHours.value)
  } else {
    console.log('📋 ProfileManager: No business hours found in backend, using defaults')
  }
}

// Watch for tab changes to reload data as needed
watch(activeTab, async (newTab) => {
  console.log(`📋 ProfileManager: Tab changed to ${newTab}`)

  if (newTab === 'location') {
    try {
      // Fetch fresh provider data from backend
      await providerStore.loadCurrentProvider(true) // Force refresh
      await loadLocationData()
    } catch (error) {
      console.error('Error loading location data:', error)
    }
  } else if (newTab === 'hours') {
    try {
      // Load business hours from current provider (no need to force refresh)
      loadBusinessHoursData()
    } catch (error) {
      console.error('Error loading business hours data:', error)
    }
  } else if (newTab === 'services') {
    try {
      // Load services from API
      await loadServices()
    } catch (error) {
      console.error('Error loading services:', error)
    }
  } else if (newTab === 'gallery') {
    console.log('📋 ProfileManager: Gallery tab activated - GalleryManager will load images automatically')
    // No need to do anything - GalleryManager component loads images on mount and watches providerId
  }
})

// Load data on mount
onMounted(async () => {
  console.log('📋 ProfileManager: Loading provider information...')

  try {
    // ❌ DON'T load provinces here - only load when Location tab is opened!
    // This prevents unnecessary API calls on page load

    // Force refresh provider data from backend (similar to /progress API)
    await providerStore.loadCurrentProvider(true)
    console.log('📋 ProfileManager: Provider data loaded successfully')

    if (currentProvider.value) {
      console.log('📋 ProfileManager: Current provider:', currentProvider.value)

      // Load personal info
      const userPhone = currentProvider.value.contactInfo?.phone || authStore.user?.phoneNumber || ''
      personalForm.value = {
        fullName: currentProvider.value.profile?.businessName || '',
        email: currentProvider.value.contactInfo?.email || authStore.user?.email || '',
        phone: userPhone,
        profileImage: currentProvider.value.profileImageUrl || null,
        profileImageUrl: currentProvider.value.profileImageUrl || '',
      }

      // Store original phone for comparison
      originalPhone.value = userPhone

      // Load business info
      businessForm.value = {
        name: currentProvider.value.profile?.businessName || '',
        category: 'آرایشگاه', // Placeholder
        description: currentProvider.value.profile?.description || '',
        logo: currentProvider.value.profile?.logoUrl || null,
        logoUrl: currentProvider.value.profile?.logoUrl || '',
      }

      // Load location
      await loadLocationData()

      // Load business hours
      loadBusinessHoursData()

      console.log('📋 ProfileManager: Forms initialized with provider data')
    } else {
      console.warn('📋 ProfileManager: No provider data available')
    }
  } catch (error) {
    console.error('📋 ProfileManager: Error loading provider information:', error)
  }
})

// Handle city change - auto-set province when city is selected
const handleCityChange = async (cityId: string | number | null) => {
  if (typeof cityId === 'string') {
    locationForm.value.cityId = parseInt(cityId, 10)
  } else {
    locationForm.value.cityId = cityId
  }

  // Auto-set province when city is selected
  if (locationForm.value.cityId) {
    const city = getLocationById(locationForm.value.cityId)
    if (city?.parentId) {
      locationForm.value.provinceId = city.parentId
    }

    // Center map to city location
    const province = locationForm.value.provinceId ? getLocationById(locationForm.value.provinceId) : null
    if (city && province) {
      const searchTerm = `${city.name}, ${province.name}`
      await centerMapToLocation(searchTerm)
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

// Phone handlers
const handlePhoneInput = () => {
  // Clear error when user types
  phoneError.value = ''

  // Remove non-numeric characters
  personalForm.value.phone = personalForm.value.phone.replace(/[^0-9]/g, '')
}

const validatePhone = (phone: string): boolean => {
  // Iranian phone number validation: must start with 09 and be 11 digits
  const phoneRegex = /^09\d{9}$/
  return phoneRegex.test(phone)
}

const handlePhoneVerified = (verifiedPhone: string) => {
  console.log('Phone verified:', verifiedPhone)

  // Update original phone to the verified one
  originalPhone.value = verifiedPhone
  personalForm.value.phone = verifiedPhone

  // Show success message
  alert('شماره موبایل شما با موفقیت تأیید شد')

  // Refresh provider data
  providerStore.loadCurrentProvider(true)
}

const handlePhoneVerificationCancel = () => {
  console.log('Phone verification cancelled')

  // Revert phone to original
  personalForm.value.phone = originalPhone.value
  phoneError.value = ''
}

// Form handlers
const savePersonalInfo = async () => {
  try {
    savingProfile.value = true
    console.log('Saving personal info:', personalForm.value)

    // If phone changed, validate and show verification modal
    if (isPhoneChanged.value) {
      if (!validatePhone(personalForm.value.phone)) {
        phoneError.value = 'شماره موبایل باید با 09 شروع شود و 11 رقم باشد'
        savingProfile.value = false
        return
      }

      // Show verification modal
      pendingPhoneNumber.value = personalForm.value.phone
      showPhoneVerification.value = true
      savingProfile.value = false
      return
    }

    // Send to backend (without phone if unchanged)
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
    await loadLocationData()

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
    breaks: [],
  }))
}

const clearAllHours = () => {
  workingHours.value = workingHours.value.map(() => ({
    isOpen: false,
    startTime: '',
    endTime: '',
    breaks: [],
  }))
}

// Helper function to parse time string to hours and minutes
const parseTimeToHoursMinutes = (timeStr: string): { hours: number; minutes: number } => {
  const [hours, minutes] = timeStr.split(':').map(Number)
  return { hours: hours || 0, minutes: minutes || 0 }
}

const saveHours = async () => {
  try {
    console.log('💾 Saving hours:', workingHours.value)

    if (!currentProvider.value?.id) {
      alert('خطا: شناسه ارائه‌دهنده یافت نشد')
      return
    }

    // Transform working hours to backend format (all days, including closed ones)
    const businessHoursToSave = weekDays.map((day, index) => {
      const hours = workingHours.value[index]

      if (hours.isOpen) {
        const openTime = parseTimeToHoursMinutes(hours.startTime)
        const closeTime = parseTimeToHoursMinutes(hours.endTime)

        return {
          dayOfWeek: day.backendDayOfWeek,
          isOpen: true,
          openTime: {
            hours: openTime.hours,
            minutes: openTime.minutes,
          },
          closeTime: {
            hours: closeTime.hours,
            minutes: closeTime.minutes,
          },
          breaks: hours.breaks ? hours.breaks.map((b: any) => {
            const startTime = parseTimeToHoursMinutes(b.start)
            const endTime = parseTimeToHoursMinutes(b.end)
            return {
              start: {
                hours: startTime.hours,
                minutes: startTime.minutes,
              },
              end: {
                hours: endTime.hours,
                minutes: endTime.minutes,
              },
            }
          }) : [],
        }
      } else {
        return {
          dayOfWeek: day.backendDayOfWeek,
          isOpen: false,
          openTime: null,
          closeTime: null,
          breaks: [],
        }
      }
    })

    console.log('💾 Transformed hours for backend:', businessHoursToSave)

    // Call API to save business hours
    const response = await providerProfileService.updateBusinessHours(currentProvider.value.id, businessHoursToSave)
    console.log('💾 Save response:', response)

    alert('ساعات کاری با موفقیت ذخیره شد')

    // Optionally reload in the background (don't wait for it)
    providerStore.loadCurrentProvider(true).then(() => {
      loadBusinessHoursData()
      console.log('✅ Provider data refreshed after save')
    }).catch(err => {
      console.error('⚠️ Error refreshing provider data (non-critical):', err)
    })
  } catch (error) {
    console.error('Error saving hours:', error)
    alert('خطا در ذخیره ساعات کاری. لطفاً دوباره تلاش کنید.')
  }
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

// Service management methods
const formatPrice = (price: number) => {
  return new Intl.NumberFormat('fa-IR').format(price)
}

// Load services from API
const loadServices = async () => {
  if (!currentProvider.value?.id) {
    console.warn('No provider ID available')
    return
  }

  try {
    loadingServices.value = true
    serviceError.value = '' // Clear previous errors
    console.log('📋 ProfileManager: Loading services for provider:', currentProvider.value.id)
    const loadedServices = await serviceService.getServicesByProvider(currentProvider.value.id)
    services.value = loadedServices || []
    console.log('📋 ProfileManager: Services loaded:', services.value.length)
  } catch (error: any) {
    console.error('📋 ProfileManager: Error loading services:', error)

    const status = error?.response?.status
    const errorCode = error?.response?.data?.error?.code
    const message = error?.response?.data?.message || error?.message || ''

    // Only suppress 404 (endpoint not found) - show all other errors
    if (status === 404) {
      // No services endpoint - this is normal for new providers
      console.log('📋 ProfileManager: No services endpoint (expected for new providers)')
      services.value = []
      serviceError.value = '' // Clear error
    } else {
      // Real error - show to user with details
      console.error('📋 ProfileManager: Service loading error:', {
        status,
        errorCode,
        message,
        fullError: error
      })

      // Show specific error message if available, otherwise generic
      if (status === 500) {
        serviceError.value = 'خطای سرور در بارگذاری خدمات. لطفاً دوباره تلاش کنید.'
      } else if (status === 403) {
        serviceError.value = 'شما اجازه دسترسی به این خدمات را ندارید.'
      } else if (message) {
        serviceError.value = `خطا در بارگذاری خدمات: ${message}`
      } else {
        serviceError.value = 'خطا در بارگذاری خدمات. لطفاً دوباره تلاش کنید.'
      }
      services.value = []
    }
  } finally {
    loadingServices.value = false
  }
}

const handleAddNewService = () => {
  // Reset form for new service
  serviceFormData.value = {
    name: '',
    price: '',
    durationMinutes: 30,
  }
  editingServiceId.value = null
  serviceError.value = ''
  fieldErrors.value = {}
  showServiceModal.value = true
}

const handleEditService = (service: Service) => {
  serviceFormData.value = {
    name: service.name,
    price: service.basePrice.toString(),
    durationMinutes: service.duration, // Total duration in minutes
  }
  editingServiceId.value = service.id
  serviceError.value = ''
  fieldErrors.value = {}
  showServiceModal.value = true
}

const handleDeleteService = async (id: string) => {
  if (!currentProvider.value?.id) {
    serviceError.value = 'خطا: شناسه ارائه‌دهنده یافت نشد'
    return
  }

  if (!confirm('آیا از حذف این خدمت اطمینان دارید؟')) {
    return
  }

  try {
    console.log('📋 ProfileManager: Deleting service:', id)
    await serviceService.deleteService(id, currentProvider.value.id)

    // Remove from local state
    services.value = services.value.filter((s) => s.id !== id)
    console.log('📋 ProfileManager: Service deleted successfully')
  } catch (error) {
    console.error('📋 ProfileManager: Error deleting service:', error)
    serviceError.value = 'خطا در حذف خدمت'
  }
}

const handleSaveService = async () => {
  if (!serviceFormData.value.name || !serviceFormData.value.price) {
    serviceError.value = 'لطفاً تمام فیلدها را پر کنید'
    return
  }

  const totalMinutes = serviceFormData.value.durationMinutes

  if (!totalMinutes || totalMinutes === 0) {
    serviceError.value = 'لطفاً مدت زمان را مشخص کنید'
    return
  }

  if (!currentProvider.value?.id) {
    serviceError.value = 'خطا: شناسه ارائه‌دهنده یافت نشد'
    return
  }

  // Convert total minutes to hours and minutes
  const durationHours = Math.floor(totalMinutes / 60)
  const durationMinutes = totalMinutes % 60

  try {
    savingService.value = true
    serviceError.value = ''
    fieldErrors.value = {}

    if (editingServiceId.value) {
      // Edit existing service
      console.log('📋 ProfileManager: Updating service:', editingServiceId.value)
      console.log('📋 Duration breakdown:', { totalMinutes, durationHours, durationMinutes })

      const updatedService = await serviceService.updateService(editingServiceId.value, {
        providerId: currentProvider.value.id,
        serviceName: serviceFormData.value.name,
        price: parseFloat(serviceFormData.value.price),
        durationHours,
        durationMinutes,
        currency: 'IRR',
      })

      // Update in local state
      services.value = services.value.map((s) =>
        s.id === editingServiceId.value ? updatedService : s
      )
      console.log('📋 ProfileManager: Service updated successfully')
    } else {
      // Add new service
      console.log('📋 ProfileManager: Creating new service')
      console.log('📋 Duration breakdown:', { totalMinutes, durationHours, durationMinutes })

      const newService = await serviceService.createService({
        providerId: currentProvider.value.id,
        serviceName: serviceFormData.value.name,
        price: parseFloat(serviceFormData.value.price),
        durationHours,
        durationMinutes,
        currency: 'IRR',
      })

      // Add to local state
      services.value.push(newService)
      console.log('📋 ProfileManager: Service created successfully')
    }

    // Reset form and state
    serviceFormData.value = { name: '', price: '', durationMinutes: 30 }
    editingServiceId.value = null
    showServiceModal.value = false
  } catch (error: any) {
    console.error('📋 ProfileManager: Error saving service:', error)

    // Clear previous errors
    fieldErrors.value = {}
    serviceError.value = ''

    // Handle validation errors from backend
    if (error?.response?.data?.error?.errors) {
      const errors = error.response.data.error.errors

      // Store field-specific errors
      fieldErrors.value = errors

      // Also create a general error message
      const errorMessages = Object.entries(errors)
        .map(([field, messages]) => {
          const fieldNameMap: Record<string, string> = {
            duration: 'مدت زمان',
            price: 'قیمت',
            serviceName: 'نام خدمت',
            name: 'نام خدمت'
          }
          const fieldName = fieldNameMap[field] || field
          return `${fieldName}: ${(messages as string[]).join(', ')}`
        })
        .join('\n')
      serviceError.value = errorMessages
    } else if (error?.response?.data?.message) {
      serviceError.value = error.response.data.message
    } else if (error instanceof Error) {
      serviceError.value = error.message
    } else {
      serviceError.value = 'خطا در ذخیره خدمت'
    }
  } finally {
    savingService.value = false
  }
}

const handleCancelServiceEdit = () => {
  editingServiceId.value = null
  serviceFormData.value = { name: '', price: '', durationMinutes: 30 }
  serviceError.value = ''
  fieldErrors.value = {}
  showServiceModal.value = false
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
  color: var(--color-gray-900);
  margin: 0 0 2rem 0;
}

/* Tabs Navigation */
.tabs-card {
  background: var(--color-gray-100);
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
    grid-template-columns: repeat(7, 1fr);
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
  color: var(--color-gray-600);
  white-space: nowrap;
  min-height: 2.25rem;

  &:hover:not(.tab-active) {
    color: var(--color-gray-900);
  }

  &.tab-active {
    background: white;
    color: var(--color-primary-500);
    border-color: var(--color-gray-300);
    box-shadow: var(--shadow-sm);
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
  box-shadow: var(--shadow-sm);
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
  color: var(--color-gray-900);
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
  background: var(--color-primary-500);
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
    background: var(--color-primary-700);
  }

  &:active {
    background: #6d28d9;
  }
}

.info-text {
  color: var(--color-gray-600);
  font-size: 0.875rem;
  line-height: 1.6;
}

/* Empty State */
.empty-state {
  text-align: center;
  color: var(--color-gray-500);
  padding: 3rem 1.5rem;
  background: var(--color-gray-50);
  border-radius: 0.5rem;
  border: 1px dashed var(--color-gray-300);
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
  color: var(--color-gray-800);
  display: block;
  margin: 0;
}

.form-hint {
  font-size: 0.8125rem;
  color: var(--color-gray-600);
  margin-top: 0.25rem;
  margin-bottom: 0.75rem;
}

.form-error {
  font-size: 0.8125rem;
  color: var(--color-danger-500);
  margin-top: 0.25rem;
  display: block;
}

/* Phone Input Wrapper */
.phone-input-wrapper {
  position: relative;
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.phone-input-wrapper .form-input {
  flex: 1;
}

.phone-changed-badge {
  position: absolute;
  left: 0.75rem;
  top: 50%;
  transform: translateY(-50%);
  background: #fef3c7;
  color: #92400e;
  font-size: 0.75rem;
  font-weight: 600;
  padding: 0.25rem 0.5rem;
  border-radius: 0.375rem;
  white-space: nowrap;
  pointer-events: none;
}

.form-input {
  width: 100%;
  padding: 0.75rem 1rem;
  font-size: 1rem;
  border: 1px solid var(--color-gray-400);
  border-radius: 0.5rem;
  background: white;
  transition: all 0.2s ease;

  &::placeholder {
    color: var(--color-gray-500);
  }

  &:focus {
    outline: none;
    border-color: var(--color-primary-500);
    box-shadow: 0 0 0 3px rgba(139, 92, 246, 0.1);
  }

  &:disabled {
    background: var(--color-gray-100);
    color: var(--color-gray-600);
    cursor: not-allowed;
    opacity: 0.6;
  }
}

.form-textarea {
  width: 100%;
  padding: 0.75rem 1rem;
  font-size: 1rem;
  border: 1px solid var(--color-gray-400);
  border-radius: 0.5rem;
  background: white;
  transition: all 0.2s ease;
  resize: vertical;
  min-height: 6rem;

  &::placeholder {
    color: var(--color-gray-500);
  }

  &:focus {
    outline: none;
    border-color: var(--color-primary-500);
    box-shadow: 0 0 0 3px rgba(139, 92, 246, 0.1);
  }
}

.submit-btn {
  width: fit-content;
  padding: 0.75rem 1.5rem;
  background: var(--color-primary-500);
  color: white;
  border: none;
  border-radius: 0.5rem;
  font-size: 1rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;
  align-self: flex-start;

  &:hover {
    background: var(--color-primary-700);
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
  background: var(--color-gray-100);
  border-radius: 0.75rem;
  overflow: hidden;
  border: 1px solid var(--color-gray-300);
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
  color: var(--color-primary-500);
}

.map-text {
  margin: 0;
  font-size: 0.875rem;
  color: var(--color-gray-600);
  text-align: center;
}

.map-change-btn {
  padding: 0.5rem 1rem;
  background: white;
  color: var(--color-primary-500);
  border: 1px solid var(--color-primary-500);
  border-radius: 0.375rem;
  font-size: 0.875rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;

  &:hover {
    background: var(--color-primary-500);
    color: white;
  }
}

.tab-icon {
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
  border: 1px solid var(--color-gray-300);
  border-radius: 0.5rem;
  font-size: 0.875rem;
  color: var(--color-gray-800);
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    background: var(--color-gray-50);
    border-color: var(--color-primary-500);
    color: var(--color-primary-500);
  }
}

/* Save Button */
.save-hours-btn {
  width: 100%;
  padding: 0.875rem 1.5rem;
  background: var(--color-primary-500);
  color: white;
  border: none;
  border-radius: 0.75rem;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    background: var(--color-primary-700);
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
  color: var(--color-gray-600);
  position: relative;
  padding-right: 1rem;

  &:before {
    content: '•';
    position: absolute;
    right: 0;
    color: var(--color-primary-500);
    font-weight: bold;
  }
}

/* Right Side - Calendar Widget */
.calendar-widget {
  background: white;
  border: 1px solid var(--color-gray-300);
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
  border-bottom: 1px solid var(--color-gray-300);
  background: var(--color-gray-50);
}

.header-icon {
  width: 1.25rem;
  height: 1.25rem;
  stroke-width: 2;
  color: var(--color-primary-500);
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
  color: var(--color-gray-600);
  margin-bottom: 1rem;
  line-height: 1.5;
}

/* Exceptions List */
.exceptions-list {
  margin-top: 1rem;
  padding-top: 1rem;
  border-top: 1px solid var(--color-gray-300);
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
  background: var(--color-gray-50);
  border-radius: 0.5rem;
  margin-bottom: 0.5rem;
}

.exception-date {
  font-size: 0.8125rem;
  font-weight: 500;
  color: var(--color-gray-800);
}

.exception-type {
  font-size: 0.75rem;
  color: var(--color-gray-600);
  background: white;
  padding: 0.25rem 0.625rem;
  border-radius: 999px;
  border: 1px solid var(--color-gray-300);
}

.view-all-btn {
  width: 100%;
  padding: 0.5rem;
  margin-top: 0.5rem;
  background: white;
  border: 1px solid var(--color-gray-300);
  border-radius: 0.5rem;
  font-size: 0.8125rem;
  color: var(--color-primary-500);
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    background: var(--color-gray-50);
    border-color: var(--color-primary-500);
  }
}

.info-note {
  display: flex;
  align-items: flex-start;
  gap: 0.75rem;
  padding: 1rem;
  background: var(--color-primary-50);
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
  color: var(--color-primary-500);
  margin-top: 0.125rem;
}

/* Required field indicator */
.required {
  color: var(--color-danger-500);
  margin-right: 0.25rem;
}

/* Location Map Section with Search Overlay */
.location-map-section {
  margin-bottom: 1.5rem;
}

.map-container-with-search {
  position: relative;
  border-radius: 0.75rem;
  overflow: hidden;
  box-shadow: var(--shadow-sm);
}

.address-search-overlay {
  position: absolute;
  top: 1rem;
  left: 1rem;
  right: 1rem;
  z-index: 1000;
  max-width: 400px;
}

.search-input-wrapper {
  position: relative;
  display: flex;
  align-items: center;
  background: white;
  border-radius: 0.5rem;
  box-shadow: var(--shadow-md);
}

.search-icon {
  position: absolute;
  right: 1rem;
  width: 1.25rem;
  height: 1.25rem;
  color: var(--color-gray-500);
  pointer-events: none;
}

.address-search-input {
  width: 100%;
  padding: 0.75rem 1rem 0.75rem 3rem;
  border: none;
  border-radius: 0.5rem;
  font-size: 0.875rem;
  color: var(--color-gray-900);
  background: transparent;
  outline: none;
}

.address-search-input::placeholder {
  color: var(--color-gray-500);
}

.search-results-dropdown {
  margin-top: 0.5rem;
  background: white;
  border-radius: 0.5rem;
  box-shadow: var(--shadow-lg);
  max-height: 300px;
  overflow-y: auto;
}

.search-result-item {
  padding: 0.75rem 1rem;
  cursor: pointer;
  border-bottom: 1px solid var(--color-gray-100);
  transition: background-color 0.15s;
}

.search-result-item:last-child {
  border-bottom: none;
}

.search-result-item:hover {
  background-color: var(--color-gray-50);
}

.result-title {
  font-size: 0.875rem;
  font-weight: 500;
  color: var(--color-gray-900);
  margin-bottom: 0.25rem;
}

.result-address {
  font-size: 0.75rem;
  color: var(--color-gray-600);
}

/* Form labels matching BusinessLocationStep */
.form-label-small {
  display: block;
  font-size: 0.875rem;
  font-weight: 500;
  color: var(--color-gray-800);
  margin-bottom: 0.5rem;
}

/* Location form spacing */
.location-form {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

/* Services Tab Styles */
.service-list {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.service-item {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 1rem;
  background: rgba(139, 92, 246, 0.05);
  border: 1px solid var(--color-gray-300);
  border-radius: 0.5rem;
}

.service-info {
  flex: 1;
}

.service-name {
  font-size: 1rem;
  font-weight: 600;
  color: var(--color-gray-900);
  margin-bottom: 0.25rem;
}

.service-details {
  font-size: 0.875rem;
  color: var(--color-gray-600);
}

.service-actions {
  display: flex;
  gap: 0.5rem;
}

.btn-icon {
  padding: 0.5rem;
  background: none;
  border: none;
  border-radius: 0.375rem;
  cursor: pointer;
  transition: all 0.2s ease;
  display: flex;
  align-items: center;
  justify-content: center;
}

.btn-icon:hover {
  background: rgba(0, 0, 0, 0.05);
}

.btn-icon .icon {
  width: 1.25rem;
  height: 1.25rem;
  color: var(--color-gray-600);
}

.btn-delete .icon {
  color: var(--color-danger-500);
}

/* Service Form */
.service-form {
  padding: 1rem;
  background: rgba(139, 92, 246, 0.05);
  border: 1px solid rgba(139, 92, 246, 0.2);
  border-radius: 0.5rem;
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.form-actions {
  display: flex;
  gap: 0.5rem;
}

.form-actions > * {
  flex: 1;
}

.btn-action-primary {
  padding: 0.75rem 1.5rem;
  background: var(--color-primary-500);
  color: white;
  border: none;
  border-radius: 0.5rem;
  font-size: 1rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;
}

.btn-action-primary:hover:not(:disabled) {
  background: var(--color-primary-700);
}

.btn-action-primary:active:not(:disabled) {
  background: #6d28d9;
}

.btn-action-primary:disabled {
  background: var(--color-gray-400);
  cursor: not-allowed;
  opacity: 0.6;
}

.btn-action-outline {
  padding: 0.75rem 1.5rem;
  background: white;
  color: var(--color-gray-600);
  border: 1px solid var(--color-gray-400);
  border-radius: 0.5rem;
  font-size: 1rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;
}

.btn-action-outline:hover:not(:disabled) {
  background: var(--color-gray-50);
  border-color: var(--color-primary-500);
  color: var(--color-primary-500);
}

.btn-action-outline:disabled {
  background: var(--color-gray-100);
  cursor: not-allowed;
  opacity: 0.6;
}

/* Add Service Button */
.btn-add-service {
  width: 100%;
  padding: 0.875rem 1.5rem;
  border: 2px dashed var(--color-gray-400);
  background: white;
  border-radius: 0.5rem;
  color: var(--color-primary-500);
  font-size: 1rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
}

.btn-add-service:hover {
  background: rgba(139, 92, 246, 0.05);
  border-color: var(--color-primary-500);
}

.icon-plus {
  width: 1.25rem;
  height: 1.25rem;
}

/* Error Message */
.error-message {
  display: flex;
  align-items: flex-start;
  gap: 0.75rem;
  padding: 1rem;
  background: var(--color-danger-50);
  border: 1px solid #fecaca;
  border-radius: 0.5rem;
  margin-top: 0.5rem;
}

.error-icon {
  width: 1.25rem;
  height: 1.25rem;
  color: var(--color-danger-500);
  flex-shrink: 0;
  margin-top: 0.125rem;
}

.error-text {
  flex: 1;
  font-size: 0.875rem;
  color: var(--color-danger-600);
  line-height: 1.5;
  white-space: pre-line;
}

/* Loading State */
.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 3rem;
  gap: 1rem;
}

.loading-state p {
  color: var(--color-gray-600);
  font-size: 0.875rem;
}

.spinner {
  width: 2.5rem;
  height: 2.5rem;
  border: 3px solid var(--color-gray-300);
  border-top-color: var(--color-primary-500);
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

/* Select Dropdown Styling */
.form-input[type="number"],
select.form-input {
  cursor: pointer;
}

select.form-input {
  background-image: url("data:image/svg+xml,%3csvg xmlns='http://www.w3.org/2000/svg' fill='none' viewBox='0 0 20 20'%3e%3cpath stroke='%236b7280' stroke-linecap='round' stroke-linejoin='round' stroke-width='1.5' d='M6 8l4 4 4-4'/%3e%3c/svg%3e");
  background-position: left 0.5rem center;
  background-repeat: no-repeat;
  background-size: 1.5em 1.5em;
  padding-left: 2.5rem;
  -webkit-appearance: none;
  -moz-appearance: none;
  appearance: none;
}

select.form-input:focus {
  background-image: url("data:image/svg+xml,%3csvg xmlns='http://www.w3.org/2000/svg' fill='none' viewBox='0 0 20 20'%3e%3cpath stroke='%238b5cf6' stroke-linecap='round' stroke-linejoin='round' stroke-width='1.5' d='M6 8l4 4 4-4'/%3e%3c/svg%3e");
}

/* Field Error Styling */
.form-input-error {
  border-color: var(--color-danger-500) !important;
  background-color: var(--color-danger-50);
}

.form-input-error:focus {
  border-color: var(--color-danger-600) !important;
  box-shadow: 0 0 0 3px rgba(239, 68, 68, 0.1) !important;
}

.field-error {
  display: block;
  font-size: 0.8125rem;
  color: var(--color-danger-600);
  margin-top: 0.375rem;
  font-weight: 500;
}

/* Service Modal Styles */
.service-modal-form {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.modal-error-message {
  display: flex;
  align-items: flex-start;
  gap: 0.75rem;
  padding: 1rem;
  background: var(--color-danger-50);
  border: 1px solid #fecaca;
  border-radius: 0.5rem;
  margin-top: 0.5rem;
}

.btn-modal-primary {
  padding: 0.625rem 1.5rem;
  background: var(--color-primary-500);
  color: white;
  border: none;
  border-radius: 0.5rem;
  font-size: 0.875rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-modal-primary:hover:not(:disabled) {
  background: var(--color-primary-700);
}

.btn-modal-primary:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.btn-modal-outline {
  padding: 0.625rem 1.5rem;
  background: white;
  color: var(--color-gray-600);
  border: 1px solid var(--color-gray-400);
  border-radius: 0.5rem;
  font-size: 0.875rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-modal-outline:hover:not(:disabled) {
  background: var(--color-gray-50);
  border-color: var(--color-gray-500);
}

.btn-modal-outline:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
</style>
