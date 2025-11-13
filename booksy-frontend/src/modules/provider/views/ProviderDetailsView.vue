<template>
  <div class="provider-details-view">
    <!-- Loading State -->
    <div v-if="isLoading" class="loading-container">
      <Spinner size="large" />
      <p>Loading provider details...</p>
    </div>

    <!-- Error State -->
    <Alert v-else-if="error" type="error" :message="error" />

    <!-- Provider Details -->
    <div v-else-if="provider" class="provider-details">
      <!-- Cover Image -->
      <div class="cover-section" :style="coverStyle">
        <div class="cover-overlay">
          <button class="btn-back" @click="goBack">
            <svg
              xmlns="http://www.w3.org/2000/svg"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M15 19l-7-7 7-7"
              />
            </svg>
            Back
          </button>
        </div>
      </div>

      <!-- Main Content -->
      <div class="content-wrapper">
        <!-- Header Section -->
        <div class="provider-header">
          <div class="header-left">
            <div class="provider-logo">
              <img
                v-if="provider.profile.logoUrl"
                :src="provider.profile.logoUrl"
                :alt="provider.profile.businessName"
              />
              <div v-else class="placeholder-logo">
                <span class="initials">{{ getInitials(provider.profile.businessName) }}</span>
              </div>
            </div>

            <div class="header-info">
              <div class="title-row">
                <h1 class="business-name">{{ provider.profile.businessName }}</h1>
                <Badge :variant="getStatusVariant(provider.status)">{{ provider.status }}</Badge>
                <Badge :variant="getTypeVariant()">{{ provider.type }}</Badge>
              </div>

              <div class="meta-row">
                <div class="meta-item">
                  <svg
                    xmlns="http://www.w3.org/2000/svg"
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                  >
                    <path
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      stroke-width="2"
                      d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z"
                    />
                    <path
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      stroke-width="2"
                      d="M15 11a3 3 0 11-6 0 3 3 0 016 0z"
                    />
                  </svg>
                  <span>{{ formatAddress(provider.address) }}</span>
                </div>

                <div v-if="provider.verifiedAt" class="meta-item verified">
                  <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                    <path
                      fill-rule="evenodd"
                      d="M8.603 3.799A4.49 4.49 0 0112 2.25c1.357 0 2.573.6 3.397 1.549a4.49 4.49 0 013.498 1.307 4.491 4.491 0 011.307 3.497A4.49 4.49 0 0121.75 12a4.49 4.49 0 01-1.549 3.397 4.491 4.491 0 01-1.307 3.497 4.491 4.491 0 01-3.497 1.307A4.49 4.49 0 0112 21.75a4.49 4.49 0 01-3.397-1.549 4.49 4.49 0 01-3.498-1.306 4.491 4.491 0 01-1.307-3.498A4.49 4.49 0 012.25 12c0-1.357.6-2.573 1.549-3.397a4.49 4.49 0 011.307-3.497 4.49 4.49 0 013.497-1.307zm7.007 6.387a.75.75 0 10-1.22-.872l-3.236 4.53L9.53 12.22a.75.75 0 00-1.06 1.06l2.25 2.25a.75.75 0 001.14-.094l3.75-5.25z"
                      clip-rule="evenodd"
                    />
                  </svg>
                  <span>Verified Provider</span>
                </div>
              </div>

              <div class="feature-badges">
                <div v-if="provider.allowOnlineBooking" class="feature-badge">
                  <svg
                    xmlns="http://www.w3.org/2000/svg"
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                  >
                    <path
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      stroke-width="2"
                      d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"
                    />
                  </svg>
                  Online Booking Available
                </div>
                <div v-if="provider.offersMobileServices" class="feature-badge">
                  <svg
                    xmlns="http://www.w3.org/2000/svg"
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                  >
                    <path
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      stroke-width="2"
                      d="M5 8h14M5 8a2 2 0 110-4h14a2 2 0 110 4M5 8v10a2 2 0 002 2h10a2 2 0 002-2V8m-9 4h4"
                    />
                  </svg>
                  Mobile Services
                </div>
              </div>
            </div>
          </div>

          <div class="header-actions">
            <button
              v-if="provider.allowOnlineBooking"
              class="btn btn-primary btn-large"
              @click="handleBookNow"
            >
              <svg
                xmlns="http://www.w3.org/2000/svg"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"
                />
              </svg>
              Book Now
            </button>
            <button class="btn btn-secondary" @click="handleContact">
              <svg
                xmlns="http://www.w3.org/2000/svg"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"
                />
              </svg>
              Contact
            </button>
          </div>
        </div>

        <!-- Tabs Navigation -->
        <div class="tabs-container">
          <button
            v-for="tab in tabs"
            :key="tab.id"
            class="tab-button"
            :class="{ active: activeTab === tab.id }"
            @click="activeTab = tab.id"
          >
            {{ tab.label }}
          </button>
        </div>

        <!-- Tab Content -->
        <div class="tab-content">
          <!-- About Tab -->
          <div v-if="activeTab === 'about'" class="tab-pane">
            <Card>
              <h2>About {{ provider.profile.businessName }}</h2>
              <p class="description">{{ provider.profile.description }}</p>

              <div v-if="provider.tags && provider.tags.length > 0" class="tags-section">
                <h3>Specializations</h3>
                <div class="tags-list">
                  <span v-for="tag in provider.tags" :key="tag" class="tag">{{ tag }}</span>
                </div>
              </div>
            </Card>

            <Card>
              <h2>Contact Information</h2>
              <div class="contact-grid">
                <div class="contact-item">
                  <svg
                    xmlns="http://www.w3.org/2000/svg"
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                  >
                    <path
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      stroke-width="2"
                      d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"
                    />
                  </svg>
                  <div>
                    <span class="label">Email</span>
                    <a :href="`mailto:${provider.contactInfo.email}`" class="value">{{
                      provider.contactInfo.email
                    }}</a>
                  </div>
                </div>

                <div class="contact-item">
                  <svg
                    xmlns="http://www.w3.org/2000/svg"
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                  >
                    <path
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      stroke-width="2"
                      d="M3 5a2 2 0 012-2h3.28a1 1 0 01.948.684l1.498 4.493a1 1 0 01-.502 1.21l-2.257 1.13a11.042 11.042 0 005.516 5.516l1.13-2.257a1 1 0 011.21-.502l4.493 1.498a1 1 0 01.684.949V19a2 2 0 01-2 2h-1C9.716 21 3 14.284 3 6V5z"
                    />
                  </svg>
                  <div>
                    <span class="label">Phone</span>
                    <a :href="`tel:${provider.contactInfo.primaryPhone}`" class="value">{{
                      provider.contactInfo.primaryPhone
                    }}</a>
                  </div>
                </div>

                <div v-if="provider.contactInfo.secondaryPhone" class="contact-item">
                  <svg
                    xmlns="http://www.w3.org/2000/svg"
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                  >
                    <path
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      stroke-width="2"
                      d="M12 18h.01M8 21h8a2 2 0 002-2V5a2 2 0 00-2-2H8a2 2 0 00-2 2v14a2 2 0 002 2z"
                    />
                  </svg>
                  <div>
                    <span class="label">Secondary Phone</span>
                    <a :href="`tel:${provider.contactInfo.secondaryPhone}`" class="value">{{
                      provider.contactInfo.secondaryPhone
                    }}</a>
                  </div>
                </div>

                <div v-if="provider.profile.websiteUrl" class="contact-item">
                  <svg
                    xmlns="http://www.w3.org/2000/svg"
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                  >
                    <path
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      stroke-width="2"
                      d="M21 12a9 9 0 01-9 9m9-9a9 9 0 00-9-9m9 9H3m9 9a9 9 0 01-9-9m9 9c1.657 0 3-4.03 3-9s-1.343-9-3-9m0 18c-1.657 0-3-4.03-3-9s1.343-9 3-9m-9 9a9 9 0 019-9"
                    />
                  </svg>
                  <div>
                    <span class="label">Website</span>
                    <a
                      :href="provider.profile.websiteUrl"
                      target="_blank"
                      rel="noopener"
                      class="value"
                      >Visit Website</a
                    >
                  </div>
                </div>
              </div>
            </Card>

            <Card>
              <h2>Location</h2>
              <div class="address-section">
                <p class="address">
                  {{ provider.address.addressLine1 }}<br />
                  <span v-if="provider.address.addressLine2"
                    >{{ provider.address.addressLine2 }}<br
                  /></span>
                  {{ provider.address.city }}, {{ provider.address.state }}
                  {{ provider.address.postalCode }}<br />
                  {{ provider.address.country }}
                </p>
                <!-- Map placeholder -->
                <div class="map-placeholder">
                  <p>Map view coming soon</p>
                </div>
              </div>
            </Card>
          </div>

          <!-- Services Tab -->
          <div v-if="activeTab === 'services'" class="tab-pane">
            <div v-if="provider.services && provider.services.length > 0" class="services-grid">
              <Card v-for="service in provider.services" :key="service.id" class="service-card">
                <div class="service-header">
                  <h3>{{ service.name }}</h3>
                  <Badge :variant="service.status === 'Active' ? 'success' : 'default'">
                    {{ service.status }}
                  </Badge>
                </div>
                <p class="service-description">{{ service.description }}</p>
                <div class="service-details">
                  <div class="detail-item">
                    <span class="label">Duration:</span>
                    <span class="value">{{ service.duration }} min</span>
                  </div>
                  <div class="detail-item">
                    <span class="label">Price:</span>
                    <span class="value">{{
                      formatPrice(service.basePrice, service.currency)
                    }}</span>
                  </div>
                  <div class="detail-item">
                    <span class="label">Category:</span>
                    <span class="value">{{ service.category }}</span>
                  </div>
                </div>
                <button
                  v-if="provider.allowOnlineBooking"
                  class="btn btn-primary btn-small"
                  @click="handleBookService(service)"
                >
                  Book This Service
                </button>
              </Card>
            </div>
            <div v-else class="empty-state">
              <p>No services available at this time.</p>
            </div>
          </div>

          <!-- Staff Tab -->
          <div v-if="activeTab === 'staff'" class="tab-pane">
            <div v-if="provider.staff && provider.staff.length > 0" class="staff-grid">
              <Card v-for="member in provider.staff" :key="member.id" class="staff-card">
                <div class="staff-photo">
                  <img
                    v-if="member.photoUrl"
                    :src="member.photoUrl"
                    :alt="`${member.firstName} ${member.lastName}`"
                  />
                  <div v-else class="photo-placeholder">
                    <span>{{ member.firstName[0] }}{{ member.lastName[0] }}</span>
                  </div>
                </div>
                <div class="staff-info">
                  <h3>{{ member.firstName }} {{ member.lastName }}</h3>
                  <p v-if="member.title" class="staff-title">{{ member.title }}</p>
                  <p v-if="member.bio" class="staff-bio">{{ member.bio }}</p>
                  <div
                    v-if="member.specializations && member.specializations.length > 0"
                    class="specializations"
                  >
                    <span v-for="spec in member.specializations" :key="spec" class="spec-tag">{{
                      spec
                    }}</span>
                  </div>
                </div>
              </Card>
            </div>
            <div v-else class="empty-state">
              <p>Staff information not available.</p>
            </div>
          </div>

          <!-- Hours Tab -->
          <div v-if="activeTab === 'hours'" class="tab-pane">
            <Card>
              <h2>Business Hours</h2>
              <div
                v-if="provider.businessHours && provider.businessHours.length > 0"
                class="hours-list"
              >
                <div v-for="hours in sortedBusinessHours" :key="hours.id" class="hours-item">
                  <span class="day">{{ getDayName(hours.dayOfWeek) }}</span>
                  <span v-if="hours.isOpen" class="time"
                    >{{ hours.openTime }} - {{ hours.closeTime }}</span
                  >
                  <span v-else class="closed">Closed</span>
                </div>
              </div>
              <p v-else class="no-hours">Business hours not available.</p>
            </Card>
          </div>

          <!-- Gallery Tab -->
          <div v-if="activeTab === 'gallery'" class="tab-pane">
            <Card>
              <GalleryDisplay
                v-if="provider"
                :provider-id="provider.id"
                :show-header="false"
                :columns="3"
                :max-images="0"
                view-mode="masonry"
                :allow-view-switch="true"
              />
            </Card>
          </div>

          <!-- Reviews Tab -->
          <div v-if="activeTab === 'reviews'" class="tab-pane">
            <Card>
              <h2>Reviews</h2>
              <div class="empty-state">
                <p>Reviews feature coming soon.</p>
              </div>
            </Card>
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
import { Alert, Spinner, Card, Badge } from '@/shared/components'
import GalleryDisplay from '@/modules/provider/components/gallery/GalleryDisplay.vue'
import type { DayOfWeek, ServiceSummary } from '../types/provider.types'

