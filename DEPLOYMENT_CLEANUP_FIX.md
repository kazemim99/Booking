# Deployment Network Error Fix

**Error**: `network booksy_booksy-network has active endpoints`

**Cause**: Temporary development container `booksy-usermanagement-api-dev` is still running and blocking network removal.

---

## 🚨 Immediate Fix (Run on Server)

SSH to your server and run these commands:

```bash
ssh root@5.223.59.167

# Navigate to deployment directory
cd /root/booksy

# Stop and remove the temporary dev container
docker stop booksy-usermanagement-api-dev
docker rm booksy-usermanagement-api-dev

# Also remove any other orphaned containers
docker-compose -f docker-compose.prod.yml down --remove-orphans

# Now start the services fresh
docker-compose -f docker-compose.prod.yml pull
docker-compose -f docker-compose.prod.yml up -d

# Verify all services are running
docker ps
```

---

## ✅ Expected Output

After running the commands above, you should see:

```bash
# docker ps output:
CONTAINER ID   IMAGE                                      STATUS
abc123...      ghcr.io/kazemim99/booksy-frontend:latest   Up (healthy)
def456...      ghcr.io/kazemim99/booksy-gateway:latest    Up
ghi789...      ghcr.io/kazemim99/booksy-servicecatalog... Up (healthy)
jkl012...      ghcr.io/kazemim99/booksy-usermanagement... Up (healthy)
mno345...      postgres:16-alpine                         Up (healthy)
pqr678...      redis:7-alpine                             Up (healthy)
stu901...      rabbitmq:3.13-management-alpine            Up (healthy)
vwx234...      datalust/seq:latest                        Up
yz5678...      dpage/pgadmin4:latest                      Up
```

**Note**: No `booksy-usermanagement-api-dev` container should be listed.

---

## 🔍 What Was the Issue?

During the previous troubleshooting session, we created a temporary container:
- `booksy-usermanagement-api-dev` - Development mode for migrations

This container was never removed and is blocking the deployment process.

---

## 🛠️ Prevent Future Issues

The `--remove-orphans` flag in the deployment script should handle this, but the dev container was created outside of docker-compose, so it wasn't tracked.

### Updated Deployment Workflow (Already in deploy.yml)

The workflow now includes:
```bash
docker-compose -f docker-compose.prod.yml down --remove-orphans
```

This will remove any orphaned containers in future deployments.

---

## 📋 Complete Cleanup Script

If you want to do a complete cleanup:

```bash
#!/bin/bash
# Run on server: /root/booksy/cleanup.sh

cd /root/booksy

echo "Stopping all Booksy containers..."
docker stop $(docker ps -a -q --filter "name=booksy-") 2>/dev/null || true

echo "Removing all Booksy containers..."
docker rm $(docker ps -a -q --filter "name=booksy-") 2>/dev/null || true

echo "Removing orphaned networks..."
docker network prune -f

echo "Removing unused images..."
docker image prune -af --filter "until=24h"

echo "Starting services fresh..."
docker-compose -f docker-compose.prod.yml pull
docker-compose -f docker-compose.prod.yml up -d

echo "Waiting for services to start..."
sleep 10

echo "Service status:"
docker-compose -f docker-compose.prod.yml ps

echo "Done!"
```

Save this as `/root/booksy/cleanup.sh` and run:
```bash
chmod +x /root/booksy/cleanup.sh
./cleanup.sh
```

---

## ✅ After Cleanup - Verify Gateway

Once the deployment succeeds, verify the gateway:

```bash
# Check gateway logs for Ocelot
docker logs booksy-gateway | grep -i "ocelot"

# Should see:
# Loading Ocelot configuration from: Configuration/ocelot.production.json
# Starting Ocelot API Gateway...

# Test gateway routing
curl http://localhost:5000/api/v1/Platform/statistics

# Test public URL
curl http://napstar.ir/api/v1/platform/statistics
```

---

## 🎯 Summary

1. **Immediate action**: Stop and remove `booksy-usermanagement-api-dev`
2. **Run deployment**: Use `docker-compose down --remove-orphans` first
3. **Verify**: Check that gateway loads Ocelot and routes work

---

**End of Document**
