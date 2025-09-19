using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Exceptions;

/// <summary>
/// Exception thrown when there is a service category mismatch or validation error
/// </summary>
public sealed class ServiceCategoryMismatchException : DomainException
{
    public ServiceId? ServiceId { get; }
    public ServiceCategory? AttemptedCategory { get; }
    public ServiceCategory? RequiredCategory { get; }
    public ServiceCategory? CurrentCategory { get; }
    public ProviderId? ProviderId { get; }
    public CategoryMismatchType MismatchType { get; }
    public string? ValidationRule { get; }

    /// <summary>
    /// Initializes a new instance for category assignment mismatch
    /// </summary>
    public ServiceCategoryMismatchException(
        ServiceId serviceId,
        ServiceCategory attemptedCategory,
        ServiceCategory requiredCategory,
        ProviderId? providerId = null)
        : base($"Service '{serviceId}' cannot be assigned to category '{attemptedCategory}'. Required category: '{requiredCategory}'.")
    {
        ServiceId = serviceId;
        AttemptedCategory = attemptedCategory;
        RequiredCategory = requiredCategory;
        ProviderId = providerId;
        MismatchType = CategoryMismatchType.AssignmentMismatch;

        Data.Add("ServiceId", serviceId.ToString());
        Data.Add("AttemptedCategory", attemptedCategory.ToString());
        Data.Add("RequiredCategory", requiredCategory.ToString());
        Data.Add("MismatchType", MismatchType.ToString());
        if (providerId != null)
            Data.Add("ProviderId", providerId.ToString());
    }

    /// <summary>
    /// Initializes a new instance for category change validation
    /// </summary>
    public ServiceCategoryMismatchException(
        ServiceId serviceId,
        ServiceCategory currentCategory,
        ServiceCategory attemptedCategory,
        string validationRule)
        : base($"Service '{serviceId}' cannot be moved from category '{currentCategory}' to '{attemptedCategory}'. Validation rule: {validationRule}")
    {
        ServiceId = serviceId;
        CurrentCategory = currentCategory;
        AttemptedCategory = attemptedCategory;
        ValidationRule = validationRule;
        MismatchType = CategoryMismatchType.CategoryChangeViolation;

        Data.Add("ServiceId", serviceId.ToString());
        Data.Add("CurrentCategory", currentCategory.ToString());
        Data.Add("AttemptedCategory", attemptedCategory.ToString());
        Data.Add("ValidationRule", validationRule);
        Data.Add("MismatchType", MismatchType.ToString());
    }

    /// <summary>
    /// Initializes a new instance for provider category restriction
    /// </summary>
    public ServiceCategoryMismatchException(
        ProviderId providerId,
        ServiceCategory attemptedCategory,
        IEnumerable<ServiceCategory> allowedCategories)
        : base($"Provider '{providerId}' is not authorized to offer services in category '{attemptedCategory}'. Allowed categories: {string.Join(", ", allowedCategories)}")
    {
        ProviderId = providerId;
        AttemptedCategory = attemptedCategory;
        MismatchType = CategoryMismatchType.ProviderRestriction;

        Data.Add("ProviderId", providerId.ToString());
        Data.Add("AttemptedCategory", attemptedCategory.ToString());
        Data.Add("AllowedCategories", string.Join(",", allowedCategories));
        Data.Add("MismatchType", MismatchType.ToString());
    }

    /// <summary>
    /// Initializes a new instance with custom message
    /// </summary>
    public ServiceCategoryMismatchException(string message, CategoryMismatchType mismatchType = CategoryMismatchType.General)
        : base(message)
    {
        MismatchType = mismatchType;
        Data.Add("MismatchType", MismatchType.ToString());
    }

    /// <summary>
    /// Initializes a new instance with custom message and inner exception
    /// </summary>
    public ServiceCategoryMismatchException(string message, Exception innerException, CategoryMismatchType mismatchType = CategoryMismatchType.General)
        : base(message, innerException)
    {
        MismatchType = mismatchType;
        Data.Add("MismatchType", MismatchType.ToString());
    }

    /// <summary>
    /// Creates exception for incompatible service attributes
    /// </summary>
    public static ServiceCategoryMismatchException IncompatibleAttributes(
        ServiceId serviceId,
        ServiceCategory category,
        string conflictingAttribute,
        string expectedValue,
        string actualValue)
    {
        var exception = new ServiceCategoryMismatchException(
            $"Service '{serviceId}' has incompatible attribute '{conflictingAttribute}' for category '{category}'. Expected: '{expectedValue}', Actual: '{actualValue}'.",
            CategoryMismatchType.AttributeIncompatibility);

        exception.Data.Add("ServiceId", serviceId.ToString());
        exception.Data.Add("Category", category.ToString());
        exception.Data.Add("ConflictingAttribute", conflictingAttribute);
        exception.Data.Add("ExpectedValue", expectedValue);
        exception.Data.Add("ActualValue", actualValue);

        return exception;
    }

