<template>
  <div class="my-join-requests-view">
    <div class="view-header">
      <h1 class="view-title">درخواست‌های پیوستن من</h1>
      <p class="view-description">
        مشاهده و مدیریت درخواست‌های پیوستن به سازمان‌ها
      </p>
    </div>

    <div class="requests-container">
      <!-- Loading State -->
      <div v-if="isLoading" class="loading-state">
        <div class="spinner"></div>
        <p>در حال بارگذاری...</p>
      </div>

      <!-- Empty State -->
      <div v-else-if="!isLoading && joinRequests.length === 0" class="empty-state">
        <i class="icon-inbox"></i>
        <h3>درخواستی وجود ندارد</h3>
        <p>شما هیچ درخواست پیوستنی ارسال نکرده‌اید.</p>
        <AppButton variant="primary" @click="$router.push('/provider/organizations/search')">
          <i class="icon-search"></i>
          جستجوی سازمان‌ها
        </AppButton>
      </div>

      <!-- Requests List -->
      <div v-else class="requests-list">
        <JoinRequestCard
          v-for="request in joinRequests"
          :key="request.id"
          :request="request"
          :show-organization="true"
          @cancel="handleCancelRequest(request.id)"
        />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useHierarchyStore } from '../../stores/hierarchy.store'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import AppButton from '@/shared/components/ui/Button/AppButton.vue'
import JoinRequestCard from '../../components/staff/JoinRequestCard.vue'
import type { ProviderJoinRequest } from '../../types/hierarchy.types'

const hierarchyStore = useHierarchyStore()
const authStore = useAuthStore()

const isLoading = ref(true)
const joinRequests = ref<ProviderJoinRequest[]>([])

onMounted(async () => {
  await loadJoinRequests()
})

async function loadJoinRequests() {
  isLoading.value = true

  try {
    const currentUserId = authStore.currentUser?.id
    if (currentUserId) {
      await hierarchyStore.loadSentJoinRequests(currentUserId)
      joinRequests.value = hierarchyStore.sentJoinRequests || []
    }
  } catch (error) {
    console.error('Error loading join requests:', error)
  } finally {
    isLoading.value = false
  }
}

async function handleCancelRequest(requestId: string) {
  const confirmed = confirm('آیا مطمئن هستید که می‌خواهید این درخواست را لغو کنید؟')
  if (!confirmed) return

  // TODO: Implement cancel request API call
  await loadJoinRequests()
}
</script>

<style scoped lang="scss">
.my-join-requests-view {
  padding: 2rem;
  max-width: 900px;
  margin: 0 auto;
}

.view-header {
  margin-bottom: 2rem;

  .view-title {
    font-size: 2rem;
    color: #1a202c;
    margin-bottom: 0.5rem;
  }

  .view-description {
    color: #718096;
    font-size: 1rem;
  }
}

.requests-container {
  background: white;
  border-radius: 12px;
  padding: 2rem;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.loading-state,
.empty-state {
  text-align: center;
  padding: 3rem 1rem;

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
    margin-bottom: 1.5rem;
  }
}

.requests-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

@media (max-width: 768px) {
  .my-join-requests-view {
    padding: 1rem;
  }

  .view-header .view-title {
    font-size: 1.5rem;
  }

  .requests-container {
    padding: 1rem;
  }
}
</style>
