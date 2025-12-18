// src/modules/provider/stores/__tests__/hierarchy.store.spec.ts

import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useHierarchyStore } from '../hierarchy.store'
import { hierarchyService } from '../../services/hierarchy.service'
import type {
  ProviderHierarchyResponse,
  StaffMember,
  ProviderInvitation,
  ProviderJoinRequest,
} from '../../types/hierarchy.types'
import type { PagedResult } from '@/core/types/common.types'

// Mock the hierarchy service
vi.mock('../../services/hierarchy.service', () => ({
  hierarchyService: {
    getProviderHierarchy: vi.fn(),
    getStaffMembers: vi.fn(),
    sendInvitation: vi.fn(),
    acceptInvitation: vi.fn(),
    getPendingInvitations: vi.fn(),
    createJoinRequest: vi.fn(),
    approveJoinRequest: vi.fn(),
    rejectJoinRequest: vi.fn(),
    getJoinRequests: vi.fn(),
    removeStaffMember: vi.fn(),
    convertToOrganization: vi.fn(),
  },
}))

describe('useHierarchyStore', () => {
  let store: ReturnType<typeof useHierarchyStore>

  beforeEach(() => {
    setActivePinia(createPinia())
    store = useHierarchyStore()
    vi.clearAllMocks()
  })

  describe('Initial State', () => {
    it('should initialize with null hierarchy', () => {
      expect(store.currentHierarchy).toBeNull()
    })

    it('should initialize with null staff members', () => {
      expect(store.staffMembers).toBeNull()
    })

    it('should initialize with loading states as false', () => {
      expect(store.isLoading).toBe(false)
      expect(store.isLoadingStaff).toBe(false)
      expect(store.isLoadingInvitations).toBe(false)
      expect(store.isLoadingJoinRequests).toBe(false)
    })

    it('should initialize with null error', () => {
      expect(store.error).toBeNull()
    })
  })

  describe('Computed Getters', () => {
    describe('isOrganization', () => {
      it('should return true when hierarchy type is Organization', () => {
        store.currentHierarchy = {
          hierarchyType: 'Organization',
        } as ProviderHierarchyResponse
        expect(store.isOrganization).toBe(true)
      })

      it('should return false when hierarchy type is Individual', () => {
        store.currentHierarchy = {
          hierarchyType: 'Individual',
        } as ProviderHierarchyResponse
        expect(store.isOrganization).toBe(false)
      })
    })

    describe('isIndividual', () => {
      it('should return true when hierarchy type is Individual', () => {
        store.currentHierarchy = {
          hierarchyType: 'Individual',
        } as ProviderHierarchyResponse
        expect(store.isIndividual).toBe(true)
      })

      it('should return false when hierarchy type is Organization', () => {
        store.currentHierarchy = {
          hierarchyType: 'Organization',
        } as ProviderHierarchyResponse
        expect(store.isIndividual).toBe(false)
      })
    })

    describe('isIndependent', () => {
      it('should return true when provider is independent', () => {
        store.currentHierarchy = {
          isIndependent: true,
        } as ProviderHierarchyResponse
        expect(store.isIndependent).toBe(true)
      })

      it('should return false when provider is not independent', () => {
        store.currentHierarchy = {
          isIndependent: false,
        } as ProviderHierarchyResponse
        expect(store.isIndependent).toBe(false)
      })

      it('should return false when hierarchy is null', () => {
        expect(store.isIndependent).toBe(false)
      })
    })

    describe('canAddStaff', () => {
      it('should return true for Organization type', () => {
        store.currentHierarchy = {
          hierarchyType: 'Organization',
        } as ProviderHierarchyResponse
        expect(store.canAddStaff).toBe(true)
      })

      it('should return false for Individual type', () => {
        store.currentHierarchy = {
          hierarchyType: 'Individual',
        } as ProviderHierarchyResponse
        expect(store.canAddStaff).toBe(false)
      })
    })

    describe('hasStaff', () => {
      it('should return true when staff count is greater than 0', () => {
        store.currentHierarchy = {
          staffCount: 5,
        } as ProviderHierarchyResponse
        expect(store.hasStaff).toBe(true)
      })

      it('should return false when staff count is 0', () => {
        store.currentHierarchy = {
          staffCount: 0,
        } as ProviderHierarchyResponse
        expect(store.hasStaff).toBe(false)
      })
    })

    describe('staffCount', () => {
      it('should return staff count from hierarchy', () => {
        store.currentHierarchy = {
          staffCount: 10,
        } as ProviderHierarchyResponse
        expect(store.staffCount).toBe(10)
      })

      it('should return 0 when hierarchy is null', () => {
        expect(store.staffCount).toBe(0)
      })
    })

    describe('activeStaffCount', () => {
      it('should return count of active staff members', () => {
        store.staffMembers = {
          items: [
            { isActive: true } as StaffMember,
            { isActive: false } as StaffMember,
            { isActive: true } as StaffMember,
          ],
        } as PagedResult<StaffMember>
        expect(store.activeStaffCount).toBe(2)
      })

      it('should return 0 when staff members is null', () => {
        expect(store.activeStaffCount).toBe(0)
      })
    })

    describe('hasPendingInvitations', () => {
      it('should return true when there are pending invitations', () => {
        store.pendingInvitations = {
          totalCount: 3,
        } as PagedResult<ProviderInvitation>
        expect(store.hasPendingInvitations).toBe(true)
      })

      it('should return false when there are no pending invitations', () => {
        store.pendingInvitations = {
          totalCount: 0,
        } as PagedResult<ProviderInvitation>
        expect(store.hasPendingInvitations).toBe(false)
      })
    })

    describe('hasPendingJoinRequests', () => {
      it('should return true when there are pending join requests', () => {
        store.pendingJoinRequests = {
          totalCount: 2,
        } as PagedResult<ProviderJoinRequest>
        expect(store.hasPendingJoinRequests).toBe(true)
      })

      it('should return false when there are no pending join requests', () => {
        store.pendingJoinRequests = {
          totalCount: 0,
        } as PagedResult<ProviderJoinRequest>
        expect(store.hasPendingJoinRequests).toBe(false)
      })
    })
  })

  describe('Actions - Hierarchy Queries', () => {
    describe('loadHierarchy', () => {
      it('should load hierarchy successfully', async () => {
        const mockHierarchy: ProviderHierarchyResponse = {
          id: 'provider-1',
          hierarchyType: 'Organization',
          isIndependent: false,
          staffCount: 5,
        } as ProviderHierarchyResponse

        vi.mocked(hierarchyService.getProviderHierarchy).mockResolvedValue(mockHierarchy)

        await store.loadHierarchy('provider-1')

        expect(store.currentHierarchy).toEqual(mockHierarchy)
        expect(store.isLoading).toBe(false)
        expect(store.error).toBeNull()
      })

      it('should set loading state during fetch', async () => {
        vi.mocked(hierarchyService.getProviderHierarchy).mockImplementation(
          () =>
            new Promise((resolve) => {
              expect(store.isLoading).toBe(true)
              resolve({} as ProviderHierarchyResponse)
            }),
        )

        await store.loadHierarchy('provider-1')
      })

      it('should handle errors', async () => {
        const error = new Error('Failed to load hierarchy')
        vi.mocked(hierarchyService.getProviderHierarchy).mockRejectedValue(error)

        await expect(store.loadHierarchy('provider-1')).rejects.toThrow()
        expect(store.error).toBe('Failed to load hierarchy')
        expect(store.isLoading).toBe(false)
      })
    })

    describe('loadStaffMembers', () => {
      it('should load staff members successfully', async () => {
        const mockStaff: PagedResult<StaffMember> = {
          items: [{ id: 'staff-1' } as StaffMember],
          totalCount: 1,
          pageNumber: 1,
          pageSize: 10,
        }

        vi.mocked(hierarchyService.getStaffMembers).mockResolvedValue(mockStaff)

        await store.loadStaffMembers({ organizationId: 'org-1' })

        expect(store.staffMembers).toEqual(mockStaff)
        expect(store.isLoadingStaff).toBe(false)
        expect(store.error).toBeNull()
      })

      it('should handle errors', async () => {
        const error = new Error('Failed to load staff members')
        vi.mocked(hierarchyService.getStaffMembers).mockRejectedValue(error)

        await expect(store.loadStaffMembers({ organizationId: 'org-1' })).rejects.toThrow()
        expect(store.error).toBe('Failed to load staff members')
        expect(store.isLoadingStaff).toBe(false)
      })
    })

    describe('refreshStaffMembers', () => {
      it('should reload staff members with organization ID', async () => {
        const mockStaff: PagedResult<StaffMember> = {
          items: [],
          totalCount: 0,
          pageNumber: 1,
          pageSize: 10,
        }

        vi.mocked(hierarchyService.getStaffMembers).mockResolvedValue(mockStaff)

        await store.refreshStaffMembers('org-1')

        expect(hierarchyService.getStaffMembers).toHaveBeenCalledWith({ organizationId: 'org-1' })
      })
    })
  })

  describe('Actions - Invitations', () => {
    describe('sendInvitation', () => {
      it('should send invitation successfully', async () => {
        const mockInvitations: PagedResult<ProviderInvitation> = {
          items: [],
          totalCount: 0,
          pageNumber: 1,
          pageSize: 10,
        }

        vi.mocked(hierarchyService.sendInvitation).mockResolvedValue(undefined)
        vi.mocked(hierarchyService.getPendingInvitations).mockResolvedValue(mockInvitations)

        await store.sendInvitation('org-1', {
          phoneNumber: '+989123456789',
          inviteeName: 'John Doe',
        })

        expect(hierarchyService.sendInvitation).toHaveBeenCalledWith('org-1', {
          phoneNumber: '+989123456789',
          inviteeName: 'John Doe',
        })
        expect(store.isLoading).toBe(false)
        expect(store.error).toBeNull()
      })

      it('should reload invitations after sending', async () => {
        vi.mocked(hierarchyService.sendInvitation).mockResolvedValue(undefined)
        vi.mocked(hierarchyService.getPendingInvitations).mockResolvedValue({
          items: [],
          totalCount: 1,
        } as PagedResult<ProviderInvitation>)

        await store.sendInvitation('org-1', {
          phoneNumber: '+989123456789',
          inviteeName: 'John Doe',
        })

        expect(hierarchyService.getPendingInvitations).toHaveBeenCalled()
      })

      it('should handle errors', async () => {
        const error = new Error('Failed to send invitation')
        vi.mocked(hierarchyService.sendInvitation).mockRejectedValue(error)

        await expect(
          store.sendInvitation('org-1', {
            phoneNumber: '+989123456789',
            inviteeName: 'John Doe',
          }),
        ).rejects.toThrow()

        expect(store.error).toBe('Failed to send invitation')
        expect(store.isLoading).toBe(false)
      })
    })

    describe('acceptInvitation', () => {
      it('should accept invitation successfully', async () => {
        vi.mocked(hierarchyService.acceptInvitation).mockResolvedValue(undefined)

        await store.acceptInvitation('org-1', 'invitation-1', {})

        expect(hierarchyService.acceptInvitation).toHaveBeenCalledWith(
          'org-1',
          'invitation-1',
          {},
        )
        expect(store.isLoading).toBe(false)
        expect(store.error).toBeNull()
      })

      it('should handle errors', async () => {
        const error = new Error('Failed to accept invitation')
        vi.mocked(hierarchyService.acceptInvitation).mockRejectedValue(error)

        await expect(store.acceptInvitation('org-1', 'invitation-1', {})).rejects.toThrow()

        expect(store.error).toBe('Failed to accept invitation')
        expect(store.isLoading).toBe(false)
      })
    })

    describe('loadPendingInvitations', () => {
      it('should load pending invitations', async () => {
        const mockInvitations: PagedResult<ProviderInvitation> = {
          items: [{ id: 'inv-1' } as ProviderInvitation],
          totalCount: 1,
          pageNumber: 1,
          pageSize: 10,
        }

        vi.mocked(hierarchyService.getPendingInvitations).mockResolvedValue(mockInvitations)

        await store.loadPendingInvitations({ organizationId: 'org-1' })

        expect(store.pendingInvitations).toEqual(mockInvitations)
        expect(store.isLoadingInvitations).toBe(false)
      })
    })
  })

  describe('Actions - Join Requests', () => {
    describe('createJoinRequest', () => {
      it('should create join request successfully', async () => {
        vi.mocked(hierarchyService.createJoinRequest).mockResolvedValue(undefined)

        await store.createJoinRequest('org-1', { message: 'Please let me join' })

        expect(hierarchyService.createJoinRequest).toHaveBeenCalledWith('org-1', {
          message: 'Please let me join',
        })
        expect(store.isLoading).toBe(false)
        expect(store.error).toBeNull()
      })

      it('should handle errors', async () => {
        const error = new Error('Failed to create join request')
        vi.mocked(hierarchyService.createJoinRequest).mockRejectedValue(error)

        await expect(store.createJoinRequest('org-1', {})).rejects.toThrow()

        expect(store.error).toBe('Failed to create join request')
        expect(store.isLoading).toBe(false)
      })
    })

    describe('approveJoinRequest', () => {
      it('should approve join request and reload data', async () => {
        vi.mocked(hierarchyService.approveJoinRequest).mockResolvedValue(undefined)
        vi.mocked(hierarchyService.getJoinRequests).mockResolvedValue({
          items: [],
          totalCount: 0,
        } as PagedResult<ProviderJoinRequest>)
        vi.mocked(hierarchyService.getStaffMembers).mockResolvedValue({
          items: [],
          totalCount: 0,
        } as PagedResult<StaffMember>)

        await store.approveJoinRequest('org-1', 'request-1', {})

        expect(hierarchyService.approveJoinRequest).toHaveBeenCalledWith('org-1', 'request-1', {})
        expect(hierarchyService.getJoinRequests).toHaveBeenCalled()
        expect(hierarchyService.getStaffMembers).toHaveBeenCalled()
      })

      it('should handle errors', async () => {
        const error = new Error('Failed to approve join request')
        vi.mocked(hierarchyService.approveJoinRequest).mockRejectedValue(error)

        await expect(store.approveJoinRequest('org-1', 'request-1', {})).rejects.toThrow()

        expect(store.error).toBe('Failed to approve join request')
      })
    })

    describe('rejectJoinRequest', () => {
      it('should reject join request successfully', async () => {
        vi.mocked(hierarchyService.rejectJoinRequest).mockResolvedValue(undefined)
        vi.mocked(hierarchyService.getJoinRequests).mockResolvedValue({
          items: [],
          totalCount: 0,
        } as PagedResult<ProviderJoinRequest>)

        await store.rejectJoinRequest('org-1', 'request-1', { reason: 'Not suitable' })

        expect(hierarchyService.rejectJoinRequest).toHaveBeenCalledWith('org-1', 'request-1', {
          reason: 'Not suitable',
        })
      })
    })

    describe('loadPendingJoinRequests', () => {
      it('should load pending join requests', async () => {
        const mockRequests: PagedResult<ProviderJoinRequest> = {
          items: [{ id: 'req-1' } as ProviderJoinRequest],
          totalCount: 1,
          pageNumber: 1,
          pageSize: 10,
        }

        vi.mocked(hierarchyService.getJoinRequests).mockResolvedValue(mockRequests)

        await store.loadPendingJoinRequests({ organizationId: 'org-1' })

        expect(store.pendingJoinRequests).toEqual(mockRequests)
        expect(store.isLoadingJoinRequests).toBe(false)
      })
    })

    describe('loadMyJoinRequests', () => {
      it('should load my join requests', async () => {
        const mockRequests: PagedResult<ProviderJoinRequest> = {
          items: [{ id: 'req-1' } as ProviderJoinRequest],
          totalCount: 1,
          pageNumber: 1,
          pageSize: 10,
        }

        vi.mocked(hierarchyService.getJoinRequests).mockResolvedValue(mockRequests)

        await store.loadMyJoinRequests('individual-1')

        expect(hierarchyService.getJoinRequests).toHaveBeenCalledWith({
          requesterId: 'individual-1',
          organizationId: '',
        })
        expect(store.myJoinRequests).toEqual(mockRequests)
      })
    })
  })

  describe('Actions - Staff Management', () => {
    describe('removeStaffMember', () => {
      it('should remove staff member and reload data', async () => {
        vi.mocked(hierarchyService.removeStaffMember).mockResolvedValue(undefined)
        vi.mocked(hierarchyService.getStaffMembers).mockResolvedValue({
          items: [],
          totalCount: 0,
        } as PagedResult<StaffMember>)
        vi.mocked(hierarchyService.getProviderHierarchy).mockResolvedValue({
          staffCount: 4,
        } as ProviderHierarchyResponse)

        await store.removeStaffMember('org-1', 'staff-1', { reason: 'Left organization' })

        expect(hierarchyService.removeStaffMember).toHaveBeenCalledWith('org-1', 'staff-1', {
          reason: 'Left organization',
        })
        expect(hierarchyService.getStaffMembers).toHaveBeenCalled()
        expect(hierarchyService.getProviderHierarchy).toHaveBeenCalled()
      })

      it('should handle errors', async () => {
        const error = new Error('Failed to remove staff member')
        vi.mocked(hierarchyService.removeStaffMember).mockRejectedValue(error)

        await expect(store.removeStaffMember('org-1', 'staff-1', {})).rejects.toThrow()

        expect(store.error).toBe('Failed to remove staff member')
      })
    })
  })

  describe('Actions - Conversion', () => {
    describe('convertToOrganization', () => {
      it('should convert to organization successfully', async () => {
        const mockHierarchy: ProviderHierarchyResponse = {
          id: 'provider-1',
          hierarchyType: 'Organization',
          isIndependent: false,
        } as ProviderHierarchyResponse

        vi.mocked(hierarchyService.convertToOrganization).mockResolvedValue(mockHierarchy)

        await store.convertToOrganization('provider-1', {
          businessName: 'Elite Salon',
        })

        expect(hierarchyService.convertToOrganization).toHaveBeenCalledWith('provider-1', {
          businessName: 'Elite Salon',
        })
        expect(store.currentHierarchy).toEqual(mockHierarchy)
        expect(store.isLoading).toBe(false)
      })

      it('should handle errors', async () => {
        const error = new Error('Failed to convert to organization')
        vi.mocked(hierarchyService.convertToOrganization).mockRejectedValue(error)

        await expect(
          store.convertToOrganization('provider-1', { businessName: 'Test' }),
        ).rejects.toThrow()

        expect(store.error).toBe('Failed to convert to organization')
      })
    })
  })

  describe('Actions - State Management', () => {
    describe('clearHierarchyData', () => {
      it('should clear all hierarchy data', () => {
        store.currentHierarchy = {} as ProviderHierarchyResponse
        store.staffMembers = {} as PagedResult<StaffMember>
        store.pendingInvitations = {} as PagedResult<ProviderInvitation>
        store.pendingJoinRequests = {} as PagedResult<ProviderJoinRequest>
        store.error = 'Some error'

        store.clearHierarchyData()

        expect(store.currentHierarchy).toBeNull()
        expect(store.staffMembers).toBeNull()
        expect(store.pendingInvitations).toBeNull()
        expect(store.pendingJoinRequests).toBeNull()
        expect(store.error).toBeNull()
      })
    })

    describe('clearError', () => {
      it('should clear error', () => {
        store.error = 'Some error'

        store.clearError()

        expect(store.error).toBeNull()
      })
    })
  })
})
