# Ubuntu Server Deployment Setup Guide

This guide will help you deploy your Booksy application to your Ubuntu server with automatic CI/CD from GitHub.

## Architecture Overview

The application uses a containerized microservices architecture with Docker Compose:

```
┌─────────────────────────────────────────────────────────┐
│                        INTERNET                          │
└──────────────────────────┬──────────────────────────────┘
                           │
                           ▼
        ┌──────────────────────────────────────┐
        │      Nginx Reverse Proxy              │
        │   (SSL/HTTPS Termination)             │
        │   Domain: napstar.ir                  │
        └──────────────────────────────────────┘
                           │
        ┌──────────────────┴──────────────────┐
        │                                      │
        ▼                                      ▼
  ┌──────────────┐                    ┌──────────────┐
  │   Frontend   │                    │   Seq        │
  │   (port 80)  │                    │   (port 5341)│
  └──────────────┘                    └──────────────┘
        │
        └─────────────────────────────────────┐
                                              │
                    ┌─────────────────────────┤
                    │                         │
        ┌───────────▼──────────┐      ┌──────▼───────────┐
        │   API Gateway        │      │   pgAdmin        │
        │   (port 8000)        │      │   (port 5050)    │
        └──────────┬───────────┘      └──────────────────┘
                   │
     ┌─────────────┼──────────────┐
     │             │              │
     ▼             ▼              ▼
┌─────────┐  ┌──────────┐   ┌──────────┐
│ User    │  │ Service  │   │ Cache &  │
│ Mgmt    │  │ Catalog  │   │ Messaging│
│ (8001)  │  │ (8002)   │   │ (Redis,  │
└────┬────┘  └────┬─────┘   │ RabbitMQ)│
     │            │         └──────────┘
     └────────────┼─────────────┬────────┘
                  │             │
                  ▼             ▼
            ┌──────────────────────────┐
            │   PostgreSQL Database    │
            │    (port 54321)          │
            └──────────────────────────┘
```

## Prerequisites

- Ubuntu 20.04 LTS or newer
- Public IP address or domain name
- SSH access to your server
- GitHub account with repository access
- Docker and Docker Compose installed

---

## Step 1: Initial Server Setup

### 1.1 Connect to Your Server
```bash
ssh root@YOUR_SERVER_IP
# or
ssh your_username@YOUR_SERVER_IP
```

### 1.2 Update System
```bash
sudo apt update
sudo apt upgrade -y
```

### 1.3 Create Application User (Optional but Recommended)
```bash
sudo useradd -m -s /bin/bash deployer
sudo usermod -aG sudo deployer
su - deployer
```

---

## Step 2: Install Docker and Docker Compose

### 2.1 Install Docker
```bash
sudo apt install -y apt-transport-https ca-certificates curl software-properties-common
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo apt-key add -
sudo add-apt-repository "deb [arch=amd64] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable"
sudo apt update
sudo apt install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin
```

### 2.2 Verify Docker Installation
```bash
docker --version
docker run hello-world
```

### 2.3 Add User to Docker Group (Skip if using root)
```bash
sudo usermod -aG docker deployer
newgrp docker
```

---

## Step 3: Setup SSH Key for GitHub Actions

### 3.1 Create SSH Key Pair on Server
```bash
# Login to your server first
ssh root@YOUR_SERVER_IP
# or
ssh deployer@YOUR_SERVER_IP

# Create SSH directory if it doesn't exist
mkdir -p ~/.ssh
chmod 700 ~/.ssh

# Generate SSH key pair (RSA is more compatible)
ssh-keygen -t rsa -b 4096 -f ~/.ssh/github_deploy -N ""

# For deployer user specifically:
# ssh-keygen -t rsa -b 4096 -f /home/deployer/.ssh/github_deploy -N ""
```

### 3.2 Add Public Key to Authorized Keys
**CRITICAL STEP**: The public key must be added to the server's `authorized_keys` file:

```bash
# Add the public key to authorized_keys
cat ~/.ssh/github_deploy.pub >> ~/.ssh/authorized_keys

# Set proper permissions
chmod 600 ~/.ssh/authorized_keys
chmod 600 ~/.ssh/github_deploy
chmod 644 ~/.ssh/github_deploy.pub

# Verify the key was added
cat ~/.ssh/authorized_keys
```

### 3.3 Test SSH Connection Locally
Before using in GitHub Actions, test the connection:

```bash
# From another terminal or your local machine
ssh -i /path/to/github_deploy deployer@YOUR_SERVER_IP

# If this works, the key is configured correctly
```

