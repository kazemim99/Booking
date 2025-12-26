# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Overview

This is a **deployment configuration repository** for Booksy, a microservices-based booking platform. The repository contains Docker Compose configurations, deployment scripts, and GitHub Actions workflows for deploying the application to production servers. The actual source code for the services is maintained in a separate repository.

## Architecture

Booksy follows a microservices architecture with the following components:

### Application Services
- **UserManagement API** (Port 5001): Handles user authentication, registration, and profile management
- **ServiceCatalog API** (Port 5002): Manages service listings, categories, and availability
- **Gateway** (Port 5000): API Gateway that routes requests to backend services
- **Frontend** (Ports 80/443): Web application frontend served via Nginx

### Infrastructure Services
- **PostgreSQL** (Port 5432): Primary database for all microservices
- **Redis** (Port 6379): Caching layer with LRU eviction policy (512MB limit)
- **RabbitMQ** (Ports 5672, 15672): Message broker for async communication between services
- **Seq** (Ports 5341, 5342): Centralized structured logging platform
- **pgAdmin** (Port 5050): Database management interface

### Service Communication
- All services connect via a Docker bridge network (`booksy-network`, subnet 172.25.0.0/16)
- Services communicate using container names as DNS hostnames
- RabbitMQ is used for event-driven communication between microservices
- Redis provides distributed caching and session management

## Common Commands

### Deployment
```bash
# Full deployment (pulls latest images and restarts all services)
cd /root/booksy && ./scripts/deploy.sh

# Manual deployment steps
cd /root/booksy
docker-compose -f docker-compose.prod.yml pull
docker-compose -f docker-compose.prod.yml down
docker-compose -f docker-compose.prod.yml up -d

# View all service status
docker-compose -f docker-compose.prod.yml ps

# View logs for specific service
docker-compose -f docker-compose.prod.yml logs -f [service-name]
# Example: docker-compose -f docker-compose.prod.yml logs -f usermanagement-api

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
docker-compose -f docker-compose.prod.yml up -d --scale usermanagement-api=3
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

### RabbitMQ Operations
```bash
# Access RabbitMQ Management UI
# Open browser to: http://server-ip:15672
# Default credentials from .env: booksy_admin / YourRabbitMQPassword123!

# List queues via CLI
docker exec booksy-rabbitmq rabbitmqctl list_queues

# List exchanges
docker exec booksy-rabbitmq rabbitmqctl list_exchanges

# View connection status
docker exec booksy-rabbitmq rabbitmqctl list_connections
```

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
# Access Swagger UI for each API service:
# UserManagement API: http://server-ip:5001/swagger
# ServiceCatalog API: http://server-ip:5002/swagger
# Gateway API: http://server-ip:5000/swagger

# Note: Services must be healthy for Swagger to be accessible
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

# Clean up old backups (manual)
cd /root/booksy/backups && ls -lt | tail -n +10 | awk '{print $9}' | xargs rm -f
```

## GitHub Actions Workflows

### Build and Push (`build-and-push.yml`)
- Triggers on: Push to main/develop, PRs, or manual dispatch
- Builds Docker images for all four services (UserManagement, ServiceCatalog, Gateway, Frontend)
- Pushes images to GitHub Container Registry (ghcr.io)
- Uses Docker layer caching for faster builds
- Tags images with branch name, PR number, commit SHA, and 'latest' for main branch

### Deploy (`deploy.yml`)
- Triggers automatically when build-and-push completes successfully on main branch
- Can also be manually triggered via workflow_dispatch
- Uses SSH to connect to production server
- Pulls latest Docker images from GHCR
- Performs zero-downtime deployment by stopping old containers and starting new ones
- Includes automatic cleanup of old Docker images

## Environment Configuration

All environment variables are stored in `/root/booksy/.env`. Key variables include:

- **Database**: `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB`
- **Redis**: `REDIS_PASSWORD`
- **RabbitMQ**: `RABBITMQ_DEFAULT_USER`, `RABBITMQ_DEFAULT_PASS`
- **Seq**: `SEQ_FIRSTRUN_ADMINUSERNAME`, `SEQ_FIRSTRUN_ADMINPASSWORD`
- **Container Registry**: `GITHUB_REPOSITORY_OWNER` (currently: kazemim99)

Never commit the `.env` file to version control. The `.env.backup` file should also be excluded from commits.

## Health Checks

All services have health checks configured using **curl**:

