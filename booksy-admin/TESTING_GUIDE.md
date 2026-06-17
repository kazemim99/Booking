# Admin Panel Testing Guide

## Prerequisites

Before testing the admin panel, ensure:

1. ✅ PostgreSQL database is running
2. ✅ Backend services are running (Gateway, UserManagement API)
3. ✅ At least one admin user exists in the database

## Step 1: Create an Admin User

### Option A: Using Backend Registration Endpoint

```bash
# Send POST request to create admin user
curl -X POST http://localhost:5000/api/v1/Users \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@booksy.com",
    "password": "Admin@123456",
    "firstName": "Admin",
    "lastName": "User",
    "role": "Admin"
  }'
```

### Option B: Direct Database Insert (if needed)

```sql
-- Connect to your PostgreSQL database
-- Adjust table/column names based on your actual schema

-- Example structure (modify as needed):
INSERT INTO "Users" (
  "Id",
  "Email",
  "PasswordHash",
  "FirstName",
  "LastName",
  "Role",
  "IsActive",
  "CreatedAt"
)
VALUES (
  gen_random_uuid(),
  'admin@booksy.com',
  -- Use BCrypt or your backend's password hashing
  '$2a$11$hash_here',  -- Replace with actual hash
  'Admin',
  'User',
  'Admin',
  true,
  NOW()
);
```

## Step 2: Start Backend Services

### Development Environment

```bash
# Terminal 1: Start Gateway (Port 5000)
cd src/APIGateway/Booksy.Gateway
dotnet run

# Terminal 2: Start UserManagement API (Port 5001)
cd src/UserManagement/Booksy.UserManagement.API
dotnet run

# Terminal 3: Start ServiceCatalog API (Port 5002)
cd src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api
dotnet run
```

### Docker Environment

```bash
# Start all services with docker-compose
docker-compose -f docker-compose.prod.yml up -d

# Check service health
docker-compose -f docker-compose.prod.yml ps
```

## Step 3: Start Admin Panel

### Development Mode

```bash
cd booksy-admin
npm install  # First time only
npm run dev
```

