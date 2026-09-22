# Booksy API Endpoints Documentation

Complete reference for all API endpoints across the Booksy platform. All endpoints are served by a single host (`booksy-api`) on `:5000`. Base URL: `http://napstar.ir/api`

**Last Updated**: 2026-01-05

---

## Table of Contents

- [UserManagement Endpoints](#usermanagement-endpoints)
  - [Authentication](#authentication)
  - [Customer Management](#customer-management)
- [ServiceCatalog Endpoints](#servicecatalog-endpoints)
  - [Categories](#categories)
  - [Providers](#providers)
  - [Bookings](#bookings)
  - [Reviews](#reviews)
  - [Review Moderation (Admin)](#review-moderation-admin)
- [Host & Routing (Port 5000)](#host--routing-port-5000)

---

## UserManagement Endpoints

> Served by the single `booksy-api` host on `:5000` under `/api/v1/...`.

### Authentication

#### Send Verification Code
```http
POST /api/v1/Auth/send-verification-code
```
**Auth**: None (AllowAnonymous)
**Rate Limit**: phone-verification
**Request**:
```json
{
  "phoneNumber": "string",
  "countryCode": "+98"  // Default: +98
}
```
**Response** (200 OK):
```json
{
  "verificationId": "string",
  "maskedPhoneNumber": "string",
  "expiresAt": "string",
  "maxAttempts": number,
  "message": "string"
}
```

#### Complete Customer Authentication
```http
POST /api/v1/Auth/customer/complete-authentication
```
**Auth**: None (AllowAnonymous)
**Rate Limit**: code-verification
**Request**:
```json
{
  "phoneNumber": "string",
  "code": "string",
  "firstName": "string?",
  "lastName": "string?",
  "email": "string?"
}
```
**Response** (200 OK):
```json
{
  "accessToken": "string",
  "refreshToken": "string",
  "userId": "string (guid)",
  "customerId": "string (guid)",
  "user": {
    "id": "string (guid)",
    "phoneNumber": "string",
    "email": "string?",
    "firstName": "string?",
    "lastName": "string?",
    "profilePictureUrl": "string?",
    "emailVerified": boolean,
    "phoneVerified": boolean,
    "createdAt": "datetime",
    "updatedAt": "datetime?"
  },
  "customer": {
    "id": "string (guid)",
    "userId": "string (guid)",
    "preferredLanguage": "string?",
    "favoriteProviders": ["string"]?,
    "bookingCount": number,
    "createdAt": "datetime",
    "updatedAt": "datetime?"
  },
  "expiresIn": number  // seconds
}
```

#### Complete Provider Authentication
```http
POST /api/v1/Auth/provider/complete-authentication
```
**Auth**: None (AllowAnonymous)
**Rate Limit**: code-verification
**Request**:
```json
{
  "phoneNumber": "string",
  "code": "string",
  "firstName": "string?",
  "lastName": "string?",
  "email": "string?"
}
```
**Response** (200 OK):
```json
{
  "accessToken": "string",
  "refreshToken": "string",
  "userId": "string (guid)",
  "providerId": "string (guid)",
  "isNewProvider": boolean,
  "expiresIn": number
}
```

#### Generate Token (Cross-Context)
```http
POST /api/v1/Auth/generate-token
```
**Auth**: Required
**Request**:
```json
{
  "userId": "string (guid)",
  "additionalClaims": {
    "provider_id": "string?",
    "provider_status": "string?"
  }
}
```
**Response** (200 OK):
```json
{
  "accessToken": "string",
  "refreshToken": "string?",
  "expiresIn": number,
  "tokenType": "Bearer"
}
```

---

### Customer Management

#### Register Customer
```http
POST /api/v1/Customers/register
```
**Auth**: None (AllowAnonymous)
**Rate Limit**: registration
**Response**: 201 Created

#### Get Customer By ID
```http
GET /api/v1/Customers/{id}
```
**Auth**: Required (Roles: Customer, Admin)
**Response**: 200 OK with CustomerDetailsViewModel

#### Update Customer Profile
```http
PUT /api/v1/Customers/{id}
```
**Auth**: Required (Roles: Customer)
**Request**: UpdateCustomerProfileCommand

#### Get Customer Favorites
```http
GET /api/v1/Customers/{id}/favorites
```
**Auth**: Required (Roles: Customer, Admin)
**Response**: List<FavoriteProviderViewModel>

#### Add Favorite Provider
```http
POST /api/v1/Customers/{id}/favorites
```
**Auth**: Required (Roles: Customer)
**Request**:
```json
{
  "providerId": "string (guid)",
  "notes": "string?"
}
```

#### Remove Favorite Provider
```http
DELETE /api/v1/Customers/{id}/favorites/{providerId}
```
**Auth**: Required (Roles: Customer)

#### Get Customer Profile
```http
GET /api/v1/Customers/{id}/profile
```
**Auth**: Required (Roles: Customer, Admin)
**Response**: CustomerProfileViewModel with notification preferences

#### Get Upcoming Bookings
```http
GET /api/v1/Customers/{id}/bookings/upcoming?limit=5
```
**Auth**: Required (Roles: Customer, Admin)
**Query Params**:
- `limit` (int): Number of bookings to retrieve (default: 5)

#### Get Booking History
```http
GET /api/v1/Customers/{id}/bookings/history?page=1&pageSize=20
```
**Auth**: Required (Roles: Customer, Admin)
**Query Params**:
- `page` (int): Page number (default: 1)
- `pageSize` (int): Page size (default: 20)
**Response**: BookingHistoryResult (paginated)

#### Update Notification Preferences
```http
PATCH /api/v1/Customers/{id}/preferences
```
**Auth**: Required (Roles: Customer)
**Request**:
```json
{
  "smsEnabled": boolean,
  "emailEnabled": boolean,
  "reminderTiming": "string"
}
```

#### Get Recently Visited Providers
```http
GET /api/v1/Customers/{id}/recently-visited?limit=20
```
**Auth**: Required (Roles: Customer, Admin)
**Query Params**:
- `limit` (int): Number of recently visited providers (default: 20, max: 50)
**Response**:
```json
[
  {
    "providerId": "string (guid)",
    "providerName": "string",
    "providerType": "string?",
    "logoUrl": "string?",
    "city": "string?",
    "state": "string?",
    "averageRating": number?,
    "totalReviews": number?,
    "lastVisitedAt": "datetime",
    "visitCount": number,
    "viewSource": "string?"
  }
]
```

#### Record Provider Visit
```http
POST /api/v1/Customers/{id}/recently-visited
```
**Auth**: Required (Roles: Customer)
**Request**:
```json
{
  "providerId": "string (guid)",
  "viewSource": "string?"  // e.g., "search", "category", "favorites"
}
```

#### Deactivate Customer
```http
DELETE /api/v1/Customers/{id}
```
**Auth**: Required (Roles: Customer, Admin)
**Note**: Not yet implemented (returns placeholder message)

---

## ServiceCatalog Endpoints

> Served by the single `booksy-api` host on `:5000` under `/api/v1/...`.

### Categories

#### Get All Categories
```http
GET /api/v1/Categories?limit=25&onlyPopular=false
```
**Auth**: None (AllowAnonymous)
**Rate Limit**: public-api
**Cache**: 300 seconds (5 minutes)
**Query Params**:
- `limit` (int): Maximum number of categories (default: 25)
- `onlyPopular` (bool): Only return categories with providers (default: false)
**Response**:
```json
[
  {
    "name": "string",
    "slug": "string",
    "description": "string?",
    "iconUrl": "string?",
    "color": "string",
    "gradient": "string?",
    "providerCount": number?,
    "displayOrder": number?
  }
]
```

#### Get Popular Categories
```http
GET /api/v1/Categories/popular?limit=8
```
**Auth**: None (AllowAnonymous)
**Rate Limit**: public-api
**Cache**: 300 seconds (5 minutes)
**Query Params**:
- `limit` (int): Number of popular categories (default: 8)
**Response**: Same as Get All Categories (filtered to only popular)

---

### Providers

#### Search Providers
```http
GET /api/v1/Providers/search
```
**Auth**: None (AllowAnonymous)
**Query Params**:
- `query` (string?): Search query
- `categoryIds` (List<string>?): Filter by category IDs
- `latitude` (double?): User latitude
- `longitude` (double?): User longitude
- `radiusKm` (double?): Search radius in kilometers
- `minRating` (int?): Minimum rating filter
- `maxPrice` (int?): Maximum price filter
- `openNow` (bool?): Filter by currently open providers
- `sortBy` (string?): Sort field ('rating', 'distance', 'price')
- `sortOrder` (string?): Sort order ('asc', 'desc')
- `pageNumber` (int?): Page number
- `pageSize` (int?): Page size
**Response**: PagedResult<ProviderSearchResponse>

#### Get Providers by Location
```http
GET /api/v1/Providers/by-location
```
**Auth**: None (AllowAnonymous)
**Response**: PagedResult<ProviderLocationResponse>

#### Get Provider by ID
```http
GET /api/v1/Providers/{id}?includeServices=false&includeStaff=false
```
**Auth**: None (AllowAnonymous)
**Query Params**:
- `includeServices` (bool): Include provider's services (default: false)
- `includeStaff` (bool): Include provider's staff (default: false)
**Response**: ProviderDetailsResponse

#### Get Provider by Owner ID
```http
GET /api/v1/Providers/by-owner/{id}
```
**Auth**: Required (commented out in code)

#### Register Provider (Simple)
```http
POST /api/v1/Providers/register
```
**Auth**: Required
**Rate Limit**: provider-registration
**Response**: 201 Created with ProviderResponse

#### Register Provider (Full)
```http
POST /api/v1/Providers/register-full
```
**Auth**: Required
**Rate Limit**: provider-registration
**Description**: Complete multi-step registration in one call
**Response**: 201 Created with ProviderFullRegistrationResponse

#### Register Organization Provider
```http
POST /api/v1/Providers/organizations
```
**Auth**: Required
**Rate Limit**: provider-registration
**Description**: Create a draft organization provider (business with potential staff)

#### Register Independent Individual
```http
POST /api/v1/Providers/individuals
```
**Auth**: Required
**Rate Limit**: provider-registration
**Description**: Create a draft independent individual provider (solo professional)

#### Create Provider Draft
```http
POST /api/v1/Providers/draft
```
**Auth**: Required
**Rate Limit**: provider-registration
**Description**: Step 3 of registration flow

#### Get Draft Provider
```http
GET /api/v1/Providers/draft
```
**Auth**: Required
**Response**: GetDraftProviderResponse or 404 if no draft exists

#### Complete Provider Registration
```http
POST /api/v1/Providers/{providerId}/complete
```
**Auth**: Required
**Rate Limit**: provider-registration
**Description**: Final step (Step 9) - moves provider to PendingVerification status and returns new JWT tokens

#### Get Current Provider Status
```http
GET /api/v1/Providers/current/status
```
**Auth**: Required
**Response**: ProviderStatusResponse

#### Refresh Provider Token
```http
POST /api/v1/Providers/current/refresh-token
```
**Auth**: Required
**Description**: Get new JWT token with updated provider status after registration
**Response**: RefreshProviderTokenResponse

#### Activate Provider (Admin)
```http
POST /api/v1/Providers/{id}/activate
```
**Auth**: Required (Policy: AdminOnly)

#### Get Providers by Status (Admin)
```http
GET /api/v1/Providers/by-status/{status}?maxResults=100
```
**Auth**: Required (Policy: AdminOnly)
**Query Params**:
- `maxResults` (int): Max results (default: 100, max: 1000)

---

### Provider Staff Management

#### Get Staff
```http
GET /api/v1/Providers/{id}/staff?activeOnly=false
```
**Auth**: Required
**Query Params**:
- `activeOnly` (bool): Return only active staff members (default: false)

#### Add Staff
```http
POST /api/v1/Providers/{providerId}/staff
```
**Auth**: Required
**Request**:
```json
{
  "firstName": "string",
  "lastName": "string",
  "email": "string?",
  "phoneNumber": "string?",
  "countryCode": "string?",
  "role": "string",  // Default: "ServiceProvider"
  "notes": "string?",
  "biography": "string?",
  "profilePhotoUrl": "string?"
}
```

#### Update Staff
```http
PUT /api/v1/Providers/{id}/staff/{staffId}
```
**Auth**: Required

#### Remove Staff
```http
DELETE /api/v1/Providers/{id}/staff/{staffId}
```
**Auth**: Required

#### Upload Staff Photo
```http
POST /api/v1/Providers/{id}/staff/{staffId}/photo
```
**Auth**: Required
**Content-Type**: multipart/form-data
**File Constraints**: Max 5MB, JPG/PNG/WebP only

---

### Provider Gallery Management

#### Upload Gallery Images
```http
POST /api/v1/Providers/{providerId}/gallery
```
**Auth**: Required
**Content-Type**: multipart/form-data
**Request Size Limit**: 50MB (max 10 files, 10MB each)

#### Get Gallery Images
```http
GET /api/v1/Providers/{providerId}/gallery
```
**Auth**: None (AllowAnonymous)

#### Update Gallery Image Metadata
```http
PUT /api/v1/Providers/{providerId}/gallery/{imageId}
```
**Auth**: Required
**Request**:
```json
{
  "caption": "string?",
  "altText": "string?"
}
```

#### Reorder Gallery Images
```http
PUT /api/v1/Providers/{providerId}/gallery/reorder
```
**Auth**: Required
**Request**:
```json
{
  "imageOrders": {
    "imageId": displayOrder
  },
  "primaryImageId": "string (guid)?"
}
```

#### Set Primary Gallery Image
```http
PUT /api/v1/Providers/{providerId}/gallery/{imageId}/set-primary
```
**Auth**: Required

#### Delete Gallery Image
```http
DELETE /api/v1/Providers/{providerId}/gallery/{imageId}
```
**Auth**: Required

---

### Provider Profile Management

#### Upload Profile Image
```http
POST /api/v1/Providers/profile/image
```
**Auth**: Required
**Content-Type**: multipart/form-data
**Request Size Limit**: 5MB

#### Upload Business Logo
```http
POST /api/v1/Providers/business/logo
```
**Auth**: Required
**Content-Type**: multipart/form-data
**Request Size Limit**: 5MB

#### Update Profile
```http
PUT /api/v1/Providers/profile
```
**Auth**: Required
**Request**:
```json
{
  "fullName": "string?",
  "email": "string?",
  "profileImageUrl": "string?"
}
```

#### Update Business Info
```http
PUT /api/v1/Providers/business
```
**Auth**: Required
**Request**:
```json
{
  "businessName": "string",
  "description": "string",
  "logoUrl": "string?"
}
```

---

### Bookings

#### Create Booking
```http
POST /api/v1/Bookings
```
**Auth**: Required
**Request**:
```json
{
  "customerId": "string (guid)",  // Auto-set from JWT
  "providerId": "string (guid)",
  "serviceId": "string (guid)",
  "staffProviderId": "string (guid)",
  "startTime": "datetime",
  "customerNotes": "string?"
}
```
**Response**: 201 Created with BookingResponse

#### Get Booking by ID
```http
GET /api/v1/Bookings/{id}
```
**Auth**: Required
**Response**: BookingDetailsResponse

#### Get My Bookings
```http
GET /api/v1/Bookings/my-bookings
```
**Auth**: Required
**Query Params**:
- `status` (string?): Filter by status
- `from` (datetime?): Start date filter
- `to` (datetime?): End date filter
- `pageNumber` (int): Page number (default: 1)
- `pageSize` (int): Page size (default: 20)
**Response**: PagedResult<CustomerBookingDto>

#### Get Provider Bookings
```http
GET /api/v1/Bookings/provider/{providerId}
```
**Auth**: Required (Policy: ProviderOrAdmin)
**Query Params**:
- `status` (string?)
- `from` (datetime?)
- `to` (datetime?)

#### Confirm Booking
```http
POST /api/v1/Bookings/{id}/confirm
```
**Auth**: Required (Policy: ProviderOrAdmin)
**Request**:
```json
{
  "paymentMethodId": "string"
}
```

#### Cancel Booking
```http
POST /api/v1/Bookings/{id}/cancel
```
**Auth**: Required
**Request**:
```json
{
  "reason": "string",
  "cancelledBy": "string (guid — the cancelling USER id; non-guid values fail body binding)"
}
```

#### Reschedule Booking
```http
POST /api/v1/Bookings/{id}/reschedule
```
**Auth**: Required
**Request**:
```json
{
  "newStartTime": "datetime",
  "newStaffId": "string (guid)?",
  "reason": "string?"
}
```

#### Complete Booking
```http
POST /api/v1/Bookings/{id}/complete
```
**Auth**: Required (Policy: ProviderOrAdmin)
**Request**:
```json
{
  "completionNotes": "string?"
}
```

#### Mark as No-Show
```http
POST /api/v1/Bookings/{id}/no-show
```
**Auth**: Required (Policy: ProviderOrAdmin)
**Request**:
```json
{
  "notes": "string?"
}
```

#### Assign Staff
```http
PUT /api/v1/Bookings/{id}/assign-staff/{staffId}
```
**Auth**: Required (Policy: ProviderOrAdmin)

#### Add Notes
```http
POST /api/v1/Bookings/{id}/notes
```
**Auth**: Required
**Request**:
```json
{
  "notes": "string",
  "isStaffNote": boolean
}
```

#### Search Bookings
```http
GET /api/v1/Bookings/search
```
**Auth**: Required
**Query Params**:
- `providerId` (guid?)
- `customerId` (guid?)
- `serviceId` (guid?)
- `staffId` (guid?)
- `status` (string?)
- `startDate` (datetime?)
- `endDate` (datetime?)
- `pageNumber` (int): default 1
- `pageSize` (int): default 20, max 100

#### Get Available Slots
```http
GET /api/v1/Bookings/available-slots
```
**Auth**: Required
**Query Params**:
- `providerId` (guid): Required
- `serviceId` (guid): Required
- `date` (datetime): Required
- `staffId` (guid?): Optional

#### Get Booking Statistics
```http
GET /api/v1/Bookings/statistics
```
**Auth**: Required (Policy: ProviderOrAdmin)
**Query Params**:
- `providerId` (guid): Required
- `startDate` (datetime?): Optional
- `endDate` (datetime?): Optional

---

### Reviews

Customer reviews of providers (OpenSpec change `provider-reviews-and-ratings`). One review per **completed**
booking, by that booking's customer. A review carries a required overall star and four **optional** dimensions;
every rating is 1.0–5.0 in 0.5 steps. Reviews and provider replies are **admin-moderated**: nothing is public
until an administrator approves it. Identity is always read from the `nameidentifier` claim.

#### Submit a Review
```http
POST /api/v1/reviews/bookings/{bookingId}
```
**Auth**: Required (the booking's customer) · **Rate limit**: `create-review`
**Request**:
```json
{
  "rating": 4.5,
  "comment": "string? (10–2000 chars)",
  "cleanlinessRating": 5.0,
  "skillRating": null,
  "punctualityRating": 3.0,
  "conductRating": null
}
```
**Response 201**: `{ reviewId, providerId, customerId, bookingId, rating, comment, isVerified, createdAt,
moderationStatus: "Pending", cleanlinessRating, skillRating, punctualityRating, conductRating }`
**Errors**: 400 invalid rating (the error names the field, e.g. `SkillRating`) · 401 · 403 not the booking's
customer · 409 booking not completed, or already reviewed.
Submitting withdraws the pending review-request reminders for the booking.

#### Edit Own Review
```http
PUT /api/v1/reviews/{reviewId}
```
**Auth**: Required (the author) · **Rate limit**: `edit-review` (10/hour — an edit unpublishes and recomputes)
**Request**: same body as submit.
**Rules**: within 7 days of writing it, and only while Pending or Published. A Published review returns to Pending
and leaves the provider's rating until approved again; a published reply attached to it returns to Pending too.
**Errors**: 400 window closed / Rejected / Hidden / invalid · 403 not the author · 404.

#### Get a Provider's Reviews (public)
```http
GET /api/v1/reviews/providers/{providerId}?pageNumber=1&pageSize=20&sortBy=date|rating|helpful&sortDescending=true
```
**Auth**: None. **Published reviews only — to everyone, including the provider's own token.**
Statistics are computed over published reviews only and include per-dimension `{ average, count }`
(`cleanliness`, `skill`, `punctuality`, `conduct`; `average: null` when nobody rated it). A reply is included only
once approved. A signed-in reader also gets `myVote` (`"helpful"` | `"notHelpful"` | absent) on each review.

#### Vote Helpful / Not Helpful
```http
PUT /api/v1/reviews/{reviewId}/helpful
```
**Auth**: Required — **BREAKING: this was anonymous before.** · **Rate limit**: `mark-review-helpful`
**Request**: `{ "isHelpful": true }` — one vote per user per review: a first vote adds, repeating the vote you
hold withdraws it, the opposite vote moves it.
**Response 200**: `{ reviewId, helpfulCount, notHelpfulCount, helpfulnessRatio, isConsideredHelpful, myVote }` —
counts are the frozen pre-voting baseline plus live votes.
**Errors**: 400 review not published · 403 own review · 404 · 409 a racing duplicate vote.

#### Report a Review
```http
POST /api/v1/reviews/{reviewId}/report
```
**Auth**: Required (any user, including the reviewed provider) · **Rate limit**: `report-review`
**Request**: `{ "reason": "string (1–500 chars)" }` · **Response 200**: `{ reportId, reviewId }`
A report never hides anything by itself. **Errors**: 400 no reason / review not published · 409 already reported.

#### Provider Reply
```http
POST   /api/v1/reviews/{reviewId}/reply     { "text": "string (1–1000)" }
PUT    /api/v1/reviews/{reviewId}/reply     { "text": "string (1–1000)" }
DELETE /api/v1/reviews/{reviewId}/reply
```
**Auth**: Required — the reviewed business's owner or a manager (`ManageOrganization`). **Administrators cannot
reply.** · **Rate limit**: `reply-review`
Every add or edit returns the reply to moderation; the review itself is untouched.
**Errors**: 400 review not published · 403 · 409 a reply already exists (edit it instead).

#### My Reviews
```http
GET /api/v1/reviews/me?pageNumber=1&pageSize=20
```
**Auth**: Required. The caller's own reviews in **every** state: `moderationStatus`, `moderationReason` (when
rejected/hidden), the reply and its `replyModerationStatus`, and `canEdit`.

#### Provider Review Inbox
```http
GET /api/v1/reviews/providers/{providerId}/inbox?pageNumber=1&pageSize=20
```
**Auth**: Required — owner or manager of that provider. Every review in every state, same item shape as My Reviews.
**Response 200**: `{ items[], totalCount, awaitingReplyCount }`. `awaitingReplyCount` counts, over every review (not the
page), the published reviews still waiting on the business: no reply yet, or a reply an administrator rejected. The
provider app's Home shows it.

---

### Review Moderation (Admin)

**Auth**: `AdminOnly` policy — any of `Admin`, `Administrator`, `SysAdmin` (never a narrower role list; see
FOLLOW-UPS #46). **Rate limit** on every decision: `moderate-review`.

#### Moderation Queue
```http
GET /api/v1/admin/reviews/queue?filter=pending|hidden|reported&pageNumber=1&pageSize=20
```
- `pending` (default): reviews AND replies awaiting a decision, oldest first, with `reviewPending`,
  `replyPending` and `wasPublishedBefore` (an edited review vs a first submission).
- `hidden`: reviews taken down, with `moderationReason`.
- `reported`: published reviews with reports, most reported first, with `reportCount` and
  `reports[] { reason, reportedByUserId, createdAt }`.

#### Decisions
```http
POST /api/v1/admin/reviews/{reviewId}/approve
POST /api/v1/admin/reviews/{reviewId}/reject        { "reason": "required" }
POST /api/v1/admin/reviews/{reviewId}/hide          { "reason": "required" }
POST /api/v1/admin/reviews/{reviewId}/restore
POST /api/v1/admin/reviews/{reviewId}/reply/approve
POST /api/v1/admin/reviews/{reviewId}/reply/reject  { "reason": "required" }
```
**Response 200**: `{ reviewId, moderationStatus, replyModerationStatus }`. Approve/reject apply to Pending,
hide to Published, restore to Hidden; **Rejected is permanent**. Illegal transitions are 400; unknown review 404.
Publishing notifies the salon's owner (`ReviewPublished`, or `ReviewRepublished` for an edit or a restore);
approving a reply notifies the review's author. The provider's rating is recomputed in the same transaction.

#### Recompute All Ratings (post-deploy backfill)
```http
POST /api/v1/admin/reviews/recompute-ratings
```
Recomputes every provider's `averageRating` and `totalReviews` from its published reviews. Run once after
deploying the review-moderation migration; re-runnable. **Response 200**: `{ providersRecomputed }`.

**Rating fields on provider responses**: `averageRating` stays numeric (0 when unrated) and `totalReviews` is the
real published count on search, by-location, detail, by-owner and the admin list. Read them together:
`totalReviews == 0` means "no reviews yet", never "rated zero". Sorting by rating puts unrated providers after all
rated ones in both directions.

---

## Host & Routing (Port 5000)

All endpoints are served by a single ASP.NET Core host (`booksy-api`) on `:5000` (internal port 80). There is no separate API gateway; ASP.NET Core routing dispatches requests to the controllers of whichever bounded context owns them. All requests reach the host via `http://napstar.ir/api` (the frontend's nginx proxies `/api` → `booksy-api:80`).

### Routing Notes

- **Convention**: Endpoint paths use **PascalCase** (e.g. `/api/v1/Categories/popular`, `/api/v1/Providers`, `/api/v1/Auth/login`).

- **Endpoint Ownership** (all on the same host):
  - `/api/v1/Auth/*` → UserManagement context
  - `/api/v1/Customers/*` → UserManagement context
  - `/api/v1/Categories/*` → ServiceCatalog context
  - `/api/v1/Providers/*` → ServiceCatalog context
  - `/api/v1/Bookings/*` → ServiceCatalog context

---

## Common Response Codes

- **200 OK**: Successful request
- **201 Created**: Resource successfully created
- **204 No Content**: Successful request with no response body
- **400 Bad Request**: Invalid request data
- **401 Unauthorized**: Authentication required or failed
- **403 Forbidden**: User lacks permission
- **404 Not Found**: Resource not found
- **409 Conflict**: Resource already exists
- **429 Too Many Requests**: Rate limit exceeded
- **500 Internal Server Error**: Server error

---

## Authentication

Most endpoints require a JWT Bearer token in the `Authorization` header:

```http
Authorization: Bearer <access_token>
```

Tokens are obtained via the authentication endpoints and include:
- **Access Token**: Short-lived (15 minutes)
- **Refresh Token**: Long-lived, used to obtain new access tokens

### Token Claims

- `sub` / `NameIdentifier`: User ID
- `customer_id`: Customer ID (for customers)
- `provider_id`: Provider ID (for providers)
- `provider_status`: Provider status (Pending, Active, etc.)
- `role`: User roles (Customer, Provider, Admin)

---

## Rate Limiting

The platform uses rate limiting policies:
- `phone-verification`: For sending OTP codes
- `code-verification`: For verifying OTP codes
- `registration`: For user/provider registration
- `provider-registration`: For provider registration steps
- `public-api`: For public endpoints
- `provider-reviews`, `create-review`, `mark-review-helpful`, `edit-review` (10/h), `report-review`,
  `reply-review`, `moderate-review`: review endpoints. Every name must also exist in
  `RateLimitingOptions.Defaults` or the endpoint throws at request time (pinned by `ReviewRateLimitingTests`).

---

## Pagination

Paginated endpoints return:
```json
{
  "items": [...],
  "totalCount": number,
  "pageNumber": number,
  "pageSize": number,
  "totalPages": number,
  "hasNextPage": boolean,
  "hasPreviousPage": boolean
}
```

Pagination headers are also included in the response.

---

## Notes

1. **Service Ownership**:
   - UserManagement: Authentication, Customers, Users
   - ServiceCatalog: Providers, Services, Categories, Bookings, Reviews

2. **Cross-Context Communication**:
   - `/api/v1/Auth/generate-token` is used for cross-context token generation
   - `/api/v1/Providers/current/refresh-token` invokes UserManagement logic in-process (same host)

3. **Provider Registration Flow**:
   - Draft creation → Step-by-step completion → Finalize → Admin approval → Activation

4. **Recently Visited Providers** (NEW):
   - Tracks customer views of provider profiles
   - Maximum 50 entries per customer (configurable)
   - Automatically deduplicates and updates visit counts

---

**For detailed request/response schemas, refer to the Swagger documentation** (single host):
- Booksy API: http://napstar.ir:5000/swagger
