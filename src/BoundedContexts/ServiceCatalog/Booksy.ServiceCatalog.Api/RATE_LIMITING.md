# API Rate Limiting Documentation

## Overview

The ServiceCatalog API implements rate limiting using AspNetCoreRateLimit library with Redis backend for distributed scenarios. Rate limits vary based on user authentication level.

## Rate Limit Tiers

| User Type | Rate Limit | Description |
|-----------|------------|-------------|
| **Anonymous** | 100 requests/minute | Unauthenticated users |
| **Authenticated** | 1000 requests/minute | Authenticated users with valid JWT token |
| **Admin** | 999,999 requests/minute | Admin users (effectively unlimited) |

## Whitelisted Endpoints

The following endpoints are exempt from rate limiting:
- `GET /health`
- `GET /health/ready`
- `GET /health/live`

## Configuration

Rate limiting is configured in `appsettings.json`:

```json
{
  "ClientRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "EndpointWhitelist": ["get:/health", "get:/health/ready", "get:/health/live"],
    "ClientRules": [
      {
        "ClientId": "anonymous",
        "Rules": [{ "Endpoint": "*", "Period": "1m", "Limit": 100 }]
      },
      {
        "ClientId": "authenticated",
        "Rules": [{ "Endpoint": "*", "Period": "1m", "Limit": 1000 }]
      },
      {
        "ClientId": "admin",
        "Rules": [{ "Endpoint": "*", "Period": "1m", "Limit": 999999 }]
      }
    ]
  }
}
```

## Components

### 1. ClientRateLimitResolver

Located at: `src/Infrastructure/Booksy.API/Middleware/ClientRateLimitResolver.cs`

Resolves client identity based on:
- **Admin**: User has "Admin" role claim
- **Authenticated**: User has valid authentication
- **Anonymous**: No authentication present

### 2. CustomRateLimitMiddleware

Located at: `src/Infrastructure/Booksy.API/Middleware/CustomRateLimitMiddleware.cs`

Returns RFC 7807 Problem Details format for rate limit errors:

```json
{
  "type": "https://tools.ietf.org/html/rfc6585#section-4",
  "title": "Too Many Requests",
  "status": 429,
  "detail": "API rate limit exceeded. Please retry after 60 seconds.",
  "instance": "/api/v1/services",
  "traceId": "0HMVFE...",
  "retryAfter": "60"
}
```

## Response Headers

When rate limit is exceeded, the following headers are included:

- `Retry-After`: Seconds to wait before retrying
- `X-RateLimit-Limit`: Information about limits by user type
- `X-RateLimit-Remaining`: 0
- `X-RateLimit-Reset`: Same as Retry-After

## Redis Backend

Rate limiting uses Redis for distributed scenarios, allowing multiple API instances to share rate limit counters.

**Connection String**: Configured in `appsettings.json` under `ConnectionStrings:Redis`

## Testing

### Test Anonymous User Rate Limit

```bash
# Send 101 requests in quick succession
for i in {1..101}; do
  curl -X GET http://localhost:5010/api/v1/services
done

# Expected: Requests 1-100 succeed, request 101 returns 429
```

### Test Authenticated User Rate Limit

```bash
# Get JWT token
TOKEN=$(curl -X POST http://localhost:5010/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"user","password":"pass"}' | jq -r '.token')

# Send 1001 requests
for i in {1..1001}; do
  curl -X GET http://localhost:5010/api/v1/services \
    -H "Authorization: Bearer $TOKEN"
done

# Expected: Requests 1-1000 succeed, request 1001 returns 429
```

### Test Admin User (No Limit)

```bash
# Get admin JWT token
ADMIN_TOKEN=$(curl -X POST http://localhost:5010/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin"}' | jq -r '.token')

# Send many requests
for i in {1..2000}; do
  curl -X GET http://localhost:5010/api/v1/services \
    -H "Authorization: Bearer $ADMIN_TOKEN"
done

# Expected: All requests succeed
```

### Test Health Endpoint (Whitelisted)

```bash
# Send unlimited requests to health endpoint
for i in {1..200}; do
  curl -X GET http://localhost:5010/health
done

# Expected: All requests succeed regardless of rate limits
```

## Troubleshooting

### Rate limits not being enforced

1. Check Redis connection:
   ```bash
   redis-cli -h localhost -p 6379 -a "Redis@2024!" ping
   ```

2. Verify middleware order in `Startup.cs`:
   ```csharp
   app.UseMiddleware<CustomRateLimitMiddleware>();
   app.UseClientRateLimiting();
   app.UseAuthentication(); // Must be after rate limiting
   ```

3. Check logs for rate limiting messages:
   ```bash
   grep "Rate limit exceeded" logs/booksy-servicecatalog-*.txt
   ```

### Different limits for specific endpoints

Update `ClientRules` in `appsettings.json`:

```json
{
  "ClientId": "authenticated",
  "Rules": [
    {
      "Endpoint": "*/api/v1/services",
      "Period": "1m",
      "Limit": 500
    },
    {
      "Endpoint": "*",
      "Period": "1m",
      "Limit": 1000
    }
  ]
}
```

## Production Considerations

1. **Redis Clustering**: Use Redis cluster for high availability
2. **Monitoring**: Set up alerts for rate limit exceeded events
3. **Documentation**: Inform API consumers about rate limits
4. **Headers**: Consider adding `X-RateLimit-Remaining` to all responses
5. **Graceful Degradation**: Implement retry logic in clients

## References

- [AspNetCoreRateLimit Documentation](https://github.com/stefanprodan/AspNetCoreRateLimit)
- [RFC 7807 - Problem Details](https://tools.ietf.org/html/rfc7807)
- [RFC 6585 - Additional HTTP Status Codes](https://tools.ietf.org/html/rfc6585#section-4)
