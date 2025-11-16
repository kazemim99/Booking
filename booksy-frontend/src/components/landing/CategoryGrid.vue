<template>
  <section class="category-section">
    <div class="container">
      <div class="section-header">
        <h2 class="section-title">Popular Categories</h2>
        <p class="section-subtitle">
          Explore top services from beauty salons, spas, wellness centers, and more
        </p>
      </div>

      <div class="category-grid">
        <div
          v-for="category in categories"
          :key="category.id"
          class="category-card"
          @click="navigateToCategory(category.slug)"
        >
          <div class="category-icon" :style="{ background: category.gradient }">
            <span class="icon">{{ category.icon }}</span>
          </div>
          <h3 class="category-name">{{ category.name }}</h3>
          <p class="category-count">{{ category.providerCount }}+ providers</p>
          <div class="category-arrow">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
            </svg>
          </div>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router'

const router = useRouter()

const categories = [
  {
    id: 1,
    name: 'Hair Salon',
    slug: 'haircut',
    icon: '💇',
    gradient: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
    providerCount: 2500,
  },
  {
    id: 2,
    name: 'Massage & Spa',
    slug: 'massage',
    icon: '💆',
    gradient: 'linear-gradient(135deg, #f093fb 0%, #f5576c 100%)',
    providerCount: 1800,
  },
  {
    id: 3,
    name: 'Facial & Skincare',
    slug: 'facial',
    icon: '✨',
    gradient: 'linear-gradient(135deg, #4facfe 0%, #00f2fe 100%)',
    providerCount: 1200,
  },
  {
    id: 4,
    name: 'Nails',
    slug: 'manicure',
    icon: '💅',
    gradient: 'linear-gradient(135deg, #fa709a 0%, #fee140 100%)',
    providerCount: 1500,
  },
  {
    id: 5,
    name: 'Makeup',
    slug: 'makeup',
    icon: '💄',
    gradient: 'linear-gradient(135deg, #ffecd2 0%, #fcb69f 100%)',
    providerCount: 900,
  },
  {
    id: 6,
    name: 'Waxing',
    slug: 'waxing',
    icon: '🌿',
    gradient: 'linear-gradient(135deg, #a8edea 0%, #fed6e3 100%)',
    providerCount: 800,
  },
  {
    id: 7,
    name: 'Barbershop',
    slug: 'barbering',
    icon: '💈',
    gradient: 'linear-gradient(135deg, #ffd89b 0%, #19547b 100%)',
    providerCount: 1100,
  },
  {
    id: 8,
    name: 'Tattoo & Piercing',
    slug: 'tattoo',
    icon: '🎨',
    gradient: 'linear-gradient(135deg, #f857a6 0%, #ff5858 100%)',
    providerCount: 600,
  },
]

const navigateToCategory = (slug: string) => {
  router.push({
    path: '/providers/search',
    query: {
      serviceCategory: slug,
      pageNumber: 1,
      pageSize: 12,
    }
  })
}
</script>

<style scoped>
.category-section {
  padding: 5rem 0;
  background: linear-gradient(180deg, #ffffff 0%, #f8fafc 100%);
}

.container {
  max-width: 1200px;
  margin: 0 auto;
  padding: 0 2rem;
}

.section-header {
  text-align: center;
  margin-bottom: 4rem;
}

.section-title {
  font-size: 2.5rem;
  font-weight: 800;
  color: #1e293b;
  margin: 0 0 1rem 0;
}

.section-subtitle {
  font-size: 1.125rem;
  color: #64748b;
  max-width: 600px;
  margin: 0 auto;
  line-height: 1.6;
}

.category-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
  gap: 2rem;
}

.category-card {
  position: relative;
  background: white;
  border-radius: 20px;
  padding: 2rem;
  cursor: pointer;
  transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1);
  border: 2px solid transparent;
  overflow: hidden;
}

.category-card::before {
  content: '';
  position: absolute;
  inset: 0;
  border-radius: 20px;
  padding: 2px;
  background: linear-gradient(135deg, rgba(102, 126, 234, 0.2), rgba(118, 75, 162, 0.2));
  -webkit-mask: linear-gradient(#fff 0 0) content-box, linear-gradient(#fff 0 0);
  -webkit-mask-composite: xor;
  mask-composite: exclude;
  opacity: 0;
  transition: opacity 0.4s;
}

.category-card:hover {
  transform: translateY(-8px);
  box-shadow: 0 20px 40px rgba(0, 0, 0, 0.1);
}

.category-card:hover::before {
  opacity: 1;
}

.category-icon {
  width: 80px;
  height: 80px;
  border-radius: 16px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 1.5rem;
  transition: transform 0.4s cubic-bezier(0.4, 0, 0.2, 1);
}

.category-card:hover .category-icon {
  transform: scale(1.1) rotate(5deg);
}

.icon {
  font-size: 2.5rem;
  filter: drop-shadow(0 2px 4px rgba(0, 0, 0, 0.1));
}

.category-name {
  font-size: 1.25rem;
  font-weight: 700;
  color: #1e293b;
  margin: 0 0 0.5rem 0;
}

.category-count {
  font-size: 0.875rem;
  color: #64748b;
  margin: 0;
}

.category-arrow {
  position: absolute;
  top: 2rem;
  right: 2rem;
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #f1f5f9;
  border-radius: 50%;
  opacity: 0;
  transform: translateX(-10px);
  transition: all 0.3s;
}

.category-card:hover .category-arrow {
  opacity: 1;
  transform: translateX(0);
}

.category-arrow svg {
  width: 16px;
  height: 16px;
  color: #667eea;
}

/* Animation on scroll */
.category-card {
  animation: fadeInUp 0.6s ease-out;
  animation-fill-mode: both;
}

.category-card:nth-child(1) { animation-delay: 0.1s; }
.category-card:nth-child(2) { animation-delay: 0.2s; }
.category-card:nth-child(3) { animation-delay: 0.3s; }
.category-card:nth-child(4) { animation-delay: 0.4s; }
.category-card:nth-child(5) { animation-delay: 0.5s; }
.category-card:nth-child(6) { animation-delay: 0.6s; }
.category-card:nth-child(7) { animation-delay: 0.7s; }
.category-card:nth-child(8) { animation-delay: 0.8s; }

@keyframes fadeInUp {
  from {
    opacity: 0;
    transform: translateY(30px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

/* Responsive */
@media (max-width: 768px) {
  .category-section {
    padding: 3rem 0;
  }

  .section-title {
    font-size: 2rem;
  }

  .category-grid {
    grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
    gap: 1rem;
  }

  .category-card {
    padding: 1.5rem 1rem;
  }

  .category-icon {
    width: 60px;
    height: 60px;
    margin-bottom: 1rem;
  }

  .icon {
    font-size: 2rem;
  }

  .category-name {
    font-size: 1rem;
  }

  .category-arrow {
    display: none;
  }
}
</style>
