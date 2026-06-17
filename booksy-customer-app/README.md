# Booksy Customer App

Flutter mobile application for Booksy platform customers. Book beauty and wellness services with ease.

## Overview

The Booksy Customer App is a mobile application built with Flutter that allows customers to:
- Browse and search for beauty and wellness service providers
- View service catalogs and provider profiles
- Book appointments with real-time availability
- Manage bookings (view, reschedule, cancel)
- Review and rate service providers
- Track booking history

## Architecture

This project follows **Clean Architecture** principles with the following layers:

```
lib/
├── core/                      # Core functionality
│   ├── api/                   # API configuration
│   │   ├── client/           # Dio client setup
│   │   ├── config/           # API constants
│   │   ├── interceptors/     # Auth & error interceptors
│   │   └── models/           # Shared API models
│   ├── di/                    # Dependency injection
│   ├── errors/                # Error handling
│   ├── storage/               # Secure storage
│   └── utils/                 # Utilities
│
├── features/                  # Feature modules
│   ├── auth/                  # Authentication
│   │   ├── data/             # Data layer
│   │   │   ├── datasources/  # API services
│   │   │   ├── models/       # DTOs
│   │   │   └── repositories/ # Repository implementations
│   │   ├── domain/           # Domain layer
│   │   │   ├── entities/     # Business entities
│   │   │   ├── repositories/ # Repository interfaces
│   │   │   └── usecases/     # Business logic
│   │   └── presentation/     # Presentation layer
│   │       ├── bloc/         # State management
│   │       ├── pages/        # UI screens
│   │       └── widgets/      # Reusable widgets
│   │
│   ├── booking/              # Booking management
│   ├── home/                 # Home screen
│   ├── profile/              # User profile
│   └── search/               # Provider search
│
└── config/                    # App configuration
    ├── routes/               # Navigation
    └── theme/                # Theming
```

## Tech Stack

### State Management
- **flutter_bloc**: BLoC pattern for state management
- **equatable**: Value equality for state and events

### API & Networking
- **dio**: HTTP client
- **retrofit**: Type-safe REST client
- **json_annotation**: JSON serialization
- **pretty_dio_logger**: API logging

### Local Storage
- **shared_preferences**: Simple key-value storage
- **flutter_secure_storage**: Encrypted storage for tokens

### Navigation
- **go_router**: Declarative routing

### UI Components
- **flutter_screenutil**: Responsive design
- **cached_network_image**: Image caching
- **shimmer**: Loading placeholders

### Dependency Injection
- **get_it**: Service locator
- **injectable**: Code generation for DI

## Getting Started

### Prerequisites

- Flutter SDK (3.0.0 or higher)
- Dart SDK (3.0.0 or higher)
- Android Studio / VS Code
- iOS: Xcode (for iOS development)
- Android: Android SDK

### Installation

1. Clone the repository:
```bash
git clone <repository-url>
cd booksy-customer-app
```

2. Install dependencies:
```bash
flutter pub get
```

3. Generate code (for JSON serialization, Retrofit, Injectable):
```bash
flutter pub run build_runner build --delete-conflicting-outputs
```

4. Run the app:
```bash
# Debug mode
flutter run

# Release mode
flutter run --release

# Specific device
flutter run -d <device-id>
```

## API Configuration

The app connects to the Booksy backend APIs:

- **Base URL**: `http://napstar.ir/api`
- **User Management API**: Port 5020
- **Service Catalog API**: Port 5010

API configuration is located in:
- [lib/core/api/config/api_constants.dart](lib/core/api/config/api_constants.dart)

### Endpoints

#### Authentication
- `POST /v1/Auth/send-verification-code` - Send OTP
- `POST /v1/Auth/customer/complete-authentication` - Verify OTP & Login
- `POST /v1/Auth/refresh` - Refresh token
- `POST /v1/Auth/logout` - Logout

#### Categories
- `GET /v1/Categories` - Get all categories
- `GET /v1/Categories/popular` - Get popular categories

