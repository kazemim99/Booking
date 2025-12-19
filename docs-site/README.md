# Booksy Documentation Site

This directory contains the Docusaurus-powered documentation site for the Booksy project, consolidating all 71+ markdown documentation files into a searchable, organized website.

## Quick Start

### Installation

```bash
npm install
```

### Local Development

```bash
npm start
```

This starts a local development server at `http://localhost:3000` with live reload.

### Build

```bash
npm run build
```

Generates static content into the `build` directory.

### Serve Production Build Locally

```bash
npm run serve
```

## Documentation Structure

```
docs/
├── getting-started/          # Setup and quick start guides
│   ├── introduction.md       # Main README (project overview)
│   ├── quick-start.md        # Quick deployment guide
│   ├── advanced-setup.md     # Advanced configuration
│   └── setup-complete.md     # Setup completion guide
│
├── architecture/             # Architecture documentation
│   ├── overview.md           # Technical documentation
│   ├── cqrs-components.md    # CQRS pattern implementation
│   └── business-requirements.md  # Business proposal & SRD
│
├── features/                 # Feature-specific documentation
│   ├── authentication/       # Authentication system docs
│   ├── booking/             # Booking management docs
│   ├── provider/            # Provider management docs
│   ├── service-catalog/     # Service catalog docs
│   └── user-management/     # User management docs
│
├── deployment/              # Deployment guides
│   ├── overview.md          # Deployment overview
│   ├── docker-compose.md    # Docker Compose setup
│   └── database-setup.md    # Database configuration
│
├── testing/                 # Testing documentation
│   ├── integration-testing.md
│   ├── reqnroll-quickstart.md
│   ├── test-coverage.md
│   └── quick-guide.md
│
├── implementation/          # Implementation tracking
│   ├── status.md
│   └── summary.md
│
└── changelog/              # Change logs
    └── changelog.md
```

## Deployment Options

### Option 1: GitHub Pages

1. Update `docusaurus.config.ts`:
   ```typescript
   organizationName: 'your-github-username',
   projectName: 'Booking',
   url: 'https://your-username.github.io',
   baseUrl: '/Booking/',
   ```

2. Deploy:
   ```bash
   npm run deploy
   ```

### Option 2: Netlify

1. Connect repository to Netlify
2. Build command: `npm run build`
3. Publish directory: `build`

### Option 3: Vercel

1. Connect repository to Vercel
2. Build command: `npm run build`
3. Output directory: `build`

### Option 4: Self-Hosted (Nginx)

```nginx
server {
    listen 80;
    server_name docs.booksy.com;
    root /var/www/booksy-docs/build;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }
}
```

## Features

✅ **Organized Navigation** - 4 main sections with categorized sidebars
✅ **Search Functionality** - Built-in search across all documentation
✅ **Syntax Highlighting** - C#, JavaScript, SQL, Docker, YAML, PowerShell support
✅ **Dark Mode** - Automatic theme switching
✅ **Responsive Design** - Mobile-friendly layout
✅ **71+ Documents Migrated** - All existing markdown files organized and accessible

## Adding New Documentation

1. Create markdown file in appropriate `docs/` subdirectory
2. Add frontmatter:
   ```markdown
   ---
   sidebar_position: 1
   title: Your Page Title
   ---
   ```
3. Update `sidebars.ts` if needed
4. Documentation auto-rebuilds in dev mode

## Customization

- **Theme**: Edit `src/css/custom.css`
- **Config**: Edit `docusaurus.config.ts`
- **Sidebars**: Edit `sidebars.ts`

## Troubleshooting

### MDX Compilation Errors

- Replace `<` with "less than" in text (MDX interprets as HTML tag)
- Use self-closing tags: `<br/>` not `<br>`
- Escape special characters in tables

### Broken Links

Run build to see warnings:
```bash
npm run build
```

Update paths to match new structure (e.g., `DEPLOYMENT.md` → `deployment/overview`)
