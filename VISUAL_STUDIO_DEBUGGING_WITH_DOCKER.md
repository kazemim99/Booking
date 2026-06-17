# Visual Studio Debugging with Docker Infrastructure

This guide explains how to debug backend services in Visual Studio while running infrastructure (PostgreSQL, Redis, RabbitMQ, Seq) in Docker containers.

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Visual Studio (Debug Mode)                │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐         │
│  │ UserMgmt    │  │ ServiceCat  │  │   Gateway   │         │
│  │ API:5001    │  │ API:5002    │  │   :5000     │         │
│  │ [F5 Debug]  │  │ [F5 Debug]  │  │ [F5 Debug]  │         │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘         │
│         │                 │                 │                │
└─────────┼─────────────────┼─────────────────┼────────────────┘
          │                 │                 │
          ▼                 ▼                 ▼
┌─────────────────────────────────────────────────────────────┐
│              Docker Infrastructure Services                  │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐         │
│  │ PostgreSQL  │  │   Redis     │  │  RabbitMQ   │         │
│  │  :54321     │  │   :6379     │  │   :5672     │         │
│  └─────────────┘  └─────────────┘  └─────────────┘         │
│  ┌─────────────┐  ┌─────────────┐                          │
│  │    Seq      │  │  pgAdmin    │                          │
│  │  :5341      │  │   :5050     │                          │
│  └─────────────┘  └─────────────┘                          │
└─────────────────────────────────────────────────────────────┘
```

## Benefits of This Approach

✅ **Full debugging capabilities** - Set breakpoints, inspect variables, step through code
✅ **Fast iteration** - No need to rebuild Docker images
✅ **Hot reload** - Visual Studio auto-recompiles on code changes
✅ **Production-like infrastructure** - Same PostgreSQL, Redis, RabbitMQ as production
✅ **Isolated services** - Infrastructure persists across debugging sessions
✅ **Easy database inspection** - Use pgAdmin at http://localhost:5050

## Setup Instructions

### Step 1: Start Infrastructure Services

Run the infrastructure services in Docker:

```powershell
# PowerShell
cd C:\Repos\Booking
.\run-infrastructure.ps1
```

This starts:
- **PostgreSQL** on port 54321
- **Redis** on port 6379
- **RabbitMQ** on ports 5672 (AMQP) and 15672 (Management UI)
- **Seq** on ports 5341 (UI) and 5342 (ingestion)
- **pgAdmin** on port 5050

### Step 2: Configure Visual Studio

1. **Open** `Booksy.sln` in Visual Studio

2. **Configure Multiple Startup Projects**:
   - Right-click Solution → "Configure Startup Projects"
   - Select "Multiple startup projects"
   - Set to **Start**:
     - `Booksy.UserManagement.API`
     - `Booksy.ServiceCatalog.Api`
     - `Booksy.Gateway`
   - Click OK

3. **Select HTTP Profile** (Important!):
   - In Visual Studio toolbar, change profile dropdown to **"http"** (not https or Docker)
   - This ensures services run on ports 5000-5002 as configured

### Step 3: Debug Backend Services

Press **F5** or click "Start Debugging"

Visual Studio will:
- Start all three backend services
- Each service runs on localhost (ports 5000-5002)
- You can set breakpoints and debug normally

### Step 4: Run Flutter App

Update Flutter app to connect to localhost:

```dart
// For Android Emulator
static const String baseUrl = 'http://10.0.2.2:5000';

// For Physical Device (use your PC's IP)
static const String baseUrl = 'http://192.168.1.x:5000';
```

Then run:
```bash
cd C:\Repos\Booking\booksy-customer-app
flutter run
```

## Port Configuration

All ports are exposed to `localhost` for Visual Studio access:

| Service | Host Port | Container Port | URL |
|---------|-----------|----------------|-----|
| **Visual Studio Services** ||||
| Gateway | 5000 | - | http://localhost:5000 |
| UserManagement API | 5001 | - | http://localhost:5001 |
| ServiceCatalog API | 5002 | - | http://localhost:5002 |
| **Docker Services** ||||
| PostgreSQL | 54321 | 5432 | localhost:54321 |
| Redis | 6379 | 6379 | localhost:6379 |
| RabbitMQ AMQP | 5672 | 5672 | localhost:5672 |
| RabbitMQ Management | 15672 | 15672 | http://localhost:15672 |
| Seq UI | 5341 | 80 | http://localhost:5341 |
| Seq Ingestion | 5342 | 5341 | localhost:5342 |
| pgAdmin | 5050 | 80 | http://localhost:5050 |

## Connection Strings (Already Configured)

Backend services in `appsettings.Development.json` are already configured to connect to Docker infrastructure:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=54321;Database=booksy_service_catalog_dev;Username=booksy_admin;Password=Booksy@2024!",
    "Redis": "localhost:6379,password=Redis@2024!,abortConnect=false",
    "RabbitMQ": "amqp://booksy_admin:Booksy@2024!@localhost:5672"
  }
}
```

