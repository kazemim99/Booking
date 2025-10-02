<template>
  <div>
    <h1>Appointment List</h1>
    <!-- Appointment list will be rendered here -->
  </div>
</template>

<script lang="ts">
export default {
  name: 'AppointmentListView',
  data() {
    return {
      appointments: [],
    }
  },
  mounted() {
    // Fetch appointments here
  },
}
</script>

<style scoped>
h1 {
  margin-bottom: 16px;
}
</style>
