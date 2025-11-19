# Booksy Application - Deployment Guide

This guide provides comprehensive instructions for setting up automated deployment of the Booksy application to a dedicated Ubuntu server.

## Overview

The deployment process is automated through GitHub Actions. When a pull request is merged to the `master` branch, the following happens:

1. All tests are executed (unit, integration, architecture)
2. Docker images are built for all services
3. Images are pushed to GitHub Container Registry
4. Application is deployed to the production server
5. Health checks are performed

## Prerequisites

### 1. Ubuntu Server Requirements

- **OS**: Ubuntu 20.04 LTS or later
- **RAM**: Minimum 4GB (8GB recommended)
- **Storage**: Minimum 50GB
- **Docker**: Version 20.10 or later
- **Docker Compose**: Version 2.0 or later

### 2. GitHub Requirements

- Repository with admin access
- Ability to add secrets
- GitHub Container Registry enabled

## Server Setup

### Step 1: Initial Server Configuration

SSH into your Ubuntu server and run the setup script:

```bash
# Download the setup script
wget https://raw.githubusercontent.com/YOUR_USERNAME/Booking/master/deployment/scripts/server-setup.sh

# Make it executable
chmod +x server-setup.sh

# Run with sudo
sudo ./server-setup.sh
```

This script will:
- Install Docker and Docker Compose
- Configure firewall rules
- Create deployment directory structure (`/opt/booksy`)
- Set up backup scripts
- Configure log rotation

### Step 2: Configure Environment Variables

Edit the environment file:

```bash
sudo nano /opt/booksy/.env
```

Update the following values:

```env
GITHUB_REPOSITORY_OWNER=your-github-username

# Database - Use strong passwords!
POSTGRES_PASSWORD=YourSecurePassword123!
DB_CONNECTION_STRING=Host=postgres;Port=5432;Database=booksy_production;Username=booksy_admin;Password=YourSecurePassword123!;Include Error Detail=true

# Redis
REDIS_PASSWORD=YourRedisPassword123!
REDIS_CONNECTION_STRING=redis:6379,password=YourRedisPassword123!

# RabbitMQ
RABBITMQ_PASSWORD=YourRabbitMQPassword123!
RABBITMQ_CONNECTION_STRING=amqp://booksy_admin:YourRabbitMQPassword123!@rabbitmq:5672

# Seq
SEQ_ADMIN_PASSWORD=YourSeqPassword123!

# Frontend - Replace with your server's IP or domain
API_BASE_URL=http://YOUR_SERVER_IP:5000
```

### Step 3: Generate SSH Key for GitHub Actions

```bash
# Generate SSH key pair
ssh-keygen -t ed25519 -C "github-actions-booksy" -f ~/.ssh/github-actions -N ""

# Add public key to authorized_keys
cat ~/.ssh/github-actions.pub >> ~/.ssh/authorized_keys

# Display private key (copy this for GitHub secrets)
cat ~/.ssh/github-actions
```

**Important**: Keep the private key secure. You'll add it to GitHub Secrets in the next step.

## GitHub Configuration

### Step 1: Enable GitHub Container Registry

1. Go to your GitHub profile → Settings → Developer settings → Personal access tokens
2. Generate a new token (classic) with these scopes:
   - `write:packages`
   - `delete:packages`
   - `read:packages`

### Step 2: Add Repository Secrets

Go to your repository: **Settings → Secrets and variables → Actions → New repository secret**

Add the following secrets:

| Secret Name | Description | Example |
|-------------|-------------|---------|
| `SERVER_HOST` | Your Ubuntu server IP or hostname | `192.168.1.100` or `booksy.example.com` |
| `SERVER_USER` | SSH username | `ubuntu` or your username |
| `SERVER_SSH_KEY` | Private SSH key from Step 3 | (paste entire key) |
| `SERVER_DEPLOY_PATH` | Deployment directory path | `/opt/booksy` |

### Step 3: Enable GitHub Packages

1. Go to repository **Settings → Actions → General**
2. Under "Workflow permissions", select:
   - ✅ Read and write permissions
   - ✅ Allow GitHub Actions to create and approve pull requests

## Deployment Workflow

### Automatic Deployment

Once configured, deployments happen automatically:

1. **Developer workflow**:
   ```bash
   git checkout -b feature/my-feature
   # Make changes
   git add .
   git commit -m "Add new feature"
   git push origin feature/my-feature
   ```

2. **Create Pull Request** on GitHub

3. **Review and Merge** to `master` branch

4. **GitHub Actions triggers** automatically:
   - Runs all tests
   - Builds Docker images
   - Pushes to registry
   - Deploys to server

### Manual Deployment

To manually trigger deployment:

1. Go to **Actions** tab in GitHub
2. Select **Deploy to Production** workflow
3. Click **Run workflow**
4. Select `master` branch
5. Click **Run workflow**

## Monitoring and Maintenance

### View Application Logs

```bash
# View all services
cd /opt/booksy
docker-compose -f docker-compose.prod.yml logs

# View specific service
docker-compose -f docker-compose.prod.yml logs usermanagement-api

# Follow logs in real-time
docker-compose -f docker-compose.prod.yml logs -f
```

### Access Seq (Centralized Logging)

Open in browser: `http://YOUR_SERVER_IP:5341`
- Username: `admin`
- Password: (value from `.env` file)

