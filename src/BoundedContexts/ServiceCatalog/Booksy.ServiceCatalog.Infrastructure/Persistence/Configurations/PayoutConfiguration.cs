// ========================================
// Booksy.ServiceCatalog.Infrastructure/Persistence/Configurations/PayoutConfiguration.cs
// ========================================
using Booksy.ServiceCatalog.Domain.Aggregates.PayoutAggregate;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Configurations
{
    public sealed class PayoutConfiguration : IEntityTypeConfiguration<Payout>
    {
        public void Configure(EntityTypeBuilder<Payout> builder)
        {
            builder.ToTable("Payouts", "ServiceCatalog");

            // Primary Key
            builder.HasKey(p => p.Id);

            // Concurrency Token
            builder.Property(p => p.Version)
                .IsConcurrencyToken()
                .HasColumnName("Version")
                .HasDefaultValue(0);

            // Payout ID (Value Object)
            builder.Property(p => p.Id)
                .HasConversion(
                    id => id.Value,
                    value => PayoutId.From(value))
                .IsRequired()
                .HasColumnName("PayoutId");

            // Provider ID (Value Object)
            builder.Property(p => p.ProviderId)
                .HasConversion(
                    id => id.Value,
                    value => ProviderId.From(value))
                .IsRequired()
                .HasColumnName("ProviderId");

            // GrossAmount (Owned Value Object)
            builder.OwnsOne(p => p.GrossAmount, amount =>
            {
                amount.Property(m => m.Amount)
                    .HasColumnName("GrossAmount")
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                amount.Property(m => m.Currency)
                    .HasColumnName("Currency")
                    .HasMaxLength(3)
                    .IsRequired();
            });

            // CommissionAmount (Owned Value Object)
            builder.OwnsOne(p => p.CommissionAmount, amount =>
            {
                amount.Property(m => m.Amount)
                    .HasColumnName("CommissionAmount")
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                amount.Property(m => m.Currency)
                    .HasColumnName("CommissionCurrency")
                    .HasMaxLength(3)
                    .IsRequired();
            });

            // NetAmount (Owned Value Object)
            builder.OwnsOne(p => p.NetAmount, amount =>
            {
                amount.Property(m => m.Amount)
                    .HasColumnName("NetAmount")
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                amount.Property(m => m.Currency)
                    .HasColumnName("NetCurrency")
                    .HasMaxLength(3)
                    .IsRequired();
            });

            // Status (Enum)
            builder.Property(p => p.Status)
                .IsRequired()
                .HasColumnName("Status")
                .HasConversion<string>()
                .HasMaxLength(50);

            // Period
            builder.Property(p => p.PeriodStart)
                .IsRequired()
                .HasColumnName("PeriodStart")
                .HasColumnType("timestamp with time zone");

            builder.Property(p => p.PeriodEnd)
                .IsRequired()
                .HasColumnName("PeriodEnd")
                .HasColumnType("timestamp with time zone");

            // Bank Details
            builder.Property(p => p.BankAccountLast4)
                .HasColumnName("BankAccountLast4")
                .HasMaxLength(4);

            builder.Property(p => p.BankName)
                .HasColumnName("BankName")
                .HasMaxLength(200);

            // External Provider Details
            builder.Property(p => p.ExternalPayoutId)
                .HasColumnName("ExternalPayoutId")
                .HasMaxLength(200);

            builder.Property(p => p.StripeAccountId)
                .HasColumnName("StripeAccountId")
                .HasMaxLength(200);

            // Timestamps
            builder.Property(p => p.CreatedAt)
                .IsRequired()
                .HasColumnName("CreatedAt")
                .HasColumnType("timestamp with time zone");

            builder.Property(p => p.ScheduledAt)
                .HasColumnName("ScheduledAt")
                .HasColumnType("timestamp with time zone");

            builder.Property(p => p.PaidAt)
                .HasColumnName("PaidAt")
                .HasColumnType("timestamp with time zone");

            builder.Property(p => p.FailedAt)
                .HasColumnName("FailedAt")
                .HasColumnType("timestamp with time zone");

            builder.Property(p => p.CancelledAt)
                .HasColumnName("CancelledAt")
                .HasColumnType("timestamp with time zone");

            // Notes
            builder.Property(p => p.FailureReason)
                .HasColumnName("FailureReason")
                .HasMaxLength(500);

            builder.Property(p => p.Notes)
                .HasColumnName("Notes")
                .HasMaxLength(1000);

            // Metadata (JSON)
            builder.Property(p => p.Metadata)
                .HasColumnName("Metadata")
                .HasColumnType("jsonb");

            // PaymentIds (JSON array)
            builder.Property("_paymentIds")
                .HasColumnName("PaymentIds")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v.Select(id => id.Value).ToList(), (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(v, (System.Text.Json.JsonSerializerOptions?)null)!
                        .Select(id => PaymentId.From(id)).ToList());

            // Indexes
            builder.HasIndex(p => p.ProviderId)
                .HasDatabaseName("IX_Payouts_ProviderId");

            builder.HasIndex(p => p.Status)
                .HasDatabaseName("IX_Payouts_Status");

            builder.HasIndex(p => p.CreatedAt)
                .HasDatabaseName("IX_Payouts_CreatedAt");

            builder.HasIndex(p => p.ScheduledAt)
                .HasDatabaseName("IX_Payouts_ScheduledAt");

            builder.HasIndex(p => new { p.PeriodStart, p.PeriodEnd })
                .HasDatabaseName("IX_Payouts_Period");

            // Ignore domain events (not persisted)
            builder.Ignore(p => p.DomainEvents);
        }
    }
}
