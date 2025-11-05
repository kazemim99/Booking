<template>
  <div class="min-h-screen flex items-center justify-center p-4 bg-gradient-to-br from-primary/5 to-accent/20">
    <div class="w-full max-w-md">
      <div class="bg-card rounded-2xl shadow-lg p-8 text-center">
        <!-- Success Icon -->
        <div class="relative mb-6">
          <div class="w-24 h-24 bg-primary/10 rounded-full flex items-center justify-center mx-auto">
            <svg xmlns="http://www.w3.org/2000/svg" class="w-16 h-16 text-primary" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
          </div>
          <svg xmlns="http://www.w3.org/2000/svg" class="w-6 h-6 text-primary absolute top-0 right-1/3 animate-pulse sparkle-icon" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 3v4M3 5h4M6 17v4m-2-2h4m5-16l2.286 6.857L21 12l-5.714 2.143L13 21l-2.286-6.857L5 12l5.714-2.143L13 3z" />
          </svg>
          <svg xmlns="http://www.w3.org/2000/svg" class="w-5 h-5 text-primary absolute bottom-2 left-1/4 animate-pulse delay-75 sparkle-icon" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 3v4M3 5h4M6 17v4m-2-2h4m5-16l2.286 6.857L21 12l-5.714 2.143L13 21l-2.286-6.857L5 12l5.714-2.143L13 3z" />
          </svg>
        </div>

        <!-- Title & Message -->
        <h1 class="mb-3 text-3xl font-bold">تبریک می‌گوییم!</h1>
        <p class="text-muted-foreground mb-8">
          ثبت‌نام شما با موفقیت انجام شد. اکنون می‌توانید از تمامی امکانات پنل استفاده کنید
        </p>

        <!-- Stats Cards -->
        <div class="grid grid-cols-3 gap-3 mb-8">
          <div class="p-3 bg-muted/50 rounded-lg">
            <div class="text-2xl mb-1">✓</div>
            <p class="text-xs text-muted-foreground">پروفایل کامل</p>
          </div>
          <div class="p-3 bg-muted/50 rounded-lg">
            <div class="text-2xl mb-1">🎯</div>
            <p class="text-xs text-muted-foreground">آماده فعالیت</p>
          </div>
          <div class="p-3 bg-muted/50 rounded-lg">
            <div class="text-2xl mb-1">🚀</div>
            <p class="text-xs text-muted-foreground">شروع موفق</p>
          </div>
        </div>

        <!-- CTA Button -->
        <AppButton
          type="button"
          variant="primary"
          size="large"
          block
          @click="goToDashboard"
          class="mb-4"
        >
          ورود به داشبورد
        </AppButton>

        <!-- Next Steps -->
        <div class="mt-6 p-4 bg-accent/20 rounded-lg text-right">
          <p class="text-sm font-medium mb-2">گام‌های بعدی:</p>
          <ul class="text-sm text-muted-foreground space-y-1">
            <li>• بررسی و تکمیل اطلاعات پروفایل</li>
            <li>• افزودن نوبت‌های جدید</li>
            <li>• دعوت از مشتریان</li>
          </ul>
        </div>
      </div>

      <!-- Additional Help -->
      <div class="mt-6 text-center">
        <p class="text-sm text-muted-foreground">
          نیاز به راهنمایی دارید؟{" "}
          <a href="#" class="text-primary hover:underline">
            مشاهده راهنمای کامل
          </a>
        </p>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router'
import AppButton from '@/shared/components/ui/Button/AppButton.vue'

const router = useRouter()

const goToDashboard = () => {
  router.push({ name: 'ProviderDashboard' })
}
</script>

<style scoped>
.bg-gradient-to-br {
  background: linear-gradient(to bottom right, rgba(139, 92, 246, 0.05), rgba(236, 72, 153, 0.2));
}

.from-primary\/5 {
  --tw-gradient-from: rgba(139, 92, 246, 0.05);
}

.to-accent\/20 {
  --tw-gradient-to: rgba(236, 72, 153, 0.2);
}

.bg-card {
  background-color: white;
}

.bg-primary\/10 {
  background-color: rgba(139, 92, 246, 0.1);
}

.bg-accent\/20 {
  background-color: rgba(236, 72, 153, 0.2);
}

.bg-muted\/50 {
  background-color: rgba(156, 163, 175, 0.5);
}

.text-primary {
  color: #8b5cf6;
}

.text-muted-foreground {
  color: #6b7280;
}

.hover\:underline:hover {
  text-decoration: underline;
}

.sparkle-icon {
  animation: pulse 2s cubic-bezier(0.4, 0, 0.6, 1) infinite;
}

.delay-75 {
  animation-delay: 75ms;
}

@keyframes pulse {
  0%, 100% {
    opacity: 1;
  }
  50% {
    opacity: 0.5;
  }
}

.animate-pulse {
  animation: pulse 2s cubic-bezier(0.4, 0, 0.6, 1) infinite;
}
</style>
