# Visual Studio Debugging Guide

## Running Booksy with Gateway in Visual Studio

This guide shows how to debug all services from Visual Studio with the Gateway configured.

## ✅ Port Configuration Summary

All services are now configured with the correct ports:

| Service | Port | Launch Profile | File |
|---------|------|----------------|------|
| UserManagement API | 5001 | http | `src/UserManagement/Booksy.UserManagement.API/Properties/launchSettings.json` |
| ServiceCatalog API | 5002 | http | `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/Properties/launchSettings.json` |
| Gateway | 5000 | http | `src/APIGateway/Booksy.Gateway/Properties/launchSettings.json` |
| Frontend | 3000 | npm dev | `booksy-frontend/` |

## 🎯 Request Flow

```
Frontend (localhost:3000)
    ↓ /api/v1/Providers/search
Vite Proxy (configured in vite.config.ts)
    ↓ http://localhost:5000/api/v1/Providers/search
Gateway (localhost:5000)
    ↓ Ocelot routes based on path
    ├─→ UserManagement API (localhost:5001) - /Auth, /Users, /Customers
    └─→ ServiceCatalog API (localhost:5002) - /Providers, /Services, /Platform, etc.
```

## 🚀 How to Run in Visual Studio

### Method 1: Run Each Service Separately (Recommended for Debugging)

1. **Start UserManagement API**
   - Right-click `Booksy.UserManagement.API` project
   - Click "Debug" → "Start New Instance"
   - Select profile: **http** (not https)
   - Verify it starts on `http://localhost:5001`

2. **Start ServiceCatalog API**
   - Right-click `Booksy.ServiceCatalog.Api` project
   - Click "Debug" → "Start New Instance"
   - Select profile: **http**
   - Verify it starts on `http://localhost:5002`

3. **Start Gateway**
   - Right-click `Booksy.Gateway` project
   - Click "Debug" → "Start New Instance"
   - Select profile: **http**
   - Verify it starts on `http://localhost:5000`
   - Check Output window for: `Starting Ocelot API Gateway...`

4. **Start Frontend** (in terminal)
   ```bash
   cd booksy-frontend
   npm run dev
   # Should start on http://localhost:3000
   ```

### Method 2: Configure Multiple Startup Projects

1. Right-click on the Solution
2. Select "Configure Startup Projects"
3. Choose "Multiple startup projects"
4. Set the following to **Start**:
   - `Booksy.UserManagement.API`
   - `Booksy.ServiceCatalog.Api`
   - `Booksy.Gateway`
5. Click OK
6. Press F5 to start all three services at once

**Note**: You still need to start the frontend separately in a terminal.

## 🔍 Debugging Tips

### Setting Breakpoints

1. **Gateway Routing**: Set breakpoints in Gateway middleware to see routing decisions
2. **API Controllers**: Set breakpoints in UserManagement or ServiceCatalog controllers
3. **Frontend**: Use browser DevTools for frontend debugging

### Viewing Logs

- **Gateway Logs**: Check Visual Studio Output window for Ocelot routing
- **API Logs**: Each API service has its own Output window
- **Frontend Logs**: Check browser Console and Vite terminal output

### Common Issues

#### Port Already in Use
**Error**: `Failed to bind to address http://127.0.0.1:5000: address already in use`

**Solution**:
```powershell
# Find process using the port
netstat -ano | findstr :5000

# Kill the process (replace PID with actual process ID)
taskkill //F //PID <PID>
```

#### Gateway Returns 502 Bad Gateway
**Cause**: Backend services (5001, 5002) are not running.

**Solution**: Ensure UserManagement API and ServiceCatalog API are both running before starting the Gateway.

#### Frontend Shows CORS Errors
**Cause**: Gateway CORS not configured or not running.

**Solution**: The Gateway is already configured with CORS for `localhost:3000`. Ensure Gateway is running.

## ✅ Verification Steps

### 1. Check All Services Are Running

```powershell
# Check ports
netstat -ano | findstr "500[0-2]"

# You should see:
# 5000 - Gateway
# 5001 - UserManagement API
# 5002 - ServiceCatalog API
```

### 2. Test Direct API Calls

```bash
# Test ServiceCatalog directly
curl http://localhost:5002/api/v1/Platform/statistics

# Test UserManagement directly
curl http://localhost:5001/health

# Test Gateway routing
curl http://localhost:5000/api/v1/Platform/statistics

# Test Frontend proxy
curl http://localhost:3000/api/v1/Platform/statistics
```

### 3. Check Browser DevTools

1. Open `http://localhost:3000` in browser
2. Open DevTools → Network tab
3. Navigate to a page that calls APIs
4. Check requests are going to `/api/v1/...` (proxied)
5. Check status codes are 200 OK

### 4. Check Vite Console

In the frontend terminal, you should see proxy logs:
```
Sending Request to Gateway: GET /api/v1/Platform/statistics
Received from Gateway: 200 /api/v1/Platform/statistics
```

## 📋 Gateway Configuration Files

### Ocelot Development Configuration
**File**: `src/APIGateway/Booksy.Gateway/Configuration/ocelot.development.json`

Routes all `/api/v1/*` requests to appropriate backend services:
- `/api/v1/Auth/*` → UserManagement (localhost:5001)
- `/api/v1/Users/*` → UserManagement (localhost:5001)
- `/api/v1/Customers/*` → UserManagement (localhost:5001)
- `/api/v1/Providers/*` → ServiceCatalog (localhost:5002)
- `/api/v1/Services/*` → ServiceCatalog (localhost:5002)
- `/api/v1/Platform/*` → ServiceCatalog (localhost:5002)
- ... and more

### Frontend Proxy Configuration
**File**: `booksy-frontend/vite.config.ts`

Proxies all `/api` requests from frontend to Gateway:
```typescript
server: {
  proxy: {
    '/api': {
      target: 'http://localhost:5000',
      changeOrigin: true,
      secure: false
    }
  }
}
```

## 🎯 Testing the Complete Flow

1. **Open Frontend**: `http://localhost:3000`
2. **Trigger an API call** (e.g., search for providers)
3. **Watch the flow**:
   - Frontend makes request to `/api/v1/...`
   - Vite proxy forwards to Gateway at `localhost:5000`
   - Gateway routes to appropriate service (5001 or 5002)
   - Service processes request and returns response
   - Response flows back through Gateway to frontend

4. **Debug at any layer**:
   - Set breakpoint in frontend code
   - Set breakpoint in Gateway
   - Set breakpoint in backend API controller

## 📚 Related Files

- Frontend configuration: `booksy-frontend/.env.development`
- Vite proxy: `booksy-frontend/vite.config.ts`
- Gateway config: `src/APIGateway/Booksy.Gateway/Configuration/ocelot.development.json`
- Gateway Program: `src/APIGateway/Booksy.Gateway/Program.cs`

## 🔧 Modified Configuration Summary

All port configurations have been updated to work together:

✅ **UserManagement API**: Port 5020 → **5001**
✅ **ServiceCatalog API**: Port 8080 → **5002**
✅ **Gateway**: Port 5206 → **5000**
✅ **Frontend**: Uses relative `/api` paths (proxied to 5000)
✅ **Gateway CORS**: Configured for `localhost:3000`

Everything is now configured for seamless development and debugging!
