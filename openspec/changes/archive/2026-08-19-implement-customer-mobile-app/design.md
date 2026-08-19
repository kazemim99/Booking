# Design: Customer Mobile App Architecture

## Overview

This document outlines the architectural approach for implementing the Booksy customer mobile application using Flutter. The app follows **Clean Architecture** principles with **BLoC pattern** for state management, ensuring maintainability, testability, and scalability.

## Architecture Principles

### 1. Clean Architecture Layers

```
┌─────────────────────────────────────────────┐
│         Presentation Layer                  │
│  ┌──────────────────────────────────────┐  │
│  │ Pages (Screens)                       │  │
│  │ - UI Widgets                          │  │
│  │ - BLoC Listeners/Builders             │  │
│  └──────────────────────────────────────┘  │
│  ┌──────────────────────────────────────┐  │
│  │ BLoC (State Management)               │  │
│  │ - Events                              │  │
│  │ - States                              │  │
│  │ - Business Logic                      │  │
│  └──────────────────────────────────────┘  │
└─────────────────────────────────────────────┘
                     │
┌─────────────────────────────────────────────┐
│         Domain Layer                        │
│  ┌──────────────────────────────────────┐  │
│  │ Entities (Pure Dart Classes)          │  │
│  │ - User, Provider, Booking, etc.       │  │
│  └──────────────────────────────────────┘  │
│  ┌──────────────────────────────────────┐  │
│  │ Repository Interfaces (Abstract)      │  │
│  │ - AuthRepository                      │  │
│  │ - ProviderRepository                  │  │
│  │ - BookingRepository                   │  │
│  └──────────────────────────────────────┘  │
│  ┌──────────────────────────────────────┐  │
│  │ Use Cases                             │  │
│  │ - SearchProvidersUseCase              │  │
│  │ - CreateBookingUseCase                │  │
│  └──────────────────────────────────────┘  │
└─────────────────────────────────────────────┘
                     │
┌─────────────────────────────────────────────┐
│         Data Layer                          │
│  ┌──────────────────────────────────────┐  │
│  │ Data Sources                          │  │
│  │ - API Services (Retrofit + Dio)       │  │
│  │ - Local Storage (Secure Storage)      │  │
│  └──────────────────────────────────────┘  │
│  ┌──────────────────────────────────────┐  │
│  │ Models/DTOs                           │  │
│  │ - JSON Serialization (@JsonSerializable)│
│  │ - Mapping to Domain Entities          │  │
│  └──────────────────────────────────────┘  │
│  ┌──────────────────────────────────────┐  │
│  │ Repository Implementations            │  │
│  │ - AuthRepositoryImpl                  │  │
│  │ - ProviderRepositoryImpl              │  │
│  └──────────────────────────────────────┘  │
└─────────────────────────────────────────────┘
                     │
┌─────────────────────────────────────────────┐
│         Core Layer (Infrastructure)         │
│  - DI Container (GetIt + Injectable)        │
│  - API Client Configuration (Dio instances) │
│  - Interceptors (Auth, Error, Logging)      │
│  - Storage Services (Secure, Cache)         │
│  - Error Handling (Failure classes)         │
│  - Utilities (Date, String, Price)          │
└─────────────────────────────────────────────┘
```

### 2. Feature-Based Module Structure

Each feature is self-contained:

```
features/
├── auth/                      ✅ COMPLETE
│   ├── data/
│   │   ├── datasources/       API service (Retrofit)
│   │   ├── models/            DTOs with JSON serialization
│   │   └── repositories/      Repository implementation
│   ├── domain/
│   │   ├── entities/          Pure Dart entities
│   │   ├── repositories/      Abstract repository interface
│   │   └── usecases/          Business logic use cases
│   └── presentation/
│       ├── bloc/              BLoC (events, states, logic)
│       ├── pages/             Screen widgets
│       └── widgets/           Reusable components
│
├── search/                    🏗️ TO IMPLEMENT
│   ├── data/
│   ├── domain/
│   └── presentation/
│
├── booking/                   🏗️ TO IMPLEMENT
│   ├── data/
│   ├── domain/
│   └── presentation/
│
├── home/                      🏗️ TO IMPLEMENT
│   └── presentation/          (Mostly presentation, minimal data)
│
└── profile/                   🏗️ TO IMPLEMENT
    ├── data/
    ├── domain/
    └── presentation/
```

