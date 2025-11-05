# provider-gallery-management Specification Delta

## ADDED Requirements

### Requirement: Gallery Image Upload
The system SHALL allow providers to upload multiple images to their gallery with automatic optimization.

#### Scenario: Provider uploads gallery images
- **WHEN** a provider uploads one or more images to the gallery via drag-and-drop or file selector
- **THEN** the system validates each file (max 10MB, formats: JPEG, PNG, WebP)
- **AND** generates optimized versions (thumbnail 200x200, medium 800x800, original max 2000x2000)
- **AND** converts images to WebP format for optimal performance
- **AND** stores images with unique IDs and URLs in the database
- **AND** displays upload progress (0-100%) for each image
- **AND** adds uploaded images to the gallery grid immediately with optimistic updates
- **AND** assigns sequential display order to new images

#### Scenario: Upload validation failure
- **WHEN** a provider attempts to upload an invalid file (too large, wrong format, corrupted)
- **THEN** the system rejects the file before upload begins
- **AND** displays a clear error message indicating the specific validation issue
- **AND** does not affect other valid files in a multi-file upload

#### Scenario: Upload limit enforcement
- **WHEN** a provider attempts to upload images beyond the configured limit (default 50 total images)
- **THEN** the system rejects the upload
- **AND** displays a message indicating the current count and maximum allowed
- **AND** suggests removing existing images to make room for new uploads

### Requirement: Gallery Image Metadata Management
The system SHALL allow providers to update metadata for gallery images including captions, alt text, and display order.

#### Scenario: Provider adds caption to image
- **WHEN** a provider edits an image and adds a caption (max 500 characters)
- **THEN** the system stores the caption in the database
- **AND** displays the caption below the image in the gallery grid
- **AND** includes the caption in customer-facing profile views

#### Scenario: Provider updates display order
- **WHEN** a provider drags an image to a new position in the gallery grid
- **THEN** the system updates the display_order field for affected images
- **AND** re-renders the gallery in the new order
- **AND** persists the order changes to the database
- **AND** reflects the new order in customer-facing views immediately

#### Scenario: Provider updates alt text for accessibility
- **WHEN** a provider edits an image and provides alt text
- **THEN** the system stores the alt text separately from the caption
- **AND** uses the alt text as the `alt` attribute in `<img>` tags for accessibility
- **AND** validates that alt text is descriptive (warns if empty or too short)

### Requirement: Gallery Image Deletion
The system SHALL allow providers to delete images from their gallery with cascade cleanup of stored files.

#### Scenario: Provider deletes a single image
- **WHEN** a provider clicks delete on a gallery image and confirms the action
- **THEN** the system removes the image record from the database
- **AND** deletes all associated files (thumbnail, medium, original) from storage
- **AND** removes the image from the gallery grid immediately
- **AND** re-indexes display_order values for remaining images to close gaps

#### Scenario: Provider deletes multiple images
- **WHEN** a provider selects multiple images (checkbox selection) and clicks bulk delete
- **THEN** the system prompts for confirmation showing the count of images to be deleted
- **AND** deletes all selected images and their files atomically
- **AND** updates the gallery grid with remaining images

#### Scenario: Deletion confirmation required
- **WHEN** a provider initiates image deletion
- **THEN** the system displays a confirmation modal
- **AND** shows a preview of the image being deleted
- **AND** warns that deletion is permanent and cannot be undone
- **AND** requires explicit confirmation (button click, not accidental)

### Requirement: Gallery Performance Optimization
The system SHALL optimize gallery loading performance through lazy loading, progressive image rendering, and CDN-ready architecture.

#### Scenario: Lazy loading of images in grid
- **WHEN** a provider or customer views a gallery with many images (10+ images)
- **THEN** the system loads only images visible in the viewport initially
- **AND** loads additional images as the user scrolls down (Intersection Observer API)
- **AND** displays low-resolution placeholders for images not yet loaded
- **AND** improves initial page load time by deferring off-screen image downloads

#### Scenario: Progressive image rendering
- **WHEN** an image is loading in the gallery grid
- **THEN** the system first displays the thumbnail version (small file size)
- **AND** progressively upgrades to medium or original size based on viewport size
- **AND** uses responsive `<img srcset>` to serve appropriate image size per device

#### Scenario: Modern image format delivery
- **WHEN** the system serves gallery images to browsers
- **THEN** it delivers WebP format to modern browsers that support it
- **AND** provides JPEG fallback for older browsers via `<picture>` element
- **AND** reduces bandwidth usage by ~30% compared to JPEG-only delivery

