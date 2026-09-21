import apiClient from '../utils/axios'

/**
 * The signed-in administrator's own notifications. Only the unread count is read here: nothing in the
 * notification catalogue is addressed to an administrator today, so an inbox page would be permanently empty.
 * The count is still read for real, so the header tells the truth — and starts working unchanged the day an
 * administrator becomes a recipient.
 */
export const notificationsApi = {
  async unreadCount(): Promise<number> {
    const response = await apiClient.get<{ unreadCount?: number }>('/Notifications/unread-count')
    return response.data?.unreadCount ?? 0
  },
}
