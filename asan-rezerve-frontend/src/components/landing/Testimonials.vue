<script setup lang="ts">
// Rename component to TestimonialList to follow multi-word naming convention
defineOptions({
  name: 'TestimonialList'
})

const testimonials = [
  {
    id: 1,
    name: 'سارا احمدی',
    service: 'کوتاهی و آرایش مو',
    text: 'تجربه فوق‌العاده‌ای بود! بهترین سالن را از طریق این پلتفرم پیدا کردم. فرآیند رزرو بسیار ساده بود و آرایشگر کاملاً حرفه‌ای بود. قویاً توصیه می‌کنم!',
  },
  {
    id: 2,
    name: 'محمد رضایی',
    service: 'ماساژ درمانی',
    text: 'عاشق این هستم که چقدر رزرو نوبت آسان است. ماساژور عالی بود و می‌توانستم دسترسی آنلاین را ببینم. این پلتفرم اولین انتخاب من برای خدمات سلامتی شده است.',
  },
  {
    id: 3,
    name: 'مریم کریمی',
    service: 'پاکسازی پوست',
    text: 'تنوع خدمات و ارائه‌دهندگان فوق‌العاده است. نظرات را خواندم، قیمت‌ها را مقایسه کردم و اسپای مورد علاقه‌ام را در چند دقیقه رزرو کردم. پوست من هرگز بهتر از این نبوده!',
  },
  {
    id: 4,
    name: 'علی محمدی',
    service: 'آرایشگاه مردانه',
    text: 'بهترین تجربه رزرو آرایشگاه! سلمانی دقیقاً می‌دانست من چه می‌خواهم و پرداخت آنلاین همه چیز را راحت کرد. قطعاً دوباره استفاده خواهم کرد.',
  },
  {
    id: 5,
    name: 'فاطمه حسینی',
    service: 'مانیکور و پدیکور',
    text: 'یک سالن ناخن شگفت‌انگیز نزدیک دفترم پیدا کردم. نظرات دقیقاً درست بود و متخصص کار فوق‌العاده‌ای انجام داد. جوایز وفاداری هم عالی است!',
  },
  {
    id: 6,
    name: 'رضا نوری',
    service: 'پکیج اسپا',
    text: 'از طریق این پلتفرم یک روز اسپا برای خودم رزرو کردم. همه چیز از رزرو تا خدمات واقعی عالی بود. تایید فوری آرامش خاطر به من داد.',
  },
]

const getInitials = (name: string) => {
  return name
    .split(' ')
    .map(n => n[0])
    .join('')
    .toUpperCase()
}
</script>

<template>
  <section class="testimonials-section" dir="rtl">
    <div class="container">
      <div class="section-header">
        <h2 class="section-title">محبوب هزاران مشتری</h2>
        <p class="section-subtitle">
          ببینید مشتریان راضی ما درباره تجربیات خود چه می‌گویند
        </p>
      </div>

      <div class="testimonials-grid">
        <div
          v-for="(testimonial, index) in testimonials"
          :key="testimonial.id"
          class="testimonial-card"
          :style="{ animationDelay: `${index * 0.1}s` }"
        >
          <div class="quote-icon">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
              <path d="M14.017 21v-7.391c0-5.704 3.731-9.57 8.983-10.609l.995 2.151c-2.432.917-3.995 3.638-3.995 5.849h4v10h-9.983zm-14.017 0v-7.391c0-5.704 3.748-9.57 9-10.609l.996 2.151c-2.433.917-3.996 3.638-3.996 5.849h3.983v10h-9.983z" />
            </svg>
          </div>

          <div class="rating">
            <svg
              v-for="star in 5"
              :key="star"
              xmlns="http://www.w3.org/2000/svg"
              viewBox="0 0 24 24"
              fill="currentColor"
            >
              <path d="M11.48 3.499a.562.562 0 011.04 0l2.125 5.111a.563.563 0 00.475.345l5.518.442c.499.04.701.663.321.988l-4.204 3.602a.563.563 0 00-.182.557l1.285 5.385a.562.562 0 01-.84.61l-4.725-2.885a.563.563 0 00-.586 0L6.982 20.54a.562.562 0 01-.84-.61l1.285-5.386a.562.562 0 00-.182-.557l-4.204-3.602a.563.563 0 01.321-.988l5.518-.442a.563.563 0 00.475-.345L11.48 3.5z" />
            </svg>
          </div>

          <p class="testimonial-text">{{ testimonial.text }}</p>

          <div class="customer-info">
            <div class="customer-avatar">
              <span>{{ getInitials(testimonial.name) }}</span>
            </div>
            <div class="customer-details">
              <div class="customer-name">{{ testimonial.name }}</div>
              <div class="service-type">{{ testimonial.service }}</div>
            </div>
          </div>

          <div class="verified-badge">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
              <path fill-rule="evenodd" d="M8.603 3.799A4.49 4.49 0 0112 2.25c1.357 0 2.573.6 3.397 1.549a4.49 4.49 0 013.498 1.307 4.491 4.491 0 011.307 3.497A4.49 4.49 0 0121.75 12a4.49 4.49 0 01-1.549 3.397 4.491 4.491 0 01-1.307 3.497 4.491 4.491 0 01-3.497 1.307A4.49 4.49 0 0112 21.75a4.49 4.49 0 01-3.397-1.549 4.49 4.49 0 01-3.498-1.306 4.491 4.491 0 01-1.307-3.498A4.49 4.49 0 012.25 12c0-1.357.6-2.573 1.549-3.397a4.49 4.49 0 011.307-3.497 4.49 4.49 0 013.497-1.307zm7.007 6.387a.75.75 0 10-1.22-.872l-3.236 4.53L9.53 12.22a.75.75 0 00-1.06 1.06l2.25 2.25a.75.75 0 001.14-.094l3.75-5.25z" clip-rule="evenodd" />
            </svg>
            مشتری تایید شده
          </div>
        </div>
      </div>

      <div class="stats-row">
        <div class="stat-box">
          <div class="stat-number">۵۰,۰۰۰+</div>
          <div class="stat-label">مشتری راضی</div>
        </div>
        <div class="stat-box">
          <div class="stat-number">۴.۸/۵</div>
          <div class="stat-label">میانگین امتیاز</div>
        </div>
        <div class="stat-box">
          <div class="stat-number">۱۰۰,۰۰۰+</div>
          <div class="stat-label">رزرو تکمیل شده</div>
        </div>
        <div class="stat-box">
          <div class="stat-number">۹۸٪</div>
          <div class="stat-label">نرخ رضایت</div>
        </div>
      </div>
    </div>
  </section>