### Check Service Health

```bash
# Check running containers
docker-compose -f docker-compose.prod.yml ps

# Check individual service health
curl http://localhost:5000/health  # Gateway
curl http://localhost:5001/health  # UserManagement
curl http://localhost:5002/health  # ServiceCatalog
```

### Database Backups

Backups run automatically at 2:00 AM daily. To manually backup:

```bash
/opt/booksy/scripts/backup.sh
```

Backups are stored in `/opt/booksy/backups/`

### Restore from Backup

```bash
# List available backups
ls -lh /opt/booksy/backups/

# Restore database
docker exec -i booksy-postgres psql -U booksy_admin -d booksy_production < /opt/booksy/backups/db_backup_YYYYMMDD_HHMMSS.sql
```

## Troubleshooting

### Deployment Failed

1. Check GitHub Actions logs:
   - Go to **Actions** tab
   - Click on failed workflow run
   - Review logs for each job

2. Check server logs:
   ```bash
   cd /opt/booksy
   docker-compose -f docker-compose.prod.yml logs
   ```

### Service Not Starting

```bash
# Restart specific service
docker-compose -f docker-compose.prod.yml restart usermanagement-api

# Restart all services
docker-compose -f docker-compose.prod.yml restart

# Rebuild and restart
docker-compose -f docker-compose.prod.yml up -d --force-recreate
```

### Cannot Connect to Server

1. Verify SSH access:
   ```bash
   ssh -i ~/.ssh/github-actions your-user@your-server-ip
   ```

2. Check firewall:
   ```bash
   sudo ufw status
   ```

3. Verify server is running:
   ```bash
   docker ps
   ```

### Database Connection Issues

1. Check database is running:
   ```bash
   docker-compose -f docker-compose.prod.yml ps postgres
   ```

2. Test connection:
   ```bash
   docker exec -it booksy-postgres psql -U booksy_admin -d booksy_production
   ```

3. Verify connection string in `.env` file

## Security Best Practices

1. **SSH Access**:
   - Use SSH keys instead of passwords
   - Disable password authentication in `/etc/ssh/sshd_config`
   - Change default SSH port (optional)

2. **Firewall**:
   - Only open necessary ports
   - Restrict access by IP if possible

3. **Secrets Management**:
   - Never commit `.env` files to Git
   - Rotate passwords regularly
   - Use strong passwords (minimum 16 characters)

4. **SSL/TLS**:
   - Set up SSL certificates (Let's Encrypt recommended)
   - Force HTTPS for production

5. **Regular Updates**:
   ```bash
   # Update server packages
   sudo apt update && sudo apt upgrade -y

   # Update Docker images
   cd /opt/booksy
   docker-compose -f docker-compose.prod.yml pull
   docker-compose -f docker-compose.prod.yml up -d
   ```

## Rollback Procedure

If deployment introduces issues:

1. **Quick rollback** (use previous images):
   ```bash
   cd /opt/booksy

   # Pull specific version (replace SHA with commit hash)
   docker pull ghcr.io/YOUR_USERNAME/booksy-usermanagement:SHORT_SHA
   docker pull ghcr.io/YOUR_USERNAME/booksy-servicecatalog:SHORT_SHA
   docker pull ghcr.io/YOUR_USERNAME/booksy-gateway:SHORT_SHA
   docker pull ghcr.io/YOUR_USERNAME/booksy-frontend:SHORT_SHA

   # Restart with specific versions
   docker-compose -f docker-compose.prod.yml down
   docker-compose -f docker-compose.prod.yml up -d
   ```

2. **Database rollback**:
   ```bash
   # Restore from backup
   docker exec -i booksy-postgres psql -U booksy_admin -d booksy_production < /opt/booksy/backups/db_backup_YYYYMMDD_HHMMSS.sql
   ```

## Performance Tuning

### Database Optimization

```bash
# Connect to database
docker exec -it booksy-postgres psql -U booksy_admin -d booksy_production

# Check database size
SELECT pg_size_pretty(pg_database_size('booksy_production'));

# Vacuum and analyze
VACUUM ANALYZE;
```

### Container Resource Limits

Edit `docker-compose.prod.yml` to add resource limits:

```yaml
services:
  usermanagement-api:
    # ... existing config
    deploy:
      resources:
        limits:
          cpus: '1.0'
          memory: 1G
        reservations:
          cpus: '0.5'
          memory: 512M
```

## Support and Resources

- **Documentation**: [booksy-frontend/README.md](booksy-frontend/README.md)
- **Issues**: GitHub Issues
- **Logs**: `/opt/booksy/logs/`
- **Seq Dashboard**: `http://YOUR_SERVER_IP:5341`

## Appendix

### Useful Commands

```bash
# View disk usage
df -h
docker system df

# Clean up unused Docker resources
docker system prune -a --volumes

# View container resource usage
docker stats

# Export/Import Docker images
docker save -o booksy-backup.tar ghcr.io/YOUR_USERNAME/booksy-usermanagement:latest
docker load -i booksy-backup.tar

# Database connection from host
docker exec -it booksy-postgres psql -U booksy_admin -d booksy_production
```

### Environment Variables Reference

See [deployment/.env.production.example](deployment/.env.production.example) for complete list.
