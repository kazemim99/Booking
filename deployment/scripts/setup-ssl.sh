#!/bin/bash

# SSL/TLS Setup Script using Let's Encrypt (Certbot)
# This script sets up automatic SSL certificate management

set -e

echo "========================================="
echo "Booksy - SSL/TLS Setup with Let's Encrypt"
echo "========================================="

# Check if running as root
if [ "$EUID" -ne 0 ]; then
    echo "Please run this script with sudo or as root"
    exit 1
fi

# Variables
DOMAIN="${1:-booksy.yourdomain.com}"
EMAIL="${2:-admin@yourdomain.com}"

if [ "$DOMAIN" = "booksy.yourdomain.com" ]; then
    echo "Usage: sudo ./setup-ssl.sh your-domain.com your-email@example.com"
    echo "Example: sudo ./setup-ssl.sh booksy.example.com admin@example.com"
    exit 1
fi

echo "Domain: $DOMAIN"
echo "Email: $EMAIL"
echo ""

# Install Nginx if not already installed
if ! command -v nginx &> /dev/null; then
    echo "Installing Nginx..."
    apt-get update
    apt-get install -y nginx
    systemctl enable nginx
    systemctl start nginx
else
    echo "Nginx is already installed"
fi

# Install Certbot
echo "Installing Certbot..."
apt-get install -y certbot python3-certbot-nginx

# Create directory for Let's Encrypt challenges
mkdir -p /var/www/certbot

# Copy Nginx configuration
echo "Setting up Nginx configuration..."
NGINX_CONF="/etc/nginx/sites-available/booksy"
cp /opt/booksy/deployment/nginx/booksy.conf $NGINX_CONF

# Replace domain placeholder
sed -i "s/booksy.yourdomain.com/$DOMAIN/g" $NGINX_CONF

# Create symlink
ln -sf $NGINX_CONF /etc/nginx/sites-enabled/booksy

# Remove default site
rm -f /etc/nginx/sites-enabled/default

# Test Nginx configuration
echo "Testing Nginx configuration..."
nginx -t

# Reload Nginx
systemctl reload nginx

# Obtain SSL certificate
echo "Obtaining SSL certificate from Let's Encrypt..."
certbot --nginx -d $DOMAIN --non-interactive --agree-tos --email $EMAIL

# Set up automatic renewal
echo "Setting up automatic certificate renewal..."
(crontab -l 2>/dev/null; echo "0 3 * * * certbot renew --quiet --post-hook 'systemctl reload nginx'") | crontab -

# Test renewal
echo "Testing certificate renewal..."
certbot renew --dry-run

echo ""
echo "========================================="
echo "SSL Setup Complete!"
echo "========================================="
echo "Your site is now accessible at: https://$DOMAIN"
echo "Certificate will automatically renew every 90 days"
echo ""
echo "Next steps:"
echo "1. Update your DNS A record to point $DOMAIN to this server's IP"
echo "2. Update API_BASE_URL in /opt/booksy/.env to https://$DOMAIN"
echo "3. Restart services: cd /opt/booksy && docker-compose -f docker-compose.prod.yml restart"
echo ""
