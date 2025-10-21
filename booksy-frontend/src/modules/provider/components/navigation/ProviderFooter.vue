<template>
  <footer class="provider-footer">
    <div class="footer-content">
      <!-- Copyright and Version Info -->
      <div class="footer-info">
        <span class="copyright">© {{ currentYear }} Booksy</span>
        <span class="version">v{{ appVersion }}</span>
      </div>

      <!-- Footer Links -->
      <nav class="footer-links" role="navigation" aria-label="Footer navigation">
        <a href="/help" class="footer-link" target="_blank" rel="noopener noreferrer">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8.228 9c.549-1.165 2.03-2 3.772-2 2.21 0 4 1.343 4 3 0 1.4-1.278 2.575-3.006 2.907-.542.104-.994.54-.994 1.093m0 3h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          <span>{{ $t('footer.help') }}</span>
        </a>

        <a href="/support" class="footer-link" target="_blank" rel="noopener noreferrer">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M18.364 5.636l-3.536 3.536m0 5.656l3.536 3.536M9.172 9.172L5.636 5.636m3.536 9.192l-3.536 3.536M21 12a9 9 0 11-18 0 9 9 0 0118 0zm-5 0a4 4 0 11-8 0 4 4 0 018 0z" />
          </svg>
          <span>{{ $t('footer.support') }}</span>
        </a>

        <a href="/terms" class="footer-link" target="_blank" rel="noopener noreferrer">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
          </svg>
          <span>{{ $t('footer.terms') }}</span>
        </a>

        <a href="/privacy" class="footer-link" target="_blank" rel="noopener noreferrer">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
          </svg>
          <span>{{ $t('footer.privacy') }}</span>
        </a>
      </nav>
    </div>
  </footer>
</template>

<script setup lang="ts">
import { computed } from 'vue'

const currentYear = computed(() => new Date().getFullYear())

// TODO: Get this from env or package.json
const appVersion = computed(() => '1.0.0')
</script>

<style scoped>
.provider-footer {
  margin-top: auto;
  background: var(--color-background);
  border-top: 1px solid var(--color-gray-200);
  transition: border-color var(--transition-base);
}

.footer-content {
  max-width: 1400px;
  margin: 0 auto;
  padding: var(--spacing-md) var(--spacing-xl);
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--spacing-lg);
}

.footer-info {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
  color: var(--color-text-secondary);
  font-size: var(--font-size-sm);
}

.copyright {
  font-weight: var(--font-weight-medium);
}

.version {
  padding: 2px var(--spacing-xs);
  background: var(--color-gray-100);
  color: var(--color-gray-600);
  border-radius: var(--radius-sm);
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
  font-family: monospace;
}

.footer-links {
  display: flex;
  align-items: center;
  gap: var(--spacing-lg);
}

.footer-link {
  display: flex;
  align-items: center;
  gap: var(--spacing-xs);
  color: var(--color-text-secondary);
  text-decoration: none;
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  padding: var(--spacing-xs) var(--spacing-sm);
  border-radius: var(--radius-md);
  transition: all var(--transition-fast);
  position: relative;
}

.footer-link svg {
  width: 16px;
  height: 16px;
  stroke-width: 2;
}

.footer-link:hover {
  color: var(--color-primary-600);
  background: var(--color-primary-50);
  transform: translateY(-2px);
}

.footer-link:active {
  transform: translateY(0);
}

/* Mobile responsive */
@media (max-width: 767px) {
  .footer-content {
    flex-direction: column;
    align-items: flex-start;
    padding: var(--spacing-md) var(--spacing-lg);
    gap: var(--spacing-md);
  }

  .footer-info {
    width: 100%;
    justify-content: space-between;
  }

  .footer-links {
    width: 100%;
    flex-wrap: wrap;
    gap: var(--spacing-sm);
  }

  .footer-link {
    flex: 1;
    min-width: calc(50% - var(--spacing-xs));
    justify-content: center;
  }
}

@media (max-width: 480px) {
  .footer-link {
    min-width: 100%;
  }

  .footer-link span {
    flex: 1;
  }
}

/* Tablet */
@media (min-width: 768px) and (max-width: 1023px) {
  .footer-content {
    padding: var(--spacing-md) var(--spacing-lg);
  }

  .footer-links {
    gap: var(--spacing-md);
  }
}

/* RTL Support - Already handled by flexbox auto-layout */
/* Logical properties ensure proper RTL behavior */

/* Print styles */
@media print {
  .provider-footer {
    border-top: 1px solid #000;
    background: transparent;
  }

  .footer-link svg {
    display: none;
  }
}
</style>
