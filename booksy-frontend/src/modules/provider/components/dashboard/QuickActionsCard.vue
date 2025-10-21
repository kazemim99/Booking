<template>
  <div class="quick-actions-card">
    <div class="actions-grid">
      <!-- Add Service Action -->
      <button class="action-button action-primary" @click="handleAction('add-service')">
        <div class="action-icon">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M12 6v6m0 0v6m0-6h6m-6 0H6"
            />
          </svg>
        </div>
        <div class="action-content">
          <h4 class="action-title">{{ $t('dashboard.actions.addService') }}</h4>
          <p class="action-description">{{ $t('dashboard.actions.addServiceDescription') }}</p>
        </div>
        <div class="action-arrow">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
          </svg>
        </div>
      </button>

      <!-- View Calendar Action -->
      <button class="action-button action-success" @click="handleAction('view-calendar')">
        <div class="action-icon">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"
            />
          </svg>
        </div>
        <div class="action-content">
          <h4 class="action-title">{{ $t('dashboard.actions.viewCalendar') }}</h4>
          <p class="action-description">{{ $t('dashboard.actions.viewCalendarDescription') }}</p>
        </div>
        <div class="action-arrow">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
          </svg>
        </div>
      </button>

      <!-- Edit Hours Action -->
      <button class="action-button action-warning" @click="handleAction('edit-hours')">
        <div class="action-icon">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"
            />
          </svg>
        </div>
        <div class="action-content">
          <h4 class="action-title">{{ $t('dashboard.actions.editHours') }}</h4>
          <p class="action-description">{{ $t('dashboard.actions.editHoursDescription') }}</p>
        </div>
        <div class="action-arrow">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
          </svg>
        </div>
      </button>

      <!-- View Profile Action -->
      <button class="action-button action-info" @click="handleAction('view-profile')">
        <div class="action-icon">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"
            />
          </svg>
        </div>
        <div class="action-content">
          <h4 class="action-title">{{ $t('dashboard.actions.viewProfile') }}</h4>
          <p class="action-description">{{ $t('dashboard.actions.viewProfileDescription') }}</p>
        </div>
        <div class="action-arrow">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
          </svg>
        </div>
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
const emit = defineEmits<{
  (e: 'action', action: string): void
}>()

const handleAction = (action: string) => {
  emit('action', action)
}
</script>

<style scoped>
.quick-actions-card {
  width: 100%;
}

.actions-grid {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
}

.action-button {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
  padding: var(--spacing-md) var(--spacing-lg);
  background: var(--color-background);
  border: 1px solid var(--color-gray-200);
  border-radius: var(--radius-lg);
  cursor: pointer;
  transition: all var(--transition-fast);
  text-align: start;
  position: relative;
  overflow: hidden;
}

.action-button::before {
  content: '';
  position: absolute;
  inset: 0;
  background: linear-gradient(135deg, transparent 0%, transparent 100%);
  opacity: 0;
  transition: opacity var(--transition-base);
  pointer-events: none;
}

.action-button:hover {
  transform: translateY(-2px);
  box-shadow: var(--shadow-md);
  border-color: transparent;
}

.action-button:hover::before {
  opacity: 0.05;
}

.action-button:active {
  transform: translateY(0);
}

.action-icon {
  flex-shrink: 0;
  width: 48px;
  height: 48px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: var(--radius-lg);
  transition: all var(--transition-base);
}

.action-icon svg {
  width: 24px;
  height: 24px;
  stroke-width: 2;
  transition: transform var(--transition-base);
}

.action-button:hover .action-icon svg {
  transform: scale(1.1);
}

.action-content {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.action-title {
  margin: 0;
  font-size: var(--font-size-base);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
}

.action-description {
  margin: 0;
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
  line-height: 1.4;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.action-arrow {
  flex-shrink: 0;
  width: 20px;
  height: 20px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--color-gray-400);
  transition: all var(--transition-fast);
}

.action-arrow svg {
  width: 18px;
  height: 18px;
  stroke-width: 2;
}

.action-button:hover .action-arrow {
  transform: translateX(2px);
}

/* Action Variants */
.action-primary .action-icon {
  background: var(--color-primary-50);
  color: var(--color-primary-600);
}

.action-primary:hover {
  border-color: var(--color-primary-300);
}

.action-primary:hover::before {
  background: linear-gradient(135deg, var(--color-primary-500) 0%, var(--color-primary-600) 100%);
}

.action-primary:hover .action-arrow {
  color: var(--color-primary-600);
}

.action-success .action-icon {
  background: var(--color-success-50);
  color: var(--color-success-600);
}

.action-success:hover {
  border-color: var(--color-success-300);
}

.action-success:hover::before {
  background: linear-gradient(135deg, var(--color-success-500) 0%, var(--color-success-600) 100%);
}

.action-success:hover .action-arrow {
  color: var(--color-success-600);
}

.action-warning .action-icon {
  background: var(--color-warning-50);
  color: var(--color-warning-600);
}

.action-warning:hover {
  border-color: var(--color-warning-300);
}

.action-warning:hover::before {
  background: linear-gradient(135deg, var(--color-warning-500) 0%, var(--color-warning-600) 100%);
}

.action-warning:hover .action-arrow {
  color: var(--color-warning-600);
}

.action-info .action-icon {
  background: var(--color-info-50);
  color: var(--color-info-600);
}

.action-info:hover {
  border-color: var(--color-info-300);
}

.action-info:hover::before {
  background: linear-gradient(135deg, var(--color-info-500) 0%, var(--color-info-600) 100%);
}

.action-info:hover .action-arrow {
  color: var(--color-info-600);
}

/* Mobile responsive */
@media (max-width: 767px) {
  .action-button {
    padding: var(--spacing-sm) var(--spacing-md);
    gap: var(--spacing-sm);
  }

  .action-icon {
    width: 40px;
    height: 40px;
  }

  .action-icon svg {
    width: 20px;
    height: 20px;
  }

  .action-title {
    font-size: var(--font-size-sm);
  }

  .action-description {
    font-size: var(--font-size-xs);
  }
}

/* Tablet */
@media (min-width: 768px) and (max-width: 1023px) {
  .actions-grid {
    gap: var(--spacing-xs);
  }

  .action-button {
    padding: var(--spacing-sm) var(--spacing-md);
  }
}

/* RTL Support - Already handled by flexbox and logical properties */

/* Reduced motion support */
@media (prefers-reduced-motion: reduce) {
  .action-button:hover {
    transform: none;
  }

  .action-button:hover .action-icon svg,
  .action-button:hover .action-arrow {
    transform: none;
  }
}

/* Print styles */
@media print {
  .action-button {
    border: 1px solid #000;
    box-shadow: none;
    page-break-inside: avoid;
  }

  .action-arrow {
    display: none;
  }
}

/* Focus styles for accessibility */
.action-button:focus-visible {
  outline: 2px solid var(--color-primary-500);
  outline-offset: 2px;
}
</style>
