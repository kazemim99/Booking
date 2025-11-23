<template>
  <div class="provider-profile-page" dir="rtl">
    <!-- Loading State -->
    <div v-if="isLoading" class="loading-container">
      <div class="loading-spinner"></div>
      <p>در حال بارگذاری اطلاعات...</p>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="error-container">
      <div class="error-card">
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
          <path fill-rule="evenodd" d="M2.25 12c0-5.385 4.365-9.75 9.75-9.75s9.75 4.365 9.75 9.75-4.365 9.75-9.75 9.75S2.25 17.385 2.25 12zM12 8.25a.75.75 0 01.75.75v3.75a.75.75 0 01-1.5 0V9a.75.75 0 01.75-.75zm0 8.25a.75.75 0 100-1.5.75.75 0 000 1.5z" clip-rule="evenodd" />
        </svg>
        <h3>مشکلی پیش آمد</h3>
        <p>{{ error }}</p>
        <button class="btn-back-home" @click="goHome">
          بازگشت به صفحه اصلی
        </button>
      </div>
    </div>

    <!-- Provider Profile -->
    <div v-else-if="provider" class="profile-container">
      <!-- Profile Header -->
      <ProfileHeader :provider="provider" />

      <!-- Profile Tabs -->
      <div class="profile-tabs-container">
        <div class="tabs-navigation">
          <button
            v-for="tab in tabs"
            :key="tab.id"
            class="tab-btn"
            :class="{ active: activeTab === tab.id }"
            @click="activeTab = tab.id"
          >
            <span class="tab-icon">{{ tab.icon }}</span>
            {{ tab.label }}
          </button>
        </div>

        <div class="tabs-content">
          <!-- Services Tab -->
          <div v-if="activeTab === 'services'" class="tab-panel">
            <ProfileServices :provider="provider" />
          </div>

          <!-- Team Tab (Organizations only) -->
          <div v-if="activeTab === 'team'" class="tab-panel">
            <ProfileTeam :provider="provider" @book-with-staff="handleBookWithStaff" />
          </div>

          <!-- Gallery Tab -->
          <div v-if="activeTab === 'gallery'" class="tab-panel">
            <ProfileGallery :provider="provider" />
          </div>

          <!-- Reviews Tab -->
          <div v-if="activeTab === 'reviews'" class="tab-panel">
            <ProfileReviews :provider="provider" />
          </div>

          <!-- About Tab -->
          <div v-if="activeTab === 'about'" class="tab-panel">
            <ProfileAbout :provider="provider" />
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useProviderStore } from '../stores/provider.store'
import { useHierarchyStore } from '../stores/hierarchy.store'
import { ProviderHierarchyType } from '../types/hierarchy.types'
import ProfileHeader from '@/components/profile/ProfileHeader.vue'
import ProfileServices from '@/components/profile/ProfileServices.vue'
import ProfileGallery from '@/components/profile/ProfileGallery.vue'
import ProfileReviews from '@/components/profile/ProfileReviews.vue'
import ProfileAbout from '@/components/profile/ProfileAbout.vue'
import ProfileTeam from '@/components/profile/ProfileTeam.vue'

const router = useRouter()
const route = useRoute()
const providerStore = useProviderStore()
const hierarchyStore = useHierarchyStore()

// State
const activeTab = ref('services')

// Computed
const provider = computed(() => providerStore.currentProvider)
const isLoading = computed(() => providerStore.isLoading)
const error = computed(() => providerStore.error)

// Check if provider is an organization with staff
const providerHierarchy = computed(() => hierarchyStore.currentHierarchy)
const isOrganizationWithStaff = computed(() => {
  return providerHierarchy.value?.provider?.hierarchyType === ProviderHierarchyType.Organization &&
         (providerHierarchy.value?.provider?.staffCount ?? 0) > 0
})

