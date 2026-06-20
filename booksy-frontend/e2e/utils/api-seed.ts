import { request as playwrightRequest, type APIRequestContext } from '@playwright/test'
import { OTP_CODE, newProviderIdentity, newBusinessName } from './identity'

/**
 * API seeding — creates a bookable provider + active staff directly through the
 * backend, mirroring tests/e2e/keystone-booking-flow.sh. This is the reliable way
 * to set up supply-side state for UI tests: the provider registration *wizard* and
 * the staff *invitation* flow are slow/awkward to drive through the browser, and the
 * UI invite flow produces a pending invitation rather than an active staff member.
 *
 * Requires the host running with OTP_SANDBOX_CODE + Sms:SandboxMode. The API base
 * defaults to the host's direct address; override with E2E_API_URL.
 */
const API_BASE = process.env.E2E_API_URL ?? 'http://localhost:5050'

export interface SeededProvider {
  providerId: string
  staffId: string
  serviceId: string
}

/** National phone (strip a leading 0): the UI uses 0913…, the API wants 913…. */
function national(phone: string): string {
  return phone.replace(/^0/, '')
}

async function authenticate(
  api: APIRequestContext,
  kind: 'provider' | 'customer',
  nationalPhone: string,
  firstName: string,
  lastName: string,
): Promise<{ token: string; userId: string }> {
  await api.post('/api/v1/Auth/send-verification-code', {
    data: { phoneNumber: nationalPhone, countryCode: '+98' },
  })
  const res = await api.post(`/api/v1/Auth/${kind}/complete-authentication`, {
    data: { phoneNumber: `+98${nationalPhone}`, code: OTP_CODE, firstName, lastName },
  })
  if (!res.ok()) throw new Error(`${kind} auth failed: ${res.status()} ${await res.text()}`)
  // Responses are wrapped in an ApiResponse envelope: { success, data: {...} }.
  const data = unwrap(await res.json())
  return { token: data.accessToken, userId: data.userId }
}

/** Unwraps the { success, data } ApiResponse envelope (tolerates already-flat bodies). */
function unwrap(json: any): any {
  return json && typeof json === 'object' && 'data' in json ? json.data : json
}

