# Ubuntu Server Docker Build Fix

## Problem
The Ubuntu server still has old Dockerfiles with BuildKit cache mount syntax that's causing build failures.

## Solution Options

### Option 1: Git Push/Pull (Recommended)

If your code is in a Git repository:

**On Windows:**
```bash
git add .
git commit -m "Optimize Docker builds with layer caching"
git push
```

**On Ubuntu Server:**
```bash
git pull
docker compose build --parallel
```

### Option 2: Manual File Transfer

Use SCP or your preferred method to copy the updated Dockerfiles from Windows to Ubuntu:

```bash
# From Windows (PowerShell/CMD)
scp src/UserManagement/Booksy.UserManagement.API/Dockerfile user@ubuntu-server:/path/to/booking/src/UserManagement/Booksy.UserManagement.API/
scp src/APIGateway/Booksy.Gateway/Dockerfile user@ubuntu-server:/path/to/booking/src/APIGateway/Booksy.Gateway/
scp src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/Dockerfile user@ubuntu-server:/path/to/booking/src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/
scp booksy-frontend/Dockerfile user@ubuntu-server:/path/to/booking/booksy-frontend/
scp .dockerignore user@ubuntu-server:/path/to/booking/
```

### Option 3: Quick Fix Script on Ubuntu

Run this on your Ubuntu server to fix the Dockerfiles in place:

```bash
# Navigate to your project directory
cd /path/to/booking

# Fix User Management API
sed -i '/RUN --mount=type=cache.*nuget/,/dotnet restore/ {
  s/RUN --mount=type=cache.*$/RUN dotnet restore ".\/src\/UserManagement\/Booksy.UserManagement.API\/Booksy.UserManagement.API.csproj"/
  /dotnet restore/d
}' src/UserManagement/Booksy.UserManagement.API/Dockerfile

# Fix API Gateway
sed -i '/RUN --mount=type=cache.*nuget/,/dotnet restore/ {
  s/RUN --mount=type=cache.*$/RUN dotnet restore ".\/src\/APIGateway\/Booksy.Gateway\/Booksy.Gateway.csproj"/
  /dotnet restore/d
}' src/APIGateway/Booksy.Gateway/Dockerfile

# Fix Service Catalog
sed -i '/RUN --mount=type=cache.*nuget/,/dotnet restore/ {
  s/RUN --mount=type=cache.*$/RUN dotnet restore ".\/src\/BoundedContexts\/ServiceCatalog\/Booksy.ServiceCatalog.Api\/Booksy.ServiceCatalog.Api.csproj"/
  /dotnet restore/d
}' src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/Dockerfile

# Fix Frontend
sed -i '/RUN --mount=type=cache.*npm/,/npm ci/ {
  s/RUN --mount=type=cache.*$/RUN npm ci --prefer-offline --no-audit/
  /npm ci/d
}' booksy-frontend/Dockerfile

# Also fix the build commands
find . -name "Dockerfile" -type f -exec sed -i 's/RUN --mount=type=cache,id=nuget,target=\/root\/.nuget\/packages \\/RUN /g' {} \;
find . -name "Dockerfile" -type f -exec sed -i 's/RUN --mount=type=cache,id=npm,target=\/root\/.npm \\/RUN /g' {} \;
```

### Option 4: Manual Edit (Most Reliable)

Edit each Dockerfile on Ubuntu and change:

**FROM:**
```dockerfile
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet restore "./path/to/project.csproj"
```

**TO:**
```dockerfile
RUN dotnet restore "./path/to/project.csproj"
```

**Files to edit:**
1. `src/UserManagement/Booksy.UserManagement.API/Dockerfile`
2. `src/APIGateway/Booksy.Gateway/Dockerfile`
3. `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/Dockerfile`
4. `booksy-frontend/Dockerfile`

Look for lines with `--mount=type=cache` and remove that part, keeping just the `RUN` command.

## Verify the Fix

After applying any option above, verify with:

```bash
# Check one of the files
grep -n "mount=type=cache" src/UserManagement/Booksy.UserManagement.API/Dockerfile
```

If this returns nothing, you're good. If it finds matches, those lines still need fixing.

## Build After Fix

```bash
docker compose build --parallel
```

## Why This Happened

The BuildKit cache mount feature (`--mount=type=cache`) requires:
1. Docker BuildKit to be explicitly enabled
2. Proper BuildKit version support

The updated Dockerfiles use standard Docker layer caching instead, which works everywhere without special configuration.
