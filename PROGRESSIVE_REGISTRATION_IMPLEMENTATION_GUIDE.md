# Progressive Registration Implementation Guide

## Overview
This guide implements **Option 1: Create Provider Early** pattern where the provider is created after Step 3 and updated incrementally through the registration flow.

---

## ✅ COMPLETED: Backend Domain Changes

### 1. Provider Entity Updates
**File**: `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Domain/Aggregates/ProviderAggregate/Provider.cs`

Added properties:
```csharp
// Registration Progress Tracking
public int RegistrationStep { get; private set; }
public bool IsRegistrationComplete { get; private set; }
```

Added methods:
```csharp
// Factory for draft provider
public static Provider CreateDraft(
    UserId ownerId,
    string businessName,
    string description,
    ProviderType type,
    ContactInfo contactInfo,
    BusinessAddress address,
    int registrationStep = 3)

// Progress tracking
public void UpdateRegistrationStep(int step)
public void CompleteRegistration()
```

### 2. EF Core Configuration
**File**: `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/Persistence/Configurations/ProviderConfiguration.cs`

Added:
```csharp
// Registration Progress
builder.Property(p => p.RegistrationStep)
    .IsRequired()
    .HasDefaultValue(1);

builder.Property(p => p.IsRegistrationComplete)
    .IsRequired()
    .HasDefaultValue(false);
```

---

## TODO: Backend Application Layer

### 3. Create Progressive Registration Commands

**File**: `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Commands/Provider/CreateProviderDraft/CreateProviderDraftCommand.cs`

```csharp
public sealed record CreateProviderDraftCommand(
    string BusinessName,
    string BusinessDescription,
    string Category, // Maps to ProviderType
    string PhoneNumber,
    string Email,
    string AddressLine1,
    string City,
    string Province,
    string PostalCode,
    decimal Latitude,
    decimal Longitude
) : ICommand<CreateProviderDraftResult>;

public sealed record CreateProviderDraftResult(
    Guid ProviderId,
    int RegistrationStep,
    string Message);
```

**Handler**: `Create ProviderDraftCommandHandler.cs`

```csharp
public sealed class CreateProviderDraftCommandHandler
    : ICommandHandler<CreateProviderDraftCommand, CreateProviderDraftResult>
{
    public async Task<Result<CreateProviderDraftResult>> Handle(
        CreateProviderDraftCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Get current user ID from context
        var userId = UserId.From(_currentUserService.UserId);

        // 2. Check if user already has a draft provider
        var existingProvider = await _providerRepository
            .GetDraftProviderByOwnerIdAsync(userId, cancellationToken);

        if (existingProvider != null)
        {
            // Return existing draft
            return new CreateProviderDraftResult(
                existingProvider.Id.Value,
                existingProvider.RegistrationStep,
                "Draft provider already exists");
        }

        // 3. Map category string to ProviderType enum
        var providerType = MapCategoryToProviderType(request.Category);

        // 4. Create value objects
        var contactInfo = ContactInfo.Create(
            request.PhoneNumber,
            request.Email);

        var address = BusinessAddress.Create(
            request.AddressLine1,
            null, // addressLine2
            request.City,
            request.Province,
            request.PostalCode,
            "IR"); // country code

        // 5. Create draft provider
        var provider = Provider.CreateDraft(
            userId,
            request.BusinessName,
            request.BusinessDescription,
            providerType,
            contactInfo,
            address,
            registrationStep: 3);

        // 6. Set location
        provider.UpdateAddress(address);

        // 7. Save
        _providerRepository.Add(provider);
        await _unitOfWork.CommitAsync(cancellationToken);

        return new CreateProviderDraftResult(
            provider.Id.Value,
            provider.RegistrationStep,
            "Draft provider created successfully");
    }
}
```

### 4. Update Step Commands

