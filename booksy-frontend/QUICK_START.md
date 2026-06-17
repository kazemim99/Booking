# Quick Start - Frontend with Gateway

## Start Everything (1 Command)

**Windows:**
```powershell
cd c:\Repos\Booking
.\start-dev.ps1
```

**Linux/Mac:**
```bash
cd /path/to/Booking
./start-dev.sh
```

## Start Manually (4 Terminals)

```bash
# Terminal 1
cd src/UserManagement/Booksy.UserManagement.API
dotnet run

# Terminal 2
cd src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api
dotnet run

# Terminal 3
cd src/APIGateway/Booksy.Gateway
dotnet run

# Terminal 4
cd booksy-frontend
npm run dev
```

## Access Points

| Service | URL |
|---------|-----|
| Frontend | http://localhost:3000 |
| Gateway | http://localhost:5000 |
| UserManagement | http://localhost:5001 |
| ServiceCatalog | http://localhost:5002 |

## Test It Works

```bash
# Test via frontend proxy
curl http://localhost:3000/api/v1/Platform/statistics

# Test via gateway directly
curl http://localhost:5000/api/v1/Platform/statistics
```

## What Changed?

1. **Frontend now uses Gateway** (port 5000) instead of direct service calls
2. **Vite proxy** forwards `/api` requests to Gateway
3. **CORS configured** in Gateway for development
4. **Consistent with production** - same routing in dev and prod

## Logs to Check

**Frontend terminal:**
```
Sending Request to Gateway: GET /api/v1/...
Received from Gateway: 200 /api/v1/...
```

**Gateway terminal:**
```
Loading Ocelot configuration from: Configuration/ocelot.development.json
Starting Ocelot API Gateway...
```

## Common Issues

| Problem | Solution |
|---------|----------|
| 502 Bad Gateway | Start backend services (5001, 5002) |
| Proxy error | Start Gateway (5000) |
| Port in use | Kill process: `taskkill /PID <pid> /F` |

## More Details

See [DEV_SETUP.md](./DEV_SETUP.md) for complete documentation.
