<template>
  <div class="invitation-card" :class="`status-${invitation.status.toLowerCase()}`">
    <div class="invitation-header">
      <div class="invitee-info">
        <h4 class="invitee-name">{{ invitation.inviteeName || 'بدون نام' }}</h4>
        <p class="invitee-phone">{{ formatPhone(invitation.inviteePhoneNumber) }}</p>
      </div>
      <div class="status-badge" :class="`badge-${invitation.status.toLowerCase()}`">
        {{ getStatusLabel(invitation.status) }}
      </div>
    </div>

    <div v-if="invitation.message" class="invitation-message">
      <i class="icon-message"></i>
      <p>{{ invitation.message }}</p>
    </div>

    <div class="invitation-meta">
      <div class="meta-item">
        <i class="icon-calendar"></i>
        <span>ارسال شده: {{ formatDate(invitation.sentAt) }}</span>
      </div>
      <div class="meta-item" :class="{ expired: isExpired }">
        <i class="icon-clock"></i>
        <span>{{ expiryText }}</span>
      </div>
    </div>

    <div class="invitation-actions">
      <!-- Resend Button (only for expired or pending invitations) -->
      <button
        v-if="canResend"
        class="btn-action btn-resend"
        @click="handleResend"
        :disabled="isResending"
      >
        <i class="icon-refresh-cw"></i>
        {{ isResending ? 'در حال ارسال...' : 'ارسال مجدد' }}
      </button>

      <!-- Cancel Button (only for pending invitations) -->
      <button
        v-if="invitation.status === 'Pending' && !isExpired"
        class="btn-action btn-cancel"
        @click="handleCancel"
      >
        <i class="icon-x"></i>
        لغو دعوت
      </button>

      <!-- View Details Button -->
      <button class="btn-action btn-details" @click="handleViewDetails">
        <i class="icon-eye"></i>
        جزئیات
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import type { ProviderInvitation } from '../../types/hierarchy.types'
import { InvitationStatus } from '../../types/hierarchy.types'

interface Props {
  invitation: ProviderInvitation
}

interface Emits {
  (e: 'resend', invitationId: string): void
  (e: 'cancel', invitationId: string): void
  (e: 'view-details', invitation: ProviderInvitation): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

const isResending = ref(false)

const isExpired = computed(() => {
  return props.invitation.status === InvitationStatus.Expired ||
    (props.invitation.status === InvitationStatus.Pending &&
     new Date(props.invitation.expiresAt) < new Date())
})

const canResend = computed(() => {
  return isExpired.value || props.invitation.status === InvitationStatus.Rejected
})

const expiryText = computed(() => {
  if (isExpired.value) {
    return 'منقضی شده'
  }

  const expiry = new Date(props.invitation.expiresAt)
  const now = new Date()
  const daysLeft = Math.ceil((expiry.getTime() - now.getTime()) / (1000 * 60 * 60 * 24))

  if (daysLeft === 0) {
    return 'امروز منقضی می‌شود'
  } else if (daysLeft === 1) {
    return 'فردا منقضی می‌شود'
  } else {
    return `${convertToPersian(daysLeft)} روز تا انقضا`
  }
})

function getStatusLabel(status: InvitationStatus): string {
  const labels: Record<InvitationStatus, string> = {
    [InvitationStatus.Pending]: 'در انتظار',
    [InvitationStatus.Accepted]: 'پذیرفته شده',
    [InvitationStatus.Rejected]: 'رد شده',
    [InvitationStatus.Expired]: 'منقضی شده',
    [InvitationStatus.Cancelled]: 'لغو شده',
  }
  return labels[status] || status
}

function formatPhone(phone: string): string {
  // Format as +98 912 345 6789
  if (phone.startsWith('+98')) {
    return phone
  }
  if (phone.startsWith('0')) {
    phone = phone.substring(1)
  }
  return `+۹۸ ${convertToPersian(phone)}`
}

function formatDate(dateString: string | Date | null | undefined): string {
  if (!dateString) {
    return 'نامشخص'
  }

  const date = new Date(dateString)

  // Check if date is valid
  if (isNaN(date.getTime())) {
    console.warn('Invalid date value:', dateString)
    return 'نامشخص'
  }

  return new Intl.DateTimeFormat('fa-IR', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  }).format(date)
}

