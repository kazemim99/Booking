<template>
  <section class="notification-preferences" dir="rtl">
    <p v-if="loading" class="state">در حال بارگذاری…</p>

    <!-- A setting that could not be read is not shown as on or off: either would be a guess. -->
    <div v-else-if="loadError" class="state" data-test="load-error">
      <p>تنظیمات اعلان بارگذاری نشد.</p>
      <button type="button" class="link" @click="load">تلاش دوباره</button>
    </div>

    <template v-else>
      <label class="setting">
        <span class="setting-info">
          <span class="setting-label">اعلان‌های برنامهٔ موبایل</span>
          <span class="setting-description">
            یادآوری‌ها و اطلاعیه‌ها روی گوشی شما. خاموش کردنش پیامک‌های مهم را متوقف نمی‌کند.
          </span>
        </span>
        <input
          type="checkbox"
          class="toggle"
          data-test="push-toggle"
          :checked="pushOn"
          :disabled="saving"
          @change="onToggle(($event.target as HTMLInputElement).checked)"
        />
      </label>

      <p v-if="saveError" class="save-error" data-test="save-error" role="alert">
        ذخیره نشد. تنظیم قبلی سر جایش ماند.
      </p>

      <div class="always-sent" data-test="always-sent">
        <p class="always-title">همیشه با پیامک ارسال می‌شود</p>
        <p class="always-body">{{ alwaysSent }}</p>
      </div>
    </template>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import {
  Channel,
  applyToggles,
  isEnabled,
  notificationPreferencesService,
} from '@/core/api/services/notification-preferences.service'

/**
 * The one notification preference this product can honour: push, on the notifications that are not critical.
 *
 * Replaces screens that offered SMS, email, reminder timing, quiet hours and a per-event matrix — none of which
 * the backend could act on. SMS is reserved for critical notifications, which cannot be switched off; no
 * notification uses email; reminder offsets are fixed in code; quiet hours were stored and never read. Those
 * facts are now stated instead of offered as choices.
 */
const props = defineProps<{ audience: 'customer' | 'provider' }>()

const mask = ref<number | null>(null)
const loading = ref(true)
const loadError = ref(false)
const saving = ref(false)
const saveError = ref(false)

const pushOn = computed(() => mask.value !== null && isEnabled(mask.value, Channel.PushNotification))

const alwaysSent = computed(() =>
  props.audience === 'provider'
    ? 'درخواست نوبت جدید، لغو نوبت توسط مشتری، و واریز تسویه. این‌ها قابل خاموش کردن نیستند، چون از دست دادنشان یعنی از دست دادن کار یا پول.'
    : 'تأیید، لغو و تغییر زمان نوبت، یادآوری ۲ ساعت قبل از نوبت، و پرداخت‌ها و بازگشت وجه. این‌ها قابل خاموش کردن نیستند، چون بدون آن‌ها ممکن است نوبتتان را از دست بدهید.',
)

async function load(): Promise<void> {
  loading.value = true
  loadError.value = false
  try {
    mask.value = await notificationPreferencesService.getChannelMask()
  } catch {
    loadError.value = true
  } finally {
    loading.value = false
  }
}

/** Optimistic, and put back exactly as it was if the server refuses. */
async function onToggle(on: boolean): Promise<void> {
  if (mask.value === null) return

  const before = mask.value
  saveError.value = false
  saving.value = true
  mask.value = applyToggles(before, { push: on })

  try {
    mask.value = await notificationPreferencesService.saveChannelMask(mask.value)
  } catch {
    mask.value = before
    saveError.value = true
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>

<style scoped>
.notification-preferences {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.setting {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  cursor: pointer;
}

.setting-info {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.setting-label {
  font-weight: 600;
  color: var(--color-gray-900);
}

.setting-description,
.always-body {
  font-size: 0.875rem;
  line-height: 1.6;
  color: var(--color-gray-600);
}

.toggle {
  width: 1.25rem;
  height: 1.25rem;
  accent-color: var(--color-primary-500);
}

.always-sent {
  padding: 0.75rem 1rem;
  border-radius: 0.5rem;
  background: var(--color-gray-50);
}

.always-title {
  margin: 0 0 0.25rem;
  font-weight: 600;
  color: var(--color-gray-800);
}

.always-body {
  margin: 0;
}

.state {
  color: var(--color-gray-500);
}

.save-error {
  margin: 0;
  color: var(--color-warning-500);
}

.link {
  border: none;
  background: none;
  color: var(--color-primary-600);
  font: inherit;
  cursor: pointer;
}
</style>
