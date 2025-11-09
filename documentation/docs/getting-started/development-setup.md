---
sidebar_position: 1
---

# Development Setup

This guide will help you set up the Booksy platform on your local development machine.

## Prerequisites

Before you begin, ensure you have the following installed:

### Required Software

- **.NET 9.0 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/9.0)
- **PostgreSQL 14+** - [Download](https://www.postgresql.org/download/)
- **Redis 7+** - [Download](https://redis.io/download)
- **Git** - [Download](https://git-scm.com/downloads)
- **Docker Desktop** (Optional but recommended) - [Download](https://www.docker.com/products/docker-desktop)

### Optional Tools

- **Visual Studio 2022** or **Rider** - IDE
- **Postman** or **Insomnia** - API testing
- **pgAdmin** - PostgreSQL management
- **RedisInsight** - Redis management

## Quick Start with Docker

The easiest way to run Booksy is using Docker Compose:

```bash
# Clone the repository
git clone https://github.com/kazemim99/Booking.git
cd Booking

# Start all services
docker-compose up

# APIs will be available at:
# UserManagement API: http://localhost:5000
# ServiceCatalog API: http://localhost:5010
```

That's it! The Docker Compose setup includes:
- PostgreSQL database
- Redis cache
- Both API services
- Database migrations

## Manual Setup

If you prefer to run services individually:

### 1. Clone the Repository

```bash
git clone https://github.com/kazemim99/Booking.git
cd Booking
```

### 2. Setup PostgreSQL

Create two databases:

```sql
CREATE DATABASE booksy_usermanagement;
CREATE DATABASE booksy_servicecatalog;
```

### 3. Setup Redis

Start Redis server:

```bash
# Linux/Mac
redis-server

# Windows (using Redis for Windows)
redis-server.exe
```

### 4. Configure Connection Strings

Update `appsettings.Development.json` in each API project:

**UserManagement API**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=booksy_usermanagement;Username=postgres;Password=yourpassword",
    "Redis": "localhost:6379"
  }
}
```

**ServiceCatalog API**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=booksy_servicecatalog;Username=postgres;Password=yourpassword",
    "Redis": "localhost:6379"
  }
}
```

### 5. Run Database Migrations

```bash
# UserManagement database
cd src/UserManagement/Booksy.UserManagement.Infrastructure
dotnet ef database update --startup-project ../Booksy.UserManagement.API

# ServiceCatalog database
cd ../../../src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure
dotnet ef database update --startup-project ../Booksy.ServiceCatalog.Api
```

### 6. Run the APIs

Open two terminal windows:

**Terminal 1 - UserManagement API**:
```bash
cd src/UserManagement/Booksy.UserManagement.API
dotnet run
# Running on http://localhost:5000
```

**Terminal 2 - ServiceCatalog API**:
```bash
cd src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api
dotnet run
# Running on http://localhost:5010
```

## Verify Installation

### 1. Check Health Endpoints

```bash
# UserManagement API
curl http://localhost:5000/health

# ServiceCatalog API
curl http://localhost:5010/health
```

You should receive a `200 OK` response with `Healthy` status.

### 2. Access Swagger UI

Open your browser and navigate to:

- **UserManagement API**: http://localhost:5000/swagger
- **ServiceCatalog API**: http://localhost:5010/swagger

### 3. Test Authentication

Using the Swagger UI or Postman:

1. **Register a new user**:
   ```http
   POST http://localhost:5000/api/v1/users
   Content-Type: application/json

   {
     "email": "test@example.com",
     "password": "SecurePass@123",
     "firstName": "Test",
     "lastName": "User",
     "phoneNumber": "09123456789"
   }
   ```

2. **Login**:
   ```http
   POST http://localhost:5000/api/v1/auth/login
   Content-Type: application/json

   {
     "email": "test@example.com",
     "password": "SecurePass@123"
   }
   ```

3. **Copy the access token** from the response
4. **Use the token** in subsequent requests:
   ```http
   Authorization: Bearer your_access_token_here
   ```

## Using Postman Collection

We provide a comprehensive Postman collection with 120+ endpoints:

1. **Download files**:
   - [Postman Collection](/Booksy_API_Collection.postman_collection.json)
   - [Environment File](/Booksy_API.postman_environment.json)

2. **Import into Postman**:
   - Open Postman → Import → Select both files

3. **Configure environment**:
   - Select "Booksy API - Local Development" environment
   - Update `baseUrl` and `servicecat_baseUrl` if needed

4. **Start testing**:
   - All sample data includes Iranian cultural context
   - Tokens are automatically saved to environment variables

See the [Postman Collection Guide](../guides/postman-collection) for detailed usage.

## Development Tools

### Hot Reload

Both APIs support hot reload during development:

```bash
dotnet watch run
```

Changes to C# files will automatically rebuild and restart the application.

### Database Migrations

Create a new migration:

```bash
# UserManagement
cd src/UserManagement/Booksy.UserManagement.Infrastructure
dotnet ef migrations add MigrationName --startup-project ../Booksy.UserManagement.API

# ServiceCatalog
cd src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure
dotnet ef migrations add MigrationName --startup-project ../Booksy.ServiceCatalog.Api
```

### Logging

Logs are written to:
- **Console** - For development
- **Files** - `logs/` directory
- **Seq** (if configured) - Structured logging UI

View logs in real-time:

```bash
tail -f logs/booksy-usermanagement-*.txt
tail -f logs/booksy-servicecatalog-*.txt
```

## Common Issues

### Port Already in Use

If ports 5000 or 5010 are already in use:

1. Find the process:
   ```bash
   # Linux/Mac
   lsof -i :5000
   lsof -i :5010

   # Windows
   netstat -ano | findstr :5000
   netstat -ano | findstr :5010
   ```

2. Kill the process or change the port in `Program.cs`

### Database Connection Failed

Ensure PostgreSQL is running:

```bash
# Check PostgreSQL status
# Linux
sudo systemctl status postgresql

# Mac
brew services list | grep postgresql

# Windows
# Check Services → PostgreSQL
```

### Redis Connection Failed

Ensure Redis is running:

```bash
# Test Redis connection
redis-cli ping
# Should return: PONG
```

## Next Steps

- [Explore the Architecture](../architecture/overview)
- [Read the API Documentation](../api/usermanagement)
- [Learn about Booking Flow](../guides/booking-flow)
- [Integrate ZarinPal Payments](../guides/payment-integration)

## Need Help?

- Check [GitHub Issues](https://github.com/kazemim99/Booking/issues)
- Read the [Architecture Guide](../architecture/overview)
- Review the [Postman Collection](../guides/postman-collection)
