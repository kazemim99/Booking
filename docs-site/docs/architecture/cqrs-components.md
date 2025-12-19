# CQRS Component Inventory - Complete System Map

**Generated:** 2025-11-06
**Purpose:** Complete inventory of all CQRS components for comprehensive test coverage

---

## ServiceCatalog Commands (50+)

### Booking Commands (6)
1. ✅ CreateBookingCommand → CreateBookingCommandHandler
2. ✅ ConfirmBookingCommand → ConfirmBookingCommandHandler
3. ✅ CancelBookingCommand → CancelBookingCommandHandler
4. ✅ RescheduleBookingCommand → RescheduleBookingCommandHandler
5. ✅ CompleteBookingCommand → CompleteBookingCommandHandler
6. ✅ MarkNoShowCommand → MarkNoShowCommandHandler

### Payment Commands (3)
7. ✅ ProcessPaymentCommand → ProcessPaymentCommandHandler + Validator
8. ✅ CapturePaymentCommand → CapturePaymentCommandHandler + Validator
9. ✅ RefundPaymentCommand → RefundPaymentCommandHandler + Validator

### Payout Commands (2)
10. ✅ CreatePayoutCommand → CreatePayoutCommandHandler + Validator
11. ✅ ExecutePayoutCommand → ExecutePayoutCommandHandler + Validator

### Service Commands (5)
12. ✅ CreateServiceCommand → CreateServiceCommandHandler + Validator
13. ✅ UpdateServiceCommand → UpdateServiceCommandHandler + Validator
14. ✅ ActivateServiceCommand → ActivateServiceCommandHandler
15. ✅ DeactivateServiceCommand → DeactivateServiceCommandHandler
16. ✅ ArchiveServiceCommand → ArchiveServiceCommandHandler
17. ✅ AddProviderServiceCommand → AddProviderServiceCommandHandler
18. ✅ AddPriceTierCommand → AddPriceTierCommandHandler + Validator
19. ✅ SetServiceAvailabilityCommand → SetServiceAvailabilityCommandHandler + Validator

### Provider Commands (30+)
20. ✅ RegisterProviderCommand → RegisterProviderCommandHandler + Validator
21. ✅ RegisterProviderFullCommand → RegisterProviderFullCommandHandler
22. ✅ CreateProviderDraftCommand → CreateProviderDraftCommandHandler
23. ✅ CompleteProviderRegistrationCommand → CompleteProviderRegistrationCommandHandler
24. ✅ ActivateProviderCommand → ActivateProviderCommandHandler
25. ✅ UpdateProviderProfileCommand → UpdateProviderProfileCommandHandler
26. ✅ UpdateBusinessProfileCommand → UpdateBusinessProfileCommandHandler + Validator
27. ✅ UpdateBusinessInfoCommand → UpdateBusinessInfoCommandHandler
28. ✅ UpdateContactInfoCommand → UpdateContactInfoCommandHandler
29. ✅ UpdateLocationCommand → UpdateLocationCommandHandler
30. ✅ UpdateBusinessLogoCommand → UpdateBusinessLogoCommandHandler
31. ✅ AddStaffCommand → AddStaffCommandHandler + Validator
32. ✅ AddStaffToProviderCommand → AddStaffToProviderCommandHandler
33. ✅ UpdateProviderStaffCommand → UpdateProviderStaffCommandHandler
34. ✅ ActivateProviderStaffCommand → ActivateProviderStaffCommandHandler
35. ✅ DeactivateProviderStaffCommand → DeactivateProviderStaffCommandHandler
36. ✅ UpdateBusinessHoursCommand → UpdateBusinessHoursCommandHandler
37. ✅ UpdateWorkingHoursCommand → UpdateWorkingHoursCommandHandler
38. ✅ AddHolidayCommand → AddHolidayCommandHandler
39. ✅ DeleteHolidayCommand → DeleteHolidayCommandHandler
40. ✅ AddExceptionCommand → AddExceptionCommandHandler
41. ✅ DeleteExceptionCommand → DeleteExceptionCommandHandler
42. ✅ UploadGalleryImagesCommand → UploadGalleryImagesCommandHandler + Validator
43. ✅ DeleteGalleryImageCommand → DeleteGalleryImageCommandHandler
44. ✅ ReorderGalleryImagesCommand → ReorderGalleryImagesCommandHandler
45. ✅ UpdateGalleryImageMetadataCommand → UpdateGalleryImageMetadataCommandHandler + Validator
46. ✅ SetPrimaryGalleryImageCommand → SetPrimaryGalleryImageCommandHandler
47. ✅ UpdateProviderVerificationCommand → UpdateProviderVerificationCommandHandler

