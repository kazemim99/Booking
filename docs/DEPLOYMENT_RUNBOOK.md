# Deployment & Operations Runbook

Operational reference for running Booksy in Docker Compose on the production/staging servers.
Moved verbatim out of `CLAUDE.md` on 2026-09-08 so the assistant instruction file can stay a
routing document; nothing here changed in the move. For what the system *is*, see
[openspec/project.md](../openspec/project.md); for CI, see `.github/workflows/`.

## Current production state (as of 2026-09-18)

**Live and verified end to end** (health 200, API 200, CORS preflight 204, sandbox OTP login issued a token):

| URL | What | Served by |
|---|---|---|
| `https://back.nahalkmi.ir` | Vue web app + `/api/*` | host nginx → `booksy-frontend` container (`127.0.0.1:8081`), whose own nginx proxies `/api/` → `booksy-api` |
| `https://provider.nahalkmi.ir` | Provider app (Flutter **web** build) | host nginx, static files from `/var/www/booksy-provider` |

- **Server:** `194.1.155.230`, Ubuntu 24.04, 2 vCPU / ~3.8 GB RAM, **shared** with unrelated services
  (see Architecture below). DNS A records for `nahalkmi.ir`, `back.`, `provider.` all point here.
- **Deploy user:** `booksy` (docker group, **no sudo, no root**). Deploy dir `/opt/booksy`
  (`.env` + `docker-compose.prod.yml`). Root SSH is used only for host-level nginx/certbot work.
- **Running containers:** `booksy-api`, `booksy-frontend`, `booksy-postgres`, `booksy-redis`.
  Seq and pgAdmin are **not** running (Compose `observability` profile, off by default).
- **Images:** `ghcr.io/kazemim99/booksy-api:latest`, `ghcr.io/kazemim99/booksy-frontend:latest`
  (repo is public, so pulls need no auth).
- **TLS:** Let's Encrypt via certbot (auto-renew), one cert per subdomain.
- **nginx vhosts:** `deployment/nginx/booksy.conf` and `deployment/nginx/booksy-provider.conf` are
  **copies of what is live** in `/etc/nginx/sites-available/`. Edit the server, then re-sync the repo
  copy (certbot rewrites these files, so the server is authoritative).

### ⚠️ Active security debt — sandbox OTP is ON in production

`/opt/booksy/.env` currently contains:

```
OTP_SANDBOX_CODE=222222
Sms__SandboxMode=true
```

That makes **`222222` a valid OTP for any phone number** on the public host, and suppresses real SMS.
It was enabled deliberately for testing the first deployment. **Remove both lines and
`docker compose -f docker-compose.prod.yml up -d` before any real user signs up.** Tracked as
FOLLOW-UPS #58. (`OtpCode.Generate()` reads `OTP_SANDBOX_CODE` straight from the process environment;
the provider app's OTP input is 6 boxes, so the code must be 6 digits.)

### CI/CD status

`.github/workflows/deploy.yml` (push to `master`): test -> keystone E2E -> build API image,
build frontend image, **build provider web app** (analyze + test + `flutter build web`) -> **deploy**.

The `deploy` job runs on a **self-hosted runner on the production box** (labels `self-hosted`,
`booksy-prod`; runs as `booksy`; installed in `/home/booksy/actions-runner` as a systemd service
`actions.runner.kazemim99-Booking.booksy-prod-1`). It copies `docker-compose.prod.yml` to
`/opt/booksy`, pulls `booksy-api` + `frontend`, `up -d`, waits for `booksy-api` to be healthy,
publishes the provider bundle to `/var/www/booksy-provider` (verified before the old one is
removed), and checks both public URLs.

