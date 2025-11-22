# ViewModel to QueryResult Renaming Plan

## Problem

ViewModels use generic names like `ProviderDetailsViewModel` which don't clearly indicate:
- Which query they belong to
- Their purpose in the CQRS pattern
- The specific use case

## Solution

Rename ViewModels to follow the pattern: `{QueryName}Result` or `{QueryName}Item` for nested types.

## Naming Convention

| Pattern | Usage | Example |
|---------|-------|---------|
| `{QueryName}Result` | Main query result | `GetProviderByIdResult` |
| `{QueryName}Item` | Nested item in result | `GetProviderByIdServiceItem` |
| `{Purpose}Info` | Reusable info object | `AddressInfo`, `ContactInfo` |

## Proposed Renamings

### Provider Queries

| Old Name | New Name | Query |
|----------|----------|-------|
| `ProviderDetailsViewModel` | `GetProviderByIdResult` | GetProviderById |
| `AddressViewModel` | `AddressInfo` | (shared) |
| `BusinessHoursViewModel` | ~~DELETE~~ Use `BusinessHoursDto` | GetProviderById |
| `StaffViewModel` | `GetProviderByIdStaffItem` | GetProviderById |
| `ServiceSummaryViewModel` | `GetProviderByIdServiceItem` | GetProviderById |
| `ProviderProfileViewModel` | `GetProviderProfileResult` | GetProviderProfile |
| `ProviderListViewModel` | `GetProvidersByStatusResult` | GetProvidersByStatus |
| `ProviderLocationViewModel` | `GetProvidersByLocationItem` | GetProvidersByLocation |
| `ProviderStatisticsViewModel` | `GetProviderStatisticsResult` | GetProviderStatistics |
| `ProviderDetailsViewModel` (SearchProviders) | `SearchProvidersItem` | SearchProviders |

### Service Queries

| Old Name | New Name | Query |
|----------|----------|-------|
| `ServiceDetailsViewModel` | `GetServiceByIdResult` | GetServiceById |
| `ServiceOptionViewModel` | `GetServiceByIdOptionItem` | GetServiceById |
| `ServiceStatisticsViewModel` | `GetServiceStatisticsResult` | GetServiceStatistics |
| `ServicesByProviderViewModel` | `GetServicesByProviderResult` | GetServicesByProvider |
| `SearchServicesViewModel` | `SearchServicesResult` | SearchServices |
| `ServiceAvailabilityViewModel` | `GetServiceAvailabilityResult` | GetServiceAvailability |
| `AvailabilitySlotViewModel` | `GetServiceAvailabilitySlotItem` | GetServiceAvailability |

### Payment Queries

| Old Name | New Name | Query |
|----------|----------|-------|
| `PaymentDetailsViewModel` | `GetPaymentDetailsResult` | GetPaymentDetails |
| `PaymentHistoryViewModel` | `GetCustomerPaymentHistoryResult` | GetCustomerPaymentHistory |
| `ProviderEarningsViewModel` | `GetProviderEarningsResult` | GetProviderEarnings |
| `RevenueStatisticsViewModel` | `GetProviderRevenueResult` | GetProviderRevenue |
| `ReconciliationReportViewModel` | `GetPaymentReconciliationResult` | GetPaymentReconciliation |
| `PricingCalculationViewModel` | `CalculatePricingResult` | CalculatePricing |

### Booking Queries

| Old Name | New Name | Query |
|----------|----------|-------|
| `BookingDetailsViewModel` | `GetBookingDetailsResult` | GetBookingDetails |

### Notification Queries

| Old Name | New Name | Query |
|----------|----------|-------|
| `NotificationHistoryViewModel` | `GetNotificationHistoryResult` | GetNotificationHistory |
| `NotificationAnalyticsViewModel` | `GetNotificationAnalyticsResult` | GetNotificationAnalytics |
| `UserPreferencesViewModel` | `GetUserPreferencesResult` | GetUserPreferences |
| `DeliveryStatusViewModel` | `GetDeliveryStatusResult` | GetDeliveryStatus |

## Implementation Priority

### Phase 1: Core Provider Queries (High Impact)
- [x] GetProviderById
  - ProviderDetailsViewModel → GetProviderByIdResult
  - BusinessHoursViewModel → DELETE (use BusinessHoursDto)
  - StaffViewModel → GetProviderByIdStaffItem
  - ServiceSummaryViewModel → GetProviderByIdServiceItem
  - AddressViewModel → AddressInfo

### Phase 2: Search & List Queries
- [ ] SearchProviders
- [ ] GetProvidersByStatus
- [ ] GetProviderProfile

### Phase 3: Service Queries
- [ ] GetServiceById
- [ ] GetServiceStatistics
- [ ] SearchServices

### Phase 4: Payment & Booking
- [ ] Payment queries
- [ ] Booking queries

### Phase 5: Notifications
- [ ] Notification queries

## Benefits

✅ **Clear Intent** - Name shows exactly which query returns this data
✅ **CQRS Alignment** - Follows command/query result pattern
✅ **Better IntelliSense** - Easy to find the result type for a query
✅ **Self-Documenting** - No ambiguity about purpose
✅ **Easier Refactoring** - Clear dependencies between queries and results

## Example Before/After

### Before (Ambiguous)
```csharp
public class GetProviderByIdQueryHandler : IQueryHandler<GetProviderByIdQuery, ProviderDetailsViewModel>
{
    public async Task<ProviderDetailsViewModel> Handle(...)
    {
        return new ProviderDetailsViewModel { ... };
    }
}
```

### After (Clear)
```csharp
public class GetProviderByIdQueryHandler : IQueryHandler<GetProviderByIdQuery, GetProviderByIdResult>
{
    public async Task<GetProviderByIdResult> Handle(...)
    {
        return new GetProviderByIdResult { ... };
    }
}
```

## Implementation Strategy

1. Rename ViewModel file
2. Rename class/record inside file
3. Update query handler return type
4. Update query interface return type
5. Update all usages (controllers, services, etc.)
6. Build and test

## Note on BusinessHoursViewModel

`BusinessHoursViewModel` should be **deleted** and replaced with `BusinessHoursDto` from the Application layer, which now has break support.

This eliminates duplication and provides a single source of truth.
