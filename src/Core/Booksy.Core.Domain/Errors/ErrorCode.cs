// ========================================
// Booksy.Core.Domain/Errors/ErrorCode.cs
// ========================================

namespace Booksy.Core.Domain.Errors
{
    /// <summary>
    /// Standardized error codes for all domain exceptions
    /// Categorized by domain area with numeric ranges
    /// </summary>
    public enum ErrorCode
    {
        // ========================================
        // Generic Errors (0-999)
        // ========================================

        /// <summary>Unknown error occurred</summary>
        UNKNOWN_ERROR = 0,

        /// <summary>Validation error</summary>
        VALIDATION_ERROR = 1,

        /// <summary>Resource not found</summary>
        NOT_FOUND = 2,

        /// <summary>Unauthorized access</summary>
        UNAUTHORIZED = 3,

        /// <summary>Forbidden access</summary>
        FORBIDDEN = 4,

        /// <summary>Conflict with existing resource</summary>
        CONFLICT = 5,

        /// <summary>Internal server error</summary>
        INTERNAL_ERROR = 6,

        /// <summary>External service error</summary>
        EXTERNAL_SERVICE_ERROR = 7,

        /// <summary>Business rule violation</summary>
        BUSINESS_RULE_VIOLATION = 8,

        /// <summary>Invalid aggregate state</summary>
        INVALID_AGGREGATE_STATE = 9,

        /// <summary>Concurrency error</summary>
        CONCURRENCY_ERROR = 10,

        /// <summary>Duplicate entity</summary>
        DUPLICATE_ENTITY = 11,

        /// <summary>Operation not allowed</summary>
        OPERATION_NOT_ALLOWED = 12,

        /// <summary>Resource limit exceeded</summary>
        RESOURCE_LIMIT_EXCEEDED = 13,

        /// <summary>Invariant violation</summary>
        INVARIANT_VIOLATION = 14,

        // ========================================
        // User Errors (1000-1999)
        // ========================================

        /// <summary>User not found</summary>
        USER_NOT_FOUND = 1000,

        /// <summary>User already exists</summary>
        USER_ALREADY_EXISTS = 1001,

        /// <summary>Invalid credentials</summary>
        INVALID_CREDENTIALS = 1002,

        /// <summary>Invalid two-factor authentication code</summary>
        INVALID_TWO_FACTOR_CODE = 1003,

        /// <summary>Invalid user profile</summary>
        INVALID_USER_PROFILE = 1004,

        /// <summary>User account locked</summary>
        USER_ACCOUNT_LOCKED = 1005,

        /// <summary>User email not verified</summary>
        USER_EMAIL_NOT_VERIFIED = 1006,

        /// <summary>User account disabled</summary>
        USER_ACCOUNT_DISABLED = 1007,

        // ========================================
        // Provider Errors (2000-2999)
        // ========================================

        /// <summary>Provider not found</summary>
        PROVIDER_NOT_FOUND = 2000,

        /// <summary>Provider inactive</summary>
        PROVIDER_INACTIVE = 2001,

        /// <summary>Provider already exists</summary>
        PROVIDER_ALREADY_EXISTS = 2002,

        /// <summary>Invalid provider</summary>
        INVALID_PROVIDER = 2003,

        /// <summary>Provider not active</summary>
        PROVIDER_NOT_ACTIVE = 2004,

        /// <summary>Business hours conflict</summary>
        BUSINESS_HOURS_CONFLICT = 2005,

        // ========================================
        // Booking Errors (3000-3999)
        // ========================================

        /// <summary>Booking not found</summary>
        BOOKING_NOT_FOUND = 3000,

        /// <summary>Time slot unavailable</summary>
        BOOKING_TIMESLOT_UNAVAILABLE = 3001,

        /// <summary>Booking already confirmed</summary>
        BOOKING_ALREADY_CONFIRMED = 3002,

        /// <summary>Booking already cancelled</summary>
        BOOKING_ALREADY_CANCELLED = 3003,

        /// <summary>Booking cannot be cancelled</summary>
        BOOKING_CANNOT_BE_CANCELLED = 3004,

        /// <summary>Invalid booking date</summary>
        BOOKING_INVALID_DATE = 3005,

        /// <summary>Booking overlaps with existing</summary>
        BOOKING_OVERLAP = 3006,

        // ========================================
        // Payment Errors (4000-4999)
        // ========================================

        /// <summary>Payment failed</summary>
        PAYMENT_FAILED = 4000,

        /// <summary>Invalid payment amount</summary>
        PAYMENT_INVALID_AMOUNT = 4001,

        /// <summary>Payment method not supported</summary>
        PAYMENT_METHOD_NOT_SUPPORTED = 4002,

        /// <summary>Payment already processed</summary>
        PAYMENT_ALREADY_PROCESSED = 4003,

        /// <summary>Payment refund failed</summary>
        PAYMENT_REFUND_FAILED = 4004,

        /// <summary>Payment not found</summary>
        PAYMENT_NOT_FOUND = 4005,

        // ========================================
        // Service Errors (5000-5999)
        // ========================================

        /// <summary>Service not found</summary>
        SERVICE_NOT_FOUND = 5000,

        /// <summary>Service inactive</summary>
        SERVICE_INACTIVE = 5001,

        /// <summary>Service not available</summary>
        SERVICE_NOT_AVAILABLE = 5002,

        /// <summary>Invalid service</summary>
        INVALID_SERVICE = 5003,

        /// <summary>Invalid service price</summary>
        INVALID_SERVICE_PRICE = 5004,

        /// <summary>Service category mismatch</summary>
        SERVICE_CATEGORY_MISMATCH = 5005,

        // ========================================
        // Staff Errors (6000-6999)
        // ========================================

        /// <summary>Staff not found</summary>
        STAFF_NOT_FOUND = 6000,

        /// <summary>Staff inactive</summary>
        STAFF_INACTIVE = 6001,

        /// <summary>Staff not available</summary>
        STAFF_NOT_AVAILABLE = 6002,

        /// <summary>Staff already assigned</summary>
        STAFF_ALREADY_ASSIGNED = 6003,

        // ========================================
        // Review Errors (7000-7999)
        // ========================================

        /// <summary>Review not found</summary>
        REVIEW_NOT_FOUND = 7000,

        /// <summary>Review already exists</summary>
        REVIEW_ALREADY_EXISTS = 7001,

        /// <summary>Cannot review own service</summary>
        REVIEW_CANNOT_REVIEW_OWN_SERVICE = 7002,

        /// <summary>Review not allowed</summary>
        REVIEW_NOT_ALLOWED = 7003
    }
}
