# Docker Build Optimization Guide

This document explains the Docker build optimizations implemented to drastically reduce build times.

## What Was Changed

### 1. Dockerfile Optimizations

All Dockerfiles now use:
- **Layer Caching**: Project files are copied before source code to maximize cache hits
- **BuildKit Cache Mounts**: NuGet and npm packages are cached between builds
- **Selective Copying**: Only necessary files are copied, reducing context size
- **No-Restore Flags**: Prevents redundant restore operations

### 2. Docker Compose Configuration

The [docker-compose.yml](docker-compose.yml) now includes:
- BuildKit cache configuration for all services
- Parallel build support

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

**Enable BuildKit:**
```bash
# Linux/Mac
export DOCKER_BUILDKIT=1
export COMPOSE_DOCKER_CLI_BUILD=1

# Windows PowerShell
$env:DOCKER_BUILDKIT=1
$env:COMPOSE_DOCKER_CLI_BUILD=1
```

**Build all services:**
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
2. **NuGet Restore with Cache Mount**: `dotnet restore` uses a persistent cache at `/root/.nuget/packages`
3. **Source Code Copied**: Only after restore, source code is copied
4. **Build with --no-restore**: Build uses cached packages
5. **Publish with --no-restore**: Final output uses cached packages

This means:
- If you change code but not dependencies → Only rebuild, no restore (FAST)
- If you change dependencies → Restore is cached across builds (FASTER)
- If nothing changed → Use cached layers (INSTANT)

### Frontend Service

1. **package.json Copied First**: Package files copied before source
2. **npm ci with Cache Mount**: npm packages cached at `/root/.npm`
3. **Source Code Copied**: Only after install
4. **Build**: Uses cached node_modules

This means:
- If you change Vue code but not packages → Only rebuild, no install (FAST)
- If you change packages → npm cache is reused (FASTER)
- If nothing changed → Use cached layers (INSTANT)

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

### Build fails with "cache mount" error
Make sure Docker BuildKit is enabled:
```bash
export DOCKER_BUILDKIT=1
```

### Builds are still slow
1. Check Docker BuildKit is enabled
2. Verify you're using `--parallel` flag
3. Clear old build cache: `docker builder prune`
4. Make sure .dockerignore is working: `docker compose build --progress=plain`

### Cache not being used
1. If you modify `.csproj` or `package.json`, cache layers before that are reused
2. Check if .dockerignore is excluding files it shouldn't
3. Try `docker compose build --progress=plain` to see which layers are cached

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
DOCKER_BUILDKIT=1 docker compose build --progress=plain booksy-gateway
```

Look for:
- `CACHED` next to layer steps (good!)
- `RUN --mount=type=cache` in the output (cache mounts active)
- Shorter build times on subsequent builds

## Reverting Changes

If you need to revert to old behavior:
1. Set `DOCKER_BUILDKIT=0`
2. Remove `--mount=type=cache` from RUN commands in Dockerfiles
3. Remove `x-bake` sections from docker-compose.yml