### 3.4 Copy Private Key for GitHub Secrets
```bash
# Display the private key (you'll copy this entire output)
cat ~/.ssh/github_deploy
```

You should see output like:
```
-----BEGIN OPENSSH PRIVATE KEY-----
b3BlbnNzaC1rZXktdjEAAAAABG5vbmUAAAAEbm9uZQAAAAAAAAABAAACFwAAAAdzc2gtcn
...
-----END OPENSSH PRIVATE KEY-----
```

### 3.5 Add Private Key to GitHub Secrets
1. Copy the ENTIRE private key output from the previous step
2. Go to: `https://github.com/kazemim99/Booking/settings/secrets/actions`
3. Click "New repository secret"
4. Name: `SERVER_SSH_KEY`
5. Value: Paste the entire private key (including the BEGIN and END lines)
6. Click "Add secret"

**IMPORTANT**:
- Make sure there are NO extra spaces or newlines at the beginning or end
- The key should start with `-----BEGIN` and end with `-----END`
- Include everything between and including these markers

### 3.6 Get Server Information for GitHub Secrets
Create these additional secrets in GitHub:
1. `SERVER_HOST`: Your server IP (e.g., `192.168.1.100` or your public IP)
2. `SERVER_USER`: `deployer` (or your username - must match the user whose `authorized_keys` has the public key)
3. `SERVER_DEPLOY_PATH`: `/home/deployer/booksy` (or your preferred path)

**To add these secrets:**
1. Go to: `https://github.com/kazemim99/Booking/settings/secrets/actions`
2. Add each secret separately
3. Example for SERVER_HOST:
   - Name: `SERVER_HOST`
   - Value: `YOUR_ACTUAL_IP_ADDRESS`

### 3.7 Verify All GitHub Secrets
You should now have these 4 secrets configured:
- ✅ `SERVER_SSH_KEY` (private key from step 3.5)
- ✅ `SERVER_HOST` (server IP)
- ✅ `SERVER_USER` (SSH username)
- ✅ `SERVER_DEPLOY_PATH` (deployment directory path)

---

## Step 4: Create Deployment Directory on Server

### 4.1 Create Project Directory
```bash
# This path MUST match your SERVER_DEPLOY_PATH secret in GitHub
mkdir -p /home/deployer/booksy
cd /home/deployer/booksy

# If using root:
sudo mkdir -p /root/booksy
cd /root/booksy
```

### 4.2 Create .env File
**CRITICAL**: The `.env` file must be created in the deployment directory (`/home/deployer/booksy/.env`)

```bash
# Navigate to deployment directory first
cd /home/deployer/booksy

# Create .env file with your configuration
cat > .env << 'EOF'
# Database Configuration
POSTGRES_USER=booksy_admin
POSTGRES_PASSWORD=YourSecurePassword123!
POSTGRES_DB=booksy_user_management
POSTGRES_INITDB_ARGS=--encoding=UTF-8 --lc-collate=C --lc-ctype=C

# Redis
REDIS_PASSWORD=YourRedisPassword123!

# RabbitMQ
RABBITMQ_DEFAULT_USER=booksy_admin
RABBITMQ_DEFAULT_PASS=YourRabbitMQPassword123!

# Seq Logging
SEQ_FIRSTRUN_ADMINUSERNAME=admin
SEQ_FIRSTRUN_ADMINPASSWORD=YourSeqPassword123!
ACCEPT_EULA=Y

# pgAdmin
PGADMIN_DEFAULT_EMAIL=admin@example.com
PGADMIN_DEFAULT_PASSWORD=YourPgAdminPassword123!

# GitHub Container Registry
# Note: You'll need to login separately using docker login command
# See Step 7.1 for docker login instructions
GITHUB_REPOSITORY_OWNER=kazemim99

# Service Configuration (for APIs running in Docker)
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
EOF
```

### 4.3 Set Proper Permissions
```bash
# Ensure you're in the deployment directory
cd /home/deployer/booksy
chmod 600 .env

# Fix ownership if needed (if created as root)
sudo chown deployer:deployer .env

# Verify the file exists and has correct permissions
ls -la .env
# Should show: -rw------- 1 deployer deployer
```

**If you created the .env file as root and see:**
```
-rw------- 1 root docker 836 Dec 25 08:10 .env
```

**Fix it with:**
```bash
sudo chown deployer:deployer /home/deployer/booksy/.env
ls -la .env
# Now should show: -rw------- 1 deployer deployer
```