/** Seeds an active, bookable provider with one staff member and one service. */
export async function seedBookableProvider(): Promise<SeededProvider> {
  // Generous timeout: a cold-start register-full (first request after host boot)
  // JITs a lot of EF/validation and can exceed the default action timeout.
  const api = await playwrightRequest.newContext({ baseURL: API_BASE, timeout: 90_000 })
  try {
    const p = newProviderIdentity()
    const businessName = newBusinessName()
    const { token, userId } = await authenticate(api, 'provider', p.phone, p.firstName, p.lastName)

    const hours: Record<string, unknown> = {}
    for (let d = 0; d < 7; d++) {
      hours[d] = {
        dayOfWeek: d,
        isOpen: true,
        openTime: { hours: 9, minutes: 0 },
        closeTime: { hours: 18, minutes: 0 },
        breaks: [],
      }
    }

    const regBody = {
      ownerId: userId,
      categoryId: 'HairSalon',
      businessInfo: {
        businessName,
        ownerFirstName: 'E2E',
        ownerLastName: 'Owner',
        phoneNumber: p.phone,
      },
      address: {
        street: 'St', city: 'Tehran', state: 'Tehran', postalCode: '1234567890',
        country: 'Iran', latitude: 35.7, longitude: 51.4,
      },
      location: { latitude: 35.7, longitude: 51.4, formattedAddress: 'Tehran' },
      businessHours: hours,
      services: [
        { name: 'Haircut', durationHours: 0, durationMinutes: 45, price: 250000, priceType: 'fixed' },
      ],
      assistanceOptions: [],
      teamMembers: [],
      ownerFirstName: 'E2E', ownerLastName: 'Owner', businessName,
      description: 't', primaryCategory: 'HairSalon', email: `e2e${p.phone}@s.com`,
      phoneNumber: p.phone, street: 'St', city: 'Tehran', state: 'Tehran',
      postalCode: '1234567890', country: 'Iran',
    }

    const headers = { Authorization: `Bearer ${token}` }
    const reg = await api.post('/api/v1/Providers/register-full', { data: regBody, headers })
    if (!reg.ok()) throw new Error(`register-full failed: ${reg.status()} ${await reg.text()}`)
    const regData = unwrap(await reg.json())
    const providerId: string = regData.providerId

    const staffRes = await api.post(`/api/v1/Providers/${providerId}/staff`, {
      data: { firstName: 'Sara', lastName: 'Stylist', role: 'Stylist' },
      headers,
    })
    if (!staffRes.ok()) throw new Error(`add-staff failed: ${staffRes.status()} ${await staffRes.text()}`)
    const staffId: string = unwrap(await staffRes.json()).id

    // Capture the service id created by register-full (needed to seed a booking).
    // Prefer the register response; fall back to GET /Services/provider (paginated).
    let serviceId: string =
      regData.serviceId ?? (Array.isArray(regData.services) ? regData.services[0]?.id : '') ?? ''
    if (!serviceId) {
      const svcRes = await api.get(`/api/v1/Services/provider/${providerId}`, { headers })
      if (svcRes.ok()) {
        const data = unwrap(await svcRes.json())
        const list = Array.isArray(data) ? data : (data?.items ?? [])
        serviceId = list.length ? list[0].id : ''
      }
    }
    if (!serviceId) throw new Error('could not resolve a serviceId for the seeded provider')

    return { providerId, staffId, serviceId }
  } finally {
    await api.dispose()
  }
}

/**
 * Seeds a booking for `customer` against the seeded provider, so the customer's
 * My Bookings has a real (backend) row to display and cancel through the UI.
 * `customerNationalPhone` is the API form (913…, no leading 0).
 */
export async function seedCustomerBooking(
  customerNationalPhone: string,
  firstName: string,
  lastName: string,
  seeded: SeededProvider,
): Promise<string> {
  const api = await playwrightRequest.newContext({ baseURL: API_BASE, timeout: 90_000 })
  try {
    const { token } = await authenticate(api, 'customer', customerNationalPhone, firstName, lastName)
    const res = await api.post('/api/v1/Bookings', {
      headers: { Authorization: `Bearer ${token}` },
      data: {
        providerId: seeded.providerId,
        serviceId: seeded.serviceId,
        staffProviderId: seeded.staffId,
        startTime: '2026-09-01T10:00:00Z',
        customerNotes: 'e2e ui keystone',
      },
    })
    if (!res.ok()) throw new Error(`create-booking failed: ${res.status()} ${await res.text()}`)
    return unwrap(await res.json()).id
  } finally {
    await api.dispose()
  }
}

/**
 * Seeds a booking for the user identified by `token` (captured from the browser
 * after UI login) — guarantees the booking belongs to exactly the logged-in
 * customer, sidestepping any phone-normalization identity mismatch.
 */
export async function seedBookingWithToken(token: string, seeded: SeededProvider): Promise<string> {
  const api = await playwrightRequest.newContext({ baseURL: API_BASE, timeout: 90_000 })
  try {
    const res = await api.post('/api/v1/Bookings', {
      headers: { Authorization: `Bearer ${token}` },
      data: {
        providerId: seeded.providerId,
        serviceId: seeded.serviceId,
        staffProviderId: seeded.staffId,
        startTime: '2026-09-01T10:00:00Z',
        customerNotes: 'e2e ui keystone',
      },
    })
    if (!res.ok()) throw new Error(`create-booking failed: ${res.status()} ${await res.text()}`)
    return unwrap(await res.json()).id
  } finally {
    await api.dispose()
  }
}

export { national }
