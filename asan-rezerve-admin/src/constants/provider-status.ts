/**
 * The provider lifecycle states, mirroring the backend's ProviderStatus enum.
 *
 * The admin UI previously filtered and colour-coded on 'Pending' | 'Approved' | 'Rejected',
 * none of which the domain has ever emitted — so every status tab matched nothing.
 */
export const PROVIDER_STATUSES = [
  'Drafted',
  'PendingVerification',
  'Verified',
  'Active',
  'Inactive',
  'Suspended',
  'Archived',
] as const

export type ProviderStatus = (typeof PROVIDER_STATUSES)[number]

/** Tabs the admin queue exposes, in triage order. `undefined` is the "all providers" tab. */
export const PROVIDER_STATUS_TABS: readonly ProviderStatus[] = [
  'PendingVerification',
  'Active',
  'Drafted',
  'Inactive',
  'Suspended',
]

const STATUS_COLORS: Record<ProviderStatus, string> = {
  Drafted: 'default',
  PendingVerification: 'orange',
  Verified: 'blue',
  Active: 'green',
  Inactive: 'default',
  Suspended: 'volcano',
  Archived: 'red',
}

export const getStatusColor = (status?: string): string =>
  STATUS_COLORS[status as ProviderStatus] ?? 'default'

/** i18n key for a status, e.g. 'providerStatus.PendingVerification'. */
export const statusLabelKey = (status?: string): string =>
  `providerStatus.${PROVIDER_STATUSES.includes(status as ProviderStatus) ? status : 'unknown'}`
