import type { RouteLocationRaw } from 'vue-router'
import type { InboxItem } from '@/core/api/services/notification-inbox.service'

export type InboxAudience = 'customer' | 'provider'

/**
 * The screen a notification opens, or `null` for "mark it read and stay here".
 *
 * Only destinations this client has a verified screen for are mapped. Anything else — a payout, a salon
 * profile, an invitation — deliberately goes nowhere: sending someone to a plausible-looking wrong screen is
 * worse than leaving them on the list. Add a kind here when its screen exists, not before.
 */
export function destinationRoute(
  item: Pick<InboxItem, 'destinationKind' | 'destinationId' | 'isActionable'>,
  audience: InboxAudience,
): RouteLocationRaw | null {
  // The backend says the target is gone or no longer this person's. The row still shows; it does not navigate.
  if (!item.isActionable || !item.destinationId) return null

  switch (item.destinationKind?.toLowerCase()) {
    case 'booking':
      return audience === 'customer'
        ? { name: 'BookingDetail', params: { id: item.destinationId } }
        : { name: 'BookingDetails', params: { id: item.destinationId } }
    default:
      return null
  }
}
