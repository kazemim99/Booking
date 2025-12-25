# Docker Build Optimization Guide

This document explains the Docker build optimizations implemented to drastically reduce build times.

## What Was Changed

### 1. Dockerfile Optimizations

All Dockerfiles now use:
- **Smart Layer Caching**: Project files are copied before source code to maximize cache hits
- **Separate Restore Layer**: NuGet and npm restore operations are isolated in their own cached layers
- **Selective Copying**: Only necessary files are copied, reducing context size
- **No-Restore Flags**: Build and publish steps skip restore, using cached packages from restore layer

### 2. Docker Compose Configuration

The [docker-compose.yml](docker-compose.yml) now includes:
- Build cache configuration for all services
- Parallel build support enabled by default

### 3. .dockerignore Improvements

The [.dockerignore](.dockerignore) file now excludes:
- Build artifacts (bin, obj, dist, node_modules)
- Documentation and test results
- IDE and OS temporary files
- Log files and cache directories

## How to Build

### Quick Start (Recommended)

**Windows (PowerShell):**
```powershell
.\build.ps1
```

**Linux/Mac (Bash):**
```bash
./build.sh
```

### Manual Build

**Build all services in parallel:**
```bash
docker compose build --parallel
```

**Build a specific service:**
```bash
docker compose build booksy-gateway
docker compose build booksy-user-management-api
docker compose build booksy-service-catalog-api
docker compose build booksy-frontend
```

## Expected Performance Improvements

### First Build (Cold Cache)
- **Before**: 10-15 minutes
- **After**: 8-12 minutes (10-20% faster due to parallel builds)

### Incremental Builds (Warm Cache)
- **Before**: 5-10 minutes (rebuilds everything)
- **After**: 30 seconds - 2 minutes (only rebuilds changed layers)

### Code-Only Changes
- **Before**: 5-10 minutes
- **After**: 20-60 seconds (NuGet/npm packages cached)

## How It Works

### .NET Services

1. **Project Files Copied First**: All `.csproj` files are copied before source code
2. **NuGet Restore in Separate Layer**: `dotnet restore` creates a cached Docker layer
3. **Source Code Copied**: Only after restore, source code is copied
4. **Build with --no-restore**: Build uses packages from cached restore layer
5. **Publish with --no-restore**: Final output uses packages from cached restore layer

This means:
- If you change code but not `.csproj` files → Restore layer is reused (FAST)
- If you change dependencies → Only restore layer rebuilds, then build continues (FASTER)
- If nothing changed → All layers are reused (INSTANT)

### Frontend Service

1. **package.json Copied First**: Package files copied before source
2. **npm ci in Separate Layer**: `npm ci` creates a cached Docker layer with node_modules
3. **Source Code Copied**: Only after install
4. **Build**: Uses node_modules from cached install layer

This means:
- If you change Vue code but not `package.json` → Install layer is reused (FAST)
- If you change packages → Only install layer rebuilds, then build continues (FASTER)
- If nothing changed → All layers are reused (INSTANT)

## Cache Management

### View Cache Size
```bash
docker system df
```

### Clear Build Cache (if needed)
```bash
docker builder prune
```

### Clear All Docker Cache
```bash
docker system prune -a
```

## Troubleshooting

### Builds are still slow
1. Verify you're using `--parallel` flag for building all services
2. Clear old build cache: `docker builder prune`
3. Make sure .dockerignore is working: `docker compose build --progress=plain`
4. Check Docker has enough resources (CPU/RAM) in Docker Desktop settings

### Cache not being used
1. When you modify `.csproj` or `package.json`, layers before that restore are still reused
2. Check if .dockerignore is excluding files it shouldn't
3. Use `docker compose build --progress=plain <service>` to see which layers are cached
4. Look for `CACHED` next to layer steps in the build output

### First build is slow
This is expected. Docker needs to:
- Download base images (dotnet SDK, node, nginx)
- Restore all NuGet and npm packages
- Build all code from scratch

Subsequent builds will be much faster as layers are reused.

## Additional Tips

1. **Build Only What Changed**: If you only changed the frontend, build only that:
   ```bash
   docker compose build booksy-frontend
   ```

2. **Use Docker Desktop**: If on Windows/Mac, Docker Desktop provides better BuildKit support

3. **Increase Docker Resources**: Give Docker more CPU/RAM in Docker Desktop settings for faster parallel builds

4. **Keep Dependencies Stable**: Avoid changing package versions frequently to maximize cache hits

## Verification

To verify the optimizations are working, watch the build output:

```bash
docker compose build --progress=plain booksy-gateway
```

Look for:
- `CACHED` next to layer steps (good! means layers are being reused)
- `dotnet restore` step showing as CACHED when you haven't changed `.csproj` files
- `npm ci` step showing as CACHED when you haven't changed `package.json`
- Significantly shorter build times on subsequent builds

### Example of Good Cache Usage

On second build after only changing code:
```
#5 [build 2/7] COPY [...csproj files...]
#5 CACHED

#6 [build 3/7] RUN dotnet restore
#6 CACHED   <-- This is what you want to see!

#7 [build 4/7] COPY [source code...]
#7 DONE 0.5s

#8 [build 5/7] RUN dotnet build
#8 DONE 12.3s  <-- Only this rebuilds
```
