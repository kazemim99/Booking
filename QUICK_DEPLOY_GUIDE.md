# Quick Deployment Setup - TL;DR

**Goal:** Deploy your app automatically when you push code to `master` branch

---

## Step 1: Generate SSH Key (On Your Ubuntu Server)

```bash
# SSH into your server first
ssh root@YOUR_SERVER_IP
# or
ssh deployer@YOUR_SERVER_IP

# Generate SSH key
ssh-keygen -t ed25519 -f ~/.ssh/github_deploy -N ""

# Display public key (copy this)
cat ~/.ssh/github_deploy.pub

# Display private key (copy this too, you'll need it)
cat ~/.ssh/github_deploy
```

---

## Step 2: Add Public Key to GitHub (Deploy Key)

1. Go to: `https://github.com/kazemim99/Booking/settings/keys`
2. Click "Add deploy key"
3. Paste your public key from above
4. Check "Allow write access"
5. Click "Add key"

---

## Step 3: Add Secrets to GitHub (4 Secrets Needed)

Go to: `https://github.com/kazemim99/Booking/settings/secrets/actions`

Add these 4 secrets:

### Secret 1: SERVER_HOST
- **Name:** `SERVER_HOST`
- **Value:** Your server IP (e.g., `192.168.1.100` or public IP)

### Secret 2: SERVER_USER
- **Name:** `SERVER_USER`
- **Value:** `deployer` (or `root` if using root user)

### Secret 3: SERVER_SSH_KEY
- **Name:** `SERVER_SSH_KEY`
- **Value:** Your PRIVATE key (the content of `~/.ssh/github_deploy`)

### Secret 4: SERVER_DEPLOY_PATH
- **Name:** `SERVER_DEPLOY_PATH`
- **Value:** `/home/deployer/booksy` (or `/root/booksy` if root)

---

## Step 4: Setup Ubuntu Server (One Time)

SSH into your server and run:

```bash
# Update system
sudo apt update && sudo apt upgrade -y

# Install Docker
sudo apt install -y apt-transport-https ca-certificates curl software-properties-common
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo apt-key add -
sudo add-apt-repository "deb [arch=amd64] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable"
sudo apt update
sudo apt install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin

# Verify Docker
docker --version

# Create deployment directory
mkdir -p /home/deployer/booksy
cd /home/deployer/booksy

# Create .env file
cat > .env << 'EOF'
POSTGRES_USER=booksy_admin
POSTGRES_PASSWORD=YourPassword123!
POSTGRES_DB=booksy
REDIS_PASSWORD=YourRedisPass123!
RABBITMQ_USER=guest
RABBITMQ_PASSWORD=guest
SEQ_ADMIN_USER=admin
SEQ_ADMIN_PASSWORD=YourSeqPass123!
PGADMIN_EMAIL=admin@booksy.local
PGADMIN_PASSWORD=YourPgAdminPass123!
GITHUB_REPOSITORY_OWNER=kazemim99
DB_CONNECTION_STRING=Host=postgres;Port=5432;Database=booksy;Username=booksy_admin;Password=YourPassword123!;Include Error Detail=true
REDIS_CONNECTION_STRING=redis:6379,password=YourRedisPass123!
RABBITMQ_CONNECTION_STRING=amqp://guest:guest@rabbitmq:5672/
USER_MANAGEMENT_URL=http://usermanagement-api:80
SERVICE_CATALOG_URL=http://servicecatalog-api:80
SEQ_SERVER_URL=http://seq:80
EOF

# Set permissions
chmod 600 .env
```

---

## Step 5: Test First Deployment

### Option A: Manual Push (Recommended First Time)

```bash
# Make a small change to your code
# Then push to master:

git add .
git commit -m "Setup deployment"
git push origin master
```

### Option B: Manual Trigger

1. Go to: `https://github.com/kazemim99/Booking/actions`
2. Click "Deploy to Production" workflow
3. Click "Run workflow" button
4. Select `master` branch
5. Click "Run workflow"

---

## Step 6: Monitor Deployment

1. Go to: `https://github.com/kazemim99/Booking/actions`
2. Watch the workflow run
3. It will:
   - Run tests ✓
   - Build Docker images ✓
   - Push to container registry ✓
   - Deploy to your server ✓
4. When it's green ✓ - deployment successful!

---

## Step 7: Verify Application is Running

SSH to your server and check:

