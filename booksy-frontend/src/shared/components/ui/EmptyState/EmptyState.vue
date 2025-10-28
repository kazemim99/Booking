<template>
  <div :class="emptyStateClasses">
    <div class="empty-state-icon">
      <slot name="icon">
        <svg
          v-if="!icon"
          class="default-icon"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4"
          />
        </svg>
        <span v-else class="custom-icon">{{ icon }}</span>
      </slot>
    </div>

    <div class="empty-state-content">
      <h3 v-if="title" class="empty-state-title">{{ title }}</h3>
      <p v-if="description" class="empty-state-description">{{ description }}</p>

      <div v-if="$slots.default" class="empty-state-body">
        <slot />
      </div>
    </div>

    <div v-if="$slots.actions" class="empty-state-actions">
      <slot name="actions" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'

interface Props {
  title?: string
  description?: string
  icon?: string
  size?: 'small' | 'medium' | 'large'
  centered?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  size: 'medium',
  centered: true,
})

const emptyStateClasses = computed(() => [
  'empty-state',
  `empty-state-${props.size}`,
  {
    'empty-state-centered': props.centered,
  },
])
</script>

<style scoped lang="scss">
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 3rem 1.5rem;

  &-centered {
    justify-content: center;
    text-align: center;
  }

  &-small {
    padding: 2rem 1rem;

    .empty-state-icon {
      width: 48px;
      height: 48px;
    }

    .empty-state-title {
      font-size: 1rem;
    }

    .empty-state-description {
      font-size: 0.875rem;
    }
  }

  &-medium {
    padding: 3rem 1.5rem;

    .empty-state-icon {
      width: 64px;
      height: 64px;
    }

    .empty-state-title {
      font-size: 1.25rem;
    }

    .empty-state-description {
      font-size: 1rem;
    }
  }

  &-large {
    padding: 4rem 2rem;

    .empty-state-icon {
      width: 96px;
      height: 96px;
    }

    .empty-state-title {
      font-size: 1.5rem;
    }

    .empty-state-description {
      font-size: 1.125rem;
    }
  }
}

.empty-state-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 1.5rem;
  color: #9ca3af;

  .default-icon {
    width: 100%;
    height: 100%;
  }

  .custom-icon {
    font-size: 3rem;
  }
}

.empty-state-content {
  max-width: 32rem;
}

.empty-state-title {
  font-weight: 600;
  color: #1f2937;
  margin: 0 0 0.5rem 0;
}

.empty-state-description {
  color: #6b7280;
  margin: 0 0 1.5rem 0;
  line-height: 1.6;
}

.empty-state-body {
  margin-bottom: 1.5rem;
}

.empty-state-actions {
  display: flex;
  gap: 0.75rem;
  flex-wrap: wrap;
  justify-content: center;
}
</style>
