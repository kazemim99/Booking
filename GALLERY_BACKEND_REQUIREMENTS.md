# Gallery Moderation Backend Requirements

## Overview
The admin panel now has gallery management UI that requires backend API support for content moderation. This document outlines the required backend changes.

## Current Backend State

### Existing Gallery Endpoints (ServiceCatalog API)
✅ `GET /api/v1/providers/{providerId}/gallery` - Get provider's gallery images
✅ `POST /api/v1/providers/{providerId}/gallery` - Upload images
✅ `PUT /api/v1/providers/{providerId}/gallery/{imageId}` - Update metadata
✅ `DELETE /api/v1/providers/{providerId}/gallery/{imageId}` - Delete image
✅ `PUT /api/v1/providers/{providerId}/gallery/reorder` - Reorder images
✅ `PUT /api/v1/providers/{providerId}/gallery/{imageId}/set-primary` - Set primary image

### Current GalleryImage Entity
```csharp
public sealed class GalleryImage : Entity<Guid>
{
    public ProviderId ProviderId { get; private set; }
    public string ImageUrl { get; private set; }
    public string ThumbnailUrl { get; private set; }
    public string MediumUrl { get; private set; }
    public int DisplayOrder { get; private set; }
    public string? Caption { get; private set; }
    public string? AltText { get; private set; }
    public DateTime UploadedAt { get; private set; }
    public bool IsActive { get; private set; }  // ← Currently used for active/inactive
    public bool IsPrimary { get; private set; }
}
```

## Required Backend Changes

### 1. Add ModerationStatus to GalleryImage Entity

**File:** `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Domain/Aggregates/ProviderAggregate/Entities/GalleryImage.cs`

```csharp
public sealed class GalleryImage : Entity<Guid>
{
    // ... existing properties ...

    public ModerationStatus Status { get; private set; }  // NEW
    public DateTime? ApprovedAt { get; private set; }    // NEW
    public DateTime? RejectedAt { get; private set; }    // NEW
    public string? RejectionReason { get; private set; } // NEW
    public Guid? ModeratedBy { get; private set; }       // NEW (Admin user ID)

    // Update Create method
    public static GalleryImage Create(...)
    {
        return new GalleryImage
        {
            // ... existing fields ...
            Status = ModerationStatus.Pending,  // Default to Pending
        };
    }

    // NEW methods
    public void Approve(Guid moderatorId)
    {
        Status = ModerationStatus.Approved;
        ApprovedAt = DateTime.UtcNow;
        ModeratedBy = moderatorId;
        IsActive = true;
    }

    public void Reject(Guid moderatorId, string? reason = null)
    {
        Status = ModerationStatus.Rejected;
        RejectedAt = DateTime.UtcNow;
        RejectionReason = reason;
        ModeratedBy = moderatorId;
        IsActive = false;
    }
}
```

### 2. Create ModerationStatus Enum

**File:** `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Domain/Enums/ModerationStatus.cs` (NEW)

```csharp
namespace Booksy.ServiceCatalog.Domain.Enums;

public enum ModerationStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}
```

### 3. Add Database Migration

```bash
cd src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure
dotnet ef migrations add AddGalleryImageModeration -o Persistence/Migrations
```

Expected migration:
```csharp
migrationBuilder.AddColumn<int>(
    name: "Status",
    table: "GalleryImages",
    type: "integer",
    nullable: false,
    defaultValue: 0);

migrationBuilder.AddColumn<DateTime>(
    name: "ApprovedAt",
    table: "GalleryImages",
    type: "timestamp with time zone",
    nullable: true);

migrationBuilder.AddColumn<DateTime>(
    name: "RejectedAt",
    table: "GalleryImages",
    type: "timestamp with time zone",
    nullable: true);

migrationBuilder.AddColumn<string>(
    name: "RejectionReason",
    table: "GalleryImages",
    type: "text",
    nullable: true);

migrationBuilder.AddColumn<Guid>(
    name: "ModeratedBy",
    table: "GalleryImages",
    type: "uuid",
    nullable: true);
```

### 4. Update GalleryImageDto

**File:** `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/DTOs/Provider/GalleryImageDto.cs`

```csharp
public class GalleryImageDto
{
    // ... existing properties ...

    public string Status { get; set; }  // NEW: "Pending", "Approved", "Rejected"
    public DateTime? ApprovedAt { get; set; }  // NEW
    public DateTime? RejectedAt { get; set; }  // NEW
}
```

### 5. Create Admin Gallery Query

**File:** `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Queries/Admin/GetAllGalleryImages/GetAllGalleryImagesQuery.cs` (NEW)

```csharp
public record GetAllGalleryImagesQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? Status = null,
    string? Search = null
) : IRequest<PagedResult<GalleryImageDto>>;
```

**File:** `...GetAllGalleryImagesQueryHandler.cs` (NEW)

```csharp
public class GetAllGalleryImagesQueryHandler
    : IRequestHandler<GetAllGalleryImagesQuery, PagedResult<GalleryImageDto>>
{
    private readonly IProviderReadRepository _repository;

    public async Task<PagedResult<GalleryImageDto>> Handle(
        GetAllGalleryImagesQuery request,
        CancellationToken cancellationToken)
    {
        // Query all gallery images across providers with filtering
        var query = _repository.GetAllGalleryImages();

        if (!string.IsNullOrEmpty(request.Status))
        {
            var status = Enum.Parse<ModerationStatus>(request.Status);
            query = query.Where(img => img.Status == status);
        }

        // ... implement pagination and mapping
    }
}
```

### 6. Create Approve/Reject Commands

