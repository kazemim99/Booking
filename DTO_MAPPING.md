# Booksy DTO Mapping Reference

This document maps Data Transfer Objects (DTOs) across all three layers of the Booksy platform:
- **Backend (C#)**: ASP.NET Core API request/response models
- **Frontend Vue**: TypeScript interfaces/types
- **Flutter App**: Dart models with JSON serialization

**Last Updated**: 2026-01-05

---

## Table of Contents

- [Naming Conventions](#naming-conventions)
- [Authentication DTOs](#authentication-dtos)
- [Customer DTOs](#customer-dtos)
- [Provider DTOs](#provider-dtos)
- [Category DTOs](#category-dtos)
- [Booking DTOs](#booking-dtos)
- [Service DTOs](#service-dtos)
- [Common/Shared DTOs](#commonshared-dtos)

---

## Naming Conventions

### Backend (C#)
- **Requests**: `[Action][Entity]Request` (e.g., `CreateBookingRequest`)
- **Responses**: `[Entity]Response` or `[Entity]Dto` (e.g., `BookingResponse`, `UserDto`)
- **Commands**: `[Action][Entity]Command` (e.g., `CreateBookingCommand`)
- **Queries**: `Get[Entity]Query` (e.g., `GetBookingDetailsQuery`)
- **ViewModels**: `[Entity]ViewModel` (e.g., `CustomerDetailsViewModel`)

### Frontend Vue (TypeScript)
- **Files**: kebab-case (e.g., `provider-list.vue`, `booking-types.ts`)
- **Interfaces**: PascalCase with `I` prefix (e.g., `IProvider`, `IBooking`)
- **Types**: PascalCase (e.g., `Provider`, `Booking`)

### Flutter (Dart)
- **Files**: snake_case (e.g., `auth_models.dart`, `booking_models.dart`)
- **Classes**: PascalCase (e.g., `BookingDto`, `UserDto`)
- **Properties**: camelCase (e.g., `firstName`, `phoneNumber`)
- **Annotation**: `@JsonSerializable()` on all DTOs

---

## Authentication DTOs

### Send Verification Code

| Backend C# | Flutter Dart | Vue TypeScript | Notes |
|------------|--------------|----------------|-------|
| `SendVerificationCodeRequest` | `SendVerificationCodeRequest` | N/A (uses fetch directly) | Phone + country code |
| `SendVerificationCodeResponse` | `SendVerificationCodeResponse` | N/A | Verification ID, masked phone |

**Backend** (`src/UserManagement/.../AuthController.cs`):
```csharp
public record SendVerificationCodeRequest(
    string PhoneNumber,
    string CountryCode = "+98"
);

public record SendVerificationCodeResponse {
    public string VerificationId { get; init; }
    public string MaskedPhoneNumber { get; init; }
    public string ExpiresAt { get; init; }
    public int MaxAttempts { get; init; }
    public string Message { get; init; }
}
```

**Flutter** (`lib/features/auth/data/models/auth_models.dart`):
```dart
@JsonSerializable()
class SendVerificationCodeRequest {
  final String phoneNumber;
  final String countryCode;  // Default: '+98'
}

@JsonSerializable()
class SendVerificationCodeResponse {
  final String verificationId;
  final String maskedPhoneNumber;
  final String expiresAt;
  final int maxAttempts;
  final String message;
}
```

---

### Complete Customer Authentication

| Backend C# | Flutter Dart | Vue TypeScript | Notes |
|------------|--------------|----------------|-------|
| `CompleteCustomerAuthenticationRequest` | `CompleteCustomerAuthRequest` | N/A | Phone, code, optional profile data |
| `CompleteCustomerAuthenticationResponse` | `CompleteCustomerAuthResponse` | N/A | Tokens + User + Customer DTOs |

**Backend** (`src/UserManagement/.../AuthController.cs`):
```csharp
public record CompleteCustomerAuthenticationRequest {
    public string PhoneNumber { get; init; }
    public string Code { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
}

public record CompleteCustomerAuthenticationResponse {
    public string AccessToken { get; init; }
    public string RefreshToken { get; init; }
    public string UserId { get; init; }
    public string CustomerId { get; init; }
    public UserDto User { get; init; }
    public CustomerDto? Customer { get; init; }
    public int ExpiresIn { get; init; }
}
```

**Flutter** (`lib/features/auth/data/models/auth_models.dart`):
```dart
@JsonSerializable()
class CompleteCustomerAuthRequest {
  final String phoneNumber;
  final String code;
  final String? firstName;
  final String? lastName;
  final String? email;
}

@JsonSerializable()
class CompleteCustomerAuthResponse {
  final String accessToken;
  final String refreshToken;
  final String userId;
  final String customerId;
  final UserDto user;
  final CustomerDto? customer;
  final int expiresIn;
}
```

---

### User DTO

| Backend C# | Flutter Dart | Vue TypeScript | Notes |
|------------|--------------|----------------|-------|
| `UserDto` | `UserDto` | `IUser` | Core user profile data |

**Backend**:
```csharp
public record UserDto {
    public Guid UserId { get; init; }
    public string PhoneNumber { get; init; }
    public string? Email { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? ProfilePictureUrl { get; init; }
    public bool EmailVerified { get; init; }
    public bool PhoneVerified { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
```

**Flutter**:
```dart
@JsonSerializable()
class UserDto {
  final String id;              // Backend: Guid UserId
  final String phoneNumber;
  final String? email;
  final String? firstName;
  final String? lastName;
  final String? profilePictureUrl;
  final bool emailVerified;
  final bool phoneVerified;
  final DateTime createdAt;
  final DateTime? updatedAt;

  String get fullName {
    if (firstName != null && lastName != null) {
      return '$firstName $lastName';
    }
    return firstName ?? lastName ?? phoneNumber;
  }
}
```

---

### Customer DTO

| Backend C# | Flutter Dart | Vue TypeScript | Notes |
|------------|--------------|----------------|-------|
| `CustomerDto` | `CustomerDto` | `ICustomer` | Customer-specific data |

**Backend**:
```csharp
public record CustomerDto {
    public Guid CustomerId { get; init; }
    public Guid UserId { get; init; }
    public string? PreferredLanguage { get; init; }
    public List<Guid>? FavoriteProviders { get; init; }
    public int BookingCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
```

**Flutter**:
```dart
@JsonSerializable()
class CustomerDto {
  final String id;              // Backend: Guid CustomerId
  final String userId;          // Backend: Guid UserId
  final String? preferredLanguage;
  final List<String>? favoriteProviders;  // Backend: List<Guid>
  final int bookingCount;
  final DateTime createdAt;
  final DateTime? updatedAt;
}
```

---

## Customer DTOs

### Favorite Provider

| Backend C# | Flutter Dart | Vue TypeScript | Notes |
|------------|--------------|----------------|-------|
| `FavoriteProviderViewModel` | `FavoriteProviderDto` | N/A | Customer's favorite provider |
| `AddFavoriteProviderRequest` | N/A (direct API call) | N/A | Add favorite |

**Backend** (`src/UserManagement/.../CustomersController.cs`):
```csharp
public sealed record AddFavoriteProviderRequest(
    Guid ProviderId,
    string? Notes = null
);

public record FavoriteProviderViewModel {
    public Guid ProviderId { get; init; }
    public string ProviderName { get; init; }
    public string? ProviderType { get; init; }
    public string? LogoUrl { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public double? AverageRating { get; init; }
    public int? TotalReviews { get; init; }
    public DateTime AddedAt { get; init; }
    public string? Notes { get; init; }
}
```

**Flutter** (`lib/core/api/models/provider_models.dart`):
```dart
@JsonSerializable()
class FavoriteProviderDto {
  final String providerId;
  final String providerName;
  final String? providerType;
  final String? logoUrl;
  final String? city;
  final String? state;
  final double? averageRating;
  final int? totalReviews;
  final DateTime addedAt;
  final String? notes;
}
```

---

### Recently Visited Provider

| Backend C# | Flutter Dart | Vue TypeScript | Notes |
|------------|--------------|----------------|-------|
| `RecentlyVisitedProviderViewModel` | `RecentlyVisitedProviderDto` | N/A | Tracks provider views |
| `RecordProviderVisitRequest` | `RecordProviderVisitRequest` | N/A | Record a visit |

**Backend**:
```csharp
public sealed record RecordProviderVisitRequest(
    Guid ProviderId,
    string? ViewSource = null  // e.g., "search", "category", "favorites"
);

public record RecentlyVisitedProviderViewModel {
    public Guid ProviderId { get; init; }
    public string ProviderName { get; init; }
    public string? ProviderType { get; init; }
    public string? LogoUrl { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public double? AverageRating { get; init; }
    public int? TotalReviews { get; init; }
    public DateTime LastVisitedAt { get; init; }
    public int VisitCount { get; init; }
    public string? ViewSource { get; init; }
}
```

**Flutter** (`lib/core/api/models/provider_models.dart`):
```dart
@JsonSerializable()
class RecordProviderVisitRequest {
  final String providerId;
  final String? viewSource;
}

@JsonSerializable()
class RecentlyVisitedProviderDto {
  final String providerId;
  final String providerName;
  final String? providerType;
  final String? logoUrl;
  final String? city;
  final String? state;
  final double? averageRating;
  final int? totalReviews;
  final DateTime lastVisitedAt;
  final int visitCount;
  final String? viewSource;
}
```

---

### Notification Preferences

| Backend C# | Flutter Dart | Vue TypeScript | Notes |
|------------|--------------|----------------|-------|
| `UpdatePreferencesRequest` | N/A (direct API) | N/A | SMS/Email preferences |

**Backend** (`src/UserManagement/.../CustomersController.cs`):
```csharp
public sealed record UpdatePreferencesRequest(
    bool SmsEnabled,
    bool EmailEnabled,
    string ReminderTiming
);
```

**Flutter**: Would use direct API call via Dio/Retrofit

---

## Provider DTOs

### Provider Search

| Backend C# | Flutter Dart | Vue TypeScript | Notes |
|------------|--------------|----------------|-------|
| `SearchProvidersRequest` | `SearchProvidersRequest` | `IProviderSearchParams` | Search filters |
| `ProviderSearchResponse` | `ProviderDto` | `IProvider` | Search result |

**Backend** (`src/ServiceCatalog/.../Models/Requests/SearchProvidersRequest.cs`):
```csharp
public record SearchProvidersRequest {
    public string? Query { get; init; }
    public List<string>? CategoryIds { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public double? RadiusKm { get; init; }
    public int? MinRating { get; init; }
    public int? MaxPrice { get; init; }
    public bool? OpenNow { get; init; }
    public string? SortBy { get; init; }
    public string? SortOrder { get; init; }
    public int? PageNumber { get; init; }
    public int? PageSize { get; init; }
}

public record ProviderSearchResponse {
    public Guid Id { get; init; }
    public string BusinessName { get; init; }
    public string? Description { get; init; }
    public string Type { get; init; }
    public string Status { get; init; }
    public string? City { get; init; }
    public string? LogoUrl { get; init; }
    public string? ProfileImageUrl { get; init; }
    public bool AllowOnlineBooking { get; init; }
    public double? AverageRating { get; init; }
    public int ServiceCount { get; init; }
    // ... (see full schema in API_ENDPOINTS.md)
}
```

**Flutter** (`lib/core/api/models/provider_models.dart`):
```dart
@JsonSerializable()
class SearchProvidersRequest {
  final String? query;
  final List<String>? categoryIds;
  final double? latitude;
  final double? longitude;
  final double? radiusKm;
  final int? minRating;
  final int? maxPrice;
  final bool? openNow;
  final String? sortBy;
  final String? sortOrder;
  final int? pageNumber;
  final int? pageSize;
}

@JsonSerializable()
class ProviderDto {
  final String id;
  final String businessName;
  final String? description;
  final String? type;
  final String? status;
  final String? city;
  final String? state;
  final String? country;
  final String? logoUrl;
  final String? profileImageUrl;
  final bool? allowOnlineBooking;
  final bool? offersMobileServices;
  final double? averageRating;
  final int? totalReviews;
  final int? serviceCount;
  final int? yearsInBusiness;
  final bool? isVerified;
  final List<String>? tags;
  final String? registeredAt;
  final String? lastActiveAt;
  final String? hierarchyType;
  final bool? isIndependent;
  final String? parentProviderId;
  final String? parentProviderName;
  final int? staffProviderCount;
}
```

---

## Category DTOs

### Category

| Backend C# | Flutter Dart | Vue TypeScript | Notes |
|------------|--------------|----------------|-------|
| `CategoryWithCountViewModel` | `CategoryDto` | `ICategory` | Service category with provider count |
| `PopularCategoryDto` | `PopularCategoryDto` | `IPopularCategory` | Popular category |

**Backend** (`src/ServiceCatalog/.../Controllers/V1/CategoriesController.cs`):
```csharp
public record CategoryWithCountViewModel {
    public string Name { get; init; }
    public string Slug { get; init; }
    public string? Description { get; init; }
    public string? IconUrl { get; init; }
    public string Color { get; init; }
    public string? Gradient { get; init; }
    public int? ProviderCount { get; init; }
    public int? DisplayOrder { get; init; }
}
```

**Flutter** (`lib/core/api/models/category_models.dart`):
```dart
@JsonSerializable()
class CategoryDto {
  final String name;
  final String? description;
  final String? iconUrl;
  final String color;
  final String slug;
  final int? providerCount;
  final String? gradient;
  final int? displayOrder;
}

@JsonSerializable()
class PopularCategoryDto {
  final String name;
  final String slug;
  final String icon;
  final String gradient;
  final int providerCount;
  final String? description;
  final String? color;
  final int? displayOrder;
}
```

---

## Booking DTOs

### Create Booking

| Backend C# | Flutter Dart | Vue TypeScript | Notes |
|------------|--------------|----------------|-------|
| `CreateBookingRequest` | `CreateBookingRequest` | `ICreateBookingRequest` | New booking |
| `BookingResponse` | N/A (uses BookingDto) | `IBooking` | Created booking |

**Backend** (`src/ServiceCatalog/.../Models/Requests/CreateBookingRequest.cs`):
```csharp
public record CreateBookingRequest {
    public Guid ProviderId { get; init; }
    public Guid ServiceId { get; init; }
    public Guid StaffProviderId { get; init; }
    public DateTime StartTime { get; init; }
    public string? CustomerNotes { get; init; }
}

public record BookingResponse {
    public Guid Id { get; init; }
    public Guid CustomerId { get; init; }
    public Guid ProviderId { get; init; }
    public Guid ServiceId { get; init; }
    public Guid StaffProviderId { get; init; }
    public string Status { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public int DurationMinutes { get; init; }
    public decimal TotalPrice { get; init; }
    public string Currency { get; init; }
    public string PaymentStatus { get; init; }
    public DateTime CreatedAt { get; init; }
}
```

**Flutter** (`lib/core/api/models/booking_models.dart`):
```dart
@JsonSerializable()
class CreateBookingRequest {
  final String customerId;
  final String providerId;
  final String serviceId;
  final String staffProviderId;
  final DateTime startTime;
  final String? customerNotes;
}
```

---

### Booking DTO (Full)

| Backend C# | Flutter Dart | Vue TypeScript | Notes |
|------------|--------------|----------------|-------|
| `BookingDto` | `BookingDto` | `IBooking` | Complete booking with relations |

**Backend**:
```csharp
public record BookingDto {
    public Guid Id { get; init; }
    public Guid CustomerId { get; init; }
    public Guid ProviderId { get; init; }
    public Guid ServiceId { get; init; }
    public Guid StaffProviderId { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime? EndTime { get; init; }
    public BookingStatus Status { get; init; }
    public string? CustomerNotes { get; init; }
    public string? ProviderNotes { get; init; }
    public decimal? TotalAmount { get; init; }
    public decimal? DepositAmount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime? CancelledAt { get; init; }
    public string? CancellationReason { get; init; }
    // Related entities
    public ServiceDto? Service { get; init; }
    public ProviderSummaryDto? Provider { get; init; }
    public StaffDto? Staff { get; init; }
}
```

**Flutter**:
```dart
@JsonSerializable()
class BookingDto {
  final String id;
  final String customerId;
  final String providerId;
  final String serviceId;
  final String staffProviderId;
  final DateTime startTime;
  final DateTime? endTime;
  final BookingStatus status;
  final String? customerNotes;
  final String? providerNotes;
  final double? totalAmount;
  final double? depositAmount;
  final DateTime createdAt;
  final DateTime? updatedAt;
  final DateTime? cancelledAt;
  final String? cancellationReason;
  // Related entities
  final ServiceDto? service;
  final ProviderSummaryDto? provider;
  final StaffDto? staff;

  // Helper methods
  bool get isUpcoming => ...
  bool get isPast => ...
  bool get canCancel => ...
  bool get canReschedule => ...
}
```

---

### Customer Booking DTO (Simplified)

| Backend C# | Flutter Dart | Vue TypeScript | Notes |
|------------|--------------|----------------|-------|
| `CustomerBookingDto` | `CustomerBookingDto` | `ICustomerBooking` | For my-bookings endpoint |

**Backend**:
```csharp
public record CustomerBookingDto {
    public Guid Id { get; init; }
    public Guid ProviderId { get; init; }
    public string ProviderName { get; init; }
    public string? ProviderImageUrl { get; init; }
    public string ServiceName { get; init; }
    public int DurationMinutes { get; init; }
    public DateTime StartTime { get; init; }
    public BookingStatus Status { get; init; }
    public decimal? TotalAmount { get; init; }
    public string? StaffName { get; init; }
}
```

**Flutter**:
```dart
@JsonSerializable()
class CustomerBookingDto {
  final String id;
  final String providerId;
  final String providerName;
  final String? providerImageUrl;
  final String serviceName;
  final int durationMinutes;
  final DateTime startTime;
  final BookingStatus status;
  final double? totalAmount;
  final String? staffName;
}
```

---

### Booking Status Enum

| Backend C# | Flutter Dart | Vue TypeScript | Notes |
|------------|--------------|----------------|-------|
| `BookingStatus` (enum) | `BookingStatus` (enum) | `BookingStatus` (string literal type) | Status values |

**Backend**:
```csharp
public enum BookingStatus {
    Pending,
    Requested,
    Confirmed,
    InProgress,
    Completed,
    Cancelled,
    NoShow
}
```

**Flutter**:
```dart
enum BookingStatus {
  @JsonValue('Pending')
  pending,
  @JsonValue('Requested')
  requested,
  @JsonValue('Confirmed')
  confirmed,
  @JsonValue('InProgress')
  inProgress,
  @JsonValue('Completed')
  completed,
  @JsonValue('Cancelled')
  cancelled,
  @JsonValue('NoShow')
  noShow,
}
```

---

## Service DTOs

### Service DTO

| Backend C# | Flutter Dart | Vue TypeScript | Notes |
|------------|--------------|----------------|-------|
| `ServiceDto` | `ServiceDto` | `IService` | Service offering |

**Backend**:
```csharp
public record ServiceDto {
    public Guid Id { get; init; }
    public string Name { get; init; }
    public string? Description { get; init; }
    public int DurationMinutes { get; init; }
    public decimal Price { get; init; }
    public string? ImageUrl { get; init; }
}
```

**Flutter** (`lib/core/api/models/booking_models.dart`):
```dart
@JsonSerializable()
class ServiceDto {
  final String id;
  final String name;
  final String? description;
  final int durationMinutes;
  final double price;
  final String? imageUrl;
}
```

---

### Provider Summary DTO

| Backend C# | Flutter Dart | Vue TypeScript | Notes |
|------------|--------------|----------------|-------|
| `ProviderSummaryDto` | `ProviderSummaryDto` | `IProviderSummary` | Minimal provider info |

**Backend**:
```csharp
public record ProviderSummaryDto {
    public Guid Id { get; init; }
    public string BusinessName { get; init; }
    public string? LogoUrl { get; init; }
    public string? Address { get; init; }
    public double? Rating { get; init; }
}
```

**Flutter** (`lib/core/api/models/booking_models.dart`):
```dart
@JsonSerializable()
class ProviderSummaryDto {
  final String id;
  final String businessName;
  final String? logoUrl;
  final String? address;
  final double? rating;
}
```

---

### Staff DTO

| Backend C# | Flutter Dart | Vue TypeScript | Notes |
|------------|--------------|----------------|-------|
| `StaffDto` | `StaffDto` | `IStaff` | Staff member |

**Backend**:
```csharp
public record StaffDto {
    public Guid Id { get; init; }
    public string Name { get; init; }
    public string? ProfilePictureUrl { get; init; }
    public string? Specialization { get; init; }
}
```

**Flutter** (`lib/core/api/models/booking_models.dart`):
```dart
@JsonSerializable()
class StaffDto {
  final String id;
  final String name;
  final String? profilePictureUrl;
  final String? specialization;
}
```

---

## Common/Shared DTOs

### Paginated Response

| Backend C# | Flutter Dart | Vue TypeScript | Notes |
|------------|--------------|----------------|-------|
| `PagedResult<T>` | `PaginatedBookingsResponse` | `IPaginatedResponse<T>` | Generic pagination |

**Backend** (`src/Infrastructure/Booksy.Core.Application/DTOs/PagedResult.cs`):
```csharp
public class PagedResult<T> {
    public IReadOnlyList<T> Items { get; init; }
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
    public bool HasNextPage { get; init; }
    public bool HasPreviousPage { get; init; }
}
```

**Flutter** (`lib/core/api/models/booking_models.dart`):
```dart
@JsonSerializable()
class PaginatedBookingsResponse {
  final List<CustomerBookingDto> items;
  final int totalItems;      // Backend: TotalCount
  final int pageNumber;
  final int pageSize;
  final int totalPages;

  bool get hasNextPage => pageNumber < totalPages;
  bool get hasPreviousPage => pageNumber > 1;
}
```

---

## Type Conversion Notes

### Backend C# → Flutter Dart

| C# Type | Dart Type | Notes |
|---------|-----------|-------|
| `Guid` | `String` | Serialized as string in JSON |
| `DateTime` | `DateTime` | ISO 8601 format |
| `decimal` | `double` | JSON doesn't distinguish |
| `int` | `int` | Direct mapping |
| `bool` | `bool` | Direct mapping |
| `string?` | `String?` | Nullable string |
| `List<T>` | `List<T>` | Direct mapping |
| `Dictionary<K,V>` | `Map<K,V>` | Direct mapping |
| Enum | Enum with `@JsonValue` | PascalCase → camelCase |

### JSON Serialization Notes

**Backend (C#)**:
- Uses System.Text.Json or Newtonsoft.Json
- PascalCase property names by default
- Can configure camelCase via `JsonSerializerOptions`

**Flutter (Dart)**:
- Uses `json_serializable` package
- Generates `fromJson` and `toJson` methods
- Properties are camelCase by default
- `@JsonKey` for custom field names

Example:
```dart
@JsonSerializable()
class UserDto {
  @JsonKey(name: 'userId')  // If backend sends 'userId' instead of 'id'
  final String id;

  // Code generation creates:
  // factory UserDto.fromJson(Map<String, dynamic> json) => _$UserDtoFromJson(json);
  // Map<String, dynamic> toJson() => _$UserDtoToJson(this);
}
```

---

## Feature-Specific Mappings

### Authentication Flow

1. **Send OTP**: `SendVerificationCodeRequest` → `SendVerificationCodeResponse`
2. **Verify OTP**: `CompleteCustomerAuthRequest` → `CompleteCustomerAuthResponse`
   - Response includes: `UserDto` + `CustomerDto` + JWT tokens

### Booking Flow

1. **Search Providers**: `SearchProvidersRequest` → `PagedResult<ProviderSearchResponse>`
2. **Get Available Slots**: Query params → `GetAvailableSlotsResult`
3. **Create Booking**: `CreateBookingRequest` → `BookingResponse`
4. **Get My Bookings**: Query params → `PagedResult<CustomerBookingDto>`

### Recently Visited Feature

1. **View Provider**: Auto-triggers `RecordProviderVisitRequest`
2. **Get Recently Visited**: GET request → `List<RecentlyVisitedProviderDto>`

---

## Best Practices

### 1. **Keep DTOs in Sync**
- When changing backend DTOs, update Flutter models
- Run `flutter pub run build_runner build` after changes
- Update TypeScript interfaces if applicable

### 2. **Use Separate Models for Domain vs API**
- **Backend**: DTOs for API, Entities for domain
- **Flutter**: DTOs in `data` layer, Entities in `domain` layer

### 3. **Handle Nullable Fields**
- Backend uses `?` for nullable reference types
- Flutter uses `?` for optional properties
- Always check nullability when mapping

### 4. **Enums**
- Keep enum values consistent across platforms
- Use `@JsonValue` in Flutter for correct serialization
- Backend PascalCase → Flutter camelCase

### 5. **Date/Time**
- Always use ISO 8601 format in JSON
- Flutter: Use `DateTime.parse()` and `.toIso8601String()`
- Backend: Use `DateTime` with JSON serialization

---

## Quick Reference: File Locations

### Backend (C#)
- Request/Response Models: `src/*/Api/Models/Requests/` and `Models/Responses/`
- Domain Entities: `src/*/Domain/Aggregates/` and `Entities/`
- DTOs: `src/*/Application/DTOs/` or inline in controllers
- View Models: Query result models in `Application/Queries/*/`

### Flutter (Dart)
- Auth Models: `lib/features/auth/data/models/auth_models.dart`
- Core API Models: `lib/core/api/models/` (booking, category, provider)
- Domain Entities: `lib/features/*/domain/entities/`
- Generated Files: `*.g.dart` (created by build_runner)

### Vue (TypeScript) - **IF APPLICABLE**
- Types: `src/modules/*/types/`
- API Services: `src/modules/*/api/`

---

## Change Log

When adding or modifying DTOs:

1. **Update Backend**: Modify C# models, rebuild, test Swagger
2. **Update Flutter**: Modify Dart models, run `build_runner`, test API calls
3. **Update Vue** (if applicable): Modify TypeScript interfaces
4. **Update This Document**: Add mapping to relevant section
5. **Update API_ENDPOINTS.md**: Add/modify endpoint documentation

---

**For implementation details, see**:
- [API_ENDPOINTS.md](API_ENDPOINTS.md) - Complete API reference
- [CLAUDE.md](CLAUDE.md) - Project overview and architecture
- Backend Swagger: http://napstar.ir:5001/swagger, http://napstar.ir:5002/swagger
