# Deployment & Operations Runbook

Operational reference for running Booksy in Docker Compose on the production/staging servers.
Moved verbatim out of `CLAUDE.md` on 2026-09-08 so the assistant instruction file can stay a
routing document; nothing here changed in the move. For what the system *is*, see
[openspec/project.md](../openspec/project.md); for CI, see `.github/workflows/`.

## Architecture

Booksy is a **modular monolith**: a single backend host composes multiple bounded contexts in-process.

The reference production deployment (`back.nahalkmi.ir`, 194.1.155.230) is a box **shared with other,
unrelated services** — it is not a dedicated Booksy host. Two consequences that shape everything
below: (1) all container ports bind to `127.0.0.1` only, never `0.0.0.0` — the box's existing
host-level nginx (already serving other domains) is the only thing that ever binds `80`/`443`
publicly, with a Booksy-only vhost (`deployment/nginx/booksy.conf`, one `server_name`) added
alongside the others, never replacing them; (2) there is no `ufw`/firewall automation in
`deployment/scripts/server-setup.sh` — see the caution comment there before ever enabling one on a
shared box. CI deploys over SSH as a dedicated, unprivileged `booksy` user (docker-group member,
no sudo), never root.

### Application Services
- **Booksy.Host** (`booksy-api`, `127.0.0.1:5000` → internal `80`): Single ASP.NET Core host that composes both bounded contexts (UserManagement and ServiceCatalog) in-process and serves all of their controllers under `/api/v1/...`. (A Booking context exists only as empty scaffolding and is not built.) Database migrations run at host startup. The host port binding is for operator debugging only — the frontend container reaches it over `booksy-network` by container name, never through this port.
- **Frontend** (`127.0.0.1:8081` → internal `80`): Web application frontend served via Nginx inside the container; its nginx config proxies `/api` to `booksy-api:80` over the Docker network. The host's own nginx (outside Docker) is what the public internet actually reaches, terminating TLS and reverse-proxying to this port — see `deployment/nginx/booksy.conf`.

### Infrastructure Services
- **PostgreSQL** (`127.0.0.1:5432`): Single primary database (`booksy`) with schema-per-context (schemas: `user_management`, `ServiceCatalog`, `cap`). One connection string (`DefaultConnection`).
- **Redis** (`127.0.0.1:6379`): Caching layer with LRU eviction policy (192MB limit on the shared reference box; raise it in `docker-compose.prod.yml` if you have more headroom)
- **Seq** (`127.0.0.1:5341`, `127.0.0.1:5342`) and **pgAdmin** (`127.0.0.1:5050`): OFF by default (Compose `profiles: ["observability"]`) — optional, RAM-hungry admin tools that aren't required for the app to run. Start them with `docker compose --profile observability up -d` if the box has headroom; otherwise use an SSH tunnel + a local pgAdmin/DBeaver, and rely on Serilog's own log output (it degrades gracefully when Seq isn't reachable).

### Service Communication
- All containers connect via a Docker bridge network (`booksy-network`, subnet 172.25.0.0/16)
- Containers communicate using container names as DNS hostnames
- Cross-context integration events run **in-process** via CAP (DotNetCore.CAP) on its in-memory transport (`EventBus:Provider=InMemory`) — there is no message broker container
- Redis provides distributed caching and session management

## Common Commands

### Deployment
```bash
# Every push to master runs this automatically (.github/workflows/deploy.yml).
# To do the same thing by hand:
cd /opt/booksy
docker compose -f docker-compose.prod.yml pull

# Clean up orphaned containers (prevents network removal errors)
docker ps -a --filter "name=booksy-" --format "{{.Names}}" | xargs -r docker rm -f || true

docker compose -f docker-compose.prod.yml down --remove-orphans
docker compose -f docker-compose.prod.yml up -d

# View all service status
docker compose -f docker-compose.prod.yml ps

# View logs for specific service
docker compose -f docker-compose.prod.yml logs -f [service-name]
# Example: docker compose -f docker-compose.prod.yml logs -f booksy-api

# View logs for all services
docker compose -f docker-compose.prod.yml logs -f
```

### Service Management
```bash
# Start all services
docker compose -f docker-compose.prod.yml up -d

# Stop all services
docker compose -f docker-compose.prod.yml down

# Restart a specific service
docker compose -f docker-compose.prod.yml restart [service-name]

# Scale a service (if supported)
docker compose -f docker-compose.prod.yml up -d --scale booksy-api=3
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
docker compose -f docker-compose.prod.yml ps

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
cd /opt/booksy/backups && ls -lt | tail -n +10 | awk '{print $9}' | xargs rm -f
```

## GitHub Actions Workflows

### Deploy to Production (`deploy.yml`)
One workflow does the whole thing on every push to `master` (or manual `workflow_dispatch`):
- `test`: unit tests against ephemeral Postgres/Redis service containers
- `e2e-keystone`: boots the real host and runs the keystone booking-flow smoke test
- `build-api` / `build-frontend`: build and push Docker images to GHCR (`ghcr.io/kazemim99/booksy-api`, `-frontend`), tagged `latest` and the commit SHA — run in parallel once tests pass
- `deploy`: SCPs `docker-compose.prod.yml` to the server, SSHes in as the dedicated `booksy` user, pulls the new images, and runs `docker compose up -d` (no `--profile observability` — Seq/pgAdmin stay off)
- A final `Health check` step curls `https://back.nahalkmi.ir/health` (through the box's own nginx, not the bare server IP — see the Architecture note above on why that matters on a shared box) and fails the run if it's not `200`

Required repo secrets (Settings → Secrets and variables → Actions): `SERVER_HOST`, `SERVER_USER`
(`booksy`), `SERVER_SSH_KEY` (the CI-only deploy key's private half — never a personal key),
`SERVER_DEPLOY_PATH` (`/opt/booksy`).

## Environment Configuration

All environment variables are stored in `/opt/booksy/.env`. Key variables include:

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

1. **Service won't start**: Check logs with `docker compose logs [service]` (e.g., `booksy-api`) and verify health check status
2. **Database connection errors**: Ensure PostgreSQL is healthy and connection string in `.env` is correct
3. **Out of memory**: Check `docker stats` and adjust resource limits in docker-compose.prod.yml
4. **Image pull failures**: Verify GHCR authentication with `docker login ghcr.io`
5. **Port conflicts**: all container ports bind `127.0.0.1` only (5000, 5432, 6379, 8081, and 5341/5342/5050 if the observability profile is running) — check with `ss -tlnp` on the host for anything else already bound to those before deploying to a new box. `80`/`443` belong to the host's own nginx, shared with whatever else it serves; Booksy's vhost is one `server_name` block among others (`deployment/nginx/booksy.conf`) — never delete or overwrite the others
6. **Swagger not accessible**: Verify `booksy-api` is healthy with `docker ps`. An unhealthy host cannot serve Swagger UI.

