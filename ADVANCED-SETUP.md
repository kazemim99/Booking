# Advanced Deployment Setup Guide

This guide covers advanced features: custom domains, SSL/TLS, monitoring, alerting, and staging environments.

## Table of Contents
1. [Custom Domain Setup](#1-custom-domain-setup)
2. [SSL/TLS with Let's Encrypt](#2-ssltls-with-lets-encrypt)
3. [Monitoring with Prometheus & Grafana](#3-monitoring-with-prometheus--grafana)
4. [Alerting Configuration](#4-alerting-configuration)
5. [Staging Environment](#5-staging-environment)

---

## 1. Custom Domain Setup

### Prerequisites
- A registered domain name (e.g., `booksy.com`)
- Access to your domain's DNS settings

### Step 1: Configure DNS

Add these DNS records:

```
Type    Name        Value               TTL
A       @           YOUR_SERVER_IP      3600
A       www         YOUR_SERVER_IP      3600
A       staging     YOUR_SERVER_IP      3600
CNAME   api         @                   3600
```

### Step 2: Install Nginx

```bash
# On your server
sudo apt update
sudo apt install -y nginx

# Copy the Nginx configuration
sudo cp /opt/booksy/deployment/nginx/booksy.conf /etc/nginx/sites-available/booksy

# Update domain in config
sudo sed -i 's/booksy.yourdomain.com/booksy.com/g' /etc/nginx/sites-available/booksy

# Enable the site
sudo ln -s /etc/nginx/sites-available/booksy /etc/nginx/sites-enabled/
sudo rm -f /etc/nginx/sites-enabled/default

# Test and reload
sudo nginx -t
sudo systemctl reload nginx
```

### Step 3: Update Environment Variables

```bash
sudo nano /opt/booksy/.env
```

Update:
```env
API_BASE_URL=https://booksy.com
```

Restart services:
```bash
cd /opt/booksy
docker-compose -f docker-compose.prod.yml restart
```

---

## 2. SSL/TLS with Let's Encrypt

### Automated Setup

```bash
# On your server
cd /opt/booksy/deployment/scripts
chmod +x setup-ssl.sh

# Run the SSL setup script
sudo ./setup-ssl.sh booksy.com admin@booksy.com
```

This script will:
- Install Certbot
- Configure Nginx for SSL
- Obtain SSL certificate from Let's Encrypt
- Set up automatic renewal

### Manual Certificate Renewal

```bash
# Test renewal (dry run)
sudo certbot renew --dry-run

# Force renewal
sudo certbot renew --force-renewal
sudo systemctl reload nginx
```

### SSL Configuration

The setup includes:
- ✅ TLS 1.2 and 1.3
- ✅ Strong cipher suites
- ✅ HSTS (HTTP Strict Transport Security)
- ✅ Security headers
- ✅ Automatic HTTP → HTTPS redirect

### Access Points After SSL:

- Frontend: `https://booksy.com`
- API: `https://booksy.com/api/`
- Logs: `https://booksy.com/logs/`
- Monitoring: `https://booksy.com/monitoring/`
- RabbitMQ: `https://booksy.com/rabbitmq/`
- pgAdmin: `https://booksy.com/pgadmin/`

---

## 3. Monitoring with Prometheus & Grafana

### Step 1: Start Monitoring Stack

```bash
cd /opt/booksy

# Start monitoring services
docker-compose -f docker-compose.prod.yml -f docker-compose.monitoring.yml up -d
```

### Step 2: Access Grafana

1. Navigate to: `http://YOUR_SERVER_IP:3000`
2. Login:
   - Username: `admin`
   - Password: (from `.env` file - `GRAFANA_ADMIN_PASSWORD`)

### Step 3: Import Dashboards

Grafana comes with pre-configured datasources. Import these dashboards:

1. **Node Exporter Dashboard** (ID: 1860)
   - Go to: Dashboards → Import → 1860
   - Select Prometheus datasource

2. **PostgreSQL Dashboard** (ID: 9628)
   - Go to: Dashboards → Import → 9628
   - Select Prometheus datasource

3. **Redis Dashboard** (ID: 11835)
   - Go to: Dashboards → Import → 11835
   - Select Prometheus datasource

### Available Metrics

The monitoring stack collects:

- **System Metrics**: CPU, Memory, Disk, Network
- **PostgreSQL Metrics**: Connections, queries, cache hits
- **Redis Metrics**: Memory, commands, keys, hit rate
- **Application Metrics**: Response times, error rates (if instrumented)

### Step 4: Add .NET Metrics (Optional)

Install Prometheus metrics in your .NET APIs:

```bash
dotnet add package prometheus-net.AspNetCore
```

Update `Program.cs`:
```csharp
using Prometheus;

var app = builder.Build();

// Add metrics endpoint
app.UseMetricServer();
app.UseHttpMetrics();
```

---

## 4. Alerting Configuration

### Step 1: Configure Slack Notifications

1. Create a Slack webhook:
   - Go to: https://api.slack.com/messaging/webhooks
   - Create incoming webhook
   - Copy webhook URL

2. Update Alertmanager config:
```bash
sudo nano /opt/booksy/deployment/monitoring/alertmanager/config.yml
```

Replace `YOUR_SLACK_WEBHOOK_URL_HERE` with your webhook URL.

3. Create Slack channels:
   - `#booksy-alerts` - All alerts
   - `#booksy-critical` - Critical alerts only
   - `#booksy-warnings` - Warning alerts
   - `#booksy-database` - Database-specific alerts
   - `#booksy-infrastructure` - Infrastructure alerts

4. Restart Alertmanager:
```bash
docker-compose -f docker-compose.monitoring.yml restart alertmanager
```

### Step 2: Configure Email Notifications (Optional)

Edit `/opt/booksy/deployment/monitoring/alertmanager/config.yml`:

```yaml
global:
  smtp_from: 'alertmanager@booksy.com'
  smtp_smarthost: 'smtp.gmail.com:587'
  smtp_auth_username: 'your-email@gmail.com'
  smtp_auth_password: 'your-app-password'
```

Uncomment email configs in receivers section.

### Available Alerts

The system monitors:

- **Service Health**: Down services, container restarts
- **Resource Usage**: CPU, memory, disk space
- **Database**: PostgreSQL down, high connections
- **Cache**: Redis down, memory usage
- **Performance**: API response times, error rates

### Alert Severity Levels

- **Critical**: Immediate attention required (service down, disk full)
- **Warning**: Should be investigated (high CPU, slow responses)

### Test Alerts

```bash
# Access Alertmanager
http://YOUR_SERVER_IP:9093

# Trigger a test alert
docker stop booksy-usermanagement-api
# Wait 2 minutes for alert
docker start booksy-usermanagement-api
```

---

## 5. Staging Environment

### Purpose

Staging environment allows you to:
- Test changes before production
- Validate deployments
- Run integration tests
- Demo new features

### Step 1: Create Staging Configuration

```bash
# On your server
cd /opt/booksy
cp deployment/.env.staging.example .env.staging

# Edit staging config
sudo nano .env.staging
```

Update passwords and settings.

### Step 2: Start Staging Environment

```bash
cd /opt/booksy

# Start staging services
docker-compose -f docker-compose.staging.yml up -d

# Check status
docker-compose -f docker-compose.staging.yml ps
```

### Step 3: Access Staging

Staging runs on different ports:

- Frontend: `http://YOUR_SERVER_IP:8080`
- API Gateway: `http://YOUR_SERVER_IP:6000`
- User Management: `http://YOUR_SERVER_IP:6001`
- Service Catalog: `http://YOUR_SERVER_IP:6002`
- Seq Logs: `http://YOUR_SERVER_IP:6341`

### Step 4: Deploy to Staging

Create a staging deployment workflow:

```yaml
# .github/workflows/deploy-staging.yml
name: Deploy to Staging

on:
  push:
    branches: [ "develop" ]

# ... similar to deploy.yml but uses staging config
```

### Staging vs Production

| Aspect | Production | Staging |
|--------|-----------|---------|
| Branch | `master` | `develop` |
| Ports | 80, 5000-5002 | 8080, 6000-6002 |
| Resources | Full | Limited (50%) |
| Data | Real data | Test data |
| Monitoring | Full alerts | Limited alerts |

### Managing Both Environments

```bash
# View all services
docker ps

# Production logs
docker-compose -f docker-compose.prod.yml logs -f

# Staging logs
docker-compose -f docker-compose.staging.yml logs -f

# Restart production
docker-compose -f docker-compose.prod.yml restart

# Restart staging
docker-compose -f docker-compose.staging.yml restart
```

---

## Quick Reference Commands

### SSL/TLS
```bash
# Check certificate expiry
sudo certbot certificates

# Renew certificate
sudo certbot renew
```

### Monitoring
```bash
# Start monitoring
docker-compose -f docker-compose.monitoring.yml up -d

# View Prometheus targets
http://YOUR_SERVER_IP:9090/targets

# View Grafana
http://YOUR_SERVER_IP:3000
```

### Alerting
```bash
# View alerts
http://YOUR_SERVER_IP:9093

# Test Slack webhook
curl -X POST -H 'Content-type: application/json' \
  --data '{"text":"Test alert from Booksy"}' \
  YOUR_SLACK_WEBHOOK_URL
```

### Staging
```bash
# Start staging
docker-compose -f docker-compose.staging.yml up -d

# Stop staging
docker-compose -f docker-compose.staging.yml down

# Deploy specific version to staging
STAGING_TAG=v1.2.3 docker-compose -f docker-compose.staging.yml up -d
```

---

## Troubleshooting

### SSL Certificate Issues

```bash
# Check Nginx error logs
sudo tail -f /var/log/nginx/error.log

# Validate certificate
openssl s_client -connect booksy.com:443 -servername booksy.com

# Force certificate renewal
sudo certbot renew --force-renewal
```

### Monitoring Not Working

```bash
# Check Prometheus targets
curl http://localhost:9090/api/v1/targets

# Check if exporters are running
docker ps | grep exporter

# Restart monitoring stack
docker-compose -f docker-compose.monitoring.yml restart
```

### Alerts Not Firing

```bash
# Check Alertmanager status
curl http://localhost:9093/api/v1/status

# View alert rules
curl http://localhost:9090/api/v1/rules

# Check Alertmanager logs
docker logs booksy-alertmanager
```

---

## Next Steps

1. ✅ Set up custom domain
2. ✅ Enable SSL/TLS
3. ✅ Configure monitoring
4. ✅ Set up alerting to Slack
5. ✅ Deploy staging environment
6. ⏭️ Set up automated backups
7. ⏭️ Configure CDN (Cloudflare)
8. ⏭️ Implement log aggregation
9. ⏭️ Add performance testing

---

## Support Resources

- **Prometheus Docs**: https://prometheus.io/docs/
- **Grafana Docs**: https://grafana.com/docs/
- **Let's Encrypt**: https://letsencrypt.org/docs/
- **Nginx SSL Guide**: https://nginx.org/en/docs/http/configuring_https_servers.html
