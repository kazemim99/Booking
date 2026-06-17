# Booksy Admin Panel - Quick Start Guide

## Setup and Run

### Development Mode

```bash
cd booksy-admin
npm install
npm run dev
```

Open [http://localhost:5173](http://localhost:5173)

### Production Build

```bash
npm run build
npm run preview
```

### Docker Deployment

```bash
docker-compose up -d
```

Admin panel available at [http://localhost:3000](http://localhost:3000)

## Default Login Credentials

Configure these in your backend API:
- Email: `admin@booksy.com`
- Password: (set in backend)

## Key Features

### 1. Dashboard
- Real-time platform statistics
- User growth charts
- Booking trends visualization
- Quick action buttons

### 2. User Management
- Create, edit, delete users
- Role assignment (Admin/Provider/Client)
- Search and filter capabilities
- Toggle active/inactive status

### 3. Provider Management
- Approval workflow (Pending → Approved/Rejected)
- Provider details view
- Suspend/Reactivate providers
- Provider statistics

### 4. Service Catalog (Placeholder)
- Ready for implementation
- Service and category management

### 5. Analytics (Placeholder)
- Advanced analytics dashboard
- Custom date ranges
- Export capabilities

### 6. Payments & Orders (Placeholder)
- Transaction monitoring
- Booking orders tracking

### 7. System Logs (Placeholder)
- Application logs viewer
- Filtering by level and source

## API Integration

The admin panel connects to your Booksy backend at:
- Development: `http://localhost:5000/api/v1`
- Production: `http://napstar.ir/api/v1`

Update in [.env.development](.env.development) and [.env.production](.env.production)

## Project Structure

```
src/
├── api/           # API service layer
├── layouts/       # AdminLayout with sidebar
├── views/         # Page components
├── stores/        # Pinia state management
├── router/        # Vue Router with guards
└── types/         # TypeScript definitions
```

## Next Steps

1. Connect to your backend API
2. Test login functionality
3. Implement remaining modules (Services, Logs, etc.)
4. Customize branding and styling
5. Add more charts to analytics
6. Deploy to production

## Troubleshooting

### CORS Issues
Ensure your backend API allows requests from the admin panel origin.

### Authentication Errors
Check that JWT tokens are correctly configured in both frontend and backend.

### Build Errors
Run `npm install` to ensure all dependencies are installed.

## Technologies Used

- Vue 3 + TypeScript
- Ant Design Vue
- Pinia (State Management)
- Vue Router
- Axios
- ECharts (Charts)
- Vite (Build Tool)
