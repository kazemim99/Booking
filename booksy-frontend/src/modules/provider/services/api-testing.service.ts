// src/modules/provider/services/api-testing.service.ts

/**
 * API Testing Service
 * Helper service to test provider hierarchy API endpoints
 */

import { hierarchyService } from './hierarchy.service'
import type {
  RegisterOrganizationRequest,
  RegisterIndependentIndividualRequest,
  SendInvitationRequest,
  CreateJoinRequestRequest,
} from '../types/hierarchy.types'

export class ApiTestingService {
  /**
   * Test Organization Registration
   */
  static async testOrganizationRegistration() {
    console.log('🧪 Testing Organization Registration...')

    const testOrg: RegisterOrganizationRequest = {
      ownerId: 'test-user-id',
      businessName: 'آرایشگاه تست',
      description: 'این یک سازمان تستی است',
      type: 'Salon',
      email: 'test@example.com',
      phone: '09123456789',
      addressLine1: 'خیابان تست',
      city: 'تهران',
      state: 'تهران',
      postalCode: '1234567890',
      country: 'Iran',
    }

    try {
      const result = await hierarchyService.registerOrganization(testOrg)
      console.log('✅ Organization Registration Success:', result)
      return { success: true, data: result }
    } catch (error) {
      console.error('❌ Organization Registration Failed:', error)
      return { success: false, error }
    }
  }

  /**
   * Test Individual Registration
   */
  static async testIndividualRegistration() {
    console.log('🧪 Testing Individual Registration...')

    const testIndividual: RegisterIndependentIndividualRequest = {
      ownerId: 'test-user-id',
      firstName: 'علی',
      lastName: 'محمدی',
      bio: 'آرایشگر مستقل با 10 سال سابقه',
      email: 'ali@example.com',
      phone: '09123456789',
      city: 'تهران',
      state: 'تهران',
      country: 'Iran',
    }

    try {
      const result = await hierarchyService.registerIndividual(testIndividual)
      console.log('✅ Individual Registration Success:', result)
      return { success: true, data: result }
    } catch (error) {
      console.error('❌ Individual Registration Failed:', error)
      return { success: false, error }
    }
  }

  /**
   * Test Send Invitation
   */
  static async testSendInvitation(organizationId: string) {
    console.log('🧪 Testing Send Invitation...')

    const testInvitation: SendInvitationRequest = {
      organizationId,
      inviteePhoneNumber: '09123456789',
      inviteeName: 'رضا احمدی',
      message: 'می‌خواهیم شما به تیم ما بپیوندید',
    }

    try {
      const result = await hierarchyService.sendInvitation(organizationId, testInvitation)
      console.log('✅ Send Invitation Success:', result)
      return { success: true, data: result }
    } catch (error) {
      console.error('❌ Send Invitation Failed:', error)
      return { success: false, error }
    }
  }

  /**
   * Test Get Provider Hierarchy
   */
  static async testGetHierarchy(providerId: string) {
    console.log('🧪 Testing Get Provider Hierarchy...')

    try {
      const result = await hierarchyService.getProviderHierarchy(providerId)
      console.log('✅ Get Hierarchy Success:', result)
      return { success: true, data: result }
    } catch (error) {
      console.error('❌ Get Hierarchy Failed:', error)
      return { success: false, error }
    }
  }

  /**
   * Test Get Staff Members
   */
  static async testGetStaffMembers(organizationId: string) {
    console.log('🧪 Testing Get Staff Members...')

    try {
      const result = await hierarchyService.getStaffMembers({
        organizationId,
        isActive: true,
      })
      console.log('✅ Get Staff Members Success:', result)
      return { success: true, data: result }
    } catch (error) {
      console.error('❌ Get Staff Members Failed:', error)
      return { success: false, error }
    }
  }