**File**: `UpdateProviderServicesCommand.cs`
```csharp
public sealed record UpdateProviderServicesCommand(
    Guid ProviderId,
    List<ServiceDto> Services
) : ICommand;

// Handler updates services and RegistrationStep to 4
```

**File**: `UpdateProviderStaffCommand.cs`
```csharp
public sealed record UpdateProviderStaffCommand(
    Guid ProviderId,
    List<StaffDto> StaffMembers
) : ICommand;

// Handler updates staff and RegistrationStep to 5
```

**File**: `UpdateProviderBusinessHoursCommand.cs`
```csharp
public sealed record UpdateProviderBusinessHoursCommand(
    Guid ProviderId,
    List<BusinessHourDto> BusinessHours
) : ICommand;

// Handler updates hours and RegistrationStep to 6
```

**File**: `CompleteProviderRegistrationCommand.cs`
```csharp
public sealed record CompleteProviderRegistrationCommand(
    Guid ProviderId,
    string? FeedbackText
) : ICommand;

// Handler calls provider.CompleteRegistration()
// Changes status to PendingVerification
```

### 5. Add Repository Methods

**File**: `IProviderRepository.cs`
```csharp
Task<Provider?> GetDraftProviderByOwnerIdAsync(
    UserId ownerId,
    CancellationToken cancellationToken = default);

Task<Provider?> GetByOwnerIdWithStatusAsync(
    UserId ownerId,
    ProviderStatus status,
    CancellationToken cancellationToken = default);
```

**Implementation**:
```csharp
public async Task<Provider?> GetDraftProviderByOwnerIdAsync(
    UserId ownerId,
    CancellationToken cancellationToken = default)
{
    return await _context.Providers
        .Include(p => p.Staff)
        .Include(p => p.Services)
        .Include(p => p.BusinessHours)
        .FirstOrDefaultAsync(
            p => p.OwnerId == ownerId &&
                 p.Status == ProviderStatus.Drafted,
            cancellationToken);
}
```

### 6. Add Controller Endpoints

**File**: `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/Controllers/V1/ProvidersController.cs`

```csharp
/// <summary>
/// Create draft provider (Step 3 of registration)
/// </summary>
[HttpPost("draft")]
[Authorize]
public async Task<ActionResult<ApiResponse<CreateProviderDraftResult>>> CreateDraft(
    [FromBody] CreateProviderDraftRequest request)
{
    var command = new CreateProviderDraftCommand(
        request.BusinessName,
        request.BusinessDescription,
        request.Category,
        request.PhoneNumber,
        request.Email,
        request.AddressLine1,
        request.City,
        request.Province,
        request.PostalCode,
        request.Latitude,
        request.Longitude);

    var result = await _sender.Send(command);

    return result.Match(
        success => Ok(ApiResponse<CreateProviderDraftResult>.Success(success)),
        error => BadRequest(ApiResponse<CreateProviderDraftResult>.Failure(error)));
}

/// <summary>
/// Get current user's draft provider
/// </summary>
[HttpGet("draft")]
[Authorize]
public async Task<ActionResult<ApiResponse<ProviderDto>>> GetDraft()
{
    var query = new GetDraftProviderQuery();
    var result = await _sender.Send(query);

    return result.Match(
        success => Ok(ApiResponse<ProviderDto>.Success(success)),
        error => NotFound(ApiResponse<ProviderDto>.Failure(error)));
}

/// <summary>
/// Update provider services (Step 4)
/// </summary>
[HttpPut("{providerId}/services")]
[Authorize]
public async Task<ActionResult<ApiResponse<object>>> UpdateServices(
    Guid providerId,
    [FromBody] UpdateServicesRequest request)
{
    var command = new UpdateProviderServicesCommand(providerId, request.Services);
    var result = await _sender.Send(command);

    return result.Match(
        _ => Ok(ApiResponse<object>.Success("Services updated successfully")),
        error => BadRequest(ApiResponse<object>.Failure(error)));
}

/// <summary>
/// Update provider staff (Step 5)
/// </summary>
[HttpPut("{providerId}/staff")]
[Authorize]
public async Task<ActionResult<ApiResponse<object>>> UpdateStaff(
    Guid providerId,
    [FromBody] UpdateStaffRequest request)
{
    var command = new UpdateProviderStaffCommand(providerId, request.StaffMembers);
    var result = await _sender.Send(command);

    return result.Match(
        _ => Ok(ApiResponse<object>.Success("Staff updated successfully")),
        error => BadRequest(ApiResponse<object>.Failure(error)));
}

/// <summary>
/// Update provider business hours (Step 6)
/// </summary>
[HttpPut("{providerId}/business-hours")]
[Authorize]
public async Task<ActionResult<ApiResponse<object>>> UpdateBusinessHours(
    Guid providerId,
    [FromBody] UpdateBusinessHoursRequest request)
{
    var command = new UpdateProviderBusinessHoursCommand(providerId, request.BusinessHours);
    var result = await _sender.Send(command);

    return result.Match(
        _ => Ok(ApiResponse<object>.Success("Business hours updated successfully")),
        error => BadRequest(ApiResponse<object>.Failure(error)));
}

/// <summary>
/// Complete registration and submit for approval (Step 8)
/// </summary>
[HttpPost("{providerId}/complete")]
[Authorize]
public async Task<ActionResult<ApiResponse<object>>> CompleteRegistration(
    Guid providerId,
    [FromBody] CompleteRegistrationRequest request)
{
    var command = new CompleteProviderRegistrationCommand(providerId, request.FeedbackText);
    var result = await _sender.Send(command);

    return result.Match(
        _ => Ok(ApiResponse<object>.Success("Registration completed successfully")),
        error => BadRequest(ApiResponse<object>.Failure(error)));
}
```

