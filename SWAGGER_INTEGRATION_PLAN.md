# Swagger Documentation Integration Plan

## Executive Summary

This document provides a comprehensive plan to integrate the existing Swagger/OpenAPI documentation with a professional documentation portal (DocFX or Docusaurus) for the Booksy platform. The goal is to create a unified documentation experience that combines API reference documentation (Swagger) with business documentation, architectural guides, and developer resources.

---

## Table of Contents

1. [Current State Analysis](#1-current-state-analysis)
2. [Integration Goals](#2-integration-goals)
3. [Technology Recommendation](#3-technology-recommendation)
4. [Implementation Steps](#4-implementation-steps)
5. [Configuration Samples](#5-configuration-samples)
6. [Security & Deployment](#6-security--deployment)
7. [Auto-Update Strategy](#7-auto-update-strategy)
8. [Professional Documentation Workflow](#8-professional-documentation-workflow)
9. [Maintenance & Best Practices](#9-maintenance--best-practices)

---

## 1. Current State Analysis

### 1.1 Existing Swagger Setup

Both bounded contexts (UserManagement and ServiceCatalog) have well-configured Swagger implementations:

**UserManagement API** (`http://localhost:5000`)
- **Location**: `src/UserManagement/Booksy.UserManagement.API`
- **Swagger Package**: Swashbuckle.AspNetCore 6.5.0
- **Current Route**: Root (`/`) - serves Swagger UI at `http://localhost:5000/`
- **Hosting Model**: Minimal Hosting (Program.cs)
- **API Versions**: v1, v2

**ServiceCatalog API** (`http://localhost:5010`)
- **Location**: `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api`
- **Swagger Package**: Swashbuckle.AspNetCore 6.5.0
- **Current Route**: Root (`/`) - serves Swagger UI at `http://localhost:5010/`
- **Hosting Model**: Traditional Startup.cs
- **API Versions**: v1, v2

### 1.2 Current Features

Both APIs include:

✅ **XML Documentation** - Generated from code comments
✅ **JWT Bearer Authentication** - Security definition configured
✅ **API Versioning** - Multi-version support (v1, v2)
✅ **Custom Filters**:
- `ApiVersionOperationFilter` - Adds version parameters
- `EnumSchemaFilter` - Displays enum names as strings
✅ **Deep Linking** - Navigate directly to operations
✅ **Collapsible Sections** - DocExpansion.None for clean UI

### 1.3 Current Documentation Structure

```
/docs/
├── api-design-notes.md
└── ZarinPal-Sandbox-Testing-Guide.md
```

**Status**: Basic markdown files, no formal documentation portal

---

## 2. Integration Goals

### 2.1 Primary Objectives

1. **Unified Documentation Portal** - Single entry point for all documentation
2. **API Reference Integration** - Seamless navigation from docs to Swagger UI
3. **Custom Routes** - Professional URL structure (e.g., `/docs/api`)
4. **Auto-Update** - Documentation stays synchronized with code changes
5. **Security** - Protect Swagger UI in production environments
6. **Developer Experience** - Easy to find, easy to use, easy to maintain

### 2.2 Success Criteria

- Documentation site accessible at `/docs`
- Swagger UI accessible at `/docs/api/usermanagement` and `/docs/api/servicecatalog`
- Navigation links from docs site to Swagger UI
- OpenAPI specs available for download
- Search functionality across all documentation
- Responsive design for mobile and desktop
- Version-specific documentation (v1, v2)

---

## 3. Technology Recommendation

### 3.1 Docusaurus (Recommended)

**Why Docusaurus over DocFX:**

| Feature | Docusaurus | DocFX |
|---------|-----------|-------|
| **Setup Complexity** | Simple (npm/yarn) | Complex (.NET tooling) |
| **Technology Stack** | React (modern web) | Razor/HTML (older) |
| **Customization** | Excellent (React components) | Limited |
| **API Documentation** | Via plugins (redoc, swagger-ui) | Native but limited |
| **Search** | Built-in (Algolia) | Basic |
| **Versioning** | Native support | Manual |
| **Deployment** | Any static host | IIS/.NET hosting |
| **Community** | Very active (Meta) | Small |
| **Mobile Support** | Excellent | Fair |

**Verdict**: Docusaurus is recommended for its modern architecture, ease of customization, and superior developer experience.

### 3.2 Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│                   docs.booksy.ir                        │
│                  (Docusaurus Site)                      │
│                                                         │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐   │
│  │  Guides     │  │  Tutorials  │  │   API Ref   │   │
│  └─────────────┘  └─────────────┘  └─────────────┘   │
│                                           │            │
│                                           ▼            │
│                    ┌───────────────────────────┐      │
│                    │   API Documentation       │      │
│                    │   (/docs/api)             │      │
│                    │                           │      │
│                    │  ┌─────────────────────┐ │      │
│                    │  │ UserManagement API  │ │      │
│                    │  │ (Swagger UI Embed)  │ │      │
│                    │  └─────────────────────┘ │      │
│                    │                           │      │
│                    │  ┌─────────────────────┐ │      │
│                    │  │ ServiceCatalog API  │ │      │
│                    │  │ (Swagger UI Embed)  │ │      │
│                    │  └─────────────────────┘ │      │
│                    └───────────────────────────┘      │
└─────────────────────────────────────────────────────────┘
           │                          │
           │                          │
           ▼                          ▼
  API Server (5000)         API Server (5010)
  (OpenAPI JSON)            (OpenAPI JSON)
```

---

## 4. Implementation Steps

### Phase 1: Docusaurus Setup (Week 1)

#### Step 1.1: Initialize Docusaurus

```bash
# Navigate to project root
cd /home/user/Booking

# Create documentation site
npx create-docusaurus@latest documentation classic

# Install additional plugins
cd documentation
npm install --save @docusaurus/plugin-client-redirects
npm install --save docusaurus-plugin-openapi-docs
npm install --save docusaurus-theme-openapi-docs
```

#### Step 1.2: Configure Docusaurus

Edit `documentation/docusaurus.config.js`:

```javascript
const config = {
  title: 'Booksy Documentation',
  tagline: 'Complete guide for the Booksy booking platform',
  url: 'https://docs.booksy.ir',
  baseUrl: '/',
  onBrokenLinks: 'throw',
  onBrokenMarkdownLinks: 'warn',

  organizationName: 'booksy',
  projectName: 'booksy-docs',

  presets: [
    [
      'classic',
      {
        docs: {
          routeBasePath: '/',
          sidebarPath: require.resolve('./sidebars.js'),
          editUrl: 'https://github.com/kazemim99/Booking/tree/master/documentation/',
        },
        theme: {
          customCss: require.resolve('./src/css/custom.css'),
        },
      },
    ],
  ],

  plugins: [
    [
      'docusaurus-plugin-openapi-docs',
      {
        id: 'openapi',
        docsPluginId: 'classic',
        config: {
          usermanagement: {
            specPath: '../src/UserManagement/Booksy.UserManagement.API/swagger/v1/swagger.json',
            outputDir: 'docs/api/usermanagement',
            sidebarOptions: {
              groupPathsBy: 'tag',
              categoryLinkSource: 'tag',
            },
          },
          servicecatalog: {
            specPath: '../src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/swagger/v1/swagger.json',
            outputDir: 'docs/api/servicecatalog',
            sidebarOptions: {
              groupPathsBy: 'tag',
              categoryLinkSource: 'tag',
            },
          },
        },
      },
    ],
  ],

  themes: ['docusaurus-theme-openapi-docs'],

  themeConfig: {
    navbar: {
      title: 'Booksy',
      logo: {
        alt: 'Booksy Logo',
        src: 'img/logo.svg',
      },
      items: [
        {
          type: 'doc',
          docId: 'intro',
          position: 'left',
          label: 'Documentation',
        },
        {
          to: '/api/usermanagement',
          label: 'UserManagement API',
          position: 'left',
        },
        {
          to: '/api/servicecatalog',
          label: 'ServiceCatalog API',
          position: 'left',
        },
        {
          href: 'https://github.com/kazemim99/Booking',
          label: 'GitHub',
          position: 'right',
        },
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: 'Documentation',
          items: [
            {
              label: 'Getting Started',
              to: '/',
            },
            {
              label: 'Architecture',
              to: '/architecture',
            },
          ],
        },
        {
          title: 'API Reference',
          items: [
            {
              label: 'UserManagement API',
              to: '/api/usermanagement',
            },
            {
              label: 'ServiceCatalog API',
              to: '/api/servicecatalog',
            },
          ],
        },
        {
          title: 'More',
          items: [
            {
              label: 'GitHub',
              href: 'https://github.com/kazemim99/Booking',
            },
          ],
        },
      ],
      copyright: `Copyright © ${new Date().getFullYear()} Booksy. Built with Docusaurus.`,
    },
    prism: {
      theme: require('prism-react-renderer/themes/github'),
      darkTheme: require('prism-react-renderer/themes/dracula'),
      additionalLanguages: ['csharp', 'json', 'bash'],
    },
    algolia: {
      appId: 'YOUR_APP_ID',
      apiKey: 'YOUR_SEARCH_API_KEY',
      indexName: 'booksy',
      contextualSearch: true,
    },
  },
};

module.exports = config;
```

#### Step 1.3: Create Initial Documentation Structure

```bash
# Create directory structure
mkdir -p documentation/docs/{getting-started,architecture,guides,tutorials,api}
mkdir -p documentation/static/img
```

Create `documentation/docs/intro.md`:

```markdown
---
sidebar_position: 1
---

# Welcome to Booksy Documentation

Booksy is a comprehensive booking and appointment management platform built with Domain-Driven Design principles.

## Quick Start

Choose your path:

- **[For Developers](./getting-started/development-setup)** - Set up your development environment
- **[For API Users](./api/usermanagement)** - Explore the API documentation
- **[Architecture Guide](./architecture/overview)** - Understand the system design

## Platform Components

### UserManagement API
Handles user authentication, registration, and profile management.

[Explore UserManagement API →](./api/usermanagement)

### ServiceCatalog API
Manages providers, services, bookings, and payments.

[Explore ServiceCatalog API →](./api/servicecatalog)

## Key Features

- 🔐 **Secure Authentication** - JWT-based authentication with phone verification
- 📅 **Booking Management** - Complete booking lifecycle from search to completion
- 💳 **Payment Integration** - ZarinPal integration for Iranian market
- 🏢 **Provider Management** - Multi-step provider onboarding and profile management
- 📊 **Analytics & Reporting** - Financial reports and booking analytics
```

---

### Phase 2: Swagger Integration (Week 1-2)

#### Step 2.1: Update API Swagger Routes

Currently, Swagger UI is served at the root. We need to change this to serve at `/swagger`:

**UserManagement API** - Update `SwaggerExtensions.cs:98`:

```csharp
public static IApplicationBuilder UseSwaggerConfiguration(
    this IApplicationBuilder app,
    IApiVersionDescriptionProvider provider)
{
    app.UseSwagger(c =>
    {
        c.RouteTemplate = "swagger/{documentName}/swagger.json";
    });

    app.UseSwaggerUI(options =>
    {
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint(
                $"/swagger/{description.GroupName}/swagger.json",
                description.GroupName.ToUpperInvariant());
        }

        // CHANGED: Move Swagger UI to /swagger instead of root
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "Booksy User Management API";
        options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
        options.EnableDeepLinking();
        options.ShowExtensions();

        // Add custom CSS and branding
        options.InjectStylesheet("/swagger-ui/custom.css");
        options.DocumentTitle = "Booksy UserManagement API - Swagger UI";
        options.HeadContent = @"
            <style>
                .swagger-ui .topbar { display: none; }
            </style>
        ";
    });

    return app;
}
```

**ServiceCatalog API** - Same changes to `SwaggerExtensions.cs:98`

#### Step 2.2: Enable Static OpenAPI Spec Export

Add a new endpoint to export OpenAPI specs as static files:

Create `src/UserManagement/Booksy.UserManagement.API/Controllers/OpenApiController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using System.Text.Json;

namespace Booksy.UserManagement.API.Controllers;

/// <summary>
/// Provides OpenAPI specification downloads
/// </summary>
[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("openapi")]
public class OpenApiController : ControllerBase
{
    private readonly ISwaggerProvider _swaggerProvider;

    public OpenApiController(ISwaggerProvider swaggerProvider)
    {
        _swaggerProvider = swaggerProvider;
    }

    /// <summary>
    /// Download OpenAPI specification in JSON format
    /// </summary>
    [HttpGet("v{version}/swagger.json")]
    [Produces("application/json")]
    public IActionResult GetSwaggerJson(string version)
    {
        var swagger = _swaggerProvider.GetSwagger($"v{version}");

        var options = new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return Content(
            JsonSerializer.Serialize(swagger, options),
            "application/json");
    }
}
```

**Add same controller to ServiceCatalog API**

#### Step 2.3: Create Build Task to Export Swagger Specs

Create `scripts/export-swagger-specs.sh`:

```bash
#!/bin/bash

# Export OpenAPI specifications for documentation site

echo "Starting API servers to export OpenAPI specs..."

# Start UserManagement API in background
cd src/UserManagement/Booksy.UserManagement.API
dotnet run --no-build &
USERMGMT_PID=$!

# Start ServiceCatalog API in background
cd ../../../src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api
dotnet run --no-build &
SERVICECAT_PID=$!

# Wait for APIs to start
echo "Waiting for APIs to start..."
sleep 10

# Create output directory
mkdir -p ../../../documentation/static/openapi

# Download OpenAPI specs
echo "Downloading UserManagement OpenAPI spec..."
curl -o ../../../documentation/static/openapi/usermanagement-v1.json \
  http://localhost:5000/swagger/v1/swagger.json

echo "Downloading ServiceCatalog OpenAPI spec..."
curl -o ../../../documentation/static/openapi/servicecatalog-v1.json \
  http://localhost:5010/swagger/v1/swagger.json

# Kill background processes
kill $USERMGMT_PID $SERVICECAT_PID

echo "OpenAPI specs exported successfully!"
```

Make it executable:

```bash
chmod +x scripts/export-swagger-specs.sh
```

---

### Phase 3: Embedding Swagger in Docusaurus (Week 2)

#### Step 3.1: Create API Documentation Pages

Create `documentation/docs/api/usermanagement.md`:

```markdown
---
sidebar_position: 1
title: UserManagement API
---

import SwaggerUI from '@theme/SwaggerUI';

# UserManagement API Reference

The UserManagement API handles all user-related operations including authentication, registration, and profile management.

## Base URL

- **Development**: `http://localhost:5000`
- **Staging**: `https://staging-api.booksy.ir`
- **Production**: `https://api.booksy.ir`

## Authentication

All protected endpoints require a JWT bearer token:

```http
Authorization: Bearer {your_access_token}
```

## Interactive API Documentation

<SwaggerUI specUrl="/openapi/usermanagement-v1.json" />

## Quick Links

- [Postman Collection](pathname:///Booksy_API_Collection.postman_collection.json)
- [Environment File](pathname:///Booksy_API.postman_environment.json)
- [View in Swagger UI](http://localhost:5000/swagger)

## Common Use Cases

### Authentication Flow

1. **Register**: `POST /api/v1/users`
2. **Login**: `POST /api/v1/auth/login`
3. **Refresh Token**: `POST /api/v1/auth/refresh`

### Phone Verification Flow

1. **Request OTP**: `POST /api/v1/phone-verification/request`
2. **Verify Code**: `POST /api/v1/phone-verification/verify`
3. **Resend Code**: `POST /api/v1/phone-verification/resend`
```

Create similar file for `documentation/docs/api/servicecatalog.md`

#### Step 3.2: Add Custom Swagger UI Component

Create `documentation/src/theme/SwaggerUI/index.js`:

```javascript
import React from 'react';
import SwaggerUI from 'swagger-ui-react';
import 'swagger-ui-react/swagger-ui.css';
import './swagger-custom.css';

export default function SwaggerUIWrapper({ specUrl }) {
  return (
    <div className="swagger-ui-wrapper">
      <SwaggerUI
        url={specUrl}
        deepLinking={true}
        displayRequestDuration={true}
        filter={true}
        tryItOutEnabled={true}
        persistAuthorization={true}
      />
    </div>
  );
}
```

Create `documentation/src/theme/SwaggerUI/swagger-custom.css`:

```css
.swagger-ui-wrapper {
  margin-top: 2rem;
  border: 1px solid var(--ifm-color-emphasis-300);
  border-radius: 8px;
  padding: 1rem;
}

.swagger-ui .topbar {
  display: none;
}

.swagger-ui .info {
  margin: 20px 0;
}

.swagger-ui .scheme-container {
  background: var(--ifm-background-color);
  box-shadow: 0 1px 2px 0 rgba(0, 0, 0, 0.1);
}
```

---

### Phase 4: Content Migration (Week 2)

#### Step 4.1: Migrate Existing Documentation

Move existing docs to Docusaurus structure:

```bash
# Copy and convert existing docs
cp docs/api-design-notes.md documentation/docs/architecture/api-design.md
cp docs/ZarinPal-Sandbox-Testing-Guide.md documentation/docs/guides/zarinpal-testing.md
cp POSTMAN_COLLECTION_README.md documentation/docs/guides/postman-collection.md
```

#### Step 4.2: Create Additional Documentation Pages

Create comprehensive guides:

1. **Getting Started**
   - `documentation/docs/getting-started/introduction.md`
   - `documentation/docs/getting-started/development-setup.md`
   - `documentation/docs/getting-started/running-locally.md`

2. **Architecture**
   - `documentation/docs/architecture/overview.md`
   - `documentation/docs/architecture/bounded-contexts.md`
   - `documentation/docs/architecture/domain-model.md`
   - `documentation/docs/architecture/infrastructure.md`

3. **Guides**
   - `documentation/docs/guides/authentication.md`
   - `documentation/docs/guides/booking-flow.md`
   - `documentation/docs/guides/payment-integration.md`
   - `documentation/docs/guides/error-handling.md`

4. **Tutorials**
   - `documentation/docs/tutorials/creating-provider.md`
   - `documentation/docs/tutorials/making-first-booking.md`
   - `documentation/docs/tutorials/implementing-payments.md`

---

### Phase 5: Deployment Setup (Week 3)

#### Step 5.1: Create Docker Configuration

Create `documentation/Dockerfile`:

```dockerfile
FROM node:18-alpine AS build

WORKDIR /app

# Copy package files
COPY package.json package-lock.json ./

# Install dependencies
RUN npm ci

# Copy source code
COPY . .

# Build documentation site
RUN npm run build

# Production stage
FROM nginx:alpine

# Copy built assets
COPY --from=build /app/build /usr/share/nginx/html

# Copy nginx configuration
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
```

Create `documentation/nginx.conf`:

```nginx
server {
    listen 80;
    server_name localhost;
    root /usr/share/nginx/html;
    index index.html;

    # Gzip compression
    gzip on;
    gzip_vary on;
    gzip_types text/plain text/css application/json application/javascript text/xml application/xml text/javascript;

    # Cache static assets
    location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2|ttf|eot)$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
    }

    # SPA routing
    location / {
        try_files $uri $uri/ /index.html;
    }

    # Security headers
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;
}
```

#### Step 5.2: Add to docker-compose.yml

Edit `/home/user/Booking/docker-compose.yml`, add:

```yaml
services:
  # ... existing services ...

  documentation:
    build:
      context: ./documentation
      dockerfile: Dockerfile
    container_name: booksy-docs
    ports:
      - "3000:80"
    networks:
      - booksy-network
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "wget", "--quiet", "--tries=1", "--spider", "http://localhost/"]
      interval: 30s
      timeout: 10s
      retries: 3
```

---

## 5. Configuration Samples

### 5.1 Production Swagger Configuration

For production, we need to secure Swagger UI:

**UserManagement API** - Update `Program.cs`:

```csharp
// Add Swagger (conditionally)
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwaggerConfiguration(app.Services.GetRequiredService<IApiVersionDescriptionProvider>());
}
else
{
    // Production: Require authentication for Swagger
    app.UseSwagger(c =>
    {
        c.RouteTemplate = "swagger/{documentName}/swagger.json";
    });

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "V1");
        options.RoutePrefix = "swagger";
    });

    // Protect Swagger with authentication
    app.MapGet("/swagger/{**path}", async context =>
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }

        await Task.CompletedTask;
    }).RequireAuthorization();
}
```

### 5.2 Environment-Specific Configuration

Create `documentation/.env.development`:

```env
USERMANAGEMENT_API_URL=http://localhost:5000
SERVICECATALOG_API_URL=http://localhost:5010
```

Create `documentation/.env.production`:

```env
USERMANAGEMENT_API_URL=https://api.booksy.ir
SERVICECATALOG_API_URL=https://catalog.booksy.ir
```

Update `docusaurus.config.js`:

```javascript
const apiUrls = {
  usermanagement: process.env.USERMANAGEMENT_API_URL || 'http://localhost:5000',
  servicecatalog: process.env.SERVICECATALOG_API_URL || 'http://localhost:5010',
};

// Use in plugin configuration
config: {
  usermanagement: {
    specPath: `${apiUrls.usermanagement}/swagger/v1/swagger.json`,
    // ...
  },
}
```

---

## 6. Security & Deployment

### 6.1 Security Best Practices

#### 6.1.1 Hide Internal Endpoints

Add `[ApiExplorerSettings(IgnoreApi = true)]` to internal controllers:

```csharp
[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]  // Hide from Swagger
[Route("internal/[controller]")]
public class InternalHealthController : ControllerBase
{
    // Internal endpoints not shown in Swagger
}
```

#### 6.1.2 Protect Swagger in Production

**Option 1: IP Whitelist**

```csharp
app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/swagger"),
    appBuilder =>
    {
        appBuilder.UseMiddleware<IpWhitelistMiddleware>(
            configuration.GetSection("Swagger:AllowedIPs").Get<string[]>());
    });
