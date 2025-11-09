---
sidebar_position: 1
slug: /
---

# Welcome to Booksy Documentation

Booksy is a comprehensive booking and appointment management platform built with Domain-Driven Design principles and modern .NET technologies.

## Quick Start

Choose your path:

- **[For Developers](./getting-started/development-setup)** - Set up your development environment
- **[For API Users](./api/usermanagement)** - Explore the API documentation
- **[Architecture Guide](./architecture/overview)** - Understand the system design

## Platform Overview

Booksy is designed using **Domain-Driven Design (DDD)** with clear bounded contexts, enabling scalability and maintainability.

### UserManagement API

Handles user authentication, registration, and profile management.

**Key Features:**
- JWT-based authentication
- Phone verification with OTP
- Customer and provider user types
- Role-based authorization

[Explore UserManagement API →](./api/usermanagement)

### ServiceCatalog API

Manages providers, services, bookings, and payments for the Iranian market.

**Key Features:**
- Provider onboarding and management
- Service catalog with Persian support
- Complete booking lifecycle
- ZarinPal payment integration
- Financial reporting and payouts

[Explore ServiceCatalog API →](./api/servicecatalog)

## Key Features

### 🔐 Secure Authentication
- JWT bearer token authentication
- Passwordless phone verification
- Refresh token support
- Rate limiting protection

### 📅 Booking Management
- Real-time availability checking
- Multi-step booking workflow
- Booking confirmation and cancellation
- No-show tracking
- Booking history

### 💳 Payment Integration
- **ZarinPal** - Iranian payment gateway
- Payment verification
- Refund support
- Financial reporting
- Provider payouts

### 🏢 Provider Management
- Multi-step provider registration
- Business profile management
- Staff management
- Service catalog
- Working hours and holidays
- Gallery and image uploads

### 📊 Analytics & Reporting
- Revenue reports (current/previous month)
- Booking analytics
- Provider earnings tracking
- Financial summaries

### 🌍 Iranian Cultural Context
- Persian language support (UTF-8)
- Iranian phone number formats (+98)
- Iranian Rial (IRR) currency
- Jalali calendar support
- Iranian business hours (Saturday-Thursday)
- National holidays (Nowruz, etc.)

## Architecture Highlights

- **Domain-Driven Design (DDD)** - Clear bounded contexts
- **CQRS Pattern** - Command Query Responsibility Segregation with MediatR
- **Clean Architecture** - Separation of concerns
- **Event-Driven** - Integration events for cross-context communication
- **PostgreSQL** - Relational database
- **Redis** - Caching and rate limiting
- **Docker** - Containerized deployment

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- PostgreSQL 14+
- Redis 7+
- Node.js 18+ (for documentation)

### Quick Setup

```bash
# Clone the repository
git clone https://github.com/kazemim99/Booking.git
cd Booking

# Run with Docker Compose
docker-compose up

# APIs will be available at:
# UserManagement API: http://localhost:5000
# ServiceCatalog API: http://localhost:5010
```

### Testing with Postman

Download our comprehensive Postman collection with 120+ endpoints and Iranian sample data:

- [Postman Collection](pathname:///Booksy_API_Collection.postman_collection.json)
- [Environment File](pathname:///Booksy_API.postman_environment.json)
- [Usage Guide](./guides/postman-collection)

## API Endpoints

### UserManagement (Port 5000)

- **Authentication** - Login, logout, refresh token
- **Phone Verification** - OTP-based verification
- **Users** - Registration, profile management
- **Customers** - Customer-specific operations

### ServiceCatalog (Port 5010)

- **Providers** - Provider CRUD and search
- **Services** - Service catalog management
- **Bookings** - Complete booking lifecycle
- **Payments** - ZarinPal integration
- **Availability** - Slot checking
- **Locations** - Iranian provinces and cities
- **Notifications** - User notifications
- **Financial** - Earnings and payouts

## Resources

- **[Swagger UI (UserManagement)](http://localhost:5000/swagger)** - Interactive API documentation
- **[Swagger UI (ServiceCatalog)](http://localhost:5010/swagger)** - Interactive API documentation
- **[GitHub Repository](https://github.com/kazemim99/Booking)** - Source code
- **[Postman Collection Guide](./guides/postman-collection)** - Testing guide

## Support

For questions, issues, or contributions:

- **GitHub Issues**: [Report bugs or request features](https://github.com/kazemim99/Booking/issues)
- **Email**: support@booksy.com

---

Ready to get started? Head to the [Development Setup Guide](./getting-started/development-setup) →
