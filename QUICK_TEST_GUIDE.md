# Quick Integration Test Guide

## 🎯 Current Status

Based on the automated check, here's what you need:

### ❌ Backend Services (Not Running)
- **ServiceCatalog API**: Should be at `http://localhost:5010`
- **UserManagement API**: Should be at `http://localhost:5020`

### ❌ Test Configuration (Not Set Up)
- `.env.test` file needs to be created

### 🔄 Dependencies
- Currently installing via `npm install`

---

## 🚀 Quick Setup (5 Minutes)

### Step 1: Start Backend Services

Open **2 separate terminals** and run:

**Terminal 1 - ServiceCatalog API:**
```bash
cd /home/user/Booking/src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api
dotnet run --launch-profile http
```

Wait for: `Now listening on: http://localhost:5010`

**Terminal 2 - UserManagement API:**
```bash
cd /home/user/Booking/src/UserManagement/Booksy.UserManagement.API
dotnet run --launch-profile http
```

Wait for: `Now listening on: http://localhost:5020`

---

### Step 2: Get Test Credentials

Once the UserManagement API is running, get a JWT token:

```bash
# Login and get token
curl -X POST http://localhost:5020/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test123!"
  }' | jq -r '.data.token'
```

**Or register a new test account:**
```bash
curl -X POST http://localhost:5020/api/v1/customers/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "testcustomer@test.com",
    "password": "Test123!",
    "firstName": "Test",
    "lastName": "Customer",
    "phoneNumber": "+989123456789"
  }'
```

---

### Step 3: Create .env.test File

```bash
cd /home/user/Booking/booksy-frontend

cat > .env.test << 'EOF'
# Replace these with actual values from your database/API
TEST_PROVIDER_ID=00000000-0000-0000-0000-000000000001
TEST_CUSTOMER_ID=00000000-0000-0000-0000-000000000002
TEST_AUTH_TOKEN=your-jwt-token-here

# API URLs (optional - defaults to localhost)
VITE_SERVICE_CATALOG_API_URL=http://localhost:5010/api
VITE_USER_MANAGEMENT_API_URL=http://localhost:5020/api
EOF
```

**To get IDs from database:**
```bash
# If using SQL Server
sqlcmd -S localhost -d BooksyDb -Q "SELECT TOP 1 Id FROM Providers"
sqlcmd -S localhost -d BooksyDb -Q "SELECT TOP 1 Id FROM Customers"
```

---

### Step 4: Run Tests

Once everything is set up:

```bash
cd /home/user/Booking/booksy-frontend

# Run all integration tests
npm run test:integration

# Or run specific suite
npm run test:integration -- gallery
npm run test:integration -- financial
npm run test:integration -- favorites
```

---

## 🔥 Alternative: Run Tests Without Backend

If you can't start the backend right now, you can run **unit tests** on the services (they'll fail but show you the test structure):

```bash
cd /home/user/Booking/booksy-frontend

# Run a dry run to see test structure
npm run test:integration -- --reporter=verbose --run false

# Or check test files
cat tests/integration/gallery.integration.spec.ts | grep "it('should"
```

---

## 📊 What Tests Will Run

### Gallery Management (9 scenarios)
- ✅ Upload single/multiple images
- ✅ File validation (size, type)
- ✅ Update metadata
- ✅ Reorder images
- ✅ Set primary image
- ✅ Delete images
- ✅ Error handling

### Financial & Payouts (8 scenarios)
- ✅ Current/previous month earnings
- ✅ Commission calculations
- ✅ Transaction history with filters
- ✅ Create payout requests
- ✅ Payout status tracking
- ✅ Financial dashboard

### Customer Favorites (10 scenarios)
- ✅ Add/remove favorites
- ✅ Toggle favorite status
- ✅ Favorites list with pagination
- ✅ Quick rebook suggestions
- ✅ Cache validation
- ✅ Persian/RTL support

**Total: 375+ automated assertions**

---

## 🐛 Troubleshooting

### "Backend not running"
```bash
# Check if ports are in use
netstat -an | grep -E "(5010|5020)"

# Or use the automated checker
./run-integration-tests.sh
```

### "Authentication failed"
```bash
# Check if token is valid
curl -H "Authorization: Bearer YOUR_TOKEN" \
  http://localhost:5020/api/v1/customers/profile
```

### "Database connection error"
```bash
# Check connection string in appsettings.json
# Run migrations
cd src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure
dotnet ef database update --startup-project ../Booksy.ServiceCatalog.Api
```

---

## 📝 Test Results

After running, you'll see output like:

```
✓ Gallery Management
  ✓ should upload a single image successfully (1234ms)
  ✓ should handle multiple image uploads (2345ms)
  ✓ should update image metadata (456ms)
  ...

Test Files  3 passed (3)
     Tests  27 passed (27)
  Duration  45.67s
```

---

## 🎯 Next Steps

1. ✅ Use automated script: `./run-integration-tests.sh`
2. ✅ Follow setup steps above
3. ✅ Run tests: `npm run test:integration`
4. ✅ Check coverage: `npm run test:integration:coverage`
5. ✅ Review results in `booksy-frontend/coverage/` directory

---

## 📚 More Resources

- **Manual Tests**: See `INTEGRATION_TESTS.md`
- **Test Documentation**: See `booksy-frontend/tests/integration/README.md`
- **API Documentation**: Check Postman collections in root directory