// Router
const router = useRouter()
const route = useRoute()

// Store
const providerStore = useProviderStore()

// State
const activeTab = ref('about')

const tabs = [
  { id: 'about', label: 'About' },
  { id: 'services', label: 'Services' },
  { id: 'staff', label: 'Staff' },
  { id: 'gallery', label: 'Gallery' },
  { id: 'hours', label: 'Hours' },
  { id: 'reviews', label: 'Reviews' },
]

// Computed
const provider = computed(() => providerStore.currentProvider)
const isLoading = computed(() => providerStore.isLoading)
const error = computed(() => providerStore.error)

const coverStyle = computed(() => {
  if (provider.value?.profile.coverImageUrl) {
    return {
      backgroundImage: `url(${provider.value.profile.coverImageUrl})`,
    }
  }
  return {
    background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
  }
})

const sortedBusinessHours = computed(() => {
  if (!provider.value?.businessHours) return []
  return [...provider.value.businessHours].sort((a, b) => a.dayOfWeek - b.dayOfWeek)
})

// Methods
const getInitials = (name: string): string => {
  return name
    .split(' ')
    .map((word) => word[0])
    .join('')
    .toUpperCase()
    .slice(0, 2)
}

const getStatusVariant = (status: string): 'success' | 'warning' | 'danger' | 'default' => {
  const variants: Record<string, 'success' | 'warning' | 'danger' | 'default'> = {
    Active: 'success',
    Pending: 'warning',
    Inactive: 'default',
    Suspended: 'danger',
  }
  return variants[status] || 'default'
}

