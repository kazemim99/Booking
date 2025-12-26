# Booksy Deployment Fixes Summary

**Date**: December 26, 2025

## ✅ Issues Fixed (Deployment Repository)

### 1. 502 Bad Gateway Error - FIXED ✅

**Problem**: Nginx was configured to proxy requests to `http://booksy-gateway:8080/` but the gateway was listening on port **80**.

**Solution**:
- Created custom nginx configuration at `/root/booksy/nginx/default.conf` with correct port (80)
- Updated `docker-compose.prod.yml` to mount the nginx config into the frontend container
- Restarted frontend container

**Files Modified**:
- `/root/booksy/nginx/default.conf` (created)
- `/root/booksy/docker-compose.prod.yml` (added volume mount)

**Verification**:
```bash
curl -v http://napstar.ir/api/v1/Platform/statistics
# Returns 404 instead of 502 (connection successful, but route not found in gateway)
```

### 2. Gateway Ocelot Configuration - PREPARED ✅

**Problem**: Gateway had empty Ocelot configuration files (`ocelot.production.json` was `{}`), causing all routes to return 404.

**Solution**:
- Created comprehensive Ocelot configuration at `/root/booksy/gateway/ocelot.production.json`
- Configured route mappings for all API endpoints:
  - UserManagement API: `/api/v1/Auth/*`, `/api/v1/Customers/*`, `/api/v1/Users/*`
  - ServiceCatalog API: All other `/api/v1/*` routes
- Updated `docker-compose.prod.yml` to mount Ocelot config into gateway container

**Files Created**:
- `/root/booksy/gateway/ocelot.production.json` (7054 bytes, comprehensive route config)

**Files Modified**:
- `/root/booksy/docker-compose.prod.yml` (added volume mount for gateway)

---

## ⚠️ Issues Requiring Source Code Changes

### 3. Gateway Not Loading Ocelot Configuration

**Problem**: The gateway Docker image doesn't load the Ocelot configuration, even though the config file is mounted correctly. No Ocelot logs appear during startup.

**Root Cause**: The gateway source code is not properly configured to:
1. Add Ocelot as a dependency
2. Load Ocelot configuration files
3. Use Ocelot middleware in the request pipeline

**Required Source Code Changes** (in your Booksy Gateway repository):

#### Step 1: Install Ocelot NuGet Package
```bash
dotnet add package Ocelot
```

#### Step 2: Update `Program.cs` or `Startup.cs`

**For Program.cs (minimal API / .NET 6+)**:
```csharp
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add Ocelot configuration
builder.Configuration.AddJsonFile($"Configuration/ocelot.{builder.Environment.EnvironmentName}.json", optional: false, reloadOnChange: true);

// Add services
builder.Services.AddOcelot();

var app = builder.Build();

// Use Ocelot middleware
await app.UseOcelot();

app.Run();
```

**For Startup.cs (traditional .NET)**:
```csharp
public class Startup
{
    public Startup(IConfiguration configuration, IWebHostEnvironment env)
    {
        Configuration = new ConfigurationBuilder()
            .SetBasePath(env.ContentRootPath)
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
            .AddJsonFile($"Configuration/ocelot.{env.EnvironmentName}.json", optional: false, reloadOnChange: true)
            .Build();
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddOcelot(Configuration);
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseOcelot().Wait();
    }
}
```

#### Step 3: Rebuild and Push Docker Image
```bash
# In your source repository
docker build -t ghcr.io/kazemim99/booksy-gateway:latest .
docker push ghcr.io/kazemim99/booksy-gateway:latest
```

#### Step 4: Deploy Updated Image
```bash
# On the deployment server
cd /root/booksy
./scripts/deploy.sh
```

### 4. Database Migrations Not Applied

**Problem**: The database is completely empty. No tables or schemas exist for either UserManagement or ServiceCatalog services.

**Error Example**:
```
relation "ServiceCatalog.Providers" does not exist
```

