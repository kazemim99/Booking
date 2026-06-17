# Migration Plan: Microservices → Modular Monolith (MVP)

> Status: In progress. Generated 2026-06-13. This migrates the Booksy backend from
> per-service hosts + Ocelot gateway + RabbitMQ to a single ASP.NET Core host,
> one Postgres database (schema-per-context), and CAP running on its in-memory
> transport in-process. Bounded-context project structure is preserved (modular
> monolith, not a rewrite).

## Target shape

One deployable host (`Booksy.Host`, :5000) referencing every context's
Application + Infrastructure + Api (controllers), one Postgres DB with
schema-per-context, CAP in-memory transport in-process, no Ocelot gateway.

```
Booksy.Host (NEW single host, :5000)
├─ refs UserManagement.{Api*,Application,Infrastructure}
├─ refs ServiceCatalog.{Api*,Application,Infrastructure}
├─ refs Core.*, Infrastructure.{Core,External,Monitoring,Security}, Booksy.API (middleware lib)
└─ one Program.cs, one DbContext-per-context → one DB (schemas: user_management, ServiceCatalog, cap)
```

Controllers approach: **(A, chosen)** demote the two `.Api` projects to class
libraries (drop `Program.cs`/`Startup.cs`, keep controllers), host references both;
controllers auto-discovered.

## The 6 real challenges

1. **CAP registered once per process** — today `AddCap` is called twice (once per
   context). Unify into a single host-level `AddCap` (one Postgres outbox in `cap`
   schema, in-memory transport). `[CapSubscribe]` handlers are discovered across all
   referenced assemblies and keep working.
2. **Two bootstrap styles → one Program.cs** — UserManagement = minimal hosting;
   ServiceCatalog = legacy `Startup.cs` + `namespace Booksy.API { class Program }`
   (name collision). New single minimal-hosting `Program.cs`; delete both old entry
   points. Register both contexts' Application + Infrastructure.
3. **One DB, schema-per-context** — already: UM `user_management`, SC `ServiceCatalog`,
   CAP `cap`. Point both at one `DefaultConnection`; give each context its own
   migrations-history table; run both `Migrate()` at startup.
4. **Health checks & migration order** — merge DbContext + Redis checks; migrate+seed
   both contexts sequentially before `app.Run()`.
5. **Drop the gateway** — routes are collision-free (UM: Auth/Users/Customers/
   PhoneVerification; SC: Providers/Services/Categories/Bookings/Reviews/Payments).
   Frontend base URL → host directly. PascalCase preserved (Ocelot case bug gone).
6. **Config consolidation** — merge both `appsettings.json` (Serilog, Cache/Redis, CAP,
   Cors, Security/JWT, ConnectionStrings) into the host.

## Deployment changes

- `docker-compose.prod.yml`: replace `usermanagement-api` + `servicecatalog-api` +
  `gateway` with one `booksy-api` on :5000. RabbitMQ optional (CAP in-memory).
- `build-and-push.yml`: 3 image builds → 1. One Dockerfile (keep `curl`).

## Execution order

1. Create `Booksy.Host` + add to sln; reference Core/Infra/Security/Monitoring/External + Booksy.API.
2. Unify CAP into a single host-level registration. Build.
3. Demote `*.Api` → libraries, write merged `Program.cs`. Build + run + Swagger.
4. Single connection string + per-context migration history; verify both schemas migrate into one DB.
5. Merge configs.
6. Update frontend base URL; retire `Booksy.Gateway`.
7. Update docker-compose + CI + Dockerfile; smoke-test.
8. Update `CLAUDE.md` / `API_ENDPOINTS.md`.

## Not changed

Domain, Application handlers, repositories, value objects, `[CapSubscribe]` handler
bodies, EF entity configs. Contexts stay isolated by namespace/schema for future
re-extraction.

## Risks / call-outs

- CAP unification (#1) needs real code, not config — verify subscribers still fire E2E.
- `ArchitectureTests` may assert no cross-context refs / layering — check & adjust.
- Integration tests use `WebApplicationFactory<Program>` per service — retarget to host `Program`.
- **Booking context is empty** (only dirs, no `.csproj`/`.cs`, not in sln) — leave a
  registration seam, nothing to wire yet.