```

**Option 2: Basic Authentication**

```csharp
app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/swagger"),
    appBuilder =>
    {
        appBuilder.Use(async (context, next) =>
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

            if (authHeader == null || !IsValidBasicAuth(authHeader))
            {
                context.Response.StatusCode = 401;
                context.Response.Headers.Add("WWW-Authenticate", "Basic realm=\"Swagger\"");
                return;
            }

            await next();
        });
    });
```

**Option 3: Environment-Based Disable**

```csharp
// Only enable Swagger in Development and Staging
if (!app.Environment.IsProduction())
{
    app.UseSwaggerConfiguration(provider);
}
```

#### 6.1.3 CORS Configuration

Update `Program.cs` for production:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowDocumentation",
        policy =>
        {
            if (builder.Environment.IsProduction())
            {
                policy.WithOrigins("https://docs.booksy.ir")
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            }
            else
            {
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            }
        });
});
```

### 6.2 Deployment Strategies

#### 6.2.1 Static Site Deployment (Recommended)

**Netlify** (Simplest):

1. Connect GitHub repository
2. Build command: `cd documentation && npm run build`
3. Publish directory: `documentation/build`
4. Deploy

**Vercel**:

```json
// documentation/vercel.json
{
  "buildCommand": "npm run build",
  "outputDirectory": "build",
  "framework": "docusaurus",
  "rewrites": [
    { "source": "/(.*)", "destination": "/index.html" }
  ]
}
```

