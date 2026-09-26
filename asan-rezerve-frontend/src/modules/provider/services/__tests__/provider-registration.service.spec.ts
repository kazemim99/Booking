import { describe, it, expect, vi, beforeEach } from 'vitest'

const client = vi.hoisted(() => ({ get: vi.fn(), post: vi.fn() }))
vi.mock('@/core/api/client/http-client', () => ({ serviceCategoryClient: client }))

import { providerRegistrationService, type CreateProviderDraftRequest } from '../provider-registration.service'
import { parseCategory } from '@/core/constants/provider-categories'
import { ProviderCategory } from '@/core/types/enums.types'

/**
 * The wizard's category reaches the backend as the step emitted it, and comes back as the backend sent it.
 *
 * Regression (Playwright keystone, 2026-09-26): the service translated categories to the pre-enum ProviderType
 * names through a table keyed by the old `hair_salon` ids. The step emits `hair-salon`, which missed the table and
 * fell back to `'Salon'` — rejected by the backend, so no salon could register on the web.
 */
describe('providerRegistrationService category', () => {
  const draft = (category: string): CreateProviderDraftRequest => ({
    businessName: 'E2E Salon',
    businessDescription: '',
    category,
    phoneNumber: '09121234567',
    email: '',
    ownerFirstName: 'E2E',
    ownerLastName: 'Owner',
    addressLine1: 'Street 1',
    city: 'Tehran',
    province: 'Tehran',
    postalCode: '',
    latitude: 35.7,
    longitude: 51.4,
  })

  beforeEach(() => {
    client.get.mockReset()
    client.post.mockReset()
    client.post.mockResolvedValue({ success: true, data: { providerId: 'p1' } })
  })

  it.each(['hair-salon', 'barbershop', 'HairSalon', '2'])('creates a draft with category %s as given', async (category) => {
    await providerRegistrationService.createProviderDraft(draft(category))

    expect(client.post).toHaveBeenCalledWith('v1/providers/draft', expect.objectContaining({ category }))
  })

  it('saves step 3 with the category as given', async () => {
    await providerRegistrationService.saveStep3Location(draft('barbershop'))

    expect(client.post).toHaveBeenCalledWith('v1/Registration/step-3/location', expect.objectContaining({ category: 'barbershop' }))
  })

  it('returns a saved draft category as the backend sent it', async () => {
    client.get.mockResolvedValue({
      success: true,
      data: { providerId: 'p1', registrationStep: 3, hasDraft: true, draftData: { category: 'Barbershop' } },
    })

    const response = await providerRegistrationService.getDraftProvider()

    expect(response.draftData!.category).toBe('Barbershop')
  })

  it('resumes a barbershop on the barbershop card, not the women\'s salon', async () => {
    client.get.mockResolvedValue({
      success: true,
      data: {
        hasDraft: true,
        currentStep: 3,
        draftData: { providerId: 'p1', registrationStep: 3, status: 'Drafted', businessInfo: { category: 'Barbershop' } },
      },
    })

    const response = await providerRegistrationService.getRegistrationProgress()

    expect(response.draftData!.businessInfo.category).toBe('Barbershop')
    expect(parseCategory(response.draftData!.businessInfo.category)).toBe(ProviderCategory.Barbershop)
  })
})