### 4.4 Verify .env File Location
**IMPORTANT**: The `.env` file MUST be at: `/home/deployer/booksy/.env` (or your `SERVER_DEPLOY_PATH`)

```bash
# Check if .env exists in the correct location
ls -la /home/deployer/booksy/.env

# Verify you can read it
cat /home/deployer/booksy/.env | head -n 5

# This should display the first 5 lines of your environment variables
# If you see "No such file or directory", the file is in the wrong place!
```

---

## Step 5: Setup Nginx Reverse Proxy (Optional but Recommended)

### 5.1 Install Nginx
```bash
sudo apt install -y nginx
```

### 5.2 Create Nginx Configuration

**NOTE**: With the new Docker Compose setup, the frontend service runs with Nginx built-in, and all services are in Docker containers. You have two options:

**Option A: Use Docker Nginx (Recommended for Docker deployment)**

If you're running everything through Docker Compose (which includes the frontend with Nginx), you only need an external Nginx for SSL/HTTPS:

```bash
sudo tee /etc/nginx/sites-available/booksy > /dev/null << 'EOF'
upstream booksy_frontend {
    server localhost:80;
}

upstream booksy_seq {
    server localhost:5341;
}

server {
    listen 80;
    server_name napstar.ir www.napstar.ir;

    client_max_body_size 50M;

    # Frontend (already served with Nginx in Docker)
    location / {
        proxy_pass http://booksy_frontend;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # Seq Logging UI
    location /logs/ {
        proxy_pass http://booksy_seq/;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
EOF
```

**Option B: Use docker-compose.prod.yml instead**

If you want all services (including SSL) managed by Docker, skip Nginx setup and let Docker Compose handle everything. The docker-compose.prod.yml file should include all services with proper port mappings.

For this guide, we'll use **Option A** - external Nginx for SSL termination.

### 5.3 Enable Nginx Configuration
```bash
sudo rm -f /etc/nginx/sites-enabled/default
sudo ln -sf /etc/nginx/sites-available/booksy /etc/nginx/sites-enabled/booksy
sudo nginx -t
sudo systemctl restart nginx
```

### 5.4 Setup SSL with Let's Encrypt (HTTPS)
```bash
sudo apt install -y certbot python3-certbot-nginx
sudo certbot certonly --nginx -d napstar.ir
```

Then update the nginx config with SSL settings and restart:
```bash
sudo systemctl restart nginx
```

---

## Step 6: GitHub Actions Deployment Workflow

Your deployment workflow is already in place (`.github/workflows/deploy.yml`), but you need to ensure GitHub Secrets are set:

### Required GitHub Secrets:
1. **SERVER_HOST** - Your server IP/domain
2. **SERVER_USER** - Deploy user (e.g., `deployer`)
3. **SERVER_SSH_KEY** - Private SSH key for authentication
4. **SERVER_DEPLOY_PATH** - `/home/deployer/booksy`
5. **GITHUB_TOKEN** - Auto-provided by GitHub Actions

Check if these are already configured in your repo at:
```
https://github.com/kazemim99/Booking/settings/secrets/actions
```

---

## Step 7: Deploy Application

### 7.1 Manual First Deployment
```bash
cd /home/deployer/booksy

# Copy docker-compose files from repo
# Use the main docker-compose.yml (includes all APIs and frontend)
wget https://raw.githubusercontent.com/kazemim99/Booking/master/docker-compose.yml -O docker-compose.prod.yml

# Or get the production variant if available:
# wget https://raw.githubusercontent.com/kazemim99/Booking/master/docker-compose.prod.yml

# Create .env file (from Step 4.2)

# Login to GitHub Container Registry (if using pre-built images)
echo "YOUR_GITHUB_PAT" | docker login ghcr.io -u kazemim99 --password-stdin

# Build and run containers
docker-compose -f docker-compose.prod.yml up -d --build

# Or if using pre-built images from GitHub Container Registry:
# docker-compose -f docker-compose.prod.yml pull
# docker-compose -f docker-compose.prod.yml up -d

# Check status
docker-compose -f docker-compose.prod.yml ps
```

