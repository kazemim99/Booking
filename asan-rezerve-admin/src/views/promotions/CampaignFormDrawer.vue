<template>
  <a-drawer
    :open="open"
    :title="editing ? t('promotions.form.editTitle') : t('promotions.form.createTitle')"
    :width="560"
    destroy-on-close
    @close="emit('close')"
  >
    <a-alert
      type="info"
      show-icon
      class="mb"
      :message="t('promotions.form.fundingNote')"
    />

    <a-form layout="vertical" @submit.prevent="submit">
      <a-form-item :label="t('promotions.form.title')" :validate-status="status('title')" :help="help('title')" required>
        <a-input v-model:value="form.title" :maxlength="TITLE_MAX" show-count :placeholder="t('promotions.form.titlePlaceholder')" />
      </a-form-item>

      <a-form-item :label="t('promotions.form.description')" :validate-status="status('description')" :help="help('description')">
        <a-textarea v-model:value="form.description" :rows="2" :maxlength="DESCRIPTION_MAX" show-count />
      </a-form-item>

      <a-divider orientation="right">{{ t('promotions.form.sectionBenefit') }}</a-divider>

      <a-form-item :label="t('promotions.form.activation')">
        <a-radio-group v-model:value="form.activation" button-style="solid">
          <a-radio-button value="Automatic">{{ t('promotions.activation.Automatic') }}</a-radio-button>
          <a-radio-button value="Code">{{ t('promotions.activation.Code') }}</a-radio-button>
        </a-radio-group>
        <div class="hint">{{ t(`promotions.form.activationHint.${form.activation}`) }}</div>
      </a-form-item>

      <a-form-item
        v-if="form.activation === 'Code'"
        :label="t('promotions.form.code')"
        :validate-status="status('code')"
        :help="help('code') || t('promotions.form.codeHint')"
        required
      >
        <a-input-group compact>
          <a-input
            v-model:value="form.code"
            class="code-input"
            dir="ltr"
            :maxlength="20"
            :disabled="codeLocked"
            @blur="form.code = normalizeCode(form.code)"
          />
          <a-button :disabled="codeLocked" @click="form.code = generateCode()">{{ t('promotions.form.generate') }}</a-button>
        </a-input-group>
        <div v-if="codeLocked" class="hint">{{ t('promotions.form.codeLocked') }}</div>
      </a-form-item>

      <a-form-item :label="t('promotions.form.kind')">
        <a-segmented
          v-model:value="form.discountKind"
          :options="[
            { label: t('promotions.kind.Percentage'), value: 'Percentage' },
            { label: t('promotions.kind.FixedAmount'), value: 'FixedAmount' },
          ]"
        />
      </a-form-item>

      <a-row :gutter="12">
        <a-col :span="12">
          <a-form-item
            :label="form.discountKind === 'Percentage' ? t('promotions.form.percent') : t('promotions.form.amount')"
            :validate-status="status('discountValue')"
            :help="help('discountValue')"
            required
          >
            <a-input-number
              v-model:value="form.discountValue"
              class="full"
              :min="form.discountKind === 'Percentage' ? MIN_PERCENT : 1"
              :max="form.discountKind === 'Percentage' ? MAX_PERCENT : undefined"
              :step="form.discountKind === 'Percentage' ? 5 : 10000"
              :addon-after="form.discountKind === 'Percentage' ? '٪' : t('promotions.toman')"
            />
          </a-form-item>
        </a-col>
        <a-col v-if="form.discountKind === 'Percentage'" :span="12">
          <a-form-item :label="t('promotions.form.cap')" :validate-status="status('maxDiscountAmount')" :help="help('maxDiscountAmount')">
            <a-input-number v-model:value="form.maxDiscountAmount" class="full" :min="1" :step="10000" :addon-after="t('promotions.toman')" />
          </a-form-item>
        </a-col>
      </a-row>

      <a-divider orientation="right">{{ t('promotions.form.sectionConditions') }}</a-divider>

      <a-form-item :label="t('promotions.form.validity')" :validate-status="status('endsAt')" :help="help('endsAt')">
        <a-row :gutter="8">
          <a-col :span="12">
            <a-date-picker v-model:value="startPicker" show-time class="full" :placeholder="t('promotions.form.startsNow')" />
            <div class="hint">{{ form.startsAt ? formatDateTime(form.startsAt) : t('promotions.form.startsNow') }}</div>
          </a-col>
          <a-col :span="12">
            <a-date-picker v-model:value="endPicker" show-time class="full" :placeholder="t('promotions.form.noEnd')" />
            <div class="hint">{{ form.endsAt ? formatDateTime(form.endsAt) : t('promotions.form.noEnd') }}</div>
          </a-col>
        </a-row>
      </a-form-item>

      <a-form-item :label="t('promotions.form.days')">
        <a-checkbox-group v-model:value="form.daysOfWeek" :options="dayOptions" />
        <div class="hint">{{ t('promotions.form.daysHint') }}</div>
      </a-form-item>

      <a-form-item :validate-status="status('dailyStartTime')" :help="help('dailyStartTime')">
        <a-checkbox v-model:checked="form.useDailyWindow">{{ t('promotions.form.dailyWindow') }}</a-checkbox>
        <div v-if="form.useDailyWindow" class="window">
          <a-time-picker v-model:value="form.dailyStartTime" format="HH:mm" value-format="HH:mm" :minute-step="15" />
          <span>{{ t('promotions.form.to') }}</span>
          <a-time-picker v-model:value="form.dailyEndTime" format="HH:mm" value-format="HH:mm" :minute-step="15" />
        </div>
        <div class="hint">{{ t('promotions.form.dailyWindowHint') }}</div>
      </a-form-item>

      <a-row :gutter="12">
        <a-col :span="12">
          <a-form-item :label="t('promotions.form.minimum')" :validate-status="status('minimumSubtotal')" :help="help('minimumSubtotal')">
            <a-input-number v-model:value="form.minimumSubtotal" class="full" :min="0" :step="50000" :addon-after="t('promotions.toman')" />
          </a-form-item>
        </a-col>
        <a-col :span="12">
          <a-form-item :label="t('promotions.form.audience')">
            <a-switch v-model:checked="form.newCustomersOnly" />
            <span class="switch-label">{{ t('promotions.form.newCustomersOnly') }}</span>
          </a-form-item>
        </a-col>
      </a-row>

      <a-row :gutter="12">
        <a-col :span="12">
          <a-form-item :label="t('promotions.form.totalLimit')" :validate-status="status('totalUsageLimit')" :help="help('totalUsageLimit')">
            <a-input-number v-model:value="form.totalUsageLimit" class="full" :min="1" :placeholder="t('promotions.form.unlimited')" />
          </a-form-item>
        </a-col>
        <a-col :span="12">
          <a-form-item :label="t('promotions.form.perCustomerLimit')" :validate-status="status('perCustomerLimit')" :help="help('perCustomerLimit')">
            <a-input-number v-model:value="form.perCustomerLimit" class="full" :min="1" :placeholder="t('promotions.form.unlimited')" />
          </a-form-item>
        </a-col>
      </a-row>

      <a-card size="small" class="preview" :title="t('promotions.form.preview')">
        <div class="preview__badge">{{ previewBenefit }}</div>
        <div class="preview__title">{{ form.title || t('promotions.form.titlePlaceholder') }}</div>
        <div v-if="form.activation === 'Code' && form.code" class="preview__code" dir="ltr">{{ normalizeCode(form.code) }}</div>
      </a-card>

      <a-alert v-if="serverError" type="error" show-icon :message="serverError" class="mb" />
    </a-form>

    <template #footer>
      <a-space>
        <a-button @click="emit('close')">{{ t('common.cancel') }}</a-button>
        <a-button type="primary" :loading="saving" @click="submit">{{ t('common.save') }}</a-button>
      </a-space>
    </template>
  </a-drawer>
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import dayjs, { type Dayjs } from 'dayjs'
import type { Promotion, PromotionTermsInput } from '../../api/promotions.api'
import { formatDateTime } from '../../utils/date'
import {
  DESCRIPTION_MAX,
  MAX_PERCENT,
  MIN_PERCENT,
  PERSIAN_WEEK,
  TITLE_MAX,
  benefitText,
  emptyForm,
  formFromPromotion,
  generateCode,
  normalizeCode,
  toPayload,
  validateForm,
  type FormErrors,
  type PromotionForm,
} from './promotionForm'

