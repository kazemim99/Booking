<template>
  <div class="my-bookings-view">
    <div class="page-header">
      <h1>رزروهای من</h1>
      <p class="subtitle">مشاهده و مدیریت رزروهای شخصی</p>
    </div>

    <!-- Coming Soon Notice -->
    <div class="info-card">
      <div class="info-icon">📅</div>
      <div class="info-content">
        <h3>در حال توسعه</h3>
        <p>این بخش نمایش رزروهای اختصاص داده شده به شما را نمایش خواهد داد.</p>
        <p class="text-muted">فقط رزروهایی که به شما اختصاص داده شده‌اند را مشاهده خواهید کرد، نه کل سازمان.</p>
      </div>
    </div>

    <!-- Placeholder for future implementation -->
    <div class="bookings-placeholder">
      <div class="placeholder-icon">
        <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
        </svg>
      </div>
      <h3>هنوز رزروی ندارید</h3>
      <p>وقتی مشتریان برای شما رزرو کنند، اینجا نمایش داده می‌شوند.</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import { useHierarchyStore } from '../../stores/hierarchy.store'

const hierarchyStore = useHierarchyStore()

onMounted(() => {
  console.log('[MyBookingsView] Staff member bookings view mounted')
  console.log('[MyBookingsView] Current provider:', hierarchyStore.currentHierarchy?.provider)
})
</script>

<style scoped lang="scss">
.my-bookings-view {
  max-width: 1200px;
  margin: 0 auto;
}

.page-header {
  margin-bottom: 32px;

  h1 {
    font-size: 28px;
    font-weight: 700;
    color: #1f2937;
    margin: 0 0 8px 0;
  }

  .subtitle {
    font-size: 16px;
    color: #6b7280;
    margin: 0;
  }
}

.info-card {
  display: flex;
  gap: 16px;
  background: #eff6ff;
  border: 1px solid #bfdbfe;
  border-radius: 12px;
  padding: 20px;
  margin-bottom: 32px;

  .info-icon {
    font-size: 32px;
    flex-shrink: 0;
  }

  .info-content {
    flex: 1;

    h3 {
      font-size: 18px;
      font-weight: 600;
      color: #1e40af;
      margin: 0 0 8px 0;
    }

    p {
      font-size: 14px;
      color: #1e40af;
      margin: 0 0 8px 0;

      &:last-child {
        margin-bottom: 0;
      }

      &.text-muted {
        color: #60a5fa;
        font-size: 13px;
      }
    }
  }
}

.bookings-placeholder {
  background: white;
  border: 2px dashed #e5e7eb;
  border-radius: 12px;
  padding: 64px 32px;
  text-align: center;

  .placeholder-icon {
    width: 80px;
    height: 80px;
    margin: 0 auto 24px;
    color: #d1d5db;

    svg {
      width: 100%;
      height: 100%;
    }
  }

  h3 {
    font-size: 20px;
    font-weight: 600;
    color: #374151;
    margin: 0 0 12px 0;
  }

  p {
    font-size: 16px;
    color: #6b7280;
    margin: 0;
  }
}
</style>
