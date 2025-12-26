# Booksy - Quick Reference Card

**Last Updated**: December 26, 2025

---

## 🎯 Current Status (One Line)
✅ ServiceCatalog fully working | ⏳ UserManagement needs manual migration | ⚠️ Gateway bypassed with direct routing

---

## ⚡ Most Important Commands

### Test If Everything Works
```bash
curl http://napstar.ir/api/v1/Platform/statistics
curl "http://napstar.ir/api/v1/Providers/search?PageNumber=1&PageSize=6"
```

### Check Service Health
```bash
docker ps | grep -E "booksy"
docker-compose -f /root/booksy/docker-compose.prod.yml ps
```

### View Logs (Last 50 Lines)
```bash
docker logs booksy-servicecatalog-api --tail 50
docker logs booksy-usermanagement-api-dev --tail 50
docker logs booksy-frontend --tail 50
```

### Restart Everything
```bash
cd /root/booksy
docker-compose -f docker-compose.prod.yml restart
```

### Full Redeployment
```bash
cd /root/booksy && ./scripts/deploy.sh
```

---

## 📊 What's Working Right Now

| Endpoint | Status | Returns |
|----------|--------|---------|
| `/api/v1/Platform/statistics` | ✅ | Provider/booking statistics |
| `/api/v1/Providers/search` | ✅ | List of 20 providers |
| `/api/v1/Categories/popular` | ✅ | Empty array (functional) |
| `/api/v1/Auth/*` | ⏳ | Pending UserManagement migration |

---

## 🗄️ Database Quick Check

```bash
# See all schemas
docker exec booksy-postgres psql -U booksy_admin -d booksy_user_management -c "\dn"

# Count ServiceCatalog tables (should be 24)
docker exec booksy-postgres psql -U booksy_admin -d booksy_user_management \
  -c "SELECT COUNT(*) FROM pg_tables WHERE schemaname = 'ServiceCatalog';"

# Count UserManagement tables (should be >0 after migration)
docker exec booksy-postgres psql -U booksy_admin -d booksy_user_management \
  -c "SELECT COUNT(*) FROM pg_tables WHERE schemaname = 'user_management';"
```

---

## ⏳ What Needs To Be Done Next

### 1. UserManagement Migration (Required)
**On your dev machine:**
```bash
cd /path/to/Booksy.UserManagement.API
dotnet ef database update \
  --connection "Host=5.223.59.167;Port=5432;Database=booksy_user_management;Username=booksy_admin;Password=YourSecurePassword123!"
```

**Then on server:**
```bash
docker stop booksy-usermanagement-api-dev
docker rm booksy-usermanagement-api-dev
docker-compose -f /root/booksy/docker-compose.prod.yml up -d usermanagement-api
```

---

## 🔧 Modified Files Location

```
/root/booksy/nginx/default.conf           # Custom nginx routing
/root/booksy/gateway/ocelot.production.json  # Gateway routes (ready but not used)
/root/booksy/docker-compose.prod.yml      # Added volume mounts
```

---

## 📝 Documentation Files

```
/root/booksy/CURRENT_STATUS_AND_NEXT_STEPS.md  # Complete session summary ⭐
/root/booksy/MIGRATION_AND_GATEWAY_GUIDE.md    # Implementation guides
/root/booksy/FIXES_SUMMARY.md                  # Initial fixes
/root/booksy/QUICK_REFERENCE.md                # This file
/root/booksy/CLAUDE.md                         # Project overview
```

---

## 🚨 If Something Breaks

### All Endpoints Return 502
```bash
# Check nginx config
docker exec booksy-frontend nginx -t

# Restart frontend
docker restart booksy-frontend
```

### Database Connection Errors
```bash
# Check PostgreSQL
docker logs booksy-postgres --tail 20

# Verify it's running
docker exec booksy-postgres psql -U booksy_admin -d booksy_user_management -c "SELECT version();"
```

### Container Won't Start
```bash
# Check logs
docker logs <container-name>

# Remove and recreate
docker stop <container-name>
docker rm <container-name>
docker-compose -f /root/booksy/docker-compose.prod.yml up -d <service-name>
```

---

## 🌐 Service URLs

- **Public API**: http://napstar.ir/api/v1/*
- **ServiceCatalog Direct**: http://5.223.59.167:5002/api/v1/*
- **UserManagement Direct**: http://5.223.59.167:5001/api/v1/*
- **Gateway Direct**: http://5.223.59.167:5000/api/v1/* (not working - bypassed)
- **Seq Logs**: http://5.223.59.167:5341
- **RabbitMQ UI**: http://5.223.59.167:15672
- **pgAdmin**: http://5.223.59.167:5050

---

## 🔐 Credentials

**Database**:
- User: `booksy_admin`
- Password: `YourSecurePassword123!`
- Database: `booksy_user_management`

**Redis**:
- Password: `YourRedisPassword123!`

**RabbitMQ**:
- User: `booksy_admin`
- Password: `YourRabbitMQPassword123!`

---

## 💡 Key Insights from This Session

1. **Migrations only run in Development mode** - `InitializeDatabaseAsync()` checks environment
2. **ServiceCatalog auto-migration worked**, UserManagement didn't (unknown reason)
3. **Direct nginx routing works perfectly** - Gateway integration optional
4. **Database uses single DB with multiple schemas** - Not separate databases per service
5. **Health checks need curl in containers** - Already included in images

---

**For full details, see**: `/root/booksy/CURRENT_STATUS_AND_NEXT_STEPS.md`
