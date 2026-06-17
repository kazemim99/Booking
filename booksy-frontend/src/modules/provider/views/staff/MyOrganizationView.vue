<template>
  <div class="my-organization-view">
    <div class="page-header">
      <h1>سازمان من</h1>
      <p class="subtitle">اطلاعات سازمانی که در آن فعالیت می‌کنید</p>
    </div>

    <div v-if="loading" class="loading-state">
      <div class="spinner"></div>
      <p>در حال بارگذاری...</p>
    </div>

    <div v-else-if="parentOrganization" class="organization-content">
      <!-- Organization Header Card -->
      <div class="organization-card">
        <div class="org-header">
          <div class="org-logo">
            <img v-if="parentOrganization.logoUrl" :src="parentOrganization.logoUrl" :alt="parentOrganization.businessName" />
            <div v-else class="logo-placeholder">
              {{ logoLetter }}
            </div>
          </div>

          <div class="org-info">
            <h2>{{ parentOrganization.businessName }}</h2>
            <p v-if="parentOrganization.city || parentOrganization.state" class="location">
              <svg class="icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
              </svg>
              {{ location }}
            </p>
          </div>
        </div>
      </div>

      <!-- Stats Cards -->
      <div class="stats-grid">
        <div class="stat-card">
          <div class="stat-icon">👥</div>
          <div class="stat-content">
            <p class="stat-label">تعداد کارمندان</p>
            <h3 class="stat-value">{{ staffCount }}</h3>
          </div>
        </div>

        <div class="stat-card">
          <div class="stat-icon">✅</div>
          <div class="stat-content">
            <p class="stat-label">کارمندان فعال</p>
            <h3 class="stat-value">{{ activeStaffCount }}</h3>
          </div>
        </div>
      </div>

      <!-- Other Staff Members -->
      <div class="section-card">
        <div class="section-header">
          <h3>سایر اعضای تیم</h3>
          <span class="count-badge">{{ otherStaffMembers.length }} نفر</span>
        </div>

        <div v-if="loadingStaff" class="loading-state-small">
          <div class="spinner-small"></div>
          <p>در حال بارگذاری...</p>
        </div>

        <div v-else-if="otherStaffMembers.length > 0" class="staff-list">
          <div v-for="staff in otherStaffMembers" :key="staff.id" class="staff-item">
            <div class="staff-avatar">
              <img v-if="staff.photoUrl" :src="staff.photoUrl" :alt="staff.fullName" />
              <div v-else class="avatar-placeholder">
                {{ staff.fullName.charAt(0) }}
              </div>
            </div>
            <div class="staff-info">
              <h4>{{ staff.fullName }}</h4>
              <p v-if="staff.title" class="staff-title">{{ staff.title }}</p>
              <p v-else class="staff-role">{{ staff.role }}</p>
            </div>
            <div class="staff-status">
              <span :class="['status-badge', staff.isActive ? 'status-active' : 'status-inactive']">
                {{ staff.isActive ? 'فعال' : 'غیرفعال' }}
              </span>
            </div>
          </div>
        </div>

        <div v-else class="empty-state">
          <p>کارمند دیگری در این سازمان وجود ندارد</p>
        </div>
      </div>

      <!-- Read-Only Notice -->
      <div class="info-notice">
        <svg class="icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
        <p>این اطلاعات فقط قابل مشاهده هستند. برای تغییرات سازمانی، با مدیر سازمان تماس بگیرید.</p>
      </div>
    </div>

    <div v-else class="error-state">
      <p>اطلاعات سازمان یافت نشد</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useHierarchyStore } from '../../stores/hierarchy.store'
import { useProviderStore } from '../../stores/provider.store'

const hierarchyStore = useHierarchyStore()
const providerStore = useProviderStore()
const loading = ref(true)
const loadingStaff = ref(false)

const parentOrganization = computed(() => hierarchyStore.currentHierarchy?.parentOrganization)
const currentProvider = computed(() => providerStore.currentProvider)

const logoLetter = computed(() => {
  return parentOrganization.value?.businessName?.charAt(0) || 'س'
})

const location = computed(() => {
  const org = parentOrganization.value
  if (!org) return ''
  const parts = [org.city, org.state].filter(Boolean)
  return parts.join(', ')
})

const staffCount = computed(() => {
  return hierarchyStore.staffMembers.length + 1 // +1 for current user
})

const activeStaffCount = computed(() => {
  const activeOthers = hierarchyStore.staffMembers.filter(s => s.isActive).length
  return activeOthers + 1 // +1 for current user (assuming they're active)
})

const otherStaffMembers = computed(() => {
  // Filter out the current user from the staff list
  return hierarchyStore.staffMembers.filter(s => s.providerId !== currentProvider.value?.id)
})

onMounted(async () => {
  console.log('[MyOrganizationView] Staff member organization view mounted')

  loading.value = false

  // Load staff members if parent organization exists
  if (parentOrganization.value?.id) {
    loadingStaff.value = true
    try {
      await hierarchyStore.loadStaffMembers({
        organizationId: parentOrganization.value.id
      })
    } catch (error) {
      console.error('[MyOrganizationView] Failed to load staff members:', error)
    } finally {
      loadingStaff.value = false
    }
  }
})
</script>

<style scoped lang="scss">
.my-organization-view {
  max-width: 1200px;
  margin: 0 auto;
}

