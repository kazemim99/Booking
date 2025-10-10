<template>
  <div class="completion-container">
    <!-- Booksy Logo -->
    <div class="logo-container">
      <svg viewBox="0 0 60 60" fill="none" xmlns="http://www.w3.org/2000/svg">
        <circle cx="30" cy="30" r="30" fill="#10b981" />
        <path d="M30 15C22 15 15 22 15 30C15 38 22 45 30 45C38 45 45 38 45 30C45 22 38 15 30 15Z" fill="white" />
      </svg>
    </div>

    <!-- Success Message -->
    <div class="success-content">
      <h1 class="success-title">{{ $t('provider.registration.complete.title') }}</h1>
      <h2 class="success-subtitle">{{ $t('provider.registration.complete.subtitle') }}</h2>

      <div class="description-box">
        <p class="description-text">
          {{ $t('provider.registration.complete.description') }}
        </p>
      </div>

      <!-- Status Alert -->
      <div class="status-alert">
        <svg class="alert-icon" viewBox="0 0 20 20" fill="currentColor">
          <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd" />
        </svg>
        <p class="alert-text">
          {{ $t('provider.registration.complete.pendingApproval') }}
        </p>
      </div>

      <!-- Continue Button -->
      <button class="btn-continue" @click="handleContinue">
        {{ $t('common.continue') }}
      </button>
    </div>

    <!-- Background Decoration -->
    <div class="background-pattern">
      <div class="pattern-symbol">$</div>
      <div class="pattern-symbol">℞</div>
      <div class="pattern-symbol">$</div>
    </div>
  </div>
</template>

<script setup lang="ts">
interface Emits {
  (e: 'complete'): void
}

const emit = defineEmits<Emits>()

const handleContinue = () => {
  emit('complete')
}
</script>

<style scoped>
.completion-container {
  position: relative;
  width: 100%;
  min-height: 70vh;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 3rem 2rem;
  overflow: hidden;
}

.logo-container {
  width: 5rem;
  height: 5rem;
  margin-bottom: 2rem;
  animation: fadeInScale 0.6s cubic-bezier(0.4, 0, 0.2, 1);
}

.logo-container svg {
  width: 100%;
  height: 100%;
  filter: drop-shadow(0 10px 20px rgba(16, 185, 129, 0.3));
}

@keyframes fadeInScale {
  from {
    opacity: 0;
    transform: scale(0.5);
  }
  to {
    opacity: 1;
    transform: scale(1);
  }
}

.success-content {
  text-align: center;
  max-width: 40rem;
  z-index: 10;
}

.success-title {
  font-size: 2.5rem;
  font-weight: 800;
  color: #111827;
  margin-bottom: 0.75rem;
  animation: fadeInUp 0.6s cubic-bezier(0.4, 0, 0.2, 1) 0.2s both;
}

.success-subtitle {
  font-size: 1.5rem;
  font-weight: 600;
  color: #374151;
  margin-bottom: 2rem;
  animation: fadeInUp 0.6s cubic-bezier(0.4, 0, 0.2, 1) 0.3s both;
}

.description-box {
  background: #f9fafb;
  padding: 1.5rem;
  border-radius: 0.75rem;
  margin-bottom: 2rem;
  animation: fadeInUp 0.6s cubic-bezier(0.4, 0, 0.2, 1) 0.4s both;
}

.description-text {
  font-size: 1rem;
  color: #6b7280;
  line-height: 1.6;
}

.status-alert {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.75rem;
  padding: 1rem 1.5rem;
  background: #fffbeb;
  border: 2px solid #fbbf24;
  border-radius: 0.5rem;
  margin-bottom: 2rem;
  animation: fadeInUp 0.6s cubic-bezier(0.4, 0, 0.2, 1) 0.5s both;
}

.alert-icon {
  width: 1.5rem;
  height: 1.5rem;
  color: #f59e0b;
  flex-shrink: 0;
}

.alert-text {
  font-size: 0.875rem;
  font-weight: 600;
  color: #92400e;
}

.btn-continue {
  width: 100%;
  max-width: 20rem;
  padding: 1rem 2rem;
  font-size: 1rem;
  font-weight: 700;
  color: #ffffff;
  background: #111827;
  border: none;
  border-radius: 0.5rem;
  cursor: pointer;
  transition: all 0.2s ease;
  animation: fadeInUp 0.6s cubic-bezier(0.4, 0, 0.2, 1) 0.6s both;
}

.btn-continue:hover {
  background: #1f2937;
  transform: translateY(-2px);
  box-shadow: 0 10px 20px rgba(0, 0, 0, 0.2);
}

@keyframes fadeInUp {
  from {
    opacity: 0;
    transform: translateY(1rem);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.background-pattern {
  position: absolute;
  inset: 0;
  opacity: 0.03;
  overflow: hidden;
  pointer-events: none;
}

.pattern-symbol {
  position: absolute;
  font-size: 10rem;
  font-weight: 700;
  color: #111827;
  animation: float 20s ease-in-out infinite;
}

.pattern-symbol:nth-child(1) {
  top: 10%;
  left: 15%;
  animation-delay: 0s;
}

.pattern-symbol:nth-child(2) {
  top: 60%;
  right: 20%;
  animation-delay: -10s;
}

.pattern-symbol:nth-child(3) {
  bottom: 10%;
  left: 50%;
  animation-delay: -5s;
}

@keyframes float {
  0%, 100% {
    transform: translate(0, 0) rotate(0deg);
  }
  33% {
    transform: translate(2rem, -2rem) rotate(5deg);
  }
  66% {
    transform: translate(-2rem, 2rem) rotate(-5deg);
  }
}

@media (max-width: 640px) {
  .success-title {
    font-size: 2rem;
  }

  .success-subtitle {
    font-size: 1.25rem;
  }

  .pattern-symbol {
    font-size: 6rem;
  }
}
</style>