**GitHub Pages**:

```yaml
# .github/workflows/deploy-docs.yml
name: Deploy Documentation

on:
  push:
    branches: [master]
    paths:
      - 'documentation/**'

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup Node
        uses: actions/setup-node@v3
        with:
          node-version: 18

      - name: Install dependencies
        run: |
          cd documentation
          npm ci

      - name: Build
        run: |
          cd documentation
          npm run build

      - name: Deploy
        uses: peaceiris/actions-gh-pages@v3
        with:
          github_token: ${{ secrets.GITHUB_TOKEN }}
          publish_dir: ./documentation/build
```

#### 6.2.2 Self-Hosted Deployment

**Docker Compose** (Included in Phase 5.2 above)

**Kubernetes**:

```yaml
# k8s/docs-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: booksy-docs
spec:
  replicas: 2
  selector:
    matchLabels:
      app: booksy-docs
  template:
    metadata:
      labels:
        app: booksy-docs
    spec:
      containers:
      - name: docs
        image: booksy/docs:latest
        ports:
        - containerPort: 80
        resources:
          limits:
            memory: "512Mi"
            cpu: "500m"
---
apiVersion: v1
kind: Service
metadata:
  name: booksy-docs-service
spec:
  selector:
    app: booksy-docs
  ports:
  - port: 80
    targetPort: 80
  type: LoadBalancer
```

