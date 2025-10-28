<template>
  <Card :hoverable="true" :clickable="true" class="staff-card" @click="handleCardClick">
    <!-- Staff Avatar/Header -->
    <div class="staff-header">
      <div class="staff-avatar">
        <div class="avatar-placeholder">
          {{ initials }}
        </div>
      </div>
      <div class="staff-status">
        <Badge :class="`status-${staff.isActive ? 'active' : 'inactive'}`">
          {{ staff.isActive ? 'Active' : 'Inactive' }}
        </Badge>
      </div>
    </div>

    <!-- Staff Content -->
    <div class="staff-content">
      <h3 class="staff-name">{{ fullName }}</h3>

      <div v-if="staff.phoneNumber" class="staff-contact">
        <div class="contact-item">
          <span class="contact-icon">📱</span>
          <span class="contact-text">{{ staff.phoneNumber }}</span>
        </div>
      </div>
    </div>

    <!-- Actions Footer -->
    <template #footer>
      <div class="staff-actions">
        <Button size="small" variant="secondary" @click.stop="$emit('edit', staff.id)">
          Edit
        </Button>
        <Button size="small" variant="danger" @click.stop="$emit('delete', staff.id)">
          Remove
        </Button>
      </div>
    </template>
  </Card>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { Card, Button, Badge } from '@/shared/components'
import type { Staff } from '../../types/staff.types'

interface Props {
  staff: Staff
}

interface Emits {
  (e: 'click', staff: Staff): void
  (e: 'edit', id: string): void
  (e: 'delete', id: string): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

const fullName = computed(() => {
  return `${props.staff.firstName} ${props.staff.lastName}`.trim() || props.staff.fullName || 'Unnamed Staff'
})

const initials = computed(() => {
  const first = props.staff.firstName?.[0] || ''
  const last = props.staff.lastName?.[0] || ''
  return (first + last).toUpperCase() || '?'
})

function handleCardClick() {
  emit('click', props.staff)
}
</script>

<style scoped lang="scss">
.staff-card {
  height: 100%;
  display: flex;
  flex-direction: column;
  transition: transform 0.2s, box-shadow 0.2s;

  &:hover {
    transform: translateY(-4px);
    box-shadow: 0 12px 24px rgba(0, 0, 0, 0.15);
  }
}

.staff-header {
  position: relative;
  padding: 1.5rem 1.5rem 0 1.5rem;
  display: flex;
  justify-content: center;
}

.staff-avatar {
  width: 80px;
  height: 80px;
  border-radius: 50%;
  overflow: hidden;
  border: 3px solid #fff;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.avatar-placeholder {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  font-size: 1.75rem;
  font-weight: 700;
}

.staff-status {
  position: absolute;
  top: 1rem;
  right: 1rem;
}

.staff-content {
  padding: 1rem 1.5rem 1.5rem 1.5rem;
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  text-align: center;
}

.staff-name {
  font-size: 1.25rem;
  font-weight: 600;
  color: #1f2937;
  margin: 0;
}

.staff-contact {
  display: flex;
  justify-content: center;
  padding: 0.75rem 0;
  border-top: 1px solid #e5e7eb;
}

.contact-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.875rem;
  color: #6b7280;
}

.contact-icon {
  font-size: 1rem;
}

.status-active {
  background-color: #10b981;
  color: white;
}

.status-inactive {
  background-color: #6b7280;
  color: white;
}

.staff-actions {
  display: flex;
  gap: 0.5rem;
  padding: 1rem;
  border-top: 1px solid #e5e7eb;
  background: #f9fafb;

  button {
    flex: 1;
  }
}

@media (max-width: 640px) {
  .staff-header {
    padding: 1rem;
  }

  .staff-avatar {
    width: 64px;
    height: 64px;
  }

  .avatar-placeholder {
    font-size: 1.5rem;
  }

  .staff-actions {
    flex-direction: column;

    button {
      width: 100%;
    }
  }
}
</style>
