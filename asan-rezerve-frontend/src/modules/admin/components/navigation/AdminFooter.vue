<template>
  <footer class="admin-footer">
    <div class="content">
      <span class="muted">© {{ currentYear }} Booksy Admin</span>
      <div class="links">
        <router-link to="/admin/reports">Reports</router-link>
        <router-link to="/admin/users">Users</router-link>
        <router-link to="/help">Help</router-link>
      </div>
    </div>
  </footer>
</template>

<script setup lang="ts">
import { computed } from 'vue'

const currentYear = computed(() => new Date().getFullYear())
</script>

<style scoped>
.admin-footer {
  position: sticky;
  bottom: 0;
  margin-top: auto;
  background: #ffffff;
  border-top: 1px solid var(--color-gray-300);
}

.content {
  max-width: 1600px;
  margin: 0 auto;
  padding: 0.75rem 2rem;
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.muted {
  color: var(--color-gray-600);
  font-size: 0.875rem;
}

.links {
  display: flex;
  gap: 1rem;
}
.links a {
  color: var(--color-gray-600);
  text-decoration: none;
  font-size: 0.875rem;
}
.links a:hover {
  color: var(--color-gray-900);
}
</style>
