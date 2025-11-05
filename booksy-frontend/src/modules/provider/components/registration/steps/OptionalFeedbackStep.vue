<template>
  <div class="min-h-screen bg-background p-4 py-8">
    <div class="max-w-2xl mx-auto">
      <ProgressIndicator :current-step="8" :total-steps="9" />

      <div class="bg-card rounded-2xl shadow-sm p-8 border">
        <div class="mb-6 text-center">
          <div class="w-16 h-16 bg-primary/10 rounded-full flex items-center justify-center mx-auto mb-4">
            <svg xmlns="http://www.w3.org/2000/svg" class="w-8 h-8 text-primary" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 8h10M7 12h4m1 8l-4-4H5a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-3l-4 4z" />
            </svg>
          </div>
          <h2 class="mb-2 text-2xl font-bold">نظر شما برای ما مهم است</h2>
          <p class="text-muted-foreground">
            چطور می‌توانیم بیشتر به شما کمک کنیم؟
          </p>
        </div>

        <form @submit.prevent="handleSubmit" class="space-y-6">
          <div class="space-y-3">
            <label class="block text-sm font-medium">گزینه‌های پیشنهادی را انتخاب کنید (اختیاری)</label>
            <div class="grid grid-cols-1 gap-2">
              <button
                v-for="answer in preAnswers"
                :key="answer"
                type="button"
                @click="togglePreAnswer(answer)"
                :class="[
                  'p-3 rounded-lg border-2 transition-all text-sm text-right hover:border-primary/50',
                  selectedPreAnswers.includes(answer)
                    ? 'border-primary bg-primary/5'
                    : 'border-border bg-card'
                ]"
              >
                <div class="flex items-center gap-2">
                  <div
                    :class="[
                      'w-5 h-5 rounded border-2 flex items-center justify-center flex-shrink-0',
                      selectedPreAnswers.includes(answer)
                        ? 'border-primary bg-primary'
                        : 'border-muted-foreground/30'
                    ]"
                  >
                    <svg
                      v-if="selectedPreAnswers.includes(answer)"
                      xmlns="http://www.w3.org/2000/svg"
                      class="w-3 h-3 text-white"
                      viewBox="0 0 24 24"
                      fill="none"
                      stroke="currentColor"
                      stroke-width="3"
                    >
                      <path stroke-linecap="round" stroke-linejoin="round" d="M5 13l4 4L19 7" />
                    </svg>
                  </div>
                  <span class="flex-1">{{ answer }}</span>
                </div>
              </button>
            </div>
          </div>

          <div class="space-y-2">
            <label for="feedback" class="block text-sm font-medium">
              یا نظرات دیگری دارید؟ (اختیاری)
            </label>
            <textarea
              id="feedback"
              v-model="feedback"
              placeholder="لطفاً نظرات، پیشنهادات یا موارد خاصی که نیاز دارید را بنویسید..."
              class="w-full min-h-[120px] resize-none p-3 rounded-lg border border-border focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary"
            />
            <p class="text-xs text-muted-foreground">
              می‌توانید هم از گزینه‌های بالا و هم از این بخش استفاده کنید
            </p>
          </div>

          <div class="p-4 bg-accent/20 rounded-lg">
            <p class="text-sm">
              💡 <span class="font-medium">نکته:</span> بازخورد شما به ما کمک می‌کند تا خدمات بهتری ارائه دهیم
            </p>
          </div>

          <div class="flex gap-3">
            <AppButton type="button" variant="secondary" @click="$emit('back')" class="flex-1">
              قبلی
            </AppButton>
            <AppButton type="submit" variant="primary" class="flex-1">
              پایان
            </AppButton>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import ProgressIndicator from '../shared/ProgressIndicator.vue'
import AppButton from '@/shared/components/ui/Button/AppButton.vue'

interface Emits {
  (e: 'next', feedback?: string): void
  (e: 'back'): void
}

const emit = defineEmits<Emits>()

const preAnswers = [
  'رابط کاربری ساده و کاربردی است',
  'نیاز به قابلیت‌های بیشتر دارم',
  'فرآیند ثبت‌نام سریع و آسان بود',
  'دوست دارم آموزش بیشتری ببینم',
  'همه چیز عالی است',
]

const feedback = ref('')
const selectedPreAnswers = ref<string[]>([])

const togglePreAnswer = (answer: string) => {
  if (selectedPreAnswers.value.includes(answer)) {
    selectedPreAnswers.value = selectedPreAnswers.value.filter(a => a !== answer)
  } else {
    selectedPreAnswers.value = [...selectedPreAnswers.value, answer]
  }
}

const handleSubmit = () => {
  const combinedFeedback = [
    ...selectedPreAnswers.value,
    feedback.value.trim() && `دیگر: ${feedback.value.trim()}`
  ]
    .filter(Boolean)
    .join(' | ')

  emit('next', combinedFeedback)
}
</script>

<style scoped>
.bg-background {
  background-color: #f9fafb;
}

.bg-card {
  background-color: white;
}

.bg-primary\/10 {
  background-color: rgba(139, 92, 246, 0.1);
}

.bg-primary\/5 {
  background-color: rgba(139, 92, 246, 0.05);
}

.bg-accent\/20 {
  background-color: rgba(236, 72, 153, 0.2);
}

.text-primary {
  color: #8b5cf6;
}

.text-muted-foreground {
  color: #6b7280;
}

.border-primary {
  border-color: #8b5cf6;
}

.border-primary\/50 {
  border-color: rgba(139, 92, 246, 0.5);
}

.border-muted-foreground\/30 {
  border-color: rgba(107, 114, 128, 0.3);
}

.border-border {
  border-color: #d1d5db;
}

.bg-primary {
  background-color: #8b5cf6;
}

.hover\:border-primary\/50:hover {
  border-color: rgba(139, 92, 246, 0.5);
}
</style>
