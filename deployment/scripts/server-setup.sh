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

# Install Docker
echo "Installing Docker..."
if ! command -v docker &> /dev/null; then
    curl -fsSL https://get.docker.com -o get-docker.sh
    sh get-docker.sh
    rm get-docker.sh

    # Add current user to docker group
    usermod -aG docker $SUDO_USER || true
    echo "Docker installed successfully"
else
    echo "Docker is already installed"
fi

# Install Docker Compose
echo "Installing Docker Compose..."
if ! command -v docker-compose &> /dev/null; then
    DOCKER_COMPOSE_VERSION=$(curl -s https://api.github.com/repos/docker/compose/releases/latest | grep 'tag_name' | cut -d\" -f4)
    curl -L "https://github.com/docker/compose/releases/download/${DOCKER_COMPOSE_VERSION}/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
    chmod +x /usr/local/bin/docker-compose
    echo "Docker Compose installed successfully"
else
    echo "Docker Compose is already installed"
fi

# Install additional utilities
echo "Installing additional utilities..."
apt-get install -y curl git htop net-tools ufw

# Configure UFW firewall
echo "Configuring firewall..."
ufw --force enable
ufw allow 22/tcp        # SSH
ufw allow 80/tcp        # HTTP
ufw allow 443/tcp       # HTTPS
ufw allow 5000/tcp      # API Gateway
ufw allow 5001/tcp      # UserManagement API
ufw allow 5002/tcp      # ServiceCatalog API
ufw allow 5341/tcp      # Seq
echo "Firewall configured"

# Create deployment directory
DEPLOY_PATH="/opt/booksy"
echo "Creating deployment directory at $DEPLOY_PATH..."
mkdir -p $DEPLOY_PATH
mkdir -p $DEPLOY_PATH/logs
mkdir -p $DEPLOY_PATH/backups
chown -R $SUDO_USER:$SUDO_USER $DEPLOY_PATH

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

# RabbitMQ Configuration
RABBITMQ_USER=booksy_admin
RABBITMQ_PASSWORD=CHANGE_ME
RABBITMQ_CONNECTION_STRING=amqp://booksy_admin:CHANGE_ME@rabbitmq:5672

# Seq Configuration
SEQ_ADMIN_USER=admin
SEQ_ADMIN_PASSWORD=CHANGE_ME
SEQ_SERVER_URL=http://seq:5341

# Service URLs
USER_MANAGEMENT_URL=http://usermanagement-api:80
SERVICE_CATALOG_URL=http://servicecatalog-api:80

# Frontend Configuration
API_BASE_URL=http://YOUR_SERVER_IP:5000
EOF

chown $SUDO_USER:$SUDO_USER $DEPLOY_PATH/.env
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
chown -R $SUDO_USER:$SUDO_USER $DEPLOY_PATH/scripts

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
echo "Docker Compose version: $(docker-compose --version)"
echo ""
echo "Deployment directory: $DEPLOY_PATH"
echo ""
echo "IMPORTANT: Please complete the following steps:"
echo "1. Edit $DEPLOY_PATH/.env and update all passwords and configuration"
echo "2. Verify firewall rules: sudo ufw status"
echo "3. Configure your GitHub repository secrets"
echo "4. Test SSH access from GitHub Actions"
echo ""
echo "========================================="
