using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

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

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:hstore", ",,");

            migrationBuilder.CreateTable(
                name: "Providers",
                schema: "ServiceCatalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Profile_Website = table.Column<string>(type: "text", nullable: true),
                    Profile_LogoUrl = table.Column<string>(type: "text", nullable: true),
                    Profile_SocialMedia = table.Column<Dictionary<string, string>>(type: "hstore", nullable: false),
                    Profile_Tags = table.Column<List<string>>(type: "text[]", nullable: false),
                    Profile_LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Profile_IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Profile_Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Profile_CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Profile_CreatedBy = table.Column<string>(type: "text", nullable: true),
                    Profile_LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Profile_LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
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
                    RegisteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastActiveAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Providers", x => x.Id);
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
                    MaxAdvanceBookingDays = table.Column<int>(type: "integer", nullable: false, defaultValue: 30),
                    MinAdvanceBookingHours = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    MaxConcurrentBookings = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Tags = table.Column<List<string>>(type: "text[]", nullable: false),
                    Metadata = table.Column<Dictionary<string, string>>(type: "hstore", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ActivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Services", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BusinessHours",
                schema: "ServiceCatalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DayOfWeek = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OpenTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    CloseTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    IsOpen = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
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
                name: "Staff",
                schema: "servicecatalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    HiredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TerminatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TerminationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
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
                name: "PriceTiers",
                schema: "ServiceCatalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PriceAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PriceCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Attributes = table.Column<Dictionary<string, string>>(type: "hstore", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceTiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceTiers_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalSchema: "ServiceCatalog",
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceOptions",
                schema: "ServiceCatalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AdditionalPriceAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AdditionalPriceCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    AdditionalDurationMinutes = table.Column<int>(type: "integer", nullable: true),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: true),
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
                name: "IX_PriceTiers_ServiceId",
                schema: "ServiceCatalog",
                table: "PriceTiers",
                column: "ServiceId");

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
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOptions_ServiceId",
                schema: "ServiceCatalog",
                table: "ServiceOptions",
                column: "ServiceId");

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
                name: "IX_Services_Status",
                schema: "ServiceCatalog",
                table: "Services",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Services_Type",
                schema: "ServiceCatalog",
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
                name: "BusinessHours",
                schema: "ServiceCatalog");

            migrationBuilder.DropTable(
                name: "PriceTiers",
                schema: "ServiceCatalog");

            migrationBuilder.DropTable(
                name: "ServiceOptions",
                schema: "ServiceCatalog");

            migrationBuilder.DropTable(
                name: "Staff",
                schema: "servicecatalog");

            migrationBuilder.DropTable(
                name: "Services",
                schema: "ServiceCatalog");

            migrationBuilder.DropTable(
                name: "Providers",
                schema: "ServiceCatalog");
        }
    }
}