- **APIs**: HTTP check on `/health` endpoint using `curl -f` (30s interval, 10s timeout, 3 retries, 40s start period)
- **PostgreSQL**: `pg_isready` command (10s interval, 5s timeout, 5 retries, 10s start period)
- **Redis**: `redis-cli ping` (10s interval, 5s timeout, 3 retries, 5s start period)
- **RabbitMQ**: `rabbitmq-diagnostics ping` (30s interval, 10s timeout, 3 retries, 30s start period)
- **Seq**: HTTP check on `/api/health` using `curl -f` (30s interval, 10s timeout, 3 retries, 20s start period)
- **Frontend**: HTTP check on `/health` using `curl -f` (30s interval, 10s timeout, 3 retries, 20s start period)

**Important**: All Docker images must have `curl` installed for health checks to work. The Dockerfiles in the source repository include curl installation:
- .NET services: `apt-get install -y curl`
- Frontend (nginx:alpine): `apk add --no-cache curl`

## Resource Limits

Services have CPU and memory constraints:

- **APIs** (UserManagement, ServiceCatalog): 1 CPU / 1GB RAM (reserved: 0.25 CPU / 256MB)
- **Gateway**: 0.5 CPU / 512MB RAM (reserved: 0.1 CPU / 128MB)
- **Frontend**: 0.5 CPU / 256MB RAM (reserved: 0.1 CPU / 64MB)
- **PostgreSQL**: 2 CPU / 2GB RAM (reserved: 0.5 CPU / 512MB)
- **Redis**: 0.5 CPU / 512MB RAM (reserved: 0.1 CPU / 128MB)
- **RabbitMQ**: 1 CPU / 1GB RAM (reserved: 0.25 CPU / 256MB)
- **Seq**: 0.5 CPU / 512MB RAM (reserved: 0.1 CPU / 128MB)
- **pgAdmin**: 0.5 CPU / 512MB RAM (reserved: 0.1 CPU / 128MB)

## Service Dependencies

The startup order is enforced through Docker Compose dependencies:

1. **Infrastructure** (PostgreSQL, Redis, RabbitMQ, Seq) starts first
2. **Backend APIs** (UserManagement, ServiceCatalog) wait for infrastructure health checks
3. **Gateway** waits for both backend APIs to be healthy
4. **Frontend** waits for Gateway to be healthy

## Security Considerations

- Database and message broker ports (5432, 6379, 5672) are bound to localhost only (`127.0.0.1`)
- Passwords should be changed from defaults in `.env` before production use
- GitHub Container Registry authentication is required for pulling images
- SSH key-based authentication is used for deployment automation
- All services run in an isolated Docker network with defined subnet

## Troubleshooting

Common issues and solutions:

### 502 Bad Gateway / 504 Gateway Timeout Errors
**Symptoms**: Frontend shows nginx 502/504 errors when accessing APIs (e.g., http://napstar.ir/api/v1/)

**Cause**: Backend services are marked as unhealthy, preventing the gateway from routing requests properly.

**Solution**:
```bash
# 1. Check service health status
docker ps
# Look for services showing "unhealthy" status

# 2. Check health check logs
docker inspect booksy-usermanagement-api | grep -A 20 "Health"
docker inspect booksy-servicecatalog-api | grep -A 20 "Health"
docker inspect booksy-gateway | grep -A 20 "Health"

# 3. Verify curl is installed in containers (required for health checks)
docker exec booksy-usermanagement-api which curl
# Should return: /usr/bin/curl

# 4. If curl is missing, rebuild and deploy latest images:
cd /root/booksy
docker-compose -f docker-compose.prod.yml pull
docker-compose -f docker-compose.prod.yml up -d

# 5. Wait 30-60 seconds and verify health
docker ps
# All services should show "healthy" status
```

**Root Cause**: Health checks use `curl` to test the `/health` endpoint. If curl is not installed in the Docker images, health checks fail, marking services as unhealthy. This prevents proper request routing and causes 502/504 errors.

### Other Common Issues

1. **Service won't start**: Check logs with `docker-compose logs [service]` and verify health check status
2. **Database connection errors**: Ensure PostgreSQL is healthy and connection string in `.env` is correct
3. **Out of memory**: Check `docker stats` and adjust resource limits in docker-compose.prod.yml
4. **Image pull failures**: Verify GHCR authentication with `docker login ghcr.io`
5. **Port conflicts**: Ensure no other services are using the required ports (5000-5002, 80, 443, 5341, 15672, 5050)
6. **Swagger not accessible**: Verify the service is healthy with `docker ps`. Unhealthy services cannot serve Swagger UI.
