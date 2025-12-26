# Quick Fix for 502 Bad Gateway Errors

## The Problem
Your services are returning 502 errors because the backend APIs (UserManagement, ServiceCatalog) are marked as **unhealthy** and the Gateway cannot route requests to them.

## Root Cause
All your Dockerfiles have curl installed ✅, but the issue might be:
1. Old Docker images without curl are being used
2. Services are failing to start properly
3. Health checks are timing out

## Quick Fix Steps

### Step 1: SSH into your server
```bash
ssh root@napstar.ir
cd /opt/booksy  # or your deployment path
```

### Step 2: Run the diagnostic script
Upload and run the diagnostic script:
```bash
# Download the diagnostic script
curl -O https://raw.githubusercontent.com/kazemim99/Booking/master/fix-502-error.sh
chmod +x fix-502-error.sh
./fix-502-error.sh
```

Or manually run these commands:

### Step 3: Check container health
```bash
docker ps
```

Look for containers with status like `unhealthy` or `starting`.

### Step 4: Check if curl is in the containers
```bash
# Check each service
docker exec booksy-usermanagement-api which curl
docker exec booksy-servicecatalog-api which curl
docker exec booksy-gateway which curl
docker exec booksy-frontend which curl
```

**Expected output:** `/usr/bin/curl`

**If you get "not found":** Your images are old and don't have curl!

### Step 5: Force pull latest images and redeploy

If curl is missing OR services are unhealthy:

```bash
cd /opt/booksy

# Login to GitHub Container Registry (use your GitHub token)
echo "YOUR_GITHUB_TOKEN" | docker login ghcr.io -u kazemim99 --password-stdin

# Force stop ALL containers
docker ps -a --filter "name=booksy-" --format "{{.Names}}" | xargs -r docker rm -f

# Remove old images to force fresh pull
docker images | grep booksy | awk '{print $3}' | xargs -r docker rmi -f

# Pull latest images (these have curl installed)
docker-compose -f docker-compose.prod.yml pull

# Start services
docker-compose -f docker-compose.prod.yml up -d

# Wait for health checks
echo "Waiting 60 seconds for services to become healthy..."
sleep 60

# Check status
docker ps
```

### Step 6: Verify health

All containers should show `(healthy)` in their status:

```bash
docker ps --format 'table {{.Names}}\t{{.Status}}'
```

Expected output:
```
NAMES                          STATUS
booksy-frontend                Up X minutes (healthy)
booksy-gateway                 Up X minutes (healthy)
booksy-servicecatalog-api      Up X minutes (healthy)
booksy-usermanagement-api      Up X minutes (healthy)
booksy-postgres                Up X minutes (healthy)
booksy-redis                   Up X minutes (healthy)
booksy-rabbitmq                Up X minutes (healthy)
```

### Step 7: Test the API

```bash
curl -v http://localhost:5000/health  # Gateway
curl -v http://localhost:5001/health  # UserManagement
curl -v http://localhost:5002/health  # ServiceCatalog
```

All should return `200 OK`.

## If It Still Doesn't Work

### Check logs for errors:
```bash
docker-compose -f docker-compose.prod.yml logs --tail=50 usermanagement-api
docker-compose -f docker-compose.prod.yml logs --tail=50 servicecatalog-api
docker-compose -f docker-compose.prod.yml logs --tail=50 gateway
```

### Common issues in logs:
1. **Database connection errors**: Check PostgreSQL is healthy and connection strings in `.env`
2. **Port conflicts**: Another service using ports 5000-5002
3. **Missing environment variables**: Check `.env` file exists and has all required vars
4. **Memory issues**: Run `docker stats` to check if containers are running out of memory

## Prevention

Your GitHub Actions workflow should automatically build images with curl. Ensure:

1. ✅ All Dockerfiles have curl installation (they do!)
2. ✅ Workflow builds and pushes images successfully
3. ✅ Server pulls the `:latest` tag on deployment

## Need More Help?

Share the output of these commands:
```bash
docker ps
docker-compose -f docker-compose.prod.yml logs --tail=100 usermanagement-api
docker-compose -f docker-compose.prod.yml logs --tail=100 servicecatalog-api
docker exec booksy-usermanagement-api curl -v http://localhost:80/health
```
