# Database Migrations & Gateway Setup Guide

**Status**: Routing is working! APIs return 500 errors due to missing database tables.

## 🎯 Current Status

| Component | Status | Details |
|-----------|--------|---------|
| Nginx Routing | ✅ Working | Routes directly to backend services (temporary workaround) |
| Backend APIs | ✅ Running | Services are healthy and responding |
| Database | ❌ Empty | No tables/schemas exist - migrations needed |
| Gateway | ⚠️ Bypassed | Using direct routing temporarily |

---

## Part 1: Run Database Migrations

You have **3 options** to run migrations. Choose the one that works best for your setup.

### Option A: Run from Development Machine (Recommended)

**Requirements**:
- .NET SDK installed on your machine
- Access to Booksy source code repositories
- EF Core tools: `dotnet tool install --global dotnet-ef`

**Steps**:

#### 1. Navigate to UserManagement API Project
```bash
cd /path/to/Booksy.UserManagement.API
# Or wherever your UserManagement source code is located
```

#### 2. Run UserManagement Migrations
```bash
dotnet ef database update \
  --connection "Host=5.223.59.167;Port=5432;Database=booksy_user_management;Username=booksy_admin;Password=YourSecurePassword123!"
```

Expected output:
```
Build started...
Build succeeded.
Applying migration '20231201_InitialCreate'.
Applying migration '20231202_AddUsers'.
...
Done.
```

#### 3. Navigate to ServiceCatalog API Project
```bash
cd /path/to/Booksy.ServiceCatalog.API
```

#### 4. Run ServiceCatalog Migrations
```bash
dotnet ef database update \
  --connection "Host=5.223.59.167;Port=5432;Database=booksy_user_management;Username=booksy_admin;Password=YourSecurePassword123!"
```

#### 5. Verify Tables Were Created
```bash
# On your deployment server:
docker exec booksy-postgres psql -U booksy_admin -d booksy_user_management -c "\dt"
```

You should see tables from both services listed.

---

### Option B: Generate and Apply SQL Scripts

If you can't connect directly from your dev machine:

#### 1. Generate SQL Scripts Locally

**UserManagement**:
```bash
cd /path/to/Booksy.UserManagement.API
dotnet ef migrations script --output ~/migrations_usermanagement.sql --idempotent
```

**ServiceCatalog**:
```bash
cd /path/to/Booksy.ServiceCatalog.API
dotnet ef migrations script --output ~/migrations_servicecatalog.sql --idempotent
```

The `--idempotent` flag makes scripts safe to run multiple times.

#### 2. Copy Scripts to Server
```bash
scp ~/migrations_usermanagement.sql root@5.223.59.167:/root/booksy/
scp ~/migrations_servicecatalog.sql root@5.223.59.167:/root/booksy/
```

#### 3. Apply Scripts on Server
```bash
# SSH to server
ssh root@5.223.59.167

# Apply UserManagement migrations
docker exec -i booksy-postgres psql -U booksy_admin -d booksy_user_management < /root/booksy/migrations_usermanagement.sql

# Apply ServiceCatalog migrations
docker exec -i booksy-postgres psql -U booksy_admin -d booksy_user_management < /root/booksy/migrations_servicecatalog.sql
```

#### 4. Clean Up
```bash
rm /root/booksy/migrations_*.sql
```

---

### Option C: Enable Auto-Migration (NOT Recommended for Production)

Only use this for testing/development environments.

**Modify your API startup code**:

In `Program.cs` (for both APIs):
```csharp
// After: var app = builder.Build();

// Add this block:
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    // Or YourDbContext - use your actual DbContext class name

    dbContext.Database.Migrate();
}

// Then: app.Run();
```

Rebuild and redeploy the Docker images.

**WARNING**: Auto-migration can cause issues in production (race conditions, downtime).

---

## Part 2: Fix Gateway to Use Ocelot

Currently, nginx routes directly to backend services. For proper API gateway functionality, you need to configure the gateway to load Ocelot.

### Gateway Source Code Changes

#### 1. Ensure Ocelot Package is Installed

In your gateway project directory:
```bash
cd /path/to/Booksy.Gateway
dotnet add package Ocelot
```

Verify in `.csproj`:
```xml
<ItemGroup>
  <PackageReference Include="Ocelot" Version="23.0.0" />
</ItemGroup>
```

#### 2. Update Program.cs

Replace your `Program.cs` with:

```csharp
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Load Ocelot configuration based on environment
builder.Configuration.AddJsonFile(
    $"Configuration/ocelot.{builder.Environment.EnvironmentName}.json",
    optional: false,
    reloadOnChange: true
);

// Add Ocelot services
builder.Services.AddOcelot(builder.Configuration);

// Optional: Add other services you need
// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerForOcelot(builder.Configuration);

var app = builder.Build();

// Use Ocelot middleware
await app.UseOcelot();

app.Run();
```

**Alternative for traditional Startup.cs pattern**:

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddJsonFile(
                    $"Configuration/ocelot.{hostingContext.HostingEnvironment.EnvironmentName}.json",
                    optional: false,
                    reloadOnChange: true
                );
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
}

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
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

