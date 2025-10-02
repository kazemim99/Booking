<template>
  <div class="tabs" :class="{ vertical: orientation === 'vertical' }">
    <!-- Tab List -->
    <div
      class="tabs-list"
      role="tablist"
      :aria-orientation="orientation"
      :class="{ scrollable: scrollable }"
    >
      <button
        v-for="(tab, index) in tabs"
        :key="tab.value"
        :ref="(el) => setTabRef(el, index)"
        class="tab-button"
        :class="{
          active: isActive(tab.value),
          disabled: tab.disabled,
        }"
        :disabled="tab.disabled"
        role="tab"
        :aria-selected="isActive(tab.value)"
        :aria-controls="`tabpanel-${tab.value}`"
        :tabindex="isActive(tab.value) ? 0 : -1"
        @click="handleTabClick(tab.value)"
        @keydown="handleKeyDown($event, index)"
      >
        <!-- Icon (optional) -->
        <span v-if="tab.icon" class="tab-icon" v-html="tab.icon"></span>

        <!-- Label -->
        <span class="tab-label">{{ tab.label }}</span>

        <!-- Badge (optional) -->
        <span v-if="tab.badge" class="tab-badge">{{ tab.badge }}</span>
      </button>

      <!-- Active indicator -->
      <span v-if="showIndicator" class="tab-indicator" :style="indicatorStyle"></span>
    </div>

    <!-- Tab Panels -->
    <div class="tabs-content">
      <slot></slot>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, nextTick } from 'vue'

// Types
export interface Tab {
  label: string
  value: string
  icon?: string
  badge?: string | number
  disabled?: boolean
}

export type TabOrientation = 'horizontal' | 'vertical'
export type TabVariant = 'default' | 'pills' | 'underline'

// Props
interface Props {
  tabs: Tab[]
  modelValue?: string
  defaultValue?: string
  orientation?: TabOrientation
  variant?: TabVariant
  showIndicator?: boolean
  scrollable?: boolean
  disabled?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  modelValue: undefined,
  defaultValue: undefined,
  orientation: 'horizontal',
  variant: 'default',
  showIndicator: true,
  scrollable: false,
  disabled: false,
})

// Emits
const emit = defineEmits<{
  (e: 'update:modelValue', value: string): void
  (e: 'change', value: string): void
}>()

// State
const internalValue = ref<string>('')
const tabRefs = ref<(HTMLElement | null)[]>([])
const indicatorStyle = ref<Record<string, string>>({})

// Computed
const activeTab = computed(() => {
  return props.modelValue !== undefined ? props.modelValue : internalValue.value
})

const activeIndex = computed(() => {
  return props.tabs.findIndex((tab) => tab.value === activeTab.value)
})

// Methods
const isActive = (value: string): boolean => {
  return activeTab.value === value
}

const setTabRef = (el: unknown, index: number) => {
  if (el instanceof HTMLElement) {
    tabRefs.value[index] = el
  }
}

const handleTabClick = (value: string) => {
  if (props.disabled) return

  const tab = props.tabs.find((t) => t.value === value)
  if (tab?.disabled) return

  if (props.modelValue !== undefined) {
    emit('update:modelValue', value)
  } else {
    internalValue.value = value
  }

  emit('change', value)
  updateIndicator()
}

const handleKeyDown = (event: KeyboardEvent, currentIndex: number) => {
  const { key } = event
  let nextIndex = currentIndex

  if (props.orientation === 'horizontal') {
    if (key === 'ArrowRight') {
      nextIndex = findNextEnabledTab(currentIndex, 1)
      event.preventDefault()
    } else if (key === 'ArrowLeft') {
      nextIndex = findNextEnabledTab(currentIndex, -1)
      event.preventDefault()
    }
  } else {
    if (key === 'ArrowDown') {
      nextIndex = findNextEnabledTab(currentIndex, 1)
      event.preventDefault()
    } else if (key === 'ArrowUp') {
      nextIndex = findNextEnabledTab(currentIndex, -1)
      event.preventDefault()
    }
  }

  if (key === 'Home') {
    nextIndex = findNextEnabledTab(-1, 1)
    event.preventDefault()
  } else if (key === 'End') {
    nextIndex = findNextEnabledTab(props.tabs.length, -1)
    event.preventDefault()
  }

  if (nextIndex !== currentIndex && nextIndex !== -1) {
    const nextTab = props.tabs[nextIndex]
    if (nextTab) {
      handleTabClick(nextTab.value)
      tabRefs.value[nextIndex]?.focus()
    }
  }
}

const findNextEnabledTab = (startIndex: number, direction: number): number => {
  const length = props.tabs.length
  let index = startIndex + direction

  while (index >= 0 && index < length) {
    if (!props.tabs[index].disabled) {
      return index
    }
    index += direction
  }

  return -1
}

