<template>
  <svg
    class="brand-mark"
    :class="[`brand-mark--${theme}`]"
    viewBox="0 0 100 100"
    fill="none"
    aria-hidden="true"
  >
    <line x1="50" y1="13" x2="14" y2="83" :stroke="letterColor" stroke-width="8.5" stroke-linecap="round" />
    <line x1="50" y1="13" x2="86" y2="83" :stroke="letterColor" stroke-width="8.5" stroke-linecap="round" />
    <polyline
      points="32,48 54,70 82,25"
      :stroke="checkColor"
      stroke-width="8.5"
      stroke-linecap="round"
      stroke-linejoin="round"
    />
  </svg>
</template>

<script setup lang="ts">
import { computed } from 'vue'

/**
 * The AsanRezerve symbol mark: an "A" whose crossbar is a check mark.
 *
 * Drawn inline from the designer's geometry (branding/symbol-color.svg, symbol-reversed-*.svg — same
 * 100x100 grid, see branding/README.md at the repo root) instead of an <img>, so it recolors for the
 * sider's dark chrome and stays crisp at any size without an extra request.
 */
interface Props {
  /** 'color': blue letter, green check, for light backgrounds. 'reversed': white on a dark background. */
  theme?: 'color' | 'reversed'
}

const props = withDefaults(defineProps<Props>(), { theme: 'color' })

const letterColor = computed(() => (props.theme === 'reversed' ? '#FFFFFF' : '#3777C0'))
const checkColor = computed(() => (props.theme === 'reversed' ? '#FFFFFF' : '#0AC075'))
</script>

<style scoped>
.brand-mark {
  display: block;
  width: 1em;
  height: 1em;
}
</style>
