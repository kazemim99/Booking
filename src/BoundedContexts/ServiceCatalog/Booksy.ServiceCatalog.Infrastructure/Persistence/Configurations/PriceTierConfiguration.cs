//// Booksy.ServiceCatalog.Infrastructure/Persistence/Configurations/PriceTierConfiguration.cs
//using Booksy.ServiceCatalog.Domain.Entities;
//using Booksy.ServiceCatalog.Domain.ValueObjects;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.ChangeTracking;
//using Microsoft.EntityFrameworkCore.Metadata.Builders;
//using System.Text.Json;

//namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Configurations
//{
//    public sealed class PriceTierConfiguration : IEntityTypeConfiguration<PriceTier>
//    {
//        public void Configure(EntityTypeBuilder<PriceTier> builder)
//        {
//            builder.ToTable("PriceTiers", "servicecatalog");

//            // Primary Key
//            builder.HasKey(pt => pt.Id);

//            // Properties
//            builder.Property(pt => pt.Name)
//                .IsRequired()
//                .HasMaxLength(100);

//            builder.Property(pt => pt.Description)
//                .HasMaxLength(500);

//            // ✅ Price (Owned Value Object)
//            builder.OwnsOne(pt => pt.Price, price =>
//            {
//                price.Property(p => p.Amount)
//                    .HasPrecision(18, 2)
//                    .IsRequired()
//                    .HasColumnName("PriceAmount");

//                price.Property(p => p.Currency)
//                    .HasMaxLength(3)
//                    .IsRequired()
//                    .HasColumnName("PriceCurrency");
//            });

//            // Boolean flags
//            builder.Property(pt => pt.IsDefault)
//                .IsRequired()
//                .HasDefaultValue(false);

//            builder.Property(pt => pt.IsActive)
//                .IsRequired()
//                .HasDefaultValue(true);

//            // Sort order
//            builder.Property(pt => pt.SortOrder)
//                .IsRequired()
//                .HasDefaultValue(0);

//            // ✅ Attributes as JSON (Dictionary<string, string>)
//            builder.Property(pt => pt.Attributes)
//                .HasColumnName("Attributes")
//                .HasColumnType("jsonb")  // Use 'nvarchar(max)' for SQL Server
//                .HasConversion(
//                    dict => JsonSerializer.Serialize(dict, (JsonSerializerOptions?)null),
//                    json => JsonSerializer.Deserialize<Dictionary<string, string>>(json, (JsonSerializerOptions?)null)
//                            ?? new Dictionary<string, string>(),
//                    new ValueComparer<Dictionary<string, string>>(
//                        (c1, c2) => c1!.SequenceEqual(c2!),
//                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
//                        c => c.ToDictionary(x => x.Key, x => x.Value)
//                    ))
//                .IsRequired(false);

//            // Timestamp
//            builder.Property(pt => pt.CreatedAt)
//                .IsRequired()
//                .HasColumnType("timestamp with time zone")
//                .HasDefaultValueSql("CURRENT_TIMESTAMP");

//            // ✅ Indexes
//            builder.HasIndex(pt => pt.IsActive)
//                .HasDatabaseName("IX_PriceTiers_IsActive");

//            builder.HasIndex(pt => pt.IsDefault)
//                .HasDatabaseName("IX_PriceTiers_IsDefault");

//            builder.HasIndex(pt => pt.SortOrder)
//                .HasDatabaseName("IX_PriceTiers_SortOrder");

//            builder.HasIndex(pt => pt.CreatedAt)
//                .HasDatabaseName("IX_PriceTiers_CreatedAt");

//            // Composite index for querying active tiers by sort order
//            builder.HasIndex(pt => new { pt.IsActive, pt.SortOrder })
//                .HasDatabaseName("IX_PriceTiers_Active_SortOrder");
//        }
//    }
//}