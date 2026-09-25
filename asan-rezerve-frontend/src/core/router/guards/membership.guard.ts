import { NavigationGuardNext, RouteLocationNormalized } from 'vue-router'
import { useMembershipStore } from '@/modules/provider/stores/membership.store'

/**
 * Route guards keyed on MEMBERSHIP, replacing hierarchy.guard.
 *
 * The old guards asked what the signed-in user's own Provider *was*: an "Organization"
 * could manage staff, an "Individual with a parentOrganizationId" was a staff member, and
 * an "Individual without one" was independent. None of those exist any more. The questions
 * they were really asking are membership questions:
 *
 *   - can this person manage a salon?  -> they hold an Owner membership
 *   - is this person somebody's staff? -> they hold a non-owner membership
 *
 * Both can be true at once — someone can own one salon and work shifts at another — which
 * the old model could not express at all.
 */

async function ensureLoaded() {
  const store = useMembershipStore()
  if (store.myMemberships.length === 0) {
    try {
      await store.loadMyMemberships()
    } catch (error) {
      console.error('[membership.guard] Failed to load memberships:', error)
    }
  }
  return store
}

/** Routes that manage a salon: owners only. */
export const ownerOnlyGuard = async (
  to: RouteLocationNormalized,
  _from: RouteLocationNormalized,
  next: NavigationGuardNext,
) => {
  const store = await ensureLoaded()

  if (!store.isOwner) {
    console.warn(`[ownerOnlyGuard] Access denied to ${to.path} — no owner membership`)
    return next({
      name: 'Forbidden',
      params: { message: 'این صفحه فقط برای مالکان سالن در دسترس است' },
      replace: true,
    })
  }

  next()
}

/** The staff workspace: for people who work at a salon they do not own. */
export const staffMemberOnlyGuard = async (
  to: RouteLocationNormalized,
  _from: RouteLocationNormalized,
  next: NavigationGuardNext,
) => {
  const store = await ensureLoaded()

  if (!store.isStaffMember) {
    console.warn(`[staffMemberOnlyGuard] Access denied to ${to.path} — no staff membership`)
    return next({
      name: 'Forbidden',
      params: { message: 'این صفحه فقط برای کارکنان سالن در دسترس است' },
      replace: true,
    })
  }

  next()
}

// Synchronous helpers for template/computed use. They read whatever is already loaded and
// do not fetch; call loadMyMemberships() first if you need them to be authoritative.
export const isOwner = (): boolean => useMembershipStore().isOwner
export const isStaffMember = (): boolean => useMembershipStore().isStaffMember