#### Providers
- `POST /v1/Providers/search` - Search providers
- `GET /v1/Providers/{id}` - Get provider details

#### Bookings
- `GET /v1/Bookings/my-bookings` - Get customer bookings
- `POST /v1/Bookings` - Create booking
- `POST /v1/Bookings/{id}/cancel` - Cancel booking
- `POST /v1/Bookings/{id}/reschedule` - Reschedule booking

## Authentication Flow

The app uses **OTP-based authentication**:

1. User enters phone number
2. Backend sends 6-digit OTP via SMS
3. User enters OTP code
4. Backend verifies OTP and returns JWT tokens
5. Tokens are stored securely using `flutter_secure_storage`
6. Access token is automatically added to API requests via interceptor
7. Refresh token is used to get new access token when expired

### Security Features

- JWT tokens stored in encrypted storage
- Automatic token refresh on 401 errors
- Secure logout (clears local session + API call)
- Request/response interceptors for auth headers

## State Management

The app uses **BLoC pattern** for state management:

### Auth BLoC Example

```dart
// Event
context.read<AuthBloc>().add(
  SendVerificationCodeEvent(phoneNumber: '09123456789'),
);

// State Listener
BlocListener<AuthBloc, AuthState>(
  listener: (context, state) {
    if (state is OtpSentSuccess) {
      // Navigate to OTP page
    } else if (state is AuthError) {
      // Show error
    }
  },
  child: ...,
)
```

## Code Generation

This project uses code generation for:

1. **JSON Serialization** (json_serializable)
2. **Retrofit API Services** (retrofit_generator)
3. **Dependency Injection** (injectable_generator)

Run code generation:

```bash
# Watch mode (auto-regenerate on file changes)
flutter pub run build_runner watch

# One-time build
flutter pub run build_runner build --delete-conflicting-outputs
```

## Project Structure Explained

### Clean Architecture Layers

1. **Data Layer** (`features/*/data/`)
   - API services (Retrofit)
   - DTOs (Data Transfer Objects)
   - Repository implementations
   - Maps external data to domain entities

2. **Domain Layer** (`features/*/domain/`)
   - Business entities
   - Repository interfaces
   - Use cases (business logic)
   - No dependencies on external frameworks

3. **Presentation Layer** (`features/*/presentation/`)
   - BLoC (state management)
   - Pages (UI screens)
   - Widgets (reusable components)
   - Depends on domain layer only

### Dependency Flow

```
Presentation → Domain ← Data
```

- Presentation depends on Domain
- Data depends on Domain
- Domain depends on nothing (pure Dart)

## Error Handling

The app uses **Either** type from `dartz` package for error handling:

```dart
Future<Either<Failure, AuthSession>> completeAuthentication(...) async {
  try {
    final result = await apiService.completeAuth(request);
    return Right(result); // Success
  } catch (e) {
    return Left(ServerFailure('Error message')); // Failure
  }
}
```

### Failure Types

- `ServerFailure`: API errors
- `NetworkFailure`: Connection errors
- `AuthFailure`: Authentication errors
- `ValidationFailure`: Input validation errors
- `CacheFailure`: Storage errors

## Testing

Run tests:

```bash
# All tests
flutter test

# Specific test file
flutter test test/features/auth/auth_bloc_test.dart

# With coverage
flutter test --coverage
```

## Building for Release

### Android

```bash
# Build APK
flutter build apk --release

# Build App Bundle (for Google Play)
flutter build appbundle --release
```

### iOS

```bash
# Build for iOS
flutter build ios --release
```

## Environment Variables

For production, create environment-specific configurations:

```dart
// lib/core/api/config/env.dart
class Environment {
  static const String apiBaseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://napstar.ir/api',
  );
}
```

Run with environment variables:

```bash
flutter run --dart-define=API_BASE_URL=https://api.production.com
```

## Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is proprietary and confidential.

## Contact

For questions or support, contact the development team.
