<template>
  <div :class="cardClasses" @click="handleClick">
    <div v-if="$slots.header || title" class="card-header">
      <slot name="header">
        <h3 v-if="title" class="card-title">{{ title }}</h3>
      </slot>
      <div v-if="$slots.actions" class="card-actions">
        <slot name="actions" />
      </div>
    </div>

    <div v-if="image" class="card-image">
      <img :src="image" :alt="imageAlt" />
    </div>

    <div class="card-body">
      <slot />
    </div>

    <div v-if="$slots.footer" class="card-footer">
      <slot name="footer" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'

interface Props {
  title?: string
  image?: string
  imageAlt?: string
  hoverable?: boolean
  bordered?: boolean
  shadow?: 'none' | 'small' | 'medium' | 'large'
  clickable?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  hoverable: false,
  bordered: true,
  shadow: 'small',
  clickable: false,
})

interface Emits {
  (event: 'click', e: MouseEvent): void
}

const emit = defineEmits<Emits>()

const cardClasses = computed(() => [
  'card',
  `card-shadow-${props.shadow}`,
  {
    'card-hoverable': props.hoverable,
    'card-bordered': props.bordered,
    'card-clickable': props.clickable,
  },
])

function handleClick(event: MouseEvent) {
  if (props.clickable) {
    emit('click', event)
  }
}
</script>

<style scoped lang="scss">
.card {
  background: var(--color-background);
  border-radius: var(--radius-2xl); /* 16px - Coliride content card */
  overflow: hidden;
  transition: border-color 0.2s ease, box-shadow 0.2s ease;

  /* Coliride defines depth with borders, not shadows */
  &-bordered {
    border: 1px solid var(--color-border); /* #e5e9f2 */
  }

  &-shadow {
    &-none {
      box-shadow: none;
    }

    &-small {
      box-shadow: var(--shadow-sm);
    }

    &-medium {
      box-shadow: var(--shadow-md);
    }

    &-large {
      box-shadow: var(--shadow-lg);
    }
  }

  &-hoverable:hover {
    border-color: var(--color-border-dark);
    box-shadow: var(--shadow-lg);
  }

  &-clickable {
    cursor: pointer;
  }
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1.25rem 1.5rem;
  border-bottom: 1px solid var(--color-border);
}

.card-title {
  font-size: 1.25rem;
  font-weight: var(--font-weight-bold);
  color: var(--color-text-primary);
  margin: 0;
}

.card-actions {
  display: flex;
  gap: 0.5rem;
  align-items: center;
}

.card-image {
  width: 100%;
  overflow: hidden;

  img {
    width: 100%;
    height: auto;
    display: block;
    object-fit: cover;
  }
}

.card-body {
  padding: 1.5rem;
}

.card-footer {
  padding: 1rem 1.5rem;
  background: var(--color-background-secondary);
  border-top: 1px solid var(--color-border);
}
</style>
