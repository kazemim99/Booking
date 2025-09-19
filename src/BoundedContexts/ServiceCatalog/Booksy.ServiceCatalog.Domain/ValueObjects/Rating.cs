using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.Exceptions;

namespace Booksy.ServiceCatalog.Domain.ValueObjects;

/// <summary>
/// Represents a rating value from 1 to 5 stars with optional half-star precision
/// </summary>
public sealed class Rating : ValueObject
{
    private Rating()
    {
        
    }
    public decimal Value { get; }
    public int Stars => (int)Math.Floor(Value);
    public bool HasHalfStar => Value % 1 == 0.5m;
    public string DisplayText => HasHalfStar ? $"{Stars}.5" : Stars.ToString();
    public int TotalReviews { get; }
    public DateTime? LastUpdated { get; }

    private Rating(decimal value, int totalReviews = 0, DateTime? lastUpdated = null)
    {
        Value = value;
        TotalReviews = totalReviews;
        LastUpdated = lastUpdated ?? DateTime.UtcNow;
    }

    public static Rating Create(decimal value, int totalReviews = 0)
    {
        if (value < 1.0m || value > 5.0m)
        {
            throw new DomainValidationException("Rating must be between 1.0 and 5.0");
        }

        // Only allow whole numbers and half-star values (e.g., 3.0, 3.5, 4.0)
        if (value % 0.5m != 0)
        {
            throw new DomainValidationException("Rating must be in increments of 0.5 (half stars)");
        }

        if (totalReviews < 0)
        {
            throw new DomainValidationException("Total reviews cannot be negative");
        }

        return new Rating(value, totalReviews);
    }

    public static Rating FromStars(int stars, int totalReviews = 0)
    {
        if (stars < 1 || stars > 5)
        {
            throw new DomainValidationException("Star rating must be between 1 and 5");
        }

        return new Rating(stars, totalReviews);
    }

    public static Rating FromStarsWithHalf(int stars, bool hasHalf = false, int totalReviews = 0)
    {
        if (stars < 1 || stars > 5)
        {
            throw new DomainValidationException("Star rating must be between 1 and 5");
        }

        if (stars == 5 && hasHalf)
        {
            throw new DomainValidationException("Cannot have 5.5 stars - maximum is 5.0");
        }

        var value = hasHalf ? stars + 0.5m : stars;
        return new Rating(value, totalReviews);
    }

    public static Rating Average(IEnumerable<Rating> ratings)
    {
        if (!ratings.Any())
        {
            throw new DomainValidationException("Cannot calculate average of empty rating collection");
        }

        var ratingList = ratings.ToList();
        var average = ratingList.Average(r => r.Value);
        var totalReviews = ratingList.Sum(r => r.TotalReviews);

        // Round to nearest half-star
        var roundedAverage = Math.Round(average * 2, MidpointRounding.AwayFromZero) / 2;

        return new Rating(Math.Max(1.0m, Math.Min(5.0m, roundedAverage)), totalReviews);
    }

    public static Rating WeightedAverage(IEnumerable<(Rating rating, decimal weight)> weightedRatings)
    {
        if (!weightedRatings.Any())
        {
            throw new DomainValidationException("Cannot calculate weighted average of empty rating collection");
        }

        var items = weightedRatings.ToList();
        var totalWeight = items.Sum(x => x.weight);

        if (totalWeight <= 0)
        {
            throw new DomainValidationException("Total weight must be greater than zero");
        }

        var weightedSum = items.Sum(x => x.rating.Value * x.weight);
        var weightedAverage = weightedSum / totalWeight;
        var totalReviews = items.Sum(x => x.rating.TotalReviews);

        // Round to nearest half-star
        var roundedAverage = Math.Round(weightedAverage * 2, MidpointRounding.AwayFromZero) / 2;

        return new Rating(Math.Max(1.0m, Math.Min(5.0m, roundedAverage)), totalReviews);
    }

    /// <summary>
    /// Calculate new rating when a review is added
    /// </summary>
    public Rating AddReview(decimal newRatingValue)
    {
        if (newRatingValue < 1.0m || newRatingValue > 5.0m)
        {
            throw new DomainValidationException("New rating must be between 1.0 and 5.0");
        }

        var currentTotal = Value * TotalReviews;
        var newTotal = currentTotal + newRatingValue;
        var newCount = TotalReviews + 1;
        var newAverage = newTotal / newCount;

        // Round to nearest half-star
        var roundedAverage = Math.Round(newAverage * 2, MidpointRounding.AwayFromZero) / 2;

        return new Rating(Math.Max(1.0m, Math.Min(5.0m, roundedAverage)), newCount, DateTime.UtcNow);
    }

    /// <summary>
    /// Calculate new rating when a review is removed
    /// </summary>
    public Rating RemoveReview(decimal removedRatingValue)
    {
        if (TotalReviews <= 1)
        {
            throw new DomainValidationException("Cannot remove review when there is only one or no reviews");
        }

        if (removedRatingValue < 1.0m || removedRatingValue > 5.0m)
        {
            throw new DomainValidationException("Removed rating must be between 1.0 and 5.0");
        }

        var currentTotal = Value * TotalReviews;
        var newTotal = currentTotal - removedRatingValue;
        var newCount = TotalReviews - 1;
        var newAverage = newTotal / newCount;

        // Round to nearest half-star
        var roundedAverage = Math.Round(newAverage * 2, MidpointRounding.AwayFromZero) / 2;

        return new Rating(Math.Max(1.0m, Math.Min(5.0m, roundedAverage)), newCount, DateTime.UtcNow);
    }

