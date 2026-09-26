using System;
using Microsoft.EntityFrameworkCore.Migrations;

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

            // EF Core cannot declare a partitioned table, so log_events is written by hand: the same columns and key as
            // the model (see the snapshot), partitioned by UTC day on timestamp (design D6). Day partitions are created
            // and dropped at run time by LogPartitions; the default partition holds any event whose day has none yet.
            migrationBuilder.Sql("""
                CREATE TABLE observability.log_events (
                    id bigint GENERATED ALWAYS AS IDENTITY,
                    "timestamp" timestamp with time zone NOT NULL,
                    level smallint NOT NULL,
                    message text NOT NULL,
                    message_template text NULL,
                    exception text NULL,
                    source_context character varying(300) NULL,
                    trace_id character varying(32) NULL,
                    span_id character varying(16) NULL,
                    request_path character varying(500) NULL,
                    route_template character varying(300) NULL,
                    status_code integer NULL,
                    elapsed_ms double precision NULL,
                    user_id character varying(100) NULL,
                    properties jsonb NULL,
                    CONSTRAINT "PK_log_events" PRIMARY KEY ("timestamp", id)
                ) PARTITION BY RANGE ("timestamp");

                CREATE TABLE observability.log_events_default PARTITION OF observability.log_events DEFAULT;
                """);

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
                name: "ix_log_events_id",
                schema: "observability",
                table: "log_events",
                column: "id");

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
