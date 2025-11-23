// ========================================
// Booksy.ServiceCatalog.Infrastructure/Persistence/Configurations/BookingConfiguration.cs
// ========================================
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Configurations
{
    public sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
    {
        public void Configure(EntityTypeBuilder<Booking> builder)
        {
            builder.ToTable("Bookings", "ServiceCatalog");

            // Primary Key
            builder.HasKey(b => b.Id);

            // Concurrency Token
            builder.Property(b => b.Version)
                .IsConcurrencyToken()
                .HasColumnName("Version")
                .HasDefaultValue(0);

            // Booking ID (Value Object)
            builder.Property(b => b.Id)
                .HasConversion(
                    id => id.Value,
                    value => BookingId.From(value))
                .IsRequired()
                .HasColumnName("BookingId");

            // Customer ID (Value Object)
            builder.Property(b => b.CustomerId)
                .HasConversion(
                    id => id.Value,
                    value => UserId.From(value))
                .IsRequired()
                .HasColumnName("CustomerId");

            // Provider ID (Value Object)
            builder.Property(b => b.ProviderId)
                .HasConversion(
                    id => id.Value,
                    value => ProviderId.From(value))
                .IsRequired()
                .HasColumnName("ProviderId");

            // Service ID (Value Object)
            builder.Property(b => b.ServiceId)
                .HasConversion(
                    id => id.Value,
                    value => ServiceId.From(value))
                .IsRequired()
                .HasColumnName("ServiceId");

            // Staff ID
            builder.Property(b => b.StaffId)
                .IsRequired()
                .HasColumnName("StaffId");

            // Individual Provider ID (for hierarchy - tracks which individual provider performs the service)
            builder.Property(b => b.IndividualProviderId)
                .HasConversion(
                    id => id != null ? id.Value : (Guid?)null,
                    value => value.HasValue ? ProviderId.From(value.Value) : null)
                .HasColumnName("IndividualProviderId");

            // TimeSlot (Owned Value Object)
            builder.OwnsOne(b => b.TimeSlot, timeSlot =>
            {
                timeSlot.Property(ts => ts.StartTime)
                    .HasColumnName("StartTime")
                    .IsRequired()
                    .HasColumnType("timestamp with time zone");

                timeSlot.Property(ts => ts.EndTime)
                    .HasColumnName("EndTime")
                    .IsRequired()
                    .HasColumnType("timestamp with time zone");
            });

            // Duration (Value Object)
            builder.Property(b => b.Duration)
                .HasConversion(
                    d => d.Value,
                    value => Duration.FromMinutes(value))
                .IsRequired()
                .HasColumnName("DurationMinutes");

            // Status (Enum)
            builder.Property(b => b.Status)
                .IsRequired()
                .HasColumnName("Status")
                .HasConversion<string>()
                .HasMaxLength(50);

            // Price (Owned Value Object)
            builder.OwnsOne(b => b.TotalPrice, price =>
            {
                price.Property(p => p.Amount)
                    .HasColumnName("TotalPriceAmount")
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                price.Property(p => p.Currency)
                    .HasColumnName("TotalPriceCurrency")
                    .HasMaxLength(3)
                    .IsRequired();
            });

            // PaymentInfo (Owned Value Object - Complex)
            builder.OwnsOne(b => b.PaymentInfo, payment =>
            {
                payment.OwnsOne(p => p.TotalAmount, total =>
                {
                    total.Property(m => m.Amount)
                        .HasColumnName("PaymentTotalAmount")
                        .HasColumnType("decimal(18,2)")
                        .IsRequired();

                    total.Property(m => m.Currency)
                        .HasColumnName("PaymentCurrency")
                        .HasMaxLength(3)
                        .IsRequired();
                });

                payment.OwnsOne(p => p.DepositAmount, deposit =>
                {
                    deposit.Property(m => m.Amount)
                        .HasColumnName("DepositAmount")
                        .HasColumnType("decimal(18,2)")
                        .IsRequired();

                    deposit.Property(m => m.Currency)
                        .HasColumnName("DepositCurrency")
                        .HasMaxLength(3)
                        .IsRequired();
                });

                payment.OwnsOne(p => p.PaidAmount, paid =>
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

                payment.OwnsOne(p => p.RefundedAmount, refunded =>
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

                payment.Property(p => p.Status)
                    .HasColumnName("PaymentStatus")
                    .HasConversion<string>()
                    .HasMaxLength(50)
                    .IsRequired();

                payment.Property(p => p.PaymentIntentId)
                    .HasColumnName("PaymentIntentId")
                    .HasMaxLength(200);

                payment.Property(p => p.DepositPaymentIntentId)
                    .HasColumnName("DepositPaymentIntentId")
                    .HasMaxLength(200);

                payment.Property(p => p.RefundId)
                    .HasColumnName("RefundId")
                    .HasMaxLength(200);

                payment.Property(p => p.PaidAt)
                    .HasColumnName("PaidAt");

                payment.Property(p => p.RefundedAt)
                    .HasColumnName("RefundedAt");
            });

            //// BookingPolicy (Owned Value Object)
            builder.OwnsOne(b => b.Policy, policy =>
            {
                policy.Property(p => p.MinAdvanceBookingHours)
                    .HasColumnName("PolicyMinAdvanceBookingHours")
                    .IsRequired();

                policy.Property(p => p.MaxAdvanceBookingDays)
                    .HasColumnName("PolicyMaxAdvanceBookingDays")
                    .IsRequired();

                policy.Property(p => p.CancellationWindowHours)
                    .HasColumnName("PolicyCancellationWindowHours")
                    .IsRequired();

                policy.Property(p => p.CancellationFeePercentage)
                    .HasColumnName("PolicyCancellationFeePercentage")
                    .HasColumnType("decimal(5,2)")
                    .IsRequired();

                policy.Property(p => p.AllowRescheduling)
                    .HasColumnName("PolicyAllowRescheduling")
                    .IsRequired();

                policy.Property(p => p.RescheduleWindowHours)
                    .HasColumnName("PolicyRescheduleWindowHours")
                    .IsRequired();

                policy.Property(p => p.RequireDeposit)
                    .HasColumnName("PolicyRequireDeposit")
                    .IsRequired();

                policy.Property(p => p.DepositPercentage)
                    .HasColumnName("PolicyDepositPercentage")
                    .HasColumnType("decimal(5,2)")
                    .IsRequired();
            });

            //// String Properties
            //builder.Property(b => b.CustomerNotes)
            //    .HasColumnName("CustomerNotes")
            //    .HasMaxLength(2000);

            //builder.Property(b => b.StaffNotes)
            //    .HasColumnName("StaffNotes")
            //    .HasMaxLength(2000);

            //builder.Property(b => b.CancellationReason)
            //    .HasColumnName("CancellationReason")
            //    .HasMaxLength(1000);

            //// Timestamps
            //builder.Property(b => b.RequestedAt)
            //    .HasColumnName("RequestedAt")
            //    .HasColumnType("timestamp with time zone")
            //    .IsRequired();

            //builder.Property(b => b.ConfirmedAt)
            //    .HasColumnName("ConfirmedAt")
            //    .HasColumnType("timestamp with time zone");

            //builder.Property(b => b.CancelledAt)
            //    .HasColumnName("CancelledAt")
            //    .HasColumnType("timestamp with time zone");

            //builder.Property(b => b.CompletedAt)
            //    .HasColumnName("CompletedAt")
            //    .HasColumnType("timestamp with time zone");

            //builder.Property(b => b.RescheduledAt)
            //    .HasColumnName("RescheduledAt")
            //    .HasColumnType("timestamp with time zone");

            //// Rescheduling References
            builder.Property(b => b.PreviousBookingId)
                .HasConversion(
                    id => id != null ? id.Value : (Guid?)null,
                    value => value.HasValue ? BookingId.From(value.Value) : null)
                .HasColumnName("PreviousBookingId");

            builder.Property(b => b.RescheduledToBookingId)
                .HasConversion(
                    id => id != null ? id.Value : (Guid?)null,
                    value => value.HasValue ? BookingId.From(value.Value) : null)
                .HasColumnName("RescheduledToBookingId");

            //// Audit Properties
            //builder.Property(b => b.CreatedAt)
            //    .HasColumnName("CreatedAt")
            //    .HasColumnType("timestamp with time zone")
            //    .IsRequired();

            //builder.Property(b => b.CreatedBy)
            //    .HasColumnName("CreatedBy")
            //    .HasMaxLength(200);

            //builder.Property(b => b.LastModifiedAt)
            //    .HasColumnName("LastModifiedAt")
            //    .HasColumnType("timestamp with time zone");

            //builder.Property(b => b.LastModifiedBy)
            //    .HasColumnName("LastModifiedBy")
            //    .HasMaxLength(200);

            //// Owned Collection: History
            //builder.OwnsMany(b => b.History, history =>
            //{
            //    history.ToTable("BookingHistory", "ServiceCatalog");

            //    history.WithOwner().HasForeignKey("BookingId");

            //    history.HasKey("Id");

            //    history.Property<Guid>("BookingId")
            //        .HasColumnName("BookingId")
            //        .IsRequired();

            //    history.Property(h => h.Description)
            //        .HasColumnName("Description")
            //        .HasMaxLength(1000)
            //        .IsRequired();

            //    history.Property(h => h.Status)
            //        .HasColumnName("Status")
            //        .HasConversion<string>()
            //        .HasMaxLength(50)
            //        .IsRequired();

            //    history.Property(h => h.OccurredAt)
            //        .HasColumnName("OccurredAt")
            //        .HasColumnType("timestamp with time zone")
            //        .IsRequired();
            //});

            //// Indexes for performance
            //builder.HasIndex(b => b.CustomerId)
            //    .HasDatabaseName("IX_Bookings_CustomerId");

            //builder.HasIndex(b => b.ProviderId)
            //    .HasDatabaseName("IX_Bookings_ProviderId");

            builder.HasIndex(b => b.ServiceId)
                .HasDatabaseName("IX_Bookings_ServiceId");

            // Index for querying bookings by individual provider in hierarchy
            builder.HasIndex(b => b.IndividualProviderId)
                .HasDatabaseName("IX_Bookings_IndividualProviderId");

            //builder.HasIndex(b => b.StaffId)
            //    .HasDatabaseName("IX_Bookings_StaffId");

            //builder.HasIndex(b => b.Status)
            //    .HasDatabaseName("IX_Bookings_Status");

            //builder.HasIndex(b => new { b.StaffId, b.Status })
            //    .HasDatabaseName("IX_Bookings_StaffId_Status");

            //// Composite index for availability checks
            //builder.HasIndex("StaffId", "Status")
            //    .HasDatabaseName("IX_Bookings_Availability")
            //    .HasFilter("[Status] IN ('Requested', 'Confirmed')");
        }
    }
}
