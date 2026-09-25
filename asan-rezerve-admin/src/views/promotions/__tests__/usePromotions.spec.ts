import { describe, it, expect, vi, beforeEach } from 'vitest'

const api = vi.hoisted(() => ({
  search: vi.fn(),
  details: vi.fn(),
  createCampaign: vi.fn(),
  updateCampaign: vi.fn(),
  changeStatus: vi.fn(),
}))
vi.mock('../../../api/promotions.api', () => ({ promotionsApi: api }))

import { usePromotions } from '../usePromotions'

const row = (id: string, extra: object = {}) => ({ id, title: id, status: 'Active', state: 'Active', providerName: 'سالن', ...extra })

describe('usePromotions', () => {
  beforeEach(() => Object.values(api).forEach((f) => f.mockReset()))

  it('loads platform campaigns first', async () => {
    api.search.mockResolvedValue({ items: [row('a')], totalCount: 1 })
    const p = usePromotions()

    await p.load()

    expect(api.search).toHaveBeenCalledWith(expect.objectContaining({ owner: 'Platform', page: 1 }))
    expect(p.items.value).toHaveLength(1)
    expect(p.loaded.value).toBe(true)
  })

  it('a failed load is an error, never an empty list', async () => {
    api.search.mockRejectedValue({ response: { data: { message: 'خطای سرور' } } })
    const p = usePromotions()

    await p.load()

    expect(p.loaded.value).toBe(false)
    expect(p.error.value).toBe('خطای سرور')
  })

  it('switching owner resets to the first page', async () => {
    api.search.mockResolvedValue({ items: [], totalCount: 0 })
    const p = usePromotions()
    p.page.value = 3

    await p.setOwner('Provider')

    expect(api.search).toHaveBeenLastCalledWith(expect.objectContaining({ owner: 'Provider', page: 1 }))
  })

  it('a lifecycle action updates the row from the server and keeps the salon name', async () => {
    api.search.mockResolvedValue({ items: [row('a')], totalCount: 1 })
    api.changeStatus.mockResolvedValue(row('a', { status: 'Paused', state: 'Paused', providerName: undefined }))
    const p = usePromotions()
    await p.load()

    const ok = await p.act(p.items.value[0] as never, 'pause')

    expect(ok).toBe(true)
    expect(p.items.value[0]).toMatchObject({ state: 'Paused', providerName: 'سالن' })
  })

  it('a refused action leaves the row as it was and says why', async () => {
    api.search.mockResolvedValue({ items: [row('a')], totalCount: 1 })
    api.changeStatus.mockRejectedValue({ response: { data: { message: 'این تخفیف پایان یافته است.' } } })
    const p = usePromotions()
    await p.load()

    expect(await p.act(p.items.value[0] as never, 'end')).toBe(false)
    expect(p.items.value[0]?.state).toBe('Active')
    expect(p.error.value).toBe('این تخفیف پایان یافته است.')
  })

  it('creating a campaign reloads the platform tab; a failure returns the server message', async () => {
    api.search.mockResolvedValue({ items: [], totalCount: 0 })
    api.createCampaign.mockResolvedValueOnce({ id: 'n' })
    const p = usePromotions()
    p.owner.value = 'Provider'

    expect(await p.save({} as never)).toEqual({ ok: true })
    expect(p.owner.value).toBe('Platform')
    expect(api.search).toHaveBeenCalled()

    api.createCampaign.mockRejectedValueOnce({ response: { data: { message: 'کد تکراری است' } } })
    expect(await p.save({} as never)).toEqual({ ok: false, message: 'کد تکراری است' })
  })
})
