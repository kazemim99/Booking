# 🚀 Quick Start - Observability

## 5-Minute Setup Guide

### Step 1: Start Seq (30 seconds)
```bash
docker-compose up -d seq
```

### Step 2: Access Seq UI (10 seconds)
- Open: http://localhost:5341
- Login: admin / Booksy@2024!

### Step 3: Run an API (1 minute)
```bash
dotnet run --project src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api
```

### Step 4: Make a Test Request (30 seconds)
```bash
curl http://localhost:5010/health
```

### Step 5: View Logs in Seq (1 minute)
1. Go to http://localhost:5341
2. See real-time logs streaming in
3. Try searching: `Application = 'ServiceCatalog.API'`

---

## Common Commands

### Docker
```bash
# Start Seq
docker-compose up -d seq

# View Seq logs
docker logs booksy-seq

# Stop Seq
docker-compose stop seq

# Restart Seq
docker-compose restart seq
```

### Querying Logs in Seq
```
# All errors
@Level = 'Error'

# Specific application
Application = 'ServiceCatalog.API'

# Specific user
UserId = 'your-guid'

# Time range
@Timestamp > DateTime.Now.AddHours(-1)

# Combined
Application = 'ServiceCatalog.API' AND @Level = 'Error' AND @Timestamp > DateTime.Now.AddHours(-1)
```

---

## Access Points

| Service | URL | Credentials |
|---------|-----|-------------|
| Seq UI | http://localhost:5341 | admin / Booksy@2024! |
| Seq Ingestion | http://localhost:5342 | - |
| ServiceCatalog API | http://localhost:5010 | - |
| UserManagement API | http://localhost:5001 | - |

---

## Log Locations

```
logs/
├── ServiceCatalog.API-2025-01-15.log     # Text logs
├── ServiceCatalog.API-2025-01-15.json    # JSON logs
└── ...
```

---

## Troubleshooting

**Seq not starting?**
```bash
docker-compose down
docker-compose up -d seq
docker logs booksy-seq
```

**Logs not appearing?**
1. Check Seq URL in `appsettings.json`:
   ```json
   {"Observability":{"Seq":{"Url":"http://localhost:5342"}}}
   ```
2. Restart the API
3. Wait 10-15 seconds
4. Refresh Seq UI

**Need more help?**
See: `OBSERVABILITY-SETUP.md`

---

## Next Steps

1. ✅ Centralized Logging (Seq) - **DONE**
2. 📊 Metrics (Prometheus + Grafana) - Coming soon
3. 🔍 Distributed Tracing (Jaeger) - Coming soon
4. 🚨 Alerts (PagerDuty integration) - Coming soon

---

**Quick Links:**
- Full Guide: `OBSERVABILITY-SETUP.md`
- Implementation Details: `OBSERVABILITY-IMPLEMENTATION-SUMMARY.md`
- Test Script: `scripts/test-observability.ps1`
