<template>
  <Card :hoverable="true" :clickable="true" class="service-card" @click="handleCardClick">
    <div v-if="service.imageUrl" class="service-image">
      <img :src="service.imageUrl" :alt="service.name" />
      <Badge v-if="service.status" :class="`status-${service.status.toLowerCase()}`">
        {{ service.status }}
      </Badge>
    </div>

    <div class="service-content">
      <div class="service-header">
        <h3 class="service-name">{{ service.name }}</h3>
        <Badge v-if="!service.imageUrl && service.status" :class="`status-${service.status.toLowerCase()}`">
          {{ service.status }}
        </Badge>
      </div>

      <p class="service-description">{{ truncatedDescription }}</p>

      <div class="service-meta">
        <div class="service-category">
          <span class="meta-icon">🏷️</span>
          <span class="meta-text">{{ getCategoryLabel(service.category) }}</span>
        </div>
        <div class="service-duration">
          <span class="meta-icon">⏱️</span>
          <span class="meta-text">{{ service.duration }} min</span>
        </div>
      </div>

      <div class="service-price">
        <span class="price-amount">{{ formatPrice(service.basePrice, service.currency) }}</span>
        <span class="price-label">Base Price</span>
      </div>

      <div v-if="service.tags && service.tags.length > 0" class="service-tags">
        <span v-for="tag in service.tags.slice(0, 3)" :key="tag" class="tag">
          {{ tag }}
        </span>
        <span v-if="service.tags.length > 3" class="tag-more">
          +{{ service.tags.length - 3 }}
        </span>
      </div>
    </div>

    <template #footer>
      <div class="service-actions">
        <Button
          v-if="service.status === 'Draft'"
          size="small"
          variant="primary"
          @click.stop="$emit('activate', service.id)"
        >
          Activate
        </Button>
        <Button
          v-else-if="service.status === 'Active'"
          size="small"
          variant="secondary"
          @click.stop="$emit('deactivate', service.id)"
        >
          Deactivate
        </Button>
        <Button size="small" variant="secondary" @click.stop="$emit('edit', service.id)">
          Edit
        </Button>
        <Button size="small" variant="danger" @click.stop="$emit('delete', service.id)">
          Delete
        </Button>
      </div>
    </template>
  </Card>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { Card, Button, Badge } from '@/shared/components'
import type { Service, ServiceCategory } from '../../types/service.types'
import { SERVICE_CATEGORY_LABELS } from '../../types/service.types'

interface Props {
  service: Service
}

interface Emits {
  (event: 'click', service: Service): void
  (event: 'edit', serviceId: string): void
  (event: 'delete', serviceId: string): void
  (event: 'activate', serviceId: string): void
  (event: 'deactivate', serviceId: string): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

const truncatedDescription = computed(() => {
  if (props.service.description.length > 120) {
    return props.service.description.substring(0, 120) + '...'
  }
  return props.service.description
})

function getCategoryLabel(category: ServiceCategory): string {
  return SERVICE_CATEGORY_LABELS[category] || category
}

function formatPrice(price: number, currency: string): string {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: currency || 'USD',
  }).format(price)
}

function handleCardClick(): void {
  emit('click', props.service)
}
</script>

<style scoped lang="scss">
.service-card {
  height: 100%;
  display: flex;
  flex-direction: column;
  transition: all 0.2s ease;

  &:hover {
    transform: translateY(-4px);
    box-shadow: 0 12px 24px rgba(0, 0, 0, 0.15);
  }
}

.service-image {
  position: relative;
  width: 100%;
  height: 200px;
  overflow: hidden;
  background: #f3f4f6;

  img {
    width: 100%;
    height: 100%;
    object-fit: cover;
  }

  .status {
    position: absolute;
    top: 0.75rem;
    right: 0.75rem;
  }
}

.service-content {
  padding: 1.25rem;
  flex: 1;
  display: flex;
  flex-direction: column;
}

.service-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 0.5rem;
}

.service-name {
  font-size: 1.125rem;
  font-weight: 600;
  color: #1f2937;
  margin: 0;
  flex: 1;
}

.service-description {
  font-size: 0.875rem;
  color: #6b7280;
  margin: 0 0 1rem 0;
  line-height: 1.5;
  flex-grow: 1;
}

.service-meta {
  display: flex;
  gap: 1rem;
  margin-bottom: 0.75rem;
  font-size: 0.875rem;

  .service-category,
  .service-duration {
    display: flex;
    align-items: center;
    gap: 0.375rem;
    color: #6b7280;
  }

  .meta-icon {
    font-size: 1rem;
  }
}

.service-price {
  display: flex;
  flex-direction: column;
  margin-bottom: 0.75rem;
  padding: 0.75rem;
  background: #f9fafb;
  border-radius: 8px;

  .price-amount {
    font-size: 1.5rem;
    font-weight: 700;
    color: #3b82f6;
  }

  .price-label {
    font-size: 0.75rem;
    color: #6b7280;
    text-transform: uppercase;
    letter-spacing: 0.05em;
  }
}

.service-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  margin-top: auto;

  .tag {
    font-size: 0.75rem;
    padding: 0.25rem 0.625rem;
    background: #e5e7eb;
    color: #4b5563;
    border-radius: 4px;
  }

  .tag-more {
    font-size: 0.75rem;
    padding: 0.25rem 0.625rem;
    color: #6b7280;
  }
}

.service-actions {
  display: flex;
  gap: 0.5rem;
  justify-content: flex-end;
  flex-wrap: wrap;
}

// Status badge styles
:deep(.status-draft) {
  background: #fef3c7;
  color: #92400e;
}

:deep(.status-active) {
  background: #d1fae5;
  color: #065f46;
}

:deep(.status-inactive) {
  background: #fee2e2;
  color: #991b1b;
}

:deep(.status-archived) {
  background: #e5e7eb;
  color: #374151;
}

@media (max-width: 768px) {
  .service-actions {
    flex-direction: column;

    button {
      width: 100%;
    }
  }
}
</style>