### 6.3 SSL/TLS Configuration

For production, always use HTTPS:

**Nginx Configuration**:

```nginx
server {
    listen 443 ssl http2;
    server_name docs.booksy.ir;

    ssl_certificate /etc/ssl/certs/booksy.crt;
    ssl_certificate_key /etc/ssl/private/booksy.key;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;

    root /usr/share/nginx/html;
    index index.html;

    # ... rest of configuration
}

# Redirect HTTP to HTTPS
server {
    listen 80;
    server_name docs.booksy.ir;
    return 301 https://$server_name$request_uri;
}
```

---

## 7. Auto-Update Strategy

### 7.1 Local Development

Developers run this script before committing API changes:

```bash
#!/bin/bash
# scripts/update-docs.sh

echo "Updating API documentation..."

# Export OpenAPI specs
./scripts/export-swagger-specs.sh

# Regenerate API docs
cd documentation
npm run docusaurus gen-api-docs all

# Build locally to verify
npm run build

echo "Documentation updated successfully!"
echo "Please commit the changes in documentation/docs/api/"
```

### 7.2 CI/CD Integration

**GitHub Actions Workflow**:

```yaml
# .github/workflows/update-api-docs.yml
name: Update API Documentation

on:
  push:
    branches: [master, develop]
    paths:
      - 'src/**/Controllers/**'
      - 'src/**/Models/**'
      - 'src/**/*.csproj'

jobs:
  update-docs:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'

      - name: Setup Node
        uses: actions/setup-node@v3
        with:
          node-version: 18

      - name: Build API projects
        run: |
          dotnet build src/UserManagement/Booksy.UserManagement.API
          dotnet build src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api

      - name: Export OpenAPI specs
        run: ./scripts/export-swagger-specs.sh

      - name: Generate API documentation
        run: |
          cd documentation
          npm ci
          npm run docusaurus gen-api-docs all

      - name: Commit updated docs
        run: |
          git config --local user.email "action@github.com"
          git config --local user.name "GitHub Action"
          git add documentation/docs/api/
          git add documentation/static/openapi/
          git diff --quiet && git diff --staged --quiet || \
            git commit -m "docs: auto-update API documentation [skip ci]"
          git push

      - name: Build documentation site
        run: |
          cd documentation
          npm run build

      - name: Deploy to Netlify
        uses: nwtgck/actions-netlify@v2.0
        with:
          publish-dir: './documentation/build'
          production-deploy: true
        env:
          NETLIFY_AUTH_TOKEN: ${{ secrets.NETLIFY_AUTH_TOKEN }}
          NETLIFY_SITE_ID: ${{ secrets.NETLIFY_SITE_ID }}
```

