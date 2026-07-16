## 1. Backend

- [x] 1.1 `CanManageProvider(providerId)` guard on the 5 gallery writes; build green

## 2. Flutter

- [x] 2.1 `GalleryImage` entity; api get/upload(multipart files)/delete/set-primary; repository methods + mapping tests
- [x] 2.2 `GalleryCubit` (list + upload/remove/set-primary mutations) + tests
- [x] 2.3 `/more/gallery` grid page (primary star, image action sheet, delete confirm, injectable picker, empty-state CTA); hub row + Home checklist `gallery` wiring; `AppStrings`
- [x] 2.4 Widget tests: grid render + primary marker, sheet actions, delete confirm, upload via injected fake picker

## 3. Verification

- [x] 3.1 `flutter analyze` + full suite green; commit + push
