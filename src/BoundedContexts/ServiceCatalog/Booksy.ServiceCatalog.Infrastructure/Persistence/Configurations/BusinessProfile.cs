//// Booksy.ServiceCatalog.Infrastructure/Persistence/Configurations/BusinessProfileConfiguration.cs
//using Booksy.ServiceCatalog.Domain.Aggregates;
//using Booksy.ServiceCatalog.Domain.Entities;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.ChangeTracking;
//using Microsoft.EntityFrameworkCore.Metadata.Builders;
//using System.Text.Json;

//namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Configurations
//{
//    public sealed class BusinessProfileConfiguration : IEntityTypeConfiguration<BusinessProfile>
//    {
//        public void Configure(EntityTypeBuilder<BusinessProfile> builder)
//        {
//            builder.ToTable("BusinessProfiles", "ServiceCatalog");

//            // Primary Key
//            builder.HasKey(bp => bp.Id);

//            // Business Name
//            builder.Property(bp => bp.BusinessName)
//                .IsRequired()
//                .HasMaxLength(200);

//            // Description
//            builder.Property(bp => bp.Description)
//                .IsRequired()
//                .HasMaxLength(2000);

//            // Website
//            builder.Property(bp => bp.Website)
//                .HasMaxLength(500);

//            // Logo URL
//            builder.Property(bp => bp.LogoUrl)
//                .HasMaxLength(500);

//            // ✅ SocialMedia as JSON (Dictionary<string, string>)
//            builder.Property(bp => bp.SocialMedia)
//                .HasColumnName("SocialMedia")
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

//            // ✅ Tags as JSON (List<string>)
//            builder.Property(bp => bp.Tags)
//                .HasColumnName("Tags")
//                .HasColumnType("jsonb")  // Use 'nvarchar(max)' for SQL Server
//                .HasConversion(
//                    tags => JsonSerializer.Serialize(tags, (JsonSerializerOptions?)null),
//                    json => JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null)
//                            ?? new List<string>(),
//                    new ValueComparer<List<string>>(
//                        (c1, c2) => c1!.SequenceEqual(c2!),
//                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
//                        c => c.ToList()
//                    ))
//                .IsRequired(false);

//            // Last Updated
//            builder.Property(bp => bp.LastUpdatedAt)
//                .IsRequired()
//                .HasColumnType("timestamp with time zone");

//            // Indexes
//            builder.HasIndex(bp => bp.BusinessName)
//                .HasDatabaseName("IX_BusinessProfiles_BusinessName")
//                .IsUnique();

//            builder.HasIndex(bp => bp.LastUpdatedAt)
//                .HasDatabaseName("IX_BusinessProfiles_LastUpdatedAt");
//        }
//    }
//}

