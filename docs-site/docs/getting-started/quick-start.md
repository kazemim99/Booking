# Quick Start Guide - Booksy Deployment

This guide will get you from zero to deployed in **~60 minutes**.

## 📋 **Prerequisites Checklist**

Before starting, make sure you have:

- [ ] Ubuntu server (20.04+ LTS) with SSH access
- [ ] Server specs: 8GB RAM, 4 CPU cores, 50GB storage minimum
- [ ] GitHub account with repository admin access
- [ ] Domain name (optional, but recommended)
- [ ] 60 minutes of time

---

## 🚀 **Basic Deployment** (~30 minutes)

### Step 1: Clone and Commit (5 min)

```bash
# On your local machine
cd /path/to/Booking
git add .
git commit -m "Add automated CI/CD deployment pipeline"
git push origin master
```

### Step 2: Server Setup (15 min)

```bash
# SSH into your server
ssh your-user@your-server-ip

# Download and run setup script
wget https://raw.githubusercontent.com/kazemim99/Booking/master/deployment/scripts/server-setup.sh
chmod +x server-setup.sh
sudo ./server-setup.sh
```

### Step 3: Configure Environment (5 min)

```bash
sudo nano /opt/booksy/.env
```

Update these **critical** values:
```env
GITHUB_REPOSITORY_OWNER=kazemim99
POSTGRES_PASSWORD=YourSecurePassword123!
REDIS_PASSWORD=YourRedisPassword456!
API_BASE_URL=http://YOUR_SERVER_IP:5000
```

### Step 4: Setup GitHub (5 min)

**A. Generate SSH Key:**
```bash
ssh-keygen -t ed25519 -C "github-actions" -f ~/.ssh/github-actions -N ""
cat ~/.ssh/github-actions.pub >> ~/.ssh/authorized_keys
cat ~/.ssh/github-actions  # Copy this output
```

**B. Add GitHub Secrets:**

Go to: `https://github.com/kazemim99/Booking/settings/secrets/actions`

Add:
- `SERVER_HOST`: Your server IP
- `SERVER_USER`: Your username (e.g., `ubuntu`)
- `SERVER_SSH_KEY`: The private key from above
- `SERVER_DEPLOY_PATH`: `/opt/booksy`

**C. Enable GitHub Packages:**

Go to: `https://github.com/kazemim99/Booking/settings/actions`
- ✅ Read and write permissions

### Step 5: Deploy! (Now automatic)

Every push to `master` will:
1. Run tests
2. Build Docker images
3. Deploy to server

**Manual trigger:** Go to Actions tab → "Deploy to Production" → "Run workflow"

---

## 🎯 **Access Your Application**

After deployment (5-10 minutes):

| Service | URL | Purpose |
|---------|-----|---------|
| **Frontend** | `http://YOUR_SERVER_IP` | Main application |
| **API Gateway** | `http://YOUR_SERVER_IP:5000` | API endpoints |
| **Seq Logs** | `http://YOUR_SERVER_IP:5341` | Centralized logs |
| **RabbitMQ** | `http://YOUR_SERVER_IP:15672` | Message queue UI |
| **pgAdmin** | `http://YOUR_SERVER_IP:5050` | Database admin |

---

## ⚙️ **Advanced Setup** (~30 minutes each)

### 🌐 Custom Domain + SSL

```bash
# Run SSL setup
cd /opt/booksy/deployment/scripts
sudo ./setup-ssl.sh booksy.com admin@booksy.com
```

**DNS Setup:**
- Add A record: `booksy.com` → Your server IP
- Wait for DNS propagation (5-30 minutes)

**Result:** `https://booksy.com` with automatic SSL

