# Project Context

## Purpose
Booksy Admin Panel is a Vue 3-based administrative dashboard for managing the Booksy booking platform. It provides interfaces for managing users, providers, services, bookings, analytics, payments, and system settings.

## Tech Stack
- **Frontend Framework**: Vue 3 (Composition API) with TypeScript
- **Build Tool**: Vite 7.2.4
- **UI Framework**: Ant Design Vue 4.2.6
- **State Management**: Pinia 3.0.4
- **Router**: Vue Router 4.6.4
- **HTTP Client**: Axios 1.13.2
- **Charts**: ECharts 6.0.0 with vue-echarts 8.0.1
- **Date Handling**: dayjs 1.11.19
- **Utilities**: @vueuse/core 14.1.0

## Project Conventions

### Code Style
- TypeScript strict mode enabled
- Vue 3 Composition API with `<script setup>` syntax
- Single File Components (SFC) with scoped styles
- Kebab-case for file names and component names
- PascalCase for TypeScript interfaces and types
- camelCase for variables and functions

### Architecture Patterns
- Feature-based directory structure under `src/views/`
- Centralized API services in `src/api/`
- Pinia stores for state management
- Axios interceptors for authentication and error handling
- Layout-based routing with AdminLayout wrapper
- Role-based access control (Admin, Provider, Client)

### Testing Strategy
- Currently no automated tests (manual testing via TESTING_GUIDE.md)
- API integration tests documented in API_INTEGRATION.md
- Health check endpoints for all services

### Git Workflow
- Main branch: `master`
- Deployment via GitHub Actions (build-and-push.yml, deploy.yml)
- Docker-based deployment to production
- Images pushed to GitHub Container Registry (ghcr.io)

## Domain Context
Booksy is a microservices-based booking platform for service providers (salons, clinics, etc.). The admin panel connects to:
- **UserManagement API** (port 5001): User auth, registration, profiles
- **ServiceCatalog API** (port 5002): Services, categories, availability
- **Gateway API** (port 5000): API Gateway routing all requests
- **Backend Services**: PostgreSQL, Redis, RabbitMQ, Seq logging

The target market is Persian-speaking users in Iran, requiring Persian (Farsi) language support with right-to-left (RTL) text direction.

## Important Constraints
- Must maintain compatibility with Ant Design Vue 4.x
- API endpoints use PascalCase routing (case-sensitive)
- Backend services run in Docker containers
- Production deployment to napstar.ir
- Must support both Persian (RTL) and English (LTR) languages
- Browser compatibility: Modern browsers (Chrome, Firefox, Edge, Safari)

## External Dependencies
- **Backend Gateway**: http://napstar.ir/api/v1 (production) or http://localhost:5000/api/v1 (development)
- **GitHub Container Registry**: ghcr.io/kazemim99/* for Docker images
- **Font Assets**: Will require Persian fonts (Vazir, B Nazanin) for proper text rendering
- **Ant Design Vue**: UI component library with built-in i18n and RTL support capabilities