### 7. Create Migration

```bash
# Stop the running API first
cd src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api
dotnet ef migrations add AddProviderRegistrationProgress \
    --context ServiceCatalogDbContext \
    --project ../Booksy.ServiceCatalog.Infrastructure/Booksy.ServiceCatalog.Infrastructure.csproj \
    --output-dir Migrations

# Apply migration
dotnet ef database update --context ServiceCatalogDbContext
```

---

## TODO: Frontend Implementation

### 8. Update Frontend Service

**File**: `booksy-frontend/src/modules/provider/services/provider.service.ts`

```typescript
// Add new methods
async createDraftProvider(data: CreateDraftRequest): Promise<CreateDraftResponse> {
  const response = await serviceCategoryClient.post<ApiResponse<CreateDraftResponse>>(
    '/api/v1/providers/draft',
    data
  )
  return response.data.data
}

async getDraftProvider(): Promise<ProviderDto | null> {
  try {
    const response = await serviceCategoryClient.get<ApiResponse<ProviderDto>>(
      '/api/v1/providers/draft'
    )
    return response.data.data
  } catch (error: any) {
    if (error.response?.status === 404) {
      return null
    }
    throw error
  }
}

async updateProviderServices(providerId: string, services: Service[]): Promise<void> {
  await serviceCategoryClient.put(
    `/api/v1/providers/${providerId}/services`,
    { services }
  )
}

async updateProviderStaff(providerId: string, staffMembers: TeamMember[]): Promise<void> {
  await serviceCategoryClient.put(
    `/api/v1/providers/${providerId}/staff`,
    { staffMembers }
  )
}

async updateProviderBusinessHours(providerId: string, businessHours: DayHours[]): Promise<void> {
  await serviceCategoryClient.put(
    `/api/v1/providers/${providerId}/business-hours`,
    { businessHours }
  )
}

async completeRegistration(providerId: string, feedbackText?: string): Promise<void> {
  await serviceCategoryClient.post(
    `/api/v1/providers/${providerId}/complete`,
    { feedbackText }
  )
}
```