.page-header {
  margin-bottom: 32px;

  h1 {
    font-size: 28px;
    font-weight: 700;
    color: var(--color-gray-900);
    margin: 0 0 8px 0;
  }

  .subtitle {
    font-size: 16px;
    color: var(--color-gray-600);
    margin: 0;
  }
}

.loading-state {
  text-align: center;
  padding: 64px 32px;

  .spinner {
    width: 48px;
    height: 48px;
    border: 4px solid var(--color-gray-300);
    border-top-color: var(--color-primary-500);
    border-radius: 50%;
    margin: 0 auto 16px;
    animation: spin 0.8s linear infinite;
  }

  p {
    color: var(--color-gray-600);
  }
}

.loading-state-small {
  text-align: center;
  padding: 32px;

  .spinner-small {
    width: 32px;
    height: 32px;
    border: 3px solid var(--color-gray-300);
    border-top-color: var(--color-primary-500);
    border-radius: 50%;
    margin: 0 auto 12px;
    animation: spin 0.8s linear infinite;
  }

  p {
    font-size: 14px;
    color: var(--color-gray-600);
    margin: 0;
  }
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.organization-content {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.organization-card {
  background: white;
  border: 1px solid var(--color-gray-300);
  border-radius: 12px;
  padding: 32px;
}

.org-header {
  display: flex;
  gap: 24px;
  align-items: flex-start;

  .org-logo {
    flex-shrink: 0;

    img {
      width: 100px;
      height: 100px;
      border-radius: 12px;
      object-fit: cover;
    }

    .logo-placeholder {
      width: 100px;
      height: 100px;
      border-radius: 12px;
      background: linear-gradient(135deg, var(--color-primary-500) 0%, var(--color-primary-600) 100%);
      color: white;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 40px;
      font-weight: 700;
    }
  }

  .org-info {
    flex: 1;

    h2 {
      font-size: 24px;
      font-weight: 700;
      color: var(--color-gray-900);
      margin: 0 0 12px 0;
    }

    .location {
      display: flex;
      align-items: center;
      gap: 8px;
      color: var(--color-gray-600);
      font-size: 15px;
      margin: 0;

      .icon {
        width: 18px;
        height: 18px;
      }
    }
  }
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 20px;
}

.stat-card {
  background: white;
  border: 1px solid var(--color-gray-300);
  border-radius: 12px;
  padding: 20px;
  display: flex;
  gap: 16px;
  align-items: flex-start;

  .stat-icon {
    font-size: 32px;
    flex-shrink: 0;
  }

  .stat-content {
    flex: 1;

    .stat-label {
      font-size: 14px;
      color: var(--color-gray-600);
      margin: 0 0 8px 0;
    }

    .stat-value {
      font-size: 24px;
      font-weight: 700;
      color: var(--color-gray-900);
      margin: 0;
    }
  }
}

.section-card {
  background: white;
  border: 1px solid var(--color-gray-300);
  border-radius: 12px;
  padding: 24px;

  .section-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 20px;
    padding-bottom: 12px;
    border-bottom: 1px solid var(--color-gray-300);

    h3 {
      font-size: 18px;
      font-weight: 600;
      color: var(--color-gray-900);
      margin: 0;
    }

    .count-badge {
      padding: 4px 12px;
      background: #e0e7ff;
      color: var(--color-primary-700);
      border-radius: 12px;
      font-size: 13px;
      font-weight: 600;
    }
  }
}

.staff-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.staff-item {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 16px;
  background: var(--color-gray-50);
  border-radius: 8px;
  transition: background-color 0.2s;

  &:hover {
    background: var(--color-gray-100);
  }

  .staff-avatar {
    flex-shrink: 0;

    img {
      width: 48px;
      height: 48px;
      border-radius: 50%;
      object-fit: cover;
    }

    .avatar-placeholder {
      width: 48px;
      height: 48px;
      border-radius: 50%;
      background: #e0e7ff;
      color: var(--color-primary-700);
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 18px;
      font-weight: 600;
    }
  }

  .staff-info {
    flex: 1;

    h4 {
      font-size: 16px;
      font-weight: 600;
      color: var(--color-gray-900);
      margin: 0 0 4px 0;
    }

    .staff-title,
    .staff-role {
      font-size: 14px;
      color: var(--color-gray-600);
      margin: 0;
    }
  }

  .staff-status {
    flex-shrink: 0;
  }
}

.status-badge {
  display: inline-block;
  padding: 4px 12px;
  border-radius: 12px;
  font-size: 12px;
  font-weight: 600;

  &.status-active {
    background: #d1fae5;
    color: #065f46;
  }

  &.status-inactive {
    background: var(--color-danger-50);
    color: var(--color-danger-600);
  }
}

.empty-state {
  text-align: center;
  padding: 32px;

  p {
    color: var(--color-gray-600);
    font-size: 15px;
    margin: 0;
  }
}

.info-notice {
  display: flex;
  gap: 12px;
  background: var(--color-primary-50);
  border: 1px solid #bfdbfe;
  border-radius: 12px;
  padding: 16px;

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
}

.error-state {
  text-align: center;
  padding: 64px 32px;
  background: white;
  border: 1px solid var(--color-gray-300);
  border-radius: 12px;

  p {
    color: var(--color-gray-600);
    font-size: 16px;
  }
}
</style>
