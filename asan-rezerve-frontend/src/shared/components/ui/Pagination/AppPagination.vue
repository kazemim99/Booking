<template>
  <nav class="pagination" role="navigation" aria-label="Pagination">
    <div class="pagination-container">
      <!-- Previous Button -->
      <button
        class="pagination-btn pagination-prev"
        :disabled="!hasPreviousPage || disabled"
        :aria-label="previousLabel"
        @click="handlePrevious"
      >
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
            d="M15 19l-7-7 7-7"
          />
        </svg>
        <span v-if="showLabels">{{ previousLabel }}</span>
      </button>

      <!-- Page Numbers -->
      <div class="pagination-pages" v-if="showPageNumbers">
        <!-- First Page -->
        <button
          v-if="showFirstLast && currentPage > 2"
          class="pagination-page"
          :aria-label="`Go to page 1`"
          @click="handlePageClick(1)"
        >
          1
        </button>

        <!-- First Ellipsis -->
        <span v-if="showFirstEllipsis" class="pagination-ellipsis" aria-hidden="true"> ... </span>

        <!-- Page Buttons -->
        <button
          v-for="page in visiblePages"
          :key="page"
          class="pagination-page"
          :class="{ active: page === currentPage }"
          :aria-label="`Go to page ${page}`"
          :aria-current="page === currentPage ? 'page' : undefined"
          @click="handlePageClick(page)"
        >
          {{ page }}
        </button>

        <!-- Last Ellipsis -->
        <span v-if="showLastEllipsis" class="pagination-ellipsis" aria-hidden="true"> ... </span>

        <!-- Last Page -->
        <button
          v-if="showFirstLast && currentPage < totalPages - 1"
          class="pagination-page"
          :aria-label="`Go to page ${totalPages}`"
          @click="handlePageClick(totalPages)"
        >
          {{ totalPages }}
        </button>
      </div>

      <!-- Page Info (Alternative to page numbers) -->
      <div v-else class="pagination-info">
        <span class="current-page">{{ currentPage }}</span>
        <span class="separator">/</span>
        <span class="total-pages">{{ totalPages }}</span>
      </div>

      <!-- Next Button -->
      <button
        class="pagination-btn pagination-next"
        :disabled="!hasNextPage || disabled"
        :aria-label="nextLabel"
        @click="handleNext"
      >
        <span v-if="showLabels">{{ nextLabel }}</span>
        <svg
          xmlns="http://www.w3.org/2000/svg"
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
        >
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
        </svg>
      </button>
    </div>

    <!-- Results Info (Optional) -->
    <div v-if="showResultsInfo && totalCount > 0" class="pagination-results-info">
      Showing {{ startItem }} to {{ endItem }} of {{ totalCount.toLocaleString() }} results
    </div>
  </nav>
</template>

<script setup lang="ts">
import { computed } from 'vue'

// Props
interface Props {
  currentPage: number
  totalPages: number
  pageSize?: number
  totalCount?: number
  hasPreviousPage?: boolean
  hasNextPage?: boolean
  disabled?: boolean
  showPageNumbers?: boolean
  showFirstLast?: boolean
  showLabels?: boolean
  showResultsInfo?: boolean
  maxVisiblePages?: number
  previousLabel?: string
  nextLabel?: string
}

const props = withDefaults(defineProps<Props>(), {
  pageSize: 10,
  totalCount: 0,
  hasPreviousPage: undefined,
  hasNextPage: undefined,
  disabled: false,
  showPageNumbers: true,
  showFirstLast: true,
  showLabels: true,
  showResultsInfo: false,
  maxVisiblePages: 5,
  previousLabel: 'Previous',
  nextLabel: 'Next',
})

// Emits
const emit = defineEmits<{
  (e: 'update:currentPage', page: number): void
  (e: 'change', page: number): void
  (e: 'previous'): void
  (e: 'next'): void
}>()

// Computed
const hasPrev = computed(() => {
  return props.hasPreviousPage !== undefined ? props.hasPreviousPage : props.currentPage > 1
})

const hasNext = computed(() => {
  return props.hasNextPage !== undefined ? props.hasNextPage : props.currentPage < props.totalPages
})

const startItem = computed(() => {
  return (props.currentPage - 1) * props.pageSize + 1
})

const endItem = computed(() => {
  const end = props.currentPage * props.pageSize
  return Math.min(end, props.totalCount)
})

