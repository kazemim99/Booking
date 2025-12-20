<template>
  <div class="staff-profile-manager">
    <h2 class="page-title">پروفایل من</h2>

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
        <div class="info-notice">
          <svg class="icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          <p>شما می‌توانید اطلاعات شخصی خود را ویرایش کنید.</p>
        </div>

        <div class="profile-form">
          <!-- Profile Image -->
          <div class="form-group">
            <label class="form-label">تصویر پروفایل</label>
            <div class="image-upload-placeholder">
              <div v-if="provider?.photoUrl" class="current-image">
                <img :src="provider.photoUrl" alt="Profile" />
              </div>
              <div v-else class="no-image">
                <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
                </svg>
              </div>
              <button class="upload-btn" disabled>آپلود تصویر (به زودی)</button>
            </div>
          </div>

          <!-- Name Fields -->
          <div class="form-row">
            <div class="form-group">
              <label class="form-label">نام</label>
              <input
                v-model="personalForm.firstName"
                type="text"
                class="form-input"
                placeholder="نام"
                disabled
              />
            </div>
            <div class="form-group">
              <label class="form-label">نام خانوادگی</label>
              <input
                v-model="personalForm.lastName"
                type="text"
                class="form-input"
                placeholder="نام خانوادگی"
                disabled
              />
            </div>
          </div>

          <!-- Contact Info -->
          <div class="form-row">
            <div class="form-group">
              <label class="form-label">ایمیل</label>
              <input
                v-model="personalForm.email"
                type="email"
                class="form-input"
                placeholder="example@email.com"
                disabled
              />
            </div>
            <div class="form-group">
              <label class="form-label">شماره موبایل</label>
              <input
                v-model="personalForm.phoneNumber"
                type="text"
                class="form-input"
                dir="ltr"
                disabled
              />
            </div>
          </div>

          <!-- Bio -->
          <div class="form-group">
            <label class="form-label">درباره من</label>
            <textarea
              v-model="personalForm.bio"
              class="form-textarea"
              rows="4"
              placeholder="توضیحاتی درباره خود بنویسید..."
              disabled
            />
          </div>

          <div class="coming-soon-notice">
            ✨ قابلیت ویرایش به زودی فعال می‌شود
          </div>
        </div>
      </div>

      <!-- Business Tab (Read-only - shows organization info) -->
      <div v-if="activeTab === 'business'" class="tab-content">
        <h3 class="section-title">اطلاعات کسب‌وکار سازمان</h3>
        <div class="info-notice warning">
          <svg class="icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
          </svg>
          <p>این اطلاعات فقط قابل مشاهده است و توسط مدیر سازمان قابل ویرایش است.</p>
        </div>

        <div class="profile-form" v-if="parentOrganization">
          <!-- Business Logo -->
          <div class="form-group">
            <label class="form-label">لوگوی سازمان</label>
            <div class="image-display">
              <div v-if="parentOrganization.logoUrl" class="current-image">
                <img :src="parentOrganization.logoUrl" :alt="parentOrganization.businessName" />
              </div>
              <div v-else class="no-image">
                <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
                </svg>
              </div>
            </div>
          </div>

          <!-- Business Name -->
          <div class="form-group">
            <label class="form-label">نام سازمان</label>
            <input
              :value="parentOrganization.businessName"
              type="text"
              class="form-input"
              disabled
              readonly
            />
          </div>

          <!-- Business Type -->
          <div class="form-group" v-if="parentOrganization.businessType">
            <label class="form-label">نوع کسب‌وکار</label>
            <input
              :value="getBusinessTypeLabel(parentOrganization.businessType)"
              type="text"
              class="form-input"
              disabled
              readonly
            />
          </div>
        </div>

        <div v-else class="placeholder-content">
          <div class="placeholder-icon">
            <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
            </svg>
          </div>
          <h3>اطلاعات سازمان</h3>
          <p>در حال بارگذاری اطلاعات سازمان...</p>
        </div>
      </div>

      <!-- Location Tab (Read-only - shows organization location) -->
      <div v-if="activeTab === 'location'" class="tab-content">
        <h3 class="section-title">موقعیت مکانی سازمان</h3>
        <div class="info-notice warning">
          <svg class="icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
          </svg>
          <p>موقعیت مکانی سازمان {{ organizationName }}. این اطلاعات فقط قابل مشاهده است.</p>
        </div>

        <div class="profile-form" v-if="parentOrganization">
          <!-- City and State -->
          <div class="form-row">
            <div class="form-group" v-if="parentOrganization.city">
              <label class="form-label">شهر</label>
              <input
                :value="parentOrganization.city"
                type="text"
                class="form-input"
                disabled
                readonly
              />
            </div>
            <div class="form-group" v-if="parentOrganization.state">
              <label class="form-label">استان</label>
              <input
                :value="parentOrganization.state"
                type="text"
                class="form-input"
                disabled
                readonly
              />
            </div>
          </div>

          <!-- Map placeholder -->
          <div class="form-group">
            <label class="form-label">نقشه</label>
            <div class="map-placeholder">
              <svg class="map-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
              </svg>
              <p>نمایش نقشه به زودی فعال می‌شود</p>
            </div>
          </div>

          <div class="help-text">
            💡 برای دیدن اطلاعات کامل سازمان، به بخش "سازمان من" مراجعه کنید.
          </div>
        </div>

        <div v-else class="placeholder-content">
          <div class="placeholder-icon">
            <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
            </svg>
          </div>
          <h3>موقعیت مکانی</h3>
          <p>در حال بارگذاری اطلاعات موقعیت...</p>
        </div>
      </div>

      <!-- Hours Tab (Staff working hours) -->
      <div v-if="activeTab === 'hours'" class="tab-content">
        <h3 class="section-title">ساعات کاری من</h3>
        <div class="info-notice">
          <svg class="icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          <p>ساعات کاری شما در سازمان.</p>
        </div>

        <div class="placeholder-content">
          <div class="placeholder-icon">
            <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
          </div>
          <h3>مدیریت ساعات کاری</h3>
          <p>ساعات کاری شما توسط مدیر سازمان تنظیم می‌شود.</p>
          <p class="text-muted">برای تغییر، با مدیر سازمان هماهنگ کنید.</p>
        </div>
      </div>

      <!-- Gallery Tab (Personal portfolio) -->
      <div v-if="activeTab === 'gallery'" class="tab-content">
        <h3 class="section-title">گالری نمونه کارها</h3>
        <div class="info-notice">
          <svg class="icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          <p>تصاویر نمونه کارهای شخصی شما.</p>
        </div>

        <div class="placeholder-content">
          <div class="placeholder-icon">
            <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
            </svg>
          </div>
          <h3>گالری شخصی</h3>
          <p>نمونه کارهای خود را آپلود کنید تا مشتریان ببینند.</p>
          <div class="coming-soon-notice">
            ✨ به زودی فعال می‌شود
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, h } from 'vue'
import { useHierarchyStore } from '../../stores/hierarchy.store'

