<template>
  <nav class="breadcrumb" aria-label="Breadcrumb">
    <ol class="breadcrumb-list">
      <!-- Home Link -->
      <li class="breadcrumb-item">
        <router-link to="/" class="breadcrumb-link home-link">
          <svg
            class="home-icon"
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6"
            />
          </svg>
          <span class="home-text">Home</span>
        </router-link>
      </li>

      <!-- Separator -->
      <li v-if="breadcrumbs.length > 0" class="breadcrumb-separator" aria-hidden="true">
        <svg
          xmlns="http://www.w3.org/2000/svg"
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
        >
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
        </svg>
      </li>

      <!-- Breadcrumb Items -->
      <template v-for="(item, index) in breadcrumbs" :key="item.path">
        <li class="breadcrumb-item">
          <!-- Link for non-last items -->
          <router-link
            v-if="index < breadcrumbs.length - 1"
            :to="item.path"
            class="breadcrumb-link"
          >
            {{ item.label }}
          </router-link>
          <!-- Current page (last item) -->
          <span v-else class="breadcrumb-current" aria-current="page">
            {{ item.label }}
          </span>
        </li>

        <!-- Separator (not for last item) -->
        <li v-if="index < breadcrumbs.length - 1" class="breadcrumb-separator" aria-hidden="true">
          <svg
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M9 5l7 7-7 7"
            />
          </svg>
        </li>
      </template>
    </ol>
  </nav>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'

defineOptions({
  name: 'BreadcrumbNav'
})

interface Breadcrumb {
  label: string
  path: string
}

const route = useRoute()

// Generate breadcrumbs from route
const breadcrumbs = computed<Breadcrumb[]>(() => {
  // Check if route has custom breadcrumbs in meta
  if (route.meta.breadcrumbs) {
    return route.meta.breadcrumbs as Breadcrumb[]
  }

  // Auto-generate breadcrumbs from path
  const pathArray = route.path.split('/').filter((p) => p)
  const crumbs: Breadcrumb[] = []

  pathArray.forEach((segment, index) => {
    const path = '/' + pathArray.slice(0, index + 1).join('/')
    const label = formatLabel(segment)

    crumbs.push({
      label,
      path,
    })
  })

  return crumbs
})

// Format segment into readable label
function formatLabel(segment: string): string {
  // Replace hyphens and underscores with spaces
  let label = segment.replace(/[-_]/g, ' ')

  // Capitalize first letter of each word
  label = label.replace(/\b\w/g, (char) => char.toUpperCase())

  return label
}
</script>

<style scoped lang="scss">
.breadcrumb {
  display: flex;
  align-items: center;
}

.breadcrumb-list {
  display: flex;
  align-items: center;
  list-style: none;
  margin: 0;
  padding: 0;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.breadcrumb-item {
  display: flex;
  align-items: center;
}

.breadcrumb-link {
  color: #6b7280;
  text-decoration: none;
  font-size: 0.875rem;
  transition: color 0.2s;
  display: flex;
  align-items: center;
  gap: 0.375rem;

  &:hover {
    color: #667eea;
  }

  &.home-link {
    font-weight: 500;
  }
}

.home-icon {
  width: 18px;
  height: 18px;
}

.home-text {
  @media (max-width: 640px) {
    display: none;
  }
}

.breadcrumb-current {
  color: #374151;
  font-size: 0.875rem;
  font-weight: 600;
}

.breadcrumb-separator {
  display: flex;
  align-items: center;
  color: #d1d5db;

  svg {
    width: 16px;
    height: 16px;
  }
}

// Responsive
@media (max-width: 768px) {
  .breadcrumb-list {
    gap: 0.375rem;
  }

  .breadcrumb-link,
  .breadcrumb-current {
    font-size: 0.8125rem;
  }
}
</style>