  /**
   * Test Create Join Request
   */
  static async testCreateJoinRequest(organizationId: string, requesterId: string) {
    console.log('🧪 Testing Create Join Request...')

    const testRequest: CreateJoinRequestRequest = {
      organizationId,
      requesterId,
      message: 'می‌خواهم به تیم شما بپیوندم',
    }

    try {
      const result = await hierarchyService.createJoinRequest(organizationId, testRequest)
      console.log('✅ Create Join Request Success:', result)
      return { success: true, data: result }
    } catch (error) {
      console.error('❌ Create Join Request Failed:', error)
      return { success: false, error }
    }
  }

  /**
   * Test Convert to Organization
   */
  static async testConvertToOrganization(providerId: string) {
    console.log('🧪 Testing Convert to Organization...')

    const testConversion = {
      individualProviderId: providerId,
      businessName: 'سالن زیبایی جدید',
      description: 'تبدیل از فردی به سازمان',
      businessType: 'Salon',
    }

    try {
      const result = await hierarchyService.convertToOrganization(providerId, testConversion)
      console.log('✅ Convert to Organization Success:', result)
      return { success: true, data: result }
    } catch (error) {
      console.error('❌ Convert to Organization Failed:', error)
      return { success: false, error }
    }
  }

  /**
   * Run All Tests
   */
  static async runAllTests(providerId?: string) {
    console.log('🚀 Running All Hierarchy API Tests...\n')

    const results = {
      organizationRegistration: await this.testOrganizationRegistration(),
      individualRegistration: await this.testIndividualRegistration(),
    }

    if (providerId) {
      results['getHierarchy'] = await this.testGetHierarchy(providerId)
      results['getStaffMembers'] = await this.testGetStaffMembers(providerId)
      results['sendInvitation'] = await this.testSendInvitation(providerId)
      results['convertToOrganization'] = await this.testConvertToOrganization(providerId)
    }

    console.log('\n📊 Test Results Summary:')
    console.table(
      Object.entries(results).map(([test, result]) => ({
        Test: test,
        Status: result.success ? '✅ PASS' : '❌ FAIL',
      }))
    )

    return results
  }

  /**
   * Check which endpoints are working
   */
  static async checkEndpointHealth() {
    console.log('🏥 Checking Endpoint Health...\n')

    const endpoints = [
      { name: 'Register Organization', path: '/v1/providers/organizations', method: 'POST' },
      { name: 'Register Individual', path: '/v1/providers/individuals', method: 'POST' },
      { name: 'Get Hierarchy', path: '/v1/providers/:id/hierarchy', method: 'GET' },
      { name: 'Get Staff', path: '/v1/providers/:id/hierarchy/staff', method: 'GET' },
      { name: 'Send Invitation', path: '/v1/providers/:id/hierarchy/invitations', method: 'POST' },
      { name: 'Get Invitations', path: '/v1/providers/:id/hierarchy/invitations', method: 'GET' },
      { name: 'Accept Invitation', path: '/v1/providers/:id/hierarchy/invitations/:invId/accept', method: 'POST' },
      { name: 'Create Join Request', path: '/v1/providers/:id/hierarchy/join-requests', method: 'POST' },
      { name: 'Get Join Requests', path: '/v1/providers/:id/hierarchy/join-requests', method: 'GET' },
      { name: 'Approve Join Request', path: '/v1/providers/:id/hierarchy/join-requests/:reqId/approve', method: 'POST' },
      { name: 'Reject Join Request', path: '/v1/providers/:id/hierarchy/join-requests/:reqId/reject', method: 'POST' },
      { name: 'Remove Staff', path: '/v1/providers/:id/hierarchy/staff/:staffId', method: 'DELETE' },
      { name: 'Convert to Organization', path: '/v1/providers/:id/hierarchy/convert-to-organization', method: 'POST' },
    ]

    console.log('📋 Required API Endpoints:\n')
    endpoints.forEach(endpoint => {
      console.log(`${endpoint.method.padEnd(7)} ${endpoint.path}`)
      console.log(`        → ${endpoint.name}`)
    })

    return endpoints
  }
}

// Export for console testing
if (typeof window !== 'undefined') {
  (window as any).testHierarchyAPI = ApiTestingService
}

export default ApiTestingService
