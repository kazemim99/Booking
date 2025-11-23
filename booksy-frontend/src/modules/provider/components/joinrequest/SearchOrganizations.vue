<template>
  <div class="search-organizations">
    <div class="search-header">
      <h2 class="search-title">جستجوی سازمان‌ها</h2>
      <p class="search-description">
        سازمانی که می‌خواهید به آن بپیوندید را جستجو کنید
      </p>
    </div>

    <!-- Search Bar -->
    <div class="search-bar">
      <div class="search-input-wrapper">
        <i class="icon-search"></i>
        <input
          v-model="searchQuery"
          type="text"
          class="search-input"
          placeholder="نام سازمان، شهر، یا نوع خدمت..."
          @input="handleSearch"
        />
        <button
          v-if="searchQuery"
          class="clear-button"
          @click="clearSearch"
          aria-label="پاک کردن جستجو"
        >
          <i class="icon-x"></i>
        </button>
      </div>

      <!-- Filters -->
      <div class="search-filters">
        <select v-model="filters.city" class="filter-select" @change="handleFilterChange">
          <option value="">همه شهرها</option>
          <option value="tehran">تهران</option>
          <option value="isfahan">اصفهان</option>
          <option value="shiraz">شیراز</option>
          <option value="mashhad">مشهد</option>
        </select>

        <select v-model="filters.type" class="filter-select" @change="handleFilterChange">
          <option value="">همه انواع</option>
          <option value="salon">آرایشگاه</option>
          <option value="clinic">کلینیک</option>
          <option value="spa">سالن زیبایی</option>
          <option value="other">سایر</option>
        </select>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="isLoading" class="loading-state">
      <div class="spinner"></div>
      <p>در حال جستجو...</p>
    </div>

    <!-- Empty State -->
    <div v-else-if="!isLoading && organizations.length === 0" class="empty-state">
      <i class="icon-search"></i>
      <h3>نتیجه‌ای یافت نشد</h3>
      <p>سازمانی با این مشخصات یافت نشد. لطفاً جستجوی خود را تغییر دهید.</p>
    </div>

    <!-- Organizations List -->
    <div v-else class="organizations-list">
      <div
        v-for="organization in organizations"
        :key="organization.id"
        class="organization-card"
      >
        <div class="organization-logo" v-if="organization.logoUrl">
          <img :src="organization.logoUrl" :alt="organization.businessName" />
        </div>
        <div class="organization-logo-placeholder" v-else>
          <i class="icon-building"></i>
        </div>

        <div class="organization-info">
          <h3 class="organization-name">{{ organization.businessName }}</h3>
          <p class="organization-description" v-if="organization.description">
            {{ organization.description }}
          </p>

          <div class="organization-meta">
            <span class="meta-item" v-if="organization.type">
              <i class="icon-tag"></i>
              {{ organization.type }}
            </span>
            <span class="meta-item" v-if="organization.city">
              <i class="icon-map-pin"></i>
              {{ organization.city }}
            </span>
            <span class="meta-item" v-if="organization.staffCount">
              <i class="icon-users"></i>
              {{ organization.staffCount }} کارمند
            </span>
          </div>
        </div>

        <div class="organization-actions">
          <AppButton
            variant="primary"
            size="small"
            @click="handleRequestJoin(organization)"
          >
            درخواست پیوستن
          </AppButton>
        </div>
      </div>
    </div>

    <!-- Pagination -->
    <div v-if="totalPages > 1" class="pagination">
      <button
        class="pagination-button"
        :disabled="currentPage === 1"
        @click="handlePageChange(currentPage - 1)"
      >
        <i class="icon-chevron-right"></i>
      </button>

      <span class="pagination-info">
        صفحه {{ currentPage }} از {{ totalPages }}
      </span>

      <button
        class="pagination-button"
        :disabled="currentPage === totalPages"
        @click="handlePageChange(currentPage + 1)"
      >
        <i class="icon-chevron-left"></i>
      </button>
    </div>

    <!-- Request to Join Modal -->
    <RequestToJoinModal
      v-if="selectedOrganization"
      :organization="selectedOrganization"
      @close="selectedOrganization = null"
      @submitted="handleJoinRequestSubmitted"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import AppButton from '@/shared/components/ui/Button/AppButton.vue'
import RequestToJoinModal from './RequestToJoinModal.vue'

interface Organization {
  id: string
  businessName: string
  description?: string
  logoUrl?: string
  type?: string
  city?: string
  staffCount?: number
}

const searchQuery = ref('')
const organizations = ref<Organization[]>([])
const selectedOrganization = ref<Organization | null>(null)
const isLoading = ref(false)
const currentPage = ref(1)
const totalPages = ref(1)
const pageSize = 10

const filters = reactive({
  city: '',
  type: '',
})

onMounted(() => {
  loadOrganizations()
})

async function loadOrganizations() {
  isLoading.value = true

  try {
    // TODO: Implement actual API call to search providers
    // const result = await providerService.searchProviders({
    //   searchTerm: searchQuery.value,
    //   city: filters.city,
    //   type: filters.type,
    //   hierarchyType: 'Organization',
    //   page: currentPage.value,
    //   pageSize: pageSize,
    // })

    // Mock data for now
    await new Promise((resolve) => setTimeout(resolve, 500))

    organizations.value = [
      {
        id: '1',
        businessName: 'آرایشگاه نیلوفر',
        description: 'آرایشگاه زنانه با بیش از 10 سال سابقه',
        logoUrl: '',
        type: 'آرایشگاه',
        city: 'تهران',
        staffCount: 5,
      },
      {
        id: '2',
        businessName: 'کلینیک زیبایی رز',
        description: 'کلینیک تخصصی پوست و مو',
        logoUrl: '',
        type: 'کلینیک',
        city: 'اصفهان',
        staffCount: 8,
      },
    ]
    totalPages.value = 1
  } catch (error) {
    console.error('Error loading organizations:', error)
  } finally {
    isLoading.value = false
  }
}