**File:** `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Commands/Admin/ApproveGalleryImage/ApproveGalleryImageCommand.cs` (NEW)

```csharp
public record ApproveGalleryImageCommand(
    Guid ProviderId,
    Guid ImageId
) : IRequest;
```

**File:** `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Commands/Admin/RejectGalleryImage/RejectGalleryImageCommand.cs` (NEW)

```csharp
public record RejectGalleryImageCommand(
    Guid ProviderId,
    Guid ImageId,
    string? Reason
) : IRequest;
```

### 7. Add Admin Controller or Extend ProvidersController

**Option A: Extend ProvidersController**

**File:** `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/Controllers/V1/ProvidersController.cs`

Add these endpoints to the existing controller:

```csharp
/// <summary>
/// Approve a gallery image (Admin only)
/// </summary>
[HttpPut("{providerId}/gallery/{imageId}/approve")]
[Authorize(Roles = "Admin")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
public async Task<IActionResult> ApproveGalleryImage(
    [FromRoute] Guid providerId,
    [FromRoute] Guid imageId,
    CancellationToken cancellationToken = default)
{
    var command = new ApproveGalleryImageCommand(providerId, imageId);
    await _mediator.Send(command, cancellationToken);
    return NoContent();
}

/// <summary>
/// Reject a gallery image (Admin only)
/// </summary>
[HttpPut("{providerId}/gallery/{imageId}/reject")]
[Authorize(Roles = "Admin")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
public async Task<IActionResult> RejectGalleryImage(
    [FromRoute] Guid providerId,
    [FromRoute] Guid imageId,
    [FromBody] RejectGalleryImageRequest request,
    CancellationToken cancellationToken = default)
{
    var command = new RejectGalleryImageCommand(providerId, imageId, request.Reason);
    await _mediator.Send(command, cancellationToken);
    return NoContent();
}
```

**Option B: Create Dedicated AdminController**

**File:** `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/Controllers/V1/AdminController.cs` (NEW)

```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    /// <summary>
    /// Get all gallery images across providers for moderation
    /// </summary>
    [HttpGet("gallery")]
    public async Task<IActionResult> GetAllGalleryImages(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetAllGalleryImagesQuery(pageNumber, pageSize, status, search);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
```

### 8. Update GalleryImageResponse

**File:** `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/Models/Responses/GalleryImageResponse.cs`

```csharp
public class GalleryImageResponse
{
    // ... existing properties ...

    public string Status { get; set; }  // NEW
    public DateTime? ApprovedAt { get; set; }  // NEW
    public DateTime? RejectedAt { get; set; }  // NEW
}
```

## Implementation Steps

1. ✅ **Database Schema** - Add Status field and moderation tracking to GalleryImage
2. ✅ **Domain Model** - Update GalleryImage entity with Approve/Reject methods
3. ✅ **Application Layer** - Create commands and queries for moderation
4. ✅ **API Layer** - Add approve/reject endpoints and admin gallery listing
5. ✅ **Authorization** - Ensure endpoints are protected with `[Authorize(Roles = "Admin")]`
6. ✅ **Migration** - Run database migration
7. ✅ **Testing** - Add integration tests for moderation workflow

## Frontend Expectations

The admin panel expects these endpoints:

```
GET    /api/v1/admin/gallery?pageNumber=1&pageSize=20&status=Pending
GET    /api/v1/providers/{providerId}/gallery
PUT    /api/v1/providers/{providerId}/gallery/{imageId}/approve
PUT    /api/v1/providers/{providerId}/gallery/{imageId}/reject
DELETE /api/v1/providers/{providerId}/gallery/{imageId}
PUT    /api/v1/providers/{providerId}/gallery/{imageId}
```

### Expected Response Format

```json
{
  "items": [
    {
      "id": "uuid",
      "thumbnailUrl": "string",
      "mediumUrl": "string",
      "originalUrl": "string",
      "caption": "string",
      "altText": "string",
      "displayOrder": 0,
      "uploadedAt": "2024-01-01T00:00:00Z",
      "isActive": true,
      "isPrimary": false,
      "status": "Pending|Approved|Rejected",
      "approvedAt": "2024-01-01T00:00:00Z",
      "rejectedAt": null
    }
  ],
  "totalCount": 100,
  "pageNumber": 1,
  "pageSize": 20
}
```

## Current Workaround

The frontend currently:
- Maps `isActive: true` → `status: "Approved"`
- Maps `isActive: false` → `status: "Rejected"`
- Shows console warnings for approve/reject actions
- Returns empty array for `/admin/gallery` endpoint

This temporary mapping allows the UI to work but doesn't support the full moderation workflow.

## Benefits of Full Implementation

1. **Content Moderation** - Prevent inappropriate images from appearing on platform
2. **Audit Trail** - Track who approved/rejected images and when
3. **Provider Feedback** - Show rejection reasons to providers
4. **Admin Dashboard** - Centralized view of all pending images
5. **Compliance** - Meet content policy requirements
6. **Auto-publish Control** - Images start as Pending instead of immediately Approved

## Testing Checklist

- [ ] Upload image → Status is Pending
- [ ] Admin can see pending images in /admin/gallery
- [ ] Admin can approve image → Status changes to Approved, ApprovedAt is set
- [ ] Admin can reject image → Status changes to Rejected, RejectionReason is saved
- [ ] Deleted images are removed from database
- [ ] Only Admin role can access moderation endpoints
- [ ] Provider can see their own gallery with status indicators
- [ ] Approved images appear in public provider profile
- [ ] Rejected/Pending images don't appear in public view
