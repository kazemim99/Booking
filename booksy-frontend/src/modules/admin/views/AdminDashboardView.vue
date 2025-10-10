<template>
  <div class="admin-dashboard">
    <h1>Admin Dashboard</h1>
    <div class="grid">
      <AppCard>
        <h2>Users</h2>
        <p>Overview of users and growth.</p>
      </AppCard>
      <AppCard>
        <h2>Bookings</h2>
        <p>Recent booking activity.</p>
      </AppCard>
      <AppCard>
        <h2>Providers</h2>
        <p>Verification and status summary.</p>
      </AppCard>
    </div>
  </div>
  
</template>

<script setup lang="ts">
import AppCard from '@/shared/components/ui/Card/AppCard.vue'
</script>

<style scoped>
.admin-dashboard {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
  gap: 1rem;
}
</style>