### Requirement: Gallery Accessibility
The system SHALL ensure gallery interfaces are fully accessible with ARIA labels, keyboard navigation, and screen reader support.

#### Scenario: Keyboard navigation for gallery grid
- **WHEN** a user navigates the gallery using only a keyboard
- **THEN** Tab key moves focus between images in logical order (left-to-right, top-to-bottom)
- **AND** Enter key opens the selected image in lightbox view
- **AND** Delete key (with confirmation) removes the focused image
- **AND** Arrow keys navigate between images within the grid
- **AND** Escape key closes any open modals or lightbox

#### Scenario: Screen reader support
- **WHEN** a screen reader user accesses the gallery
- **THEN** each image has a descriptive alt attribute (from provider-provided alt text or auto-generated description)
- **AND** ARIA labels describe image actions ("Delete image 1 of 12", "Edit caption for image 2")
- **AND** ARIA live regions announce upload progress and completion
- **AND** role attributes correctly identify gallery elements (role="img", role="button")

#### Scenario: Focus management in modals
- **WHEN** a provider opens an image edit or delete confirmation modal
- **THEN** focus moves to the first interactive element in the modal
- **AND** Tab key cycles focus only within the modal (focus trap)
- **AND** Escape key closes the modal and returns focus to the trigger element

### Requirement: Gallery Image Storage Architecture
The system SHALL store image files separately from metadata with support for future CDN migration without API changes.

#### Scenario: Local file storage with CDN-ready URLs
- **WHEN** the system stores an uploaded image
- **THEN** it saves files to local disk at `/wwwroot/uploads/providers/{providerId}/gallery/`
- **AND** stores absolute URLs in the database (e.g., `https://api.booksy.com/uploads/providers/{providerId}/gallery/{imageId}_thumb.webp`)
- **AND** allows seamless migration to CDN by updating URL prefix in configuration
- **AND** does not require API changes when switching storage providers

#### Scenario: Multiple image size generation
- **WHEN** an image is uploaded
- **THEN** the system generates exactly three versions: thumbnail (200x200px), medium (800x800px), original (max 2000x2000px)
- **AND** stores each version with size suffix in filename (`{imageId}_thumb.webp`, `{imageId}_medium.webp`, `{imageId}_original.webp`)
- **AND** preserves aspect ratio in all versions using smart cropping for thumbnails

#### Scenario: File cleanup on deletion
- **WHEN** a gallery image is deleted from the database
- **THEN** the system asynchronously deletes all three associated files (thumbnail, medium, original) from storage
- **AND** logs any file deletion failures for manual cleanup
- **AND** does not fail the database deletion if file deletion fails (eventual consistency)

### Requirement: Gallery API Endpoints
The system SHALL provide RESTful API endpoints for gallery CRUD operations following existing API versioning and authentication patterns.

#### Scenario: Upload images via API
- **WHEN** an authenticated provider sends POST `/api/v1/providers/{providerId}/gallery` with multipart form data
- **THEN** the system validates the provider owns the specified providerId
- **AND** accepts multiple files in the `images` field
- **AND** processes uploads asynchronously and returns 202 Accepted with upload IDs
- **AND** allows polling the upload status or uses WebSocket for real-time progress

#### Scenario: Retrieve gallery images
- **WHEN** a request is made to GET `/api/v1/providers/{providerId}/gallery`
- **THEN** the system returns all active gallery images for the provider
- **AND** includes URLs for all image sizes (thumbnail, medium, original)
- **AND** sorts images by display_order

#### Scenario: Update image metadata
- **WHEN** a provider sends PUT `/api/v1/providers/{providerId}/gallery/{imageId}` with updated caption or alt text
- **THEN** the system validates ownership and updates only the provided fields
- **AND** returns the updated image metadata in the response
- **AND** logs the metadata change for audit purposes

#### Scenario: Reorder images
- **WHEN** a provider sends PUT `/api/v1/providers/{providerId}/gallery/reorder` with an array of `{imageId, newOrder}` objects
- **THEN** the system atomically updates all display_order fields in a single transaction
- **AND** validates that all imageIds belong to the provider
- **AND** ensures no duplicate display_order values

#### Scenario: Delete image via API
- **WHEN** a provider sends DELETE `/api/v1/providers/{providerId}/gallery/{imageId}`
- **THEN** the system soft-deletes the image (sets is_active = false) for audit trail
- **AND** schedules file deletion as a background job
- **AND** returns 204 No Content on successful deletion
- **AND** returns 404 if the image does not exist or does not belong to the provider
