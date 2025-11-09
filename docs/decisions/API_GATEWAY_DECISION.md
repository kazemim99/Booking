# API Gateway Decision

**Date**: 2025-11-09
**Status**: Deferred
**Decision Maker**: Architecture Review

## Context

The project currently has an API Gateway scaffold (`/src/APIGateway/Booksy.Gateway/`) with:
- Empty `ocelot.json` configuration (`{}`)
- Only boilerplate WeatherForecast endpoint
- No actual routing or gateway logic implemented
- Referenced in README but not functional

## Current Architecture

Booksy is a **modular monolith** with:
- Bounded contexts in same process (ServiceCatalog, UserManagement)
- Direct HTTP calls between contexts (e.g., `ProviderInfoService.cs`)
- Event-driven integration via RabbitMQ + CAP
- Single deployable unit

## Decision

**Remove the API Gateway scaffold for now.** Reasons:

### Why NOT Implement Now

1. **No Distribution**: Bounded contexts share the same process space
   - Inter-context calls are in-process or local HTTP
   - No network latency to optimize
   - Gateway adds complexity without benefit

2. **Single Entry Point Already Exists**: Each API has its own controllers
   - ServiceCatalog.Api exposes all ServiceCatalog endpoints
   - UserManagement.API exposes all UserManagement endpoints
   - Clients can connect directly

3. **YAGNI (You Aren't Gonna Need It)**:
   - No microservices deployment planned immediately
   - No multiple client apps requiring unified API
   - No need for aggregation, transformation, or BFF pattern yet

4. **Maintenance Burden**:
   - Empty scaffold creates confusion
   - README claims feature exists but it doesn't
   - Route configuration would duplicate controller routes

### When to Revisit

Implement API Gateway when you have:

1. **Microservices Deployment**
   - Bounded contexts become separate deployments
   - Need centralized routing and service discovery
   - Multiple backend services to coordinate

2. **Multiple Client Types**
   - Mobile app needing different payload formats (BFF pattern)
   - Third-party API consumers needing versioned contracts
   - Public API vs internal API separation

3. **Cross-Cutting Concerns**
   - Centralized authentication/authorization
   - Request aggregation (GraphQL-style)
   - Rate limiting per client/tenant (currently per-API)
   - API composition from multiple services

4. **Advanced Patterns**
   - Circuit breakers for resilience
   - Request/response transformation
   - A/B testing or canary deployments

## Recommended Gateway Technologies (Future)

When you do need a gateway:

1. **YARP (YARP: A Reverse Proxy)** - Microsoft's modern .NET proxy
   - Native .NET integration
   - High performance
   - Code-based configuration (strongly typed)
   - Good for .NET-only ecosystems

2. **Ocelot** - Lightweight .NET API Gateway
   - Simpler than YARP for basic routing
   - JSON configuration
   - Good middleware ecosystem

3. **Kong/Nginx** - Production-grade external gateways
   - Language-agnostic
   - Battle-tested at scale
   - Rich plugin ecosystem
   - Best for polyglot microservices

## Alternative: Keep Modular Monolith Optimizations

Instead of gateway, focus on:

1. **HTTP/2** - Already supported in .NET 9
   - Multiplexing
   - Header compression
   - No code changes needed

2. **Response Compression** - Already configured
   - Reduces payload sizes
   - Better than gateway routing for performance

3. **Caching Layer** - Already using Redis
   - Distributed caching for frequently accessed data
   - Cache-aside pattern in repositories

4. **Direct Service-to-Service Calls**
   - Continue using `HttpClient` with `IHttpClientFactory`
   - Proper connection pooling already in place
   - Add Polly for retry/circuit breaker if needed

## Impact of This Decision

- **Code Cleanup**: Remove `/src/APIGateway/Booksy.Gateway/` project
- **Documentation**: Update README to remove API Gateway from current features
- **Build**: Remove gateway from solution file (if referenced)
- **Docker**: Gateway not needed in docker-compose

## Monitoring Instead

Since we're not using a gateway, ensure observability through:

- ✅ **Distributed Tracing** (Jaeger) - Track requests across contexts
- ✅ **Metrics** (Prometheus + Grafana) - Monitor individual API performance
- ✅ **Logging** (Seq + Serilog) - Centralized structured logs
- ✅ **Health Checks** - `/health`, `/health/ready`, `/health/live`

## Conclusion

The API Gateway scaffold is **premature architecture** for the current modular monolith. Remove it to reduce confusion and maintenance burden. Revisit when migrating to true microservices or when cross-cutting concerns require centralized management.

**Action Items**:
1. ❌ Remove `/src/APIGateway/` directory (optional cleanup)
2. ✅ Update README to remove gateway from "Implemented Features"
3. ✅ Document this decision for future reference
4. 📋 Add gateway implementation to backlog for microservices phase

---

**References**:
- [Martin Fowler - API Gateway Pattern](https://microservices.io/patterns/apigateway.html)
- [Microsoft YARP Documentation](https://microsoft.github.io/reverse-proxy/)
- [Ocelot Documentation](https://ocelot.readthedocs.io/)