**Root Cause**: Entity Framework migrations have never been applied to the production database.

**Required Actions** (in your source repositories):

#### Option A: Run Migrations from Development Machine

**For UserManagement API**:
```bash
# In UserManagement source repository
cd Booksy.UserManagement.API
dotnet ef database update --connection "Host=5.223.59.167;Port=5432;Database=booksy_user_management;Username=booksy_admin;Password=YourSecurePassword123!"
```

**For ServiceCatalog API**:
```bash
# In ServiceCatalog source repository
cd Booksy.ServiceCatalog.API
dotnet ef database update --connection "Host=5.223.59.167;Port=5432;Database=booksy_user_management;Username=booksy_admin;Password=YourSecurePassword123!"
```

#### Option B: Generate SQL Scripts and Apply Manually

**Generate migration scripts**:
```bash
# UserManagement
dotnet ef migrations script --output migrations_usermanagement.sql

# ServiceCatalog
dotnet ef migrations script --output migrations_servicecatalog.sql
```

**Apply scripts on server**:
```bash
# Copy scripts to server, then:
docker exec -i booksy-postgres psql -U booksy_admin -d booksy_user_management < migrations_usermanagement.sql
docker exec -i booksy-postgres psql -U booksy_admin -d booksy_user_management < migrations_servicecatalog.sql
```

#### Option C: Enable Auto-Migration on Startup (Not Recommended for Production)

Add this to your API startup code:
```csharp
// In Program.cs or Startup.cs
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}
```

**Note**: Auto-migration is NOT recommended for production environments.

---

## 📝 Current Status

| Issue | Status | Notes |
|-------|--------|-------|
| 502 Bad Gateway | ✅ Fixed | Nginx port configuration corrected |
| Gateway Routing | ⚠️ Partial | Config created but needs source code changes to load |
| Database Tables | ❌ Not Fixed | Migrations need to be run from source repository |

---

## 🔄 Next Steps

1. **Update Gateway Source Code**:
   - Add Ocelot package
   - Configure Ocelot in Program.cs/Startup.cs
   - Rebuild and push Docker image
   - Deploy updated image

2. **Run Database Migrations**:
   - Choose one of the options above (A, B, or C)
   - Verify tables are created successfully
   - Test API endpoints

3. **Verify Everything Works**:
   ```bash
   # Test gateway routing
   curl http://napstar.ir/api/v1/Platform/statistics

   # Test provider search
   curl "http://napstar.ir/api/v1/Providers/search?PageNumber=1&PageSize=6"

   # Test categories
   curl http://napstar.ir/api/v1/Categories/popular
   ```

---

## 📂 Files Created/Modified in Deployment Repository

**Created**:
- `/root/booksy/nginx/default.conf` - Custom nginx config with correct gateway port
- `/root/booksy/gateway/ocelot.production.json` - Complete Ocelot route configuration
- `/root/booksy/FIXES_SUMMARY.md` - This file

**Modified**:
- `/root/booksy/docker-compose.prod.yml` - Added volume mounts for frontend nginx config and gateway Ocelot config

---

## 🛠️ Testing Commands

Once all fixes are applied:

```bash
# Test frontend is healthy
docker ps | grep frontend

# Test gateway is running
docker ps | grep gateway

# Test nginx routing to gateway
docker exec booksy-frontend curl -v http://gateway:80/api/v1/Categories/popular

# Test external access
curl -v http://napstar.ir/api/v1/Categories/popular

# Check database tables exist
docker exec booksy-postgres psql -U booksy_admin -d booksy_user_management -c "\dt"

# View service logs
docker logs booksy-gateway --tail 50
docker logs booksy-servicecatalog-api --tail 50
docker logs booksy-usermanagement-api --tail 50
```

---

## 📞 Support

If you encounter issues:
1. Check logs: `docker logs <container-name>`
2. Verify health: `docker ps`
3. Test connectivity: `docker exec <container> curl http://<service>:<port>/health`
4. Review this document for required source code changes
