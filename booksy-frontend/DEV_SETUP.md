# Development Setup Guide

## Frontend Gateway Configuration

The frontend is now configured to use the API Gateway in development mode, ensuring consistent behavior between development and production environments.

## Architecture Flow

```
Frontend Dev Server (localhost:3000)
    ↓ GET /api/v1/Users/profile
Vite Proxy
    ↓ http://localhost:5000/api/v1/Users/profile
API Gateway (localhost:5000)
    ↓ http://localhost:5001/api/v1/Users/profile
UserManagement API (localhost:5001)
```

## Starting the Development Environment

### Prerequisites
- .NET 9.0 SDK installed
- Node.js 18+ and npm/pnpm installed
- All backend services cloned and built

### Step 1: Start Backend Services

Open 3 separate terminals:

**Terminal 1 - UserManagement API:**
```bash
cd src/UserManagement/Booksy.UserManagement.API
dotnet run
# Should start on http://localhost:5001
```

**Terminal 2 - ServiceCatalog API:**
```bash
cd src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api
dotnet run
# Should start on http://localhost:5002
```

**Terminal 3 - API Gateway:**
```bash
cd src/APIGateway/Booksy.Gateway
dotnet run
# Should start on http://localhost:5000
```

### Step 2: Start Frontend

**Terminal 4 - Frontend:**
```bash
cd booksy-frontend
npm install  # or pnpm install (first time only)
npm run dev  # or pnpm dev
# Should start on http://localhost:3000
```

## Verifying Gateway Integration

### 1. Check Console Logs
When you make API calls from the frontend, you should see in the Vite terminal:
```
Sending Request to Gateway: GET /api/v1/Platform/statistics
Received from Gateway: 200 /api/v1/Platform/statistics
```

### 2. Check Browser Network Tab
- Open DevTools → Network tab
- Make an API call (e.g., search for providers)
- Request URL should be: `http://localhost:3000/api/v1/...`
- Status should be 200 (or appropriate status)

### 3. Check Gateway Logs
In the Gateway terminal, you should see Ocelot routing logs showing requests being forwarded to backend services.

### 4. Test Endpoints
```bash
# Test via Gateway (should work)
curl http://localhost:5000/api/v1/Platform/statistics

# Test via frontend (should proxy to Gateway)
curl http://localhost:3000/api/v1/Platform/statistics
```

## Configuration Files

### Vite Proxy Configuration
File: `vite.config.ts`
- Proxies all `/api` requests to Gateway at `http://localhost:5000`
- Includes logging for debugging proxy requests

### Environment Variables
File: `.env.development`
```env
VITE_SERVICE_CATALOG_API_URL=/api
VITE_USER_MANAGEMENT_API_URL=/api
```
- Uses relative paths
- Vite proxy handles forwarding to Gateway

### Gateway Configuration
File: `src/APIGateway/Booksy.Gateway/Configuration/ocelot.development.json`
- Routes requests to backend services on localhost:5001 and localhost:5002
- Supports all HTTP methods and case-insensitive routing

## Troubleshooting

### Issue: 502 Bad Gateway
**Cause:** Backend services (5001, 5002) are not running or unhealthy.
**Solution:**
```bash
# Check if services are running
curl http://localhost:5001/health
curl http://localhost:5002/health

# Restart backend services
```

### Issue: Proxy Error in Vite
**Cause:** Gateway is not running on port 5000.
**Solution:**
```bash
# Check if Gateway is running
curl http://localhost:5000/api/v1/Platform/statistics

# Start Gateway
cd src/APIGateway/Booksy.Gateway
dotnet run
```

### Issue: CORS Errors
**Cause:** Gateway doesn't have CORS configured for localhost:3000.
**Solution:** Add CORS policy to Gateway's `Program.cs`:
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Then use it before UseOcelot():
app.UseCors("DevPolicy");
```

### Issue: Port Already in Use
**Cause:** Another process is using the required ports.
**Solution:**
```bash
# Windows: Find process using port
netstat -ano | findstr :5000
taskkill /PID <PID> /F

# Linux/Mac: Find and kill process
lsof -ti:5000 | xargs kill -9
```

## Benefits of This Configuration

1. **Consistent Routing:** Same routing logic in dev and production
2. **Easy Debugging:** Console logs show all proxy requests
3. **No CORS Issues:** Vite proxy handles CORS automatically
4. **Production-like:** Tests the Gateway locally before deployment
5. **Single Entry Point:** All API calls go through Gateway

## Next Steps

- Add health check endpoint to Gateway
- Configure CORS for development
- Set up hot reload for Gateway configuration
- Add request/response logging middleware
