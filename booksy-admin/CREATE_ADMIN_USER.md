# Booksy Admin Panel - Create Admin User Guide

**Official guide for creating admin users for the Booksy Admin Panel**

---

## Prerequisites

Before creating an admin user, ensure:
- ✅ PostgreSQL database is running (port 5432)
- ✅ Backend Gateway is running (port 5000)
- ✅ UserManagement API is running (port 5001)

Check services:
```bash
# Check if all services are running
docker ps

# Or check manually
curl http://localhost:5000/health  # Gateway
curl http://localhost:5001/health  # UserManagement API
```

---

## Quick Start: Create Admin User (3 Steps)

### Method 1: API + Database Update (Recommended)

#### Step 1: Register User via Booksy API

```bash
curl -X POST http://localhost:5000/api/v1/Users \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@booksy.com",
    "password": "Booksy@Admin2025",
    "firstName": "Booksy",
    "lastName": "Admin",
    "phoneNumber": "+989123456789"
  }'
```

#### Step 2: Update User Role to Admin

The API creates users as "Client" by default. Update the role in the database:

```bash
# Connect to PostgreSQL (via Docker)
docker exec -it booksy-postgres psql -U booksy_admin -d booksy_user_management

# Or connect directly if PostgreSQL is running locally
psql -U booksy_admin -d booksy_user_management -h localhost
```

```sql
-- Update the user role to Admin
UPDATE "Users"
SET "Role" = 'Admin'
WHERE "Email" = 'admin@booksy.com';

-- Verify the update
SELECT "Id", "Email", "FirstName", "LastName", "Role", "IsActive"
FROM "Users"
WHERE "Email" = 'admin@booksy.com';
```

Expected output:
```
                  Id                  |      Email       | FirstName | LastName | Role  | IsActive
--------------------------------------+------------------+-----------+----------+-------+----------
 <uuid-here>                          | admin@booksy.com | Booksy    | Admin    | Admin | true
```

#### Step 3: Test Login

**Via API:**
```bash
curl -X POST http://localhost:5000/api/v1/Auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@booksy.com",
    "password": "Booksy@Admin2025",
    "rememberMe": false
  }'
```

**Expected Response (200 OK):**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "refresh-token-here",
  "expiresIn": 3600,
  "tokenType": "Bearer",
  "userInfo": {
    "id": "uuid-here",
    "email": "admin@booksy.com",
    "displayName": "Booksy Admin",
    "roles": ["Admin"]
  }
}
```

**Via Admin Panel:**
1. Open http://localhost:5173 (dev) or http://localhost:3000 (docker)
2. Login with:
   - Email: `admin@booksy.com`
   - Password: `Booksy@Admin2025`
3. You should be redirected to the Dashboard ✅

---

## Method 2: Direct Database Insert

If you prefer to create the admin user directly in the database:

```bash
# Connect to PostgreSQL
docker exec -it booksy-postgres psql -U booksy_admin -d booksy_user_management
```

```sql
-- Generate BCrypt hash for password "Booksy@Admin2025"
-- You must use your backend's password hasher to generate this hash
-- Example: In .NET use BCrypt.HashPassword("Booksy@Admin2025")

INSERT INTO "Users" (
  "Id",
  "Email",
  "PasswordHash",
  "FirstName",
  "LastName",
  "PhoneNumber",
  "Role",
  "IsActive",
  "EmailConfirmed",
  "CreatedAt",
  "UpdatedAt"
)
VALUES (
  gen_random_uuid(),
  'admin@booksy.com',
  -- REPLACE THIS with your BCrypt hash
  '$2a$11$YourBCryptHashHere',
  'Booksy',
  'Admin',
  '+989123456789',
  'Admin',
  true,
  true,
  NOW(),
  NOW()
);
```

**⚠️ Important:** You need to generate a BCrypt hash using your backend. See "Password Hashing" section below.

---

## Password Hashing

To generate a BCrypt hash for your password:

### Option A: Using .NET (Recommended)

Create a small C# console app or use the existing backend:

```csharp
using BCrypt.Net;