const updateIndicator = async () => {
  if (!props.showIndicator) return

  await nextTick()

  const activeTabElement = tabRefs.value[activeIndex.value]
  if (!activeTabElement) return

  if (props.orientation === 'horizontal') {
    indicatorStyle.value = {
      left: `${activeTabElement.offsetLeft}px`,
      width: `${activeTabElement.offsetWidth}px`,
    }
  } else {
    indicatorStyle.value = {
      top: `${activeTabElement.offsetTop}px`,
      height: `${activeTabElement.offsetHeight}px`,
    }
  }
}

// Initialize
onMounted(() => {
  // Set initial value
  if (props.modelValue) {
    internalValue.value = props.modelValue
  } else if (props.defaultValue) {
    internalValue.value = props.defaultValue
  } else {
    const firstEnabled = props.tabs.find((tab) => !tab.disabled)
    if (firstEnabled) {
      internalValue.value = firstEnabled.value
    }
  }

  updateIndicator()
})

// Watch for changes
watch(activeTab, () => {
  updateIndicator()
})

watch(
  () => props.tabs,
  () => {
    updateIndicator()
  },
  { deep: true },
)
</script>

<style scoped>
.tabs {
  display: flex;
  flex-direction: column;
  width: 100%;
}

.tabs.vertical {
  flex-direction: row;
}

/* Tab List */
.tabs-list {
  position: relative;
  display: flex;
  gap: 0.5rem;
  background: var(--color-bg-secondary);
  padding: 0.5rem;
  border-radius: 12px;
  overflow-x: auto;
  scrollbar-width: none;
}

.tabs-list::-webkit-scrollbar {
  display: none;
}

.tabs-list.scrollable {
  overflow-x: auto;
}

.vertical .tabs-list {
  flex-direction: column;
  min-width: 200px;
}

/* Tab Buttons */
.tab-button {
  position: relative;
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.75rem 1.25rem;
  background: transparent;
  border: none;
  border-radius: 8px;
  font-size: 0.95rem;
  font-weight: 500;
  color: var(--color-text-secondary);
  cursor: pointer;
  transition: all 0.2s;
  white-space: nowrap;
  z-index: 1;
}

.tab-button:hover:not(.disabled) {
  color: var(--color-text-primary);
  background: rgba(0, 0, 0, 0.03);
}

.tab-button:focus-visible {
  outline: 2px solid var(--color-primary);
  outline-offset: 2px;
}

.tab-button.active {
  color: var(--color-primary);
  background: white;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.tab-button.disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

/* Tab Icon */
.tab-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 20px;
  height: 20px;
}

.tab-icon :deep(svg) {
  width: 100%;
  height: 100%;
  stroke: currentColor;
}

/* Tab Badge */
.tab-badge {
  display: flex;
  align-items: center;
  justify-content: center;
  min-width: 20px;
  height: 20px;
  padding: 0 0.375rem;
  background: var(--color-primary);
  color: white;
  border-radius: 10px;
  font-size: 0.75rem;
  font-weight: 600;
}

/* Active Indicator */
.tab-indicator {
  position: absolute;
  bottom: 0.5rem;
  background: var(--color-primary);
  border-radius: 8px;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  pointer-events: none;
  z-index: 0;
}

.tabs:not(.vertical) .tab-indicator {
  height: calc(100% - 1rem);
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.vertical .tab-indicator {
  left: 0.5rem;
  width: calc(100% - 1rem);
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

/* Tab Content */
.tabs-content {
  padding: 1.5rem 0;
}

.vertical .tabs-content {
  flex: 1;
  padding: 0 0 0 1.5rem;
}

/* Variants */
.tabs.pills .tab-button {
  border-radius: 20px;
}

.tabs.pills .tab-button.active {
  background: var(--color-primary);
  color: white;
}

.tabs.underline .tabs-list {
  background: transparent;
  padding: 0;
  border-bottom: 2px solid var(--color-border);
  border-radius: 0;
}

.tabs.underline .tab-button {
  border-radius: 0;
  padding-bottom: 1rem;
}

.tabs.underline .tab-button.active {
  background: transparent;
  box-shadow: none;
}

.tabs.underline .tab-indicator {
  height: 2px;
  bottom: -2px;
  box-shadow: none;
}

/* Responsive */
@media (max-width: 768px) {
  .tab-button {
    padding: 0.625rem 1rem;
    font-size: 0.875rem;
  }

  .tabs.vertical {
    flex-direction: column;
  }

  .vertical .tabs-list {
    flex-direction: row;
    min-width: auto;
  }

  .vertical .tabs-content {
    padding: 1.5rem 0 0 0;
  }
}
</style>
