/**
 * Deterministic-but-unique test identities.
 *
 * Mirrors tests/e2e/keystone-booking-flow.sh: a unique per-run 7-digit suffix
 * yields valid 10-digit Iranian mobiles (912… for providers, 913… for customers),
 * so repeated runs never collide on phone numbers.
 */

/** The OTP code the host accepts when OTP_SANDBOX_CODE is set. Overridable via env. */
export const OTP_CODE = process.env.E2E_OTP_CODE ?? '123456'

function suffix(): string {
  // time + random → 7 digits
  const n = (Date.now() % 1_000_000) * 10 + Math.floor(Math.random() * 10)
  return String(n % 10_000_000).padStart(7, '0')
}

export interface TestIdentity {
  /** 10-digit national mobile, e.g. 9121234567 */
  phone: string
  firstName: string
  lastName: string
}

export function newProviderIdentity(): TestIdentity {
  return { phone: `912${suffix()}`, firstName: 'E2E', lastName: 'Provider' }
}

/**
 * Customer logs in through the UI, whose LoginView validates /^09\d{9}$/ (11 digits,
 * leading 0). The composable normalizes it to national +98 before calling the API.
 */
export function newCustomerIdentity(): TestIdentity {
  return { phone: `0913${suffix()}`, firstName: 'E2E', lastName: 'Customer' }
}

/** A unique business name per run. */
export function newBusinessName(): string {
  return `E2E Salon ${suffix()}`
}
