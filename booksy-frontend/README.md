# Booksy Frontend

Modern, responsive Vue 3 + TypeScript frontend for the Booksy service booking platform with full RTL (Right-to-Left) support for Persian language.

---

## Recent Updates (2025-11-11) 🎉

### Critical Bug Fixes
✅ **Gallery Image Submission** - Fixed gallery images not submitting during provider registration (Step 7)
✅ **UI Fixes** - Resolved distorted UI in CompletionStep and OptionalFeedbackStep components
✅ **Registration Progress** - Fixed "not found" error after completing registration

See [CHANGELOG.md](../CHANGELOG.md) for detailed information.

---

## Tech Stack

- **Framework**: Vue 3 (Composition API)
- **Language**: TypeScript
- **State Management**: Pinia
- **HTTP Client**: Axios
- **Routing**: Vue Router
- **Build Tool**: Vite
- **Styling**: Tailwind CSS + Scoped CSS
- **Maps**: Neshan Maps (Iranian map service)
- **Date/Time**: Jalaali calendar support
- **Testing**: Vitest (unit) + Cypress (e2e)

---

## Project Structure

```
booksy-frontend/
├── src/
│   ├── core/                    # Core infrastructure
│   │   ├── api/                 # API clients & interceptors
│   │   ├── router/              # Routes & navigation guards
│   │   ├── stores/              # Global Pinia stores
│   │   ├── services/            # Shared services (toast, etc.)
│   │   └── types/               # Global TypeScript types
│   ├── modules/                 # Feature modules
│   │   ├── auth/                # Authentication & phone verification
│   │   │   ├── views/           # Login, Verification
│   │   │   ├── composables/     # usePhoneVerification
│   │   │   └── stores/          # authStore
│   │   ├── provider/            # Provider features
│   │   │   ├── views/           # Registration, Dashboard, Gallery
│   │   │   ├── components/      # Registration steps, gallery components
│   │   │   ├── composables/     # useProviderRegistration, useLocations
│   │   │   ├── stores/          # providerStore, galleryStore
│   │   │   ├── services/        # API services
│   │   │   └── types/           # TypeScript types
│   │   ├── customer/            # Customer features (future)
│   │   └── booking/             # Booking features (future)
│   └── shared/                  # Shared UI components
│       └── components/          # Buttons, Inputs, Cards, etc.
```

---

## Key Features

### 🌐 Provider Registration Flow (9 Steps)
1. **Business Info** - Name, owner details
2. **Category Selection** - Business type
3. **Location** - Address with Neshan Maps integration
4. **Services** - Service catalog configuration
5. **Staff** - Team member management
6. **Working Hours** - Business hours and breaks
7. **Gallery** - Portfolio image uploads
8. **Optional Feedback** - User experience survey
9. **Completion** - Success screen

### 📱 Authentication
- Phone verification with OTP
- JWT token management
- Automatic token refresh
- Route guards for protected pages

### 🎨 UI/UX
- Full RTL support for Persian
- Responsive design (mobile-first)
- Loading states and skeletons
- Toast notifications
- Form validation with error messages

### 🗺️ Map Integration
- Neshan Maps for location selection
- Bidirectional sync (map ↔ form)
- Reverse geocoding
- Province/city dropdowns

### 🖼️ Gallery Management
- Drag-and-drop image upload
- Progress tracking
- Image preview and reordering
- Metadata editing (captions, alt text)

---

## Development Setup

### Prerequisites
- Node.js 18+ and npm

### Install Dependencies
```sh
npm install
```

