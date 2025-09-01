using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.Exceptions;

namespace Booksy.Core.Domain.ValueObjects;

/// <summary>
/// Represents a geographical location with latitude and longitude coordinates.
/// </summary>
public sealed class GeoLocation : ValueObject
{
    public double Latitude { get; }
    public double Longitude { get; }

    private GeoLocation(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public static GeoLocation Create(double latitude, double longitude)
    {
        if (latitude < -90 || latitude > 90)
            throw new DomainValidationException("Latitude", "Latitude must be between -90 and 90 degrees");

        if (longitude < -180 || longitude > 180)
            throw new DomainValidationException("Longitude", "Longitude must be between -180 and 180 degrees");

        return new GeoLocation(latitude, longitude);
    }

    /// <summary>
    /// Calculates the distance between two locations using the Haversine formula
    /// </summary>
    /// <param name="other">The other location</param>
    /// <returns>Distance in kilometers</returns>
    public double DistanceTo(GeoLocation other)
    {
        const double earthRadiusKm = 6371;

        var latRad1 = ToRadians(Latitude);
        var latRad2 = ToRadians(other.Latitude);
        var deltaLat = ToRadians(other.Latitude - Latitude);
        var deltaLon = ToRadians(other.Longitude - Longitude);

        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(latRad1) * Math.Cos(latRad2) *
                Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return earthRadiusKm * c;
    }

    /// <summary>
    /// Checks if this location is within a specified radius of another location
    /// </summary>
    /// <param name="center">The center location</param>
    /// <param name="radiusKm">The radius in kilometers</param>
    /// <returns>True if within radius, false otherwise</returns>
    public bool IsWithinRadius(GeoLocation center, double radiusKm)
    {
        return DistanceTo(center) <= radiusKm;
    }

    private static double ToRadians(double degrees) => degrees * (Math.PI / 180);

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Latitude;
        yield return Longitude;
    }

    public override string ToString() => $"({Latitude:F6}, {Longitude:F6})";
}