import { ref } from 'vue'
import { reviewsApi, type ModerationFilter, type ModerationItem } from '../../api/reviews.api'

export type ModerationAction = 'approve' | 'reject' | 'hide' | 'restore' | 'approveReply' | 'rejectReply'

/** Actions that must carry the moderator's reason. */
export const NEEDS_REASON: ReadonlySet<ModerationAction> = new Set(['reject', 'hide', 'rejectReply'])

/**
 * Who ends up reading the reason typed for an action — the moderation form says so before it is sent.
 * Rejecting a review notifies its author with the reason (task 7.7); rejecting a reply shows it to the salon
 * in the provider app. Hiding notifies nobody, so its reason stays internal.
 */
export function reasonAudience(action: ModerationAction): 'customer' | 'provider' | null {
  if (action === 'reject') return 'customer'
  if (action === 'rejectReply') return 'provider'
  return null
}

/**
 * The moderation page's behaviour, kept out of the template so it can be tested without mounting Ant Design.
 *
 * An item leaves the list only once the server accepted the decision. A failed load is an error — never an empty
 * queue, which would read as "nothing to moderate".
 */
export function useReviewModeration(pageSize = 20) {
  const filter = ref<ModerationFilter>('pending')
  const items = ref<ModerationItem[]>([])
  const totalCount = ref(0)
  const loading = ref(false)
  const loaded = ref(false)
  const error = ref('')
  const busy = ref<string | null>(null)

  async function load() {
    loading.value = true
    error.value = ''
    try {
      const page = await reviewsApi.queue(filter.value, 1, pageSize)
      items.value = page.items
      totalCount.value = page.totalCount
      loaded.value = true
    } catch (e) {
      loaded.value = false
      error.value = messageOf(e) || 'بارگذاری صف بررسی ناموفق بود'
    } finally {
      loading.value = false
    }
  }

  async function setFilter(next: ModerationFilter) {
    filter.value = next
    await load()
  }

  async function act(action: ModerationAction, item: ModerationItem, reason = ''): Promise<boolean> {
    busy.value = item.reviewId
    error.value = ''
    try {
      if (NEEDS_REASON.has(action)) {
        await (reviewsApi[action] as (id: string, r: string) => Promise<void>)(item.reviewId, reason)
      } else {
        await (reviewsApi[action] as (id: string) => Promise<void>)(item.reviewId)
      }
      items.value = items.value.filter((i) => i.reviewId !== item.reviewId)
      totalCount.value = Math.max(0, totalCount.value - 1)
      return true
    } catch (e) {
      error.value = messageOf(e) || 'این تصمیم ثبت نشد'
      return false
    } finally {
      busy.value = null
    }
  }

  return { filter, items, totalCount, loading, loaded, error, busy, load, setFilter, act }
}

function messageOf(e: unknown): string {
  const response = (e as { response?: { data?: { message?: string } } })?.response
  return response?.data?.message ?? (e instanceof Error ? e.message : '')
}