const props = defineProps<{
  open: boolean
  editing: Promotion | null
  save: (terms: PromotionTermsInput, editingId?: string) => Promise<{ ok: true } | { ok: false; message: string }>
}>()
const emit = defineEmits<{ close: []; saved: [] }>()

const { t } = useI18n()
const form = reactive<PromotionForm>(emptyForm())
const errors = ref<FormErrors>({})
const serverError = ref('')
const saving = ref(false)
const submitted = ref(false)

// A code customers were already given cannot change (the server refuses it too).
const codeLocked = computed(() => !!props.editing && props.editing.uses > 0)

watch(
  () => [props.open, props.editing] as const,
  ([open, editing]) => {
    if (!open) return
    Object.assign(form, editing ? formFromPromotion(editing) : emptyForm())
    errors.value = {}
    serverError.value = ''
    submitted.value = false
  },
  { immediate: true },
)

watch(form, () => {
  if (submitted.value) errors.value = validateForm(form)
})

const startPicker = computed<Dayjs | undefined>({
  get: () => (form.startsAt ? dayjs(form.startsAt) : undefined),
  set: (v) => (form.startsAt = v ? v.toISOString() : null),
})
const endPicker = computed<Dayjs | undefined>({
  get: () => (form.endsAt ? dayjs(form.endsAt) : undefined),
  set: (v) => (form.endsAt = v ? v.toISOString() : null),
})

