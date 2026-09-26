import { ref } from 'vue'
import { observabilityApi, type CategoryLevel, type LogLevelRow } from '../../api/observability.api'
import { messageOf } from './useLogExplorer'

/** The same rule the server enforces: a namespace or type name. */
const CATEGORY = /^[A-Za-z_][A-Za-z0-9_.]*$/

export const DURATIONS = [15, 30, 60, 240, 1440] as const

/**
 * The Log levels tab. A change applies at once on the server and is audited there; the row is replaced only with
 * what the server answered, so the page never shows a level that is not in force.
 */
export function useLogLevels() {
  const rows = ref<LogLevelRow[]>([])
  const loading = ref(false)
  const error = ref('')
  const busy = ref<string | null>(null)

  async function load() {
    loading.value = true
    error.value = ''
    try {
      rows.value = await observabilityApi.logLevels()
    } catch (e) {
      error.value = messageOf(e) || 'بارگذاری سطوح گزارش ناموفق بود'
    } finally {
      loading.value = false
    }
  }

  function isValidCategory(category: string): boolean {
    return category.length > 0 && category.length <= 200 && CATEGORY.test(category)
  }

  /** `durationMinutes` null = until reset. */
  async function set(category: string, level: CategoryLevel, durationMinutes: number | null): Promise<boolean> {
    const name = category.trim()
    if (!isValidCategory(name)) {
      error.value = 'نام دسته معتبر نیست'
      return false
    }

    busy.value = name
    error.value = ''
    try {
      const row = await observabilityApi.setLogLevel(name, level, durationMinutes)
      const index = rows.value.findIndex((r) => r.category === row.category)
      if (index >= 0) rows.value.splice(index, 1, row)
      else rows.value.push(row)
      return true
    } catch (e) {
      error.value = messageOf(e) || 'تغییر سطح گزارش ناموفق بود'
      return false
    } finally {
      busy.value = null
    }
  }

  async function reset(category: string): Promise<boolean> {
    busy.value = category
    error.value = ''
    try {
      await observabilityApi.resetLogLevel(category)
      await load()
      return true
    } catch (e) {
      error.value = messageOf(e) || 'بازگرداندن سطح گزارش ناموفق بود'
      return false
    } finally {
      busy.value = null
    }
  }

  return { rows, loading, error, busy, load, set, reset, isValidCategory }
}
