# API Integration Guide

## Backend Connection Status

✅ **Admin panel is now connected to your Booksy backend API**

## API Configuration

### Environment Files

#### Development ([.env.development](.env.development))
```env
VITE_API_BASE_URL=http://localhost:5000/api/v1
```

#### Production ([.env.production](.env.production))
```env
VITE_API_BASE_URL=http://napstar.ir/api/v1
```

## Authentication Flow

### 1. Login Endpoint
- **URL**: `POST /api/v1/auth/login`
- **Request**:
```json
{
  "email": "admin@booksy.com",
  "password": "your-password",
  "rememberMe": false
}
```
- **Response**:
```json
{
  "accessToken": "eyJ...",
  "refreshToken": "refresh-token-here",
  "expiresIn": 3600,
  "tokenType": "Bearer",
  "userInfo": {
    "id": "user-id",
    "email": "admin@booksy.com",
    "displayName": "Admin User",
    "roles": ["Admin"]
  }
}
```

### 2. Token Storage
- Access token stored in: `localStorage.admin_token`
- Refresh token stored in: `localStorage.admin_refresh_token`

### 3. Authenticated Requests
All API requests automatically include:
```
Authorization: Bearer <access-token>
```

### 4. Token Refresh
- **URL**: `POST /api/v1/auth/refresh`
- **Request**:
```json
{
  "refreshToken": "your-refresh-token"
}
```

### 5. Logout
- **URL**: `POST /api/v1/auth/logout`
- Clears all tokens from localStorage

## API Endpoints Mapping

### Authentication
| Admin Panel | Backend Endpoint |
|------------|------------------|
| Login | `POST /api/v1/auth/login` |
| Logout | `POST /api/v1/auth/logout` |
| Refresh Token | `POST /api/v1/auth/refresh` |
| Get Current User | `GET /api/v1/Users/me` |

### User Management
| Admin Panel | Backend Endpoint |
|------------|------------------|
| Get Users | `GET /api/v1/Users?pageNumber=1&pageSize=10` |
| Get User | `GET /api/v1/Users/{id}` |
| Create User | `POST /api/v1/Users` |
| Update User | `PUT /api/v1/Users/{id}` |
| Delete User | `DELETE /api/v1/Users/{id}` |

### Provider Management
| Admin Panel | Backend Endpoint |
|------------|------------------|
| Get Providers | `GET /api/v1/Providers?status=Pending` |
| Get Provider | `GET /api/v1/Providers/{id}` |
| Approve Provider | `POST /api/v1/Providers/{id}/approve` |
| Reject Provider | `POST /api/v1/Providers/{id}/reject` |
| Suspend Provider | `POST /api/v1/Providers/{id}/suspend` |

## Testing the Connection

### 1. Start Backend Services

```bash
# Start Gateway (port 5000)
cd src/APIGateway/Booksy.Gateway
dotnet run

# Start UserManagement API (port 5001)
cd src/UserManagement/Booksy.UserManagement.API
dotnet run

# Start ServiceCatalog API (port 5002)
cd src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api
dotnet run
```

### 2. Start Admin Panel

```bash
cd booksy-admin
npm run dev
```

### 3. Test Login

1. Open [http://localhost:5173](http://localhost:5173)
2. You should see the login page
3. Enter admin credentials (create an admin user in your backend first)
4. Click "Login"
5. If successful, you'll be redirected to the dashboard

### 4. Create Test Admin User

You'll need to create an admin user in your backend database. Here's a sample SQL:

```sql
-- This is a reference - adjust according to your actual database schema
INSERT INTO "Users" ("Id", "Email", "PasswordHash", "Role", "IsActive", "CreatedAt")
VALUES (
  gen_random_uuid(),
  'admin@booksy.com',
  -- Use your password hashing logic
  'hashed-password-here',
  'Admin',
  true,
  NOW()
);
```

Or use your backend's registration endpoint with an admin role.

## CORS Configuration

Ensure your backend allows requests from the admin panel:

### In ASP.NET Core (Program.cs or Startup.cs):

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AdminPanel", policy =>
    {
        policy.WithOrigins(
            "http://localhost:5173",  // Development
            "http://localhost:3000",  // Docker
            "http://napstar.ir"       // Production
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

app.UseCors("AdminPanel");
```

## Troubleshooting

### Issue: CORS Error
**Solution**: Add admin panel URL to backend CORS policy (see above)

### Issue: 401 Unauthorized
**Solution**:
- Check that admin user exists with correct password
- Verify JWT configuration in backend matches
- Check token is being sent in Authorization header

### Issue: Network Error
**Solution**:
- Verify backend is running on port 5000
- Check firewall settings
- Ensure API Gateway is properly routing requests

### Issue: 404 Not Found
**Solution**:
- Verify endpoint paths match between frontend and backend
- Check Ocelot routing configuration
- Ensure all route paths use correct casing (PascalCase)

## Admin Panel Features vs Backend Endpoints

### ✅ Ready to Use (Endpoints Exist)
- Authentication (Login/Logout)
- User Management (if Users endpoints exist)
- Basic Provider Management

### ⚠️ Requires Backend Implementation
- Analytics Dashboard (`/api/v1/analytics/dashboard`)
- Payment Management
- Order/Booking Management
- System Logs API
- Settings Management

## Next Steps

1. **Create Admin User**: Set up at least one admin user in your backend
2. **Test Login**: Verify authentication works end-to-end
3. **Implement Missing Endpoints**: Add backend endpoints for analytics, payments, etc.
4. **Configure CORS**: Ensure frontend can communicate with backend
5. **Set Up Production**: Deploy both admin panel and backend to production

## Security Notes

- Never commit `.env` files with production credentials
- Use HTTPS in production
- Implement proper JWT token expiration
- Add refresh token rotation for enhanced security
- Consider implementing rate limiting for authentication endpoints
- Use secure password hashing (bcrypt, Argon2)

## Support

For backend-related issues, check:
- Backend API logs
- Ocelot Gateway logs
- Database connection
- JWT token configuration

For frontend issues, check:
- Browser console for errors
- Network tab in DevTools
- Admin panel logs (Console)
