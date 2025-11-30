<template>
  <div class="my-earnings-view">
    <div class="page-header">
      <h1>درآمد من</h1>
      <p class="subtitle">مشاهده درآمد و تسویه‌حساب شخصی</p>
    </div>

    <!-- Coming Soon Notice -->
    <div class="info-card">
      <div class="info-icon">💰</div>
      <div class="info-content">
        <h3>در حال توسعه</h3>
        <p>این بخش اطلاعات درآمد شخصی شما را نمایش خواهد داد.</p>
        <p class="text-muted">فقط درآمد حاصل از خدمات ارائه شده توسط شما را مشاهده خواهید کرد.</p>
      </div>
    </div>

    <!-- Stats Grid -->
    <div class="stats-grid">
      <div class="stat-card">
        <div class="stat-icon">📊</div>
        <div class="stat-content">
          <p class="stat-label">درآمد این ماه</p>
          <h3 class="stat-value">-</h3>
        </div>
      </div>

      <div class="stat-card">
        <div class="stat-icon">✅</div>
        <div class="stat-content">
          <p class="stat-label">خدمات انجام شده</p>
          <h3 class="stat-value">-</h3>
        </div>
      </div>

      <div class="stat-card">
        <div class="stat-icon">💵</div>
        <div class="stat-content">
          <p class="stat-label">در انتظار پرداخت</p>
          <h3 class="stat-value">-</h3>
        </div>
      </div>

      <div class="stat-card">
        <div class="stat-icon">🎯</div>
        <div class="stat-content">
          <p class="stat-label">میانگین هر سرویس</p>
          <h3 class="stat-value">-</h3>
        </div>
      </div>
    </div>

    <!-- Placeholder for earnings chart -->
    <div class="earnings-placeholder">
      <div class="placeholder-icon">
        <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
        </svg>
      </div>
      <h3>نمودار درآمد</h3>
      <p>نمودار تحلیلی درآمد ماهانه شما به زودی در دسترس خواهد بود.</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import { useHierarchyStore } from '../../stores/hierarchy.store'

const hierarchyStore = useHierarchyStore()

onMounted(() => {
  console.log('[MyEarningsView] Staff member earnings view mounted')
  console.log('[MyEarningsView] Current provider:', hierarchyStore.currentHierarchy?.provider)
})
</script>

<style scoped lang="scss">
.my-earnings-view {
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
  background: #fef3c7;
  border: 1px solid #fde68a;
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
      color: #92400e;
      margin: 0 0 8px 0;
    }

    p {
      font-size: 14px;
      color: #92400e;
      margin: 0 0 8px 0;

      &:last-child {
        margin-bottom: 0;
      }

      &.text-muted {
        color: #b45309;
        font-size: 13px;
      }
    }
  }
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 20px;
  margin-bottom: 32px;
}

.stat-card {
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: 12px;
  padding: 20px;
  display: flex;
  gap: 16px;
  align-items: flex-start;

  .stat-icon {
    font-size: 32px;
    flex-shrink: 0;
  }

  .stat-content {
    flex: 1;

    .stat-label {
      font-size: 14px;
      color: #6b7280;
      margin: 0 0 8px 0;
    }

    .stat-value {
      font-size: 24px;
      font-weight: 700;
      color: #1f2937;
      margin: 0;
    }
  }
}

.earnings-placeholder {
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