string password = "Booksy@Admin2025";
string hash = BCrypt.HashPassword(password, workFactor: 11);
Console.WriteLine(hash);
```

Output example:
```
$2a$11$N9qo8uLOickgx2ZMRZoMye1234567890abcdefghijklmnopqr
```

### Option B: Using Node.js

```bash
npm install bcrypt
```

```javascript
const bcrypt = require('bcrypt');

async function hashPassword() {
  const hash = await bcrypt.hash('Booksy@Admin2025', 11);
  console.log(hash);
}

hashPassword();
```

### Option C: Online Tool (Development Only)

⚠️ **Use only for development/testing, never for production!**

1. Visit: https://bcrypt-generator.com/
2. Input: `Booksy@Admin2025`
3. Rounds: `11`
4. Copy the generated hash

---

## Verify Admin User Creation

### Check in Database

```sql
-- View all admin users
SELECT "Id", "Email", "FirstName", "LastName", "Role", "IsActive"
FROM "Users"
WHERE "Role" = 'Admin';
```

### Test Login Endpoint

```bash
# Should return 200 OK with tokens
curl -X POST http://localhost:5000/api/v1/Auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@booksy.com","password":"Booksy@Admin2025","rememberMe":false}' \
  -w "\nHTTP Status: %{http_code}\n"
```

**Success Response:**
- HTTP Status: `200`
- Body contains: `accessToken`, `refreshToken`, `userInfo`

**Failure Response:**
- HTTP Status: `401` - Invalid credentials (wrong password or user doesn't exist)
- HTTP Status: `404` - Endpoint not found (check backend is running)

---

## Troubleshooting

### ❌ "Invalid credentials" Error (401)

**Possible Causes:**
1. Password doesn't match the hash in database
2. User's `IsActive` is `false`
3. Email address is incorrect
4. User doesn't have `Admin` role

**Solutions:**

```sql
-- Check user details
SELECT "Email", "Role", "IsActive", "EmailConfirmed"
FROM "Users"
WHERE "Email" = 'admin@booksy.com';

-- Activate user
UPDATE "Users"
SET "IsActive" = true, "EmailConfirmed" = true
WHERE "Email" = 'admin@booksy.com';

-- Set role to Admin
UPDATE "Users"
SET "Role" = 'Admin'
WHERE "Email" = 'admin@booksy.com';

-- Reset password (you need to generate new hash)
UPDATE "Users"
SET "PasswordHash" = '$2a$11$YourNewBCryptHash'
WHERE "Email" = 'admin@booksy.com';
```

### ❌ Can't Access Admin Panel After Login

**Check user role:**
```sql
SELECT "Email", "Role"
FROM "Users"
WHERE "Email" = 'admin@booksy.com';
```

The `Role` must be exactly: **`Admin`** or **`SuperAdmin`**

**Fix:**
```sql
UPDATE "Users"
SET "Role" = 'Admin'
WHERE "Email" = 'admin@booksy.com';
```

### ❌ Database Connection Failed

**Check PostgreSQL is running:**
```bash
# Via Docker
docker ps | grep postgres

# Test connection
docker exec -it booksy-postgres psql -U booksy_admin -d booksy_user_management -c "SELECT 1;"
```

### ❌ 404 Not Found Error

**Ensure backend is running:**
```bash
# Check Gateway
curl http://localhost:5000/health

# Check UserManagement API
curl http://localhost:5001/health
```

If not running, start them:
```bash
# Via Docker Compose
docker-compose -f docker-compose.prod.yml up -d

