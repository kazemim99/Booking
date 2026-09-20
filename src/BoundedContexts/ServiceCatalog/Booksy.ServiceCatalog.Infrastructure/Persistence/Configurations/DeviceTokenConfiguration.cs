using Booksy.ServiceCatalog.Infrastructure.Persistence.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Configurations
{
    public sealed class DeviceTokenConfiguration : IEntityTypeConfiguration<DeviceToken>
    {
        public void Configure(EntityTypeBuilder<DeviceToken> builder)
        {
            builder.ToTable("DeviceTokens", "ServiceCatalog");

            builder.HasKey(d => d.Id);
            builder.Property(d => d.Id).ValueGeneratedNever();

            // FCM registration tokens run to a few hundred characters and the length is not contractual,
            // so this is generous rather than exact.
            builder.Property(d => d.Token).HasMaxLength(512).IsRequired();

            builder.Property(d => d.Platform)
                .HasConversion<string>()
                .HasMaxLength(16)
                .IsRequired();

            builder.Property(d => d.RegisteredAt).HasColumnType("timestamp with time zone").IsRequired();
            builder.Property(d => d.LastSeenAt).HasColumnType("timestamp with time zone").IsRequired();
            builder.Property(d => d.RevokedAt).HasColumnType("timestamp with time zone");

            // One row per device, enforced by the database. This is what makes "reassign, never duplicate"
            // true even when two sign-ins race: the second must update the existing row, it cannot insert.
            builder.HasIndex(d => d.Token)
                .IsUnique()
                .HasDatabaseName("UX_DeviceTokens_Token");

            // The send path's query: this person's live devices.
            builder.HasIndex(d => new { d.UserId, d.RevokedAt })
                .HasDatabaseName("IX_DeviceTokens_User");
        }
    }
}