    public bool IsExcellent() => Value >= 4.5m;
    public bool IsGood() => Value >= 3.5m && Value < 4.5m;
    public bool IsAverage() => Value >= 2.5m && Value < 3.5m;
    public bool IsPoor() => Value < 2.5m;

    /// <summary>
    /// Get rating quality level
    /// </summary>
    public RatingQuality GetQuality()
    {
        return Value switch
        {
            >= 4.5m => RatingQuality.Excellent,
            >= 3.5m => RatingQuality.Good,
            >= 2.5m => RatingQuality.Average,
            >= 1.5m => RatingQuality.Poor,
            _ => RatingQuality.VeryPoor
        };
    }

    /// <summary>
    /// Get rating confidence based on number of reviews
    /// </summary>
    public RatingConfidence GetConfidence()
    {
        return TotalReviews switch
        {
            >= 100 => RatingConfidence.VeryHigh,
            >= 50 => RatingConfidence.High,
            >= 20 => RatingConfidence.Medium,
            >= 5 => RatingConfidence.Low,
            _ => RatingConfidence.VeryLow
        };
    }

    /// <summary>
    /// Check if rating is statistically significant
    /// </summary>
    public bool IsStatisticallySignificant(int minimumReviews = 5)
    {
        return TotalReviews >= minimumReviews;
    }

    /// <summary>
    /// Get star representation as string (e.g., "★★★★☆")
    /// </summary>
    public string ToStarString(char fullStar = '★', char halfStar = '⭐', char emptyStar = '☆')
    {
        var stars = "";
        var fullStars = (int)Math.Floor(Value);
        var hasHalf = HasHalfStar;

        // Add full stars
        for (int i = 0; i < fullStars; i++)
        {
            stars += fullStar;
        }

        // Add half star if applicable
        if (hasHalf)
        {
            stars += halfStar;
        }

        // Add empty stars to make 5 total
        var remainingStars = 5 - fullStars - (hasHalf ? 1 : 0);
        for (int i = 0; i < remainingStars; i++)
        {
            stars += emptyStar;
        }

        return stars;
    }

    /// <summary>
    /// Get percentage representation (0-100%)
    /// </summary>
    public decimal ToPercentage()
    {
        return (Value - 1) / 4 * 100; // Convert 1-5 scale to 0-100%
    }

    /// <summary>
    /// Compare with another rating and get difference
    /// </summary>
    public decimal CompareTo(Rating other)
    {
        return Value - other.Value;
    }

    /// <summary>
    /// Check if rating is better than another by a significant margin
    /// </summary>
    public bool IsSignificantlyBetterThan(Rating other, decimal threshold = 0.5m)
    {
        return Value - other.Value >= threshold;
    }

    /// <summary>
    /// Get display text with review count
    /// </summary>
    public string GetDisplayWithCount()
    {
        var reviewText = TotalReviews switch
        {
            0 => "No reviews",
            1 => "1 review",
            _ => $"{TotalReviews:N0} reviews"
        };

        return $"{Value:F1} stars ({reviewText})";
    }

    /// <summary>
    /// Check if rating needs more reviews to be reliable
    /// </summary>
    public bool NeedsMoreReviews(int targetReviews = 10)
    {
        return TotalReviews < targetReviews;
    }

    /// <summary>
    /// Get color representation for UI (hex colors)
    /// </summary>
    public string GetColor()
    {
        return Value switch
        {
            >= 4.5m => "#4CAF50", // Green - Excellent
            >= 3.5m => "#8BC34A", // Light Green - Good  
            >= 2.5m => "#FFC107", // Yellow - Average
            >= 1.5m => "#FF9800", // Orange - Poor
            _ => "#F44336"         // Red - Very Poor
        };
    }

    public Rating Add(Rating other)
    {
        var sum = Value + other.Value;
        return Create(Math.Min(5.0m, sum));
    }

    public static implicit operator decimal(Rating rating) => rating.Value;

    public static bool operator >(Rating left, Rating right) => left.Value > right.Value;
    public static bool operator <(Rating left, Rating right) => left.Value < right.Value;
    public static bool operator >=(Rating left, Rating right) => left.Value >= right.Value;
    public static bool operator <=(Rating left, Rating right) => left.Value <= right.Value;

  
    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
        yield return TotalReviews;
    }

    public override string ToString() => $"{Value:F1} stars";

    /// <summary>
    /// Format rating for different display contexts
    /// </summary>
    public string ToString(RatingDisplayFormat format)
    {
        return format switch
        {
            RatingDisplayFormat.Short => $"{Value:F1}",
            RatingDisplayFormat.Stars => ToStarString(),
            RatingDisplayFormat.WithCount => GetDisplayWithCount(),
            RatingDisplayFormat.Percentage => $"{ToPercentage():F0}%",
            RatingDisplayFormat.Quality => GetQuality().ToString(),
            _ => ToString()
        };
    }
}

/// <summary>
/// Rating quality levels
/// </summary>
public enum RatingQuality
{
    VeryPoor,
    Poor,
    Average,
    Good,
    Excellent
}

/// <summary>
/// Rating confidence levels based on review count
/// </summary>
public enum RatingConfidence
{
    VeryLow,
    Low,
    Medium,
    High,
    VeryHigh
}

/// <summary>
/// Rating display format options
/// </summary>
public enum RatingDisplayFormat
{
    Default,
    Short,
    Stars,
    WithCount,
    Percentage,
    Quality
}