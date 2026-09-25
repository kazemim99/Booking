// ========================================
// AsanRezerve.ServiceCatalog.Infrastructure/Persistence/Configurations/ReviewConfiguration.cs
// ========================================
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.Aggregates;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Configurations
{
    public sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
    {
        public void Configure(EntityTypeBuilder<Review> builder)
        {
            builder.ToTable("Reviews", "ServiceCatalog");

            // Primary Key
            builder.HasKey(r => r.Id);

            // Concurrency Token
            builder.Property(r => r.Version)
                .IsConcurrencyToken()
                .HasColumnName("Version")
                .HasDefaultValue(0);

            // ID
            builder.Property(r => r.Id)
                .IsRequired()
                .HasColumnName("ReviewId");

            // Provider ID (Value Object)
            builder.Property(r => r.ProviderId)
                .HasConversion(
                    id => id.Value,
                    value => ProviderId.From(value))
                .IsRequired()
                .HasColumnName("ProviderId");

            // Customer ID (Value Object)
            builder.Property(r => r.CustomerId)
                .HasConversion(
                    id => id.Value,
                    value => UserId.From(value))
                .IsRequired()
                .HasColumnName("CustomerId");

            // Booking ID
            builder.Property(r => r.BookingId)
                .IsRequired()
                .HasColumnName("BookingId");

            // Rating Value
            builder.Property(r => r.RatingValue)
                .IsRequired()
                .HasColumnName("RatingValue")
                .HasColumnType("decimal(3,1)") // e.g., 4.5
                .HasPrecision(3, 1);

            // Comment (Persian/English text)
            builder.Property(r => r.Comment)
                .IsRequired(false)
                .HasColumnName("Comment")
                .HasMaxLength(2000);

            // The author's «نامم نمایش داده نشود» choice. Every review from before it existed shows the name: the
            // migration fills the column with true. No HasDefaultValue(true) in the model on purpose — EF would take
            // the CLR default (false) as "unset" and insert the database default, so no one could ever hide their name.
            builder.Property(r => r.ShowName)
                .IsRequired()
                .HasColumnName("ShowName");

            // Verification Status
            builder.Property(r => r.IsVerified)
                .IsRequired()
                .HasColumnName("IsVerified")
                .HasDefaultValue(true);

            // Provider Response
            builder.Property(r => r.ProviderResponse)
                .IsRequired(false)
                .HasColumnName("ProviderResponse")
                .HasMaxLength(1000);

            builder.Property(r => r.ProviderResponseAt)
                .IsRequired(false)
                .HasColumnName("ProviderResponseAt")
                .HasColumnType("timestamp with time zone");

            // Helpfulness — legacy baseline.
            // The CLR properties are renamed; the COLUMNS are not. A physical rename would make the previous image
            // throw 42703 on every review read during a rolling deploy, behind a green /health, and would make
            // rollback a schema reversal instead of a redeploy. Same technique as Id → "ReviewId" above.
            builder.Property(r => r.LegacyHelpfulCount)
                .IsRequired()
                .HasColumnName("HelpfulCount")
                .HasDefaultValue(0);

            builder.Property(r => r.LegacyNotHelpfulCount)
                .IsRequired()
                .HasColumnName("NotHelpfulCount")
                .HasDefaultValue(0);

            // Helpfulness — live per-user tallies, kept in step with ReviewVotes.
            builder.Property(r => r.HelpfulVoteCount)
                .IsRequired()
                .HasColumnName("HelpfulVoteCount")
                .HasDefaultValue(0);

            builder.Property(r => r.NotHelpfulVoteCount)
                .IsRequired()
                .HasColumnName("NotHelpfulVoteCount")
                .HasDefaultValue(0);

            // Dimension ratings — each optional; null means "not rated", never zero.
            builder.Property(r => r.CleanlinessRating).HasColumnName("CleanlinessRating").HasPrecision(3, 1);
            builder.Property(r => r.SkillRating).HasColumnName("SkillRating").HasPrecision(3, 1);
            builder.Property(r => r.PunctualityRating).HasColumnName("PunctualityRating").HasPrecision(3, 1);
            builder.Property(r => r.ConductRating).HasColumnName("ConductRating").HasPrecision(3, 1);

            // Moderation — independent of IsVerified.
            // Default 'Pending': a row inserted by code that predates moderation waits for a moderator
            // instead of going straight to the public.
            builder.Property(r => r.ModerationStatus)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasColumnName("ModerationStatus")
                .HasDefaultValue(ReviewModerationStatus.Pending);

            builder.Property(r => r.ModeratedAt)
                .HasColumnName("ModeratedAt")
                .HasColumnType("timestamp with time zone");

            builder.Property(r => r.ModeratedBy)
                .HasColumnName("ModeratedBy")
                .HasMaxLength(100);

            builder.Property(r => r.ModerationReason)
                .HasColumnName("ModerationReason")
                .HasMaxLength(500);

            builder.Property(r => r.FirstPublishedAt)
                .HasColumnName("FirstPublishedAt")
                .HasColumnType("timestamp with time zone");

            builder.Property(r => r.EditedAt)
                .HasColumnName("EditedAt")
                .HasColumnType("timestamp with time zone");

            // Reply moderation — null means there is no reply.
            builder.Property(r => r.ReplyModerationStatus)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasColumnName("ReplyModerationStatus");

            builder.Property(r => r.ReplyModerationReason)
                .HasColumnName("ReplyModerationReason")
                .HasMaxLength(500);

            // Audit Properties
            builder.Property(r => r.CreatedAt)
                .IsRequired()
                .HasColumnName("CreatedAt")
                .HasColumnType("timestamp with time zone");

            builder.Property(r => r.CreatedBy)
                .IsRequired(false)
                .HasColumnName("CreatedBy")
                .HasMaxLength(100);

            builder.Property(r => r.LastModifiedAt)
                .IsRequired(false)
                .HasColumnName("LastModifiedAt")
                .HasColumnType("timestamp with time zone");

            builder.Property(r => r.LastModifiedBy)
                .IsRequired(false)
                .HasColumnName("LastModifiedBy")
                .HasMaxLength(100);

            // Indexes for query performance
            builder.HasIndex(r => r.ProviderId)
                .HasDatabaseName("IX_Reviews_ProviderId");

            builder.HasIndex(r => r.CustomerId)
                .HasDatabaseName("IX_Reviews_CustomerId");

            builder.HasIndex(r => r.BookingId)
                .HasDatabaseName("IX_Reviews_BookingId")
                .IsUnique(); // One review per booking

            builder.HasIndex(r => new { r.ProviderId, r.RatingValue })
                .HasDatabaseName("IX_Reviews_Provider_Rating");

            builder.HasIndex(r => new { r.ProviderId, r.CreatedAt })
                .HasDatabaseName("IX_Reviews_Provider_CreatedAt");

            builder.HasIndex(r => new { r.IsVerified, r.CreatedAt })
                .HasDatabaseName("IX_Reviews_Verified_CreatedAt");

            // The public listing and the rating recompute both filter a provider's reviews by moderation state.
            builder.HasIndex(r => new { r.ProviderId, r.ModerationStatus })
                .HasDatabaseName("IX_Reviews_Provider_ModerationStatus");

            // The moderation queue: pending items, oldest first.
            builder.HasIndex(r => new { r.ModerationStatus, r.CreatedAt })
                .HasDatabaseName("IX_Reviews_ModerationStatus_CreatedAt");
        }
    }
}
