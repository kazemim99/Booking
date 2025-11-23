<template>
  <div class="provider-hierarchy">
    <!-- Organization Header -->
    <div v-if="isOrganization" class="organization-header">
      <div class="organization-badge">
        <i class="icon-building"></i>
        <span>سازمان</span>
      </div>
      <h2 class="organization-name">{{ provider.businessName }}</h2>
      <p v-if="provider.description" class="organization-description">
        {{ provider.description }}
      </p>
    </div>

    <!-- Individual Provider (with Parent) -->
    <div v-else-if="provider.parentOrganization" class="individual-with-parent">
      <div class="parent-link">
        <i class="icon-arrow-up"></i>
        <span>کارمند در</span>
        <router-link :to="`/providers/${provider.parentOrganization.id}`" class="parent-name">
          {{ provider.parentOrganization.businessName }}
        </router-link>
      </div>
    </div>

    <!-- Staff Members (for organizations) -->
    <div v-if="isOrganization && hasStaff" class="staff-section">
      <div class="section-header">
        <h3 class="section-title">
          <i class="icon-users"></i>
          تیم ما
        </h3>
        <span class="staff-count">{{ staffCount }} کارمند</span>
      </div>

      <div class="staff-grid">
        <div
          v-for="staff in staffMembers"
          :key="staff.id"
          class="staff-card"
        >
          <router-link :to="`/providers/${staff.id}`" class="staff-link">
            <div class="staff-avatar">
              <img v-if="staff.photoUrl" :src="staff.photoUrl" :alt="staff.fullName" />
              <div v-else class="avatar-placeholder">
                <i class="icon-user"></i>
              </div>
            </div>

            <div class="staff-info">
              <h4 class="staff-name">{{ staff.fullName }}</h4>
              <p v-if="staff.title" class="staff-title">{{ staff.title }}</p>

              <div v-if="staff.specializations?.length" class="staff-tags">
                <span
                  v-for="(spec, index) in staff.specializations.slice(0, 2)"
                  :key="index"
                  class="tag"
                >
                  {{ spec }}
                </span>
                <span v-if="staff.specializations.length > 2" class="tag-more">
                  +{{ staff.specializations.length - 2 }}
                </span>
              </div>
            </div>
          </router-link>
        </div>
      </div>

      <div v-if="canShowMore" class="show-more">
        <AppButton variant="secondary" size="small" @click="loadMoreStaff">
          مشاهده بیشتر
        </AppButton>
      </div>
    </div>

    <!-- Solo Organization Message -->
    <div v-else-if="isOrganization && !hasStaff" class="solo-message">
      <i class="icon-info-circle"></i>
      <p>در حال حاضر این سازمان کارمندی ندارد</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import AppButton from '@/shared/components/ui/Button/AppButton.vue'
import type { ProviderHierarchyType } from '../../types/hierarchy.types'

interface Provider {
  id: string
  hierarchyType?: ProviderHierarchyType
  businessName?: string
  description?: string
  parentOrganization?: {
    id: string
    businessName: string
    logoUrl?: string
  }
  staffMembers?: StaffMember[]
  staffCount?: number
}

interface StaffMember {
  id: string
  fullName: string
  title?: string
  photoUrl?: string
  specializations?: string[]
}

interface Props {
  provider: Provider
  maxStaffShown?: number
}

interface Emits {
  (e: 'load-more'): void
}

const props = withDefaults(defineProps<Props>(), {
  maxStaffShown: 6,
})

const emit = defineEmits<Emits>()

const isOrganization = computed(() => props.provider.hierarchyType === 'Organization')
const hasStaff = computed(() => (props.provider.staffCount ?? 0) > 0)
const staffCount = computed(() => props.provider.staffCount ?? 0)
const staffMembers = computed(() => props.provider.staffMembers?.slice(0, props.maxStaffShown) || [])
const canShowMore = computed(() => (props.provider.staffMembers?.length ?? 0) > props.maxStaffShown)

