using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates.MembershipAuditAggregate;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Configurations
{
    public sealed class MembershipAuditEntryConfiguration : IEntityTypeConfiguration<MembershipAuditEntry>
    {
        public void Configure(EntityTypeBuilder<MembershipAuditEntry> builder)
        {
            builder.ToTable("membership_audit_entries", "ServiceCatalog");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("id").IsRequired();

            builder.Property(e => e.MembershipId).HasColumnName("membership_id");
            builder.Property(e => e.InvitationId).HasColumnName("invitation_id");

            builder.Property(e => e.OrganizationId)
                .HasConversion(id => id.Value, value => ProviderId.From(value))
                .HasColumnName("organization_id")
                .IsRequired();

            builder.Property(e => e.SubjectPersonId)
                .HasConversion(
                    id => id != null ? id.Value : (Guid?)null,
                    value => value.HasValue ? UserId.From(value.Value) : null)
                .HasColumnName("subject_person_id");

            builder.Property(e => e.ActorPersonId)
                .HasConversion(
                    id => id != null ? id.Value : (Guid?)null,
                    value => value.HasValue ? UserId.From(value.Value) : null)
                .HasColumnName("actor_person_id");

            builder.Property(e => e.Action)
                .HasConversion<string>().HasMaxLength(40)
                .HasColumnName("action").IsRequired();

            builder.Property(e => e.StatusAfter)
                .HasConversion<string>().HasMaxLength(20)
                .HasColumnName("status_after").IsRequired();

            builder.Property(e => e.RolesSnapshot).HasMaxLength(200).HasColumnName("roles_snapshot");
            builder.Property(e => e.Reason).HasMaxLength(500).HasColumnName("reason");
            builder.Property(e => e.OccurredAt).HasColumnName("occurred_at").IsRequired();

            // Auditable base columns
            builder.Property(e => e.CreatedAt).HasColumnName("created_at");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.LastModifiedAt).HasColumnName("last_modified_at");
            builder.Property(e => e.LastModifiedBy).HasColumnName("last_modified_by");
            builder.Property(e => e.IsDeleted).HasColumnName("is_deleted");

            builder.HasIndex(e => e.MembershipId).HasDatabaseName("ix_membership_audit_membership");
            builder.HasIndex(e => new { e.OrganizationId, e.OccurredAt })
                .HasDatabaseName("ix_membership_audit_org_time");
        }
    }
}
