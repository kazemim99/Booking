<template>
  <a-layout class="admin-layout">
    <a-layout-sider v-model:collapsed="collapsed" :trigger="null" collapsible theme="dark">
      <div class="logo">
        <h2 v-if="!collapsed">{{ $t('app.name') }}</h2>
        <h2 v-else>{{ isPersian ? 'بوکسی' : 'BA' }}</h2>
      </div>

      <a-menu
        v-model:selectedKeys="selectedKeys"
        theme="dark"
        mode="inline"
        @click="handleMenuClick"
      >
        <a-menu-item key="dashboard">
          <dashboard-outlined />
          <span>{{ $t('navigation.dashboard') }}</span>
        </a-menu-item>

        <a-menu-item key="users">
          <user-outlined />
          <span>{{ $t('navigation.users') }}</span>
        </a-menu-item>

        <a-menu-item key="providers">
          <shop-outlined />
          <span>{{ $t('navigation.providers') }}</span>
        </a-menu-item>

        <a-menu-item key="gallery">
          <picture-outlined />
          <span>{{ $t('navigation.gallery') }}</span>
        </a-menu-item>

        <a-menu-item key="services">
          <appstore-outlined />
          <span>{{ $t('navigation.services') }}</span>
        </a-menu-item>

        <a-menu-item key="analytics">
          <bar-chart-outlined />
          <span>{{ $t('navigation.analytics') }}</span>
        </a-menu-item>

        <a-menu-item key="payments">
          <dollar-outlined />
          <span>{{ $t('navigation.payments') }}</span>
        </a-menu-item>

        <a-menu-item key="orders">
          <shopping-cart-outlined />
          <span>{{ $t('navigation.orders') }}</span>
        </a-menu-item>

        <a-menu-item key="logs">
          <file-text-outlined />
          <span>{{ $t('logs.title') }}</span>
        </a-menu-item>

        <a-menu-item key="settings">
          <setting-outlined />
          <span>{{ $t('navigation.settings') }}</span>
        </a-menu-item>
      </a-menu>
    </a-layout-sider>

    <a-layout>
      <a-layout-header class="header">
        <menu-unfold-outlined
          v-if="collapsed"
          class="trigger"
          @click="collapsed = !collapsed"
        />
        <menu-fold-outlined
          v-else
          class="trigger"
          @click="collapsed = !collapsed"
        />

        <div class="header-right">
          <language-switcher />

          <a-badge :count="notifications" :overflow-count="99">
            <bell-outlined class="icon" />
          </a-badge>

          <a-dropdown>
            <a-space class="user-info">
              <a-avatar>
                <template #icon><user-outlined /></template>
              </a-avatar>
              <span>{{ authStore.user?.email || authStore.user?.displayName }}</span>
            </a-space>

            <template #overlay>
              <a-menu>
                <a-menu-item key="profile">
                  <user-outlined />
                  {{ $t('settings.profile') }}
                </a-menu-item>
                <a-menu-divider />
                <a-menu-item key="logout" @click="handleLogout">
                  <logout-outlined />
                  {{ $t('auth.logout') }}
                </a-menu-item>
              </a-menu>
            </template>
          </a-dropdown>
        </div>
      </a-layout-header>

      <a-layout-content class="content">
        <router-view />
      </a-layout-content>

      <a-layout-footer class="footer">
        {{ $t('app.name') }} - {{ new Date().getFullYear() }}
      </a-layout-footer>
    </a-layout>
  </a-layout>
</template>

<script setup lang="ts">
import { ref, watch, computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '../stores/auth.store'
import { useLocaleStore } from '../stores/locale.store'
import LanguageSwitcher from '../components/common/LanguageSwitcher.vue'
import {
  MenuUnfoldOutlined,
  MenuFoldOutlined,
  DashboardOutlined,
  UserOutlined,
  ShopOutlined,
  PictureOutlined,
  AppstoreOutlined,
  BarChartOutlined,
  DollarOutlined,
  ShoppingCartOutlined,
  FileTextOutlined,
  SettingOutlined,
  BellOutlined,
  LogoutOutlined,
} from '@ant-design/icons-vue'

const localeStore = useLocaleStore()
const isPersian = computed(() => localeStore.isPersian)

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()

const collapsed = ref(false)
const selectedKeys = ref<string[]>([])
const notifications = ref(5)

watch(
  () => route.path,
  (newPath) => {
    const key = newPath.split('/')[1] || 'dashboard'
    selectedKeys.value = [key]
  },
  { immediate: true }
)

const handleMenuClick = ({ key }: { key: string }) => {
  router.push(`/${key}`)
}

const handleLogout = async () => {
  await authStore.logout()
  router.push('/login')
}
</script>

<style scoped>
.admin-layout {
  min-height: 100vh;
}

.logo {
  height: 64px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(255, 255, 255, 0.1);
}

.logo h2 {
  color: white;
  margin: 0;
  font-size: 20px;
  font-weight: bold;
}

.header {
  background: #fff;
  padding: 0 24px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  box-shadow: 0 1px 4px rgba(0, 21, 41, 0.08);
}

.trigger {
  font-size: 18px;
  cursor: pointer;
  transition: color 0.3s;
}

.trigger:hover {
  color: #1890ff;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 24px;
}

.icon {
  font-size: 18px;
  cursor: pointer;
  transition: color 0.3s;
}

.icon:hover {
  color: #1890ff;
}

.user-info {
  cursor: pointer;
}

.content {
  margin: 24px;
  background: #f0f2f5;
  min-height: calc(100vh - 134px);
}

.footer {
  text-align: center;
  background: #fff;
}
</style>
