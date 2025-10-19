# 📊 Booksy Platform - Observability & Monitoring Guide

## Overview

This document describes the comprehensive observability and monitoring setup for the Booksy platform, including centralized logging, distributed tracing, metrics collection, and health monitoring.

---

## 🎯 Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                      OBSERVABILITY STACK                         │
└─────────────────────────────────────────────────────────────────┘

┌──────────────────┐     ┌──────────────────┐     ┌──────────────┐
│ ServiceCatalog   │────▶│   Serilog        │────▶│   Seq        │
│      API         │     │   (Logging)      │     │  (http:5341) │
└──────────────────┘     └──────────────────┘     └──────────────┘
                                                            │
┌──────────────────┐     ┌──────────────────┐              │
│ UserManagement   │────▶│   Serilog        │──────────────┤
│      API         │     │   (Logging)      │              │
└──────────────────┘     └──────────────────┘              │
                                                            │
                         ┌──────────────────┐              │
                         │  File Logs       │◀─────────────┘
                         │  (JSON + Text)   │
                         └──────────────────┘
```

---

## 📦 Components

### 1. Seq - Centralized Logging

**Purpose:** Centralized structured logging platform for all microservices

**Access:**
- **URL:** http://localhost:5341
- **Username:** admin
- **Password:** Booksy@2024!

**Features:**
- Real-time log streaming
- Full-text search across all logs
- Structured log querying (Serilog + JSON)
- Log correlation by RequestId, UserId, TraceId
- Custom dashboards and alerts
- Retention: 30 days

**Docker Configuration:**
```yaml
seq:
  image: datalust/seq:latest
  ports:
    - "5341:80"        # Seq UI
    - "5342:5341"      # Seq ingestion endpoint
  environment:
    - ACCEPT_EULA=Y
    - SEQ_FIRSTRUN_ADMINUSERNAME=admin
    - SEQ_FIRSTRUN_ADMINPASSWORDHASH=Booksy@2024!
```

---

### 2. Serilog - Structured Logging

**Purpose:** .NET logging framework with structured logging support

**Sinks Configured:**
1. **Console** - Development debugging
2. **File (Rolling)** - Local backup logs (30-day retention)
3. **File (JSON)** - Machine-readable structured logs
4. **Seq** - Centralized logging platform

**Enrichers:**
- `FromLogContext` - Adds contextual properties (RequestId, UserId)
- `WithMachineName` - Server/container identification
- `WithEnvironmentName` - Environment (Development/Staging/Production)
- `WithThreadId` - Thread identification for debugging
- `WithExceptionDetails` - Detailed exception information
- Custom properties:
  - `Application` - API name (e.g., "ServiceCatalog.API")
  - `Environment` - Deployment environment

**Log Levels:**
```
Debug    - Detailed diagnostic information (only in Development)
Information - General informational messages
Warning  - Potentially harmful situations
Error    - Error events (but application continues)
Fatal    - Critical errors causing application shutdown
```

**Configuration Location:**
```
src/Infrastructure/Booksy.API/Observability/SerilogConfiguration.cs
```

---

### 3. File Logging

**Location:** `logs/` directory in each API project root

**Log Files:**
1. **Text Logs:** `{ApplicationName}-YYYY-MM-DD.log`
   - Human-readable format
   - Timestamp, Level, Source, Message, Exception
   - Example: `ServiceCatalog.API-2025-01-15.log`

2. **JSON Logs:** `{ApplicationName}-YYYY-MM-DD.json`
   - Machine-readable compact JSON format
   - Easy parsing for external tools (ELK, Splunk)
   - Example: `ServiceCatalog.API-2025-01-15.json`

**Retention:** 30 days (configurable in `SerilogConfiguration.cs`)

---

## 🚀 Getting Started

### 1. Start Observability Infrastructure

```bash
# Start Seq container
docker-compose up -d seq

# Verify Seq is running
docker ps | grep seq

