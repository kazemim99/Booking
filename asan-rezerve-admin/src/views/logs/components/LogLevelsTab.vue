<template>
  <div>
    <a-alert type="info" show-icon :message="t('logs.levelsTab.intro')" class="alert" />
    <a-alert type="warning" show-icon :message="t('logs.levelsTab.prodWarning')" class="alert" />
    <a-alert v-if="levels.error.value" type="error" show-icon :message="levels.error.value" class="alert" closable />

    <div class="add">
      <a-input
        v-model:value="newCategory"
        dir="ltr"
        class="category-input"
        :placeholder="t('logs.levelsTab.newCategoryPlaceholder')"
        :status="newCategory && !levels.isValidCategory(newCategory.trim()) ? 'error' : undefined"
      />
      <a-select v-model:value="newLevel" class="level-select">
        <a-select-option v-for="level in CATEGORY_LEVELS" :key="level" :value="level">{{ t(`logs.levels.${level}`) }}</a-select-option>
      </a-select>
      <a-select v-model:value="newDuration" class="duration-select">
        <a-select-option v-for="minutes in DURATIONS" :key="minutes" :value="minutes">{{ t(`logs.levelsTab.durations.${minutes}`) }}</a-select-option>
        <a-select-option :value="0">{{ t('logs.levelsTab.permanent') }}</a-select-option>
      </a-select>
      <a-button type="primary" :disabled="!levels.isValidCategory(newCategory.trim())" :loading="levels.busy.value === newCategory.trim()" @click="addCategory">
        {{ t('logs.levelsTab.set') }}
      </a-button>
    </div>

    <a-table :columns="columns" :data-source="levels.rows.value" :loading="levels.loading.value" :pagination="false" row-key="category" size="small" :scroll="{ x: 900 }">
      <template #bodyCell="{ column, record }">
        <template v-if="column.key === 'category'">
          <code dir="ltr">{{ record.category }}</code>
        </template>
        <template v-else-if="column.key === 'configured'">
          <a-tag v-if="record.configuredLevel" :color="LEVEL_COLORS[record.configuredLevel as CategoryLevel]">{{ t(`logs.levels.${record.configuredLevel}`) }}</a-tag>
          <span v-else class="muted">{{ t('logs.levelsTab.inherits') }}</span>
        </template>
        <template v-else-if="column.key === 'effective'">
          <a-tag :color="LEVEL_COLORS[record.effectiveLevel as CategoryLevel]">{{ t(`logs.levels.${record.effectiveLevel}`) }}</a-tag>
        </template>
        <template v-else-if="column.key === 'override'">
          <template v-if="record.override">
            <div>
              {{ record.override.expiresAt ? t('logs.levelsTab.until', { time: formatTimestamp(record.override.expiresAt) }) : t('logs.levelsTab.untilReset') }}
            </div>
            <div class="muted">{{ t('logs.levelsTab.by', { name: record.override.updatedBy }) }}</div>
          </template>
        </template>
        <template v-else-if="column.key === 'actions'">
          <a-space>
            <a-select v-model:value="pending[record.category]" class="level-select" size="small" :placeholder="t('logs.columns.level')">
              <a-select-option v-for="level in CATEGORY_LEVELS" :key="level" :value="level">{{ t(`logs.levels.${level}`) }}</a-select-option>
            </a-select>
            <a-select v-model:value="durations[record.category]" class="duration-select" size="small">
              <a-select-option v-for="minutes in DURATIONS" :key="minutes" :value="minutes">{{ t(`logs.levelsTab.durations.${minutes}`) }}</a-select-option>
              <a-select-option :value="0">{{ t('logs.levelsTab.permanent') }}</a-select-option>
            </a-select>
            <a-button
              size="small"
              type="primary"
              :disabled="!pending[record.category]"
              :loading="levels.busy.value === record.category"
              @click="apply(record.category)"
            >
              {{ t('logs.levelsTab.set') }}
            </a-button>
            <a-button v-if="record.override" size="small" :loading="levels.busy.value === record.category" @click="reset(record.category)">
              {{ t('logs.levelsTab.reset') }}
            </a-button>
          </a-space>
        </template>
      </template>
    </a-table>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { message } from 'ant-design-vue'
import { CATEGORY_LEVELS, type CategoryLevel } from '../../../api/observability.api'
import { formatTimestamp } from '../../../utils/date'
import { DURATIONS, useLogLevels } from '../useLogLevels'
import { LEVEL_COLORS } from './levelColors'

const DEFAULT_MINUTES = 30

const { t } = useI18n()
const levels = useLogLevels()

const pending = reactive<Record<string, CategoryLevel | undefined>>({})
const durations = reactive<Record<string, number>>({})
const newCategory = ref('')
const newLevel = ref<CategoryLevel>('Debug')
const newDuration = ref<number>(DEFAULT_MINUTES)

const columns = computed(() => [
  { title: t('logs.levelsTab.category'), key: 'category' },
  { title: t('logs.levelsTab.configured'), key: 'configured', width: 120 },
  { title: t('logs.levelsTab.effective'), key: 'effective', width: 120 },
  { title: t('logs.levelsTab.override'), key: 'override', width: 220 },
  { title: t('logs.columns.actions'), key: 'actions', width: 420 },
])

async function apply(category: string) {
  const level = pending[category]
  if (!level) return
  const minutes = durations[category] ?? DEFAULT_MINUTES
  if (await levels.set(category, level, minutes === 0 ? null : minutes)) {
    pending[category] = undefined
    message.success(t('logs.levelsTab.applied'))
  }
}

async function reset(category: string) {
  if (await levels.reset(category)) message.success(t('logs.levelsTab.restored'))
}

async function addCategory() {
  const minutes = newDuration.value
  if (await levels.set(newCategory.value, newLevel.value, minutes === 0 ? null : minutes)) {
    newCategory.value = ''
    message.success(t('logs.levelsTab.applied'))
  }
}

onMounted(levels.load)
</script>

<style scoped>
.alert {
  margin-bottom: 12px;
}
.add {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-bottom: 16px;
}
.category-input {
  width: 380px;
}
.level-select {
  width: 130px;
}
.duration-select {
  width: 130px;
}
.muted {
  color: rgba(0, 0, 0, 0.45);
  font-size: 12px;
}
</style>
