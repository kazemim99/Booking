import { describe, it, expect, vi, beforeEach } from 'vitest'

/**
 * The client for the ONE notification-preferences store the backend consults.
 *
 * Until now no client called `/notifications/preferences` at all: the customer screen saved to UserManagement
 * fields nothing reads, and the provider screen saved to a route that does not exist. This service is the
 * seam every preference screen goes through from here on.
 *
 * The rule these tests hold above all: a save changes ONLY the channels the screen shows, and carries every
 * other channel through unchanged. No screen has an in-app toggle, so a save that sent "just the channels I
 * know about" would switch in-app off by omission — the same shape as the backend default that switched push
 * off (see NotificationPreferenceDefaultsTests).
 */

const get = vi.fn()
const put = vi.fn()

vi.mock('@/core/api/client/http-client', () => ({
  serviceCategoryClient: {
    get: (...args: unknown[]) => get(...args),
    put: (...args: unknown[]) => put(...args),
  },
}))

import {
  Channel,
  applyToggles,
  channelMask,
  isEnabled,
  notificationPreferencesService,
} from '../notification-preferences.service'

describe('channelMask', () => {
  it('reads the channel names the backend renders into bits', () => {
    expect(channelMask(['Email', 'SMS', 'PushNotification', 'InApp'])).toBe(
      Channel.Email | Channel.SMS | Channel.PushNotification | Channel.InApp,
    )
  })

  it('tolerates the camel-cased names the response transform can produce', () => {
    expect(channelMask(['email', 'sms', 'pushNotification', 'inApp'])).toBe(
      Channel.Email | Channel.SMS | Channel.PushNotification | Channel.InApp,
    )
  })

  it('ignores a name it does not recognise rather than guessing at it', () => {
    expect(channelMask(['SMS', 'CarrierPigeon'])).toBe(Channel.SMS)
  })
})

describe('applyToggles', () => {
  const everything = Channel.Email | Channel.SMS | Channel.PushNotification | Channel.InApp

  it('switches off only the channel that was toggled', () => {
    expect(applyToggles(everything, { push: false })).toBe(
      Channel.Email | Channel.SMS | Channel.InApp,
    )
  })

  it('never touches in-app, which no screen offers', () => {
    // The trap. A screen that knows about push, SMS and email must not switch in-app off by leaving it out.
    const next = applyToggles(everything, { push: false, sms: false, email: false })
    expect(isEnabled(next, Channel.InApp)).toBe(true)
  })

  it('carries through channels the product has not built a screen for', () => {
    const withWhatsApp = everything | Channel.WhatsApp
    expect(isEnabled(applyToggles(withWhatsApp, { push: false }), Channel.WhatsApp)).toBe(true)
  })

  it('switches a channel back on', () => {
    expect(isEnabled(applyToggles(Channel.InApp, { push: true }), Channel.PushNotification)).toBe(true)
  })

  it('leaves the mask alone when nothing was toggled', () => {
    expect(applyToggles(everything, {})).toBe(everything)
  })
})

describe('notificationPreferencesService', () => {
  beforeEach(() => {
    get.mockReset()
    put.mockReset()
  })

  it('reads preferences past the GET cache', async () => {
    // The HTTP client caches GETs for five minutes. A preferences screen that reads through it shows the
    // value from before the person's last save — "I turned it off and it came back on".
    get.mockResolvedValue({ success: true, data: { enabledChannels: ['SMS', 'InApp'] } })

    await notificationPreferencesService.getChannelMask()

    expect(get).toHaveBeenCalledWith(
      '/v1/notifications/preferences',
      expect.objectContaining({ cache: false }),
    )
  })

  it('returns the channels the backend actually holds', async () => {
    get.mockResolvedValue({
      success: true,
      data: { enabledChannels: ['Email', 'SMS', 'PushNotification', 'InApp'] },
    })

    const mask = await notificationPreferencesService.getChannelMask()

    expect(isEnabled(mask, Channel.PushNotification)).toBe(true)
  })

  it('unwraps the response envelope wherever it lands', async () => {
    // Responses arrive wrapped in a { data } envelope by the API middleware, and the client unwraps it once;
    // this service must not care which of the two shapes it is handed.
    get.mockResolvedValue({ success: true, data: { data: { enabledChannels: ['InApp'] } } })

    expect(await notificationPreferencesService.getChannelMask()).toBe(Channel.InApp)
  })

  it('saves the whole mask, so channels the screen does not show survive', async () => {
    put.mockResolvedValue({ success: true, data: { enabledChannels: 'SMS, InApp' } })
    const mask = Channel.SMS | Channel.InApp

    await notificationPreferencesService.saveChannelMask(mask)

    expect(put).toHaveBeenCalledWith('/v1/notifications/preferences', { enabledChannels: mask })
  })

  it('reports a failed save as a failure, not as the value it tried to save', async () => {
    put.mockResolvedValue({ success: false, message: 'nope' })

    await expect(notificationPreferencesService.saveChannelMask(Channel.InApp)).rejects.toThrow()
  })
})
