<template>
  <div class="home-view">
    <HeroSection />
    <CategoryGrid />
    <FeaturedProviders />
    <HowItWorks />
    <Testimonials />
    <CTASection />
  </div>
</template>

<script setup lang="ts">
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
