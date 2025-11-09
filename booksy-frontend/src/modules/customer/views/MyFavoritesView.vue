<template>
  <div class="my-favorites-view" dir="rtl">
    <!-- Page Header -->
    <div class="page-header">
      <h1>علاقه‌مندی‌های من</h1>
      <p class="page-description">ارائه‌دهندگان مورد علاقه خود را مدیریت کنید و به سرعت رزرو مجدد کنید</p>
    </div>

    <!-- View Toggle -->
    <div class="view-controls">
      <div class="view-toggle">
        <button
          @click="currentView = 'list'"
          :class="['toggle-btn', { active: currentView === 'list' }]"
        >
          <svg viewBox="0 0 20 20" fill="currentColor">
            <path fill-rule="evenodd" d="M3 4a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1zm0 4a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1zm0 4a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1zm0 4a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1z" clip-rule="evenodd" />
          </svg>
          <span>لیست</span>
        </button>
        <button
          @click="currentView = 'quick-rebook'"
          :class="['toggle-btn', { active: currentView === 'quick-rebook' }]"
        >
          <svg viewBox="0 0 20 20" fill="currentColor">
            <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm1-12a1 1 0 10-2 0v4a1 1 0 00.293.707l2.828 2.829a1 1 0 101.415-1.415L11 9.586V6z" clip-rule="evenodd" />
          </svg>
          <span>رزرو سریع</span>
        </button>
      </div>
    </div>

    <!-- Favorites List View -->
    <div v-if="currentView === 'list'">
      <FavoritesList />
    </div>

    <!-- Quick Rebook View -->
    <div v-else-if="currentView === 'quick-rebook'" class="quick-rebook-view">
      <div v-if="loading" class="loading">
        <div class="spinner"></div>
        <p>در حال بارگذاری پیشنهادات...</p>
      </div>

      <div v-else-if="quickRebookSuggestions.length > 0" class="suggestions-grid">
        <QuickRebookCard
          v-for="suggestion in quickRebookSuggestions"
          :key="suggestion.favorite.id"
          :suggestion="suggestion"
          @rebook="handleRebook"
          @view-provider="handleViewProvider"
          @unfavorited="handleUnfavorited"
        />
      </div>

      <div v-else class="empty-state">
        <svg viewBox="0 0 20 20" fill="currentColor">
          <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd" />
        </svg>
        <p>در حال حاضر پیشنهاد رزرو سریع وجود ندارد</p>
        <button @click="currentView = 'list'" class="btn-switch">
          مشاهده لیست علاقه‌مندی‌ها
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import FavoritesList from '../components/favorites/FavoritesList.vue'
import QuickRebookCard from '../components/favorites/QuickRebookCard.vue'
import { favoritesService } from '../services/favorites.service'
import type { QuickRebookSuggestion, TimeSlot } from '../types/favorites.types'

// ============================================================================
// Composables
// ============================================================================

const router = useRouter()
const authStore = useAuthStore()

// ============================================================================
// State
// ============================================================================

const currentView = ref<'list' | 'quick-rebook'>('list')
const loading = ref(false)
const quickRebookSuggestions = ref<QuickRebookSuggestion[]>([])

const customerId = computed(() => authStore.user?.id || '')

// ============================================================================
// Lifecycle
// ============================================================================

onMounted(() => {
  loadQuickRebookSuggestions()
})

// ============================================================================
// Methods
// ============================================================================

async function loadQuickRebookSuggestions(): Promise<void> {
  if (!customerId.value) return

  loading.value = true

  try {
    quickRebookSuggestions.value = await favoritesService.getQuickRebookSuggestions(
      customerId.value
    )
  } catch (error) {
    console.error('[MyFavoritesView] Error loading quick rebook suggestions:', error)
  } finally {
    loading.value = false
  }
}

function handleRebook(slot: TimeSlot, suggestion: QuickRebookSuggestion): void {
  console.log('[MyFavoritesView] Rebook:', slot, suggestion)
  // Navigation is handled by QuickRebookCard component
}

function handleViewProvider(providerId: string): void {
  router.push(`/customer/provider/${providerId}`)
}

function handleUnfavorited(providerId: string): void {
  // Reload quick rebook suggestions
  loadQuickRebookSuggestions()
}
</script>

<style scoped>
.my-favorites-view {
  max-width: 1400px;
  margin: 0 auto;
  padding: 2rem 1.5rem;
}

/* Page Header */
.page-header {
  margin-bottom: 2rem;
}

.page-header h1 {
  font-size: 2rem;
  font-weight: 700;
  color: #1a202c;
  margin: 0 0 0.5rem 0;
}

.page-description {
  color: #718096;
  font-size: 1rem;
  margin: 0;
}

/* View Controls */
.view-controls {
  margin-bottom: 2rem;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.view-toggle {
  display: flex;
  gap: 0.5rem;
  background: #f7fafc;
  padding: 0.375rem;
  border-radius: 0.5rem;
}

.toggle-btn {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.625rem 1.25rem;
  background: transparent;
  border: none;
  border-radius: 0.375rem;
  color: #718096;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
}

.toggle-btn svg {
  width: 1.125rem;
  height: 1.125rem;
}

.toggle-btn:hover {
  background: #e2e8f0;
  color: #2d3748;
}

.toggle-btn.active {
  background: white;
  color: #3182ce;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

/* Quick Rebook View */
.quick-rebook-view {
  min-height: 400px;
}

.suggestions-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(400px, 1fr));
  gap: 1.5rem;
}

/* Loading */
.loading {
  text-align: center;
  padding: 4rem 2rem;
  color: #718096;
}

.spinner {
  width: 3rem;
  height: 3rem;
  border: 4px solid #e2e8f0;
  border-top-color: #3182ce;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
  margin: 0 auto 1rem;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

/* Empty State */
.empty-state {
  text-align: center;
  padding: 4rem 2rem;
  color: #718096;
}

.empty-state svg {
  width: 4rem;
  height: 4rem;
  color: #cbd5e0;
  margin-bottom: 1rem;
}

.empty-state p {
  font-size: 1rem;
  margin: 0 0 1.5rem 0;
}

.btn-switch {
  padding: 0.75rem 1.5rem;
  background: #3182ce;
  color: white;
  border: none;
  border-radius: 0.5rem;
  font-weight: 500;
  cursor: pointer;
  transition: background 0.2s;
}

.btn-switch:hover {
  background: #2c5aa0;
}

/* Responsive */
@media (max-width: 768px) {
  .my-favorites-view {
    padding: 1.5rem 1rem;
  }

  .page-header h1 {
    font-size: 1.5rem;
  }

  .suggestions-grid {
    grid-template-columns: 1fr;
  }

  .view-toggle {
    width: 100%;
  }

  .toggle-btn {
    flex: 1;
    justify-content: center;
  }
}
</style>
