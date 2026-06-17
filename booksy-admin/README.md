# Booksy Admin Panel

Professional admin panel for managing the Booksy booking platform.

## Features

- **User Management**: Complete CRUD operations for users with role-based access control
- **Provider Management**: Approval workflow for providers with status management (Pending, Approved, Rejected, Suspended)
- **Service Catalog**: Manage services and categories
- **Analytics Dashboard**: Platform statistics and insights with interactive charts
- **Payment Management**: Transaction monitoring
- **Order Management**: Booking orders tracking
- **System Logs**: Application logs and monitoring
- **Modern UI**: Built with Ant Design Vue for a professional look and feel

## Tech Stack

- **Vue 3** with Composition API
- **TypeScript** for type safety
- **Vite** for fast development and building
- **Ant Design Vue** for UI components
- **Pinia** for state management
- **Vue Router** for navigation with authentication guards
- **Axios** for API communication
- **ECharts** for data visualization
- **Day.js** for date handling

## Getting Started

### Local Development

1. Install dependencies:
```bash
npm install
```

2. Start development server:
```bash
npm run dev
```

The admin panel will be available at `http://localhost:5173`

3. Build for production:
```bash
npm run build
```

## Docker Deployment

```bash
# Build and run
docker-compose up -d

# View logs
docker-compose logs -f booksy-admin
```

The admin panel will be available at `http://localhost:3000`

## Project Structure

```
booksy-admin/
├── src/
│   ├── api/              # API service layer
│   ├── assets/           # Static assets
│   ├── components/       # Reusable components
│   ├── layouts/          # Layout components
│   ├── router/           # Vue Router configuration
│   ├── stores/           # Pinia stores
│   ├── types/            # TypeScript types
│   ├── utils/            # Utility functions
│   ├── views/            # Page components
│   ├── App.vue
│   └── main.ts
├── Dockerfile
├── docker-compose.yml
└── nginx.conf
```

## Environment Variables

Create `.env.development` and `.env.production` files:

```env
# .env.development
VITE_API_BASE_URL=http://localhost:5000/api/v1

# .env.production
VITE_API_BASE_URL=http://napstar.ir/api/v1
```

## License

Private - Booksy Platform
