<template>
  <div class="join-request-card" :class="statusClass">
    <div class="card-header">
      <!-- Requester Info -->
      <div class="requester-info">
        <div class="requester-avatar avatar-placeholder">
          <span>{{ initials }}</span>
        </div>

        <div class="requester-details">
          <h3 class="requester-name">{{ request.requesterName || 'درخواست‌کننده' }}</h3>
          <div class="request-date">
            <i class="icon-calendar"></i>
            <span>{{ formatDateTime(request.createdAt.toString()) }}</span>
          </div>
        </div>
      </div>

      <!-- Status Badge -->
      <div class="status-badge" :class="statusBadgeClass">
        <i :class="statusIcon"></i>
        <span>{{ statusLabel }}</span>
      </div>
    </div>

    <!-- Request Message -->
    <div v-if="request.message" class="request-message">
      <div class="message-header">
        <i class="icon-message-square"></i>
        <span>پیام درخواست:</span>
      </div>
      <p class="message-text">{{ request.message }}</p>
    </div>

    <!-- Organization Info (if available) -->
    <div v-if="request.organizationName" class="organization-info">
      <div class="org-logo">
        <img
          v-if="request.organizationLogo"
          :src="request.organizationLogo"
          :alt="request.organizationName"
        />
        <i v-else class="icon-building"></i>
      </div>
      <div class="org-details">
        <div class="org-label">سازمان:</div>
        <div class="org-name">{{ request.organizationName }}</div>
      </div>
    </div>

    <!-- Review Info (if reviewed) -->
    <div v-if="request.respondedAt" class="review-info">
      <div class="review-item">
        <i class="icon-user-check"></i>
        <span>بررسی شده توسط: {{ request.respondedByName || 'مدیر' }}</span>
      </div>
      <div class="review-item">
        <i class="icon-clock"></i>
        <span>{{ formatDateTime(request.respondedAt.toString()) }}</span>
      </div>
      <div v-if="request.rejectionReason" class="rejection-reason">
        <i class="icon-info-circle"></i>
        <span>دلیل رد: {{ request.rejectionReason }}</span>
      </div>
    </div>

    <!-- Actions -->
    <div v-if="canTakeAction" class="card-actions">
      <AppButton
        variant="danger"
        size="medium"
        @click="handleReject"
        :disabled="isProcessing"
      >
        <i class="icon-x"></i>
        رد درخواست
      </AppButton>

      <AppButton
        variant="primary"
        size="medium"
        @click="handleApprove"
        :disabled="isProcessing"
        :loading="isProcessing"
      >
        <i class="icon-check"></i>
        تأیید و افزودن به تیم
      </AppButton>
    </div>

    <!-- Pending Info -->
    <div v-else-if="isPending && !canTakeAction" class="pending-info">
      <i class="icon-clock"></i>
      <span>در انتظار بررسی توسط مدیر سازمان</span>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import type { ProviderJoinRequest } from '../../types/hierarchy.types'
import {
  JoinRequestStatus,
  getJoinRequestStatusLabel,
  canApproveJoinRequest,
} from '../../types/hierarchy.types'
import AppButton from '@/shared/components/ui/Button/AppButton.vue'
import { getNameInitials, formatDateTime } from '@/core/utils'

// ============================================
// Props & Emits
// ============================================

interface Props {
  request: ProviderJoinRequest
  canManage?: boolean // Can approve/reject
}

const props = withDefaults(defineProps<Props>(), {
  canManage: true,
})

const emit = defineEmits<{
  (e: 'approve', request: ProviderJoinRequest): void
  (e: 'reject', request: ProviderJoinRequest): void
}>()

// ============================================
// State
// ============================================

const isProcessing = ref(false)

// ============================================
// Computed
// ============================================

const isPending = computed(() => props.request.status === JoinRequestStatus.Pending)

const isApproved = computed(() => props.request.status === JoinRequestStatus.Approved)

const isRejected = computed(() => props.request.status === JoinRequestStatus.Rejected)

const canTakeAction = computed(() => {
  return props.canManage && canApproveJoinRequest(props.request)
})

const initials = computed(() => {
  return props.request.requesterName ? getNameInitials(props.request.requesterName) : '؟'
})

const statusClass = computed(() => {
  return `status-${props.request.status.toLowerCase()}`
})

const statusBadgeClass = computed(() => {
  switch (props.request.status) {
    case JoinRequestStatus.Pending:
      return 'badge-pending'
    case JoinRequestStatus.Approved:
      return 'badge-approved'
    case JoinRequestStatus.Rejected:
      return 'badge-rejected'
    default:
      return 'badge-default'
  }
})

const statusIcon = computed(() => {
  switch (props.request.status) {
    case JoinRequestStatus.Pending:
      return 'icon-clock'
    case JoinRequestStatus.Approved:
      return 'icon-check-circle'
    case JoinRequestStatus.Rejected:
      return 'icon-x-circle'
    default:
      return 'icon-info'
  }
})

const statusLabel = computed(() => getJoinRequestStatusLabel(props.request.status))