# Access Seq UI
http://localhost:5341
```

### 2. Configure APIs

All APIs are pre-configured with Serilog + Seq. Configuration is in `appsettings.json`:

```json
{
  "Observability": {
    "Seq": {
      "Url": "http://localhost:5342",
      "ApiKey": ""  // Optional, for production use API keys
    }
  }
}
```

**For Docker deployments**, update Seq URL:
```json
{
  "Observability": {
    "Seq": {
      "Url": "http://seq:5341",  // Use Docker service name
      "ApiKey": ""
    }
  }
}
```

### 3. Run APIs

```bash
# Run ServiceCatalog API
dotnet run --project src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api

# Run UserManagement API
dotnet run --project src/UserManagement/Booksy.UserManagement.API
```

### 4. View Logs in Seq

1. Open http://localhost:5341
2. Login with credentials
3. Search logs by:
   - **Application name:** `Application = 'ServiceCatalog.API'`
   - **Log level:** `@Level = 'Error'`
   - **Time range:** Use time picker
   - **Request ID:** `RequestId = 'abc123'`
   - **User ID:** `UserId = 'guid'`

---

## 📝 Logging Best Practices

### 1. Structured Logging

**❌ Bad:**
```csharp
_logger.LogInformation("User " + userId + " created provider " + providerId);
```

**✅ Good:**
```csharp
_logger.LogInformation("User {UserId} created provider {ProviderId}", userId, providerId);
```

### 2. Log Levels

**Debug:** Detailed diagnostic information
```csharp
_logger.LogDebug("Validating provider registration request for {OwnerId}", request.OwnerId);
```

**Information:** General flow events
```csharp
_logger.LogInformation("Provider {ProviderId} registered successfully", provider.Id);
```

**Warning:** Unexpected situations (but application continues)
```csharp
_logger.LogWarning("Provider {ProviderId} has no services configured", providerId);
```

**Error:** Error events (exception logged)
```csharp
_logger.LogError(ex, "Failed to register provider for owner {OwnerId}", ownerId);
```

**Fatal:** Critical errors (application shutdown)
```csharp
_logger.LogFatal(ex, "Database connection failed. Shutting down.");
```

### 3. Correlation IDs

All HTTP requests automatically include a `RequestId` for correlation:

```csharp
// Automatically added by ASP.NET Core
using (LogContext.PushProperty("RequestId", HttpContext.TraceIdentifier))
{
    _logger.LogInformation("Processing request");
}
```

### 4. Exception Logging

```csharp
try
{
    await _repository.SaveAsync(entity);
}
catch (DbUpdateException ex)
{
    _logger.LogError(ex, "Failed to save provider {ProviderId}", providerId);
    throw; // Re-throw after logging
}
```

---

## 🔍 Querying Logs in Seq

### Basic Queries

**Find all errors:**
```
@Level = 'Error'
```

**Find logs from specific API:**
```
Application = 'ServiceCatalog.API'
```

**Find logs for specific user:**
```
UserId = 'your-user-guid'
```

**Find logs for specific provider:**
```
ProviderId = 'provider-guid'
```

**Combine filters:**
```
Application = 'ServiceCatalog.API' AND @Level = 'Error' AND UserId = 'guid'
```

### Time-Based Queries

**Last hour:**
```
@Timestamp > DateTime.Now.AddHours(-1)
```

**Specific date range:**
```
@Timestamp >= DateTime.Parse('2025-01-15') AND @Timestamp < DateTime.Parse('2025-01-16')
```

### Advanced Queries

**Find slow requests (> 5 seconds):**
```
Duration > 5000
```

**Find failed authentication attempts:**
```
@Message like '%authentication failed%'
```

**Find database errors:**
```
@Exception like '%DbUpdate%'
```

---

## 📊 Dashboards & Alerts

### Seq Dashboards

1. **Errors Dashboard**
   - All errors in last 24 hours
   - Group by Application
   - Group by Exception Type

2. **Performance Dashboard**
   - Request duration by endpoint
   - Slow queries (> 1 second)
   - Database operation duration

3. **User Activity Dashboard**
   - User registrations
   - Provider registrations
   - Service creations

### Setting Up Alerts

1. Navigate to **Alerts** in Seq UI
2. Create new alert with query:
   ```
   @Level = 'Error' AND Application = 'ServiceCatalog.API'
   ```
3. Configure notification (Email/Slack/Webhook)
4. Set threshold (e.g., > 10 errors in 5 minutes)

---

## 🔧 Troubleshooting

### Logs not appearing in Seq

1. **Check Seq is running:**
   ```bash
   docker ps | grep seq
   ```

2. **Check API can reach Seq:**
   ```bash
   curl http://localhost:5342/api/health
   ```

3. **Check Seq URL in appsettings.json:**
   ```json
   {
     "Observability": {
       "Seq": {
         "Url": "http://localhost:5342"  // Correct URL
       }
     }
   }
   ```

4. **Check Serilog configuration in Program.cs:**
   ```csharp
   .UseSerilogWithConfiguration("ServiceCatalog.API")
   ```

### Seq UI not accessible

1. **Check port mapping:**
   ```bash
   docker port booksy-seq
   ```

2. **Check Docker logs:**
   ```bash
   docker logs booksy-seq
   ```

3. **Restart Seq container:**
   ```bash
   docker-compose restart seq
   ```

---

## 🌐 Production Configuration

### 1. Seq API Keys

For production, use API keys for security:

1. Generate API key in Seq UI:
   - Navigate to **Settings → API Keys**
   - Create new key with appropriate permissions
   - Copy the key

2. Update `appsettings.Production.json`:
   ```json
   {
     "Observability": {
       "Seq": {
         "Url": "https://seq.booksy.com",
         "ApiKey": "your-production-api-key"
       }
     }
   }
   ```

3. Store API key in environment variables or Azure Key Vault:
   ```bash
   export SEQ_API_KEY="your-production-api-key"
   ```

### 2. External Seq Hosting

For production, consider:
- **Seq Cloud:** https://datalust.co/seq/cloud
- **Self-hosted Seq:** On Kubernetes, Azure Container Instances, AWS ECS
- **Seq with load balancer:** For high-availability

### 3. Log Retention

Configure retention policies in Seq:
1. Navigate to **Settings → Retention**
2. Set retention period (e.g., 90 days for production)
3. Configure archival to blob storage (Azure/AWS S3)

---

## 📈 Next Steps

### Planned Enhancements

1. **Distributed Tracing**
   - Add OpenTelemetry instrumentation
   - Integrate with Jaeger or Zipkin
   - Trace requests across microservices

2. **Metrics Collection**
   - Add Prometheus exporters
   - Create Grafana dashboards
   - Monitor:
     - Request rate
     - Error rate
     - Response time (P50, P95, P99)
     - Database connection pool
     - Memory/CPU usage

3. **Application Performance Monitoring (APM)**
   - Integrate with Application Insights
   - Or use Datadog/New Relic
   - Monitor end-to-end user journeys

4. **Real-time Alerting**
   - Integrate with PagerDuty
   - Configure Slack/Teams notifications
   - Set up on-call rotations

---

## 📚 Resources

- **Serilog Documentation:** https://serilog.net/
- **Seq Documentation:** https://docs.datalust.co/
- **Serilog Best Practices:** https://github.com/serilog/serilog/wiki/Best-Practices
- **Structured Logging Guide:** https://github.com/serilog/serilog/wiki/Structured-Data

---

## 👥 Support

For questions or issues:
- **Email:** devops@booksy.com
- **Slack:** #observability
- **Documentation:** https://docs.booksy.com/observability

---

**Last Updated:** January 2025
**Version:** 1.0.0