### 9. Refactor useProviderRegistration Composable

**File**: `booksy-frontend/src/modules/provider/composables/useProviderRegistration.ts`

Key changes:
```typescript
// Add provider ID to state
const registrationState = ref<RegistrationState>({
  currentStep: 1,
  providerId: null, // NEW: Store provider ID once created
  data: {
    // ... existing fields
  },
  isLoading: false,
  error: null,
  isDirty: false,
})

// Load draft on initialize
const initialize = async () => {
  try {
    const draft = await providerService.getDraftProvider()

    if (draft) {
      // Resume from saved draft
      registrationState.value.providerId = draft.id
      registrationState.value.currentStep = draft.registrationStep
      registrationState.value.data = mapDraftToRegistrationData(draft)
    } else {
      // New registration
      registrationState.value.providerId = null
      registrationState.value.currentStep = 1
    }
  } catch (error) {
    console.error('Failed to load draft:', error)
  }
}

// Create provider at Step 3
const handleStepCompletion = async (step: number) => {
  if (step === 3 && !registrationState.value.providerId) {
    // Create draft provider after Step 3
    const result = await providerService.createDraftProvider({
      businessName: registrationState.value.data.businessInfo.businessName!,
      businessDescription: registrationState.value.data.businessInfo.description!,
      category: registrationState.value.data.categoryId!,
      phoneNumber: registrationState.value.data.businessInfo.phoneNumber!,
      email: registrationState.value.data.businessInfo.email!,
      addressLine1: registrationState.value.data.address.addressLine1!,
      city: registrationState.value.data.address.city!,
      province: registrationState.value.data.address.province!,
      postalCode: registrationState.value.data.address.postalCode!,
      latitude: registrationState.value.data.location.latitude!,
      longitude: registrationState.value.data.location.longitude!,
    })

    registrationState.value.providerId = result.providerId
  }

  // Update specific step data
  if (registrationState.value.providerId) {
    switch (step) {
      case 4:
        await providerService.updateProviderServices(
          registrationState.value.providerId,
          registrationState.value.data.services
        )
        break
      case 5:
        await providerService.updateProviderStaff(
          registrationState.value.providerId,
          registrationState.value.data.teamMembers
        )
        break
      case 6:
        await providerService.updateProviderBusinessHours(
          registrationState.value.providerId,
          registrationState.value.data.businessHours
        )
        break
    }
  }
}
```

### 10. Update Registration Flow

**File**: `booksy-frontend/src/modules/provider/views/registration/ProviderRegistrationFlow.vue`

```typescript
const handleNext = async () => {
  const canProceed = canProceedToNextStep()

  if (canProceed) {
    try {
      // Save progress for current step
      await handleStepCompletion(currentStep.value)

      // Move to next step
      nextStep()
    } catch (error: any) {
      toastService.error(error.message || 'Failed to save progress')
    }
  } else {
    toastService.error('Please complete all required fields')
  }
}
```

### 11. Add Route Guard

**File**: `booksy-frontend/src/core/router/guards/provider-registration.guard.ts`

```typescript
export const providerRegistrationGuard = async (to: RouteLocationNormalized) => {
  const authStore = useAuthStore()
  const providerStore = useProviderStore()

  if (!authStore.isAuthenticated) {
    return { name: 'login' }
  }

  // Check if user has a provider
  if (!providerStore.currentProvider) {
    await providerStore.fetchCurrentProvider()
  }

  const provider = providerStore.currentProvider

  if (!provider) {
    // No provider - allow registration
    return true
  }

  // Check provider status
  if (provider.status === 'Drafted' && !provider.isRegistrationComplete) {
    // Has incomplete registration - allow to continue
    return true
  }

  if (provider.status === 'PendingVerification' || provider.status === 'Active') {
    // Already registered - redirect to dashboard
    return { name: 'provider-dashboard' }
  }

  return true
}
```

