import { describe, it, expect, vi, beforeEach } from 'vitest'

const api = vi.hoisted(() => ({
  logLevels: vi.fn(),
  setLogLevel: vi.fn(),
  resetLogLevel: vi.fn(),
}))
vi.mock('../../../api/observability.api', () => ({ observabilityApi: api }))

import { useLogLevels } from '../useLogLevels'

const row = (category: string, effectiveLevel = 'Information', override: unknown = null) =>
  ({ category, configuredLevel: null, effectiveLevel, override })

describe('useLogLevels', () => {
  beforeEach(() => {
    Object.values(api).forEach((f) => f.mockReset())
  })

  it('loads the categories', async () => {
    api.logLevels.mockResolvedValue([row('Default'), row('AsanRezerve')])
    const levels = useLogLevels()

    await levels.load()

    expect(levels.rows.value.map((r) => r.category)).toEqual(['Default', 'AsanRezerve'])
  })

  it('replaces the row with what the server put in force', async () => {
    api.logLevels.mockResolvedValue([row('AsanRezerve')])
    api.setLogLevel.mockResolvedValue(row('AsanRezerve', 'Debug', { level: 'Debug', expiresAt: '2026-09-26T12:30:00Z', updatedBy: 'admin', updatedAt: '2026-09-26T12:00:00Z' }))
    const levels = useLogLevels()
    await levels.load()

    const ok = await levels.set('AsanRezerve', 'Debug', 30)

    expect(ok).toBe(true)
    expect(api.setLogLevel).toHaveBeenCalledWith('AsanRezerve', 'Debug', 30)
    expect(levels.rows.value).toHaveLength(1)
    expect(levels.rows.value[0]!.effectiveLevel).toBe('Debug')
  })

  it('adds a category the list did not have', async () => {
    api.setLogLevel.mockResolvedValue(row('AsanRezerve.ServiceCatalog.Application.Commands', 'Debug'))
    const levels = useLogLevels()

    await levels.set(' AsanRezerve.ServiceCatalog.Application.Commands ', 'Debug', null)

    expect(api.setLogLevel).toHaveBeenCalledWith('AsanRezerve.ServiceCatalog.Application.Commands', 'Debug', null)
    expect(levels.rows.value).toHaveLength(1)
  })

  it('refuses a category the server would refuse, without calling it', async () => {
    const levels = useLogLevels()

    expect(await levels.set('Bad Category', 'Debug', 30)).toBe(false)
    expect(levels.error.value).not.toBe('')
    expect(api.setLogLevel).not.toHaveBeenCalled()
    expect(levels.isValidCategory('AsanRezerve.X_1')).toBe(true)
    expect(levels.isValidCategory('a;b')).toBe(false)
  })

  it('keeps the row and shows the error when the server refuses', async () => {
    api.logLevels.mockResolvedValue([row('AsanRezerve')])
    api.setLogLevel.mockRejectedValue({ response: { data: { message: 'level must be …' } } })
    const levels = useLogLevels()
    await levels.load()

    expect(await levels.set('AsanRezerve', 'Debug', 30)).toBe(false)

    expect(levels.error.value).toBe('level must be …')
    expect(levels.rows.value[0]!.effectiveLevel).toBe('Information')
  })

  it('resets and re-reads', async () => {
    api.logLevels.mockResolvedValue([row('AsanRezerve')])
    api.resetLogLevel.mockResolvedValue(undefined)
    const levels = useLogLevels()

    await levels.reset('AsanRezerve')

    expect(api.resetLogLevel).toHaveBeenCalledWith('AsanRezerve')
    expect(api.logLevels).toHaveBeenCalledTimes(1)
  })
})