### 7.3 Pre-commit Hook

Create `.git/hooks/pre-commit`:

```bash
#!/bin/bash

# Check if any API files were changed
API_FILES=$(git diff --cached --name-only | grep -E "(Controllers|Models|SwaggerExtensions)" || true)

if [ -n "$API_FILES" ]; then
    echo "API files modified. Updating documentation..."

    # Update docs (but don't commit automatically)
    ./scripts/update-docs.sh

    echo "⚠️  API documentation has been updated."
    echo "Please review changes in documentation/docs/api/ before committing."
    echo "Run 'git add documentation/' to include updated docs."
fi

exit 0
```

### 7.4 Versioning Strategy

When releasing a new API version:

```bash
# Create version snapshot
cd documentation
npm run docusaurus docs:version 1.0

# This creates:
# - versioned_docs/version-1.0/
# - versioned_sidebars/version-1.0-sidebars.json
# - versions.json
```

Configure version dropdown in `docusaurus.config.js`:

```javascript
themeConfig: {
  navbar: {
    items: [
      {
        type: 'docsVersionDropdown',
        position: 'right',
        dropdownActiveClassDisabled: true,
      },
    ],
  },
},
```

---

## 8. Professional Documentation Workflow

### 8.1 Documentation Lifecycle

```
┌─────────────────────────────────────────────────────────┐
│                 Documentation Workflow                  │
└─────────────────────────────────────────────────────────┘

1. CODE DEVELOPMENT
   │
   ├─ Developer writes code
   ├─ Adds XML comments to controllers
   ├─ Updates request/response models
   │
   ▼
2. LOCAL TESTING
   │
   ├─ Run: npm run start (in documentation/)
   ├─ Verify API changes in Swagger UI
   ├─ Test embedded Swagger in Docusaurus
   │
   ▼
3. DOCUMENTATION UPDATE
   │
   ├─ Run: ./scripts/update-docs.sh
   ├─ Export OpenAPI specs
   ├─ Regenerate API documentation pages
   ├─ Update guides if needed
   │
   ▼
4. REVIEW & COMMIT
   │
   ├─ Review documentation changes
   ├─ Commit code + documentation together
   ├─ Create pull request
   │
   ▼
5. CI/CD PIPELINE
   │
   ├─ Build APIs
   ├─ Export OpenAPI specs
   ├─ Build documentation site
   ├─ Run tests
   │
   ▼
6. DEPLOYMENT
   │
   ├─ Deploy APIs to servers
   ├─ Deploy documentation to CDN
   ├─ Update production Swagger endpoints
   │
   ▼
7. MONITORING
   │
   ├─ Monitor documentation site uptime
   ├─ Check for broken links
   ├─ Review user feedback
   │
   └─ Continuous improvement
```

