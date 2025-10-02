<template>
  <div :class="avatarClasses">
    <img v-if="src && !imageError" :src="src" :alt="alt" @error="handleImageError" />
    <span v-else-if="initials" class="avatar-initials">{{ displayInitials }}</span>
    <span v-else class="avatar-icon">
      <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path
          stroke-linecap="round"
          stroke-linejoin="round"
          stroke-width="2"
          d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"
        />
      </svg>
    </span>
    <span v-if="badge" class="avatar-badge" :class="`badge-${badgeVariant}`"></span>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'

interface Props {
  src?: string
  alt?: string
  initials?: string
  size?: 'small' | 'medium' | 'large' | 'xlarge'
  shape?: 'circle' | 'square'
  badge?: boolean
  badgeVariant?: 'success' | 'warning' | 'danger' | 'info'
}

const props = withDefaults(defineProps<Props>(), {
  size: 'medium',
  shape: 'circle',
  badge: false,
  badgeVariant: 'success',
})

const imageError = ref(false)

const avatarClasses = computed(() => ['avatar', `avatar-${props.size}`, `avatar-${props.shape}`])

const displayInitials = computed(() => {
  if (!props.initials) return ''
  return props.initials.slice(0, 2).toUpperCase()
})

function handleImageError() {
  imageError.value = true
}
</script>

<style scoped lang="scss">
.avatar {
  position: relative;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  font-weight: 600;
  overflow: hidden;

  img {
    width: 100%;
    height: 100%;
    object-fit: cover;
  }

  // Sizes
  &-small {
    width: 32px;
    height: 32px;
    font-size: 0.75rem;
  }

  &-medium {
    width: 48px;
    height: 48px;
    font-size: 1rem;
  }

  &-large {
    width: 64px;
    height: 64px;
    font-size: 1.25rem;
  }

  &-xlarge {
    width: 96px;
    height: 96px;
    font-size: 1.75rem;
  }

  // Shapes
  &-circle {
    border-radius: 50%;
  }

  &-square {
    border-radius: 8px;
  }
}

.avatar-initials {
  user-select: none;
}

.avatar-icon {
  width: 60%;
  height: 60%;
  display: flex;
  align-items: center;
  justify-content: center;

  svg {
    width: 100%;
    height: 100%;
  }
}

.avatar-badge {
  position: absolute;
  bottom: 0;
  right: 0;
  width: 12px;
  height: 12px;
  border-radius: 50%;
  border: 2px solid white;

  &.badge-success {
    background: #10b981;
  }

  &.badge-warning {
    background: #f59e0b;
  }

  &.badge-danger {
    background: #ef4444;
  }

  &.badge-info {
    background: #3b82f6;
  }
}
</style>
