import { describe, it, expect, vi, beforeEach } from 'vitest'

const api = vi.hoisted(() => ({
  overview: vi.fn(),
  cache: vi.fn(),
  invalidateCache: vi.fn(),
  digestMarkdown: vi.fn(),
}))
vi.mock('../../../api/observability.api', () => ({ observabilityApi: api }))

import { useSystemOverview } from '../useSystemOverview'

describe('useSystemOverview', () => {
  beforeEach(() => {
    Object.values(api).forEach((f) => f.mockReset())
  })

  it('loads the overview', async () => {
    api.overview.mockResolvedValue({ environment: 'Production', cache: { regions: [] } })
    const system = useSystemOverview()

    await system.load()

    expect(system.overview.value?.environment).toBe('Production')
  })

  it('purges one tag and re-reads the cache figures', async () => {
    api.overview.mockResolvedValue({ environment: 'Production', cache: { regions: [] } })
    api.invalidateCache.mockResolvedValue(undefined)
    api.cache.mockResolvedValue({ regions: [{ region: 'GetProviderByIdQuery' }] })
    const system = useSystemOverview()
    await system.load()

    expect(await system.purge(' provider:1 ')).toBe(true)

    expect(api.invalidateCache).toHaveBeenCalledWith({ tag: 'provider:1' })
    expect(system.overview.value?.cache.regions).toHaveLength(1)
  })

  it('purges everything when no tag is given', async () => {
    api.invalidateCache.mockResolvedValue(undefined)
    const system = useSystemOverview()

    await system.purge(null)

    expect(api.invalidateCache).toHaveBeenCalledWith({ all: true })
  })

  it('prepares the AI digest', async () => {
    api.digestMarkdown.mockResolvedValue('# AsanRezerve system digest')
    const system = useSystemOverview()

    await system.loadDigest('AsanRezerve.ServiceCatalog')

    expect(api.digestMarkdown).toHaveBeenCalledWith({ source: 'AsanRezerve.ServiceCatalog' })
    expect(system.digest.value).toBe('# AsanRezerve system digest')
  })

  it('shows a failed purge as an error', async () => {
    api.invalidateCache.mockRejectedValue({ response: { data: { message: 'Give a tag, or all: true' } } })
    const system = useSystemOverview()

    expect(await system.purge('x')).toBe(false)
    expect(system.error.value).toBe('Give a tag, or all: true')
  })
})