function handleSearch() {
  currentPage.value = 1
  loadOrganizations()
}

function handleFilterChange() {
  currentPage.value = 1
  loadOrganizations()
}

function clearSearch() {
  searchQuery.value = ''
  handleSearch()
}

function handlePageChange(page: number) {
  currentPage.value = page
  loadOrganizations()
}

function handleRequestJoin(organization: Organization) {
  selectedOrganization.value = organization
}

function handleJoinRequestSubmitted() {
  selectedOrganization.value = null
  // Optionally show success message
}
</script>

<style scoped lang="scss">
.search-organizations {
  padding: 2rem;
}

.search-header {
  margin-bottom: 2rem;

  .search-title {
    font-size: 1.75rem;
    color: #1a202c;
    margin-bottom: 0.5rem;
  }

  .search-description {
    color: #718096;
    font-size: 1rem;
  }
}

.search-bar {
  margin-bottom: 2rem;

  .search-input-wrapper {
    position: relative;
    margin-bottom: 1rem;

    .icon-search {
      position: absolute;
      right: 1rem;
      top: 50%;
      transform: translateY(-50%);
      color: #a0aec0;
      font-size: 1.25rem;
    }

    .search-input {
      width: 100%;
      padding: 0.875rem 3rem 0.875rem 3rem;
      border: 1px solid #cbd5e0;
      border-radius: 10px;
      font-size: 1rem;
      transition: all 0.2s;

      &:focus {
        outline: none;
        border-color: #667eea;
        box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
      }

      &::placeholder {
        color: #a0aec0;
      }
    }

    .clear-button {
      position: absolute;
      left: 1rem;
      top: 50%;
      transform: translateY(-50%);
      background: none;
      border: none;
      color: #a0aec0;
      cursor: pointer;
      padding: 0.25rem;
      display: flex;
      align-items: center;
      justify-content: center;

      &:hover {
        color: #718096;
      }
    }
  }

  .search-filters {
    display: flex;
    gap: 1rem;

    .filter-select {
      flex: 1;
      padding: 0.75rem 1rem;
      border: 1px solid #cbd5e0;
      border-radius: 8px;
      font-size: 0.95rem;
      background: white;
      cursor: pointer;
      transition: all 0.2s;

      &:focus {
        outline: none;
        border-color: #667eea;
      }
    }
  }
}

.loading-state,
.empty-state {
  text-align: center;
  padding: 3rem 1rem;
  color: #718096;

  .spinner {
    width: 50px;
    height: 50px;
    border: 4px solid #f3f4f6;
    border-top-color: #667eea;
    border-radius: 50%;
    animation: spin 1s linear infinite;
    margin: 0 auto 1rem;
  }

  i {
    font-size: 3rem;
    color: #cbd5e0;
    margin-bottom: 1rem;
  }

  h3 {
    font-size: 1.25rem;
    color: #4a5568;
    margin-bottom: 0.5rem;
  }

  p {
    color: #a0aec0;
  }
}

.organizations-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.organization-card {
  background: white;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  padding: 1.5rem;
  display: flex;
  align-items: center;
  gap: 1.5rem;
  transition: all 0.2s;

  &:hover {
    border-color: #cbd5e0;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05);
  }

  .organization-logo,
  .organization-logo-placeholder {
    width: 80px;
    height: 80px;
    flex-shrink: 0;
    border-radius: 12px;
    overflow: hidden;
  }

  .organization-logo {
    img {
      width: 100%;
      height: 100%;
      object-fit: cover;
    }
  }

  .organization-logo-placeholder {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    display: flex;
    align-items: center;
    justify-content: center;
    color: white;
    font-size: 2rem;
  }

  .organization-info {
    flex: 1;

    .organization-name {
      font-size: 1.25rem;
      color: #1a202c;
      margin-bottom: 0.5rem;
      font-weight: 600;
    }

    .organization-description {
      color: #718096;
      margin-bottom: 0.75rem;
      font-size: 0.95rem;
      line-height: 1.5;
    }

    .organization-meta {
      display: flex;
      flex-wrap: wrap;
      gap: 1rem;

      .meta-item {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        color: #a0aec0;
        font-size: 0.875rem;

        i {
          color: #667eea;
        }
      }
    }
  }

  .organization-actions {
    flex-shrink: 0;
  }
}

.pagination {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 1rem;
  margin-top: 2rem;

  .pagination-button {
    padding: 0.5rem 0.75rem;
    border: 1px solid #cbd5e0;
    border-radius: 6px;
    background: white;
    cursor: pointer;
    transition: all 0.2s;

    &:hover:not(:disabled) {
      background: #f7fafc;
      border-color: #a0aec0;
    }

    &:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    i {
      font-size: 1rem;
    }
  }

  .pagination-info {
    color: #718096;
    font-size: 0.95rem;
  }
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

@media (max-width: 768px) {
  .organization-card {
    flex-direction: column;
    text-align: center;

    .organization-info {
      .organization-meta {
        justify-content: center;
      }
    }

    .organization-actions {
      width: 100%;

      button {
        width: 100%;
      }
    }
  }

  .search-filters {
    flex-direction: column;
  }
}
</style>
