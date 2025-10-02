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
  background: white;
  border-radius: 12px;
  overflow: hidden;
  transition: all 0.3s ease;

  &-bordered {
    border: 1px solid #e5e7eb;
  }

  &-shadow {
    &-none {
      box-shadow: none;
    }

    &-small {
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
    }

    &-medium {
      box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
    }

    &-large {
      box-shadow: 0 10px 25px rgba(0, 0, 0, 0.15);
    }
  }

  &-hoverable:hover {
    transform: translateY(-4px);
    box-shadow: 0 12px 30px rgba(0, 0, 0, 0.2);
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
  border-bottom: 1px solid #e5e7eb;
}

.card-title {
  font-size: 1.25rem;
  font-weight: 600;
  color: #1f2937;
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
  background: #f9fafb;
  border-top: 1px solid #e5e7eb;
}
</style>
