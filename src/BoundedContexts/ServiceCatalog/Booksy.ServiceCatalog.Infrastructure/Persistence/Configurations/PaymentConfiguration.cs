// ========================================
// Booksy.ServiceCatalog.Infrastructure/Persistence/Configurations/PaymentConfiguration.cs
// ========================================
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates.PaymentAggregate;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Configurations
{
    public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.ToTable("Payments", "ServiceCatalog");

            // Primary Key
            builder.HasKey(p => p.Id);

            // Concurrency Token
            builder.Property(p => p.Version)
                .IsConcurrencyToken()
                .HasColumnName("Version")
                .HasDefaultValue(0);

            // Payment ID (Value Object)
            builder.Property(p => p.Id)
                .HasConversion(
                    id => id.Value,
                    value => PaymentId.From(value))
                .IsRequired()
                .HasColumnName("PaymentId");

            // Booking ID (Value Object - nullable)
            builder.Property(p => p.BookingId)
                .HasConversion(
                    id => id != null ? id.Value : (Guid?)null,
                    value => value.HasValue ? BookingId.From(value.Value) : null)
                .HasColumnName("BookingId");

            // Customer ID (Value Object)
            builder.Property(p => p.CustomerId)
                .HasConversion(
                    id => id.Value,
                    value => UserId.From(value))
                .IsRequired()
                .HasColumnName("CustomerId");

            // Provider ID (Value Object)
            builder.Property(p => p.ProviderId)
                .HasConversion(
                    id => id.Value,
                    value => ProviderId.From(value))
                .IsRequired()
                .HasColumnName("ProviderId");

            // Amount (Owned Value Object)
            builder.OwnsOne(p => p.Amount, amount =>
            {
                amount.Property(m => m.Amount)
                    .HasColumnName("Amount")
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                amount.Property(m => m.Currency)
                    .HasColumnName("Currency")
                    .HasMaxLength(3)
                    .IsRequired();
            });

            // PaidAmount (Owned Value Object)
            builder.OwnsOne(p => p.PaidAmount, paid =>
            {
                paid.Property(m => m.Amount)
                    .HasColumnName("PaidAmount")
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                paid.Property(m => m.Currency)
                    .HasColumnName("PaidCurrency")
                    .HasMaxLength(3)
                    .IsRequired();
            });

            // RefundedAmount (Owned Value Object)
            builder.OwnsOne(p => p.RefundedAmount, refunded =>
            {
                refunded.Property(m => m.Amount)
                    .HasColumnName("RefundedAmount")
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                refunded.Property(m => m.Currency)
                    .HasColumnName("RefundedCurrency")
                    .HasMaxLength(3)
                    .IsRequired();
            });

            // Status (Enum)
            builder.Property(p => p.Status)
                .IsRequired()
                .HasColumnName("Status")
                .HasConversion<string>()
                .HasMaxLength(50);

            // Method (Enum)
            builder.Property(p => p.Method)
                .IsRequired()
                .HasColumnName("Method")
                .HasConversion<string>()
                .HasMaxLength(50);

            // Provider (Enum)
            builder.Property(p => p.Provider)
                .IsRequired()
                .HasColumnName("Provider")
                .HasConversion<string>()
                .HasMaxLength(50);

            // Payment Provider Details
            builder.Property(p => p.PaymentIntentId)
                .HasColumnName("PaymentIntentId")
                .HasMaxLength(200);

            builder.Property(p => p.PaymentMethodId)
                .HasColumnName("PaymentMethodId")
                .HasMaxLength(200);

            builder.Property(p => p.CustomCustomerId)
                .HasColumnName("StripeCustomerId")
                .HasMaxLength(200);

            // ZarinPal-Specific Details
            builder.Property(p => p.Authority)
                .HasColumnName("Authority")
                .HasMaxLength(100);

            builder.Property(p => p.RefNumber)
                .HasColumnName("RefNumber")
                .HasMaxLength(100);

            builder.Property(p => p.CardPan)
                .HasColumnName("CardPan")
                .HasMaxLength(20);

            builder.Property(p => p.PaymentUrl)
                .HasColumnName("PaymentUrl")
                .HasMaxLength(500);

            // Fee (Owned Value Object - nullable)
            builder.OwnsOne(p => p.Fee, fee =>
            {
                fee.Property(m => m.Amount)
                    .HasColumnName("Fee")
                    .HasColumnType("decimal(18,2)");

                fee.Property(m => m.Currency)
                    .HasColumnName("FeeCurrency")
                    .HasMaxLength(3);
            });

            // Timestamps
            builder.Property(p => p.CreatedAt)
                .IsRequired()
                .HasColumnName("CreatedAt")
                .HasColumnType("timestamp with time zone");

            builder.Property(p => p.AuthorizedAt)
                .HasColumnName("AuthorizedAt")
                .HasColumnType("timestamp with time zone");

            builder.Property(p => p.CapturedAt)
                .HasColumnName("CapturedAt")
                .HasColumnType("timestamp with time zone");

            builder.Property(p => p.RefundedAt)
                .HasColumnName("RefundedAt")
                .HasColumnType("timestamp with time zone");

            builder.Property(p => p.FailedAt)
                .HasColumnName("FailedAt")
                .HasColumnType("timestamp with time zone");

            // Description and Notes
            builder.Property(p => p.Description)
                .HasColumnName("Description")
                .HasMaxLength(500);

            builder.Property(p => p.FailureReason)
                .HasColumnName("FailureReason")
                .HasMaxLength(500);

            // Metadata (JSON)
            builder.Property(p => p.Metadata)
                .HasColumnName("Metadata")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null)
                        ?? new Dictionary<string, object>());

            // Transactions (Owned Collection)
            builder.OwnsMany(p => p.Transactions, transactions =>
            {
                transactions.ToTable("PaymentTransactions", "ServiceCatalog");

                transactions.WithOwner()
                    .HasForeignKey("PaymentId");

                // Configure the shadow property PaymentId as Guid
                transactions.Property<Guid>("PaymentId")
                    .HasColumnName("PaymentId")
                    .IsRequired();

                transactions.HasKey("Id");

                transactions.Property(t => t.Type)
                    .IsRequired()
                    .HasConversion<string>()
                    .HasMaxLength(50);

                transactions.OwnsOne(t => t.Amount, amount =>
                {
                    amount.Property(m => m.Amount)
                        .HasColumnName("Amount")
                        .HasColumnType("decimal(18,2)")
                        .IsRequired();

                    amount.Property(m => m.Currency)
                        .HasColumnName("Currency")
                        .HasMaxLength(3)
                        .IsRequired();
                });

                transactions.Property(t => t.ExternalTransactionId)
                    .HasMaxLength(200);

                transactions.Property(t => t.Reference)
                    .HasMaxLength(500);

                transactions.Property(t => t.Status)
                    .IsRequired()
                    .HasMaxLength(50);

                transactions.Property(t => t.StatusReason)
                    .HasMaxLength(500);

                transactions.Property(t => t.ProcessedAt)
                    .IsRequired()
                    .HasColumnType("timestamp with time zone");

                transactions.Property(t => t.CompletedAt)
                    .HasColumnType("timestamp with time zone");

                transactions.Property(t => t.Metadata)
                    .HasColumnType("jsonb")
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null)
                            ?? new Dictionary<string, object>());
            });

            // Indexes
            builder.HasIndex(p => p.BookingId)
                .HasDatabaseName("IX_Payments_BookingId");

            builder.HasIndex(p => p.CustomerId)
                .HasDatabaseName("IX_Payments_CustomerId");

            builder.HasIndex(p => p.ProviderId)
                .HasDatabaseName("IX_Payments_ProviderId");

            builder.HasIndex(p => p.Status)
                .HasDatabaseName("IX_Payments_Status");

            builder.HasIndex(p => p.Provider)
                .HasDatabaseName("IX_Payments_Provider");

            builder.HasIndex(p => p.PaymentIntentId)
                .HasDatabaseName("IX_Payments_PaymentIntentId");

            builder.HasIndex(p => p.Authority)
                .HasDatabaseName("IX_Payments_Authority");

            builder.HasIndex(p => p.CreatedAt)
                .HasDatabaseName("IX_Payments_CreatedAt");

            // Ignore domain events (not persisted)
            builder.Ignore(p => p.DomainEvents);
        }
    }
}
