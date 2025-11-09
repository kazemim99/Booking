/**
 * User Status Enumeration
 * Re-export from main enums file for modular access
 */

export { UserStatus } from '../enums.types'

// Additional user status specific helpers can be added here
export const USER_STATUS_LABELS: Record<string, string> = {
  active: 'A9'D',
  inactive: ':л1A9'D',
  pending: '/1 'F*8'1 *'лл/',
  suspended: 'E9DB 4/G',
  banned: 'E3/H/ 4/G',
  deleted: '-0A 4/G',
}

export const USER_STATUS_COLORS: Record<string, string> = {
  active: 'green',
  inactive: 'gray',
  pending: 'yellow',
  suspended: 'orange',
  banned: 'red',
  deleted: 'red',
}
