/**
 * Integration Test Setup
 *
 * This file runs before all integration tests
 */

import { beforeAll, afterAll } from 'vitest'

// Load environment variables
if (process.env.NODE_ENV === 'test') {
  console.log('🧪 Integration Test Environment')
  console.log('================================')
  console.log(`ServiceCatalog API: ${process.env.VITE_SERVICE_CATALOG_API_URL || 'http://localhost:5010/api'}`)
  console.log(`UserManagement API: ${process.env.VITE_USER_MANAGEMENT_API_URL || 'http://localhost:5020/api'}`)
  console.log('')
}

// Global test configuration
beforeAll(() => {
  // Check required environment variables
  const requiredEnvVars = [
    'TEST_PROVIDER_ID',
    'TEST_CUSTOMER_ID',
    'TEST_AUTH_TOKEN',
  ]

  const missing = requiredEnvVars.filter(varName => !process.env[varName])

  if (missing.length > 0) {
    console.warn('⚠️  Warning: Missing environment variables:')
    missing.forEach(varName => console.warn(`   - ${varName}`))
    console.warn('')
    console.warn('   Tests may fail without these variables.')
    console.warn('   Create a .env.test file with required values.')
    console.warn('')
  }
})

afterAll(() => {
  console.log('')
  console.log('✅ Integration tests completed')
})

// Global error handler
process.on('unhandledRejection', (reason, promise) => {
  console.error('Unhandled Rejection at:', promise, 'reason:', reason)
})
