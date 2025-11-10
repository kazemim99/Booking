using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Booksy.ServiceCatalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ServiceCatalog");

            migrationBuilder.CreateTable(
                name: "Bookings",
                schema: "ServiceCatalog",
                columns: table => new
                {
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    StaffId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TotalPriceAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalPriceCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PaymentTotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PaymentCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    DepositAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DepositCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PaidCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    RefundedAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    RefundedCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PaymentStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PaymentIntentId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DepositPaymentIntentId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RefundId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RefundedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PolicyMinAdvanceBookingHours = table.Column<int>(type: "integer", nullable: false),
                    PolicyMaxAdvanceBookingDays = table.Column<int>(type: "integer", nullable: false),
                    PolicyCancellationWindowHours = table.Column<int>(type: "integer", nullable: false),
                    PolicyCancellationFeePercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    PolicyAllowRescheduling = table.Column<bool>(type: "boolean", nullable: false),
                    PolicyRescheduleWindowHours = table.Column<int>(type: "integer", nullable: false),
                    PolicyRequireDeposit = table.Column<bool>(type: "boolean", nullable: false),
                    PolicyDepositPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    CustomerNotes = table.Column<string>(type: "text", nullable: true),
                    StaffNotes = table.Column<string>(type: "text", nullable: true),
                    CancellationReason = table.Column<string>(type: "text", nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RescheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PreviousBookingId = table.Column<Guid>(type: "uuid", nullable: true),
                    RescheduledToBookingId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.BookingId);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                schema: "ServiceCatalog",
                columns: table => new
                {
                    NotificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    RecipientPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RecipientName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Channel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    PlainTextBody = table.Column<string>(type: "text", nullable: true),
                    TemplateId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TemplateData = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ScheduledFor = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    GatewayMessageId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: true),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: false),
                    CampaignId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BatchId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OpenedFrom = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ClickedLink = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    OpenCount = table.Column<int>(type: "integer", nullable: false),
                    ClickCount = table.Column<int>(type: "integer", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.NotificationId);
                });

            migrationBuilder.CreateTable(
                name: "NotificationTemplates",
                schema: "ServiceCatalog",
                columns: table => new
                {
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SupportedChannels = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    EmailSubjectTemplate = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EmailBodyTemplate = table.Column<string>(type: "text", nullable: true),
                    EmailPlainTextTemplate = table.Column<string>(type: "text", nullable: true),
                    SmsBodyTemplate = table.Column<string>(type: "character varying(1600)", maxLength: 1600, nullable: true),
                    PushTitleTemplate = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PushBodyTemplate = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    InAppTitleTemplate = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    InAppBodyTemplate = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TemplateVersion = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsDraft = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    RequiredVariables = table.Column<string>(type: "jsonb", nullable: false),
                    OptionalVariables = table.Column<string>(type: "jsonb", nullable: false),
                    DefaultLanguage = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true, defaultValue: "en"),
                    LocalizedVersions = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ArchivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UsageCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationTemplates", x => x.TemplateId);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                schema: "ServiceCatalog",
                columns: table => new
                {
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PaidCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    RefundedAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    RefundedCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PaymentIntentId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PaymentMethodId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    StripeCustomerId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Authority = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RefNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CardPan = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Fee = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    FeeCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    PaymentUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AuthorizedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CapturedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RefundedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.PaymentId);
                });

            migrationBuilder.CreateTable(
                name: "Payouts",
                schema: "ServiceCatalog",
                columns: table => new
                {
                    PayoutId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    GrossAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CommissionCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    NetCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BankAccountLast4 = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    BankName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ExternalPayoutId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    StripeAccountId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: false),
                    PaymentIds = table.Column<List<Guid>>(type: "jsonb", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payouts", x => x.PayoutId);
                });

            migrationBuilder.CreateTable(
                name: "Providers",
                schema: "ServiceCatalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BusinessDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    BusinessWebsite = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BusinessLogoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ProfileImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BusinessSocialMedia = table.Column<string>(type: "jsonb", nullable: true),
                    BusinessTags = table.Column<string>(type: "jsonb", nullable: true),
                    BusinessProfileLastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Profile_IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Profile_CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Profile_CreatedBy = table.Column<string>(type: "text", nullable: true),
                    Profile_LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Profile_LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProviderType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RegistrationStep = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    IsRegistrationComplete = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    PrimaryPhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PrimaryPhoneCountryCode = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    PrimaryPhoneNationalNumber = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    SecondaryPhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    SecondaryPhoneCountryCode = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    SecondaryPhoneNationalNumber = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    Website = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AddressFormattedAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AddressStreet = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AddressCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AddressState = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AddressPostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AddressCountry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AddressProvinceId = table.Column<int>(type: "integer", nullable: true),
                    AddressCityId = table.Column<int>(type: "integer", nullable: true),
                    AddressLatitude = table.Column<double>(type: "double precision", precision: 10, scale: 8, nullable: true),
                    AddressLongitude = table.Column<double>(type: "double precision", precision: 11, scale: 8, nullable: true),
                    RequiresApproval = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AllowOnlineBooking = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    OffersMobileServices = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastActiveAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AverageRating = table.Column<decimal>(type: "numeric", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Providers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProvinceCities",
                schema: "ServiceCatalog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProvinceCode = table.Column<int>(type: "integer", nullable: false),
                    CityCode = table.Column<int>(type: "integer", nullable: true),
                    ParentId = table.Column<int>(type: "integer", nullable: true),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProvinceCities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProvinceCities_ProvinceCities_ParentId",
                        column: x => x.ParentId,
                        principalSchema: "ServiceCatalog",
                        principalTable: "ProvinceCities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserNotificationPreferences",
                schema: "ServiceCatalog",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnabledChannels = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    EnabledTypes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    QuietHoursStart = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    QuietHoursEnd = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    PreferredLanguage = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true, defaultValue: "en"),
                    MarketingOptIn = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    MaxNotificationsPerDay = table.Column<int>(type: "integer", nullable: false, defaultValue: 50),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotificationPreferences", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "BookingHistoryEntry",
                schema: "ServiceCatalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingHistoryEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingHistoryEntry_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalSchema: "ServiceCatalog",
                        principalTable: "Bookings",
                        principalColumn: "BookingId");
                });

            migrationBuilder.CreateTable(
                name: "NotificationDeliveryAttempts",
                schema: "ServiceCatalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    AttemptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Channel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    GatewayMessageId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    HttpStatusCode = table.Column<int>(type: "integer", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: false),
                    NextRetryAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RetryDelaySeconds = table.Column<int>(type: "integer", nullable: false),
                    NotificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationDeliveryAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationDeliveryAttempts_Notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalSchema: "ServiceCatalog",
                        principalTable: "Notifications",
                        principalColumn: "NotificationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentTransactions",
                schema: "ServiceCatalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    ExternalTransactionId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StatusReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalSchema: "ServiceCatalog",
                        principalTable: "Payments",
                        principalColumn: "PaymentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BusinessHours",
                schema: "ServiceCatalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    OpenTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    CloseTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessHours", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessHours_Providers_ProviderId",
                        column: x => x.ProviderId,
                        principalSchema: "ServiceCatalog",
                        principalTable: "Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "provider_gallery_images",
                schema: "ServiceCatalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    thumbnail_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    medium_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    caption = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    alt_text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    uploaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_gallery_images", x => x.id);
                    table.ForeignKey(
                        name: "FK_provider_gallery_images_Providers_provider_id",
                        column: x => x.provider_id,
                        principalSchema: "ServiceCatalog",
                        principalTable: "Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProviderExceptions",
                schema: "ServiceCatalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    OpenTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    CloseTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    _providerId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderExceptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProviderExceptions_Providers__providerId",
                        column: x => x._providerId,
                        principalSchema: "ServiceCatalog",
                        principalTable: "Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProviderHolidays",
                schema: "ServiceCatalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsRecurring = table.Column<bool>(type: "boolean", nullable: false),
                    Pattern = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    _providerId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderHolidays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProviderHolidays_Providers__providerId",
                        column: x => x._providerId,
                        principalSchema: "ServiceCatalog",
                        principalTable: "Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Services",
                schema: "ServiceCatalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CategoryName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CategoryDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CategoryIconUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BasePriceAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    BasePriceCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    PreparationTimeMinutes = table.Column<int>(type: "integer", nullable: true),
                    BufferTimeMinutes = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RequiresDeposit = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DepositPercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0m),
                    AllowOnlineBooking = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    AvailableAtLocation = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    AvailableAsMobile = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    MaxAdvanceBookingDays = table.Column<int>(type: "integer", nullable: false, defaultValue: 90),
                    MinAdvanceBookingHours = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    MaxConcurrentBookings = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    BookingPolicyMinAdvanceBookingHours = table.Column<int>(type: "integer", nullable: true),
                    BookingPolicyMaxAdvanceBookingDays = table.Column<int>(type: "integer", nullable: true),
                    BookingPolicyCancellationWindowHours = table.Column<int>(type: "integer", nullable: true),
                    BookingPolicyCancellationFeePercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    BookingPolicyAllowRescheduling = table.Column<bool>(type: "boolean", nullable: true),
                    BookingPolicyRescheduleWindowHours = table.Column<int>(type: "integer", nullable: true),
                    BookingPolicyRequireDeposit = table.Column<bool>(type: "boolean", nullable: true),
                    BookingPolicyDepositPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ActivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProviderId1 = table.Column<Guid>(type: "uuid", nullable: true),
                    QualifiedStaff = table.Column<string>(type: "jsonb", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Services", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Services_Providers_ProviderId",
                        column: x => x.ProviderId,
                        principalSchema: "ServiceCatalog",
                        principalTable: "Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Services_Providers_ProviderId1",
                        column: x => x.ProviderId1,
                        principalSchema: "ServiceCatalog",
                        principalTable: "Providers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Staff",
                schema: "ServiceCatalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    HiredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TerminatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TerminationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ProfilePhotoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Biography = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Staff", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Staff_Providers_ProviderId",
                        column: x => x.ProviderId,
                        principalSchema: "ServiceCatalog",
                        principalTable: "Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BreakPeriods",
                schema: "ServiceCatalog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    Label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BusinessHoursId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BreakPeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BreakPeriods_BusinessHours_BusinessHoursId",
                        column: x => x.BusinessHoursId,
                        principalSchema: "ServiceCatalog",
                        principalTable: "BusinessHours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceOptions",
                schema: "ServiceCatalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AdditionalPriceAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AdditionalPriceCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    AdditionalDurationMinutes = table.Column<int>(type: "integer", nullable: true),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceOptions_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalSchema: "ServiceCatalog",
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServicePriceTiers",
                schema: "ServiceCatalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Attributes = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePriceTiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServicePriceTiers_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalSchema: "ServiceCatalog",
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingHistoryEntry_BookingId",
                schema: "ServiceCatalog",
                table: "BookingHistoryEntry",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ServiceId",
                schema: "ServiceCatalog",
                table: "Bookings",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_BreakPeriods_BusinessHoursId",
                schema: "ServiceCatalog",
                table: "BreakPeriods",
                column: "BusinessHoursId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessHours_DayOfWeek",
                schema: "ServiceCatalog",
                table: "BusinessHours",
                column: "DayOfWeek");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessHours_ProviderId",
                schema: "ServiceCatalog",
                table: "BusinessHours",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveryAttempts_NotificationId",
                schema: "ServiceCatalog",
                table: "NotificationDeliveryAttempts",
                column: "NotificationId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_BookingId_Type",
                schema: "ServiceCatalog",
                table: "Notifications",
                columns: new[] { "BookingId", "Type" },
                filter: "\"BookingId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Channel",
                schema: "ServiceCatalog",
                table: "Notifications",
                column: "Channel");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CreatedAt",
                schema: "ServiceCatalog",
                table: "Notifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientId",
                schema: "ServiceCatalog",
                table: "Notifications",
                column: "RecipientId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ScheduledFor",
                schema: "ServiceCatalog",
                table: "Notifications",
                column: "ScheduledFor",
                filter: "\"ScheduledFor\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Status",
                schema: "ServiceCatalog",
                table: "Notifications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Status_ScheduledFor",
                schema: "ServiceCatalog",
                table: "Notifications",
                columns: new[] { "Status", "ScheduledFor" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Type",
                schema: "ServiceCatalog",
                table: "Notifications",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplates_IsActive",
                schema: "ServiceCatalog",
                table: "NotificationTemplates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplates_Key_Version",
                schema: "ServiceCatalog",
                table: "NotificationTemplates",
                columns: new[] { "TemplateKey", "TemplateVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplates_TemplateKey",
                schema: "ServiceCatalog",
                table: "NotificationTemplates",
                column: "TemplateKey");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplates_Type_Active",
                schema: "ServiceCatalog",
                table: "NotificationTemplates",
                columns: new[] { "Type", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Authority",
                schema: "ServiceCatalog",
                table: "Payments",
                column: "Authority");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_BookingId",
                schema: "ServiceCatalog",
                table: "Payments",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CreatedAt",
                schema: "ServiceCatalog",
                table: "Payments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CustomerId",
                schema: "ServiceCatalog",
                table: "Payments",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentIntentId",
                schema: "ServiceCatalog",
                table: "Payments",
                column: "PaymentIntentId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Provider",
                schema: "ServiceCatalog",
                table: "Payments",
                column: "Provider");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ProviderId",
                schema: "ServiceCatalog",
                table: "Payments",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Status",
                schema: "ServiceCatalog",
                table: "Payments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_PaymentId",
                schema: "ServiceCatalog",
                table: "PaymentTransactions",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_Payouts_CreatedAt",
                schema: "ServiceCatalog",
                table: "Payouts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Payouts_Period",
                schema: "ServiceCatalog",
                table: "Payouts",
                columns: new[] { "PeriodStart", "PeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_Payouts_ProviderId",
                schema: "ServiceCatalog",
                table: "Payouts",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Payouts_ScheduledAt",
                schema: "ServiceCatalog",
                table: "Payouts",
                column: "ScheduledAt");

            migrationBuilder.CreateIndex(
                name: "IX_Payouts_Status",
                schema: "ServiceCatalog",
                table: "Payouts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderGalleryImages_Provider_DisplayOrder",
                schema: "ServiceCatalog",
                table: "provider_gallery_images",
                columns: new[] { "provider_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderGalleryImages_Provider_IsPrimary",
                schema: "ServiceCatalog",
                table: "provider_gallery_images",
                columns: new[] { "provider_id", "is_primary" });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderGalleryImages_ProviderId",
                schema: "ServiceCatalog",
                table: "provider_gallery_images",
                column: "provider_id");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderExceptions__providerId",
                schema: "ServiceCatalog",
                table: "ProviderExceptions",
                column: "_providerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderExceptions_Date",
                schema: "ServiceCatalog",
                table: "ProviderExceptions",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderHolidays__providerId",
                schema: "ServiceCatalog",
                table: "ProviderHolidays",
                column: "_providerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderHolidays_Date",
                schema: "ServiceCatalog",
                table: "ProviderHolidays",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_OwnerId",
                schema: "ServiceCatalog",
                table: "Providers",
                column: "OwnerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Providers_Status",
                schema: "ServiceCatalog",
                table: "Providers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_Type",
                schema: "ServiceCatalog",
                table: "Providers",
                column: "ProviderType");

            migrationBuilder.CreateIndex(
                name: "IX_ProvinceCities_ParentId",
                schema: "ServiceCatalog",
                table: "ProvinceCities",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProvinceCities_ProvinceCode",
                schema: "ServiceCatalog",
                table: "ProvinceCities",
                column: "ProvinceCode");

            migrationBuilder.CreateIndex(
                name: "IX_ProvinceCities_ProvinceCode_CityCode",
                schema: "ServiceCatalog",
                table: "ProvinceCities",
                columns: new[] { "ProvinceCode", "CityCode" });

            migrationBuilder.CreateIndex(
                name: "IX_ProvinceCities_Type",
                schema: "ServiceCatalog",
                table: "ProvinceCities",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOptions_Active_SortOrder",
                schema: "ServiceCatalog",
                table: "ServiceOptions",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOptions_IsActive",
                schema: "ServiceCatalog",
                table: "ServiceOptions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOptions_ServiceId",
                schema: "ServiceCatalog",
                table: "ServiceOptions",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOptions_SortOrder",
                schema: "ServiceCatalog",
                table: "ServiceOptions",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_ServicePriceTiers_ServiceId_IsActive",
                schema: "ServiceCatalog",
                table: "ServicePriceTiers",
                columns: new[] { "ServiceId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Services_CreatedAt",
                schema: "ServiceCatalog",
                table: "Services",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Services_ProviderId",
                schema: "ServiceCatalog",
                table: "Services",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Services_ProviderId_Name",
                schema: "ServiceCatalog",
                table: "Services",
                columns: new[] { "ProviderId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Services_ProviderId1",
                schema: "ServiceCatalog",
                table: "Services",
                column: "ProviderId1");

            migrationBuilder.CreateIndex(
                name: "IX_Services_Status",
                schema: "ServiceCatalog",
                table: "Services",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Services_Status_AllowOnlineBooking",
                schema: "ServiceCatalog",
                table: "Services",
                columns: new[] { "Status", "AllowOnlineBooking" });

            migrationBuilder.CreateIndex(
                name: "IX_Services_Type",
                schema: "ServiceCatalog",
                table: "Services",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Staff_CreatedAt",
                schema: "ServiceCatalog",
                table: "Staff",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Staff_Email",
                schema: "ServiceCatalog",
                table: "Staff",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Staff_IsActive",
                schema: "ServiceCatalog",
                table: "Staff",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Staff_Name",
                schema: "ServiceCatalog",
                table: "Staff",
                columns: new[] { "FirstName", "LastName" });

            migrationBuilder.CreateIndex(
                name: "IX_Staff_ProviderId",
                schema: "ServiceCatalog",
                table: "Staff",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotificationPreferences_LastUpdated",
                schema: "ServiceCatalog",
                table: "UserNotificationPreferences",
                column: "LastUpdated");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotificationPreferences_UserId",
                schema: "ServiceCatalog",
                table: "UserNotificationPreferences",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingHistoryEntry",
                schema: "ServiceCatalog");

            migrationBuilder.DropTable(
                name: "BreakPeriods",
                schema: "ServiceCatalog");

            migrationBuilder.DropTable(
                name: "NotificationDeliveryAttempts",
                schema: "ServiceCatalog");

            migrationBuilder.DropTable(
                name: "NotificationTemplates",
                schema: "ServiceCatalog");

            migrationBuilder.DropTable(
                name: "PaymentTransactions",
                schema: "ServiceCatalog");

            migrationBuilder.DropTable(
                name: "Payouts",
                schema: "ServiceCatalog");

            migrationBuilder.DropTable(
                name: "provider_gallery_images",
                schema: "ServiceCatalog");

            migrationBuilder.DropTable(
                name: "ProviderExceptions",
                schema: "ServiceCatalog");

            migrationBuilder.DropTable(
                name: "ProviderHolidays",
                schema: "ServiceCatalog");

            migrationBuilder.DropTable(
                name: "ProvinceCities",
                schema: "ServiceCatalog");

            migrationBuilder.DropTable(
                name: "ServiceOptions",
                schema: "ServiceCatalog");

            migrationBuilder.DropTable(
                name: "ServicePriceTiers",
                schema: "ServiceCatalog");

            migrationBuilder.DropTable(
                name: "Staff",
                schema: "ServiceCatalog");

            migrationBuilder.DropTable(
                name: "UserNotificationPreferences",
                schema: "ServiceCatalog");

            migrationBuilder.DropTable(
                name: "Bookings",
                schema: "ServiceCatalog");

            migrationBuilder.DropTable(
                name: "BusinessHours",
                schema: "ServiceCatalog");

            migrationBuilder.DropTable(
                name: "Notifications",
                schema: "ServiceCatalog");

            migrationBuilder.DropTable(
                name: "Payments",
                schema: "ServiceCatalog");

            migrationBuilder.DropTable(
                name: "Services",
                schema: "ServiceCatalog");

            migrationBuilder.DropTable(
                name: "Providers",
                schema: "ServiceCatalog");
        }
    }
}
