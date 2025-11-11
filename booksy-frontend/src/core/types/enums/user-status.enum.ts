/**
 * User Status Enumeration
 * Re-export from main enums file for modular access
 */

export { UserStatus } from '../enums.types'

// Additional user status specific helpers can be added here
export const USER_STATUS_LABELS: Record<string, string> = {
  active: 'فعال',
  inactive: 'غیرفعال',
  pending: 'در انتظار تایید',
  suspended: 'معلق شده',
  banned: 'مسدود شده',
  deleted: 'حذف شده',
}

export const USER_STATUS_COLORS: Record<string, string> = {
  active: 'green',
  inactive: 'gray',
  pending: 'yellow',
  suspended: 'orange',
  banned: 'red',
  deleted: 'red',
}