### Progressive Registration Commands (7)
48. ✅ SaveStep3LocationCommand → SaveStep3LocationCommandHandler
49. ✅ SaveStep4ServicesCommand → SaveStep4ServicesCommandHandler
50. ✅ SaveStep5StaffCommand → SaveStep5StaffCommandHandler
51. ✅ SaveStep6WorkingHoursCommand → SaveStep6WorkingHoursCommandHandler
52. ✅ SaveStep7GalleryCommand → SaveStep7GalleryCommandHandler
53. ✅ SaveStep8FeedbackCommand → SaveStep8FeedbackCommandHandler
54. ✅ SaveStep9CompleteCommand → SaveStep9CompleteCommandHandler

### Notification Commands (5)
55. ✅ SendNotificationCommand → SendNotificationCommandHandler
56. ✅ ScheduleNotificationCommand → ScheduleNotificationCommandHandler
57. ✅ SendBulkNotificationCommand → SendBulkNotificationCommandHandler
58. ✅ CancelNotificationCommand → CancelNotificationCommandHandler
59. ✅ ResendNotificationCommand → ResendNotificationCommandHandler

---

## ServiceCatalog Queries (30+)

### Booking Queries
1. ✅ GetBookingByIdQuery → GetBookingByIdQueryHandler
2. ✅ GetCustomerBookingsQuery → GetCustomerBookingsQueryHandler
3. ✅ GetProviderBookingsQuery → GetProviderBookingsQueryHandler

### Payment Queries
4. ✅ GetPaymentByIdQuery → GetPaymentByIdQueryHandler
5. ✅ GetCustomerPaymentsQuery → GetCustomerPaymentsQueryHandler
6. ✅ GetProviderEarningsQuery → GetProviderEarningsQueryHandler

### Payout Queries
7. ✅ GetProviderPayoutsQuery → GetProviderPayoutsQueryHandler
8. ✅ GetPendingPayoutsQuery → GetPendingPayoutsQueryHandler

### Service Queries
9. ✅ GetServiceByIdQuery → GetServiceByIdQueryHandler
10. ✅ GetProviderServicesQuery → GetProviderServicesQueryHandler
11. ✅ SearchServicesQuery → SearchServicesQueryHandler

### Provider Queries (15+)
12. ✅ GetProviderByIdQuery → GetProviderByIdQueryHandler
13. ✅ SearchProvidersQuery → SearchProvidersQueryHandler
14. ✅ GetProvidersByLocationQuery → GetProvidersByLocationQueryHandler
15. ✅ GetCurrentProviderStatusQuery → GetCurrentProviderStatusQueryHandler + Validator
16. ✅ GetProviderStaffQuery → GetProviderStaffQueryHandler
17. ✅ GetBusinessHoursQuery → GetBusinessHoursQueryHandler
18. ✅ GetHolidaysQuery → GetHolidaysQueryHandler
19. ✅ GetExceptionsQuery → GetExceptionsQueryHandler
20. ✅ GetGalleryImagesQuery → GetGalleryImagesQueryHandler

### Availability Queries
21. ✅ GetAvailableSlotsQuery → GetAvailableSlotsQueryHandler
22. ✅ CheckAvailabilityQuery → CheckAvailabilityQueryHandler

### Notification Queries
23. ✅ GetNotificationHistoryQuery → GetNotificationHistoryQueryHandler
24. ✅ GetDeliveryStatusQuery → GetDeliveryStatusQueryHandler
25. ✅ GetNotificationAnalyticsQuery → GetNotificationAnalyticsQueryHandler

---

## ServiceCatalog Domain Events (50+)

### Booking Events (8)
1. ✅ BookingRequestedEvent
2. ✅ BookingConfirmedEvent
3. ✅ BookingCancelledEvent
4. ✅ BookingRescheduledEvent
5. ✅ BookingCompletedEvent
6. ✅ BookingNoShowEvent
7. ✅ BookingPaymentProcessedEvent
8. ✅ BookingRefundProcessedEvent