function convertToPersian(value: string | number): string {
  const persianDigits = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹']
  return value.toString().split('').map(char => {
    const digit = parseInt(char)
    return !isNaN(digit) ? persianDigits[digit] : char
  }).join('')
}

async function handleResend() {
  if (isResending.value) return

  const confirmed = confirm('آیا مطمئن هستید که می‌خواهید دعوت را مجدداً ارسال کنید؟')
  if (!confirmed) return

  isResending.value = true
  try {
    emit('resend', props.invitation.id)
  } finally {
    setTimeout(() => {
      isResending.value = false
    }, 1000)
  }
}

function handleCancel() {
  const confirmed = confirm('آیا مطمئن هستید که می‌خواهید این دعوت را لغو کنید؟')
  if (confirmed) {
    emit('cancel', props.invitation.id)
  }
}

function handleViewDetails() {
  emit('view-details', props.invitation)
}
</script>

<style scoped lang="scss">
.invitation-card {
  background: white;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  padding: 1.5rem;
  transition: all 0.2s;

  &:hover {
    border-color: #cbd5e0;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05);
  }

  &.status-expired,
  &.status-rejected {
    opacity: 0.75;
  }
}

.invitation-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 1rem;
}

.invitee-info {
  flex: 1;

  .invitee-name {
    font-size: 1.125rem;
    font-weight: 600;
    color: #1a202c;
    margin: 0 0 0.25rem 0;
  }

  .invitee-phone {
    font-size: 0.875rem;
    color: #718096;
    margin: 0;
    direction: ltr;
    text-align: right;
  }
}

.status-badge {
  padding: 0.375rem 0.875rem;
  border-radius: 12px;
  font-size: 0.75rem;
  font-weight: 600;
  white-space: nowrap;

  &.badge-pending {
    background: #fef3c7;
    color: #92400e;
  }

  &.badge-accepted {
    background: #d1fae5;
    color: #065f46;
  }

  &.badge-rejected {
    background: #fee2e2;
    color: #991b1b;
  }

  &.badge-expired {
    background: #f3f4f6;
    color: #6b7280;
  }

  &.badge-cancelled {
    background: #f3f4f6;
    color: #6b7280;
  }
}

.invitation-message {
  display: flex;
  align-items: flex-start;
  gap: 0.75rem;
  background: #f7fafc;
  border-radius: 8px;
  padding: 1rem;
  margin-bottom: 1rem;

  i {
    color: #667eea;
    font-size: 1.125rem;
    margin-top: 0.125rem;
  }

  p {
    flex: 1;
    color: #4a5568;
    margin: 0;
    font-size: 0.95rem;
    line-height: 1.6;
  }
}

.invitation-meta {
  display: flex;
  flex-wrap: wrap;
  gap: 1.5rem;
  margin-bottom: 1rem;
  padding-bottom: 1rem;
  border-bottom: 1px solid #e2e8f0;

  .meta-item {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    color: #64748b;
    font-size: 0.875rem;

    i {
      color: #a0aec0;
    }

    &.expired {
      color: #ef4444;

      i {
        color: #ef4444;
      }
    }
  }
}

.invitation-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 0.75rem;

  .btn-action {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.5rem 1rem;
    border: 1px solid #e2e8f0;
    border-radius: 8px;
    background: white;
    color: #4a5568;
    font-size: 0.875rem;
    font-weight: 500;
    cursor: pointer;
    transition: all 0.2s;

    i {
      font-size: 1rem;
    }

    &:hover:not(:disabled) {
      background: #f7fafc;
      border-color: #cbd5e0;
    }

    &:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    &.btn-resend {
      border-color: #667eea;
      color: #667eea;

      &:hover:not(:disabled) {
        background: #ebf4ff;
      }
    }

    &.btn-cancel {
      border-color: #ef4444;
      color: #ef4444;

      &:hover:not(:disabled) {
        background: #fef2f2;
      }
    }

    &.btn-details {
      border-color: #10b981;
      color: #10b981;

      &:hover:not(:disabled) {
        background: #f0fdf4;
      }
    }
  }
}

@media (max-width: 640px) {
  .invitation-header {
    flex-direction: column;
    gap: 0.75rem;
  }

  .invitation-meta {
    flex-direction: column;
    gap: 0.75rem;
  }

  .invitation-actions {
    .btn-action {
      flex: 1;
      justify-content: center;
    }
  }
}
</style>