**New Docker Compose Structure** (as of latest update):
- `booksy-frontend` - Vue.js frontend with Nginx (port 80)
- `booksy-gateway` - API Gateway (port 8000)
- `booksy-user-management-api` - User Management API (port 8001)
- `booksy-service-catalog-api` - Service Catalog API (port 8002)
- `postgres` - PostgreSQL database (port 54321)
- `redis` - Redis cache (port 6379)
- `rabbitmq` - RabbitMQ message broker (ports 5672, 15672)
- `pgadmin` - PostgreSQL admin UI (port 5050)
- `seq` - Structured event logging (port 5341)

### 7.2 Automatic Deployment (After Push)
Every time you push to `master` branch:
1. GitHub Actions tests and builds Docker images
2. Pushes images to GitHub Container Registry
3. SSHs into your server
4. Pulls latest images
5. Restarts containers with new version

Just push code:
```bash
git push origin master
```

Monitor deployment:
- Go to: `https://github.com/kazemim99/Booking/actions`
- Watch the "Deploy to Production" workflow

---

## Step 8: Verify Deployment

### 8.1 Check Container Status
```bash
docker-compose -f /home/deployer/booksy/docker-compose.prod.yml ps
```

### 8.2 Check Logs
```bash
# All services
docker-compose -f /home/deployer/booksy/docker-compose.prod.yml logs -f

# Specific service
docker-compose -f /home/deployer/booksy/docker-compose.prod.yml logs -f frontend
docker-compose -f /home/deployer/booksy/docker-compose.prod.yml logs -f gateway
```

### 8.3 Access Your Application

**Option 1: Direct IP (Docker Compose ports)**
```
http://YOUR_SERVER_IP              # Frontend (port 80)
http://YOUR_SERVER_IP:8000         # API Gateway
http://YOUR_SERVER_IP:8001         # UserManagement API
http://YOUR_SERVER_IP:8002         # ServiceCatalog API
http://YOUR_SERVER_IP:5341         # Seq Logs
http://YOUR_SERVER_IP:5050         # pgAdmin
http://YOUR_SERVER_IP:54321        # PostgreSQL (port 54321)
http://YOUR_SERVER_IP:6379         # Redis (port 6379)
```

**Option 2: With Domain (After Nginx setup - Recommended)**
```
http://napstar.ir                  # Frontend via Nginx
http://napstar.ir/api/             # API calls (proxied to Gateway)
http://napstar.ir/logs/            # Seq Logs
```

**Note**: The frontend at `http://napstar.ir` internally proxies `/api/` requests to the API Gateway at `booksy-gateway:8080` within the Docker network.

---

## Troubleshooting

### Container Health Check Failures

If you see 504 Gateway Timeout errors or containers stuck in unhealthy state:

**Symptoms:**
- Gateway or Frontend show as "unhealthy" in `docker ps`
- 504 errors when accessing the application
- Containers restart repeatedly

**Root Cause:**
Health checks fail when `curl` or `wget` are not installed in Docker images.

**Solution (Already Applied):**
The following changes have been made to fix this issue:

1. **Curl installed in all images:**
   - .NET images: `apt-get install -y curl`
   - Frontend (nginx): `apk add --no-cache curl`

2. **Health check commands updated:**
   ```dockerfile
   # Frontend Dockerfile
   HEALTHCHECK CMD curl -f http://localhost:80/ || exit 1
   ```

3. **Dependencies adjusted in docker-compose.prod.yml:**
   - Gateway depends on backend services with `service_started` (not `service_healthy`)
   - Frontend depends on Gateway with `service_started`

**To verify health checks are working:**
```bash
# Check container health status
docker ps
# Look for "(healthy)" status next to container names

# Test health check manually
docker exec booksy-gateway curl -f http://localhost:80/health
docker exec booksy-frontend curl -f http://localhost:80/

# View health check logs
docker inspect booksy-gateway --format='{{json .State.Health}}' | jq
```

**If issues persist after deployment:**
```bash
# Rebuild images with curl installed
docker compose -f docker-compose.prod.yml pull
docker compose -f docker-compose.prod.yml up -d --force-recreate

# Check logs for health check errors
docker compose -f docker-compose.prod.yml logs gateway
```

### SSH Authentication Errors in GitHub Actions

If you see errors like:
- `error: can't connect without a private SSH key or password`
- `ssh: handshake failed: ssh: unable to authenticate, attempted methods [none publickey]`

**Follow these steps on your server:**

1. **Verify public key is in authorized_keys:**
   ```bash
   # On your server
   cat ~/.ssh/authorized_keys
   # You should see your github_deploy.pub key listed here
   ```

