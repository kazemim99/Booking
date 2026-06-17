# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Overview

This repository contains both the **source code** and the **deployment configuration** for Booksy, a **modular-monolith** booking platform. The backend is a single ASP.NET Core host (`Booksy.Host`) that composes multiple bounded contexts (UserManagement, ServiceCatalog) in-process. The repository also contains the Vue frontend, Docker Compose configurations, deployment scripts, and GitHub Actions workflows for deploying the application to production servers.

> **Migration note**: The backend was migrated from a microservices architecture to a modular monolith. The Ocelot API Gateway and per-service hosts have been retired, and RabbitMQ has been removed in favor of in-process CAP events. See [MONOLITH_MIGRATION_PLAN.md](MONOLITH_MIGRATION_PLAN.md) for details.

## 📚 Developer Documentation

### API & Integration Reference

- **[API_ENDPOINTS.md](API_ENDPOINTS.md)** - Complete API endpoint reference (single host on :5000)
  - UserManagement endpoints: Authentication, Customer Management
  - ServiceCatalog endpoints: Categories, Providers, Bookings, Services
  - All endpoints served from one host (`booksy-api`) under `/api/v1/...`
  - All request/response schemas, authentication requirements, and examples

- **[DTO_MAPPING.md](DTO_MAPPING.md)** - DTO mapping across all application layers
  - Backend C# DTOs ↔ Flutter Dart Models ↔ Vue TypeScript Interfaces
  - Type conversion guidelines (Guid → String, decimal → double, etc.)
  - JSON serialization best practices
  - Naming conventions and file locations

### Application-Specific Documentation

- **Flutter Customer App**: [booksy-customer-app/](booksy-customer-app/)
  - [PROJECT_SUMMARY.md](booksy-customer-app/PROJECT_SUMMARY.md) - Architecture & features
  - [FLUTTER_BACKEND_CONNECTION.md](booksy-customer-app/FLUTTER_BACKEND_CONNECTION.md) - Backend integration guide
  - [CUSTOMER_APP_UX_FLOW.md](booksy-customer-app/CUSTOMER_APP_UX_FLOW.md) - User experience flow

- **Vue Admin Panel**: [booksy-admin/](booksy-admin/) *(if applicable)*
  - Admin dashboard for managing providers, services, and bookings

- **Backend Source**: `src/Host/Booksy.Host` (single ASP.NET Core host) plus bounded contexts under `src/`
  - Domain-Driven Design with CQRS pattern
  - In-process integration events via CAP (DotNetCore.CAP) on the in-memory transport
  - Clean Architecture principles

## Architecture

Booksy is a **modular monolith**: a single backend host composes multiple bounded contexts in-process.

### Application Services
- **Booksy.Host** (`booksy-api`, Port 5000 → internal 80): Single ASP.NET Core host that composes both bounded contexts (UserManagement and ServiceCatalog) in-process and serves all of their controllers under `/api/v1/...`. (A Booking context exists only as empty scaffolding and is not built.) Database migrations run at host startup.
- **Frontend** (Ports 80/443): Web application frontend served via Nginx; its nginx config proxies `/api` to `booksy-api:80`.

### Infrastructure Services
- **PostgreSQL** (Port 5432): Single primary database (`booksy`) with schema-per-context (schemas: `user_management`, `ServiceCatalog`, `cap`). One connection string (`DefaultConnection`).
- **Redis** (Port 6379): Caching layer with LRU eviction policy (512MB limit)
- **Seq** (Ports 5341, 5342): Centralized structured logging platform
- **pgAdmin** (Port 5050): Database management interface

### Service Communication
- All containers connect via a Docker bridge network (`booksy-network`, subnet 172.25.0.0/16)
- Containers communicate using container names as DNS hostnames
- Cross-context integration events run **in-process** via CAP (DotNetCore.CAP) on its in-memory transport (`EventBus:Provider=InMemory`) — there is no message broker container
- Redis provides distributed caching and session management

