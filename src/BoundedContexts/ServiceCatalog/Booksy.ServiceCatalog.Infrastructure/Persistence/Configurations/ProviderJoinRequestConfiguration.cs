using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Configurations
{
    public sealed class ProviderJoinRequestConfiguration : IEntityTypeConfiguration<ProviderJoinRequest>
    {
        public void Configure(EntityTypeBuilder<ProviderJoinRequest> builder)
        {
            builder.ToTable("provider_join_requests", "ServiceCatalog");

            // Primary Key
            builder.HasKey(pjr => pjr.Id);

            builder.Property(pjr => pjr.Id)
                .HasColumnName("id")
                .IsRequired();

            // Organization ID
            builder.Property(pjr => pjr.OrganizationId)
                .HasConversion(
                    id => id.Value,
                    value => ProviderId.From(value))
                .HasColumnName("organization_id")
                .IsRequired();

            // Requester ID
            builder.Property(pjr => pjr.RequesterId)
                .HasConversion(
                    id => id.Value,
                    value => ProviderId.From(value))
                .HasColumnName("requester_id")
                .IsRequired();

            // Other Properties
            builder.Property(pjr => pjr.Message)
                .HasMaxLength(500)
                .HasColumnName("message");

            builder.Property(pjr => pjr.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasColumnName("status")
                .IsRequired();

            builder.Property(pjr => pjr.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            builder.Property(pjr => pjr.ReviewedAt)
                .HasColumnName("reviewed_at");

            builder.Property(pjr => pjr.ReviewedBy)
                .HasColumnName("reviewed_by");

            builder.Property(pjr => pjr.ReviewNote)
                .HasMaxLength(500)
                .HasColumnName("review_note");

            // Indexes
            builder.HasIndex(pjr => pjr.OrganizationId)
                .HasDatabaseName("IX_ProviderJoinRequests_OrganizationId");

            builder.HasIndex(pjr => pjr.RequesterId)
                .HasDatabaseName("IX_ProviderJoinRequests_RequesterId");

            builder.HasIndex(pjr => pjr.Status)
                .HasDatabaseName("IX_ProviderJoinRequests_Status");

            // Composite index for querying pending requests by organization
            builder.HasIndex(pjr => new { pjr.OrganizationId, pjr.Status })
                .HasDatabaseName("IX_ProviderJoinRequests_OrgId_Status");
        }
    }
}