const getTypeVariant = (): 'info' | 'default' => {
  return 'info'
}

const formatAddress = (address: { city: string; state: string; country?: string }): string => {
  return `${address.city}, ${address.state}`
}

const formatPrice = (amount: number, currency: string): string => {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: currency || 'USD',
  }).format(amount)
}

const getDayName = (day: DayOfWeek): string => {
  const days = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday']
  return days[day]
}

const goBack = () => {
  router.back()
}

const handleBookNow = () => {
  if (provider.value) {
    router.push({
      name: 'NewBooking',
      query: { providerId: provider.value.id },
    })
  }
}

const handleContact = () => {
  if (provider.value) {
    window.location.href = `mailto:${provider.value.contactInfo.email}`
  }
}

const handleBookService = (service: ServiceSummary) => {
  if (provider.value) {
    router.push({
      name: 'NewBooking',
      query: {
        providerId: provider.value.id,
        serviceId: service.id,
      },
    })
  }
}

// Lifecycle
onMounted(async () => {
  const providerId = route.params.id as string
  if (providerId) {
    await providerStore.getProviderById(providerId, true, true)
  }
})
</script>

<style scoped>
.provider-details-view {
  min-height: 100vh;
  background: var(--color-bg-secondary);
}

.loading-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 60vh;
  gap: 1rem;
}

