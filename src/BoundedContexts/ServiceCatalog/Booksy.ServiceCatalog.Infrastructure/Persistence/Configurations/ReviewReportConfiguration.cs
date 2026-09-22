using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Configurations
{
    public sealed class ReviewReportConfiguration : IEntityTypeConfiguration<ReviewReport>
    {
        public void Configure(EntityTypeBuilder<ReviewReport> builder)
        {
            builder.ToTable("ReviewReports", "ServiceCatalog");

            builder.HasKey(r => r.Id);
            builder.Property(r => r.Id)
                .HasColumnName("ReviewReportId")
                .ValueGeneratedNever();

            builder.Property(r => r.Version)
                .IsConcurrencyToken()
                .HasColumnName("Version")
                .HasDefaultValue(0);

            builder.Property(r => r.ReviewId).IsRequired().HasColumnName("ReviewId");

            builder.Property(r => r.ReportedByUserId)
                .HasConversion(id => id.Value, value => UserId.From(value))
                .IsRequired()
                .HasColumnName("ReportedByUserId");

            builder.Property(r => r.Reason)
                .IsRequired()
                .HasColumnName("Reason")
                .HasMaxLength(ReviewReport.MaxReasonLength);

            builder.Property(r => r.CreatedAt)
                .IsRequired()
                .HasColumnName("CreatedAt")
                .HasColumnType("timestamp with time zone");

            // One report per user per review.
            builder.HasIndex(r => new { r.ReviewId, r.ReportedByUserId })
                .IsUnique()
                .HasDatabaseName("UX_ReviewReports_Review_Reporter");

            builder.HasOne<Review>()
                .WithMany()
                .HasForeignKey(r => r.ReviewId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