const hierarchyStore = useHierarchyStore()

// Icon components
const UserIcon = () => h('svg', { class: 'w-5 h-5', fill: 'none', stroke: 'currentColor', viewBox: '0 0 24 24' }, [
  h('path', { 'stroke-linecap': 'round', 'stroke-linejoin': 'round', 'stroke-width': '2', d: 'M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z' })
])

const BuildingIcon = () => h('svg', { class: 'w-5 h-5', fill: 'none', stroke: 'currentColor', viewBox: '0 0 24 24' }, [
  h('path', { 'stroke-linecap': 'round', 'stroke-linejoin': 'round', 'stroke-width': '2', d: 'M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4' })
])

const MapPinIcon = () => h('svg', { class: 'w-5 h-5', fill: 'none', stroke: 'currentColor', viewBox: '0 0 24 24' }, [
  h('path', { 'stroke-linecap': 'round', 'stroke-linejoin': 'round', 'stroke-width': '2', d: 'M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z' }),
  h('path', { 'stroke-linecap': 'round', 'stroke-linejoin': 'round', 'stroke-width': '2', d: 'M15 11a3 3 0 11-6 0 3 3 0 016 0z' })
])

const ClockIcon = () => h('svg', { class: 'w-5 h-5', fill: 'none', stroke: 'currentColor', viewBox: '0 0 24 24' }, [
  h('path', { 'stroke-linecap': 'round', 'stroke-linejoin': 'round', 'stroke-width': '2', d: 'M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z' })
])

