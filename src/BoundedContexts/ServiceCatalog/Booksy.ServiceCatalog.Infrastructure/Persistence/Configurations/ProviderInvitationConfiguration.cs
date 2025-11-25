using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Configurations
{
    public sealed class ProviderInvitationConfiguration : IEntityTypeConfiguration<ProviderInvitation>
    {
        public void Configure(EntityTypeBuilder<ProviderInvitation> builder)
        {
            builder.ToTable("provider_invitations", "ServiceCatalog");

            // Primary Key
            builder.HasKey(pi => pi.Id);

            builder.Property(pi => pi.Id)
                .HasColumnName("id")
                .IsRequired();

            // Organization ID
            builder.Property(pi => pi.OrganizationId)
                .HasConversion(
                    id => id.Value,
                    value => ProviderId.From(value))
                .HasColumnName("organization_id")
                .IsRequired();

            // Phone Number (Value Object)
            builder.OwnsOne(pi => pi.PhoneNumber, phone =>
            {
                phone.Property(ph => ph.Value)
                    .HasMaxLength(20)
                    .HasColumnName("phone_number")
                    .IsRequired();

                phone.Property(ph => ph.CountryCode)
                    .HasMaxLength(5)
                    .HasColumnName("phone_country_code");

                phone.Property(ph => ph.NationalNumber)
                    .HasMaxLength(15)
                    .HasColumnName("phone_national_number");
            });

            // Other Properties
            builder.Property(pi => pi.InviteeName)
                .HasMaxLength(100)
                .HasColumnName("invitee_name");

            builder.Property(pi => pi.Message)
                .HasMaxLength(500)
                .HasColumnName("message");

            builder.Property(pi => pi.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasColumnName("status")
                .IsRequired();

            builder.Property(pi => pi.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            builder.Property(pi => pi.ExpiresAt)
                .HasColumnName("expires_at")
                .IsRequired();

            builder.Property(pi => pi.RespondedAt)
                .HasColumnName("responded_at");

            builder.Property(pi => pi.AcceptedByProviderId)
                .HasConversion(
                    id => id != null ? id.Value : (Guid?)null,
                    value => value.HasValue ? ProviderId.From(value.Value) : null)
                .HasColumnName("accepted_by_provider_id");

            // Indexes
            builder.HasIndex(pi => pi.OrganizationId)
                .HasDatabaseName("IX_ProviderInvitations_OrganizationId");

            builder.HasIndex(pi => pi.Status)
                .HasDatabaseName("IX_ProviderInvitations_Status");

            builder.HasIndex(pi => pi.ExpiresAt)
                .HasDatabaseName("IX_ProviderInvitations_ExpiresAt");
        }
    }
}
