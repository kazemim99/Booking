// If you need mixins, use proper typing
import type { ComponentOptions } from 'vue'

export const formMixin: ComponentOptions = {
  data() {
    return {
      isSubmitting: false,
      errors: {} as Record<string, string>,
    }
  },
  methods: {
    setError(field: string, message: string): void {
      this.errors[field] = message
    },
    clearError(field: string): void {
      delete this.errors[field]
    },
    clearAllErrors(): void {
      this.errors = {}
    },
  },
}
