# ViewModel Naming - Final Convention

## Problem with Previous Approach

❌ **GetProviderByIdResult** - Too verbose, repeats "Get" and "ById"
❌ **GetProviderByIdStaffItem** - Very long, awkward to use

## Better Approach: Natural Result Names

✅ **{Entity}{Context}Result** - Result suffix at the END
✅ **{Entity}{Purpose}Item** - Item suffix for collections

## Revised Naming Convention

### Main Results (End with "Result")

| Query | Result Type |
|-------|-------------|
| GetProviderById | `ProviderDetailsResult` |
| GetProviderProfile | `ProviderProfileResult` |
| SearchProviders | `ProvidersSearchResult` |
| GetProviderStatistics | `ProviderStatisticsResult` |
| GetServiceById | `ServiceDetailsResult` |
| GetPaymentDetails | `PaymentDetailsResult` |
| CalculatePricing | `PricingCalculationResult` |

### Nested Items (End with "Item")

| Context | Item Type |
|---------|-----------|
| Provider's staff | `ProviderStaffItem` |
| Provider's services | `ProviderServiceItem` |
| Service options | `ServiceOptionItem` |
| Payment history | `PaymentHistoryItem` |

### Shared Info Objects (End with "Info")

| Purpose | Type |
|---------|------|
| Address | `AddressInfo` |
| Contact | `ContactInfo` |
| Business hours | Use `BusinessHoursDto` |

## Examples

### Before (Verbose)
```csharp
public sealed record GetProviderByIdQuery(...)
    : IQuery<GetProviderByIdResult?>;

public class GetProviderByIdResult
{
    public List<GetProviderByIdStaffItem> Staff { get; set; }
    public List<GetProviderByIdServiceItem> Services { get; set; }
}
```

### After (Natural)
```csharp
public sealed record GetProviderByIdQuery(...)
    : IQuery<ProviderDetailsResult?>;

public class ProviderDetailsResult
{
    public List<ProviderStaffItem> Staff { get; set; }
    public List<ProviderServiceItem> Services { get; set; }
}
```

## Implementation for GetProviderById

- `ProviderDetailsViewModel` → `ProviderDetailsResult`
- `StaffViewModel` → `ProviderStaffItem`
- `ServiceSummaryViewModel` → `ProviderServiceItem`
- `AddressViewModel` → `AddressInfo`
- `BusinessHoursViewModel` → **DELETE** (use `BusinessHoursDto`)

## Benefits

✅ **Shorter, cleaner names**
✅ **"Result" suffix is familiar** (like `ActionResult`, `Task<T>`)
✅ **More readable in code**
✅ **Follows .NET conventions**