📖 **Full guide:** [ADVANCED-SETUP.md](ADVANCED-SETUP.md#1-custom-domain-setup)

---

### 📊 Monitoring (Prometheus + Grafana)

```bash
cd /opt/booksy

# Start monitoring stack
docker-compose -f docker-compose.prod.yml -f docker-compose.monitoring.yml up -d
```

**Access:**
- Grafana: `http://YOUR_SERVER_IP:3000` (admin/admin)
- Prometheus: `http://YOUR_SERVER_IP:9090`

**Import Dashboards:**
1. Node Exporter (ID: 1860)
2. PostgreSQL (ID: 9628)
3. Redis (ID: 11835)

📖 **Full guide:** [ADVANCED-SETUP.md](ADVANCED-SETUP.md#3-monitoring-with-prometheus--grafana)

---

### 🔔 Slack Alerts

1. **Create Slack webhook:** https://api.slack.com/messaging/webhooks

2. **Update config:**
```bash
sudo nano /opt/booksy/deployment/monitoring/alertmanager/config.yml
```

Replace `YOUR_SLACK_WEBHOOK_URL_HERE`

3. **Restart:**
```bash
docker-compose -f docker-compose.monitoring.yml restart alertmanager
```

📖 **Full guide:** [ADVANCED-SETUP.md](ADVANCED-SETUP.md#4-alerting-configuration)

---

### 🧪 Staging Environment

```bash
cd /opt/booksy

# Copy and configure staging env
cp deployment/.env.staging.example .env.staging
sudo nano .env.staging  # Update passwords

# Start staging
docker-compose -f docker-compose.staging.yml up -d
```

**Access:** `http://YOUR_SERVER_IP:8080`

**Deploy to staging:** Push to `develop` branch (auto-deploys)

📖 **Full guide:** [ADVANCED-SETUP.md](ADVANCED-SETUP.md#5-staging-environment)

---

## 🛠️ **Common Commands**

### View Status
```bash
cd /opt/booksy
docker-compose -f docker-compose.prod.yml ps
```

### View Logs
```bash
# All services
docker-compose -f docker-compose.prod.yml logs -f

# Specific service
docker-compose -f docker-compose.prod.yml logs -f usermanagement-api
```

### Restart Services
```bash
# All services
docker-compose -f docker-compose.prod.yml restart

# Specific service
docker-compose -f docker-compose.prod.yml restart gateway
```

### Manual Backup
```bash
/opt/booksy/scripts/backup.sh
```

### Update Application
```bash
# Just push to GitHub - deployment is automatic!
git push origin master

# Or manually:
cd /opt/booksy
docker-compose -f docker-compose.prod.yml pull
docker-compose -f docker-compose.prod.yml up -d
```

---

## 🆘 **Troubleshooting**

### Deployment Failed

**Check GitHub Actions:**
- Go to repository → Actions tab
- Click failed workflow
- Review error logs

**Check server:**
```bash
ssh your-user@your-server-ip
cd /opt/booksy
docker-compose -f docker-compose.prod.yml logs
```

### Service Not Starting

```bash
# Check specific service
docker logs booksy-usermanagement-api

# Restart service
docker-compose -f docker-compose.prod.yml restart usermanagement-api
```

### Can't Access Application

```bash
# Check firewall
sudo ufw status

# Check if services are running
docker ps

# Check Nginx (if using domain)
sudo systemctl status nginx
sudo nginx -t
```

### Database Issues

```bash
# Connect to database
docker exec -it booksy-postgres psql -U booksy_admin -d booksy_production

# Check database size
SELECT pg_size_pretty(pg_database_size('booksy_production'));

# View connections
SELECT count(*) FROM pg_stat_activity;
```

---

## 📚 **Documentation Index**

| Document | Purpose | When to Read |
|----------|---------|--------------|
| **[DEPLOYMENT.md](DEPLOYMENT.md)** | Complete deployment guide | Detailed setup |
| **[ADVANCED-SETUP.md](ADVANCED-SETUP.md)** | Domain, SSL, monitoring | After basic setup |
| **[DOCKER-COMPOSE-REVIEW.md](DOCKER-COMPOSE-REVIEW.md)** | Docker improvements | Understanding config |
| **[DATABASE-FIX.md](DATABASE-FIX.md)** | PostgreSQL vs MS SQL | Technical details |
| **This file** | Quick start | First deployment |

---

## ✅ **Success Checklist**

After deployment, verify:

- [ ] Frontend loads at `http://YOUR_SERVER_IP`
- [ ] API responds at `http://YOUR_SERVER_IP:5000/health`
- [ ] All containers are "healthy": `docker ps`
- [ ] Logs are viewable in Seq: `http://YOUR_SERVER_IP:5341`
- [ ] GitHub Actions workflows pass
- [ ] Automatic deployments work (test with a small change)

---

## 🎓 **Next Steps**

Once basic deployment works:

1. ✅ Set up custom domain with SSL ([ADVANCED-SETUP.md](ADVANCED-SETUP.md#1-custom-domain-setup))
2. ✅ Enable monitoring ([ADVANCED-SETUP.md](ADVANCED-SETUP.md#3-monitoring-with-prometheus--grafana))
3. ✅ Configure alerts ([ADVANCED-SETUP.md](ADVANCED-SETUP.md#4-alerting-configuration))
4. ✅ Create staging environment ([ADVANCED-SETUP.md](ADVANCED-SETUP.md#5-staging-environment))
5. ⏭️ Set up CDN (Cloudflare)
6. ⏭️ Implement automated backups to cloud storage
7. ⏭️ Add performance testing
8. ⏭️ Configure log retention policies

---

## 💡 **Pro Tips**

1. **Use staging:** Test changes in staging before production
2. **Monitor logs:** Check Seq regularly for errors
3. **Watch resources:** Keep an eye on CPU/memory in Grafana
4. **Backup regularly:** Automated daily, but test restores monthly
5. **Update secrets:** Rotate passwords every 90 days
6. **Document changes:** Keep track of configuration changes

---

## 🤝 **Getting Help**

- **Documentation Issues:** Check other .md files
- **Deployment Issues:** Review GitHub Actions logs
- **Server Issues:** Check `docker logs` and system logs
- **Questions:** Review [DEPLOYMENT.md](DEPLOYMENT.md) FAQ section

---

**Ready to deploy?** Start with [Step 1](#step-1-clone-and-commit-5-min)! 🚀