.cover-section {
  height: 300px;
  background-size: cover;
  background-position: center;
  position: relative;
}

.cover-overlay {
  position: absolute;
  inset: 0;
  background: linear-gradient(to bottom, rgba(0, 0, 0, 0.3), rgba(0, 0, 0, 0.1));
  display: flex;
  align-items: flex-start;
  padding: 2rem;
}

.btn-back {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.75rem 1.25rem;
  background: rgba(255, 255, 255, 0.9);
  border: none;
  border-radius: 8px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-back:hover {
  background: white;
  transform: translateX(-4px);
}

.btn-back svg {
  width: 20px;
  height: 20px;
}

.content-wrapper {
  max-width: 1200px;
  margin: -80px auto 0;
  padding: 0 2rem 3rem;
  position: relative;
}

.provider-header {
  background: white;
  border-radius: 16px;
  padding: 2rem;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
  margin-bottom: 2rem;
  display: flex;
  justify-content: space-between;
  gap: 2rem;
}

.header-left {
  display: flex;
  gap: 1.5rem;
  flex: 1;
}

.provider-logo {
  width: 120px;
  height: 120px;
  flex-shrink: 0;
  border-radius: 12px;
  overflow: hidden;
  border: 4px solid white;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.provider-logo img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.placeholder-logo {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, var(--color-primary) 0%, var(--color-primary-dark) 100%);
}

.initials {
  font-size: 2.5rem;
  font-weight: 700;
  color: white;
}

.header-info {
  flex: 1;
}

.title-row {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-bottom: 0.75rem;
}

.business-name {
  font-size: 2rem;
  font-weight: 700;
  margin: 0;
  color: var(--color-text-primary);
}

.meta-row {
  display: flex;
  gap: 1.5rem;
  margin-bottom: 1rem;
}

.meta-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  color: var(--color-text-secondary);
  font-size: 0.95rem;
}

