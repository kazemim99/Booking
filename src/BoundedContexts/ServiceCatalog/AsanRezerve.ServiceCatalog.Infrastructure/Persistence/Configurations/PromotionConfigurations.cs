using AsanRezerve.ServiceCatalog.Domain.Aggregates.PromotionAggregate;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Promotions (openspec/changes/add-discounts-and-campaigns). <c>redemption_count</c> is the concurrency token:
    /// every redemption and release updates the row, so two bookings racing for the last use serialize (design D3).
    /// </summary>
    public sealed class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
    {
        /// <summary>The scope key for code uniqueness of platform promotions (design D10).</summary>
        public const string PlatformScope = "00000000-0000-0000-0000-000000000000";

        public void Configure(EntityTypeBuilder<Promotion> builder)
        {
            builder.ToTable("promotions", "ServiceCatalog");

            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedNever();

            builder.Property(p => p.Owner).HasColumnName("owner").HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(p => p.ProviderId)
                .HasConversion(id => id!.Value, value => ProviderId.From(value))
                .HasColumnName("provider_id");

            builder.Property(p => p.Title).HasColumnName("title").HasMaxLength(Promotion.MaxTitleLength).IsRequired();
            builder.Property(p => p.Description).HasColumnName("description").HasMaxLength(Promotion.MaxDescriptionLength);
            builder.Property(p => p.Activation).HasColumnName("activation").HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(p => p.Code).HasColumnName("code").HasMaxLength(20);
            builder.Property(p => p.DiscountKind).HasColumnName("discount_kind").HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(p => p.DiscountValue).HasColumnName("discount_value").HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(p => p.MaxDiscountAmount).HasColumnName("max_discount_amount").HasColumnType("decimal(18,2)");
            builder.Property(p => p.MinimumSubtotal).HasColumnName("minimum_subtotal").HasColumnType("decimal(18,2)");
            builder.Property(p => p.NewCustomersOnly).HasColumnName("new_customers_only").IsRequired();

            builder.Ignore(p => p.ServiceIds);
            builder.Property<List<Guid>>("_serviceIds").HasColumnName("service_ids").IsRequired();

            builder.Ignore(p => p.DaysOfWeek);
            builder.Property(p => p.DaysOfWeekMask).HasColumnName("days_of_week_mask").IsRequired();
            builder.Property(p => p.DailyStartTime).HasColumnName("daily_start_time");
            builder.Property(p => p.DailyEndTime).HasColumnName("daily_end_time");
            builder.Property(p => p.StartsAt).HasColumnName("starts_at").IsRequired();
            builder.Property(p => p.EndsAt).HasColumnName("ends_at");
            builder.Property(p => p.TotalUsageLimit).HasColumnName("total_usage_limit");
            builder.Property(p => p.PerCustomerLimit).HasColumnName("per_customer_limit");
            builder.Property(p => p.RedemptionCount).HasColumnName("redemption_count").IsConcurrencyToken().IsRequired();
            builder.Property(p => p.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(p => p.PausedByPlatform).HasColumnName("paused_by_platform").IsRequired();
            builder.Property(p => p.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
            builder.Property(p => p.CreatedBy).HasColumnName("created_by").IsRequired();
            builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
            builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
            builder.Property(p => p.EndedAt).HasColumnName("ended_at");

            // Version is the base aggregate's event counter; promotions raise no events, and the counter above is the
            // real concurrency guard. Not mapped.
            builder.Ignore(p => p.Version);

            builder.HasIndex(p => new { p.ProviderId, p.Status }).HasDatabaseName("ix_promotions_provider_status");
            builder.HasIndex(p => new { p.Owner, p.Status }).HasDatabaseName("ix_promotions_owner_status");

            // One code per owner scope while the promotion is not ended: a salon's codes are its own, platform codes
            // are unique among campaigns. COALESCE puts every platform code in one scope.
            builder.HasIndex(p => new { p.Owner, p.ProviderId, p.Code })
                .HasDatabaseName("ux_promotions_scope_code")
                .IsUnique()
                .HasFilter("code IS NOT NULL AND status <> 'Ended'")
                .AreNullsDistinct(false);
        }
    }

    public sealed class CampaignEnrollmentConfiguration : IEntityTypeConfiguration<CampaignEnrollment>
    {
        public void Configure(EntityTypeBuilder<CampaignEnrollment> builder)
        {
            builder.ToTable("campaign_enrollments", "ServiceCatalog");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            builder.Property(e => e.PromotionId).HasColumnName("promotion_id").IsRequired();
            builder.Property(e => e.ProviderId)
                .HasConversion(id => id.Value, value => ProviderId.From(value))
                .HasColumnName("provider_id")
                .IsRequired();
            builder.Property(e => e.IsActive).HasColumnName("is_active").IsRequired();
            builder.Property(e => e.JoinedAt).HasColumnName("joined_at").IsRequired();
            builder.Property(e => e.LeftAt).HasColumnName("left_at");
            builder.Property(e => e.ChangedBy).HasColumnName("changed_by").IsRequired();
            builder.Ignore(e => e.Version);

            builder.HasOne<Promotion>().WithMany().HasForeignKey(e => e.PromotionId).OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(e => new { e.PromotionId, e.ProviderId })
                .IsUnique()
                .HasDatabaseName("ux_campaign_enrollments_promotion_provider");
            builder.HasIndex(e => new { e.ProviderId, e.IsActive }).HasDatabaseName("ix_campaign_enrollments_provider");
        }
    }

    public sealed class PromotionRedemptionConfiguration : IEntityTypeConfiguration<PromotionRedemption>
    {
        public void Configure(EntityTypeBuilder<PromotionRedemption> builder)
        {
            builder.ToTable("promotion_redemptions", "ServiceCatalog");

            builder.HasKey(r => r.Id);
            builder.Property(r => r.Id).HasColumnName("id").ValueGeneratedNever();
            builder.Property(r => r.PromotionId).HasColumnName("promotion_id").IsRequired();
            builder.Property(r => r.BookingId).HasColumnName("booking_id").IsRequired();
            builder.Property(r => r.CustomerId).HasColumnName("customer_id").IsRequired();
            builder.Property(r => r.ProviderId).HasColumnName("provider_id").IsRequired();
            builder.Property(r => r.Amount).HasColumnName("amount").HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(r => r.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
            builder.Property(r => r.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(r => r.RedeemedAt).HasColumnName("redeemed_at").IsRequired();
            builder.Property(r => r.ReleasedAt).HasColumnName("released_at");
            builder.Ignore(r => r.Version);

            builder.HasOne<Promotion>().WithMany().HasForeignKey(r => r.PromotionId).OnDelete(DeleteBehavior.Restrict);

            // One discount per booking, backed by the database (design D3).
            builder.HasIndex(r => r.BookingId)
                .IsUnique()
                .HasFilter("status = 'Applied'")
                .HasDatabaseName("ux_promotion_redemptions_applied_booking");
            builder.HasIndex(r => new { r.PromotionId, r.CustomerId, r.Status })
                .HasDatabaseName("ix_promotion_redemptions_promotion_customer");
        }
    }
}
