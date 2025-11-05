using Booksy.Core.Domain.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booksy.Infrastructure.Core.Persistence.Configurations
{
    /// <summary>
    /// Entity Framework configuration for the ProvinceCities entity
    /// </summary>
    public sealed class ProvinceCitiesConfiguration : IEntityTypeConfiguration<ProvinceCities>
    {
        public void Configure(EntityTypeBuilder<ProvinceCities> builder)
        {
            // Table name
            builder.ToTable("ProvinceCities", "ServiceCatalog");

            // Primary key
            builder.HasKey(l => l.Id);

            // Properties
            builder.Property(l => l.Id)
                .ValueGeneratedNever(); // We're using IDs from the JSON file

            builder.Property(l => l.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(l => l.Type)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(l => l.ProvinceCode)
                .IsRequired();

            builder.Property(l => l.CityCode)
                .IsRequired(false); // Nullable for provinces

            builder.Property(l => l.ParentId)
                .IsRequired(false); // Nullable for provinces (root nodes)

            // Self-referencing relationship (Parent-Child hierarchy)
            builder.HasOne(l => l.Parent)
                .WithMany(l => l.Children)
                .HasForeignKey(l => l.ParentId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

            // Indexes for better query performance
            builder.HasIndex(l => l.ParentId)
                .HasDatabaseName("IX_ProvinceCities_ParentId");

            builder.HasIndex(l => l.ProvinceCode)
                .HasDatabaseName("IX_ProvinceCities_ProvinceCode");

            builder.HasIndex(l => l.Type)
                .HasDatabaseName("IX_ProvinceCities_Type");

            builder.HasIndex(l => new { l.ProvinceCode, l.CityCode })
                .HasDatabaseName("IX_ProvinceCities_ProvinceCode_CityCode");
        }
    }
}
