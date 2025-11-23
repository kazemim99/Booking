<template>
  <div class="invitation-card" :class="statusClass">
    <div class="card-content">
      <!-- Left Section: Info -->
      <div class="invitation-info">
        <div class="invitee-avatar">
          <i class="icon-user"></i>
        </div>

        <div class="invitation-details">
          <h3 class="invitee-name">{{ invitation.inviteeName || 'کارمند جدید' }}</h3>
          <div class="invitee-phone" dir="ltr">{{ formatPhoneNumber(invitation.inviteePhoneNumber) }}</div>

          <div v-if="invitation.message" class="invitation-message">
            <i class="icon-message-circle"></i>
            <span>{{ invitation.message }}</span>
          </div>

          <div class="invitation-meta">
            <div class="meta-item">
              <i class="icon-calendar"></i>
              <span>ارسال شده: {{ formatDate(invitation.createdAt) }}</span>
            </div>
            <div class="meta-item">
              <i class="icon-clock"></i>
              <span>{{ expiryText }}</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Right Section: Status & Actions -->
      <div class="invitation-status">
        <div class="status-badge" :class="statusBadgeClass">
          <i :class="statusIcon"></i>
          <span>{{ statusLabel }}</span>
        </div>

        <div class="invitation-actions">
          <!-- Pending Actions -->
          <template v-if="isPending">
            <AppButton
              v-if="canResend"
              variant="outline"
              size="small"
              @click="handleResend"
              :disabled="isProcessing"
            >
              <i class="icon-refresh-cw"></i>
              ارسال مجدد
            </AppButton>
            <AppButton
              variant="danger-outline"
              size="small"
              @click="handleCancel"
              :disabled="isProcessing"
            >
              <i class="icon-x"></i>
              لغو دعوت
            </AppButton>
          </template>

          <!-- Accepted Status -->
          <template v-else-if="isAccepted">
            <div class="accepted-info">
              <i class="icon-check-circle"></i>
              <span>دعوت پذیرفته شد</span>
            </div>
          </template>

          <!-- Expired/Rejected Actions -->
          <template v-else>
            <AppButton
              variant="primary"
              size="small"
              @click="handleResend"
              :disabled="isProcessing"
            >
              <i class="icon-send"></i>
              ارسال دعوت جدید
            </AppButton>
          </template>
        </div>
      </div>
    </div>

    <!-- Expiry Warning -->
    <div v-if="isExpiringSoon && isPending" class="expiry-warning">
      <i class="icon-alert-triangle"></i>
      <span>این دعوت به زودی منقضی می‌شود!</span>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import type { ProviderInvitation } from '../../types/hierarchy.types'
import {
  InvitationStatus,
  getInvitationStatusLabel,
  isInvitationExpired,
  isInvitationPending,
} from '../../types/hierarchy.types'
import AppButton from '@/shared/components/AppButton.vue'

// ============================================
// Props & Emits
// ============================================

interface Props {
  invitation: ProviderInvitation
}

const props = defineProps<Props>()

const emit = defineEmits<{
  (e: 'resend', invitation: ProviderInvitation): void
  (e: 'cancel', invitation: ProviderInvitation): void
}>()

// ============================================
// State
// ============================================

const isProcessing = ref(false)

// ============================================
// Computed
// ============================================

const isPending = computed(() => isInvitationPending(props.invitation))

const isAccepted = computed(() => props.invitation.status === InvitationStatus.Accepted)

const isExpired = computed(() => isInvitationExpired(props.invitation))

const isExpiringSoon = computed(() => {
  if (!isPending.value) return false

  const expiryDate = new Date(props.invitation.expiresAt)
  const now = new Date()
  const hoursUntilExpiry = (expiryDate.getTime() - now.getTime()) / (1000 * 60 * 60)

  return hoursUntilExpiry <= 24 && hoursUntilExpiry > 0
})

const canResend = computed(() => {
  // Can resend if invitation is still pending and not expired yet
  return isPending.value && !isExpired.value
})

const statusClass = computed(() => {
  return `status-${props.invitation.status.toLowerCase()}`
})

const statusBadgeClass = computed(() => {
  switch (props.invitation.status) {
    case InvitationStatus.Pending:
      return 'badge-pending'
    case InvitationStatus.Accepted:
      return 'badge-accepted'
    case InvitationStatus.Rejected:
      return 'badge-rejected'
    case InvitationStatus.Expired:
      return 'badge-expired'
    default:
      return 'badge-default'
  }
})

const statusIcon = computed(() => {
  switch (props.invitation.status) {
    case InvitationStatus.Pending:
      return 'icon-clock'
    case InvitationStatus.Accepted:
      return 'icon-check-circle'
    case InvitationStatus.Rejected:
      return 'icon-x-circle'
    case InvitationStatus.Expired:
      return 'icon-alert-circle'
    default:
      return 'icon-info'
  }
})