Why self-hosted (FOLLOW-UPS #59): GitHub-hosted runners cannot reach this server's SSH at all --
`/var/log/auth.log` shows sshd connections from Iranian address ranges only, never a runner, so
the old `appleboy/scp-action` deploy could not work whatever the secrets said. The runner makes
outbound connections only.

Runner operations (as root):

```bash
systemctl status 'actions.runner.*'                       # is it online?
journalctl -u 'actions.runner.*' -n 100                   # its log
cd /home/booksy/actions-runner && ./svc.sh stop|start     # restart it
```

Re-registering (e.g. after removing it in GitHub): get a token from repo Settings -> Actions ->
Runners -> New self-hosted runner, then as `booksy` in `~/actions-runner`:
`./config.sh --unattended --replace --url https://github.com/kazemim99/Booking --token <TOKEN> --name booksy-prod-1 --labels booksy-prod`
and as root `./svc.sh install booksy && ./svc.sh start`.

Security: the repo is public, so a fork's pull request could otherwise edit a workflow to run on
this box. Settings -> Actions -> General -> "Fork pull request workflows from outside
collaborators" must be **"Require approval for all outside collaborators"**. No workflow triggered
by `pull_request` may use `runs-on: [self-hosted, ...]`.

### Manual deploy (fallback when the runner is down)

Backend (after CI has pushed new images):

```bash
scp -i <deploy-key> docker-compose.prod.yml booksy@194.1.155.230:/opt/booksy/
ssh -i <deploy-key> booksy@194.1.155.230 \
  'cd /opt/booksy && docker compose -f docker-compose.prod.yml pull && docker compose -f docker-compose.prod.yml up -d'
curl -s -o /dev/null -w '%{http_code}\n' https://back.nahalkmi.ir/health   # expect 200
```

Provider app (Flutter web):

```bash
cd booksy-provider-app
flutter build web --release --dart-define=API_BASE_URL=https://back.nahalkmi.ir
tar --force-local -czf provider-web.tar.gz -C build/web .   # --force-local: Git Bash reads "C:" as a host
scp -i <deploy-key> provider-web.tar.gz booksy@194.1.155.230:provider-web.tar.gz   # home dir, not /tmp
ssh -i <deploy-key> booksy@194.1.155.230 'set -e
  test -s ~/provider-web.tar.gz
  rm -rf ~/provider-web.new && mkdir ~/provider-web.new
  tar -xzf ~/provider-web.tar.gz -C ~/provider-web.new
  test -f ~/provider-web.new/index.html
  rm -rf /var/www/booksy-provider/* && cp -a ~/provider-web.new/. /var/www/booksy-provider/
  rm -rf ~/provider-web.new ~/provider-web.tar.gz'
```

Never `rm` the live directory before the new bundle is verified on the server: on 2026-09-18 an
scp to `/tmp` reported success but the file was not there for the ssh session (`/tmp` is not a
reliable hand-off on this host), the old one-liner deleted the site first, and provider.nahalkmi.ir
served 403 for about a minute.

Verify the API URL was actually compiled in: `grep -c back.nahalkmi.ir build/web/main.dart.js`
should be ≥1 and `grep -c localhost:5000 build/web/main.dart.js` should be 0. Without the
`--dart-define`, the bundle silently points at `http://localhost:5000` (see
`booksy-provider-app/lib/core/api/config/api_constants.dart`).

### Provider app caching (why a deploy reaches every browser)

Flutter names the whole app `main.dart.js` on every build. Until 2026-09-19 the vhost cached it for
30 days (`public, max-age=2592000`) on the false belief that Flutter hashes its file names, so
browsers kept running an old build after each deploy — a user saw last week's services screen in an
incognito window. Now:

- CI renames the entry file to `main.dart.<sha256-16>.js` and rewrites `mainJsPath` in
  `flutter_bootstrap.js` (`booksy-provider-app/tool/cache_bust_web.sh`); the deploy job refuses a
  bundle whose bootstrap does not name that file.
- nginx caches that hashed file for a year (`immutable`: a new build is a new name), revalidates
  every other app file (`no-cache`, ETag -> 304), and never caches `index.html`,
  `flutter_bootstrap.js` or the service worker.

A manual web build must run `bash tool/cache_bust_web.sh build/web` before upload, or the site
references `main.dart.js` through a bootstrap that still works but caches nothing specially.

### Outbound services the API depends on

- **Nominatim** (`nominatim.openstreetmap.org`) — place search and reverse geocoding for the map
  picker, via `GET /api/v1/Geocoding/search|reverse`. The SERVER makes this call (2026-09-19): from
  a browser it fails on networks that cannot reach that host, and the usage policy does not allow a
  crowd of unidentified clients. Answers are cached 24 h in memory; a restart empties that cache.
  `Geocoding:BaseUrl` overrides the host. Check it from the box with
  `curl -s -o /dev/null -w '%{http_code}' -A 'BooksyProvider/1.0' 'https://nominatim.openstreetmap.org/search?q=tehran&format=jsonv2&limit=1'`.
  A 503 from our endpoint means the upstream failed; the app then keeps whatever the user typed.
- **tile.openstreetmap.org** — map tiles, fetched by the browser directly (no server involvement).
- **Firebase Cloud Messaging** (`fcm.googleapis.com`) — push notifications, sent by the server. Configured
  by `Notifications:Firebase:CredentialsPath` (a mounted service-account JSON) **or**
  `Notifications:Firebase:CredentialsJson` (its contents, for a secret env var); the JSON form wins if both
  are set. **Both blank is a supported state:** the host starts, logs
  `Firebase is not configured; push notifications will be skipped`, and every push is recorded as *skipped*.
  It is never recorded as delivered — that distinction matters, because this service previously returned a
  fabricated success and the delivery log claimed messages that were never sent. A misconfigured credential
  degrades the same way rather than failing startup.
  To check which state a running host is in: `docker logs booksy-api 2>&1 | grep -i firebase`. Expect either
  `Firebase messaging initialised` or the "not configured" warning.
  Note that push also needs registered devices — `POST /api/v1/DeviceTokens` from the apps. With no
  registered device a push is skipped with "The recipient has no registered device", which is not a failure
  and does not consume the notification's retry budget.

### Gotchas learned the hard way

- **fail2ban bans the operator.** Many SSH connections in a short time got the working IP banned
  (port 22 timed out while 80/443 and ping still worked). Unban from the VPS provider's web console:
  `fail2ban-client set sshd unbanip <ip>` and `fail2ban-client set sshd addignoreip <ip>`.
- **The operator's VPN exit IP changes.** When it switched, the server became unreachable on *every*
  port from that route while an external probe still saw it healthy. Check `curl https://api.ipify.org`
  before assuming the server is down.
- **nginx here is 1.24.** It does not accept the standalone `http2 on;` directive — use
  `listen 443 ssl http2;`. A failed `nginx -t`/reload keeps the running config, so the other sites on
  the box stay up; always `nginx -t` before `systemctl reload nginx`.
- **certbot turns a placeholder into a redirect loop.** Issuing a cert against a vhost whose only
  `location /` is a `return 301` copies that redirect into the new 443 block (HTTPS → HTTPS forever).
  Write the real 443 server block after issuance.
- **Never enable `ufw`** on this box without allow-listing every service it runs (VPN, proxy panel,
  tunnel ports) — see `deployment/scripts/server-setup.sh`.

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