const ImageIcon = () => h('svg', { class: 'w-5 h-5', fill: 'none', stroke: 'currentColor', viewBox: '0 0 24 24' }, [
  h('path', { 'stroke-linecap': 'round', 'stroke-linejoin': 'round', 'stroke-width': '2', d: 'M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z' })
])

const tabs = [
  { id: 'personal', label: 'پروفایل من', icon: UserIcon },
  { id: 'business', label: 'خدمات', icon: BuildingIcon },
  { id: 'location', label: 'موقعیت', icon: MapPinIcon },
  { id: 'hours', label: 'ساعات کاری', icon: ClockIcon },
  { id: 'gallery', label: 'گالری', icon: ImageIcon },
]

const activeTab = ref('personal')

const provider = computed(() => hierarchyStore.currentHierarchy?.provider)
const parentOrganization = computed(() => hierarchyStore.currentHierarchy?.parentOrganization)
const organizationName = computed(() => hierarchyStore.currentHierarchy?.parentOrganization?.businessName)

const personalForm = ref({
  firstName: '',
  lastName: '',
  email: '',
  phoneNumber: '',
  bio: '',
})

const getBusinessTypeLabel = (type: string) => {
  const labels: Record<string, string> = {
    'Salon': 'سالن زیبایی',
    'Barbershop': 'آرایشگاه',
    'SpaWellness': 'اسپا و سلامتی',
    'Clinic': 'کلینیک',
    'BeautySalon': 'سالن زیبایی',
    'Other': 'سایر'
  }
  return labels[type] || type
}

onMounted(() => {
  // Populate form with provider data
  if (provider.value) {
    personalForm.value = {
      firstName: (provider.value as any).firstName || '',
      lastName: (provider.value as any).lastName || '',
      email: (provider.value as any).email || '',
      phoneNumber: (provider.value as any).phoneNumber || '',
      bio: (provider.value as any).bio || '',
    }
  }
})
</script>

<style scoped lang="scss">
.staff-profile-manager {
  max-width: 1200px;
  margin: 0 auto;
}

.page-title {
  font-size: 28px;
  font-weight: 700;
  color: #1f2937;
  margin: 0 0 24px 0;
}

/* Tabs Card */
.tabs-card {
  background: white;
  border-radius: 12px;
  border: 1px solid #e5e7eb;
  margin-bottom: 24px;
  overflow: hidden;
}

.tabs-header {
  display: flex;
  border-bottom: 2px solid #e5e7eb;
  overflow-x: auto;
  scrollbar-width: none;

  &::-webkit-scrollbar {
    display: none;
  }
}

.tab-button {
  flex: 1;
  min-width: 120px;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  padding: 16px 20px;
  border: none;
  background: transparent;
  cursor: pointer;
  transition: all 0.2s;
  color: #6b7280;
  font-size: 15px;
  font-weight: 500;
  position: relative;

  &:hover {
    background: #f9fafb;
    color: #374151;
  }

  &.tab-active {
    color: #6366f1;
    background: #f0f1ff;

    &::after {
      content: '';
      position: absolute;
      bottom: -2px;
      left: 0;
      right: 0;
      height: 2px;
      background: #6366f1;
    }
  }

  .tab-icon {
    width: 20px;
    height: 20px;
  }
}

/* Content Card */
.content-card {
  background: white;
  border-radius: 12px;
  border: 1px solid #e5e7eb;
  padding: 32px;
}

.tab-content {
  animation: fadeIn 0.3s ease-in;
}

