import { ref } from 'vue'
import { observabilityApi, type SystemOverview } from '../../api/observability.api'
import { messageOf } from './useLogExplorer'

/**
 * The Overview tab: process health, latency and errors of the last hour, the cache (with purge), the log store's
 * health, and the AI digest to copy into an assistant.
 */
export function useSystemOverview() {
  const overview = ref<SystemOverview | null>(null)
  const loading = ref(false)
  const error = ref('')
  const purging = ref(false)
  const digest = ref('')
  const digestLoading = ref(false)

  async function load() {
    loading.value = true
    error.value = ''
    try {
      overview.value = await observabilityApi.overview()
    } catch (e) {
      error.value = messageOf(e) || 'بارگذاری نمای کلی ناموفق بود'
    } finally {
      loading.value = false
    }
  }

  /** Evicts one tag, or everything; the cache figures are re-read afterwards. */
  async function purge(tag: string | null): Promise<boolean> {
    purging.value = true
    error.value = ''
    try {
      await observabilityApi.invalidateCache(tag ? { tag: tag.trim() } : { all: true })
      if (overview.value) overview.value = { ...overview.value, cache: await observabilityApi.cache() }
      return true
    } catch (e) {
      error.value = messageOf(e) || 'پاک‌سازی حافظهٔ نهان ناموفق بود'
      return false
    } finally {
      purging.value = false
    }
  }

  async function loadDigest(source?: string) {
    digestLoading.value = true
    error.value = ''
    try {
      digest.value = await observabilityApi.digestMarkdown({ source })
    } catch (e) {
      error.value = messageOf(e) || 'تهیهٔ خلاصه ناموفق بود'
    } finally {
      digestLoading.value = false
    }
  }

  return { overview, loading, error, purging, digest, digestLoading, load, purge, loadDigest }
}