2. **Check file permissions (CRITICAL):**
   ```bash
   # Fix permissions if needed
   chmod 700 ~/.ssh
   chmod 600 ~/.ssh/authorized_keys
   chmod 600 ~/.ssh/github_deploy
   chmod 644 ~/.ssh/github_deploy.pub

   # Verify ownership
   ls -la ~/.ssh/
   # Should show: deployer deployer (or your username)
   ```

3. **Regenerate and re-add the key:**
   ```bash
   # On server
   cd ~/.ssh
   ssh-keygen -t rsa -b 4096 -f github_deploy -N ""
   cat github_deploy.pub >> authorized_keys
   chmod 600 authorized_keys

   # Copy the private key
   cat github_deploy
   ```

   Then update the `SERVER_SSH_KEY` secret in GitHub with the new private key.

4. **Check SSH server configuration:**
   ```bash
   # Ensure PubkeyAuthentication is enabled
   sudo grep "PubkeyAuthentication" /etc/ssh/sshd_config
   # Should show: PubkeyAuthentication yes

   # If it's set to 'no' or commented out:
   sudo nano /etc/ssh/sshd_config
   # Uncomment and set: PubkeyAuthentication yes

   # Restart SSH service
   sudo systemctl restart sshd
   ```

5. **Test SSH connection manually:**
   ```bash
   # From your local machine (copy private key locally first)
   ssh -i github_deploy -v deployer@YOUR_SERVER_IP
   # The -v flag shows verbose output for debugging
   ```

### SSH Connection Issues
```bash
# Test SSH connection
ssh -i ~/.ssh/github_deploy deployer@YOUR_SERVER_IP

# Check SSH agent
ssh-add -l

# Add key to agent
ssh-add ~/.ssh/github_deploy

# View SSH logs on server
sudo tail -f /var/log/auth.log
# Look for authentication failures
```

### Docker Login Issues

**To create a GitHub Personal Access Token (PAT) for Container Registry:**

