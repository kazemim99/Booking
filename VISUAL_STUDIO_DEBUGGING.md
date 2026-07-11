# Visual Studio Debugging Guide

How to debug the Booksy backend in Visual Studio while infrastructure (PostgreSQL, Redis, Seq, pgAdmin) runs in Docker.

> The backend is a **single modular-monolith host** (`Booksy.Host`) — there is no separate Gateway, UserManagement API, or ServiceCatalog API process to start, and no RabbitMQ (cross-context events run in-process via CAP). If you find older instructions describing a Gateway + per-context APIs + RabbitMQ setup, they predate the monolith migration (see `MONOLITH_MIGRATION_PLAN.md`) and no longer apply.

## Architecture

```
┌───────────────────────────┐
│  Visual Studio (F5/Debug) │
│      Booksy.Host :5000    │
└─────────────┬──────────────┘
              │
┌─────────────▼──────────────────────────────────────────┐
│              Docker Infrastructure Services              │
│  PostgreSQL :54321   Redis :6379/16379*   Seq :5341/5342 │
│  pgAdmin :5050                                            │
└────────────────────────────────────────────────────────┘
```
\* `docker-compose.infrastructure.yml` maps Redis to host port `16379` (Windows reserves `6379` in some configurations); `appsettings.json`'s default `Redis` connection string still points at `6379` — adjust locally if you hit a port-in-use error.

## Setup

### 1. Start infrastructure

```powershell
cd C:\Repos\Booking
.\run-infrastructure.ps1   # starts docker-compose.infrastructure.yml: Postgres, Redis, Seq, pgAdmin
```

(This script's printed next-steps still mention the old Gateway/multi-project setup — ignore that part; just start `Booksy.Host` per step 2 below.)

### 2. Run Booksy.Host in Visual Studio

1. Open `Booksy.sln`
2. Set `Booksy.Host` as the startup project (single project — no "multiple startup projects" needed)
3. Select the **http** launch profile
4. Press **F5** — it starts on `http://localhost:5000` and opens Swagger

### 3. Run the frontend

```bash
cd booksy-frontend
npm run dev   # http://localhost:3000, Vite proxies /api to localhost:5000
```

## Debugging tips

- **Breakpoints**: set them directly in controllers, command/query handlers, or domain code in `Booksy.Host`'s composed bounded contexts — everything runs in one process, so a single F5 session covers UserManagement and ServiceCatalog code alike.
- **Logs**: Visual Studio's Output window shows the host's console log; for structured/searchable logs, use **Seq** at http://localhost:5341 (`admin` / `Booksy@2024!`).
- **Database**: inspect via **pgAdmin** at http://localhost:5050 (`admin@booksy.com` / `Booksy@2024!`) — add a server pointing at `localhost:54321`, database `booksy`. Or use Visual Studio's SQL Server Object Explorer with the Npgsql provider.
- **Redis**: `docker exec -it booksy-redis redis-cli -a Redis@2024!` (adjust the port per the note above), then `KEYS booksy:*` / `GET <key>`.
- **Integration events (CAP)**: no broker to inspect — query the `cap` schema in Postgres for outbox/inbox rows, or watch Seq for CAP's own log lines.

## Common issues

| Symptom | Cause | Fix |
| --- | --- | --- |
| `Failed to bind to address http://127.0.0.1:5000: address already in use` | Another process holds port 5000 | `netstat -ano \| findstr :5000` then `taskkill /F /PID <PID>` |
| Can't connect to PostgreSQL | Container not healthy | `docker ps \| findstr postgres`; `docker exec booksy-postgres pg_isready -U booksy_admin` |
| Can't connect to Redis | Wrong port (see note above) or container down | `docker exec booksy-redis redis-cli -a Redis@2024! ping` should return `PONG` |
| Database doesn't exist | Fresh Postgres volume | `docker exec -it booksy-postgres psql -U booksy_admin -c "CREATE DATABASE booksy;"` — migrations then run automatically at host startup |
| Frontend shows CORS errors | Backend not running, or CORS config out of date | Confirm `Booksy.Host` is up on :5000; CORS is configured for `localhost:3000` |

## Stopping services

```powershell
# Stop Visual Studio: Shift+F5, or close Visual Studio

# Stop infrastructure
cd C:\Repos\Booking
.\stop-infrastructure.ps1

# Stop everything AND delete data (careful):
docker-compose -f docker-compose.infrastructure.yml down -v
```

## Running the Flutter customer app against your local backend

```dart
// Android Emulator
static const String baseUrl = 'http://10.0.2.2:5000';

// Physical device (use your PC's LAN IP)
static const String baseUrl = 'http://192.168.1.x:5000';
```

```bash
cd booksy-customer-app
flutter run
```
