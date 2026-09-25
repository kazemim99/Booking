<template>
  <div class="app-confirm-modal">
    <div class="modal-overlay" @click="$emit('close')"></div>
    <div class="modal-content">
      <h3>{{ title }}</h3>
      <p>{{ message }}</p>
      <div class="modal-actions">
        <button @click="$emit('confirm')">Confirm</button>
        <button @click="$emit('close')">Cancel</button>
      </div>
    </div>
  </div>
</template>

<script lang="ts">
export default {
  name: 'AppConfirmModal',
  props: {
    title: {
      type: String,
      default: 'Confirm Action',
    },
    message: {
      type: String,
      default: 'Are you sure you want to proceed?',
    },
  },
}
</script>

<style scoped>
.app-confirm-modal {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}
.modal-overlay {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.5);
}
.modal-content {
  position: relative;
  background: #fff;
  padding: 2rem;
  border-radius: 8px;
  z-index: 1;
  min-width: 300px;
}
.modal-actions {
  margin-top: 1.5rem;
  display: flex;
  gap: 1rem;
  justify-content: flex-end;
}
</style>