const dayOptions = computed(() => PERSIAN_WEEK.map((d) => ({ label: t(`promotions.days.${d}`), value: d })))

const previewBenefit = computed(() =>
  benefitText({
    discountKind: form.discountKind,
    discountValue: form.discountValue ?? 0,
    maxDiscountAmount: form.discountKind === 'Percentage' ? form.maxDiscountAmount : null,
  }),
)

const status = (field: keyof PromotionForm) => (errors.value[field] ? 'error' : undefined)
const help = (field: keyof PromotionForm) => (errors.value[field] ? t(errors.value[field]!) : undefined)

async function submit() {
  submitted.value = true
  errors.value = validateForm(form)
  if (Object.keys(errors.value).length) return

  saving.value = true
  serverError.value = ''
  const result = await props.save(toPayload(form), props.editing?.id)
  saving.value = false
  if (result.ok) emit('saved')
  else serverError.value = result.message
}
</script>

<style scoped>
.mb { margin-bottom: 16px; }
.full { width: 100%; }
.hint { color: var(--color-gray-500, #8c8c8c); font-size: 12px; margin-top: 4px; }
.code-input { width: calc(100% - 96px); font-family: monospace; letter-spacing: 1px; }
.window { display: flex; align-items: center; gap: 8px; margin-top: 8px; }
.switch-label { margin-inline-start: 8px; }
.preview { background: var(--color-primary-50, #f6f8ff); margin-bottom: 16px; }
.preview__badge {
  display: inline-block; padding: 2px 10px; border-radius: 999px; font-weight: 600;
  background: var(--color-success-100, #e6f7ea); color: var(--color-success-700, #237804);
}
.preview__title { margin-top: 8px; font-weight: 600; }
.preview__code { margin-top: 6px; font-family: monospace; letter-spacing: 2px; }
</style>