const statusLabel = computed(() => getInvitationStatusLabel(props.invitation.status))

const expiryText = computed(() => {
  const expiryDate = new Date(props.invitation.expiresAt)
  const now = new Date()

  if (isExpired.value) {
    return 'منقضی شده'
  }

  const hoursUntilExpiry = Math.floor((expiryDate.getTime() - now.getTime()) / (1000 * 60 * 60))

  if (hoursUntilExpiry < 1) {
    const minutesUntilExpiry = Math.floor((expiryDate.getTime() - now.getTime()) / (1000 * 60))
    return `${minutesUntilExpiry} دقیقه تا انقضا`
  }

  if (hoursUntilExpiry < 24) {
    return `${hoursUntilExpiry} ساعت تا انقضا`
  }

  const daysUntilExpiry = Math.floor(hoursUntilExpiry / 24)
  return `${daysUntilExpiry} روز تا انقضا`
})

// ============================================
// Methods
// ============================================

function formatPhoneNumber(phone: string): string {
  // Format: +98 912 345 6789
  if (phone.startsWith('+98')) {
    const number = phone.substring(3)
    return `+98 ${number.substring(0, 3)} ${number.substring(3, 6)} ${number.substring(6)}`
  }
  return phone
}

function formatDate(dateString: string): string {
  const date = new Date(dateString)
  return new Intl.DateTimeFormat('fa-IR', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(date)
}

async function handleResend(): Promise<void> {
  isProcessing.value = true
  try {
    emit('resend', props.invitation)
  } finally {
    isProcessing.value = false
  }
}

async function handleCancel(): Promise<void> {
  isProcessing.value = true
  try {
    emit('cancel', props.invitation)
  } finally {
    isProcessing.value = false
  }
}
</script>

<style scoped lang="scss">
.invitation-card {
  background: #fff;
  border: 1px solid #e5e7eb;
  border-radius: 12px;
  padding: 1.5rem;
  margin-bottom: 1rem;
  transition: all 0.2s;

  &:hover {
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
  }

  &.status-expired {
    opacity: 0.7;
    background: #f9fafb;
  }

  &.status-rejected {
    opacity: 0.8;
    background: #fef2f2;
  }

  &.status-accepted {
    background: #f0fdf4;
    border-color: #bbf7d0;
  }
}

.card-content {
  display: flex;
  justify-content: space-between;
  gap: 1.5rem;

  @media (max-width: 768px) {
    flex-direction: column;
  }
}

.invitation-info {
  flex: 1;
  display: flex;
  gap: 1rem;
}

.invitee-avatar {
  width: 60px;
  height: 60px;
  background: linear-gradient(135deg, #7c3aed 0%, #9333ea 100%);
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;

  i {
    font-size: 1.75rem;
    color: #fff;
  }
}

.invitation-details {
  flex: 1;
}

.invitee-name {
  font-size: 1.125rem;
  font-weight: 700;
  color: #1a1a1a;
  margin-bottom: 0.25rem;
}

.invitee-phone {
  font-size: 0.95rem;
  color: #6b7280;
  margin-bottom: 0.75rem;
  direction: ltr;
  text-align: right;
}

.invitation-message {
  display: flex;
  align-items: flex-start;
  gap: 0.5rem;
  padding: 0.75rem;
  background: #f9fafb;
  border-radius: 8px;
  margin-bottom: 0.75rem;

  i {
    color: #7c3aed;
    font-size: 1rem;
    flex-shrink: 0;
    margin-top: 0.125rem;
  }

  span {
    font-size: 0.9rem;
    color: #4b5563;
    line-height: 1.5;
  }
}

.invitation-meta {
  display: flex;
  flex-wrap: wrap;
  gap: 1rem;
}

.meta-item {
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

.invitation-status {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 1rem;
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

  &.badge-accepted {
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

  &.badge-expired {
    background: #e5e7eb;
    color: #374151;

    i {
      color: #6b7280;
    }
  }
}

.invitation-actions {
  display: flex;
  gap: 0.75rem;

  @media (max-width: 768px) {
    width: 100%;
    flex-direction: column;
  }
}

.accepted-info {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.75rem 1rem;
  background: #d1fae5;
  border-radius: 8px;
  color: #065f46;
  font-size: 0.9rem;
  font-weight: 600;

  i {
    color: #10b981;
    font-size: 1.25rem;
  }
}

.expiry-warning {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-top: 1rem;
  padding: 0.75rem 1rem;
  background: #fef3c7;
  border: 1px solid #fcd34d;
  border-radius: 8px;
  color: #92400e;
  font-size: 0.875rem;
  font-weight: 600;

  i {
    color: #f59e0b;
    font-size: 1.125rem;
  }
}
</style>