// Calculate visible page numbers
const visiblePages = computed(() => {
  const pages: number[] = []
  const maxVisible = props.maxVisiblePages
  const total = props.totalPages
  const current = props.currentPage

  if (total <= maxVisible) {
    // Show all pages if total is less than max
    for (let i = 1; i <= total; i++) {
      pages.push(i)
    }
  } else {
    // Calculate start and end of visible range
    let start = Math.max(1, current - Math.floor(maxVisible / 2))
    const end = Math.min(total, start + maxVisible - 1)

    // Adjust start if we're near the end
    if (end - start < maxVisible - 1) {
      start = Math.max(1, end - maxVisible + 1)
    }

    for (let i = start; i <= end; i++) {
      pages.push(i)
    }
  }

  return pages
})

const showFirstEllipsis = computed(() => {
  return props.showFirstLast && visiblePages.value.length > 0 && visiblePages.value[0] > 2
})

const showLastEllipsis = computed(() => {
  return (
    props.showFirstLast &&
    visiblePages.value.length > 0 &&
    visiblePages.value[visiblePages.value.length - 1] < props.totalPages - 1
  )
})

// Methods
const handlePrevious = () => {
  if (!hasPrev.value || props.disabled) return

  const newPage = props.currentPage - 1
  emit('update:currentPage', newPage)
  emit('change', newPage)
  emit('previous')
}

const handleNext = () => {
  if (!hasNext.value || props.disabled) return

  const newPage = props.currentPage + 1
  emit('update:currentPage', newPage)
  emit('change', newPage)
  emit('next')
}

const handlePageClick = (page: number) => {
  if (page === props.currentPage || props.disabled) return

  emit('update:currentPage', page)
  emit('change', page)
}
</script>

<style scoped>
.pagination {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1rem;
}

.pagination-container {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

/* Buttons */
.pagination-btn {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 1rem;
  border: 1px solid var(--color-border);
  background: white;
  border-radius: 8px;
  font-size: 0.95rem;
  font-weight: 500;
  color: var(--color-text-primary);
  cursor: pointer;
  transition: all 0.2s;
  min-height: 40px;
}

.pagination-btn svg {
  width: 16px;
  height: 16px;
  flex-shrink: 0;
}

.pagination-btn:not(:disabled):hover {
  background: var(--color-bg-secondary);
  border-color: var(--color-primary);
  color: var(--color-primary);
}

.pagination-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
  background: var(--color-bg-secondary);
}

/* Page Numbers */
.pagination-pages {
  display: flex;
  align-items: center;
  gap: 0.25rem;
}

.pagination-page {
  min-width: 40px;
  height: 40px;
  padding: 0.5rem;
  border: 1px solid var(--color-border);
  background: white;
  border-radius: 8px;
  font-size: 0.95rem;
  font-weight: 500;
  color: var(--color-text-primary);
  cursor: pointer;
  transition: all 0.2s;
  display: flex;
  align-items: center;
  justify-content: center;
}

.pagination-page:hover {
  background: var(--color-bg-secondary);
  border-color: var(--color-primary);
  color: var(--color-primary);
}

.pagination-page.active {
  background: var(--color-primary);
  border-color: var(--color-primary);
  color: white;
  cursor: default;
}

.pagination-ellipsis {
  padding: 0.5rem;
  color: var(--color-text-tertiary);
  font-weight: 500;
  user-select: none;
}

/* Simple Info Display */
.pagination-info {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 1rem;
  font-size: 1rem;
  font-weight: 500;
}

.current-page {
  color: var(--color-primary);
  font-size: 1.25rem;
  font-weight: 600;
}

.separator {
  color: var(--color-text-tertiary);
}

.total-pages {
  color: var(--color-text-secondary);
}

/* Results Info */
.pagination-results-info {
  font-size: 0.9rem;
  color: var(--color-text-secondary);
  text-align: center;
}

/* Responsive */
@media (max-width: 768px) {
  .pagination-container {
    flex-wrap: wrap;
    justify-content: center;
  }

  .pagination-btn span {
    display: none;
  }

  .pagination-btn {
    padding: 0.5rem;
    min-width: 40px;
    justify-content: center;
  }

  .pagination-pages {
    order: -1;
    width: 100%;
    justify-content: center;
    flex-wrap: wrap;
  }

  .pagination-page {
    min-width: 36px;
    height: 36px;
    font-size: 0.85rem;
  }
}

@media (max-width: 480px) {
  .pagination-page {
    min-width: 32px;
    height: 32px;
    font-size: 0.8rem;
  }

  .pagination-btn svg {
    width: 14px;
    height: 14px;
  }
}
</style>
