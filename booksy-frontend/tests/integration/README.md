# Integration Tests

Automated integration tests for Booksy frontend features.

## Overview

These tests verify the integration between frontend services and backend APIs for:

- **Gallery Management** (Priority 4)
- **Financial & Payouts** (Priority 5)
- **Customer Favorites** (Priority 6)

## Prerequisites

### 1. Backend Services Running

```bash
# Terminal 1: ServiceCatalog API
cd src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api
dotnet run --launch-profile http

# Terminal 2: UserManagement API
cd src/UserManagement/Booksy.UserManagement.API
dotnet run --launch-profile http
```

### 2. Test Environment Variables

Create a `.env.test` file in the `booksy-frontend` directory:

```env
# Test Account Credentials
TEST_PROVIDER_ID=your-test-provider-uuid
TEST_CUSTOMER_ID=your-test-customer-uuid
TEST_AUTH_TOKEN=your-jwt-token

# API Endpoints (optional, defaults to localhost)
VITE_SERVICE_CATALOG_API_URL=http://localhost:5010/api
VITE_USER_MANAGEMENT_API_URL=http://localhost:5020/api
```

### 3. Test Data

Ensure your test database has:
- At least one provider account
- At least one customer account
- Some completed bookings (for financial tests)
- Some providers to favorite

## Running Tests

### Run All Integration Tests

```bash
npm run test:integration
```

### Run Specific Test Suites

```bash
# Gallery tests only
npm run test:integration -- gallery

# Financial tests only
npm run test:integration -- financial

# Favorites tests only
npm run test:integration -- favorites
```

### Run with Coverage

```bash
npm run test:integration:coverage
```

### Watch Mode (for development)

```bash
npm run test:integration:watch
```

## Test Structure

```
tests/integration/
├── README.md (this file)
├── gallery.integration.spec.ts
├── financial.integration.spec.ts
└── favorites.integration.spec.ts
```

## Test Patterns

### Setup & Teardown

```typescript
describe('Feature Tests', () => {
  beforeAll(async () => {
    // Setup: Load test data, authenticate
  })

  afterEach(async () => {
    // Cleanup: Remove created resources
  })

  it('should test something', async () => {
    // Test implementation
  })
})
```

### Authentication

Tests use JWT tokens from environment variables:

```typescript
const authToken = process.env.TEST_AUTH_TOKEN
```

To get a token:

```bash
# Login and extract token
curl -X POST http://localhost:5020/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email": "test@example.com", "password": "Test123!"}' \
  | jq -r '.data.token'
```

### API Validation

Each test validates both:
1. Frontend service behavior
2. Backend API response

Example:

```typescript
it('should fetch data correctly', async () => {
  const result = await service.getData(id)

  // Validate structure
  expect(result).toHaveProperty('id')
  expect(result).toHaveProperty('name')

  // Validate data types
  expect(typeof result.id).toBe('string')

  // Validate business logic
  expect(result.amount).toBeGreaterThan(0)
})
```

## Troubleshooting

### Test Timeout

If tests timeout, increase the timeout in `vitest.config.ts`:

```typescript
export default defineConfig({
  test: {
    testTimeout: 30000, // 30 seconds
  },
})
```

### Authentication Errors

```
Error: 401 Unauthorized
```

**Solution**: Regenerate your JWT token and update `.env.test`

### Network Errors

```
Error: connect ECONNREFUSED
```

**Solution**: Ensure backend services are running on correct ports

### Database Errors

```
Error: Provider not found
```

**Solution**: Ensure test data exists in database

## Best Practices

### 1. Test Independence

Each test should be independent and not rely on other tests:

```typescript
// ✅ Good
it('should add favorite', async () => {
  await favoritesService.addFavorite(customerId, { providerId })
  // ...
})

// ❌ Bad
let favoriteId: string

it('should add favorite', async () => {
  const fav = await favoritesService.addFavorite(customerId, { providerId })
  favoriteId = fav.id // Don't rely on this in next test
})
```

### 2. Cleanup

Always cleanup resources created during tests:

```typescript
afterEach(async () => {
  for (const id of createdIds) {
    await service.delete(id)
  }
  createdIds.length = 0
})
```

### 3. Meaningful Assertions

Use specific assertions:

```typescript
// ✅ Good
expect(result.amount).toBe(500000)
expect(result.status).toBe('Pending')

// ❌ Bad
expect(result).toBeTruthy()
```

### 4. Test Real Scenarios

Test realistic user workflows:

```typescript
it('should complete booking workflow', async () => {
  // 1. Search for provider
  const providers = await searchProviders('salon')

  // 2. View provider details
  const provider = await getProviderDetails(providers[0].id)

  // 3. Select service
  const service = provider.services[0]

  // 4. Create booking
  const booking = await createBooking({
    providerId: provider.id,
    serviceId: service.id,
    // ...
  })

  expect(booking.status).toBe('Confirmed')
})
```

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Integration Tests

on: [push, pull_request]

jobs:
  integration-tests:
    runs-on: ubuntu-latest

    services:
      postgres:
        image: postgres:15
        env:
          POSTGRES_PASSWORD: postgres
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'

      - name: Start Backend Services
        run: |
          cd src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api
          dotnet run --launch-profile http &

          cd ../../UserManagement/Booksy.UserManagement.API
          dotnet run --launch-profile http &

      - name: Setup Node.js
        uses: actions/setup-node@v3
        with:
          node-version: '18'

      - name: Install Dependencies
        run: |
          cd booksy-frontend
          npm ci

      - name: Run Integration Tests
        env:
          TEST_PROVIDER_ID: ${{ secrets.TEST_PROVIDER_ID }}
          TEST_CUSTOMER_ID: ${{ secrets.TEST_CUSTOMER_ID }}
          TEST_AUTH_TOKEN: ${{ secrets.TEST_AUTH_TOKEN }}
        run: |
          cd booksy-frontend
          npm run test:integration
```

## Coverage Goals

Target coverage for integration tests:

- **API Integration**: 90%+
- **Critical Paths**: 100%
- **Error Handling**: 80%+

## Reporting Issues

When reporting test failures, include:

1. Test name and file
2. Error message
3. Backend logs
4. Network request/response (from browser DevTools)
5. Environment details (OS, Node version, browser)

## Resources

- [Vitest Documentation](https://vitest.dev/)
- [Testing Library](https://testing-library.com/)
- [Booksy API Documentation](../../../API_DOCUMENTATION.md)
