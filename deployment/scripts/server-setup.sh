#!/bin/bash

# Server Setup Script for Booksy Application
# This script should be run once on your Ubuntu server to prepare it for deployment

set -e

echo "========================================="
echo "Booksy Application - Server Setup"
echo "========================================="

# Check if running as root or with sudo
if [ "$EUID" -ne 0 ]; then
    echo "Please run this script with sudo or as root"
    exit 1
fi

# Update system packages
echo "Updating system packages..."
apt-get update
apt-get upgrade -y

# Install Docker (the convenience script also installs docker-compose-plugin, i.e.
# the `docker compose` v2 subcommand — this script never installs the deprecated
# standalone v1 `docker-compose` binary; docker-compose.prod.yml and deploy.yml both
# assume v2's `docker compose` syntax).
echo "Installing Docker..."
if ! command -v docker &> /dev/null; then
    curl -fsSL https://get.docker.com -o get-docker.sh
    sh get-docker.sh
    rm get-docker.sh
    echo "Docker installed successfully"
else
    echo "Docker is already installed"
fi

# Dedicated, unprivileged deploy user (not the invoking sudo user, and not root):
# CI's SSH key only needs docker-group membership, never root or sudo, to run
# `docker compose` in $DEPLOY_PATH.
echo "Creating dedicated deploy user 'booksy'..."
if ! id booksy &> /dev/null; then
    useradd -m -s /bin/bash booksy
fi
usermod -aG docker booksy

# Install additional utilities
echo "Installing additional utilities..."
apt-get install -y curl git htop net-tools

# Firewall: SKIPPED by default. This script must not blindly `ufw enable` — on a
# server that already runs other services (a VPN/proxy panel, another site, custom
# tunnel ports, etc.), enabling a firewall with only Booksy's ports allow-listed
# would silently cut off everything else already reachable. If this IS a fresh,
# single-purpose box and you want a firewall, configure it yourself with the full
# port list for EVERYTHING this box runs — not just Booksy's — for example:
#   ufw allow 22/tcp && ufw allow 80/tcp && ufw allow 443/tcp && ufw --force enable
# Booksy itself needs only 80 and 443 open publicly (host nginx terminates TLS and
# reverse-proxies to the containers, which bind 127.0.0.1 only); nothing else it
# runs needs to be reachable from outside this box.
echo "Firewall: not configured by this script — see the comment above before enabling one yourself."

# Create deployment directory, owned by the deploy user (not root, not $SUDO_USER)
DEPLOY_PATH="/opt/booksy"
echo "Creating deployment directory at $DEPLOY_PATH..."
mkdir -p $DEPLOY_PATH
mkdir -p $DEPLOY_PATH/logs
mkdir -p $DEPLOY_PATH/backups
chown -R booksy:booksy $DEPLOY_PATH

# Create environment file
echo "Creating environment file template..."
cat > $DEPLOY_PATH/.env << 'EOF'
# GitHub Container Registry
GITHUB_REPOSITORY_OWNER=your-github-username

# Database Configuration
POSTGRES_USER=booksy_admin
POSTGRES_PASSWORD=CHANGE_ME
POSTGRES_DB=booksy_production
DB_CONNECTION_STRING=Host=postgres;Port=5432;Database=booksy_production;Username=booksy_admin;Password=CHANGE_ME;Include Error Detail=true

# Redis Configuration
REDIS_PASSWORD=CHANGE_ME
REDIS_CONNECTION_STRING=redis:6379,password=CHANGE_ME

# RabbitMQ — not required (CAP runs in-process via EventBus__Provider=InMemory)
# RABBITMQ_USER=booksy_admin
# RABBITMQ_PASSWORD=CHANGE_ME
# RABBITMQ_CONNECTION_STRING=amqp://booksy_admin:CHANGE_ME@rabbitmq:5672

# Seq Configuration
SEQ_ADMIN_USER=admin
SEQ_ADMIN_PASSWORD=CHANGE_ME
SEQ_SERVER_URL=http://seq:5341

# Service URLs — obsolete (single host on :5000, no gateway)
# USER_MANAGEMENT_URL=http://booksy-api:80
# SERVICE_CATALOG_URL=http://booksy-api:80

# Frontend Configuration
API_BASE_URL=https://YOUR_DOMAIN  # e.g. https://back.yourdomain.ir — reverse-proxied by host nginx; see setup-ssl.sh
EOF

chown booksy:booksy $DEPLOY_PATH/.env
chmod 600 $DEPLOY_PATH/.env

# Create backup script
echo "Creating backup script..."
cat > $DEPLOY_PATH/scripts/backup.sh << 'EOF'
#!/bin/bash

BACKUP_DIR="/opt/booksy/backups"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)

# Backup database
docker exec booksy-postgres pg_dump -U booksy_admin booksy_production > $BACKUP_DIR/db_backup_$TIMESTAMP.sql

# Backup volumes
docker run --rm -v booksy_postgres_data:/data -v $BACKUP_DIR:/backup alpine tar czf /backup/postgres_volume_$TIMESTAMP.tar.gz /data
docker run --rm -v booksy_redis_data:/data -v $BACKUP_DIR:/backup alpine tar czf /backup/redis_volume_$TIMESTAMP.tar.gz /data

# Keep only last 7 days of backups
find $BACKUP_DIR -name "*.sql" -mtime +7 -delete
find $BACKUP_DIR -name "*.tar.gz" -mtime +7 -delete

echo "Backup completed: $TIMESTAMP"
EOF

chmod +x $DEPLOY_PATH/scripts/backup.sh
chown -R booksy:booksy $DEPLOY_PATH/scripts

# Create log rotation config
echo "Configuring log rotation..."
cat > /etc/logrotate.d/booksy << 'EOF'
/opt/booksy/logs/*.log {
    daily
    rotate 14
    compress
    delaycompress
    notifempty
    create 0640 www-data www-data
    sharedscripts
}
EOF

# Setup cron job for backups
echo "Setting up daily backup cron job..."
(crontab -l 2>/dev/null; echo "0 2 * * * /opt/booksy/scripts/backup.sh >> /opt/booksy/logs/backup.log 2>&1") | crontab -

# Enable Docker service
echo "Enabling Docker service..."
systemctl enable docker
systemctl start docker

# Display versions
echo ""
echo "========================================="
echo "Installation Complete!"
echo "========================================="
echo "Docker version: $(docker --version)"
echo "Docker Compose version: $(docker compose version)"
echo ""
echo "Deployment directory: $DEPLOY_PATH"
echo ""
echo "IMPORTANT: Please complete the following steps:"
echo "1. Edit $DEPLOY_PATH/.env and update all passwords and configuration"
echo "2. Run setup-ssl.sh for your domain (installs nginx + certbot if needed)"
echo "3. Generate a dedicated SSH keypair for CI and add its public half to"
echo "   /home/booksy/.ssh/authorized_keys (never reuse a personal key)"
echo "4. Add SERVER_HOST, SERVER_USER=booksy, SERVER_SSH_KEY (the CI private key),"
echo "   SERVER_DEPLOY_PATH=$DEPLOY_PATH as GitHub Actions repo secrets"
echo "5. Push to master (or run the Deploy workflow manually) to trigger the first deploy"
echo ""
echo "========================================="