// ============================================
// Methods
// ============================================

async function handleApprove(): Promise<void> {
  isProcessing.value = true
  try {
    emit('approve', props.request)
  } finally {
    isProcessing.value = false
  }
}

async function handleReject(): Promise<void> {
  isProcessing.value = true
  try {
    emit('reject', props.request)
  } finally {
    isProcessing.value = false
  }
}
</script>

<style scoped lang="scss">
.join-request-card {
  background: #fff;
  border: 1px solid #e5e7eb;
  border-radius: 12px;
  padding: 1.5rem;
  margin-bottom: 1rem;
  transition: all 0.2s;

  &:hover {
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
  }

  &.status-approved {
    background: #f0fdf4;
    border-color: #bbf7d0;
  }

  &.status-rejected {
    opacity: 0.8;
    background: #fef2f2;
    border-color: #fecaca;
  }
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 1rem;
  gap: 1rem;

  @media (max-width: 640px) {
    flex-direction: column;
    align-items: flex-start;
  }
}

.requester-info {
  display: flex;
  gap: 1rem;
  flex: 1;
}

.requester-avatar {
  width: 60px;
  height: 60px;
  border-radius: 50%;
  overflow: hidden;
  border: 3px solid #f3f4f6;
  flex-shrink: 0;

  img {
    width: 100%;
    height: 100%;
    object-fit: cover;
  }

  &.avatar-placeholder {
    background: linear-gradient(135deg, #8b5cf6 0%, #7c3aed 100%);
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 1.5rem;
    font-weight: 700;
    color: #fff;
  }
}

.requester-details {
  flex: 1;
}

.requester-name {
  font-size: 1.125rem;
  font-weight: 700;
  color: #1a1a1a;
  margin-bottom: 0.5rem;
}

.request-date {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.875rem;
  color: #6b7280;

  i {
    font-size: 0.875rem;
    color: #9ca3af;
  }
}

.status-badge {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 1rem;
  border-radius: 20px;
  font-size: 0.875rem;
  font-weight: 600;
  white-space: nowrap;

  &.badge-pending {
    background: #fef3c7;
    color: #92400e;

    i {
      color: #f59e0b;
    }
  }

  &.badge-approved {
    background: #d1fae5;
    color: #065f46;

    i {
      color: #10b981;
    }
  }

  &.badge-rejected {
    background: #fee2e2;
    color: #991b1b;

    i {
      color: #dc2626;
    }
  }
}

.request-message {
  padding: 1rem;
  background: #f9fafb;
  border-radius: 8px;
  margin-bottom: 1rem;
}

.message-header {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.875rem;
  font-weight: 600;
  color: #4b5563;
  margin-bottom: 0.5rem;

  i {
    color: #7c3aed;
    font-size: 1rem;
  }
}

.message-text {
  font-size: 0.95rem;
  color: #1f2937;
  line-height: 1.6;
  margin: 0;
}

.organization-info {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 1rem;
  background: linear-gradient(135deg, #f8f5ff 0%, #fff 100%);
  border: 1px solid #e9d5ff;
  border-radius: 8px;
  margin-bottom: 1rem;
}

.org-logo {
  width: 48px;
  height: 48px;
  border-radius: 8px;
  overflow: hidden;
  background: #fff;
  border: 2px solid #e9d5ff;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;

  img {
    width: 100%;
    height: 100%;
    object-fit: cover;
  }

  i {
    font-size: 1.5rem;
    color: #7c3aed;
  }
}

.org-details {
  flex: 1;
}

.org-label {
  font-size: 0.875rem;
  color: #6b7280;
  margin-bottom: 0.25rem;
}

.org-name {
  font-size: 1rem;
  font-weight: 600;
  color: #1a1a1a;
}

.review-info {
  padding: 1rem;
  background: #f3f4f6;
  border-radius: 8px;
  margin-bottom: 1rem;
}

.review-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.875rem;
  color: #4b5563;
  margin-bottom: 0.5rem;

  &:last-child {
    margin-bottom: 0;
  }

  i {
    color: #6b7280;
    font-size: 0.875rem;
  }
}

.rejection-reason {
  margin-top: 0.75rem;
  padding: 0.75rem;
  background: #fee2e2;
  border-radius: 6px;
  display: flex;
  align-items: flex-start;
  gap: 0.5rem;

  i {
    color: #dc2626;
    font-size: 1rem;
    flex-shrink: 0;
    margin-top: 0.125rem;
  }

  span {
    font-size: 0.875rem;
    color: #991b1b;
    line-height: 1.5;
  }
}

.card-actions {
  display: flex;
  gap: 1rem;
  margin-top: 1.5rem;

  @media (max-width: 640px) {
    flex-direction: column-reverse;
  }
}

.pending-info {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 1rem;
  background: #fef3c7;
  border-radius: 8px;
  color: #92400e;
  font-size: 0.9rem;
  font-weight: 600;
  margin-top: 1rem;

  i {
    color: #f59e0b;
    font-size: 1.125rem;
  }
}
</style>