## Debugging Workflow

### Setting Breakpoints

1. Open any controller file (e.g., `ProvidersController.cs`)
2. Click in the left margin to set a breakpoint (red dot)
3. Make a request from Flutter app
4. Visual Studio will pause at your breakpoint
5. Inspect variables, step through code, etc.

### Viewing Logs

**Option 1: Visual Studio Output Window**
- View → Output
- Select "Show output from: Debug" or service name

**Option 2: Seq (Recommended)**
- Open http://localhost:5341
- View structured logs from all services
- Filter by level, service, timestamp, etc.

### Database Inspection

**Option 1: pgAdmin**
- Open http://localhost:5050
- Login: `admin@booksy.local` / `Booksy@2024!`
- Add server:
  - Host: `postgres` (container name) or `localhost`
  - Port: `54321` (if using localhost) or `5432` (if using container name)
  - Database: `booksy_user_management` or `booksy_service_catalog_dev`
  - User: `booksy_admin`
  - Password: `Booksy@2024!`

**Option 2: Visual Studio SQL Server Object Explorer**
- View → SQL Server Object Explorer
- Add Connection → PostgreSQL (via Npgsql)

### Monitoring RabbitMQ

- Open http://localhost:15672
- Login: `booksy_admin` / `Booksy@2024!`
- View queues, exchanges, messages

## Common Debugging Scenarios

### Debugging API Requests

1. Set breakpoint in controller action:
   ```csharp
   [HttpGet("search")]
   public async Task<ActionResult> SearchProviders(...)
   {
       // Breakpoint here
       var query = request.ToQuery();
   ```

2. Make request from Flutter app
3. Visual Studio pauses at breakpoint
4. Inspect `request` parameter, step through code

### Debugging Database Queries

1. Set breakpoint in repository or handler
2. Watch variables in Locals/Autos window
3. Check SQL in Output window (if EF logging enabled)

### Debugging Redis Cache

1. Set breakpoint in cache service
2. Use Redis CLI to verify:
   ```bash
   docker exec -it booksy-redis redis-cli -a Redis@2024!
   > KEYS booksy:*
   > GET booksy:categories
   ```

### Debugging Message Queue

1. Set breakpoint in message handler
2. Check RabbitMQ Management UI for message status
3. Manually publish test message via Management UI

## Stopping Services

### Stop Visual Studio Services
- Click "Stop Debugging" in Visual Studio (Shift+F5)
- Or close Visual Studio

### Stop Infrastructure Services
```powershell
cd C:\Repos\Booking
.\stop-infrastructure.ps1
```

### Stop Everything (Including Data)
```powershell
cd C:\Repos\Booking
docker-compose -f docker-compose.infrastructure.yml down -v
```
⚠️ **Warning**: This deletes all database data!

## Troubleshooting

### Issue: Can't Connect to PostgreSQL

**Check**:
```powershell
docker ps | findstr postgres
docker logs booksy-postgres
```

**Solution**: Ensure PostgreSQL is healthy
```powershell
docker exec booksy-postgres pg_isready -U booksy_admin
```

### Issue: Can't Connect to Redis

**Check**:
```powershell
docker exec booksy-redis redis-cli -a Redis@2024! ping
```

Should return `PONG`

### Issue: Port Already in Use

**Find Process**:
```powershell
netstat -ano | findstr :5001
```

**Kill Process**:
```powershell
taskkill /F /PID <PID>
```

### Issue: Database Doesn't Exist

**Create Database**:
```powershell
docker exec -it booksy-postgres psql -U booksy_admin -c "CREATE DATABASE booksy_service_catalog_dev;"
```

**Run Migrations**:
```powershell
cd C:\Repos\Booking\src\BoundedContexts\ServiceCatalog\Booksy.ServiceCatalog.Api
dotnet ef database update
```

## Performance Tips

### Fast Startup
- Keep infrastructure running between debugging sessions
- Only restart Visual Studio services

### Reduce Build Time
- Build only projects you're debugging
- Use Solution Configurations to exclude projects

### Database Performance
- Use indexes on frequently queried columns
- Monitor slow queries in Seq

## Alternative: Full Docker Debugging

If you prefer to debug everything in Docker:

1. Use `docker-compose.prod.yml` with debug configuration
2. Attach Visual Studio debugger to running container
3. See: https://docs.microsoft.com/visualstudio/containers/

**Pros**: Exact production environment
**Cons**: Slower build/restart cycle

## Summary

This hybrid approach gives you:
- ✅ Fast debugging (Visual Studio)
- ✅ Production-like infrastructure (Docker)
- ✅ Easy database/cache inspection
- ✅ Centralized logging (Seq)

Perfect for daily development and troubleshooting!
