import { ref } from 'vue'
import { notificationsApi } from '../api/notifications.api'

/**
 * The header badge's number. Replaces `ref(5)`, which every administrator was shown and nothing produced.
 *
 * Starts at zero and stays at zero if the request fails: a badge is a hint, and a guessed number is exactly
 * the defect this replaces.
 */
export function useUnreadCount() {
  const count = ref(0)

  notificationsApi
    .unreadCount()
    .then((value) => {
      count.value = value
    })
    .catch(() => {
      count.value = 0
    })

  return { count }
}
