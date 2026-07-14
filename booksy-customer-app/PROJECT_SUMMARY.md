# Booksy Customer App - Project Summary

## What Has Been Built

A complete **Flutter mobile application** for Booksy platform customers with:

✅ **Clean Architecture** implementation
✅ **OTP-based Authentication** flow (pinput OTP input, SMS autofill, resend timer)
✅ **Secure token storage** with auto-refresh
✅ **BLoC state management**
✅ **Retrofit API integration**
✅ **Dependency injection** with Injectable
✅ **Error handling** with Either pattern
✅ **Responsive UI** with ScreenUtil (layout only — text follows OS font scale)
✅ **Production-ready structure**

## Architecture Updates (July 2026 — `customer-app-ux-redesign`)

The UX redesign (OpenSpec change `customer-app-ux-redesign`) reshaped the presentation layer:

- **Routing**: `go_router` with `StatefulShellRoute.indexedStack` (`lib/config/routes/app_router.dart`). Four tabs with independent back stacks; deep-linkable routes for `/providers/:id`, `/providers/:id/book`, `/appointments/:id`; auth-gated routes redirect to `/login?redirect=…` with return-to-intent. `AuthNotifier` latches the AuthBloc stream for redirects (transient states never bounce navigation).
- **Theme**: single Material 3 `ThemeData` built from design tokens (`lib/config/theme/` — `app_colors.dart`, `app_text_styles.dart`, `app_tokens.dart`, `app_theme.dart`). WCAG-AA-verified palette (semantic `*Text`/`*Tint` variants). No inline hex or ad-hoc `TextStyle`s in feature code.
- **Component library**: `lib/core/widgets/` — `AppButton`, `AppTextField`, `OtpInput`, `AppCard`, `StatusBadge`, `SkeletonLoader`, `EmptyState`, `ErrorState`, `OfflineBanner`, `AppBottomSheet`/`ConfirmSheet`, `AppSnackbar`, and `StateSwitcher` (the mandatory skeleton/content/empty/error mapping for every data-backed screen).
- **Features implemented**: home (independent sections with per-section retry), explore (debounced provider search), provider detail, stepped booking flow (service → staff → Jalali slot picker → confirm), appointments (cancel with optimistic rollback, reschedule via shared `SlotPicker`), profile (edit + logout).
- **Strings**: all user-facing Persian text lives in `core/constants/app_strings.dart`.
- **Known constraint**: `build_runner` codegen is broken (retrofit_generator/SDK incompatibility), so newer services use manual JSON parsing and are registered manually in `core/di/injection.dart`.
- **Docs**: audit outcome and deferred findings in `CUSTOMER_APP_UX_FLOW.md`.

## Project Structure

```
booksy-customer-app/
├── lib/
│   ├── core/                          # Core infrastructure
│   │   ├── api/
│   │   │   ├── client/
│   │   │   │   └── dio_client.dart   # Dio client configuration
│   │   │   ├── config/
│   │   │   │   └── api_constants.dart # API endpoints
│   │   │   ├── interceptors/
│   │   │   │   ├── auth_interceptor.dart  # Auto token handling
│   │   │   │   └── error_interceptor.dart # Error handling
│   │   │   └── models/
│   │   │       ├── api_response.dart     # Generic response wrapper
│   │   │       ├── booking_models.dart   # Booking DTOs
│   │   │       └── category_models.dart  # Category DTOs
│   │   ├── di/
│   │   │   └── injection.dart        # Dependency injection setup
│   │   ├── errors/
│   │   │   └── failures.dart         # Failure types
│   │   └── storage/
│   │       └── secure_storage_service.dart # Encrypted storage
│   │
│   ├── features/
│   │   └── auth/                      # Authentication feature
│   │       ├── data/
│   │       │   ├── datasources/
│   │       │   │   └── auth_api_service.dart  # Retrofit API
│   │       │   ├── models/
│   │       │   │   └── auth_models.dart       # DTOs
│   │       │   └── repositories/
│   │       │       └── auth_repository_impl.dart
│   │       ├── domain/
│   │       │   ├── entities/
│   │       │   │   └── user.dart              # Business entities
│   │       │   ├── repositories/
│   │       │   │   └── auth_repository.dart   # Interface
│   │       │   └── usecases/
│   │       │       ├── send_verification_code_usecase.dart
│   │       │       └── complete_authentication_usecase.dart
│   │       └── presentation/
│   │           ├── bloc/
│   │           │   ├── auth_bloc.dart
│   │           │   ├── auth_event.dart
│   │           │   └── auth_state.dart
│   │           └── pages/
│   │               ├── splash_page.dart
│   │               ├── login_page.dart
│   │               └── otp_verification_page.dart
│   │
│   │   └── home/                      # Home feature
│   │       └── presentation/
│   │           └── pages/
│   │               └── home_page.dart
│   │
│   └── main.dart                      # App entry point
│
├── pubspec.yaml                       # Dependencies
├── README.md                          # Full documentation
├── SETUP_GUIDE.md                     # Setup instructions
└── PROJECT_SUMMARY.md                 # This file
```