#### 3. Verify Ocelot Configuration Exists

The configuration file is already created on the server at:
- `/root/booksy/gateway/ocelot.production.json`

And it's already mounted in `docker-compose.prod.yml`.

#### 4. Rebuild and Deploy Gateway

```bash
# In your gateway source repository
docker build -t ghcr.io/kazemim99/booksy-gateway:latest .
docker push ghcr.io/kazemim99/booksy-gateway:latest

# On deployment server
cd /root/booksy
docker pull ghcr.io/kazemim99/booksy-gateway:latest
docker stop booksy-gateway
docker rm booksy-gateway
docker run -d \
  --name booksy-gateway \
  --network booksy_booksy-network \
  --env-file /root/booksy/.env \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ASPNETCORE_URLS=http://+:80 \
  -p 5000:80 \
  -v /root/booksy/gateway/ocelot.production.json:/app/Configuration/ocelot.production.json:ro \
  --restart unless-stopped \
  ghcr.io/kazemim99/booksy-gateway:latest
```

#### 5. Update Nginx to Use Gateway

Once the gateway is working, update nginx config:

```bash
# Edit /root/booksy/nginx/default.conf
# Change the temporary direct routing back to:

location /api/ {
    proxy_pass http://gateway:80/;
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection 'upgrade';
    proxy_set_header Host $host;
    proxy_cache_bypass $http_upgrade;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
}

# Then reload nginx:
docker exec booksy-frontend nginx -s reload
```

---

## Verification Steps

### After Running Migrations:

```bash
# 1. Check tables exist
docker exec booksy-postgres psql -U booksy_admin -d booksy_user_management -c "\dt"

# 2. Check schemas exist
docker exec booksy-postgres psql -U booksy_admin -d booksy_user_management -c "\dn"

# 3. Test API endpoints
curl http://napstar.ir/api/v1/Categories/popular
curl "http://napstar.ir/api/v1/Providers/search?PageNumber=1&PageSize=6"
curl http://napstar.ir/api/v1/Platform/statistics

# Should return data (or empty arrays) instead of 500 errors
```

### After Gateway Fix:

```bash
# 1. Check gateway logs for Ocelot
docker logs booksy-gateway | grep -i "ocelot"

# Should see: "Ocelot started successfully" or similar

# 2. Test gateway routing
curl http://localhost:5000/api/v1/Categories/popular

# 3. Test through nginx
curl http://napstar.ir/api/v1/Categories/popular
```

---

## Troubleshooting

### Migration Errors

**Error**: "A network-related instance-specific error occurred"
- **Solution**: Check firewall allows port 5432 from your IP
```bash
sudo ufw allow from YOUR_IP to any port 5432
```

**Error**: "relation already exists"
- **Solution**: Migrations were partially applied. Use `--idempotent` flag or drop and recreate database

**Error**: "password authentication failed"
- **Solution**: Verify password in .env matches what you're using

### Gateway Errors

**Error**: "Ocelot configuration not found"
- **Solution**: Check file permissions and path
```bash
chmod 644 /root/booksy/gateway/ocelot.production.json
docker exec booksy-gateway ls -la /app/Configuration/
```

**Error**: Gateway still returns 404
- **Solution**: Check gateway logs for errors
```bash
docker logs booksy-gateway --tail 100
```

---

## Quick Commands Reference

```bash
# View all service status
docker ps

# View database tables
docker exec booksy-postgres psql -U booksy_admin -d booksy_user_management -c "\dt"

# Test API endpoint
curl http://napstar.ir/api/v1/Categories/popular

# View service logs
docker logs booksy-servicecatalog-api --tail 50
docker logs booksy-usermanagement-api --tail 50
docker logs booksy-gateway --tail 50

# Restart a service
docker restart booksy-frontend
docker restart booksy-gateway

# Full redeployment
cd /root/booksy && ./scripts/deploy.sh
```

---

## Summary of What's Been Done

### ✅ Completed on Server:
1. Fixed 502 error (nginx port configuration)
2. Created comprehensive Ocelot configuration for gateway
3. Created temporary nginx direct routing (bypassing gateway)
4. Updated docker-compose.prod.yml with volume mounts

### ⏳ Requires Action From You:
1. **Run database migrations** (use Option A, B, or C above)
2. **Update gateway source code** to load Ocelot (see Part 2)
3. **Rebuild and deploy** gateway Docker image
4. **Switch nginx back** to use gateway (once working)

---

## Files Modified/Created

**Deployment Server** (`/root/booksy/`):
- `nginx/default.conf` - Temporary direct routing configuration
- `gateway/ocelot.production.json` - Complete Ocelot routes (ready to use)
- `docker-compose.prod.yml` - Added volume mounts
- `MIGRATION_AND_GATEWAY_GUIDE.md` - This file
- `FIXES_SUMMARY.md` - Previous summary

**Requires Changes in Source Repositories**:
- `Booksy.Gateway/Program.cs` - Add Ocelot configuration loading
- Both API projects - Optionally add auto-migration (not recommended)

---

Need help with any specific step? I can guide you through the process!