1. **Go to GitHub Settings:**
   - Navigate to: [https://github.com/settings/tokens](https://github.com/settings/tokens)
   - OR: Click your profile picture → Settings → Developer settings → Personal access tokens → Tokens (classic)

2. **Generate new token (classic):**
   - Click "Generate new token" → "Generate new token (classic)"
   - Give it a descriptive name (e.g., "Booksy Container Registry")
   - Set expiration (recommend: 90 days or No expiration for production)

3. **Select required scopes/permissions:**
   - ✅ `read:packages` - Download packages from GitHub Package Registry
   - ✅ `write:packages` - Upload packages to GitHub Package Registry
   - ✅ `delete:packages` - Delete packages from GitHub Package Registry
   - ✅ `repo` - Full control of private repositories (needed for private repos)

4. **Generate and copy the token:**
   - Click "Generate token"
   - **IMPORTANT:** Copy the token immediately - you won't be able to see it again!

5. **Use the token on your server:**
   ```bash
   # Login to GitHub Container Registry
   # Replace ghp_xxxx... with your actual token (starts with ghp_ or github_pat_)
   echo "ghp_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx" | docker login ghcr.io -u kazemim99 --password-stdin

   # Test the login
   docker pull ghcr.io/kazemim99/booksy-frontend:latest
   ```

   **Note:** Your actual token will be a long string starting with `ghp_` or `github_pat_` that you copied from GitHub in step 4.

**Alternative: Fine-grained tokens (Beta)**
- Navigate to: [https://github.com/settings/tokens?type=beta](https://github.com/settings/tokens?type=beta)
- Click "Generate new token"
- Repository access: Select "Only select repositories" → Choose "Booking"
- Permissions:
  - Repository permissions → Contents: Read-only
  - Repository permissions → Packages: Read and write
- Generate token and copy it

### Missing .env File Error

If you see errors in GitHub Actions like:
```
err: Couldn't find env file: ***/deployment-package/.env
err: The POSTGRES_USER variable is not set. Defaulting to a blank string.
```

This means the `.env` file is missing on your server. **Fix:**

```bash
# 1. SSH into your server
ssh deployer@YOUR_SERVER_IP

# 2. Navigate to deployment directory (must match SERVER_DEPLOY_PATH)
cd /home/deployer/booksy

# 3. Check if .env exists
ls -la .env

# 4. If missing, create it following Step 4.2 of this guide
# Copy the .env template from Step 4.2 and customize the values

# 5. Verify the file exists and has correct permissions
ls -la .env
# Should show: -rw------- 1 deployer deployer

# 6. Verify you're in the right directory
pwd
# Should show: /home/deployer/booksy (or your SERVER_DEPLOY_PATH)
```

### Nginx Configuration Errors

If you see errors like:
```
nginx: configuration file /etc/nginx/nginx.conf test failed
[emerg] "location" directive is not allowed here
```

This means your nginx config has syntax errors. **Fix:**

```bash
# 1. Test nginx config to see the exact error
sudo nginx -t

# 2. Edit the config file
sudo nano /etc/nginx/sites-available/booksy

# 3. Verify the file structure:
#    - All "server {" blocks should have matching "}"
#    - All "location" blocks must be INSIDE a "server" block
#    - No location blocks should be outside server blocks

# 4. Check for common issues:
#    - Extra spaces or indentation errors
#    - Unclosed braces
#    - Location directives outside server blocks

# 5. After fixing, test again
sudo nginx -t

# 6. If test passes, restart nginx
sudo systemctl restart nginx
```

### Docker Pull Errors (denied: denied)

If you see errors like:
```
ERROR: Head "https://ghcr.io/v2/kazemim99/booksy-usermanagement/manifests/latest": denied: denied
```

This means you're not authenticated with GitHub Container Registry. **Fix:**

```bash
# 1. Create a GitHub PAT if you haven't already
# Go to: https://github.com/settings/tokens
# Create a token with: read:packages, write:packages, repo permissions

# 2. Login to GitHub Container Registry (ON YOUR SERVER)
echo "YOUR_ACTUAL_TOKEN_HERE" | docker login ghcr.io -u kazemim99 --password-stdin

# 3. Verify login succeeded (should show "Login Succeeded")

# 4. Now try pulling again
docker-compose -f docker-compose.prod.yml pull
```

**Check if images exist:**
```bash
# Verify images were built and pushed by GitHub Actions
# Go to: https://github.com/kazemim99?tab=packages&repo_name=Booking

# If images don't exist, trigger a build:
# Push to master branch or run the workflow manually at:
# https://github.com/kazemim99/Booking/actions/workflows/deploy.yml
```

### Container Won't Start
```bash
# Check Docker logs
docker-compose -f docker-compose.prod.yml logs

# Rebuild and restart
docker-compose -f docker-compose.prod.yml down
docker-compose -f docker-compose.prod.yml pull
docker-compose -f docker-compose.prod.yml up -d
```

### Nginx Not Forwarding
```bash
# Test Nginx config
sudo nginx -t

# Check Nginx logs
sudo tail -f /var/log/nginx/error.log
sudo tail -f /var/log/nginx/access.log
```

---

## Maintenance Tasks

### Update Containers Manually
```bash
cd /home/deployer/booksy
docker-compose -f docker-compose.prod.yml pull
docker-compose -f docker-compose.prod.yml up -d
```

### View Database
```bash
# pgAdmin: http://SERVER_IP:5050
# Default credentials in .env file
```

### View Application Logs
```bash
# Seq Dashboard: http://SERVER_IP:5341
```

### Restart Services
```bash
# Restart frontend
docker-compose -f /home/deployer/booksy/docker-compose.prod.yml restart booksy-frontend

# Restart API Gateway
docker-compose -f /home/deployer/booksy/docker-compose.prod.yml restart booksy-gateway

# Restart User Management API
docker-compose -f /home/deployer/booksy/docker-compose.prod.yml restart booksy-user-management-api

# Restart Service Catalog API
docker-compose -f /home/deployer/booksy/docker-compose.prod.yml restart booksy-service-catalog-api

# Restart all services
docker-compose -f /home/deployer/booksy/docker-compose.prod.yml restart
```

---

## Security Checklist

- [ ] SSH key authentication enabled (no password login)
- [ ] Firewall configured (ufw)
- [ ] HTTPS enabled (Let's Encrypt)
- [ ] Strong passwords in .env file
- [ ] Database backed up regularly
- [ ] Logs monitored (Seq)
- [ ] Health checks working
- [ ] Resource limits set (done in docker-compose)

---

## Next Steps

1. **SSH Setup**: Run Steps 1-3 on your server
2. **GitHub Secrets**: Add SERVER_HOST, SERVER_USER, SERVER_DEPLOY_PATH to GitHub
3. **Deploy Directory**: Run Step 4 to create directory and .env file
4. **Nginx (Optional)**: Run Step 5 for domain setup
5. **First Deploy**: Run Step 7.1 for manual deployment
6. **Push Code**: After verification, run Step 7.2 for automatic deployments

Need help with any specific step? Let me know!