### 8.2 Content Organization

```
documentation/
├── docs/
│   ├── intro.md                          # Landing page
│   ├── getting-started/
│   │   ├── introduction.md               # What is Booksy?
│   │   ├── prerequisites.md              # System requirements
│   │   ├── installation.md               # Setup guide
│   │   └── quick-start.md                # First API call
│   │
│   ├── architecture/
│   │   ├── overview.md                   # System architecture
│   │   ├── bounded-contexts.md           # DDD contexts
│   │   ├── domain-model.md               # Domain entities
│   │   ├── infrastructure.md             # Infrastructure layer
│   │   └── security.md                   # Auth & authorization
│   │
│   ├── guides/
│   │   ├── authentication.md             # Auth patterns
│   │   ├── booking-flow.md               # Booking lifecycle
│   │   ├── payment-integration.md        # Payment gateway
│   │   ├── error-handling.md             # Error responses
│   │   ├── rate-limiting.md              # Rate limit policies
│   │   └── postman-collection.md         # Using Postman
│   │
│   ├── tutorials/
│   │   ├── creating-provider.md          # Step-by-step provider setup
│   │   ├── making-first-booking.md       # Step-by-step booking
│   │   └── implementing-payments.md      # Payment integration tutorial
│   │
│   ├── api/
│   │   ├── usermanagement.md             # UserManagement API
│   │   └── servicecatalog.md             # ServiceCatalog API
│   │
│   └── reference/
│       ├── glossary.md                   # Terms and definitions
│       ├── error-codes.md                # All error codes
│       └── changelog.md                  # Version history
│
├── static/
│   ├── openapi/                          # OpenAPI specs
│   ├── img/                              # Images
│   └── files/                            # Downloadable files
│       ├── Booksy_API_Collection.postman_collection.json
│       └── Booksy_API.postman_environment.json
│
└── src/
    ├── components/                       # React components
    ├── css/                              # Custom styles
    └── theme/                            # Theme overrides
```

