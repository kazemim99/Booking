using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AsanRezerve.Infrastructure.Observability.LogStore.Migrations
{
    /// <inheritdoc />
    public partial class InitialObservabilityStore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "observability");

            migrationBuilder.CreateTable(
                name: "log_events",
                schema: "observability",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    level = table.Column<short>(type: "smallint", nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    message_template = table.Column<string>(type: "text", nullable: true),
                    exception = table.Column<string>(type: "text", nullable: true),
                    source_context = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    trace_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    span_id = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    request_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    route_template = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    status_code = table.Column<int>(type: "integer", nullable: true),
                    elapsed_ms = table.Column<double>(type: "double precision", nullable: true),
                    user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    properties = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_log_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "log_level_overrides",
                schema: "observability",
                columns: table => new
                {
                    category = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_log_level_overrides", x => x.category);
                });

            migrationBuilder.CreateIndex(
                name: "ix_log_events_level_timestamp",
                schema: "observability",
                table: "log_events",
                columns: new[] { "level", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "ix_log_events_source_timestamp",
                schema: "observability",
                table: "log_events",
                columns: new[] { "source_context", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "ix_log_events_timestamp",
                schema: "observability",
                table: "log_events",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "ix_log_events_trace_id",
                schema: "observability",
                table: "log_events",
                column: "trace_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "log_events",
                schema: "observability");

            migrationBuilder.DropTable(
                name: "log_level_overrides",
                schema: "observability");
        }
    }
}
