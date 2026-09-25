import { describe, it, expect, vi, beforeEach } from 'vitest'

const client = vi.hoisted(() => ({ get: vi.fn(), post: vi.fn() }))
vi.mock('@/core/api/client/http-client', () => ({ serviceCategoryClient: client }))

import { promotionService } from '../promotion.service'

describe('promotionService', () => {
  beforeEach(() => {
    client.get.mockReset()
    client.post.mockReset()
  })

  it('reads a salon offers from the envelope', async () => {
    client.get.mockResolvedValue({ success: true, data: [{ id: 'o1' }] })

    const offers = await promotionService.getOffers('p1')

    expect(client.get).toHaveBeenCalledWith('/v1/providers/p1/offers')
    expect(offers).toEqual([{ id: 'o1' }])
  })

  it('a missing list is an empty one', async () => {
    client.get.mockResolvedValue({ success: true, data: null })

    expect(await promotionService.getOffers('p1')).toEqual([])
  })

  it('asks for a quote with a trimmed code, or none', async () => {
    client.post.mockResolvedValue({ success: true, data: { subtotal: 100, discount: 20, total: 80, codeOutcome: 'Applied' } })

    const quote = await promotionService.quote({
      providerId: 'p1', serviceIds: ['s1'], startTime: '2026-10-03T11:00:00', promotionCode: '  spring ',
    })
    await promotionService.quote({ providerId: 'p1', serviceIds: ['s1'], startTime: '2026-10-03T11:00:00', promotionCode: '  ' })

    expect(client.post).toHaveBeenNthCalledWith(1, '/v1/Bookings/quote', {
      providerId: 'p1', serviceIds: ['s1'], startTime: '2026-10-03T11:00:00', promotionCode: 'spring',
    })
    expect(client.post.mock.calls[1][1].promotionCode).toBeNull()
    expect(quote.total).toBe(80)
  })
})