### 8.3 Writing Guidelines

**For Technical Writers**:

1. **API Reference** (auto-generated from Swagger)
   - Maintained through XML comments in code
   - Regenerated on each build
   - Do not edit manually

2. **Guides** (manual documentation)
   - Use clear, concise language
   - Include code examples
   - Add diagrams for complex flows
   - Link to relevant API endpoints

3. **Tutorials** (step-by-step)
   - Start with objectives
   - Use numbered steps
   - Include screenshots
   - Provide complete code samples
   - Add "What's Next" section

**Markdown Standards**:

```markdown
---
sidebar_position: 1
title: Page Title
description: Brief description for SEO
keywords: [keyword1, keyword2]
---

# Page Title

Brief introduction paragraph.

## Section Heading

Content here.

### Subsection

More specific content.

:::tip
Helpful tips use this admonition
:::

:::warning
Important warnings use this admonition
:::

:::danger
Critical information uses this admonition
:::

```

### 8.4 Quality Assurance

**Documentation Quality Checklist**:

- [ ] All API endpoints documented
- [ ] Code examples tested and working
- [ ] Screenshots up to date
- [ ] Links verified (no 404s)
- [ ] Spelling and grammar checked
- [ ] Search functionality working
- [ ] Mobile responsiveness verified
- [ ] Load time < 3 seconds
- [ ] SEO metadata added
- [ ] Version dropdown working

**Automated Checks**:

```yaml
# .github/workflows/docs-quality.yml
name: Documentation Quality Checks

on:
  pull_request:
    paths:
      - 'documentation/**'

jobs:
  quality:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Check for broken links
        uses: lycheeverse/lychee-action@v1
        with:
          args: --verbose --no-progress './documentation/**/*.md'

      - name: Spell check
        uses: rojopolis/spellcheck-github-actions@v0
        with:
          source_files: documentation/docs/**/*.md

      - name: Markdown lint
        uses: articulate/actions-markdownlint@v1
        with:
          config: documentation/.markdownlint.json
          files: 'documentation/docs/**/*.md'
```

---

## 9. Maintenance & Best Practices

### 9.1 Regular Maintenance Tasks

**Weekly**:
- [ ] Review user feedback
- [ ] Check for broken links
- [ ] Update changelog

**Monthly**:
- [ ] Review analytics (popular pages)
- [ ] Update outdated screenshots
- [ ] Check dependencies for updates
- [ ] Review and improve search results

**Quarterly**:
- [ ] Comprehensive content audit
- [ ] User survey for feedback
- [ ] Performance optimization
- [ ] SEO review

### 9.2 Performance Optimization

**Build Optimization**:

```javascript
// docusaurus.config.js
module.exports = {
  // ...

  // Enable faster builds
  future: {
    experimental_faster: true,
  },

  webpack: {
    jsLoader: (isServer) => ({
      loader: require.resolve('swc-loader'),
      options: {
        jsc: {
          parser: {
            syntax: 'typescript',
            tsx: true,
          },
          transform: {
            react: {
              runtime: 'automatic',
            },
          },
        },
      },
    }),
  },
};
```