</template>

<style scoped>
.testimonials-section {
  padding: 5rem 0;
  background: white;
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
}

.testimonials-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(340px, 1fr));
  gap: 2rem;
  margin-bottom: 4rem;
}

.testimonial-card {
  position: relative;
  background: linear-gradient(135deg, #f8fafc 0%, #ffffff 100%);
  border: 2px solid #e2e8f0;
  border-radius: 20px;
  padding: 2rem;
  transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1);
  animation: fadeIn 0.6s ease-out;
  animation-fill-mode: both;
}

@keyframes fadeIn {
  from {
    opacity: 0;
    transform: translateY(20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.testimonial-card:hover {
  transform: translateY(-4px);
  border-color: #667eea;
  box-shadow: 0 12px 30px rgba(102, 126, 234, 0.15);
}

.quote-icon {
  width: 40px;
  height: 40px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  border-radius: 10px;
  margin-bottom: 1rem;
}

.quote-icon svg {
  width: 20px;
  height: 20px;
  color: white;
}

.rating {
  display: flex;
  gap: 0.25rem;
  margin-bottom: 1rem;
}

.rating svg {
  width: 18px;
  height: 18px;
  color: #fbbf24;
}

.testimonial-text {
  font-size: 1rem;
  line-height: 1.7;
  color: #475569;
  margin: 0 0 1.5rem 0;
}

.customer-info {
  display: flex;
  align-items: center;
  gap: 1rem;
  margin-bottom: 1rem;
}

.customer-avatar {
  width: 48px;
  height: 48px;
  border-radius: 50%;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
  font-weight: 700;
  font-size: 1rem;
}

.customer-details {
  flex: 1;
}

.customer-name {
  font-weight: 700;
  color: #1e293b;
  font-size: 1rem;
  margin-bottom: 0.25rem;
}

.service-type {
  font-size: 0.875rem;
  color: #64748b;
}

.verified-badge {
  display: inline-flex;
  align-items: center;
  gap: 0.375rem;
  padding: 0.375rem 0.75rem;
  background: #dbeafe;
  color: #1e40af;
  border-radius: 20px;
  font-size: 0.75rem;
  font-weight: 600;
}

.verified-badge svg {
  width: 14px;
  height: 14px;
}

.stats-row {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 2rem;
  padding: 3rem 2rem;
  background: linear-gradient(135deg, #f8fafc 0%, #e2e8f0 100%);
  border-radius: 20px;
}

.stat-box {
  text-align: center;
}

.stat-number {
  font-size: 2.5rem;
  font-weight: 800;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  margin-bottom: 0.5rem;
}

.stat-label {
  font-size: 0.95rem;
  color: #64748b;
  font-weight: 600;
}

/* Responsive */
@media (max-width: 768px) {
  .testimonials-section {
    padding: 3rem 0;
  }

  .section-title {
    font-size: 2rem;
  }

  .testimonials-grid {
    grid-template-columns: 1fr;
  }

  .stats-row {
    grid-template-columns: repeat(2, 1fr);
    gap: 1.5rem;
  }

  .stat-number {
    font-size: 2rem;
  }
}
</style>
