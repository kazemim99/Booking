import { describe, it, expect, vi } from 'vitest'
import { reactive } from 'vue'

/**
 * A booking needs the customer's real first AND last name (QA 2026-09-24) — asked for when they book, only when they
 * have none. Shared by every web path that creates a booking: the booking wizard and "rebook" in My Bookings.
 */

const auth = reactive({ user: null as null | { firstName: string; lastName: string } })
vi.mock('@/core/stores/modules/auth.store', () => ({ useAuthStore: () => auth }))

const { useNameBeforeBooking } = await import('../useNameBeforeBooking')

describe('useNameBeforeBooking', () => {
  it('books at once for a customer who has a real name', () => {
    auth.user = { firstName: 'مصطفی', lastName: 'کاظمی' }
    const gate = useNameBeforeBooking()
    const book = vi.fn()

    gate.requireName(book)

    expect(book).toHaveBeenCalledTimes(1)
    expect(gate.nameFormOpen.value).toBe(false)
  })

  it('asks a nameless customer for their name first, then books once it is saved', () => {
    auth.user = { firstName: 'مشتری', lastName: '9384444636' }
    const gate = useNameBeforeBooking()
    const book = vi.fn()

    gate.requireName(book)
    expect(gate.nameFormOpen.value).toBe(true)
    expect(book).not.toHaveBeenCalled()

    auth.user = { firstName: 'مصطفی', lastName: 'کاظمی' }
    gate.onNameFormClosed()

    expect(gate.nameFormOpen.value).toBe(false)
    expect(book).toHaveBeenCalledTimes(1)
  })

  it('books nothing when the form is closed without a name', () => {
    auth.user = { firstName: 'مشتری', lastName: '9384444636' }
    const gate = useNameBeforeBooking()
    const book = vi.fn()

    gate.requireName(book)
    gate.onNameFormClosed()

    expect(book).not.toHaveBeenCalled()
  })
})