    /// <summary>
    /// Creates exception for pricing model mismatch
    /// </summary>
    public static ServiceCategoryMismatchException PricingModelMismatch(
        ServiceId serviceId,
        ServiceCategory category,
        string attemptedPricingModel,
        IEnumerable<string> allowedPricingModels)
    {
        var exception = new ServiceCategoryMismatchException(
            $"Service '{serviceId}' in category '{category}' cannot use pricing model '{attemptedPricingModel}'. Allowed models: {string.Join(", ", allowedPricingModels)}",
            CategoryMismatchType.PricingModelMismatch);

        exception.Data.Add("ServiceId", serviceId.ToString());
        exception.Data.Add("Category", category.ToString());
        exception.Data.Add("AttemptedPricingModel", attemptedPricingModel);
        exception.Data.Add("AllowedPricingModels", string.Join(",", allowedPricingModels));

        return exception;
    }

    /// <summary>
    /// Creates exception for duration constraints
    /// </summary>
    public static ServiceCategoryMismatchException DurationConstraintViolation(
        ServiceId serviceId,
        ServiceCategory category,
        TimeSpan attemptedDuration,
        TimeSpan minDuration,
        TimeSpan maxDuration)
    {
        var exception = new ServiceCategoryMismatchException(
            $"Service '{serviceId}' duration {attemptedDuration} violates category '{category}' constraints. Duration must be between {minDuration} and {maxDuration}.",
            CategoryMismatchType.DurationConstraintViolation);

        exception.Data.Add("ServiceId", serviceId.ToString());
        exception.Data.Add("Category", category.ToString());
        exception.Data.Add("AttemptedDuration", attemptedDuration.ToString());
        exception.Data.Add("MinDuration", minDuration.ToString());
        exception.Data.Add("MaxDuration", maxDuration.ToString());

        return exception;
    }

    /// <summary>
    /// Creates exception for qualification requirements
    /// </summary>
    public static ServiceCategoryMismatchException QualificationRequirementMismatch(
        ServiceId serviceId,
        ServiceCategory category,
        IEnumerable<string> requiredQualifications,
        IEnumerable<string> availableQualifications)
    {
        var exception = new ServiceCategoryMismatchException(
            $"Service '{serviceId}' cannot be offered in category '{category}' due to missing qualifications. Required: {string.Join(", ", requiredQualifications)}. Available: {string.Join(", ", availableQualifications)}",
            CategoryMismatchType.QualificationMismatch);

        exception.Data.Add("ServiceId", serviceId.ToString());
        exception.Data.Add("Category", category.ToString());
        exception.Data.Add("RequiredQualifications", string.Join(",", requiredQualifications));
        exception.Data.Add("AvailableQualifications", string.Join(",", availableQualifications));

        return exception;
    }

    /// <summary>
    /// Creates exception for license requirements
    /// </summary>
    public static ServiceCategoryMismatchException LicenseRequirementMismatch(
        ServiceId serviceId,
        ServiceCategory category,
        string requiredLicenseType,
        string? currentLicenseType = null)
    {
        var message = currentLicenseType != null
            ? $"Service '{serviceId}' requires license type '{requiredLicenseType}' for category '{category}', but provider has '{currentLicenseType}'."
            : $"Service '{serviceId}' requires license type '{requiredLicenseType}' for category '{category}', but no license is available.";

        var exception = new ServiceCategoryMismatchException(message, CategoryMismatchType.LicenseRequirementMismatch);

        exception.Data.Add("ServiceId", serviceId.ToString());
        exception.Data.Add("Category", category.ToString());
        exception.Data.Add("RequiredLicenseType", requiredLicenseType);
        if (currentLicenseType != null)
            exception.Data.Add("CurrentLicenseType", currentLicenseType);

        return exception;
    }

    /// <summary>
    /// Creates exception for equipment requirements
    /// </summary>
    public static ServiceCategoryMismatchException EquipmentRequirementMismatch(
        ServiceId serviceId,
        ServiceCategory category,
        IEnumerable<string> requiredEquipment,
        IEnumerable<string> availableEquipment)
    {
        var exception = new ServiceCategoryMismatchException(
            $"Service '{serviceId}' cannot be offered in category '{category}' due to missing equipment. Required: {string.Join(", ", requiredEquipment)}. Available: {string.Join(", ", availableEquipment)}",
            CategoryMismatchType.EquipmentRequirementMismatch);

        exception.Data.Add("ServiceId", serviceId.ToString());
        exception.Data.Add("Category", category.ToString());
        exception.Data.Add("RequiredEquipment", string.Join(",", requiredEquipment));
        exception.Data.Add("AvailableEquipment", string.Join(",", availableEquipment));

        return exception;
    }

    /// <summary>
    /// Get error code for this exception
    /// </summary>
    public override string ErrorCode => $"CATEGORY_MISMATCH_{MismatchType.ToString().ToUpperInvariant()}";

