using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Configurations
{
    public sealed class ReviewVoteConfiguration : IEntityTypeConfiguration<ReviewVote>
    {
        public void Configure(EntityTypeBuilder<ReviewVote> builder)
        {
            builder.ToTable("ReviewVotes", "ServiceCatalog");

            builder.HasKey(v => v.Id);
            builder.Property(v => v.Id)
                .HasColumnName("ReviewVoteId")
                .ValueGeneratedNever(); // The domain mints it; see EfOwnedEntityKeyConventionTests.

            builder.Property(v => v.Version)
                .IsConcurrencyToken()
                .HasColumnName("Version")
                .HasDefaultValue(0);

            builder.Property(v => v.ReviewId).IsRequired().HasColumnName("ReviewId");

            builder.Property(v => v.UserId)
                .HasConversion(id => id.Value, value => UserId.From(value))
                .IsRequired()
                .HasColumnName("UserId");

            builder.Property(v => v.IsHelpful).IsRequired().HasColumnName("IsHelpful");

            builder.Property(v => v.CreatedAt)
                .IsRequired()
                .HasColumnName("CreatedAt")
                .HasColumnType("timestamp with time zone");

            builder.Property(v => v.LastModifiedAt)
                .HasColumnName("LastModifiedAt")
                .HasColumnType("timestamp with time zone");

            // One vote per user per review. This index — not a check in the handler — is what holds under
            // concurrency: a check-then-insert races, a unique violation does not.
            builder.HasIndex(v => new { v.ReviewId, v.UserId })
                .IsUnique()
                .HasDatabaseName("UX_ReviewVotes_Review_User");

            builder.HasOne<Review>()
                .WithMany()
                .HasForeignKey(v => v.ReviewId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
