/**
 * The domain for admin accounts that sign in with a plain username.
 *
 * The backend authenticates by email only. The admin asked to sign in as "kazemi.mst", so an
 * identifier without "@" is taken to be a username and becomes `<username>@nahalkmi.ir` — the
 * account's actual email. A full email is sent as typed. (Decision recorded 2026-09-19.)
 */
export const ADMIN_EMAIL_DOMAIN = 'nahalkmi.ir'

export function toLoginEmail(identifier: string): string {
  const value = identifier.trim().toLowerCase()
  if (value === '' || value.includes('@')) return value
  return `${value}@${ADMIN_EMAIL_DOMAIN}`
}
