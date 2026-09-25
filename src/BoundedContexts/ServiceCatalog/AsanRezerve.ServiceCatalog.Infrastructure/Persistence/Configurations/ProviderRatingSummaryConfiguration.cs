using AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Reviews;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Configurations
{
    public sealed class ProviderRatingSummaryConfiguration : IEntityTypeConfiguration<ProviderRatingSummary>
    {
        public void Configure(EntityTypeBuilder<ProviderRatingSummary> builder)
        {
            builder.ToTable("ProviderRatingSummaries", "ServiceCatalog");

            builder.HasKey(s => s.ProviderId);
            builder.Property(s => s.ProviderId).HasColumnName("ProviderId").ValueGeneratedNever();

            // Averages of half-star values over many reviews: two decimals is display precision, and the
            // overall average on Providers is unconstrained numeric already.
            builder.Property(s => s.CleanlinessAverage).HasColumnName("CleanlinessAverage").HasPrecision(4, 2);
            builder.Property(s => s.SkillAverage).HasColumnName("SkillAverage").HasPrecision(4, 2);
            builder.Property(s => s.PunctualityAverage).HasColumnName("PunctualityAverage").HasPrecision(4, 2);
            builder.Property(s => s.ConductAverage).HasColumnName("ConductAverage").HasPrecision(4, 2);

            builder.Property(s => s.CleanlinessCount).IsRequired().HasColumnName("CleanlinessCount").HasDefaultValue(0);
            builder.Property(s => s.SkillCount).IsRequired().HasColumnName("SkillCount").HasDefaultValue(0);
            builder.Property(s => s.PunctualityCount).IsRequired().HasColumnName("PunctualityCount").HasDefaultValue(0);
            builder.Property(s => s.ConductCount).IsRequired().HasColumnName("ConductCount").HasDefaultValue(0);

            builder.Property(s => s.UpdatedAt)
                .IsRequired()
                .HasColumnName("UpdatedAt")
                .HasColumnType("timestamp with time zone");
        }
    }
}
