import { describe, it, expect } from 'vitest'
import fa from '../../locales/fa.json'
import en from '../../locales/en.json'
import {
  PROVIDER_STATUSES,
  PROVIDER_STATUS_TABS,
  getStatusColor,
  statusLabelKey,
} from '../provider-status'

/**
 * The admin's status vocabulary was 'Pending' | 'Approved' | 'Rejected' | 'Suspended'.
 * The domain has only ever emitted Drafted, PendingVerification, Verified, Active,
 * Inactive, Suspended and Archived — so three of the four tabs could never match a
 * provider, and real statuses had no colour or label at all.
 */
describe('provider statuses', () => {
  it('matches the backend ProviderStatus enum exactly', () => {
    expect([...PROVIDER_STATUSES]).toEqual([
      'Drafted',
      'PendingVerification',
      'Verified',
      'Active',
      'Inactive',
      'Suspended',
      'Archived',
    ])
  })

  it('contains none of the statuses the backend never emits', () => {
    for (const invented of ['Pending', 'Approved', 'Rejected']) {
      expect(PROVIDER_STATUSES).not.toContain(invented)
    }
  })

  it('only offers tabs for real statuses', () => {
    for (const tab of PROVIDER_STATUS_TABS) {
      expect(PROVIDER_STATUSES).toContain(tab)
    }
  })

  it('surfaces the verification queue as the first tab', () => {
    expect(PROVIDER_STATUS_TABS[0]).toBe('PendingVerification')
  })

  it('colours the states an admin must act on or notice distinctly', () => {
    // Drafted and Inactive are deliberately neutral — they need no attention.
    const attention = ['PendingVerification', 'Verified', 'Active', 'Suspended', 'Archived'] as const
    const colors = attention.map(getStatusColor)

    expect(colors).not.toContain('default')
    expect(new Set(colors).size).toBe(attention.length)
  })

  it('falls back to a neutral colour for an unrecognised status', () => {
    expect(getStatusColor('Whatever')).toBe('default')
    expect(getStatusColor(undefined)).toBe('default')
  })
})

describe('status labels', () => {
  it('maps each status to its translation key', () => {
    expect(statusLabelKey('PendingVerification')).toBe('providerStatus.PendingVerification')
  })

  it('routes an unrecognised status to the unknown label rather than echoing it', () => {
    // The table used to render the raw value, which is how an admin reading Persian
    // was shown the literal string "PendingVerification".
    expect(statusLabelKey('Pending')).toBe('providerStatus.unknown')
    expect(statusLabelKey(undefined)).toBe('providerStatus.unknown')
  })

  it.each(['fa', 'en'])('translates every status in %s', locale => {
    const messages = (locale === 'fa' ? fa : en) as { providerStatus: Record<string, string> }

    for (const status of [...PROVIDER_STATUSES, 'unknown']) {
      expect(messages.providerStatus[status], `${locale} is missing ${status}`).toBeTruthy()
    }
  })

  it('translates Persian statuses into Persian, not English passthrough', () => {
    const messages = fa as unknown as { providerStatus: Record<string, string> }

    for (const status of PROVIDER_STATUSES) {
      expect(messages.providerStatus[status]).not.toBe(status)
      expect(messages.providerStatus[status]).toMatch(/[؀-ۿ]/)
    }
  })
})