## Key Design Patterns

### 1. BLoC Pattern (State Management)

**Why BLoC?**
- Clear separation between UI and business logic
- Testable (events/states can be unit tested)
- Reactive (stream-based)
- Established in Flutter community

**BLoC Structure:**
```dart
// Event
abstract class SearchEvent {}
class SearchProvidersEvent extends SearchEvent {
  final String query;
  final List<String> categoryIds;
  SearchProvidersEvent({required this.query, this.categoryIds = const []});
}

// State
abstract class SearchState {}
class SearchInitial extends SearchState {}
class SearchLoading extends SearchState {}
class SearchSuccess extends SearchState {
  final List<Provider> providers;
  SearchSuccess(this.providers);
}
class SearchError extends SearchState {
  final String message;
  SearchError(this.message);
}

// BLoC
class SearchBloc extends Bloc<SearchEvent, SearchState> {
  final SearchProvidersUseCase searchProvidersUseCase;

  SearchBloc({required this.searchProvidersUseCase}) : super(SearchInitial()) {
    on<SearchProvidersEvent>(_onSearchProviders);
  }

  Future<void> _onSearchProviders(
    SearchProvidersEvent event,
    Emitter<SearchState> emit,
  ) async {
    emit(SearchLoading());
    final result = await searchProvidersUseCase(
      query: event.query,
      categoryIds: event.categoryIds,
    );
    result.fold(
      (failure) => emit(SearchError(failure.message)),
      (providers) => emit(SearchSuccess(providers)),
    );
  }
}
```

### 2. Either/Result Pattern (Error Handling)

**Why Either?**
- Type-safe error handling without exceptions
- Forces explicit error handling
- Clear success vs failure paths

**Usage:**
```dart
// Use Case returns Either<Failure, Success>
Future<Either<Failure, List<Provider>>> searchProviders({
  required String query,
  List<String> categoryIds = const [],
}) async {
  try {
    final result = await repository.searchProviders(query, categoryIds);
    return Right(result);
  } on ServerException catch (e) {
    return Left(ServerFailure(e.message));
  } on NetworkException {
    return Left(NetworkFailure('اتصال اینترنت قطع است'));
  }
}
```

### 3. Repository Pattern

**Why Repository?**
- Abstracts data sources (API, local storage, cache)
- Swappable implementations (mock for tests)
- Single source of truth for data access

**Pattern:**
```dart
// Domain layer (abstract)
abstract class ProviderRepository {
  Future<Either<Failure, List<Provider>>> searchProviders(
    String query,
    List<String> categoryIds,
  );
  Future<Either<Failure, Provider>> getProviderById(String id);
}

// Data layer (concrete)
class ProviderRepositoryImpl implements ProviderRepository {
  final ProviderApiService apiService;
  final CacheService cacheService;

  @override
  Future<Either<Failure, List<Provider>>> searchProviders(...) async {
    // Check cache first
    final cached = await cacheService.get('providers_$query');
    if (cached != null) return Right(cached);

    // Fetch from API
    final response = await apiService.searchProviders(...);

    // Map DTO to Entity
    final providers = response.data.map((dto) => dto.toEntity()).toList();

    // Cache result
    await cacheService.set('providers_$query', providers);

    return Right(providers);
  }
}
```

### 4. Dependency Injection (GetIt + Injectable)

**Why DI?**
- Loose coupling between layers
- Easy to swap implementations
- Testability (inject mocks)
- Singleton management

**Setup:**
```dart
// injection.dart
@InjectableInit()
void configureDependencies() => getIt.init();

final getIt = GetIt.instance;

// DioModule (separate Dio instances)
@module
abstract class DioModule {
  @Named('authDio')
  @singleton
  Dio get authDio => /* config without auth interceptor */;

  @Named('userManagementDio')
  @singleton
  Dio get userManagementDio => /* config with auth interceptor */;

  @Named('serviceCatalogDio')
  @singleton
  Dio get serviceCatalogDio => /* config with auth interceptor */;
}

// Usage
@injectable
class AuthApiService {
  AuthApiService(@Named('authDio') Dio dio);
}

@injectable
class ProviderApiService {
  ProviderApiService(@Named('serviceCatalogDio') Dio dio);
}
```

