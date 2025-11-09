# Booksy Documentation

This directory contains the Docusaurus-based documentation site for the Booksy platform.

## Quick Start

### Development Mode

```bash
cd documentation
npm install
npm start
```

The documentation site will open at http://localhost:3000

### Build for Production

```bash
npm run build
```

The static files will be generated in the `build/` directory.

### Preview Production Build

```bash
npm run serve
```

## Updating API Documentation

When you make changes to the API controllers, update the documentation:

```bash
# From project root
./scripts/export-swagger-specs.sh
```

This will:
1. Export OpenAPI specifications from running APIs
2. Save them to `static/openapi/`
3. Allow Docusaurus to display them in the API documentation pages

## Project Structure

```
documentation/
├── docs/                          # Documentation markdown files
│   ├── intro.md                   # Landing page
│   ├── getting-started/           # Setup guides
│   ├── architecture/              # Architecture documentation
│   ├── guides/                    # How-to guides
│   └── api/                       # API reference (placeholder)
├── static/                        # Static assets
│   ├── openapi/                   # OpenAPI specifications
│   │   ├── usermanagement-v1.json
│   │   └── servicecatalog-v1.json
│   ├── Booksy_API_Collection.postman_collection.json
│   └── Booksy_API.postman_environment.json
├── src/                           # React components and custom code
├── docusaurus.config.ts           # Docusaurus configuration
├── sidebars.ts                    # Sidebar configuration
└── package.json                   # Dependencies
```

## Available Commands

- `npm start` - Start development server
- `npm run build` - Build for production
- `npm run serve` - Preview production build
- `npm run clear` - Clear Docusaurus cache
- `npm run swizzle` - Eject Docusaurus components for customization

## Adding New Documentation

### 1. Create a Markdown File

```bash
# Create new guide
touch docs/guides/my-new-guide.md
```

### 2. Add Frontmatter

```markdown
---
sidebar_position: 2
title: My New Guide
---

# My New Guide

Content here...
```

### 3. The Page Will Automatically Appear

Docusaurus will automatically add it to the sidebar based on the folder structure and `sidebar_position`.

## Customization

### Theme

Edit `src/css/custom.css` to customize colors and styles.

### Navbar

Edit `docusaurus.config.ts` → `themeConfig.navbar`

### Footer

Edit `docusaurus.config.ts` → `themeConfig.footer`

## Deployment

### Option 1: Netlify (Recommended)

1. Connect GitHub repository
2. Build command: `cd documentation && npm run build`
3. Publish directory: `documentation/build`
4. Deploy

### Option 2: GitHub Pages

```bash
GIT_USER=<your-username> npm run deploy
```

### Option 3: Docker

```bash
# From project root
docker-compose up documentation
```

## Troubleshooting

### Port 3000 Already in Use

```bash
npm start -- --port 3001
```

### Build Fails

```bash
# Clear cache and rebuild
npm run clear
npm run build
```

### OpenAPI Specs Not Showing

1. Ensure APIs are running
2. Run `./scripts/export-swagger-specs.sh` from project root
3. Verify JSON files exist in `static/openapi/`
4. Rebuild docs

## Resources

- [Docusaurus Documentation](https://docusaurus.io/)
- [Markdown Features](https://docusaurus.io/docs/markdown-features)
- [Deployment Guide](https://docusaurus.io/docs/deployment)

## Support

For documentation-related issues:
- Check the [Swagger Integration Plan](../SWAGGER_INTEGRATION_PLAN.md)
- Review [GitHub Issues](https://github.com/kazemim99/Booking/issues)
