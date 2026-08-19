using Booksy.ServiceCatalog.Infrastructure.Persistence.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Configurations
{
    public sealed class NotificationDeliveryConfiguration : IEntityTypeConfiguration<NotificationDelivery>
    {
        public void Configure(EntityTypeBuilder<NotificationDelivery> builder)
        {
            builder.ToTable("NotificationDeliveries", "ServiceCatalog");

            // Composite primary key = the dedup tuple. Exactly one concurrent INSERT of a given
            // (EventId, Channel, Recipient) succeeds; the losers read the winner's row and stand down.
            builder.HasKey(d => new { d.EventId, d.Channel, d.Recipient });

            builder.Property(d => d.Channel).HasMaxLength(50).IsRequired();
            builder.Property(d => d.Recipient).HasMaxLength(320).IsRequired();
            builder.Property(d => d.Status).HasMaxLength(20).IsRequired();
            builder.Property(d => d.GatewayMessageId).HasMaxLength(200);
            builder.Property(d => d.ErrorMessage).HasMaxLength(1000);
            builder.Property(d => d.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
            builder.Property(d => d.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();

            builder.HasIndex(d => d.NotificationId).HasDatabaseName("IX_NotificationDeliveries_NotificationId");
            builder.HasIndex(d => d.CreatedAt).HasDatabaseName("IX_NotificationDeliveries_CreatedAt");
        }
    }
}