## Navigation Architecture

### GoRouter Configuration

**Why GoRouter?**
- Declarative routing
- Deep linking support
- Type-safe routes
- Web support (future-proof)

**Route Structure:**
```dart
final router = GoRouter(
  initialLocation: '/splash',
  routes: [
    GoRoute(
      path: '/splash',
      builder: (context, state) => SplashPage(),
    ),
    GoRoute(
      path: '/login',
      builder: (context, state) => LoginPage(),
    ),
    GoRoute(
      path: '/otp',
      builder: (context, state) => OtpVerificationPage(
        phoneNumber: state.extra as String,
      ),
    ),
    ShellRoute(
      builder: (context, state, child) => MainScaffold(child: child),
      routes: [
        GoRoute(
          path: '/home',
          builder: (context, state) => HomePage(),
        ),
        GoRoute(
          path: '/search',
          builder: (context, state) => SearchPage(),
        ),
        GoRoute(
          path: '/bookings',
          builder: (context, state) => BookingsPage(),
        ),
        GoRoute(
          path: '/profile',
          builder: (context, state) => ProfilePage(),
        ),
      ],
    ),
    GoRoute(
      path: '/provider/:id',
      builder: (context, state) => ProviderDetailPage(
        providerId: state.pathParameters['id']!,
      ),
    ),
    GoRoute(
      path: '/booking/:providerId',
      builder: (context, state) => BookingWizardPage(
        providerId: state.pathParameters['providerId']!,
      ),
    ),
  ],
);
```

### Bottom Navigation (Main Scaffold)

```dart
class MainScaffold extends StatelessWidget {
  final Widget child;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: child,
      bottomNavigationBar: BottomNavigationBar(
        currentIndex: _calculateSelectedIndex(context),
        onTap: (index) => _onItemTapped(index, context),
        items: [
          BottomNavigationBarItem(icon: Icon(Icons.home), label: 'خانه'),
          BottomNavigationBarItem(icon: Icon(Icons.search), label: 'جستجو'),
          BottomNavigationBarItem(icon: Icon(Icons.calendar_today), label: 'رزروها'),
          BottomNavigationBarItem(icon: Icon(Icons.person), label: 'پروفایل'),
        ],
      ),
    );
  }
}
```

## API Integration Strategy

### Three Dio Instances (Prevents Circular Dependencies)

1. **authDio** - No auth interceptor
   - Used for: Login, OTP verification, registration
   - No Bearer token

2. **userManagementDio** - With auth interceptor
   - Used for: User profile, customer endpoints
   - Auto-adds Bearer token
   - Auto-refreshes on 401

3. **serviceCatalogDio** - With auth interceptor
   - Used for: Providers, services, bookings
   - Auto-adds Bearer token
   - Auto-refreshes on 401

### Auth Interceptor (Token Refresh Logic)

```dart
class AuthInterceptor extends Interceptor {
  final Dio authDio; // Uses authDio to avoid circular dependency
  final SecureStorageService storage;

  @override
  Future<void> onError(DioException err, ErrorInterceptorHandler handler) async {
    if (err.response?.statusCode == 401) {
      // Token expired, try refresh
      try {
        final refreshToken = await storage.getRefreshToken();
        final response = await authDio.post(
          '/api/v1/Auth/refresh',
          data: {'refreshToken': refreshToken},
        );

        // Save new tokens
        await storage.saveAccessToken(response.data['accessToken']);
        await storage.saveRefreshToken(response.data['refreshToken']);

        // Retry original request
        final opts = Options(
          method: err.requestOptions.method,
          headers: {
            ...err.requestOptions.headers,
            'Authorization': 'Bearer ${response.data['accessToken']}',
          },
        );
        final clonedRequest = await authDio.request(
          err.requestOptions.path,
          options: opts,
          data: err.requestOptions.data,
        );

        return handler.resolve(clonedRequest);
      } catch (e) {
        // Refresh failed, logout user
        await storage.clearAuthSession();
        // Navigate to login
        return handler.reject(err);
      }
    }
    return handler.next(err);
  }
}
```

### Error Interceptor (User-Friendly Messages)