## Key Features Implemented

### 1. Authentication System

**OTP-based Authentication Flow:**

1. **Phone Number Entry**
   - [login_page.dart](lib/features/auth/presentation/pages/login_page.dart)
   - Validates Iranian phone numbers (09xxxxxxxxx)
   - Sends OTP via backend

2. **OTP Verification**
   - [otp_verification_page.dart](lib/features/auth/presentation/pages/otp_verification_page.dart)
   - 6-digit code verification
   - Auto-login/register on success
   - Resend OTP functionality

3. **Session Management**
   - JWT tokens stored securely
   - Automatic token refresh on 401 errors
   - Secure logout with session cleanup

### 2. API Integration

**Configured Endpoints:**

- `POST /v1/Auth/send-verification-code` - Send OTP
- `POST /v1/Auth/customer/complete-authentication` - Verify & Login
- `POST /v1/Auth/refresh` - Refresh token
- `POST /v1/Auth/logout` - Logout
- `GET /v1/Bookings/my-bookings` - Get bookings
- `POST /v1/Bookings` - Create booking
- `GET /v1/Categories` - Get categories
- `POST /v1/Providers/search` - Search providers

**API Configuration:**
- Base URL: `http://napstar.ir/api`
- Auto-authentication headers via interceptor
- Persian language support (`fa-IR`)
- Comprehensive error handling

### 3. State Management (BLoC)

**Auth BLoC:**
- Events: SendVerificationCode, VerifyCode, ResendOtp, Logout
- States: Initial, Loading, OtpSent, Authenticated, Error
- Automatic auth status check on app launch

### 4. Secure Storage

**Encrypted Storage:**
- Access tokens
- Refresh tokens
- User ID & Customer ID
- Phone number

**Features:**
- Uses Flutter Secure Storage
- Automatic cleanup on logout
- Session persistence across app restarts

### 5. Clean Architecture

**Three-Layer Architecture:**

1. **Presentation Layer**
   - BLoC for state management
   - Flutter widgets
   - UI/UX implementation

2. **Domain Layer**
   - Business entities (User, Customer, AuthSession)
   - Repository interfaces
   - Use cases (business logic)
   - Pure Dart (no framework dependencies)

3. **Data Layer**
   - API services (Retrofit)
   - DTOs (Data Transfer Objects)
   - Repository implementations
   - External data sources

## Technology Stack

### Core Dependencies

```yaml
# State Management
flutter_bloc: ^8.1.3          # BLoC pattern
equatable: ^2.0.5             # Value equality

# API & Networking
dio: ^5.4.0                   # HTTP client
retrofit: ^4.0.3              # Type-safe REST client
json_annotation: ^4.8.1       # JSON serialization
pretty_dio_logger: ^1.3.1     # API logging

# Local Storage
shared_preferences: ^2.2.2    # Key-value storage
flutter_secure_storage: ^9.0.0 # Encrypted storage

# Navigation
go_router: ^13.0.0            # Declarative routing

# UI
flutter_screenutil: ^5.9.0    # Responsive design
cached_network_image: ^3.3.1  # Image caching

# Dependency Injection
get_it: ^7.6.7                # Service locator
injectable: ^2.3.2            # DI code generation

# Functional Programming
dartz: ^0.10.1                # Either type for error handling
```

### Dev Dependencies

```yaml
build_runner: ^2.4.8          # Code generation
retrofit_generator: ^8.0.6    # Retrofit code gen
json_serializable: ^6.7.1     # JSON code gen
injectable_generator: ^2.4.1  # DI code gen
```

## API Models Created

### Authentication

- `SendVerificationCodeRequest` / `Response`
- `CompleteCustomerAuthRequest` / `Response`
- `ResendOtpRequest` / `Response`
- `RefreshTokenRequest` / `Response`
- `UserDto` - User data transfer object
- `CustomerDto` - Customer data transfer object

### Bookings

- `BookingDto` - Full booking details
- `CustomerBookingDto` - Simplified for customer view
- `CreateBookingRequest`
- `CancelBookingRequest`
- `RescheduleBookingRequest`
- `PaginatedBookingsResponse`
- `ServiceDto`, `ProviderSummaryDto`, `StaffDto`

