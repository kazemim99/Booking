import { ref, computed, nextTick } from 'vue'

export function useOtpInput(length = 6) {
  // Array to store each digit
  const digits = ref<string[]>(Array(length).fill(''))
  const inputRefs = ref<HTMLInputElement[]>([])

  // Computed full code
  const code = computed(() => digits.value.join(''))
  const isComplete = computed(() => digits.value.every((d) => d !== ''))

  // Set input refs
  const setInputRef = (index: number, el: HTMLInputElement | null) => {
    if (el) {
      inputRefs.value[index] = el
    }
  }

  // Handle input
  const handleInput = (index: number, event: Event) => {
    const input = event.target as HTMLInputElement
    const value = input.value

    // Only allow single digit
    if (value.length > 1) {
      input.value = value.slice(-1)
    }

    // Only allow numbers
    if (!/^\d*$/.test(input.value)) {
      input.value = ''
      return
    }

    digits.value[index] = input.value

    // Auto-advance to next input
    if (input.value && index < length - 1) {
      nextTick(() => {
        inputRefs.value[index + 1]?.focus()
      })
    }
  }

  // Handle keydown
  const handleKeydown = (index: number, event: KeyboardEvent) => {
    // Backspace: move to previous input if current is empty
    if (event.key === 'Backspace') {
      if (!digits.value[index] && index > 0) {
        nextTick(() => {
          inputRefs.value[index - 1]?.focus()
        })
      }
    }

    // Arrow Left: move to previous input
    if (event.key === 'ArrowLeft' && index > 0) {
      event.preventDefault()
      inputRefs.value[index - 1]?.focus()
    }

    // Arrow Right: move to next input
    if (event.key === 'ArrowRight' && index < length - 1) {
      event.preventDefault()
      inputRefs.value[index + 1]?.focus()
    }
  }

  // Handle paste
  const handlePaste = (event: ClipboardEvent) => {
    event.preventDefault()
    const pastedData = event.clipboardData?.getData('text') || ''

    // Only allow numeric paste
    if (!/^\d+$/.test(pastedData)) {
      return
    }

    // Fill digits from paste
    const pastedDigits = pastedData.slice(0, length).split('')
    pastedDigits.forEach((digit, index) => {
      digits.value[index] = digit
      if (inputRefs.value[index]) {
        inputRefs.value[index].value = digit
      }
    })

    // Focus on next empty input or last input
    const nextEmptyIndex = digits.value.findIndex((d) => !d)
    const focusIndex = nextEmptyIndex !== -1 ? nextEmptyIndex : length - 1

    nextTick(() => {
      inputRefs.value[focusIndex]?.focus()
    })
  }

  // Focus first input
  const focusFirst = () => {
    nextTick(() => {
      inputRefs.value[0]?.focus()
    })
  }

  // Clear all inputs
  const clear = () => {
    digits.value = Array(length).fill('')
    inputRefs.value.forEach((input) => {
      if (input) input.value = ''
    })
    focusFirst()
  }

  // Set error state (shake animation)
  const setError = () => {
    // Trigger shake animation via class
    inputRefs.value.forEach((input) => {
      input.classList.add('shake')
      setTimeout(() => {
        input.classList.remove('shake')
      }, 500)
    })
    clear()
  }

  return {
    digits: computed(() => digits.value),
    code,
    isComplete,
    setInputRef,
    handleInput,
    handleKeydown,
    handlePaste,
    focusFirst,
    clear,
    setError,
  }
}