```dart
class ErrorInterceptor extends Interceptor {
  @override
  void onError(DioException err, ErrorInterceptorHandler handler) {
    String message;

    switch (err.type) {
      case DioExceptionType.connectionTimeout:
      case DioExceptionType.receiveTimeout:
        message = 'زمان اتصال به سرور به پایان رسید';
        break;
      case DioExceptionType.connectionError:
        message = 'اتصال اینترنت قطع است';
        break;
      case DioExceptionType.badResponse:
        message = _extractServerError(err.response);
        break;
      default:
        message = 'خطای غیرمنتظره رخ داد';
    }

    // Attach user-friendly message
    err = err.copyWith(message: message);
    handler.next(err);
  }

  String _extractServerError(Response? response) {
    try {
      final data = response?.data;
      if (data is Map && data.containsKey('message')) {
        return data['message'];
      }
      return 'خطا در پردازش درخواست';
    } catch (_) {
      return 'خطا در سرور';
    }
  }
}
```

## State Persistence Strategy

### Secure Storage (Encrypted)
- **Use Case**: Sensitive data (tokens, user ID)
- **Implementation**: Flutter Secure Storage (Android: EncryptedSharedPreferences)
- **Data**:
  - access_token
  - refresh_token
  - user_id
  - customer_id
  - phone_number

### Regular Storage (Shared Preferences)
- **Use Case**: Non-sensitive preferences
- **Data**:
  - Theme preference (light/dark)
  - Language preference
  - Notification settings
  - Last search queries

### In-Memory Cache (Hive or similar)
- **Use Case**: API response caching
- **Data**:
  - Provider search results (TTL: 5 min)
  - Category list (TTL: 1 hour)
  - User profile (TTL: 10 min)

## Performance Optimizations

### 1. Image Loading
```dart
CachedNetworkImage(
  imageUrl: provider.thumbnailUrl,
  placeholder: (context, url) => Shimmer(...),
  errorWidget: (context, url, error) => Icon(Icons.error),
  memCacheWidth: 400, // Resize for display
  maxHeightDiskCache: 400,
)
```

### 2. Pagination
```dart
class ProvidersListView extends StatefulWidget {
  @override
  Widget build(BuildContext context) {
    return ListView.builder(
      itemCount: providers.length + 1,
      itemBuilder: (context, index) {
        if (index == providers.length) {
          // Load more trigger
          context.read<SearchBloc>().add(LoadMoreProvidersEvent());
          return LoadingIndicator();
        }
        return ProviderCard(providers[index]);
      },
    );
  }
}
```

### 3. Debounced Search
```dart
class SearchBar extends StatefulWidget {
  final void Function(String) onSearch;

  @override
  _SearchBarState createState() => _SearchBarState();
}

class _SearchBarState extends State<SearchBar> {
  Timer? _debounce;

  void _onSearchChanged(String query) {
    if (_debounce?.isActive ?? false) _debounce!.cancel();
    _debounce = Timer(const Duration(milliseconds: 500), () {
      widget.onSearch(query);
    });
  }

  @override
  void dispose() {
    _debounce?.cancel();
    super.dispose();
  }
}
```

## Testing Strategy

### 1. Unit Tests (Domain & BLoC)
```dart
// Use Case Test
test('SearchProvidersUseCase returns providers on success', () async {
  when(mockRepository.searchProviders(any, any))
      .thenAnswer((_) async => Right(mockProviders));

  final result = await useCase(query: 'salon');

  expect(result, Right(mockProviders));
  verify(mockRepository.searchProviders('salon', [])).called(1);
});

// BLoC Test
blocTest<SearchBloc, SearchState>(
  'emits [SearchLoading, SearchSuccess] when search succeeds',
  build: () => SearchBloc(searchProvidersUseCase: mockUseCase),
  act: (bloc) => bloc.add(SearchProvidersEvent(query: 'salon')),
  expect: () => [
    SearchLoading(),
    SearchSuccess(mockProviders),
  ],
);
```

### 2. Widget Tests
```dart
testWidgets('SearchPage displays results', (WidgetTester tester) async {
  await tester.pumpWidget(
    MaterialApp(
      home: BlocProvider(
        create: (_) => mockSearchBloc,
        child: SearchPage(),
      ),
    ),
  );

  // Trigger search
  await tester.enterText(find.byType(TextField), 'salon');
  await tester.pump(Duration(milliseconds: 500)); // Debounce

  // Verify loading state
  expect(find.byType(CircularProgressIndicator), findsOneWidget);

  // Emit success state
  when(mockSearchBloc.state).thenReturn(SearchSuccess(mockProviders));
  await tester.pump();

  // Verify providers displayed
  expect(find.byType(ProviderCard), findsNWidgets(mockProviders.length));
});
```