### Categories

- `CategoryDto` - Service category
- `PopularCategoryDto` - Popular categories with counts

## Security Features

1. **Token Management**
   - JWT tokens in encrypted storage
   - Auto token refresh (401 interceptor)
   - Secure logout

2. **Input Validation**
   - Phone number validation
   - OTP code validation
   - Email format validation

3. **Error Handling**
   - User-friendly Persian error messages
   - Network error detection
   - Graceful failure handling

## UI Screens Implemented

1. **Splash Screen**
   - Auto-check auth status
   - Route to login or home

2. **Login Screen**
   - Phone number input
   - Form validation
   - Loading states
   - Error display

3. **OTP Verification Screen**
   - 6-digit code input
   - Resend OTP button
   - Auto-navigation on success

4. **Home Screen**
   - Welcome message
   - User info display
   - Bottom navigation
   - Logout functionality

## Next Steps for Development

### Immediate Tasks

1. **Run Code Generation**
   ```bash
   flutter pub run build_runner build --delete-conflicting-outputs
   ```

2. **Test Authentication Flow**
   - Run the app
   - Enter phone number
   - Verify OTP
   - Check token storage

### Phase 2 Features (To Implement)

1. **Home Screen Enhancement**
   - Popular categories display
   - Featured providers
   - Quick search
   - Promotional banners

2. **Search & Discovery**
   - Provider search by category
   - Location-based search
   - Filters (price, rating, distance)
   - Map view

3. **Provider Details**
   - Provider profile
   - Services list
   - Reviews & ratings
   - Gallery
   - Working hours

4. **Booking Flow**
   - Service selection
   - Date & time picker
   - Staff selection
   - Booking confirmation
   - Payment integration

5. **Bookings Management**
   - Upcoming bookings
   - Past bookings
   - Booking details
   - Cancel/reschedule
   - Booking history

6. **User Profile**
   - Edit profile
   - Profile picture
   - Favorite providers
   - Notification settings
   - Payment methods

7. **Reviews & Ratings**
   - Leave reviews
   - Rate services
   - Upload photos
   - View review history

### Additional Features

- Push notifications (Firebase Cloud Messaging)
- Deep linking
- Share functionality
- In-app chat with providers
- Payment gateway integration
- Multi-language support (English/Persian)
- Dark mode
- Offline mode with caching

## How to Get Started

### 1. Install Dependencies

```bash
flutter pub get
```

### 2. Generate Code

```bash
flutter pub run build_runner build --delete-conflicting-outputs
```

### 3. Run the App

```bash
flutter run
```

See [SETUP_GUIDE.md](SETUP_GUIDE.md) for detailed instructions.

## Architecture Benefits

### Testability
- Each layer can be tested independently
- Mock repositories for UI testing
- Mock API services for repository testing

### Maintainability
- Clear separation of concerns
- Easy to locate and fix bugs
- Consistent code structure

### Scalability
- Easy to add new features
- Modular architecture
- Reusable components

### Team Collaboration
- Clear boundaries between layers
- Multiple developers can work simultaneously
- Standardized patterns

## Development Best Practices

1. **Always run code generation** after model changes
2. **Use BLoC for state management** (consistent pattern)
3. **Keep UI logic in BLoC**, not in widgets
4. **Use Either for error handling** in repositories
5. **Validate inputs** in use cases
6. **Keep entities pure** (no framework dependencies)
7. **Use dependency injection** for all services

## Common Commands

```bash
# Get dependencies
flutter pub get

# Code generation
flutter pub run build_runner build --delete-conflicting-outputs

# Run app
flutter run

# Run tests
flutter test

# Build APK
flutter build apk --release

# Format code
dart format lib/

# Analyze code
flutter analyze
```

## Documentation Files

- [README.md](README.md) - Full project documentation
- [SETUP_GUIDE.md](SETUP_GUIDE.md) - Setup instructions
- [PROJECT_SUMMARY.md](PROJECT_SUMMARY.md) - This file

## API Backend

The app connects to the Booksy backend:

- **Base URL**: http://napstar.ir/api
- **UserManagement API**: Port 5020
- **ServiceCatalog API**: Port 5010
- **Swagger Docs**: http://napstar.ir:5001/swagger

## Contact & Support

For questions about the codebase:
- Review the architecture documentation
- Check code comments
- Refer to Flutter/BLoC documentation

---

**Built with Clean Architecture + BLoC + Retrofit + Injectable**

Ready for production deployment! 🚀