Admin panel will be available at: [http://localhost:5173](http://localhost:5173)

### Production Mode (Docker)

```bash
cd booksy-admin
docker-compose up -d
```

Admin panel will be available at: [http://localhost:3000](http://localhost:3000)

## Step 4: Test Authentication

### 4.1 Access Login Page

1. Open browser to [http://localhost:5173](http://localhost:5173)
2. You should see the login page with email and password fields

### 4.2 Test Login

1. Enter credentials:
   - Email: `admin@booksy.com`
   - Password: `Admin@123456` (or your password)
2. Click "Login"

**Expected Results:**
- ✅ Success message appears
- ✅ Redirected to Dashboard
- ✅ Sidebar navigation visible
- ✅ User email displayed in top-right corner

**Common Issues:**
- ❌ "Invalid credentials" → Check password is correct
- ❌ "Network error" → Verify backend is running on port 5000
- ❌ CORS error → Add admin panel URL to backend CORS policy

### 4.3 Verify Token Storage

Open Browser DevTools (F12) → Console:

```javascript
// Check tokens are stored
localStorage.getItem('admin_token')
localStorage.getItem('admin_refresh_token')
```

Both should return JWT tokens (long strings starting with `eyJ...`)

## Step 5: Test Dashboard

After successful login, verify Dashboard features:

### 5.1 Statistics Cards
- [ ] Total Users card displays
- [ ] Total Providers card displays
- [ ] Total Bookings card displays
- [ ] Total Revenue card displays

### 5.2 Charts
- [ ] User Growth chart renders
- [ ] Booking Trends chart renders

### 5.3 Quick Actions
- [ ] Click "Manage Providers" → Navigates to Providers page
- [ ] Click "Manage Users" → Navigates to Users page
- [ ] Click "View Analytics" → Navigates to Analytics page

## Step 6: Test User Management

### 6.1 View Users List

1. Click "Users" in sidebar
2. Verify:
   - [ ] Users table displays
   - [ ] Pagination controls visible
   - [ ] Search box functional

### 6.2 Create New User

1. Click "Create User" button
2. Fill in form:
   - Email: `test@example.com`
   - Password: `Test@123456`
   - First Name: `Test`
   - Last Name: `User`
   - Role: `Client`
3. Click "OK"

**Expected:**
- ✅ Success message
- ✅ User appears in list
- ✅ Modal closes

### 6.3 Search Users

1. Type email in search box
2. Press Enter or wait for debounce
3. Verify filtered results

### 6.4 Filter Users

1. Select role from "Filter by role" dropdown
2. Verify filtered results
3. Select status from "Filter by status" dropdown
4. Verify filtered results

### 6.5 Edit User

1. Click "Edit" button on a user
2. Modify first name
3. Click "OK"
4. Verify changes are saved

### 6.6 Toggle User Status

1. Click "Deactivate" on an active user
2. Verify user status changes to "Inactive"
3. Click "Activate" to reactivate
4. Verify user status changes back to "Active"

### 6.7 Delete User

1. Click "Delete" on a user
2. Confirm deletion in popup
3. Verify user is removed from list

## Step 7: Test Provider Management

### 7.1 View Providers

1. Click "Providers" in sidebar
2. Verify tabs:
   - [ ] All Providers
   - [ ] Pending Approval (with badge if any pending)
   - [ ] Approved
   - [ ] Rejected
   - [ ] Suspended

### 7.2 Approve Provider

1. Click "Pending Approval" tab
2. Click "Approve" on a pending provider
3. Verify:
   - [ ] Success message
   - [ ] Provider moves to "Approved" tab
   - [ ] Pending badge count decreases

### 7.3 Reject Provider

1. Click "Pending Approval" tab
2. Click "Reject" on a pending provider
3. Enter rejection reason
4. Click "OK"
5. Verify:
   - [ ] Success message
   - [ ] Provider moves to "Rejected" tab

### 7.4 View Provider Details

1. Click "View" on any provider
2. Verify:
   - [ ] Provider details page displays
   - [ ] Business information shown
   - [ ] Statistics displayed
   - [ ] Action buttons available (based on status)

### 7.5 Suspend Provider

1. Go to "Approved" tab
2. Click "Suspend" on an approved provider
3. Enter suspension reason
4. Click "OK"
5. Verify provider moves to "Suspended" tab

### 7.6 Reactivate Provider

1. Go to "Suspended" tab
2. Click "Reactivate" on a suspended provider
3. Verify provider moves back to "Approved" tab

## Step 8: Test Navigation & Layout

### 8.1 Sidebar Navigation

Test all menu items:
- [ ] Dashboard
- [ ] Users
- [ ] Providers
- [ ] Services (placeholder)
- [ ] Analytics (placeholder)
- [ ] Payments (placeholder)
- [ ] Orders (placeholder)
- [ ] System Logs (placeholder)
- [ ] Settings (placeholder)

### 8.2 Sidebar Collapse

1. Click collapse icon (≡)
2. Verify:
   - [ ] Sidebar collapses to icons only
   - [ ] Logo changes to "BA"
   - [ ] Menu text hidden

### 8.3 User Menu

1. Click user avatar/email in top-right
2. Verify dropdown appears with:
   - [ ] Profile option
   - [ ] Logout option

### 8.4 Logout

1. Click "Logout" from user menu
2. Verify:
   - [ ] Redirected to login page
   - [ ] Tokens cleared from localStorage
   - [ ] Cannot access protected pages without login

## Step 9: Test Authentication Guards

### 9.1 Protected Routes

1. Logout if logged in
2. Try accessing: `http://localhost:5173/dashboard`
3. Verify:
   - [ ] Redirected to login page
   - [ ] URL becomes `/login?redirect=/dashboard`

### 9.2 Login Redirect

1. From above step, login successfully
2. Verify:
   - [ ] Redirected to original URL (`/dashboard`)

### 9.3 Login Page Access When Authenticated

1. While logged in, try accessing `/login`
2. Verify:
   - [ ] Redirected to Dashboard
   - [ ] Cannot access login page when authenticated

## Step 10: Test Error Handling

### 10.1 Invalid Credentials

1. Try logging in with wrong password
2. Verify:
   - [ ] "Invalid credentials" error message
   - [ ] Stays on login page
   - [ ] No token stored

### 10.2 Network Error Simulation

1. Stop backend services
2. Try any API operation (login, load users, etc.)
3. Verify:
   - [ ] "Network error" message appears
   - [ ] UI doesn't crash

### 10.3 Session Expiry

1. Login successfully
2. Manually delete `admin_token` from localStorage:
   ```javascript
   localStorage.removeItem('admin_token')
   ```
3. Try accessing any protected page
4. Verify:
   - [ ] Redirected to login page
   - [ ] Prompted to login again

## Step 11: Verify API Requests

Open Browser DevTools → Network tab:

### 11.1 Login Request
```
POST http://localhost:5000/api/v1/auth/login
Status: 200 OK
Response: { accessToken, refreshToken, userInfo, ... }
```

### 11.2 Get Users Request
```
GET http://localhost:5000/api/v1/Users?pageNumber=1&pageSize=10
Status: 200 OK
Headers: Authorization: Bearer eyJ...
```

### 11.3 CORS Headers
Verify response headers include:
```
Access-Control-Allow-Origin: http://localhost:5173
Access-Control-Allow-Credentials: true
```

## Troubleshooting Common Issues

### Issue: Cannot login

**Check:**
1. Backend is running: `curl http://localhost:5000/health`
2. Admin user exists in database
3. Password is correct
4. CORS is configured properly

**Debug:**
```bash
# Check Gateway logs
docker logs booksy-gateway

# Check UserManagement API logs
docker logs booksy-usermanagement-api
```

### Issue: Users/Providers don't load

**Check:**
1. API endpoints exist in backend
2. JWT token is being sent in requests (Network tab)
3. User has Admin role
4. Database has data

### Issue: CORS errors

**Solution:**
Add to backend CORS configuration:
```csharp
policy.WithOrigins("http://localhost:5173")
```

### Issue: Build fails

**Solution:**
```bash
cd booksy-admin
rm -rf node_modules package-lock.json
npm install
npm run build
```

## Performance Testing

### Load Time
- First load: Should be < 2 seconds
- Subsequent loads: Should be < 500ms (with caching)

### Bundle Size
Check build output:
```bash
npm run build
# Check dist/ folder size
```

Should be around:
- Total: ~2MB (uncompressed)
- Gzipped: ~500KB

## Browser Compatibility

Test in:
- [ ] Chrome (latest)
- [ ] Firefox (latest)
- [ ] Safari (latest)
- [ ] Edge (latest)

## Mobile Responsive

Test on mobile viewport:
1. Open DevTools → Toggle device toolbar
2. Select mobile device (e.g., iPhone 12)
3. Verify:
   - [ ] Layout adapts to mobile
   - [ ] Sidebar works on mobile
   - [ ] Tables are scrollable
   - [ ] Forms are usable

## Test Checklist Summary

- [ ] Backend services running
- [ ] Admin user created
- [ ] Login successful
- [ ] Dashboard loads with data
- [ ] User management CRUD works
- [ ] Provider approval workflow works
- [ ] Navigation functional
- [ ] Logout works
- [ ] Authentication guards working
- [ ] Error handling appropriate
- [ ] API requests correct
- [ ] CORS configured
- [ ] Mobile responsive
- [ ] Browser compatible

## Next Steps After Testing

1. **Production Deployment**: Deploy to production server
2. **Create More Admin Users**: Add team members
3. **Implement Missing Modules**: Analytics, Payments, Logs
4. **Customize Branding**: Update colors, logo, etc.
5. **Add Real Data**: Connect to live database
6. **Monitor Performance**: Set up monitoring/logging
7. **Security Audit**: Review security settings
8. **User Training**: Train team on using the admin panel

## Support

If you encounter issues during testing:

1. Check browser console for errors
2. Check Network tab for failed requests
3. Check backend logs
4. Review [API_INTEGRATION.md](API_INTEGRATION.md)
5. Verify environment configuration

Happy testing! 🎉