.meta-item svg {
  width: 18px;
  height: 18px;
}

.meta-item.verified {
  color: var(--color-success);
  font-weight: 500;
}

.feature-badges {
  display: flex;
  gap: 0.75rem;
}

.feature-badge {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 1rem;
  background: var(--color-bg-secondary);
  border-radius: 8px;
  font-size: 0.9rem;
  font-weight: 500;
}

.feature-badge svg {
  width: 18px;
  height: 18px;
}

.header-actions {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.btn {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 0.875rem 1.75rem;
  border: none;
  border-radius: 8px;
  font-size: 1rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
  white-space: nowrap;
}

.btn svg {
  width: 20px;
  height: 20px;
}

.btn-primary {
  background: var(--color-primary);
  color: white;
}

.btn-primary:hover {
  background: var(--color-primary-dark);
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(99, 102, 241, 0.3);
}

.btn-secondary {
  background: var(--color-bg-secondary);
  color: var(--color-text-primary);
}

.btn-secondary:hover {
  background: var(--color-border);
}

.btn-large {
  padding: 1rem 2rem;
  font-size: 1.1rem;
}

.btn-small {
  padding: 0.5rem 1rem;
  font-size: 0.9rem;
}

.tabs-container {
  display: flex;
  gap: 0.5rem;
  background: white;
  padding: 0.5rem;
  border-radius: 12px;
  margin-bottom: 2rem;
  overflow-x: auto;
}

.tab-button {
  flex: 1;
  padding: 0.875rem 1.5rem;
  border: none;
  background: transparent;
  border-radius: 8px;
  font-size: 1rem;
  font-weight: 500;
  color: var(--color-text-secondary);
  cursor: pointer;
  transition: all 0.2s;
  white-space: nowrap;
}

.tab-button:hover {
  background: var(--color-bg-secondary);
  color: var(--color-text-primary);
}

.tab-button.active {
  background: var(--color-primary);
  color: white;
}

.tab-content {
  margin-bottom: 2rem;
}

.tab-pane {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.description {
  font-size: 1.05rem;
  line-height: 1.7;
  color: var(--color-text-secondary);
}

.tags-section {
  margin-top: 2rem;
}

.tags-section h3 {
  font-size: 1.1rem;
  margin-bottom: 1rem;
}

.tags-list {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.tag {
  padding: 0.5rem 1rem;
  background: var(--color-primary-light);
  color: var(--color-primary-dark);
  border-radius: 6px;
  font-size: 0.9rem;
  font-weight: 500;
}

.contact-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 1.5rem;
  margin-top: 1.5rem;
}

.contact-item {
  display: flex;
  gap: 1rem;
}

.contact-item svg {
  width: 24px;
  height: 24px;
  color: var(--color-primary);
  flex-shrink: 0;
}

.contact-item .label {
  display: block;
  font-size: 0.85rem;
  color: var(--color-text-tertiary);
  margin-bottom: 0.25rem;
}

.contact-item .value {
  display: block;
  font-size: 1rem;
  color: var(--color-text-primary);
  font-weight: 500;
}

.contact-item a {
  color: var(--color-primary);
  text-decoration: none;
}

.contact-item a:hover {
  text-decoration: underline;
}

.address-section {
  margin-top: 1.5rem;
}

.address {
  font-size: 1rem;
  line-height: 1.8;
  color: var(--color-text-secondary);
  margin-bottom: 1.5rem;
}

.map-placeholder {
  height: 300px;
  background: var(--color-bg-secondary);
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--color-text-tertiary);
}

.services-grid,
.staff-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  gap: 1.5rem;
}