// Tabs configuration with conditional team tab
const tabs = computed(() => {
  const baseTabs = [
    { id: 'services', label: 'خدمات', icon: '✨' },
    { id: 'gallery', label: 'گالری', icon: '🖼️' },
    { id: 'reviews', label: 'نظرات', icon: '⭐' },
    { id: 'about', label: 'درباره', icon: 'ℹ️' },
  ]

  // Add team tab for organizations with staff
  if (isOrganizationWithStaff.value) {
    baseTabs.splice(1, 0, { id: 'team', label: 'تیم ما', icon: '👥' })
  }

  return baseTabs
})

// Methods
const goHome = () => {
  router.push('/')
}

const handleBookWithStaff = (staffId: string) => {
  // Navigate to booking page with pre-selected staff member
  // You can also show a booking modal here
  console.log('Booking with staff:', staffId)
  // TODO: Implement booking flow with pre-selected staff
  // Option 1: Navigate to booking page with staff ID
  // router.push({ name: 'booking', query: { providerId: provider.value?.id, staffId } })
  // Option 2: Show booking modal
  // showBookingModal.value = true
  // selectedStaffId.value = staffId
}

// Lifecycle
onMounted(async () => {
  const providerId = route.params.id as string
  if (providerId) {
    // Load provider and hierarchy data
    await Promise.all([
      providerStore.getProviderById(providerId, true, true),
      hierarchyStore.loadProviderHierarchy(providerId)
    ])
  }
})
</script>

<style scoped>
.provider-profile-page {
  min-height: 100vh;
  background: linear-gradient(180deg, #f8fafc 0%, #ffffff 100%);
}

.loading-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 60vh;
  gap: 1.5rem;
}

.loading-spinner {
  width: 64px;
  height: 64px;
  border: 5px solid #e2e8f0;
  border-top-color: #667eea;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.loading-container p {
  font-size: 1.125rem;
  color: #64748b;
  font-weight: 500;
}

.error-container {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 60vh;
  padding: 2rem;
}

.error-card {
  background: white;
  border-radius: 24px;
  padding: 3rem 2rem;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.1);
  text-align: center;
  max-width: 400px;
}

.error-card svg {
  width: 64px;
  height: 64px;
  color: #ef4444;
  margin-bottom: 1rem;
}

.error-card h3 {
  font-size: 1.5rem;
  font-weight: 700;
  color: #1e293b;
  margin: 0 0 0.75rem 0;
}

.error-card p {
  font-size: 1rem;
  color: #64748b;
  margin: 0 0 2rem 0;
}

.btn-back-home {
  padding: 0.875rem 2rem;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  border: none;
  border-radius: 12px;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s;
}

.btn-back-home:hover {
  transform: translateY(-2px);
  box-shadow: 0 8px 20px rgba(102, 126, 234, 0.4);
}

.profile-container {
  max-width: 1200px;
  margin: 0 auto;
  padding: 2rem 2rem 4rem;
}

.profile-tabs-container {
  margin-top: 3rem;
}

.tabs-navigation {
  display: flex;
  gap: 0.75rem;
  background: white;
  padding: 0.75rem;
  border-radius: 16px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.06);
  margin-bottom: 2rem;
  overflow-x: auto;
}

.tab-btn {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 1rem 1.5rem;
  background: transparent;
  border: none;
  border-radius: 12px;
  font-size: 1rem;
  font-weight: 600;
  color: #64748b;
  cursor: pointer;
  transition: all 0.3s;
  white-space: nowrap;
}

.tab-icon {
  font-size: 1.25rem;
}

.tab-btn:hover {
  background: #f1f5f9;
  color: #475569;
}

.tab-btn.active {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
}

.tabs-content {
  min-height: 400px;
}

.tab-panel {
  animation: fadeIn 0.4s ease-out;
}

@keyframes fadeIn {
  from {
    opacity: 0;
    transform: translateY(20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

/* Responsive */
@media (max-width: 768px) {
  .profile-container {
    padding: 1rem 1rem 3rem;
  }

  .tabs-navigation {
    overflow-x: auto;
    scrollbar-width: none;
  }

  .tabs-navigation::-webkit-scrollbar {
    display: none;
  }

  .tab-btn {
    padding: 0.875rem 1.25rem;
  }
}
</style>