---

## TODO: Integration Tests

**File**: `tests/Booksy.ServiceCatalog.IntegrationTests/ProgressiveRegistrationTests.cs`

```csharp
public class ProgressiveRegistrationTests : IntegrationTestBase
{
    [Fact]
    public async Task Should_CreateDraftProvider_WhenStep3Completed()
    {
        // Arrange
        var userId = await CreateTestUserAsync();
        var command = new CreateProviderDraftCommand(
            "Test Salon",
            "Beautiful salon",
            "BeautySalon",
            "+989123456789",
            "test@example.com",
            "123 Main St",
            "Tehran",
            "Tehran",
            "1234567890",
            35.6892m,
            51.3890m);

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.RegistrationStep.Should().Be(3);

        var provider = await DbContext.Providers
            .FirstOrDefaultAsync(p => p.Id == ProviderId.From(result.Value.ProviderId));

        provider.Should().NotBeNull();
        provider!.Status.Should().Be(ProviderStatus.Drafted);
        provider.IsRegistrationComplete.Should().BeFalse();
    }

    [Fact]
    public async Task Should_UpdateServices_AndIncrementStep()
    {
        // Arrange
        var provider = await CreateDraftProviderAsync();
        var services = new List<ServiceDto>
        {
            new("Haircut", "Basic haircut", 50000, 30)
        };
        var command = new UpdateProviderServicesCommand(provider.Id.Value, services);

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updated = await DbContext.Providers.FindAsync(provider.Id);
        updated!.RegistrationStep.Should().Be(4);
        updated.Services.Should().HaveCount(1);
    }

    [Fact]
    public async Task Should_CompleteRegistration_AndChangeStatus()
    {
        // Arrange
        var provider = await CreateFullyFilledProviderAsync();
        var command = new CompleteProviderRegistrationCommand(
            provider.Id.Value,
            "Great experience!");

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var completed = await DbContext.Providers.FindAsync(provider.Id);
        completed!.Status.Should().Be(ProviderStatus.PendingVerification);
        completed.IsRegistrationComplete.Should().BeTrue();
        completed.RegistrationStep.Should().Be(9);
    }

    [Fact]
    public async Task Should_ResumeDraftProvider_WhenUserReturns()
    {
        // Arrange
        var provider = await CreateDraftProviderAsync();
        provider.UpdateRegistrationStep(5);
        await DbContext.SaveChangesAsync();

        // Act
        var query = new GetDraftProviderQuery();
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.RegistrationStep.Should().Be(5);
        result.Value.Status.Should().Be("Drafted");
    }

    [Fact]
    public async Task Should_AllowGalleryUpload_AfterProviderCreated()
    {
        // Arrange
        var provider = await CreateDraftProviderAsync();
        var images = CreateTestImages(3);
        var command = new UploadGalleryImagesCommand(provider.Id.Value, images);

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);

        var gallery = await DbContext.GalleryImages
            .Where(g => g.ProviderId == provider.Id)
            .ToListAsync();

        gallery.Should().HaveCount(3);
    }
}
```

---

## Summary

This implementation:

1. ✅ **Domain Updated**: Provider entity tracks registration progress
2. ✅ **EF Configuration**: Database schema ready for new fields
3. 🔄 **Commands**: Need to create progressive registration commands
4. 🔄 **API Endpoints**: Need to add draft/update endpoints
5. 🔄 **Frontend Service**: Need to add new API calls
6. 🔄 **Frontend Composable**: Need to refactor to create provider at Step 3
7. 🔄 **Route Guard**: Need to implement resume logic
8. 🔄 **Tests**: Need comprehensive integration tests

## Benefits

- ✅ Provider exists after Step 3
- ✅ Gallery uploads work immediately (provider ID available)
- ✅ User can leave and resume anytime
- ✅ No data loss
- ✅ Better error handling per step
- ✅ Focused, incremental updates
