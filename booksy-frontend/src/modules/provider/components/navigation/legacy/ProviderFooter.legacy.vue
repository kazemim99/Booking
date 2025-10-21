<template>
  <footer class="provider-footer">
    <div class="content">
      <span class="muted">© {{ currentYear }} Booksy Provider</span>
      <div class="links">
        <router-link to="/provider/profile">Profile</router-link>
        <router-link to="/provider/settings">Settings</router-link>
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
.provider-footer {
  position: sticky;
  bottom: 0;
  margin-top: auto;
  background: #ffffff;
  border-top: 1px solid #e5e7eb;
}

.content {
  max-width: 1400px;
  margin: 0 auto;
  padding: 0.75rem 2rem;
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.muted {
  color: #6b7280;
  font-size: 0.875rem;
}

.links {
  display: flex;
  gap: 1rem;
}
.links a {
  color: #6b7280;
  text-decoration: none;
  font-size: 0.875rem;
}
.links a:hover {
  color: #111827;
}
</style>