# Or manually
cd src/APIGateway/Booksy.Gateway && dotnet run &
cd src/UserManagement/Booksy.UserManagement.API && dotnet run &
```

---

## Default Admin Credentials

After following this guide, use these credentials to login to the admin panel:

**Login URL:** http://localhost:5173 (dev) or http://localhost:3000 (docker)

**Credentials:**
- **Email:** `admin@booksy.com`
- **Password:** `Booksy@Admin2025`

⚠️ **SECURITY WARNING:** Change this password immediately after first login!

---

## Create Additional Admin Users

Once logged in to the admin panel:

1. Navigate to **Users** in the sidebar
2. Click **Create User** button
3. Fill in the form:
   - Email: New admin email
   - Password: Strong password
   - First Name: First name
   - Last Name: Last name
   - Role: Select **Admin**
4. Click **OK**

The new admin user can now login to the admin panel.

---

## Security Best Practices

### Password Requirements
- ✅ Minimum 12 characters
- ✅ Mix of uppercase and lowercase letters
- ✅ Include numbers
- ✅ Include special characters (@, !, #, $, etc.)
- ❌ Don't use common passwords
- ❌ Don't reuse passwords

### General Security
1. **Change default password immediately**
2. **Use unique passwords** for each admin account
3. **Enable 2FA** if your backend supports it
4. **Regularly audit admin users** - remove inactive accounts
5. **Monitor login attempts** - check for suspicious activity
6. **Use HTTPS in production** - Never use HTTP for admin panel
7. **Rotate passwords quarterly**
8. **Never commit passwords** to Git repositories
9. **Use environment variables** for sensitive configuration
10. **Limit admin access** to only trusted team members

---

## Next Steps After Creating Admin User

1. ✅ **Login to Admin Panel**
   - Verify you can access dashboard
   - Check all menu items load

2. ✅ **Change Default Password**
   - Go to Profile (click user avatar)
   - Update password to something secure

3. ✅ **Create Additional Admins**
   - Add other team members who need admin access
   - Use strong, unique passwords for each

4. ✅ **Test User Management**
   - Create a test user
   - Edit and delete test user
   - Verify all CRUD operations work

5. ✅ **Test Provider Management**
   - Navigate to Providers section
   - Test approval workflow if you have pending providers

6. ✅ **Configure Production Settings**
   - Update `.env.production` with your production API URL
   - Build and deploy admin panel
   - Test in production environment

---

## Quick Reference

### Database Connection

**Via Docker:**
```bash
docker exec -it booksy-postgres psql -U booksy_admin -d booksy_user_management
```

**Direct Connection:**
```bash
psql -U booksy_admin -d booksy_user_management -h localhost -p 5432
```

**Connection String:**
```
Host=localhost;Port=5432;Database=booksy_user_management;Username=booksy_admin;Password=YourPostgresPassword123!
```

### Common SQL Queries

```sql
-- List all admin users
SELECT * FROM "Users" WHERE "Role" = 'Admin';

-- Count users by role
SELECT "Role", COUNT(*) FROM "Users" GROUP BY "Role";

-- Find user by email
SELECT * FROM "Users" WHERE "Email" = 'admin@booksy.com';

-- Activate all users
UPDATE "Users" SET "IsActive" = true;

-- Delete a user (use with caution!)
DELETE FROM "Users" WHERE "Email" = 'user@example.com';
```

### Admin Panel URLs

- **Development:** http://localhost:5173
- **Docker:** http://localhost:3000
- **Production:** http://napstar.ir/admin (or your configured domain)

### Backend API Endpoints

- **Gateway:** http://localhost:5000
- **UserManagement API:** http://localhost:5001
- **ServiceCatalog API:** http://localhost:5002

---

## Support & Contact

If you encounter issues not covered in this guide:

1. **Check Logs:**
   - Gateway logs: `docker logs booksy-gateway`
   - UserManagement API logs: `docker logs booksy-usermanagement-api`
   - Admin panel console: Open browser DevTools (F12)

2. **Verify Configuration:**
   - Database schema matches expectations
   - Backend services are healthy
   - CORS is properly configured

3. **Documentation:**
   - [README.md](README.md) - Full admin panel documentation
   - [API_INTEGRATION.md](API_INTEGRATION.md) - API integration details
   - [TESTING_GUIDE.md](TESTING_GUIDE.md) - Complete testing procedures

---

**Last Updated:** December 2025
**Version:** 1.0
**For:** Booksy Admin Panel
