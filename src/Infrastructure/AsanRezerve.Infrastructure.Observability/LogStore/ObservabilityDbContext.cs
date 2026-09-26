using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AsanRezerve.Infrastructure.Observability.LogStore;

/// <summary>
/// Schema <c>observability</c>: stored log events and log-level overrides. Its own context and migrations history,
/// beside <c>user_management</c> and <c>ServiceCatalog</c> in the one database.
/// </summary>
public sealed class ObservabilityDbContext(DbContextOptions<ObservabilityDbContext> options) : DbContext(options)
{
    public const string Schema = "observability";

    public DbSet<LogEventRecord> LogEvents => Set<LogEventRecord>();

    public DbSet<LogLevelOverrideRecord> LogLevelOverrides => Set<LogLevelOverrideRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<LogEventRecord>(e =>
        {
            e.ToTable("log_events");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            e.Property(x => x.Timestamp).HasColumnName("timestamp");
            e.Property(x => x.Level).HasColumnName("level");
            e.Property(x => x.Message).HasColumnName("message");
            e.Property(x => x.MessageTemplate).HasColumnName("message_template");
            e.Property(x => x.Exception).HasColumnName("exception");
            e.Property(x => x.SourceContext).HasColumnName("source_context").HasMaxLength(300);
            e.Property(x => x.TraceId).HasColumnName("trace_id").HasMaxLength(32);
            e.Property(x => x.SpanId).HasColumnName("span_id").HasMaxLength(16);
            e.Property(x => x.RequestPath).HasColumnName("request_path").HasMaxLength(500);
            e.Property(x => x.RouteTemplate).HasColumnName("route_template").HasMaxLength(300);
            e.Property(x => x.StatusCode).HasColumnName("status_code");
            e.Property(x => x.ElapsedMs).HasColumnName("elapsed_ms");
            e.Property(x => x.UserId).HasColumnName("user_id").HasMaxLength(100);
            e.Property(x => x.Properties).HasColumnName("properties").HasColumnType("jsonb");

            e.HasIndex(x => x.Timestamp).HasDatabaseName("ix_log_events_timestamp");
            e.HasIndex(x => new { x.Level, x.Timestamp }).HasDatabaseName("ix_log_events_level_timestamp");
            e.HasIndex(x => x.TraceId).HasDatabaseName("ix_log_events_trace_id");
            e.HasIndex(x => new { x.SourceContext, x.Timestamp }).HasDatabaseName("ix_log_events_source_timestamp");
        });

        modelBuilder.Entity<LogLevelOverrideRecord>(e =>
        {
            e.ToTable("log_level_overrides");
            e.HasKey(x => x.Category);
            e.Property(x => x.Category).HasColumnName("category").HasMaxLength(200);
            e.Property(x => x.Level).HasColumnName("level").HasMaxLength(20);
            e.Property(x => x.ExpiresAt).HasColumnName("expires_at");
            e.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasMaxLength(200);
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });
    }
}

/// <summary>For <c>dotnet ef migrations add</c>; the running host configures the context itself.</summary>
public sealed class ObservabilityDbContextDesignTimeFactory : IDesignTimeDbContextFactory<ObservabilityDbContext>
{
    public ObservabilityDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<ObservabilityDbContext>()
            .UseNpgsql("Host=localhost;Database=design_time", npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", ObservabilityDbContext.Schema))
            .Options);
}