## Common Commands

### Deployment
```bash
# Full deployment (pulls latest images and restarts all services)
cd /root/booksy && ./scripts/deploy.sh

# Manual deployment steps
cd /root/booksy
docker-compose -f docker-compose.prod.yml pull

# Clean up orphaned containers (prevents network removal errors)
docker ps -a --filter "name=booksy-" --format "{{.Names}}" | xargs -r docker rm -f || true

docker-compose -f docker-compose.prod.yml down --remove-orphans
docker-compose -f docker-compose.prod.yml up -d

# View all service status
docker-compose -f docker-compose.prod.yml ps

# View logs for specific service
docker-compose -f docker-compose.prod.yml logs -f [service-name]
# Example: docker-compose -f docker-compose.prod.yml logs -f booksy-api

# View logs for all services
docker-compose -f docker-compose.prod.yml logs -f
```

### Service Management
```bash
# Start all services
docker-compose -f docker-compose.prod.yml up -d

# Stop all services
docker-compose -f docker-compose.prod.yml down

# Restart a specific service
docker-compose -f docker-compose.prod.yml restart [service-name]

# Scale a service (if supported)
docker-compose -f docker-compose.prod.yml up -d --scale booksy-api=3
```

### Database Operations
```bash
# Access PostgreSQL shell
docker exec -it booksy-postgres psql -U booksy_admin -d booksy_user_management

# Create database backup
docker exec booksy-postgres pg_dump -U booksy_admin booksy_user_management > backup_$(date +%Y%m%d_%H%M%S).sql

# Restore from backup
docker exec -i booksy-postgres psql -U booksy_admin booksy_user_management < backup.sql

# View database logs
docker logs booksy-postgres
```

### Redis Operations
```bash
# Access Redis CLI
docker exec -it booksy-redis redis-cli -a YourRedisPassword123!

# Monitor Redis commands in real-time
docker exec -it booksy-redis redis-cli -a YourRedisPassword123! MONITOR

# Check Redis memory usage
docker exec -it booksy-redis redis-cli -a YourRedisPassword123! INFO memory
```

### Integration Events (CAP)

Cross-context integration events run in-process via CAP on its in-memory transport — there is no RabbitMQ broker. CAP persists outbox/inbox state in the `cap` schema of the PostgreSQL database. To inspect published/received messages, query the CAP tables in Postgres or use the Seq logs.

### Monitoring and Logging
```bash
# Access Seq logging UI
# Open browser to: http://server-ip:5341

# View container resource usage
docker stats

# Check health status of all services
docker-compose -f docker-compose.prod.yml ps

# View specific service health
docker inspect --format='{{.State.Health.Status}}' booksy-[service-name]
```

### API Documentation (Swagger)
```bash
# Access Swagger UI on the single host:
# Booksy API: http://server-ip:5000/swagger

# Note: The service must be healthy for Swagger to be accessible
# Check service health: docker ps
```

### Cleanup and Maintenance
```bash
# Remove stopped containers and unused images
docker system prune -a

# Remove only unused images
docker image prune -f

# View disk usage by Docker
docker system df

# Clean up orphaned booksy containers (all containers with 'booksy-' prefix)
docker ps -a --filter "name=booksy-" --format "{{.Names}}" | xargs -r docker rm -f

# Clean up old backups (manual)
cd /root/booksy/backups && ls -lt | tail -n +10 | awk '{print $9}' | xargs rm -f
```

## GitHub Actions Workflows

### Build and Push (`build-and-push.yml`)
- Triggers on: Push to main/develop, PRs, or manual dispatch
- Builds Docker images for the backend host (`booksy-api`) and the Frontend
- Pushes images to GitHub Container Registry (ghcr.io)
- Uses Docker layer caching for faster builds
- Tags images with branch name, PR number, commit SHA, and 'latest' for main branch