### Payment Events (6)
9. ✅ PaymentCreatedEvent
10. ✅ PaymentAuthorizedEvent
11. ✅ PaymentCapturedEvent
12. ✅ PaymentProcessedEvent
13. ✅ PaymentRefundedEvent
14. ✅ PaymentFailedEvent

### Payout Events (8)
15. ✅ PayoutCreatedEvent
16. ✅ PayoutScheduledEvent
17. ✅ PayoutProcessingEvent
18. ✅ PayoutCompletedEvent
19. ✅ PayoutFailedEvent
20. ✅ PayoutCancelledEvent
21. ✅ PayoutOnHoldEvent
22. ✅ PayoutReleasedFromHoldEvent

### Service Events (5)
23. ✅ ServiceCreatedEvent
24. ✅ ServiceUpdatedEvent
25. ✅ ServiceActivatedEvent
26. ✅ ServiceDeactivatedEvent
27. ✅ ServicePriceChangedEvent

### Provider Events (13)
28. ✅ ProviderRegisteredEvent
29. ✅ ProviderActivatedEvent
30. ✅ ProviderDeactivatedEvent
31. ✅ ProviderVerificationStatusChangedEvent
32. ✅ BusinessProfileUpdatedEvent
33. ✅ BusinessHoursUpdatedEvent
34. ✅ ProviderLocationUpdatedEvent
35. ✅ HolidayAddedEvent
36. ✅ HolidayRemovedEvent
37. ✅ ExceptionAddedEvent
38. ✅ ExceptionRemovedEvent
39. ✅ GalleryImageUploadedEvent
40. ✅ GalleryImageDeletedEvent
41. ✅ GalleryImagesReorderedEvent

### Staff Events (4)
42. ✅ StaffAddedEvent
43. ✅ StaffUpdatedEvent
44. ✅ StaffRemovedEvent
45. ✅ StaffDeactivatedEvent
46. ✅ StaffActivatedEvent

### Notification Events (4)
47. ✅ NotificationSentEvent
48. ✅ NotificationDeliveredEvent
49. ✅ NotificationFailedEvent
50. ✅ NotificationCancelledEvent

---

## Validators (20+)

### Command Validators
1. ✅ ProcessPaymentCommandValidator
   - Amount > 0
   - Currency valid
   - PaymentMethodId not empty
   - CustomerId valid

2. ✅ CapturePaymentCommandValidator
   - Amount > 0
   - Amount less than or equal to authorized amount

3. ✅ RefundPaymentCommandValidator
   - Amount > 0
   - Amount less than or equal to paid amount
   - Reason required

4. ✅ CreatePayoutCommandValidator
   - ProviderId valid
   - Period dates valid
   - Commission rate 0-100%

5. ✅ ExecutePayoutCommandValidator
   - PayoutId valid
   - ExternalTransactionId not empty

6. ✅ CreateServiceCommandValidator
   - Name not empty
   - Duration > 0
   - BasePrice >= 0
   - Category valid

7. ✅ UpdateServiceCommandValidator
   - ServiceId valid
   - Name not empty if provided
   - Price >= 0 if provided

8. ✅ AddPriceTierCommandValidator
   - Duration > 0
   - Price >= 0

9. ✅ RegisterProviderCommandValidator
   - BusinessName not empty (2-100 chars)
   - ContactInfo valid
   - Address valid

10. ✅ AddStaffCommandValidator
    - FirstName not empty
    - LastName not empty
    - Phone valid format
    - Role valid

11. ✅ UploadGalleryImagesCommandValidator
    - At least 1 image
    - Max 10 images
    - Each file less than or equal to 10MB
    - Valid image formats

12. ✅ UpdateGalleryImageMetadataCommandValidator
    - Caption max length
    - Description max length

13. ✅ UpdateBusinessProfileCommandValidator
    - BusinessName 2-100 chars if provided
    - Description max 1000 chars

14. ✅ SetServiceAvailabilityCommandValidator
    - Valid days of week
    - Start time before end time

### Common Validators
15. ✅ PriceValidator
    - Amount >= 0
    - Currency ISO 4217 format

16. ✅ DurationValidator
    - Minutes > 0
    - Hours >= 0

17. ✅ BusinessAddressValidator
    - Street required
    - City required (2-50 chars)
    - State required
    - Postal code format
    - Country ISO 3166

18. ✅ OperatingHoursValidator
    - Open time before close time
    - Valid time format
    - Break times within operating hours

19. ✅ ContactInfoValidator
    - Email valid format
    - Phone valid E.164 format
    - Website URL valid if provided

