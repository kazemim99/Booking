import { ref } from 'vue'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import { hasFullName } from '@/core/utils/person-name'

/**
 * A salon has to know who it is confirming (QA 2026-09-24), so a booking needs the customer's real first AND last
 * name. Sign-up stays phone-only; the name is asked for when they book, and only when they have none.
 *
 * `requireName(book)` books at once for a named customer; otherwise it opens the name form (bind `nameFormOpen` to a
 * `ProfileEditModal` and its `close` to `onNameFormClosed`), and books once the form closes with a name saved.
 */
export function useNameBeforeBooking() {
  const authStore = useAuthStore()
  const nameFormOpen = ref(false)
  let pending: (() => unknown) | null = null

  const customerHasFullName = () => hasFullName(authStore.user?.firstName, authStore.user?.lastName)

  function requireName(book: () => unknown): void {
    if (customerHasFullName()) {
      book()
      return
    }
    pending = book
    nameFormOpen.value = true
  }

  function onNameFormClosed(): void {
    nameFormOpen.value = false
    const book = pending
    pending = null
    if (book && customerHasFullName()) book()
  }

  return { nameFormOpen, requireName, onNameFormClosed }
}
