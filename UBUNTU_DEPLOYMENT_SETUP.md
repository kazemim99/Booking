# Ubuntu Server Deployment Setup Guide

This guide will help you deploy your Booksy application to your Ubuntu server with automatic CI/CD from GitHub.

## Prerequisites

- Ubuntu 20.04 LTS or newer
- Public IP address or domain name
- SSH access to your server
- GitHub account with repository access

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
ssh-keygen -t ed25519 -f /home/deployer/.ssh/github_deploy -N ""
# Or if using root:
ssh-keygen -t ed25519 -f ~/.ssh/github_deploy -N ""
```

### 3.2 Copy Public Key
```bash
cat ~/.ssh/github_deploy.pub
```

### 3.3 Add Deploy Key to GitHub Repository
1. Go to: `https://github.com/kazemim99/Booking/settings/keys`
2. Click "Add deploy key"
3. Paste the public key
4. Check "Allow write access"
5. Click "Add key"

### 3.4 Add Private Key to GitHub Secrets
1. Copy the private key:
   ```bash
   cat ~/.ssh/github_deploy
   ```
2. Go to: `https://github.com/kazemim99/Booking/settings/secrets/actions`
3. Click "New repository secret"
4. Name: `DEPLOY_KEY`
5. Value: Paste the entire private key (including `-----BEGIN OPENSSH PRIVATE KEY-----`)
6. Click "Add secret"

### 3.5 Get Server Information for GitHub Secrets
Create these secrets in GitHub:
1. `SERVER_HOST`: Your server IP (e.g., `192.168.1.100` or your public IP)
2. `SERVER_USER`: `deployer` (or your username)
3. `SERVER_DEPLOY_PATH`: `/home/deployer/booksy` (or your preferred path)

**To add these secrets:**
1. Go to: `https://github.com/kazemim99/Booking/settings/secrets/actions`
2. Add each secret separately
3. Example for SERVER_HOST:
   - Name: `SERVER_HOST`
   - Value: `YOUR_ACTUAL_IP_ADDRESS`

---

## Step 4: Create Deployment Directory on Server

### 4.1 Create Project Directory
```bash
mkdir -p /home/deployer/booksy
cd /home/deployer/booksy

# If using root:
sudo mkdir -p /root/booksy
cd /root/booksy
```

### 4.2 Create .env File
```bash
# Create .env file with your configuration
cat > .env << 'EOF'
# Database
POSTGRES_USER=booksy_admin
POSTGRES_PASSWORD=YourSecurePassword123!
POSTGRES_DB=booksy

# Redis
REDIS_PASSWORD=YourRedisPassword123!

# RabbitMQ
RABBITMQ_USER=guest
RABBITMQ_PASSWORD=guest

# Seq Logging
SEQ_ADMIN_USER=admin
SEQ_ADMIN_PASSWORD=YourSeqPassword123!

# pgAdmin
PGADMIN_EMAIL=admin@booksy.local
PGADMIN_PASSWORD=YourPgAdminPassword123!

# GitHub Container Registry (use your PAT)
GITHUB_REPOSITORY_OWNER=kazemim99

# Connection Strings (point to internal Docker network)
DB_CONNECTION_STRING=Host=postgres;Port=5432;Database=booksy;Username=booksy_admin;Password=YourSecurePassword123!;Include Error Detail=true
REDIS_CONNECTION_STRING=redis:6379,password=YourRedisPassword123!
RABBITMQ_CONNECTION_STRING=amqp://guest:guest@rabbitmq:5672/

# Service URLs (for Gateway)
USER_MANAGEMENT_URL=http://usermanagement-api:80
SERVICE_CATALOG_URL=http://servicecatalog-api:80
SEQ_SERVER_URL=http://seq:80
EOF
```

### 4.3 Set Proper Permissions
```bash
chmod 600 .env
```

---

## Step 5: Setup Nginx Reverse Proxy (Optional but Recommended)

### 5.1 Install Nginx
```bash
sudo apt install -y nginx
```

### 5.2 Create Nginx Configuration

Replace `YOUR_DOMAIN.COM` with your actual domain:

```bash
sudo tee /etc/nginx/sites-available/booksy > /dev/null << 'EOF'
upstream booksy_frontend {
    server localhost:80;
}

upstream booksy_api {
    server localhost:5000;
}

upstream booksy_seq {
    server localhost:5341;
}

server {
    listen 80;
    server_name YOUR_DOMAIN.COM;

    # Redirect HTTP to HTTPS
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name YOUR_DOMAIN.COM;

    # SSL Certificate (install certbot for Let's Encrypt)
    # ssl_certificate /etc/letsencrypt/live/YOUR_DOMAIN.COM/fullchain.pem;
    # ssl_certificate_key /etc/letsencrypt/live/YOUR_DOMAIN.COM/privkey.pem;

    client_max_body_size 50M;

    # Frontend
    location / {
        proxy_pass http://booksy_frontend;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
    }

    # API Gateway
    location /api/ {
        proxy_pass http://booksy_api/;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
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

### 5.3 Enable Nginx Configuration
```bash
sudo ln -s /etc/nginx/sites-available/booksy /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl restart nginx
```

### 5.4 Setup SSL with Let's Encrypt (HTTPS)
```bash
sudo apt install -y certbot python3-certbot-nginx
sudo certbot certonly --nginx -d YOUR_DOMAIN.COM
```

Then uncomment the SSL lines in the Nginx config and restart:
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
# (This should be done by CI/CD, but for first run)
wget https://raw.githubusercontent.com/kazemim99/Booking/master/docker-compose.prod.yml

# Create .env file (from Step 4.2)

# Login to GitHub Container Registry
echo "YOUR_GITHUB_PAT" | docker login ghcr.io -u kazemim99 --password-stdin

# Pull and run containers
docker-compose -f docker-compose.prod.yml up -d

# Check status
docker-compose -f docker-compose.prod.yml ps
```

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

**Option 1: Direct IP**
```
http://YOUR_SERVER_IP
http://YOUR_SERVER_IP:5001  # UserManagement API
http://YOUR_SERVER_IP:5002  # ServiceCatalog API
http://YOUR_SERVER_IP:5341  # Seq Logs
http://YOUR_SERVER_IP:5050  # pgAdmin
```

**Option 2: With Domain (After Nginx setup)**
```
https://YOUR_DOMAIN.COM
https://YOUR_DOMAIN.COM/api/
https://YOUR_DOMAIN.COM/logs/  (Seq)
```

---

## Troubleshooting

### SSH Connection Issues
```bash
# Test SSH connection
ssh -i ~/.ssh/github_deploy deployer@YOUR_SERVER_IP

# Check SSH agent
ssh-add -l

# Add key to agent
ssh-add ~/.ssh/github_deploy
```

### Docker Login Issues
```bash
# Create GitHub Personal Access Token:
# Go to: https://github.com/settings/tokens?type=beta
# Select "repo" and "write:packages" permissions
# Copy token value

# Login
echo "YOUR_GITHUB_PAT" | docker login ghcr.io -u kazemim99 --password-stdin
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
docker-compose -f /home/deployer/booksy/docker-compose.prod.yml restart frontend
docker-compose -f /home/deployer/booksy/docker-compose.prod.yml restart gateway
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
