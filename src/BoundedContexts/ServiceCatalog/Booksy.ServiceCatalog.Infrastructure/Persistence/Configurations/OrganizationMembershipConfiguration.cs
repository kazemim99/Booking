using System.Text.Json;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates.OrganizationMembershipAggregate;
using Booksy.ServiceCatalog.Domain.Aggregates.OrganizationMembershipAggregate.Entities;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Configurations
{
    public sealed class OrganizationMembershipConfiguration : IEntityTypeConfiguration<OrganizationMembership>
    {
        public void Configure(EntityTypeBuilder<OrganizationMembership> builder)
        {
            builder.ToTable("organization_memberships", "ServiceCatalog");

            builder.HasKey(m => m.Id);
            builder.Property(m => m.Id).HasColumnName("id").IsRequired();

            // Person (a UserManagement UserId). Nullable to allow legacy staff records
            // migrated without a linked account (claimable by phone later).
            builder.Property(m => m.PersonId)
                .HasConversion(
                    id => id != null ? id.Value : (Guid?)null,
                    value => value.HasValue ? UserId.From(value.Value) : null)
                .HasColumnName("person_id");

            builder.Property(m => m.OrganizationId)
                .HasConversion(
                    id => id.Value,
                    value => ProviderId.From(value))
                .HasColumnName("organization_id")
                .IsRequired();

            builder.Property(m => m.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasColumnName("status")
                .IsRequired();

            // Org-scoped roles: a small set persisted as a comma-separated string.
            // Phase 1 keeps this inline; a dedicated membership_roles table is a later
            // option if role-level querying is needed.
            var rolesConverter = new ValueConverter<IReadOnlyCollection<MembershipRole>, string>(
                v => string.Join(',', v.Select(r => r.ToString())),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                      .Select(Enum.Parse<MembershipRole>)
                      .ToHashSet());

            var rolesComparer = new ValueComparer<IReadOnlyCollection<MembershipRole>>(
                (a, b) => a != null && b != null && a.OrderBy(r => r).SequenceEqual(b.OrderBy(r => r)),
                // Hash over the ordered set so equal sets in any order hash equally
                // (equals is order-independent — the hash must be too).
                v => v.OrderBy(r => r).Aggregate(0, (hash, r) => HashCode.Combine(hash, r.GetHashCode())),
                v => v.ToHashSet());

            builder.Property(m => m.Roles)
                .HasConversion(rolesConverter, rolesComparer)
                .HasColumnName("roles")
                .HasMaxLength(200)
                .IsRequired()
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.Property(m => m.InvitedAt).HasColumnName("invited_at");
            builder.Property(m => m.JoinedAt).HasColumnName("joined_at");
            builder.Property(m => m.LeftAt).HasColumnName("left_at");
            builder.Property(m => m.TerminationReason)
                .HasMaxLength(500)
                .HasColumnName("termination_reason");

            // Auditable base columns
            builder.Property(m => m.CreatedAt).HasColumnName("created_at");
            builder.Property(m => m.CreatedBy).HasColumnName("created_by");
            builder.Property(m => m.LastModifiedAt).HasColumnName("last_modified_at");
            builder.Property(m => m.LastModifiedBy).HasColumnName("last_modified_by");
            builder.Property(m => m.IsDeleted).HasColumnName("is_deleted");

            // StaffProfile: an optional owned entity in its own table (a NULL row => no
            // profile). Separate-table ownership lets EF detect absence reliably, which
            // an inline optional owned type with a non-nullable bool cannot.
            builder.OwnsOne(m => m.StaffProfile, sp =>
            {
                sp.ToTable("staff_profiles", "ServiceCatalog");
                sp.WithOwner().HasForeignKey("membership_id");
                sp.Property<Guid>("membership_id").HasColumnName("membership_id");
                sp.HasKey("membership_id");

                sp.Property(p => p.ProvidesServices)
                    .HasColumnName("provides_services")
                    .IsRequired();

                sp.Property(p => p.BioOverride)
                    .HasMaxLength(500)
                    .HasColumnName("bio_override");

                sp.Property(p => p.DisplayName)
                    .HasMaxLength(200)
                    .HasColumnName("display_name");

                sp.Property(p => p.PhotoUrl)
                    .HasMaxLength(500)
                    .HasColumnName("photo_url");

                // Both collections are mapped through their backing fields, so the
                // read-only projections the domain exposes must be left unmapped —
                // otherwise EF discovers WorkingDays as a second, owner-less navigation.
                sp.Ignore(p => p.WorkingDays);
                sp.Ignore(p => p.ServiceIds);

                // Which of the salon's services this member performs. Empty = all of them,
                // so the common case stores an empty array rather than a row per service.
                sp.Property<List<Guid>>("_serviceIds")
                    .HasColumnName("service_ids")
                    .HasColumnType("jsonb")
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>(),
                        new ValueComparer<List<Guid>>(
                            (a, b) => (a ?? new List<Guid>()).SequenceEqual(b ?? new List<Guid>()),
                            c => c == null ? 0 : c.Aggregate(0, (h, v) => HashCode.Combine(h, v.GetHashCode())),
                            c => c == null ? new List<Guid>() : c.ToList()));

                // The member's working week at this salon, as its own table so a day can be
                // queried and edited individually. Empty = the salon's own opening hours.
                sp.OwnsMany<StaffWorkingDay>("_workingDays", wd =>
                {
                    wd.ToTable("staff_working_days", "ServiceCatalog");
                    wd.WithOwner().HasForeignKey("membership_id");
                    wd.Property<Guid>("membership_id").HasColumnName("membership_id");
                    wd.Property<int>("Id").ValueGeneratedOnAdd().HasColumnName("id");
                    wd.HasKey("Id");

                    wd.Property(d => d.DayOfWeek)
                        .HasConversion<string>()
                        .HasMaxLength(20)
                        .IsRequired()
                        .HasColumnName("day_of_week");

                    wd.Property(d => d.StartTime).IsRequired().HasColumnName("start_time");
                    wd.Property(d => d.EndTime).IsRequired().HasColumnName("end_time");

                    wd.HasIndex("membership_id").HasDatabaseName("ix_staff_working_days_membership");
                });

                sp.Navigation("_workingDays").UsePropertyAccessMode(PropertyAccessMode.Field);
            });
            builder.Navigation(m => m.StaffProfile).IsRequired(false);

            builder.HasIndex(m => m.OrganizationId)
                .HasDatabaseName("ix_membership_org");

            builder.HasIndex(m => m.PersonId)
                .HasDatabaseName("ix_membership_person");

            // One live (non-terminated) membership per (person, org). NULL person_id
            // rows (claimable legacy staff) are exempt: Postgres treats NULLs as distinct.
            builder.HasIndex(m => new { m.PersonId, m.OrganizationId })
                .HasDatabaseName("ux_membership_person_org_active")
                .IsUnique()
                .HasFilter("status <> 'Terminated' AND person_id IS NOT NULL");
        }
    }
}