### 3. Integration Tests (E2E)
```dart
void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets('Complete booking flow', (tester) async {
    app.main();
    await tester.pumpAndSettle();

    // Login
    await tester.tap(find.text('ورود / ثبت‌نام'));
    await tester.pumpAndSettle();

    await tester.enterText(find.byType(TextField), '09123456789');
    await tester.tap(find.text('ارسال کد تایید'));
    await tester.pumpAndSettle();

    // ... OTP verification ...

    // Search provider
    await tester.tap(find.byIcon(Icons.search));
    await tester.pumpAndSettle();

    await tester.enterText(find.byType(TextField), 'آرایشگاه');
    await tester.pumpAndSettle(Duration(seconds: 1));

    // Tap first result
    await tester.tap(find.byType(ProviderCard).first);
    await tester.pumpAndSettle();

    // Select service and book
    await tester.tap(find.text('رزرو نوبت'));
    await tester.pumpAndSettle();

    // ... complete booking wizard ...

    // Verify booking confirmation
    expect(find.text('رزرو شما ثبت شد!'), findsOneWidget);
  });
}
```

## Design System & Visual Language

### Color Palette (Professional & Gender-Neutral)

**Primary Color**: Dark Blue (#1A365D) - Professional, trustworthy, gender-neutral
- Used for: Primary CTAs, active states, important icons
- Tint variants: #2D4A7C (hover), #0F2744 (pressed)

**Neutral Palette**:
- Background: #FFFFFF (light mode), #0F1419 (dark mode)
- Surface: #F7F9FC (light), #1A1F25 (dark)
- Borders: #E2E8F0
- Text Primary: #1A202C (light), #F7FAFC (dark)
- Text Secondary: #718096
- Text Tertiary: #A0AEC0

**Semantic Colors** (minimal, functional only):
- Success: #059669 (green) - booking confirmed, successful actions
- Warning: #D97706 (amber) - reminders, caution
- Error: #DC2626 (red) - validation errors, cancellations
- Info: #0284C7 (blue) - informational messages

**Design Principles**:
- ✅ Single primary color (dark blue) for all interactive elements
- ✅ Neutral grays for backgrounds, borders, and text hierarchy
- ✅ No decorative colors (no pink, purple, bright colors)
- ✅ High contrast for readability (WCAG AA compliance)
- ✅ Gender-neutral throughout (professional service booking platform)

### Typography (Vazir Font Family)

**Font Stack**: Vazir (Persian), SF Pro (iOS fallback), Roboto (Android fallback)

```dart
// Font weights
regular: 400
medium: 500
semibold: 600
bold: 700

// Size scale
h1: 28.sp, bold         // Page titles
h2: 22.sp, semibold     // Section headers
h3: 18.sp, semibold     // Card titles
body: 16.sp, regular    // Main text
caption: 14.sp, regular // Secondary text
small: 12.sp, regular   // Helper text
```

### Component Design (Minimal & Functional)

**Buttons**:
```dart
// Primary Button
- Background: #1A365D (dark blue)
- Text: #FFFFFF (white)
- Border radius: 12.r
- Height: 48.h (min touch target)
- Padding: 16.h vertical, 24.w horizontal
- Elevation: 0 (flat design, no shadows)
- Pressed state: #0F2744 (darker blue)

// Secondary Button
- Background: transparent
- Border: 1.5px solid #1A365D
- Text: #1A365D
- Border radius: 12.r
- Height: 48.h

// Text Button
- Background: transparent
- Text: #1A365D
- No border
- Underline on press
```

**Cards**:
```dart
- Background: #FFFFFF (light), #1A1F25 (dark)
- Border: 1px solid #E2E8F0
- Border radius: 16.r
- Padding: 16.w
- Elevation: 0 (no shadow, flat design)
- Hover: Border color #1A365D
```

**Input Fields**:
```dart
- Border: 1.5px solid #E2E8F0
- Border radius: 12.r
- Height: 56.h
- Padding: 16.w horizontal
- Focus: Border color #1A365D, 2px width
- Error: Border color #DC2626
- Disabled: Background #F7F9FC, border #E2E8F0
```

**Icons**:
- Line icons only (no filled icons except for states)
- Weight: 1.5px stroke
- Size: 24.w × 24.h standard
- Color: Inherit from text color (#718096 default, #1A365D active)

**Spacing Scale**:
```dart
4.w  // xs  - tight spacing
8.w  // s   - small spacing
12.w // m   - medium spacing
16.w // l   - large spacing (default)
24.w // xl  - section spacing
32.w // xxl - page spacing
```

**Simplicity Guidelines**:
- ❌ No gradients
- ❌ No shadows/elevations (flat design)
- ❌ No decorative illustrations
- ❌ No background patterns
- ❌ No multiple accent colors
- ✅ Clean lines and borders
- ✅ Generous whitespace
- ✅ Functional icons only
- ✅ Clear visual hierarchy through typography and spacing

## Persian/RTL Considerations

### 1. Text Direction
```dart
MaterialApp(
  locale: Locale('fa', 'IR'),
  supportedLocales: [Locale('fa', 'IR')],
  localizationsDelegates: [
    GlobalMaterialLocalizations.delegate,
    GlobalWidgetsLocalizations.delegate,
    GlobalCupertinoLocalizations.delegate,
  ],
  builder: (context, child) {
    return Directionality(
      textDirection: TextDirection.rtl,
      child: child!,
    );
  },
)
```

### 2. Jalali Date Formatting
```dart
class PersianDateService {
  static String formatDate(DateTime date) {
    final jalali = Jalali.fromDateTime(date);
    return '${jalali.day} ${_monthName(jalali.month)} ${jalali.year}';
  }

  static String _monthName(int month) {
    const months = [
      'فروردین', 'اردیبهشت', 'خرداد', 'تیر', 'مرداد', 'شهریور',
      'مهر', 'آبان', 'آذر', 'دی', 'بهمن', 'اسفند'
    ];
    return months[month - 1];
  }
}
```

### 3. Persian Numbers
```dart
extension PersianNumbers on String {
  String toPersianDigits() {
    const english = ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9'];
    const persian = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹'];

    var result = this;
    for (var i = 0; i < english.length; i++) {
      result = result.replaceAll(english[i], persian[i]);
    }
    return result;
  }
}

// Usage
Text('قیمت: ${price.toString().toPersianDigits()} تومان')
```

## Security Considerations

1. **Token Storage**: Use Flutter Secure Storage (encrypted at rest)
2. **Certificate Pinning**: Implement for production API calls
3. **Obfuscation**: Enable Dart code obfuscation in release builds
4. **API Keys**: Store in environment-specific config files (not in source)
5. **Input Validation**: Sanitize all user inputs before API submission
6. **Deep Link Validation**: Verify deep link sources to prevent phishing

## Build & Deployment

### Flavor Configuration
```dart
// Development
flutter run --flavor development --dart-define=BASE_URL=http://192.168.1.5:5000

// Production
flutter run --flavor production --dart-define=BASE_URL=http://napstar.ir
```

### Release Build
```bash
# Android
flutter build apk --release --flavor production

# iOS
flutter build ios --release --flavor production
```

### CI/CD Pipeline (GitHub Actions)
```yaml
name: Build and Test
on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - uses: subosito/flutter-action@v2
      - run: flutter pub get
      - run: flutter analyze
      - run: flutter test

  build-android:
    needs: test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - uses: subosito/flutter-action@v2
      - run: flutter build apk --release --flavor production
      - uses: actions/upload-artifact@v2
        with:
          name: release-apk
          path: build/app/outputs/flutter-apk/app-production-release.apk
```

## Conclusion

This architecture provides:
- ✅ **Maintainability**: Clear separation of concerns, modular structure
- ✅ **Testability**: Mockable dependencies, pure business logic
- ✅ **Scalability**: Easy to add new features without breaking existing code
- ✅ **Performance**: Optimized for mobile (caching, pagination, image loading)
- ✅ **Cultural Fit**: Persian-first design with RTL, Jalali dates, localized UX
- ✅ **Reliability**: Error handling, offline support, token refresh
