<template>
  <div class="home-view">
    <!-- Beautiful Transparent Landing Header -->
    <LandingHeader />

    <!-- Main Content -->
    <main class="home-content">
      <HeroSection />
      <CategoryGrid />
      <FeaturedProviders />
      <HowItWorks />
      <Testimonials />
      <CTASection />
    </main>
  </div>
</template>

<script setup lang="ts">
import LandingHeader from '@/components/landing/LandingHeader.vue'
import HeroSection from '@/components/landing/HeroSection.vue'
import CategoryGrid from '@/components/landing/CategoryGrid.vue'
import FeaturedProviders from '@/components/landing/FeaturedProviders.vue'
import HowItWorks from '@/components/landing/HowItWorks.vue'
import Testimonials from '@/components/landing/Testimonials.vue'
import CTASection from '@/components/landing/CTASection.vue'
</script>

<style scoped>
.home-view {
  min-height: 100vh;
  background: white;
}

.home-content {
  padding-top: 80px; /* Height of transparent landing header */
}

/* Smooth scroll behavior */
:deep(html) {
  scroll-behavior: smooth;
}

/* Global animations */
@media (prefers-reduced-motion: no-preference) {
  :deep(*) {
    scroll-behavior: smooth;
  }
}
</style>