### Development Server
```sh
npm run dev
```
Open [http://localhost:5173](http://localhost:5173) to view in browser.

### Build for Production
```sh
npm run build
```

### Type Checking
```sh
npm run type-check
```

### Linting
```sh
npm run lint
```

### Fix Lint Errors
```sh
npm run lint:fix
```

---

## Testing

### Unit Tests (Vitest)
```sh
npm run test:unit
```

### E2E Tests (Cypress)
```sh
# Development mode
npm run test:e2e:dev

# Production build
npm run build
npm run test:e2e
```

---

## Environment Variables

Create a `.env.local` file:

```env
VITE_API_BASE_URL=http://localhost:5108
VITE_NESHAN_API_KEY=your_neshan_api_key
```

---

## Code Style & Patterns

### Composition API
We use Vue 3 Composition API with `<script setup>`:

```vue
<script setup lang="ts">
import { ref, computed } from 'vue'

const count = ref(0)
const double = computed(() => count.value * 2)

function increment() {
  count.value++
}
</script>
```

### Composables
Reusable logic extracted into composables:

```typescript
// composables/useProviderRegistration.ts
export function useProviderRegistration() {
  const state = ref({ currentStep: 1, ... })

  const nextStep = () => { ... }
  const previousStep = () => { ... }

  return { state, nextStep, previousStep }
}
```

### Pinia Stores
State management with Pinia:

```typescript
// stores/authStore.ts
export const useAuthStore = defineStore('auth', () => {
  const user = ref<User | null>(null)
  const isAuthenticated = computed(() => !!user.value)

  const login = async (credentials) => { ... }

  return { user, isAuthenticated, login }
})
```

### TypeScript
Full type safety with interfaces and types:

```typescript
interface Provider {
  id: string
  businessName: string
  category: string
  // ...
}
```

---

## Architecture Patterns

### API Services
Centralized API calls with type-safe DTOs:

```typescript
// services/provider-registration.service.ts
class ProviderRegistrationService {
  async saveStep3Location(request: CreateProviderDraftRequest): Promise<CreateProviderDraftResponse> {
    const response = await serviceCategoryClient.post('v1/registration/step-3/location', request)
    return response.data!
  }
}
```

### Axios Interceptors
Request/response transformation and error handling:

```typescript
// api/interceptors/auth.interceptor.ts
httpClient.interceptors.request.use(config => {
  const token = authStore.accessToken
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})
```

### Route Guards
Authentication and authorization checks:

```typescript
// router/guards/auth.guard.ts
router.beforeEach((to, from, next) => {
  const requiresAuth = !to.meta.isPublic
  const isAuthenticated = authStore.isAuthenticated

  if (requiresAuth && !isAuthenticated) {
    next('/login')
  } else {
    next()
  }
})
```

---

## Recommended IDE Setup

### VS Code Extensions
- [Vue (Official)](https://marketplace.visualstudio.com/items?itemName=Vue.volar)
- [TypeScript Vue Plugin (Volar)](https://marketplace.visualstudio.com/items?itemName=Vue.vscode-typescript-vue-plugin)
- [ESLint](https://marketplace.visualstudio.com/items?itemName=dbaeumer.vscode-eslint)
- [Prettier](https://marketplace.visualstudio.com/items?itemName=esbenp.prettier-vscode)

### Browser Extensions
- **Chrome/Edge**: [Vue.js devtools](https://chromewebstore.google.com/detail/vuejs-devtools/nhdogjmejiglipccpnnnanhbledajbpd)
- **Firefox**: [Vue.js devtools](https://addons.mozilla.org/en-US/firefox/addon/vue-js-devtools/)

---

## Related Documentation

- [Main README](../README.md) - Project overview
- [CHANGELOG](../CHANGELOG.md) - Detailed changelog with all fixes
- [Technical Documentation](../TECHNICAL_DOCUMENTATION.md) - Comprehensive technical guide
- [Provider Registration Spec](../openspec/specs/provider-registration/spec.md)
- [Gallery Implementation Summary](../openspec/changes/add-provider-image-gallery/IMPLEMENTATION_SUMMARY.md)

---

## Contributing

1. Follow the existing code style and patterns
2. Use TypeScript for type safety
3. Write composables for reusable logic
4. Keep components small and focused
5. Use Pinia for global state
6. Write tests for critical functionality
7. Ensure RTL compatibility for all UI components

---

## License

Proprietary - All rights reserved
