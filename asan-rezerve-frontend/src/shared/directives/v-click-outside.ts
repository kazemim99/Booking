import type { Directive } from 'vue'

export const vClickOutside: Directive<HTMLElement, () => void> = {
  mounted(el: HTMLElement, binding) {
    el.clickOutsideEvent = (event: Event) => {
      if (!(el === event.target || el.contains(event.target as Node))) {
        binding.value()
      }
    }
    setTimeout(() => {
      document.addEventListener('click', el.clickOutsideEvent!)
    }, 0)
  },

  unmounted(el: HTMLElement) {
    if (el.clickOutsideEvent) {
      document.removeEventListener('click', el.clickOutsideEvent)
      delete el.clickOutsideEvent
    }
  },
}

export default vClickOutside