**Lazy Loading**:

```javascript
// Lazy load Swagger UI component
import React, { lazy, Suspense } from 'react';

const SwaggerUI = lazy(() => import('./SwaggerUI'));

export default function ApiDocs({ specUrl }) {
  return (
    <Suspense fallback={<div>Loading API documentation...</div>}>
      <SwaggerUI specUrl={specUrl} />
    </Suspense>
  );
}
```

### 9.3 Analytics Integration

**Google Analytics**:

```javascript
// docusaurus.config.js
module.exports = {
  themeConfig: {
    googleAnalytics: {
      trackingID: 'UA-XXXXXXXXX-X',
      anonymizeIP: true,
    },
  },
};
```

**Custom Analytics Events**:

```javascript
// Track API documentation views
import ExecutionEnvironment from '@docusaurus/ExecutionEnvironment';

if (ExecutionEnvironment.canUseDOM) {
  window.gtag('event', 'api_docs_view', {
    api_name: 'UserManagement',
    version: 'v1',
  });
}
```

### 9.4 Feedback System

Create `documentation/src/components/Feedback.js`:

```javascript
import React, { useState } from 'react';

export default function Feedback() {
  const [helpful, setHelpful] = useState(null);

  const handleFeedback = (isHelpful) => {
    setHelpful(isHelpful);

    // Send to analytics
    window.gtag('event', 'documentation_feedback', {
      helpful: isHelpful,
      page: window.location.pathname,
    });
  };

  return (
    <div className="feedback-widget">
      <p>Was this page helpful?</p>
      {helpful === null ? (
        <div>
          <button onClick={() => handleFeedback(true)}>👍 Yes</button>
          <button onClick={() => handleFeedback(false)}>👎 No</button>
        </div>
      ) : (
        <p>Thank you for your feedback!</p>
      )}
    </div>
  );
}
```

Add to docs layout:

```javascript
// documentation/src/theme/DocItem/Footer/index.js
import React from 'react';
import Footer from '@theme-original/DocItem/Footer';
import Feedback from '@site/src/components/Feedback';

export default function FooterWrapper(props) {
  return (
    <>
      <Feedback />
      <Footer {...props} />
    </>
  );
}
```

---

## Implementation Timeline

### Week 1: Foundation
- [x] Review existing Swagger setup
- [ ] Initialize Docusaurus project
- [ ] Configure base structure
- [ ] Update Swagger routes to `/swagger`
- [ ] Create OpenAPI export endpoints

### Week 2: Integration
- [ ] Embed Swagger UI in Docusaurus
- [ ] Create API documentation pages
- [ ] Migrate existing docs
- [ ] Set up auto-export scripts
- [ ] Test local integration

### Week 3: Deployment
- [ ] Create Docker configuration
- [ ] Set up CI/CD pipeline
- [ ] Configure production security
- [ ] Deploy to staging
- [ ] Performance testing

### Week 4: Polish & Launch
- [ ] Add analytics
- [ ] Implement feedback system
- [ ] Final content review
- [ ] SEO optimization
- [ ] Deploy to production

---

## Success Metrics

### Quantitative
- Documentation site load time < 2 seconds
- Search results < 500ms
- 90%+ uptime
- Zero broken links
- Mobile performance score > 90

### Qualitative
- Easy to find API endpoints
- Clear navigation structure
- Positive developer feedback
- Reduced support questions
- High engagement metrics

---

## Conclusion

This integration plan provides a comprehensive roadmap for creating a professional documentation portal that seamlessly integrates Swagger/OpenAPI documentation with business and architectural documentation. By following this plan, you will:

1. ✅ Create a unified documentation experience
2. ✅ Maintain auto-updated API documentation
3. ✅ Secure Swagger UI for production use
4. ✅ Provide multiple deployment options
5. ✅ Establish a sustainable documentation workflow

The recommended approach using Docusaurus offers the best balance of features, developer experience, and long-term maintainability for the Booksy platform.

---

## Appendix

### A. Useful Commands

```bash
# Docusaurus
npm run start                    # Start development server
npm run build                    # Build for production
npm run serve                    # Preview production build
npm run clear                    # Clear cache
npm run docusaurus gen-api-docs  # Generate API docs

# Export Swagger specs
./scripts/export-swagger-specs.sh

# Update all documentation
./scripts/update-docs.sh

# Docker
docker-compose up documentation  # Run docs container
docker-compose build documentation  # Rebuild docs image
```

### B. Resources

- [Docusaurus Documentation](https://docusaurus.io/)
- [OpenAPI Specification](https://swagger.io/specification/)
- [Swashbuckle Documentation](https://github.com/domaindrivendev/Swashbuckle.AspNetCore)
- [DocFX Documentation](https://dotnet.github.io/docfx/)

### C. Contact

For questions or feedback about this integration plan:
- GitHub Issues: https://github.com/kazemim99/Booking/issues
- Email: support@booksy.com

---

**Document Version**: 1.0
**Last Updated**: 2025-11-09
**Author**: Claude (Senior Software Architect Assistant)
**Status**: Ready for Implementation