.service-card,
.staff-card {
  height: 100%;
}

.service-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 1rem;
  gap: 1rem;
}

.service-header h3 {
  font-size: 1.25rem;
  margin: 0;
}

.service-description {
  color: var(--color-text-secondary);
  margin-bottom: 1.5rem;
  line-height: 1.6;
}

.service-details {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  margin-bottom: 1.5rem;
  padding: 1rem;
  background: var(--color-bg-secondary);
  border-radius: 8px;
}

.detail-item {
  display: flex;
  justify-content: space-between;
}

.detail-item .label {
  color: var(--color-text-secondary);
  font-size: 0.9rem;
}

.detail-item .value {
  font-weight: 600;
  color: var(--color-text-primary);
}

.staff-photo {
  width: 120px;
  height: 120px;
  margin: 0 auto 1rem;
  border-radius: 50%;
  overflow: hidden;
}

.staff-photo img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.photo-placeholder {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--color-primary);
  color: white;
  font-size: 2rem;
  font-weight: 700;
}

.staff-info {
  text-align: center;
}

.staff-info h3 {
  font-size: 1.25rem;
  margin-bottom: 0.25rem;
}

.staff-title {
  color: var(--color-text-secondary);
  font-size: 0.95rem;
  margin-bottom: 0.75rem;
}

.staff-bio {
  color: var(--color-text-secondary);
  line-height: 1.6;
  margin-bottom: 1rem;
}

.specializations {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  justify-content: center;
}

.spec-tag {
  padding: 0.25rem 0.75rem;
  background: var(--color-bg-secondary);
  border-radius: 4px;
  font-size: 0.85rem;
}

.hours-list {
  margin-top: 1.5rem;
}

.hours-item {
  display: flex;
  justify-content: space-between;
  padding: 1rem;
  border-bottom: 1px solid var(--color-border);
}

.hours-item:last-child {
  border-bottom: none;
}

.day {
  font-weight: 600;
  color: var(--color-text-primary);
}

.time {
  color: var(--color-text-secondary);
}

.closed {
  color: var(--color-text-tertiary);
  font-style: italic;
}

.no-hours {
  color: var(--color-text-secondary);
  font-style: italic;
  margin-top: 1rem;
}

.empty-state {
  text-align: center;
  padding: 3rem 1rem;
  color: var(--color-text-secondary);
}

@media (max-width: 768px) {
  .content-wrapper {
    padding: 0 1rem 2rem;
    margin-top: -60px;
  }

  .provider-header {
    flex-direction: column;
  }

  .header-left {
    flex-direction: column;
    align-items: center;
    text-align: center;
  }

  .header-actions {
    width: 100%;
  }

  .btn {
    width: 100%;
  }

  .tabs-container {
    overflow-x: auto;
  }

  .services-grid,
  .staff-grid {
    grid-template-columns: 1fr;
  }
}
</style>