### Deploy (`deploy.yml`)
- Triggers automatically when build-and-push completes successfully on main branch
- Can also be manually triggered via workflow_dispatch
- Uses SSH to connect to production server
- Pulls latest Docker images from GHCR
- **Forcibly removes orphaned containers** before docker-compose down to prevent network errors
- Performs zero-downtime deployment by stopping old containers and starting new ones
- Includes automatic cleanup of old Docker images

## Environment Configuration

All environment variables are stored in `/root/booksy/.env`. Key variables include:

- **Database**: `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB`
- **Redis**: `REDIS_PASSWORD`
- **Seq**: `SEQ_FIRSTRUN_ADMINUSERNAME`, `SEQ_FIRSTRUN_ADMINPASSWORD`
- **Container Registry**: `GITHUB_REPOSITORY_OWNER` (currently: kazemim99)

Never commit the `.env` file to version control. The `.env.backup` file should also be excluded from commits.

## Health Checks

All services have health checks configured using **curl**:

- **Backend API** (`booksy-api`): HTTP check on `/health` endpoint using `curl -f` (30s interval, 10s timeout, 3 retries, 40s start period)
- **PostgreSQL**: `pg_isready` command (10s interval, 5s timeout, 5 retries, 10s start period)
- **Redis**: `redis-cli ping` (10s interval, 5s timeout, 3 retries, 5s start period)
- **Seq**: HTTP check on `/api/health` using `curl -f` (30s interval, 10s timeout, 3 retries, 20s start period)
- **Frontend**: HTTP check on `/health` using `curl -f` (30s interval, 10s timeout, 3 retries, 20s start period)

**Important**: All Docker images must have `curl` installed for health checks to work. The Dockerfiles in the source repository include curl installation:
- .NET host: `apt-get install -y curl`
- Frontend (nginx:alpine): `apk add --no-cache curl`

## Resource Limits

Services have CPU and memory constraints:

- **Backend API** (`booksy-api`): 1 CPU / 1GB RAM (reserved: 0.25 CPU / 256MB)
- **Frontend**: 0.5 CPU / 256MB RAM (reserved: 0.1 CPU / 64MB)
- **PostgreSQL**: 2 CPU / 2GB RAM (reserved: 0.5 CPU / 512MB)
- **Redis**: 0.5 CPU / 512MB RAM (reserved: 0.1 CPU / 128MB)
- **Seq**: 0.5 CPU / 512MB RAM (reserved: 0.1 CPU / 128MB)
- **pgAdmin**: 0.5 CPU / 512MB RAM (reserved: 0.1 CPU / 128MB)

## Service Dependencies

The startup order is enforced through Docker Compose dependencies:

1. **Infrastructure** (PostgreSQL, Redis, Seq) starts first
2. **Backend host** (`booksy-api`) waits for infrastructure health checks, then runs migrations at startup
3. **Frontend** waits for `booksy-api` to be healthy

## Security Considerations

- Database and cache ports (5432, 6379) are bound to localhost only (`127.0.0.1`)
- Passwords should be changed from defaults in `.env` before production use
- GitHub Container Registry authentication is required for pulling images
- SSH key-based authentication is used for deployment automation
- All services run in an isolated Docker network with defined subnet

## Troubleshooting

Common issues and solutions:

### Other Common Issues

1. **Service won't start**: Check logs with `docker-compose logs [service]` (e.g., `booksy-api`) and verify health check status
2. **Database connection errors**: Ensure PostgreSQL is healthy and connection string in `.env` is correct
3. **Out of memory**: Check `docker stats` and adjust resource limits in docker-compose.prod.yml
4. **Image pull failures**: Verify GHCR authentication with `docker login ghcr.io`
5. **Port conflicts**: Ensure no other services are using the required ports (5000, 80, 443, 5341, 5050)
6. **Swagger not accessible**: Verify `booksy-api` is healthy with `docker ps`. An unhealthy host cannot serve Swagger UI.