    /// <summary>
    /// Get severity level for this exception
    /// </summary>
    public ExceptionSeverity Severity => MismatchType switch
    {
        CategoryMismatchType.LicenseRequirementMismatch => ExceptionSeverity.Critical,
        CategoryMismatchType.QualificationMismatch => ExceptionSeverity.High,
        CategoryMismatchType.ProviderRestriction => ExceptionSeverity.High,
        CategoryMismatchType.EquipmentRequirementMismatch => ExceptionSeverity.Medium,
        CategoryMismatchType.CategoryChangeViolation => ExceptionSeverity.Medium,
        CategoryMismatchType.PricingModelMismatch => ExceptionSeverity.Medium,
        CategoryMismatchType.DurationConstraintViolation => ExceptionSeverity.Low,
        CategoryMismatchType.AttributeIncompatibility => ExceptionSeverity.Low,
        _ => ExceptionSeverity.Medium
    };

    /// <summary>
    /// Get suggested actions for resolving this exception
    /// </summary>
    public IEnumerable<string> GetSuggestedActions()
    {
        return MismatchType switch
        {
            CategoryMismatchType.AssignmentMismatch => new[]
            {
                "Verify the correct category for the service",
                "Check service attributes and requirements",
                "Update service category to match requirements"
            },
            CategoryMismatchType.CategoryChangeViolation => new[]
            {
                "Review category change rules and restrictions",
                "Update service attributes to match new category",
                "Consider creating a new service instead of changing category"
            },
            CategoryMismatchType.ProviderRestriction => new[]
            {
                "Verify provider authorization for the category",
                "Update provider permissions or licenses",
                "Choose a category the provider is authorized for"
            },
            CategoryMismatchType.AttributeIncompatibility => new[]
            {
                "Update service attributes to match category requirements",
                "Choose a different category that supports the attributes",
                "Review category rules and attribute compatibility"
            },
            CategoryMismatchType.PricingModelMismatch => new[]
            {
                "Change pricing model to one allowed for the category",
                "Review category pricing requirements",
                "Consider moving to a category that supports the pricing model"
            },
            CategoryMismatchType.DurationConstraintViolation => new[]
            {
                "Adjust service duration to meet category requirements",
                "Choose a category that supports the desired duration",
                "Review category duration constraints"
            },
            CategoryMismatchType.QualificationMismatch => new[]
            {
                "Obtain required qualifications or certifications",
                "Assign qualified staff to the service",
                "Choose a category that matches available qualifications"
            },
            CategoryMismatchType.LicenseRequirementMismatch => new[]
            {
                "Obtain the required license type",
                "Update existing license to meet requirements",
                "Choose a category that matches current license"
            },
            CategoryMismatchType.EquipmentRequirementMismatch => new[]
            {
                "Acquire required equipment",
                "Update equipment inventory",
                "Choose a category that matches available equipment"
            },
            _ => new[]
            {
                "Review service and category compatibility",
                "Check all requirements and constraints",
                "Contact support for category guidance"
            }
        };
    }

    /// <summary>
    /// Check if this exception is recoverable
    /// </summary>
    public bool IsRecoverable => MismatchType switch
    {
        CategoryMismatchType.LicenseRequirementMismatch => false,
        CategoryMismatchType.QualificationMismatch => false,
        _ => true
    };

    /// <summary>
    /// Get impact level of this mismatch
    /// </summary>
    public CategoryMismatchImpact GetImpactLevel()
    {
        return MismatchType switch
        {
            CategoryMismatchType.LicenseRequirementMismatch => CategoryMismatchImpact.ServiceBlocked,
            CategoryMismatchType.QualificationMismatch => CategoryMismatchImpact.ServiceBlocked,
            CategoryMismatchType.ProviderRestriction => CategoryMismatchImpact.ServiceBlocked,
            CategoryMismatchType.EquipmentRequirementMismatch => CategoryMismatchImpact.ServiceLimited,
            CategoryMismatchType.CategoryChangeViolation => CategoryMismatchImpact.ServiceLimited,
            CategoryMismatchType.PricingModelMismatch => CategoryMismatchImpact.ConfigurationError,
            CategoryMismatchType.DurationConstraintViolation => CategoryMismatchImpact.ConfigurationError,
            CategoryMismatchType.AttributeIncompatibility => CategoryMismatchImpact.ConfigurationError,
            _ => CategoryMismatchImpact.ConfigurationError
        };
    }
}

/// <summary>
/// Types of category mismatches
/// </summary>
public enum CategoryMismatchType
{
    General,
    AssignmentMismatch,
    CategoryChangeViolation,
    ProviderRestriction,
    AttributeIncompatibility,
    PricingModelMismatch,
    DurationConstraintViolation,
    QualificationMismatch,
    LicenseRequirementMismatch,
    EquipmentRequirementMismatch
}

/// <summary>
/// Impact levels for category mismatches
/// </summary>
public enum CategoryMismatchImpact
{
    ConfigurationError,    // Can be fixed by updating configuration
    ServiceLimited,        // Service can run with limitations
    ServiceBlocked         // Service cannot be offered
}