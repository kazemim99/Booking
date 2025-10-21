<template>
  <div :class="cardClasses" @mouseenter="handleHover" @mouseleave="handleLeave">
    <!-- Icon Circle -->
    <div class="stat-icon">
      <component :is="iconComponent" />
    </div>

    <!-- Stat Content -->
    <div class="stat-content">
      <p class="stat-title">{{ title }}</p>
      <div class="stat-value-row">
        <h3 class="stat-value">
          {{ value }}
          <span v-if="suffix" class="stat-suffix">{{ suffix }}</span>
        </h3>
      </div>

      <!-- Trend Indicator -->
      <div v-if="trend" class="stat-trend" :class="trendClass">
        <svg v-if="trendDirection === 'up'" viewBox="0 0 24 24" fill="none" stroke="currentColor">
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M13 7h8m0 0v8m0-8l-8 8-4-4-6 6"
          />
        </svg>
        <svg v-else-if="trendDirection === 'down'" viewBox="0 0 24 24" fill="none" stroke="currentColor">
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M13 17h8m0 0V9m0 8l-8-8-4 4-6-6"
          />
        </svg>
        <svg v-else viewBox="0 0 24 24" fill="none" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 12h14" />
        </svg>
        <span>{{ trend }}</span>
      </div>
    </div>

    <!-- Decorative gradient overlay -->
    <div class="gradient-overlay"></div>
  </div>
</template>

<script setup lang="ts">
import { computed, h, ref } from 'vue'

interface Props {
  title: string
  value: string | number
  icon?: 'calendar' | 'dollar' | 'clock' | 'star' | 'users' | 'chart'
  trend?: string
  suffix?: string
  variant?: 'primary' | 'success' | 'warning' | 'danger' | 'info'
}

const props = withDefaults(defineProps<Props>(), {
  icon: 'chart',
  variant: 'primary',
})

const isHovered = ref(false)

const handleHover = () => {
  isHovered.value = true
}

const handleLeave = () => {
  isHovered.value = false
}

const cardClasses = computed(() => [
  'quick-stats-card',
  `variant-${props.variant}`,
  { hovered: isHovered.value },
])

const trendDirection = computed(() => {
  if (!props.trend) return null
  const normalized = props.trend.toLowerCase()
  if (normalized.includes('+') || normalized.includes('up')) return 'up'
  if (normalized.includes('-') || normalized.includes('down')) return 'down'
  return 'neutral'
})

const trendClass = computed(() => {
  return {
    positive: trendDirection.value === 'up',
    negative: trendDirection.value === 'down',
    neutral: trendDirection.value === 'neutral',
  }
})

// Icon components
const CalendarIcon = () =>
  h(
    'svg',
    { viewBox: '0 0 24 24', fill: 'none', stroke: 'currentColor' },
    h('path', {
      'stroke-linecap': 'round',
      'stroke-linejoin': 'round',
      'stroke-width': '2',
      d: 'M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z',
    })
  )

const DollarIcon = () =>
  h(
    'svg',
    { viewBox: '0 0 24 24', fill: 'none', stroke: 'currentColor' },
    h('path', {
      'stroke-linecap': 'round',
      'stroke-linejoin': 'round',
      'stroke-width': '2',
      d: 'M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z',
    })
  )

const ClockIcon = () =>
  h(
    'svg',
    { viewBox: '0 0 24 24', fill: 'none', stroke: 'currentColor' },
    h('path', {
      'stroke-linecap': 'round',
      'stroke-linejoin': 'round',
      'stroke-width': '2',
      d: 'M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z',
    })
  )

const StarIcon = () =>
  h(
    'svg',
    { viewBox: '0 0 24 24', fill: 'none', stroke: 'currentColor' },
    h('path', {
      'stroke-linecap': 'round',
      'stroke-linejoin': 'round',
      'stroke-width': '2',
      d: 'M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.197-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z',
    })
  )

const UsersIcon = () =>
  h(
    'svg',
    { viewBox: '0 0 24 24', fill: 'none', stroke: 'currentColor' },
    h('path', {
      'stroke-linecap': 'round',
      'stroke-linejoin': 'round',
      'stroke-width': '2',
      d: 'M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z',
    })
  )

const ChartIcon = () =>
  h(
    'svg',
    { viewBox: '0 0 24 24', fill: 'none', stroke: 'currentColor' },
    h('path', {
      'stroke-linecap': 'round',
      'stroke-linejoin': 'round',
      'stroke-width': '2',
      d: 'M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z',
    })
  )

const iconComponent = computed(() => {
  const icons = {
    calendar: CalendarIcon,
    dollar: DollarIcon,
    clock: ClockIcon,
    star: StarIcon,
    users: UsersIcon,
    chart: ChartIcon,
  }
  return icons[props.icon] || ChartIcon
})
</script>

