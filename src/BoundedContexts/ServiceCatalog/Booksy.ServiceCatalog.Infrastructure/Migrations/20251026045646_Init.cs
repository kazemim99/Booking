using System;
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

            migrationBuilder.EnsureSchema(
                name: "servicecatalog");

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
                    BusinessSocialMedia = table.Column<string>(type: "jsonb", nullable: true),
                    BusinessTags = table.Column<string>(type: "jsonb", nullable: true),
                    BusinessProfileLastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Profile_IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Profile_CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Profile_CreatedBy = table.Column<string>(type: "text", nullable: true),
                    Profile_LastModifiedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Profile_LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProviderType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    PrimaryPhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PrimaryPhoneCountryCode = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    SecondaryPhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    SecondaryPhoneCountryCode = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    Website = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AddressStreet = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AddressCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AddressState = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AddressPostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AddressCountry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AddressLatitude = table.Column<double>(type: "double precision", precision: 10, scale: 8, nullable: true),
                    AddressLongitude = table.Column<double>(type: "double precision", precision: 11, scale: 8, nullable: true),
                    RequiresApproval = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AllowOnlineBooking = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    OffersMobileServices = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ActivatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastActiveAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AverageRating = table.Column<decimal>(type: "numeric", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Providers", x => x.Id);
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
                        name: "FK_ProviderHolidays_Providers_ProviderId",
                        column: x => x.ProviderId,
                        principalSchema: "ServiceCatalog",
                        principalTable: "Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Services",
                schema: "servicecatalog",
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
                schema: "servicecatalog",
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
                schema: "servicecatalog",
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
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceOptions_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalSchema: "servicecatalog",
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServicePriceTiers",
                schema: "servicecatalog",
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
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePriceTiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServicePriceTiers_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalSchema: "servicecatalog",
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "IX_ProviderHolidays_Date",
                schema: "ServiceCatalog",
                table: "ProviderHolidays",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderHolidays_ProviderId",
                schema: "ServiceCatalog",
                table: "ProviderHolidays",
                column: "ProviderId");

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
                name: "IX_ServiceOptions_Active_SortOrder",
                schema: "servicecatalog",
                table: "ServiceOptions",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOptions_IsActive",
                schema: "servicecatalog",
                table: "ServiceOptions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOptions_ServiceId",
                schema: "servicecatalog",
                table: "ServiceOptions",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOptions_SortOrder",
                schema: "servicecatalog",
                table: "ServiceOptions",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_ServicePriceTiers_ServiceId_IsActive",
                schema: "servicecatalog",
                table: "ServicePriceTiers",
                columns: new[] { "ServiceId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Services_CreatedAt",
                schema: "servicecatalog",
                table: "Services",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Services_ProviderId",
                schema: "servicecatalog",
                table: "Services",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Services_ProviderId_Name",
                schema: "servicecatalog",
                table: "Services",
                columns: new[] { "ProviderId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Services_ProviderId1",
                schema: "servicecatalog",
                table: "Services",
                column: "ProviderId1");

            migrationBuilder.CreateIndex(
                name: "IX_Services_Status",
                schema: "servicecatalog",
                table: "Services",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Services_Status_AllowOnlineBooking",
                schema: "servicecatalog",
                table: "Services",
                columns: new[] { "Status", "AllowOnlineBooking" });

            migrationBuilder.CreateIndex(
                name: "IX_Services_Type",
                schema: "servicecatalog",
                table: "Services",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Staff_CreatedAt",
                schema: "servicecatalog",
                table: "Staff",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Staff_Email",
                schema: "servicecatalog",
                table: "Staff",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Staff_IsActive",
                schema: "servicecatalog",
                table: "Staff",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Staff_Name",
                schema: "servicecatalog",
                table: "Staff",
                columns: new[] { "FirstName", "LastName" });

            migrationBuilder.CreateIndex(
                name: "IX_Staff_ProviderId",
                schema: "servicecatalog",
                table: "Staff",
                column: "ProviderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BreakPeriods",
                schema: "ServiceCatalog");

            migrationBuilder.DropTable(
                name: "ProviderExceptions",
                schema: "ServiceCatalog");

            migrationBuilder.DropTable(
                name: "ProviderHolidays",
                schema: "ServiceCatalog");

            migrationBuilder.DropTable(
                name: "ServiceOptions",
                schema: "servicecatalog");

            migrationBuilder.DropTable(
                name: "ServicePriceTiers",
                schema: "servicecatalog");

            migrationBuilder.DropTable(
                name: "Staff",
                schema: "servicecatalog");

            migrationBuilder.DropTable(
                name: "BusinessHours",
                schema: "ServiceCatalog");

            migrationBuilder.DropTable(
                name: "Services",
                schema: "servicecatalog");

            migrationBuilder.DropTable(
                name: "Providers",
                schema: "ServiceCatalog");
        }
    }
}
