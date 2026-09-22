namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Reviews
{
    /// <summary>
    /// A provider's per-dimension averages, derived from their published reviews.
    /// </summary>
    /// <remarks>
    /// <para>Derived data, not business state: it has no invariants of its own and is overwritten wholesale by the
    /// rating recompute whenever the set of published reviews changes. Hence a persistence type here rather than a
    /// domain aggregate — the same placement as <c>NotificationOutboxEntry</c>.</para>
    ///
    /// <para>Kept beside <c>Providers</c> rather than on it: the overall average and count live on the provider
    /// because search sorts by them, but only the profile renders these, and the search table is the hot path.</para>
    ///
    /// <para>Each average is over the reviews that rated that dimension only; its count says how many that was.
    /// A null average with a zero count means no one has rated it yet — never zero stars.</para>
    /// </remarks>
    public sealed class ProviderRatingSummary
    {
        public Guid ProviderId { get; private set; }

        public decimal? CleanlinessAverage { get; private set; }
        public int CleanlinessCount { get; private set; }

        public decimal? SkillAverage { get; private set; }
        public int SkillCount { get; private set; }

        public decimal? PunctualityAverage { get; private set; }
        public int PunctualityCount { get; private set; }

        public decimal? ConductAverage { get; private set; }
        public int ConductCount { get; private set; }

        public DateTime UpdatedAt { get; private set; }

        private ProviderRatingSummary() { }

        public static ProviderRatingSummary For(Guid providerId) => new() { ProviderId = providerId };

        public void Overwrite(DimensionAggregate cleanliness, DimensionAggregate skill, DimensionAggregate punctuality,
            DimensionAggregate conduct, DateTime utcNow)
        {
            (CleanlinessAverage, CleanlinessCount) = cleanliness;
            (SkillAverage, SkillCount) = skill;
            (PunctualityAverage, PunctualityCount) = punctuality;
            (ConductAverage, ConductCount) = conduct;
            UpdatedAt = utcNow;
        }
    }

    /// <summary>One dimension's average and how many reviews it is over. Null average ⇔ zero count.</summary>
    public readonly record struct DimensionAggregate(decimal? Average, int Count)
    {
        public static readonly DimensionAggregate None = new(null, 0);
    }
}