<style scoped>
.quick-stats-card {
  position: relative;
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
  padding: var(--spacing-lg) var(--spacing-xl);
  background: var(--color-background);
  border: 1px solid var(--color-gray-200);
  border-radius: var(--radius-xl);
  box-shadow: var(--shadow-sm);
  overflow: hidden;
  transition: all var(--transition-base);
  cursor: default;
}

.quick-stats-card:hover {
  transform: translateY(-4px);
  box-shadow: var(--shadow-lg);
  border-color: transparent;
}

.gradient-overlay {
  position: absolute;
  inset: 0;
  background: linear-gradient(135deg, transparent 0%, transparent 100%);
  opacity: 0;
  transition: opacity var(--transition-base);
  pointer-events: none;
}

.quick-stats-card.hovered .gradient-overlay {
  opacity: 0.05;
}

.stat-icon {
  flex-shrink: 0;
  width: 56px;
  height: 56px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: var(--radius-lg);
  transition: all var(--transition-base);
}

.stat-icon svg {
  width: 28px;
  height: 28px;
  stroke-width: 2;
  transition: transform var(--transition-base);
}

.quick-stats-card:hover .stat-icon svg {
  transform: scale(1.1);
}

.stat-content {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: var(--spacing-xs);
}

.stat-title {
  margin: 0;
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-text-secondary);
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.stat-value-row {
  display: flex;
  align-items: baseline;
  gap: var(--spacing-xs);
}

.stat-value {
  margin: 0;
  font-size: var(--font-size-3xl);
  font-weight: var(--font-weight-bold);
  color: var(--color-text-primary);
  line-height: 1;
  transition: color var(--transition-base);
}

.stat-suffix {
  font-size: var(--font-size-base);
  font-weight: var(--font-weight-medium);
  color: var(--color-text-secondary);
  margin-inline-start: var(--spacing-xs);
}

.stat-trend {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 2px var(--spacing-xs);
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
  border-radius: var(--radius-sm);
  width: fit-content;
}

.stat-trend svg {
  width: 14px;
  height: 14px;
  stroke-width: 2.5;
}

.stat-trend.positive {
  color: var(--color-success-700);
  background: var(--color-success-50);
}

.stat-trend.negative {
  color: var(--color-danger-700);
  background: var(--color-danger-50);
}

.stat-trend.neutral {
  color: var(--color-gray-700);
  background: var(--color-gray-100);
}

/* Variant Styles */
.variant-primary .stat-icon {
  background: var(--color-primary-50);
  color: var(--color-primary-600);
}

.variant-primary.hovered .gradient-overlay {
  background: linear-gradient(135deg, var(--color-primary-500) 0%, var(--color-primary-600) 100%);
}

.variant-success .stat-icon {
  background: var(--color-success-50);
  color: var(--color-success-600);
}

.variant-success.hovered .gradient-overlay {
  background: linear-gradient(135deg, var(--color-success-500) 0%, var(--color-success-600) 100%);
}

.variant-warning .stat-icon {
  background: var(--color-warning-50);
  color: var(--color-warning-600);
}

.variant-warning.hovered .gradient-overlay {
  background: linear-gradient(135deg, var(--color-warning-500) 0%, var(--color-warning-600) 100%);
}

.variant-danger .stat-icon {
  background: var(--color-danger-50);
  color: var(--color-danger-600);
}

.variant-danger.hovered .gradient-overlay {
  background: linear-gradient(135deg, var(--color-danger-500) 0%, var(--color-danger-600) 100%);
}

.variant-info .stat-icon {
  background: var(--color-info-50);
  color: var(--color-info-600);
}

.variant-info.hovered .gradient-overlay {
  background: linear-gradient(135deg, var(--color-info-500) 0%, var(--color-info-600) 100%);
}

/* Mobile responsive */
@media (max-width: 767px) {
  .quick-stats-card {
    padding: var(--spacing-md) var(--spacing-lg);
    gap: var(--spacing-sm);
  }

  .stat-icon {
    width: 48px;
    height: 48px;
  }

  .stat-icon svg {
    width: 24px;
    height: 24px;
  }

  .stat-value {
    font-size: var(--font-size-2xl);
  }

  .stat-suffix {
    font-size: var(--font-size-sm);
  }
}

/* Tablet */
@media (min-width: 768px) and (max-width: 1023px) {
  .quick-stats-card {
    padding: var(--spacing-md) var(--spacing-lg);
  }
}

/* RTL Support - Already handled by logical properties */

/* Reduced motion support */
@media (prefers-reduced-motion: reduce) {
  .quick-stats-card:hover {
    transform: none;
  }

  .quick-stats-card:hover .stat-icon svg {
    transform: none;
  }
}

/* Print styles */
@media print {
  .quick-stats-card {
    border: 1px solid #000;
    box-shadow: none;
    page-break-inside: avoid;
  }

  .gradient-overlay {
    display: none;
  }
}
</style>
