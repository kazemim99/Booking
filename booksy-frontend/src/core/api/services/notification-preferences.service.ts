import type { AxiosRequestConfig } from 'axios'
import { serviceCategoryClient } from '@/core/api/client/http-client'

/**
 * The one notification-preferences store the backend actually consults when deciding whether to send.
 *
 * Before this existed, no client called `/notifications/preferences` at all. The customer settings screen saved
 * to UserManagement fields that nothing reads, and the provider settings screen saved to a route that does not
 * exist. Every preference screen should go through here, because this is the only place a preference can
 * change what a person receives.
 */

/** Mirrors the backend's `NotificationChannel` flags enum — the values must stay identical. */
export const Channel = {
  None: 0,
  Email: 1,
  SMS: 2,
  PushNotification: 4,
  InApp: 8,
  WhatsApp: 16,
  Telegram: 32,
  Slack: 64,
  Phone: 128,
} as const

export type ChannelBit = (typeof Channel)[keyof typeof Channel]

/** The channels a screen may offer a toggle for. Everything else is carried through untouched on save. */
export interface ShownChannels {
  email?: boolean
  sms?: boolean
  push?: boolean
}

const TOGGLE_BITS: Record<keyof ShownChannels, ChannelBit> = {
  email: Channel.Email,
  sms: Channel.SMS,
  push: Channel.PushNotification,
}

/** Name → bit, case-insensitive, because the response transform may or may not have camel-cased the values. */
const BY_NAME: Record<string, ChannelBit> = Object.fromEntries(
  Object.entries(Channel)
    .filter(([name]) => name !== 'None')
    .map(([name, bit]) => [name.toLowerCase(), bit]),
)

/**
 * Turns the backend's rendered channel list into a mask. An unrecognised name is ignored rather than guessed
 * at: a channel this client does not know about cannot be shown, and dropping it on READ is harmless because
 * saves never rebuild the mask from names — they edit the mask they were given.
 */
export function channelMask(names: readonly string[]): number {
  return names.reduce((mask, name) => mask | (BY_NAME[name.trim().toLowerCase()] ?? 0), 0)
}

export function isEnabled(mask: number, bit: ChannelBit): boolean {
  return (mask & bit) === bit
}

/**
 * Applies a screen's toggles to the mask it loaded, changing ONLY the channels the screen shows.
 *
 * This is the rule that matters. No screen offers an in-app toggle, so a save that sent "the channels I know
 * about" would switch in-app off by omission — the same shape as the backend default that silently switched
 * push off for anyone who saved a setting.
 */
export function applyToggles(current: number, shown: ShownChannels): number {
  let next = current

  for (const [key, bit] of Object.entries(TOGGLE_BITS) as [keyof ShownChannels, ChannelBit][]) {
    const value = shown[key]
    if (value === undefined) continue
    next = value ? next | bit : next & ~bit
  }

  return next
}

interface PreferencesPayload {
  enabledChannels?: string[] | string | number
}

/** The response is wrapped in a `{ data }` envelope by the API; the client unwraps it once. Accept either. */
function unwrap(body: unknown): PreferencesPayload {
  const outer = (body ?? {}) as { data?: unknown }
  const inner = (outer.data ?? {}) as { data?: unknown; enabledChannels?: unknown }
  return (inner.enabledChannels !== undefined ? inner : (inner.data ?? {})) as PreferencesPayload
}

function toMask(value: PreferencesPayload['enabledChannels']): number {
  if (typeof value === 'number') return value
  if (typeof value === 'string') return channelMask(value.split(','))
  return channelMask(value ?? [])
}

const ENDPOINT = '/v1/notifications/preferences'

/**
 * `cache` is read by the client's request-cache interceptor but is not part of axios's own config type; the
 * interceptor's `withoutCache` helper casts the same way.
 */
const UNCACHED = { cache: false } as AxiosRequestConfig

export const notificationPreferencesService = {
  /**
   * Reads the channel mask. Bypasses the HTTP client's five-minute GET cache: a preferences screen read through
   * it shows the value from before the person's last save.
   */
  async getChannelMask(): Promise<number> {
    const response = await serviceCategoryClient.get<PreferencesPayload>(ENDPOINT, UNCACHED)
    return toMask(unwrap(response).enabledChannels)
  },

  /**
   * Saves a whole mask — build it with {@link applyToggles} so channels the screen does not show survive.
   * Throws on failure, so a caller cannot mistake the value it tried to save for the value that was kept.
   */
  async saveChannelMask(mask: number): Promise<number> {
    const response = await serviceCategoryClient.put<PreferencesPayload>(ENDPOINT, { enabledChannels: mask })

    if (!response.success) {
      throw new Error(response.message || 'ذخیرهٔ تنظیمات اعلان انجام نشد')
    }

    const saved = unwrap(response).enabledChannels
    return saved === undefined ? mask : toMask(saved)
  },
}
