import { ref } from 'vue'
import {
  promotionsApi,
  type LifecycleAction,
  type Promotion,
  type PromotionDetails,
  type PromotionOwner,
  type PromotionStatus,
  type PromotionTermsInput,
} from '../../api/promotions.api'

/**
 * The promotions page's behaviour, testable without Ant Design. A failed load is an error with a retry, never an
 * empty table (which would read as "no promotions"); a row changes only after the server accepted the change.
 */
export function usePromotions(pageSize = 20) {
  const owner = ref<PromotionOwner>('Platform')
  const status = ref<PromotionStatus | 'all'>('all')
  const search = ref('')
  const page = ref(1)
  const items = ref<Promotion[]>([])
  const totalCount = ref(0)
  const loading = ref(false)
  const loaded = ref(false)
  const error = ref('')
  const busy = ref<string | null>(null)

  async function load() {
    loading.value = true
    error.value = ''
    try {
      const result = await promotionsApi.search({
        owner: owner.value,
        status: status.value,
        search: search.value,
        page: page.value,
        pageSize,
      })
      items.value = result.items
      totalCount.value = result.totalCount
      loaded.value = true
    } catch (e) {
      loaded.value = false
      error.value = messageOf(e) || 'بارگذاری تخفیف‌ها ناموفق بود'
    } finally {
      loading.value = false
    }
  }

  async function setOwner(next: PromotionOwner) {
    owner.value = next
    page.value = 1
    await load()
  }

  async function applyFilters() {
    page.value = 1
    await load()
  }

  async function goTo(next: number) {
    page.value = next
    await load()
  }

  function replace(updated: Promotion) {
    items.value = items.value.map((p) => (p.id === updated.id ? { ...p, ...updated, providerName: p.providerName } : p))
  }

  async function act(promotion: Promotion, action: LifecycleAction): Promise<boolean> {
    busy.value = promotion.id
    error.value = ''
    try {
      replace(await promotionsApi.changeStatus(promotion.id, action))
      return true
    } catch (e) {
      error.value = messageOf(e) || 'این تغییر ثبت نشد'
      return false
    } finally {
      busy.value = null
    }
  }

  /** Creates or edits a platform campaign. Returns the server's message on failure, so the form can show it. */
  async function save(terms: PromotionTermsInput, editingId?: string): Promise<{ ok: true } | { ok: false; message: string }> {
    try {
      if (editingId) {
        replace(await promotionsApi.updateCampaign(editingId, terms))
      } else {
        await promotionsApi.createCampaign(terms)
        owner.value = 'Platform'
        page.value = 1
        await load()
      }
      return { ok: true }
    } catch (e) {
      return { ok: false, message: messageOf(e) || 'ذخیره انجام نشد' }
    }
  }

  async function details(id: string): Promise<PromotionDetails | null> {
    try {
      return await promotionsApi.details(id)
    } catch (e) {
      error.value = messageOf(e) || 'جزئیات بارگذاری نشد'
      return null
    }
  }

  return {
    owner, status, search, page, items, totalCount, loading, loaded, error, busy,
    load, setOwner, applyFilters, goTo, act, save, details,
  }
}

function messageOf(e: unknown): string {
  const response = (e as { response?: { data?: { message?: string } } })?.response
  return response?.data?.message ?? ''
}
