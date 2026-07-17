# provider-gallery-management Specification

## Purpose
TBD - created by archiving change implement-gallery-management. Update Purpose after archive.
## Requirements
### Requirement: Manage the Public Gallery

The app SHALL show the provider's gallery as a thumbnail grid (More → گالری) with the primary image visibly marked, and SHALL support uploading images from the device (multi-select), setting an image as primary, and deleting an image behind an explicit confirmation. Mutations refresh the grid on success; failures surface a message and change nothing.

#### Scenario: Upload adds images to the grid
- **WHEN** the provider picks images and the upload succeeds
- **THEN** the refreshed grid contains the new images

#### Scenario: Set primary re-marks the grid
- **WHEN** the provider sets another image as primary
- **THEN** that image shows the primary marker and the previous one does not

#### Scenario: Delete requires confirmation
- **WHEN** the provider taps delete on an image
- **THEN** nothing is deleted until they confirm, and on confirmation the image leaves the grid

#### Scenario: Empty gallery invites upload
- **WHEN** the gallery is empty
- **THEN** an empty state offers the upload action

### Requirement: Gallery Writes Are Owner-Guarded

All gallery mutation endpoints (upload, metadata, reorder, set-primary, delete) SHALL reject callers who are neither the provider's owner nor an admin with 403, including first-session tokens via the ownership fallback.

#### Scenario: Stranger cannot modify a gallery
- **WHEN** an authenticated non-owner uploads to or deletes from another provider's gallery
- **THEN** the request is rejected with 403

