# Docker Compose Configuration Review & Improvements

## Summary of Changes

I've reviewed and improved your Docker Compose production configuration. Here are the key changes made:

## ✅ **Improvements Applied**

### 1. **Security Enhancements**

#### **Port Binding Security**
```yaml
# BEFORE (exposed to internet)
ports:
  - "5432:5432"
  - "6379:6379"
  - "5672:5672"

# AFTER (only localhost access)
ports:
  - "127.0.0.1:5432:5432"  # PostgreSQL
  - "127.0.0.1:6379:6379"  # Redis
  - "127.0.0.1:5672:5672"  # RabbitMQ
```

**Impact**: Database and cache services are no longer exposed to the internet, only accessible from the host machine.

### 2. **Health Check Fixes**

#### **Fixed Health Checks to Use `wget` Instead of `curl`**
Alpine-based images don't include `curl` by default, but they do have `wget`.

```yaml
# BEFORE (would fail)
healthcheck:
  test: ["CMD", "curl", "-f", "http://localhost:80/health"]

# AFTER (works with Alpine)
healthcheck:
  test: ["CMD-SHELL", "wget --no-verbose --tries=1 --spider http://localhost:80/health || exit 1"]
```

#### **Fixed Redis Health Check**
```yaml
# BEFORE (incorrect command)
healthcheck:
  test: ["CMD", "redis-cli", "--raw", "incr", "ping"]

# AFTER (correct ping command)
healthcheck:
  test: ["CMD", "redis-cli", "ping"]
```

#### **Improved RabbitMQ Health Check**
```yaml
# BEFORE
healthcheck:
  test: ["CMD", "rabbitmqctl", "status"]

# AFTER (faster and more reliable)
healthcheck:
  test: ["CMD", "rabbitmq-diagnostics", "-q", "ping"]
```

### 3. **Resource Management**

Added resource limits and reservations for all services to prevent memory issues:

```yaml
deploy:
  resources:
    limits:
      cpus: '1.0'
      memory: 1G
    reservations:
      cpus: '0.25'
      memory: 256M
```

**Resource Allocation by Service:**

| Service | CPU Limit | Memory Limit | CPU Reserve | Memory Reserve |
|---------|-----------|--------------|-------------|----------------|
| UserManagement API | 1.0 | 1G | 0.25 | 256M |
| ServiceCatalog API | 1.0 | 1G | 0.25 | 256M |
| Gateway | 0.5 | 512M | 0.1 | 128M |
| Frontend | 0.5 | 256M | 0.1 | 64M |
| PostgreSQL | 2.0 | 2G | 0.5 | 512M |
| Redis | 0.5 | 512M | 0.1 | 128M |
| RabbitMQ | 1.0 | 1G | 0.25 | 256M |
| Seq | 0.5 | 512M | 0.1 | 128M |
| pgAdmin | 0.5 | 512M | 0.1 | 128M |

**Total Maximum Resources:**
- **CPUs**: ~8 cores max
- **Memory**: ~8GB max

### 4. **Environment File Integration**

Added `env_file` directive to all services for proper environment variable loading:

```yaml
services:
  usermanagement-api:
    env_file:
      - .env
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      # ... other env vars
```

**Impact**: The `.env` file is now properly loaded by Docker Compose on the server.

### 5. **Dependency Management**

Improved service dependencies with health conditions:

```yaml
# BEFORE
depends_on:
  - postgres
  - redis

# AFTER
depends_on:
  postgres:
    condition: service_healthy
  redis:
    condition: service_healthy
```

**Impact**: Services wait for dependencies to be healthy before starting, preventing startup failures.

### 6. **Redis Configuration**

Added memory management and eviction policy:

```yaml
command: redis-server --requirepass ${REDIS_PASSWORD} --maxmemory 512mb --maxmemory-policy allkeys-lru
```

**Impact**: Redis won't consume unlimited memory and will automatically evict old keys when full.

### 7. **Network Configuration**

Added explicit subnet configuration:

```yaml
networks:
  booksy-network:
    driver: bridge
    ipam:
      config:
        - subnet: 172.25.0.0/16
```

**Impact**: Predictable IP addressing for containers.

### 8. **Added pgAdmin**

Added pgAdmin service for easy database management in production:

```yaml
pgadmin:
  image: dpage/pgadmin4
  container_name: booksy-pgadmin
  ports:
    - "5050:80"
  # ... configuration
```

**Access**: `http://YOUR_SERVER_IP:5050`

### 9. **Health Check Start Periods**

Added `start_period` to all health checks to prevent false negatives during startup:

```yaml
healthcheck:
  test: ["CMD-SHELL", "..."]
  interval: 30s
  timeout: 10s
  retries: 3
  start_period: 40s  # NEW: Grace period for startup
```

