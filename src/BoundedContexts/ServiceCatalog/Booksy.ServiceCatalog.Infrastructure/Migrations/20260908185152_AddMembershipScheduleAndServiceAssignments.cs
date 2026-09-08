using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Booksy.ServiceCatalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMembershipScheduleAndServiceAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "service_ids",
                schema: "ServiceCatalog",
                table: "staff_profiles",
                type: "jsonb",
                nullable: false,
                // EF scaffolds "" for a non-nullable string column, which is not valid
                // jsonb — the ALTER would fail on the first existing staff_profiles row.
                // An empty array is the domain default: "performs every service".
                defaultValue: "[]");

            migrationBuilder.CreateTable(
                name: "staff_working_days",
                schema: "ServiceCatalog",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    day_of_week = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    membership_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff_working_days", x => x.id);
                    table.ForeignKey(
                        name: "FK_staff_working_days_staff_profiles_membership_id",
                        column: x => x.membership_id,
                        principalSchema: "ServiceCatalog",
                        principalTable: "staff_profiles",
                        principalColumn: "membership_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_staff_working_days_membership",
                schema: "ServiceCatalog",
                table: "staff_working_days",
                column: "membership_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "staff_working_days",
                schema: "ServiceCatalog");

            migrationBuilder.DropColumn(
                name: "service_ids",
                schema: "ServiceCatalog",
                table: "staff_profiles");
        }
    }
}