@keyframes fadeIn {
  from {
    opacity: 0;
    transform: translateY(10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.section-title {
  font-size: 20px;
  font-weight: 600;
  color: #1f2937;
  margin: 0 0 20px 0;
  padding-bottom: 12px;
  border-bottom: 1px solid #e5e7eb;
}

/* Info Notice */
.info-notice {
  display: flex;
  gap: 12px;
  background: #eff6ff;
  border: 1px solid #bfdbfe;
  border-radius: 8px;
  padding: 12px 16px;
  margin-bottom: 24px;

  .icon {
    width: 20px;
    height: 20px;
    color: #1e40af;
    flex-shrink: 0;
    margin-top: 2px;
  }

  p {
    font-size: 14px;
    color: #1e40af;
    margin: 0;
  }

  &.warning {
    background: #fef3c7;
    border-color: #fde68a;

    .icon {
      color: #92400e;
    }

    p {
      color: #92400e;
    }
  }
}

/* Forms */
.profile-form {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.form-row {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 20px;
}

.form-label {
  font-size: 14px;
  font-weight: 500;
  color: #374151;
}

.form-input,
.form-textarea {
  width: 100%;
  padding: 12px 16px;
  border: 1px solid #d1d5db;
  border-radius: 8px;
  font-size: 15px;
  color: #1f2937;
  transition: all 0.2s;

  &:focus {
    outline: none;
    border-color: #6366f1;
    box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.1);
  }

  &:disabled {
    background: #f9fafb;
    color: #9ca3af;
    cursor: not-allowed;
  }
}

.form-textarea {
  resize: vertical;
  min-height: 100px;
}

/* Image Upload */
.image-upload-placeholder {
  display: flex;
  flex-direction: column;
  gap: 12px;

  .current-image,
  .no-image {
    width: 120px;
    height: 120px;
    border-radius: 12px;
    overflow: hidden;
    border: 2px solid #e5e7eb;
    display: flex;
    align-items: center;
    justify-content: center;
    background: #f9fafb;

    img {
      width: 100%;
      height: 100%;
      object-fit: cover;
    }

    svg {
      width: 48px;
      height: 48px;
      color: #d1d5db;
    }
  }

  .upload-btn {
    padding: 10px 20px;
    background: #6366f1;
    color: white;
    border: none;
    border-radius: 8px;
    font-size: 14px;
    font-weight: 500;
    cursor: pointer;
    align-self: flex-start;
    transition: all 0.2s;

    &:hover:not(:disabled) {
      background: #4f46e5;
    }

    &:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }
  }
}

/* Placeholder Content */
.placeholder-content {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 64px 32px;
  text-align: center;

  .placeholder-icon {
    width: 80px;
    height: 80px;
    margin-bottom: 24px;
    color: #d1d5db;

    svg {
      width: 100%;
      height: 100%;
    }
  }

  h3 {
    font-size: 20px;
    font-weight: 600;
    color: #374151;
    margin: 0 0 12px 0;
  }

  p {
    font-size: 16px;
    color: #6b7280;
    margin: 0 0 8px 0;

    &:last-of-type {
      margin-bottom: 0;
    }

    &.text-muted {
      font-size: 14px;
      color: #9ca3af;
    }
  }
}

/* Coming Soon Notice */
.coming-soon-notice {
  display: inline-block;
  padding: 8px 16px;
  background: #fef3c7;
  color: #92400e;
  border-radius: 8px;
  font-size: 14px;
  font-weight: 500;
  margin-top: 16px;
}

/* Image Display (read-only) */
.image-display {
  .current-image,
  .no-image {
    width: 120px;
    height: 120px;
    border-radius: 12px;
    overflow: hidden;
    border: 2px solid #e5e7eb;
    display: flex;
    align-items: center;
    justify-content: center;
    background: #f9fafb;

    img {
      width: 100%;
      height: 100%;
      object-fit: cover;
    }

    svg {
      width: 48px;
      height: 48px;
      color: #d1d5db;
    }
  }
}

/* Map Placeholder */
.map-placeholder {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 48px;
  background: #f9fafb;
  border: 2px dashed #e5e7eb;
  border-radius: 12px;

  .map-icon {
    width: 64px;
    height: 64px;
    color: #d1d5db;
    margin-bottom: 12px;
  }

  p {
    font-size: 14px;
    color: #6b7280;
    margin: 0;
  }
}

/* Help Text */
.help-text {
  padding: 12px 16px;
  background: #f0f9ff;
  border: 1px solid #bae6fd;
  border-radius: 8px;
  font-size: 14px;
  color: #0c4a6e;
  margin-top: 16px;
}

/* Responsive */
@media (max-width: 768px) {
  .content-card {
    padding: 20px;
  }

  .form-row {
    grid-template-columns: 1fr;
  }

  .tabs-header {
    justify-content: flex-start;
  }

  .tab-button {
    min-width: 100px;
    padding: 12px 16px;
    font-size: 14px;

    .tab-label {
      display: none;
    }
  }
}
</style>
