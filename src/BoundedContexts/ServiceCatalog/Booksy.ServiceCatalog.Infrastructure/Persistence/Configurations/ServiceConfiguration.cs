using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Entities;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Configurations
{
    public sealed class ServiceConfiguration : IEntityTypeConfiguration<Service>
    {
        public void Configure(EntityTypeBuilder<Service> builder)
        {
            builder.ToTable("Services", "ServiceCatalog");

            // ========================================
            // PRIMARY KEY
            // ========================================
            builder.HasKey(s => s.Id);

            // Ignore Version property (not using optimistic concurrency for now)
            builder.Ignore(s => s.Version);

            builder.Property(s => s.Id)
                .HasConversion(
                    id => id.Value,
                    value => ServiceId.From(value))
                .IsRequired();

            // ========================================
            // FOREIGN KEYS
            // ========================================
            builder.Property(s => s.ProviderId)
                .HasConversion(
                    id => id.Value,
                    value => ProviderId.From(value))
                .IsRequired()
                .HasColumnName("ProviderId");

            // ========================================
            // CORE PROPERTIES
            // ========================================
            builder.Property(s => s.Name)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(s => s.Description)
                .HasMaxLength(1000)
                .IsRequired();

            builder.Property(s => s.ImageUrl)
                .HasMaxLength(500);

            // ========================================
            // ENUMS
            // ========================================
            builder.Property(s => s.Type)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(s => s.Status)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(50);

            // ========================================
            // VALUE OBJECTS
            // ========================================

            // Service Category
            builder.OwnsOne(s => s.Category, category =>
            {
                category.Property(c => c.Name)
                    .HasMaxLength(100)
                    .IsRequired()
                    .HasColumnName("CategoryName");

                category.Property(c => c.Description)
                    .HasMaxLength(500)
                    .HasColumnName("CategoryDescription");

                category.Property(c => c.IconUrl)
                    .HasMaxLength(500)
                    .HasColumnName("CategoryIconUrl");
            });

            // Base Price
            builder.OwnsOne(s => s.BasePrice, price =>
            {
                price.Property(p => p.Amount)
                    .HasPrecision(18, 2)
                    .IsRequired()
                    .HasColumnName("BasePriceAmount");

                price.Property(p => p.Currency)
                    .HasMaxLength(3)
                    .IsRequired()
                    .HasColumnName("BasePriceCurrency");
            });

            // Durations
            builder.Property(s => s.Duration)
                .HasConversion(
                    duration => duration.Value,
                    value => Duration.FromMinutes(value))
                .IsRequired()
                .HasColumnName("DurationMinutes");

            builder.Property(s => s.PreparationTime)
                .HasConversion(
                    duration => duration != null ? duration.Value : (int?)null,
                    value => value.HasValue ? Duration.FromMinutes(value.Value) : null)
                .HasColumnName("PreparationTimeMinutes");

            builder.Property(s => s.BufferTime)
                .HasConversion(
                    duration => duration != null ? duration.Value : (int?)null,
                    value => value.HasValue ? Duration.FromMinutes(value.Value) : null)
                .HasColumnName("BufferTimeMinutes");

            // BookingPolicy (Owned Value Object)
            builder.OwnsOne(s => s.BookingPolicy, policy =>
            {
                policy.Property(p => p.MinAdvanceBookingHours)
                    .HasColumnName("BookingPolicyMinAdvanceBookingHours")
                    .IsRequired();

                policy.Property(p => p.MaxAdvanceBookingDays)
                    .HasColumnName("BookingPolicyMaxAdvanceBookingDays")
                    .IsRequired();

                policy.Property(p => p.CancellationWindowHours)
                    .HasColumnName("BookingPolicyCancellationWindowHours")
                    .IsRequired();

                policy.Property(p => p.CancellationFeePercentage)
                    .HasColumnName("BookingPolicyCancellationFeePercentage")
                    .HasColumnType("decimal(5,2)")
                    .IsRequired();

                policy.Property(p => p.AllowRescheduling)
                    .HasColumnName("BookingPolicyAllowRescheduling")
                    .IsRequired();

                policy.Property(p => p.RescheduleWindowHours)
                    .HasColumnName("BookingPolicyRescheduleWindowHours")
                    .IsRequired();

                policy.Property(p => p.RequireDeposit)
                    .HasColumnName("BookingPolicyRequireDeposit")
                    .IsRequired();

                policy.Property(p => p.DepositPercentage)
                    .HasColumnName("BookingPolicyDepositPercentage")
                    .HasColumnType("decimal(5,2)")
                    .IsRequired();
            });

            // ========================================
            // BOOLEAN FLAGS
            // ========================================
            builder.Property(s => s.RequiresDeposit)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(s => s.AllowOnlineBooking)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(s => s.AvailableAtLocation)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(s => s.AvailableAsMobile)
                .IsRequired()
                .HasDefaultValue(false);

            // ========================================
            // NUMERIC PROPERTIES
            // ========================================
            builder.Property(s => s.DepositPercentage)
                .HasPrecision(5, 2)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(s => s.MaxAdvanceBookingDays)
                .IsRequired()
                .HasDefaultValue(90);

            builder.Property(s => s.MinAdvanceBookingHours)
                .IsRequired()
                .HasDefaultValue(1);

            builder.Property(s => s.MaxConcurrentBookings)
                .IsRequired()
                .HasDefaultValue(1);

            // ========================================
            // JSON PROPERTIES
            // ========================================

            // Metadata Dictionary
            builder.Property(s => s.Metadata)
                .HasColumnType("jsonb")
                .HasConversion(
                    dict => JsonSerializer.Serialize(dict, (JsonSerializerOptions?)null),
                    json => JsonSerializer.Deserialize<Dictionary<string, string>>(json, (JsonSerializerOptions?)null)
                            ?? new Dictionary<string, string>(),
                    new ValueComparer<Dictionary<string, string>>(
                        (c1, c2) => c1!.SequenceEqual(c2!),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c.ToDictionary(x => x.Key, x => x.Value)))
                .HasDefaultValueSql("'{}'::jsonb")
                .IsRequired();

            // ========================================
            // COLLECTIONS (Using Backing Fields)
            // ========================================

            // Service Options - Owned collection with backing field
            builder.OwnsMany(s => s.Options, option =>
            {
                option.ToTable("ServiceOptions", "ServiceCatalog");

                option.HasKey(so => so.Id);
                option.Property(so => so.Id).HasColumnName("Id");

                // Shadow property for foreign key (EF Core 9 requirement for owned entities)
                option.WithOwner()
                    .HasForeignKey("ServiceId");

                // Properties
                option.Property(so => so.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                option.Property(so => so.Description)
                    .HasMaxLength(500);

                // Additional Price (Owned Value Object)
                option.OwnsOne(so => so.AdditionalPrice, price =>
                {
                    price.Property(p => p.Amount)
                        .HasPrecision(18, 2)
                        .IsRequired()
                        .HasColumnName("AdditionalPriceAmount");

                    price.Property(p => p.Currency)
                        .HasMaxLength(3)
                        .IsRequired()
                        .HasColumnName("AdditionalPriceCurrency");
                });

                // Additional Duration
                option.Property(so => so.AdditionalDuration)
                    .HasConversion(
                        duration => duration != null ? duration.Value : (int?)null,
                        value => value.HasValue ? Duration.FromMinutes(value.Value) : null)
                    .HasColumnName("AdditionalDurationMinutes");

                // Boolean flags
                option.Property(so => so.IsRequired)
                    .IsRequired()
                    .HasDefaultValue(false);

                option.Property(so => so.IsActive)
                    .IsRequired()
                    .HasDefaultValue(true);

                // Sort order
                option.Property(so => so.SortOrder)
                    .IsRequired()
                    .HasDefaultValue(0);

                // Audit properties from base Entity class
                option.Property(so => so.CreatedAt)
                    .IsRequired()
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                option.Property(so => so.CreatedBy);

                option.Property(so => so.LastModifiedAt)
                    .HasColumnType("timestamp with time zone");

                option.Property(so => so.LastModifiedBy);

                // Soft delete from base Entity class
                option.Property(so => so.IsDeleted)
                    .IsRequired()
                    .HasDefaultValue(false);

                // Indexes for ServiceOptions
                option.HasIndex("ServiceId", nameof(ServiceOption.IsActive))
                    .HasDatabaseName("IX_ServiceOptions_ServiceId_IsActive");

                option.HasIndex(so => so.SortOrder)
                    .HasDatabaseName("IX_ServiceOptions_SortOrder");

                option.HasIndex(so => new { so.IsActive, so.SortOrder })
                    .HasDatabaseName("IX_ServiceOptions_Active_SortOrder");
            });

            builder.Navigation(s => s.Options)
                .UsePropertyAccessMode(PropertyAccessMode.Field)
                .HasField("_options");

            // Price Tiers - Owned collection with backing field
            builder.OwnsMany(s => s.PriceTiers, priceTier =>
            {
                priceTier.ToTable("ServicePriceTiers", "ServiceCatalog");

                priceTier.HasKey(pt => pt.Id);
                priceTier.Property(pt => pt.Id).HasColumnName("Id");

                // Shadow property for foreign key
                priceTier.WithOwner()
                    .HasForeignKey("ServiceId");

                priceTier.Property(pt => pt.Name)
                    .HasMaxLength(100)
                    .IsRequired();

                priceTier.Property(pt => pt.Description)
                    .HasMaxLength(500);

                priceTier.Property(pt => pt.IsDefault)
                    .HasDefaultValue(false)
                    .IsRequired();

                priceTier.Property(pt => pt.IsActive)
                    .HasDefaultValue(true)
                    .IsRequired();

                priceTier.Property(pt => pt.SortOrder)
                    .HasDefaultValue(0)
                    .IsRequired();

                // Price Value Object
                priceTier.OwnsOne(pt => pt.Price, price =>
                {
                    price.Property(p => p.Amount)
                        .HasColumnName("Price")
                        .HasPrecision(18, 2)
                        .IsRequired();

                    price.Property(p => p.Currency)
                        .HasColumnName("Currency")
                        .HasMaxLength(3)
                        .IsRequired();
                });

                // Attributes Dictionary
                priceTier.Property(pt => pt.Attributes)
                    .HasColumnType("jsonb")
                    .HasConversion(
                        dict => JsonSerializer.Serialize(dict, (JsonSerializerOptions?)null),
                        json => JsonSerializer.Deserialize<Dictionary<string, string>>(json, (JsonSerializerOptions?)null)
                                ?? new Dictionary<string, string>(),
                        new ValueComparer<Dictionary<string, string>>(
                            (c1, c2) => c1!.SequenceEqual(c2!),
                            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                            c => c.ToDictionary(x => x.Key, x => x.Value)))
                    .HasDefaultValueSql("'{}'::jsonb");

                // Indexes for PriceTiers
                priceTier.HasIndex("ServiceId", nameof(PriceTier.IsActive))
                    .HasDatabaseName("IX_ServicePriceTiers_ServiceId_IsActive");
            });

            builder.Navigation(s => s.PriceTiers)
                .UsePropertyAccessMode(PropertyAccessMode.Field)
                .HasField("_priceTiers");

            builder.Property<List<Guid>>("_qualifiedStaff")
       .HasColumnName("QualifiedStaff")
       .HasColumnType("jsonb")
       .HasConversion(
           v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
           v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>(),
           new ValueComparer<List<Guid>>(
               (c1, c2) => (c1 ?? new List<Guid>()).SequenceEqual(c2 ?? new List<Guid>()),
               c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
               c => c == null ? new List<Guid>() : c.ToList()
           )
       )
       .Metadata.SetPropertyAccessMode(PropertyAccessMode.Field);


            // ========================================
            // TIMESTAMPS
            // ========================================
            builder.Property(s => s.CreatedAt)
                .IsRequired()
                .HasColumnType("timestamp with time zone");

            builder.Property(s => s.CreatedBy)
                .HasMaxLength(100);

            builder.Property(s => s.LastModifiedAt)
                .HasColumnType("timestamp with time zone");

            builder.Property(s => s.LastModifiedBy)
                .HasMaxLength(100);

            builder.Property(s => s.ActivatedAt)
                .HasColumnType("timestamp with time zone");

            // ========================================
            // DOMAIN EVENTS
            // ========================================
            builder.Ignore(s => s.DomainEvents);

            // ========================================
            // RELATIONSHIPS
            // ========================================
            builder.HasOne(s => s.Provider)
                .WithMany()
                .HasForeignKey(s => s.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            // ========================================
            // INDEXES
            // ========================================
            builder.HasIndex(s => s.ProviderId)
                .HasDatabaseName("IX_Services_ProviderId");

            builder.HasIndex(s => s.Status)
                .HasDatabaseName("IX_Services_Status");

            builder.HasIndex(s => s.Type)
                .HasDatabaseName("IX_Services_Type");

            builder.HasIndex(s => new { s.ProviderId, s.Name })
                .IsUnique()
                .HasDatabaseName("IX_Services_ProviderId_Name");

            builder.HasIndex(s => s.CreatedAt)
                .HasDatabaseName("IX_Services_CreatedAt");

            // Composite index for active services
            builder.HasIndex(s => new { s.Status, s.AllowOnlineBooking })
                .HasDatabaseName("IX_Services_Status_AllowOnlineBooking");
        }
    }
}