### Query Validators
20. ✅ GetCurrentProviderStatusQueryValidator
    - UserId valid GUID

---

## Conditional Logic Paths

### CreateBookingCommandHandler
- ✅ Provider not found → NotFoundException
- ✅ Service not found → NotFoundException
- ✅ Service doesn't belong to provider → ConflictException
- ✅ Staff not found → NotFoundException
- ✅ Time slot not available → ConflictException
- ✅ Validation constraints fail → ConflictException
- ✅ Success path → Booking created, events published

### ProcessPaymentCommandHandler
- ✅ BookingId provided → CreateForBooking
- ✅ No BookingId → CreateDirect
- ✅ Gateway success → ProcessCharge
- ✅ Gateway failure → MarkAsFailed
- ✅ Both paths save payment

### RefundPaymentCommandHandler
- ✅ Payment not found → NotFoundException
- ✅ Already refunded → ConflictException
- ✅ Refund > paid amount → ValidationException
- ✅ Partial refund → PartialRefund
- ✅ Full refund → FullRefund

### CapturePaymentCommandHandler
- ✅ Payment not authorized → ConflictException
- ✅ Capture amount > authorized → ValidationException
- ✅ Gateway success → Captured
- ✅ Gateway failure → CaptureFailedEvent

### CreatePayoutCommandHandler
- ✅ Provider not found → NotFoundException
- ✅ No payments in period → EmptyPayoutException
- ✅ Success → PayoutCreated + PayoutScheduled events

### ActivateProviderCommandHandler
- ✅ Provider not found → NotFoundException
- ✅ Already active → No change
- ✅ Success → ProviderActivatedEvent

### UploadGalleryImagesCommandHandler
- ✅ Exceeds max images (10) → ValidationException
- ✅ File size > 10MB → ValidationException
- ✅ Invalid format → ValidationException
- ✅ Success → Images uploaded, GalleryImageUploadedEvent

### AddStaffCommandHandler
- ✅ Provider not found → NotFoundException
- ✅ Duplicate email → ConflictException
- ✅ Success → StaffAddedEvent

### UpdateBusinessHoursCommandHandler
- ✅ Provider not found → NotFoundException
- ✅ Invalid time range → ValidationException
- ✅ Overlapping breaks → ValidationException
- ✅ Success → BusinessHoursUpdatedEvent

---

## Test Coverage Requirements

### For Each Command
1. ✅ Happy path
2. ✅ All validation failures
3. ✅ Not found scenarios
4. ✅ Conflict scenarios
5. ✅ Authorization failures
6. ✅ Events published correctly
7. ✅ Idempotency where applicable

### For Each Query
1. ✅ Happy path with results
2. ✅ Empty results
3. ✅ Not found scenarios
4. ✅ Pagination (if applicable)
5. ✅ Filtering (if applicable)
6. ✅ Sorting (if applicable)

### For Each Event Handler
1. ✅ Event received and processed
2. ✅ Side effects executed
3. ✅ Notifications sent (if applicable)
4. ✅ State changes persisted
5. ✅ Error handling

### For Each Validator
1. ✅ Valid inputs pass
2. ✅ Each validation rule fails independently
3. ✅ Multiple validation failures
4. ✅ Edge cases (min/max values, boundaries)

---

## Total Coverage Needed

- **Commands:** 59 handlers × 7 scenarios each = **413 scenarios**
- **Queries:** 25 handlers × 4 scenarios each = **100 scenarios**
- **Events:** 50 events × 2 scenarios each = **100 scenarios**
- **Validators:** 20 validators × 4 scenarios each = **80 scenarios**

**TOTAL:** ~**693 test scenarios** for complete CQRS coverage

---

## Implementation Strategy

### Phase 1: Command Coverage
- Create feature files for each command group
- Test all paths including validations
- Verify events published

### Phase 2: Query Coverage
- Create feature files for each query group
- Test filtering, pagination, sorting
- Test empty and not found cases

### Phase 3: Event Handler Coverage
- Create feature files for event processing
- Test side effects and notifications
- Test integration between events

### Phase 4: Validator Coverage
- Create feature files for validation rules
- Test each rule independently
- Test combination scenarios

### Phase 5: Integration Coverage
- Test complete workflows end-to-end
- Test event chains
- Test cross-aggregate scenarios

---

**This inventory ensures COMPLETE coverage of all CQRS components.**
