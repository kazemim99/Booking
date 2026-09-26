import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { effectScope } from 'vue'

const api = vi.hoisted(() => ({
  search: vi.fn(),
  get: vi.fn(),
  trace: vi.fn(),
  exportLogs: vi.fn(),
}))
vi.mock('../../../api/observability.api', () => ({ observabilityApi: api }))

import { useLogExplorer } from '../useLogExplorer'

const NOW = Date.parse('2026-09-26T12:00:00.000Z')
const page = (n: number) => ({ items: Array.from({ length: n }, (_, i) => ({ id: i + 1, message: `m${i}` })), totalCount: n, page: 1, pageSize: 50 })

describe('useLogExplorer', () => {
  beforeEach(() => {
    Object.values(api).forEach((f) => f.mockReset())
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('searches the chosen window with the chosen filters', async () => {
    api.search.mockResolvedValue(page(2))
    const logs = useLogExplorer(50, () => NOW)
    logs.range.value = '24h'
    logs.minLevel.value = 'Warning'
    logs.search.value = 'booking'

    await logs.applyFilters()

    expect(api.search).toHaveBeenCalledWith(expect.objectContaining({
      from: '2026-09-25T12:00:00.000Z',
      minLevel: 'Warning',
      search: 'booking',
      page: 1,
      pageSize: 50,
    }))
    expect(logs.items.value).toHaveLength(2)
    expect(logs.totalCount.value).toBe(2)
    expect(logs.loaded.value).toBe(true)
  })

  it('shows a failure as an error, not as an empty log', async () => {
    api.search.mockRejectedValue({ response: { data: { message: 'خطای سرور' } } })
    const logs = useLogExplorer(50, () => NOW)

    await logs.load()

    expect(logs.error.value).toBe('خطای سرور')
    expect(logs.loaded.value).toBe(false)
  })

  it('opens one event and a whole request', async () => {
    api.get.mockResolvedValue({ id: 7, message: 'x' })
    api.trace.mockResolvedValue([{ id: 1 }, { id: 2 }])
    const logs = useLogExplorer(50, () => NOW)

    await logs.open(7)
    await logs.openTrace('abc')

    expect(logs.detail.value?.id).toBe(7)
    expect(logs.traceOpen.value).toBe('abc')
    expect(logs.traceEvents.value).toHaveLength(2)
    logs.closeTrace()
    expect(logs.traceEvents.value).toEqual([])
  })

  it('pages through results', async () => {
    api.search.mockResolvedValue(page(0))
    const logs = useLogExplorer(50, () => NOW)

    await logs.goTo(3)

    expect(api.search).toHaveBeenCalledWith(expect.objectContaining({ page: 3 }))
  })

  it('refreshes the first page while auto-refresh is on, and stops when switched off', async () => {
    vi.useFakeTimers()
    api.search.mockResolvedValue(page(1))
    const scope = effectScope()
    const logs = scope.run(() => useLogExplorer(50, () => NOW))!

    logs.setAutoRefresh(true, 1000)
    await vi.advanceTimersByTimeAsync(3000)
    expect(api.search).toHaveBeenCalledTimes(3)

    logs.setAutoRefresh(false)
    await vi.advanceTimersByTimeAsync(3000)
    expect(api.search).toHaveBeenCalledTimes(3)
    scope.stop()
  })

  it('stops refreshing when the page goes away', async () => {
    vi.useFakeTimers()
    api.search.mockResolvedValue(page(1))
    const scope = effectScope()
    const logs = scope.run(() => useLogExplorer(50, () => NOW))!
    logs.setAutoRefresh(true, 1000)

    scope.stop()
    await vi.advanceTimersByTimeAsync(3000)

    expect(api.search).not.toHaveBeenCalled()
  })

  it('exports the same filters without paging', async () => {
    api.exportLogs.mockResolvedValue(new Blob(['{}']))
    const logs = useLogExplorer(50, () => NOW)
    logs.minLevel.value = 'Error'

    const blob = await logs.exportLogs()

    expect(blob).toBeInstanceOf(Blob)
    expect(api.exportLogs).toHaveBeenCalledWith(expect.objectContaining({ minLevel: 'Error', page: undefined, pageSize: undefined }))
  })
})