function loadMoreStaff() {
  emit('load-more')
}
</script>

<style scoped lang="scss">
.provider-hierarchy {
  background: white;
  border-radius: 12px;
  padding: 1.5rem;
  border: 1px solid #e2e8f0;
}

.organization-header {
  text-align: center;
  margin-bottom: 2rem;

  .organization-badge {
    display: inline-flex;
    align-items: center;
    gap: 0.5rem;
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
    padding: 0.5rem 1rem;
    border-radius: 20px;
    font-size: 0.875rem;
    font-weight: 500;
    margin-bottom: 1rem;

    i {
      font-size: 1rem;
    }
  }

  .organization-name {
    font-size: 1.75rem;
    color: #1a202c;
    margin-bottom: 0.5rem;
    font-weight: 700;
  }

  .organization-description {
    color: #718096;
    line-height: 1.6;
    max-width: 600px;
    margin: 0 auto;
  }
}

.individual-with-parent {
  margin-bottom: 1.5rem;

  .parent-link {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 1rem;
    background: #f7fafc;
    border-radius: 8px;
    color: #718096;
    font-size: 0.95rem;

    i {
      color: #667eea;
    }

    .parent-name {
      color: #667eea;
      font-weight: 600;
      text-decoration: none;
      transition: color 0.2s;

      &:hover {
        color: #5a67d8;
        text-decoration: underline;
      }
    }
  }
}

.staff-section {
  .section-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 1.5rem;

    .section-title {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      font-size: 1.25rem;
      color: #1a202c;
      margin: 0;

      i {
        color: #667eea;
      }
    }

    .staff-count {
      background: #f7fafc;
      color: #718096;
      padding: 0.375rem 0.75rem;
      border-radius: 12px;
      font-size: 0.875rem;
      font-weight: 500;
    }
  }

  .staff-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
    gap: 1rem;
  }

  .staff-card {
    border: 1px solid #e2e8f0;
    border-radius: 10px;
    transition: all 0.2s;

    &:hover {
      border-color: #cbd5e0;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
    }

    .staff-link {
      display: block;
      padding: 1rem;
      text-decoration: none;
      color: inherit;
    }

    .staff-avatar {
      width: 70px;
      height: 70px;
      margin: 0 auto 1rem;
      border-radius: 50%;
      overflow: hidden;

      img {
        width: 100%;
        height: 100%;
        object-fit: cover;
      }

      .avatar-placeholder {
        width: 100%;
        height: 100%;
        background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        display: flex;
        align-items: center;
        justify-content: center;
        color: white;
        font-size: 1.75rem;
      }
    }

    .staff-info {
      text-align: center;

      .staff-name {
        font-size: 1.125rem;
        color: #1a202c;
        margin-bottom: 0.25rem;
        font-weight: 600;
      }

      .staff-title {
        color: #667eea;
        font-size: 0.875rem;
        margin-bottom: 0.75rem;
        font-weight: 500;
      }

      .staff-tags {
        display: flex;
        flex-wrap: wrap;
        gap: 0.375rem;
        justify-content: center;

        .tag {
          background: #f7fafc;
          color: #4a5568;
          padding: 0.25rem 0.625rem;
          border-radius: 12px;
          font-size: 0.75rem;
        }

        .tag-more {
          background: #667eea;
          color: white;
          padding: 0.25rem 0.625rem;
          border-radius: 12px;
          font-size: 0.75rem;
          font-weight: 600;
        }
      }
    }
  }

  .show-more {
    margin-top: 1.5rem;
    text-align: center;
  }
}

.solo-message {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.75rem;
  padding: 2rem;
  color: #a0aec0;
  text-align: center;

  i {
    font-size: 1.5rem;
  }
}

@media (max-width: 768px) {
  .staff-grid {
    grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
  }
}

@media (max-width: 480px) {
  .staff-grid {
    grid-template-columns: 1fr;
  }
}
</style>