```bash
cd /home/deployer/booksy

# Check running containers
docker-compose -f docker-compose.prod.yml ps

# Expected output:
# CONTAINER ID   IMAGE                    STATUS
# xxxxx          booksy-usermanagement    Up (healthy)
# xxxxx          booksy-servicecatalog    Up (healthy)
# xxxxx          booksy-gateway           Up (healthy)
# xxxxx          booksy-frontend          Up (healthy)

# View logs
docker-compose -f docker-compose.prod.yml logs -f frontend
```

---

## Step 8: Access Your Application

### Via IP Address
- **Frontend:** `http://YOUR_SERVER_IP`
- **API:** `http://YOUR_SERVER_IP:5000`
- **UserManagement API:** `http://YOUR_SERVER_IP:5001`
- **ServiceCatalog API:** `http://YOUR_SERVER_IP:5002`
- **Logs (Seq):** `http://YOUR_SERVER_IP:5341`
- **Database (pgAdmin):** `http://YOUR_SERVER_IP:5050`

### Via Domain (Optional - Setup Nginx)

To use a domain like `myapp.com`, run on your server:

```bash
# Install Nginx
sudo apt install -y nginx

# Create Nginx config
sudo tee /etc/nginx/sites-available/booksy > /dev/null << 'EOF'
server {
    listen 80;
    server_name YOUR_DOMAIN.COM;

    location / {
        proxy_pass http://localhost;
        proxy_set_header Host $host;
    }
}
EOF

# Enable it
sudo ln -s /etc/nginx/sites-available/booksy /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl restart nginx

# Setup HTTPS (optional but recommended)
sudo apt install -y certbot python3-certbot-nginx
sudo certbot certonly --nginx -d YOUR_DOMAIN.COM
```

---

## Troubleshooting

### Workflow Fails - Check GitHub Secrets
```bash
# Go to: https://github.com/kazemim99/Booking/settings/secrets/actions
# Make sure all 4 secrets are filled:
# - SERVER_HOST
# - SERVER_USER
# - SERVER_SSH_KEY (full private key with -----BEGIN-----END----- lines)
# - SERVER_DEPLOY_PATH
```

### Can't SSH to Server
```bash
# Test SSH connection
ssh -i ~/.ssh/github_deploy deployer@YOUR_SERVER_IP

# If key permission error:
chmod 600 ~/.ssh/github_deploy
chmod 700 ~/.ssh
```

### Containers Won't Start
```bash
# Check what's wrong
docker-compose -f /home/deployer/booksy/docker-compose.prod.yml logs

# Login to GitHub registry first
echo "YOUR_GITHUB_PAT" | docker login ghcr.io -u kazemim99 --password-stdin

# Pull and restart
docker-compose -f /home/deployer/booksy/docker-compose.prod.yml pull
docker-compose -f /home/deployer/booksy/docker-compose.prod.yml up -d
```

### Port Already in Use
```bash
# Check which process is using port
sudo lsof -i :80
sudo lsof -i :5000

# Kill it
sudo kill -9 PID

# Or use different ports in docker-compose.prod.yml
```

---

## What Happens on Every Push

```
You push code to master
    ↓
GitHub Actions triggered
    ↓
Run tests (C# unit + integration tests)
    ↓
Build Docker images (backend APIs, frontend)
    ↓
Push images to GitHub Container Registry
    ↓
SSH into your server
    ↓
Pull latest images
    ↓
Stop old containers
    ↓
Start new containers with latest code
    ↓
Health checks ✓
    ↓
✅ Your app is updated!
```

---

## Commands You'll Use Often

```bash
# SSH to server
ssh deployer@YOUR_SERVER_IP

# Go to app directory
cd /home/deployer/booksy

# Check status
docker-compose -f docker-compose.prod.yml ps

# View logs
docker-compose -f docker-compose.prod.yml logs -f

# Restart a service
docker-compose -f docker-compose.prod.yml restart frontend

# Manually deploy (if workflow fails)
docker-compose -f docker-compose.prod.yml pull
docker-compose -f docker-compose.prod.yml up -d

# View .env file
cat .env

# Edit .env file
nano .env
# Then restart: docker-compose -f docker-compose.prod.yml up -d
```

---

That's it! You now have:
✅ Automatic deployment on every push
✅ Docker containerized application
✅ Health checks
✅ Multiple services (backend, frontend, database, etc.)
✅ Logs accessible via Seq
✅ Database management via pgAdmin

Any issues? Check the GitHub Actions logs at:
`https://github.com/kazemim99/Booking/actions`