## 📊 **Comparison: Before vs After**

### **Before**
❌ Database ports exposed to internet
❌ Health checks using unavailable `curl`
❌ No resource limits (could crash server)
❌ No dependency health checks
❌ Redis could consume all memory
❌ No environment file integration
❌ No database management tool

### **After**
✅ Database ports only on localhost
✅ Health checks using `wget` (available in Alpine)
✅ Resource limits prevent memory issues
✅ Services wait for healthy dependencies
✅ Redis has memory limits and eviction
✅ Environment file properly loaded
✅ pgAdmin included for DB management

## 🔧 **Configuration Files Updated**

1. **[docker-compose.prod.yml](docker-compose.prod.yml)** - Production configuration
2. **[deployment/.env.production.example](deployment/.env.production.example)** - Environment template

## 📝 **Action Items**

### **On Your Server:**

1. Update the `.env` file with pgAdmin credentials:
   ```bash
   sudo nano /opt/booksy/.env
   ```

   Add these lines:
   ```env
   PGADMIN_EMAIL=admin@booksy.local
   PGADMIN_PASSWORD=YourSecurePgAdminPassword123!
   ```

2. Test the updated configuration:
   ```bash
   cd /opt/booksy
   docker-compose -f docker-compose.prod.yml config
   ```

3. When ready to deploy with new configuration:
   ```bash
   docker-compose -f docker-compose.prod.yml down
   docker-compose -f docker-compose.prod.yml pull
   docker-compose -f docker-compose.prod.yml up -d
   ```

4. Verify all services are healthy:
   ```bash
   docker-compose -f docker-compose.prod.yml ps
   ```

### **In Your Repository:**

1. Commit these changes:
   ```bash
   git add .
   git commit -m "Improve Docker Compose production configuration

   - Fix health checks to use wget instead of curl
   - Add resource limits to prevent memory issues
   - Secure database ports (localhost only)
   - Add proper dependency health checks
   - Add Redis memory management
   - Include pgAdmin for database management
   - Add environment file integration"
   ```

2. Push and deploy:
   ```bash
   git push origin master
   ```

## 🚨 **Important Notes**

### **Port Security**
The following ports are now **only** accessible from localhost:
- PostgreSQL: `127.0.0.1:5432`
- Redis: `127.0.0.1:6379`
- RabbitMQ AMQP: `127.0.0.1:5672`

To access these from your local machine, use SSH tunneling:
```bash
# PostgreSQL
ssh -L 5432:127.0.0.1:5432 user@your-server-ip

# Redis
ssh -L 6379:127.0.0.1:6379 user@your-server-ip
```

### **Resource Monitoring**
Monitor resource usage with:
```bash
# Real-time stats
docker stats

# Check if limits are being hit
docker inspect <container_name> | grep -A 10 "Memory"
```

### **Memory Considerations**
The configuration reserves ~8GB of memory maximum. Ensure your server has:
- **Minimum**: 8GB RAM
- **Recommended**: 16GB RAM (for headroom)

## 🔍 **Testing Checklist**

After deploying the updated configuration:

- [ ] All containers start successfully
- [ ] All health checks pass (green/healthy status)
- [ ] Frontend accessible at `http://YOUR_SERVER_IP`
- [ ] API Gateway responds at `http://YOUR_SERVER_IP:5000`
- [ ] Seq logging dashboard accessible at `http://YOUR_SERVER_IP:5341`
- [ ] pgAdmin accessible at `http://YOUR_SERVER_IP:5050`
- [ ] RabbitMQ management at `http://YOUR_SERVER_IP:15672`
- [ ] Services can connect to database
- [ ] Services can connect to Redis
- [ ] Services can connect to RabbitMQ

## 📚 **Additional Resources**

- [Docker Compose Health Check Documentation](https://docs.docker.com/compose/compose-file/compose-file-v3/#healthcheck)
- [Docker Resource Constraints](https://docs.docker.com/config/containers/resource_constraints/)
- [Redis Memory Management](https://redis.io/docs/manual/eviction/)

## ❓ **FAQ**

**Q: Will this break my existing deployment?**
A: No, but you need to redeploy with `docker-compose up -d` to apply changes.

**Q: What if I need more memory for a service?**
A: Edit the `docker-compose.prod.yml` file and increase the limits for that service.

**Q: Can I access the database from my local machine?**
A: Yes, use SSH port forwarding: `ssh -L 5432:127.0.0.1:5432 user@server-ip`

**Q: Why use wget instead of curl?**
A: Alpine Linux (used in most official Docker images) includes wget by default but not curl, saving space.

**Q: What's the purpose of start_period?**
A: It gives services time to initialize before health checks count as failures, preventing false alarms during startup.
