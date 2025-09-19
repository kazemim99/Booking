using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.Exceptions;

namespace Booksy.ServiceCatalog.Domain.ValueObjects;

/// <summary>
/// Represents geographic coordinates (latitude and longitude) with distance calculation capabilities
/// </summary>
public sealed class Coordinates : ValueObject
{

    private Coordinates()
    {
        
    }
    public double Latitude { get; }
    public double Longitude { get; }

    // Pre-calculated for performance
    private readonly double _latitudeRadians;
    private readonly double _longitudeRadians;

    private Coordinates(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
        _latitudeRadians = ToRadians(latitude);
        _longitudeRadians = ToRadians(longitude);
    }

    public static Coordinates Create(double latitude, double longitude)
    {
        if (latitude < -90.0 || latitude > 90.0)
        {
            throw new DomainValidationException("Latitude must be between -90 and 90 degrees");
        }

        if (longitude < -180.0 || longitude > 180.0)
        {
            throw new DomainValidationException("Longitude must be between -180 and 180 degrees");
        }

        return new Coordinates(latitude, longitude);
    }

    public static Coordinates FromString(string coordinates)
    {
        if (string.IsNullOrWhiteSpace(coordinates))
        {
            throw new DomainValidationException("Coordinates string cannot be null or empty");
        }

        var parts = coordinates.Split(',', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 2)
        {
            throw new DomainValidationException("Coordinates must be in format 'latitude,longitude'");
        }

        if (!double.TryParse(parts[0].Trim(), out var latitude) ||
            !double.TryParse(parts[1].Trim(), out var longitude))
        {
            throw new DomainValidationException("Invalid coordinate format - must be numeric values");
        }

        return Create(latitude, longitude);
    }

    /// <summary>
    /// Calculate distance to another coordinate using Haversine formula
    /// </summary>
    /// <param name="other">The target coordinates</param>
    /// <returns>Distance in kilometers</returns>
    public double DistanceToInKilometers(Coordinates other)
    {
        const double earthRadiusKm = 6371.0;

        var deltaLat = other._latitudeRadians - _latitudeRadians;
        var deltaLon = other._longitudeRadians - _longitudeRadians;

        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(_latitudeRadians) * Math.Cos(other._latitudeRadians) *
                Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return earthRadiusKm * c;
    }

    /// <summary>
    /// Calculate distance in miles
    /// </summary>
    public double DistanceToInMiles(Coordinates other)
    {
        return DistanceToInKilometers(other) * 0.621371;
    }

    /// <summary>
    /// Check if coordinates are within specified radius (in kilometers)
    /// </summary>
    public bool IsWithinRadius(Coordinates center, double radiusKm)
    {
        if (radiusKm <= 0)
        {
            throw new DomainValidationException("Radius must be greater than 0");
        }

        return DistanceToInKilometers(center) <= radiusKm;
    }

    /// <summary>
    /// Get approximate bounding box for search optimization
    /// </summary>
    public (Coordinates SouthWest, Coordinates NorthEast) GetBoundingBox(double radiusKm)
    {
        // Rough approximation: 1 degree latitude ≈ 111 km
        var latOffset = radiusKm / 111.0;

        // Longitude offset varies by latitude
        var lonOffset = radiusKm / (111.0 * Math.Cos(_latitudeRadians));

        var southWest = Create(
            Math.Max(-90, Latitude - latOffset),
            Math.Max(-180, Longitude - lonOffset)
        );

        var northEast = Create(
            Math.Min(90, Latitude + latOffset),
            Math.Min(180, Longitude + lonOffset)
        );

        return (southWest, northEast);
    }

    /// <summary>
    /// Generate Google Maps URL
    /// </summary>
    public string ToGoogleMapsUrl()
    {
        return $"https://www.google.com/maps?q={Latitude},{Longitude}";
    }

    /// <summary>
    /// Get coordinates in Degrees, Minutes, Seconds format
    /// </summary>
    public string ToDmsFormat()
    {
        var latDms = ToDegreesMinutesSeconds(Latitude, true);
        var lonDms = ToDegreesMinutesSeconds(Longitude, false);
        return $"{latDms}, {lonDms}";
    }

    /// <summary>
    /// Calculate bearing (direction) to another coordinate
    /// </summary>
    public double BearingTo(Coordinates other)
    {
        var deltaLon = other._longitudeRadians - _longitudeRadians;

        var y = Math.Sin(deltaLon) * Math.Cos(other._latitudeRadians);
        var x = Math.Cos(_latitudeRadians) * Math.Sin(other._latitudeRadians) -
                Math.Sin(_latitudeRadians) * Math.Cos(other._latitudeRadians) * Math.Cos(deltaLon);

        var bearing = Math.Atan2(y, x);

        // Convert to degrees and normalize to 0-360
        return (ToDegrees(bearing) + 360) % 360;
    }

    /// <summary>
    /// Get compass direction to another coordinate
    /// </summary>
    public string GetCompassDirection(Coordinates other)
    {
        var bearing = BearingTo(other);

        var directions = new[]
        {
            "N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE",
            "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW"
        };

        var index = (int)Math.Round(bearing / 22.5) % 16;
        return directions[index];
    }

    /// <summary>
    /// Calculate midpoint between two coordinates
    /// </summary>
    public Coordinates MidpointTo(Coordinates other)
    {
        var deltaLon = other._longitudeRadians - _longitudeRadians;

        var bx = Math.Cos(other._latitudeRadians) * Math.Cos(deltaLon);
        var by = Math.Cos(other._latitudeRadians) * Math.Sin(deltaLon);

        var latMid = Math.Atan2(
            Math.Sin(_latitudeRadians) + Math.Sin(other._latitudeRadians),
            Math.Sqrt((Math.Cos(_latitudeRadians) + bx) * (Math.Cos(_latitudeRadians) + bx) + by * by)
        );

        var lonMid = _longitudeRadians + Math.Atan2(by, Math.Cos(_latitudeRadians) + bx);

        return Create(ToDegrees(latMid), ToDegrees(lonMid));
    }

    /// <summary>
    /// Check if coordinate is within a polygon defined by vertices
    /// </summary>
    public bool IsWithinPolygon(IEnumerable<Coordinates> polygonVertices)
    {
        var vertices = polygonVertices.ToArray();
        if (vertices.Length < 3)
        {
            throw new DomainValidationException("Polygon must have at least 3 vertices");
        }

        var inside = false;
        var j = vertices.Length - 1;

        for (var i = 0; i < vertices.Length; i++)
        {
            var xi = vertices[i].Latitude;
            var yi = vertices[i].Longitude;
            var xj = vertices[j].Latitude;
            var yj = vertices[j].Longitude;

            if (((yi > Longitude) != (yj > Longitude)) &&
                (Latitude < (xj - xi) * (Longitude - yi) / (yj - yi) + xi))
            {
                inside = !inside;
            }
            j = i;
        }

        return inside;
    }

    /// <summary>
    /// Get nearest coordinate from a collection
    /// </summary>
    public Coordinates GetNearestFrom(IEnumerable<Coordinates> coordinates)
    {
        return coordinates
            .OrderBy(c => DistanceToInKilometers(c))
            .FirstOrDefault() ?? throw new DomainValidationException("No coordinates provided");
    }

    /// <summary>
    /// Validate if coordinates represent a real location (basic validation)
    /// </summary>
    public bool IsValidLocation()
    {
        // Basic validation - not in the middle of oceans, poles, etc.
        // This is a simplified check - real implementation would use geographic databases

        // Exclude extreme polar regions
        if (Math.Abs(Latitude) > 85) return false;

        // Exclude some known invalid coordinate combinations
        if (Latitude == 0 && Longitude == 0) return false; // Null Island

        return true;
    }

    /// <summary>
    /// Get timezone offset estimate based on longitude (rough approximation)
    /// </summary>
    public TimeSpan GetApproximateTimezoneOffset()
    {
        // Very rough approximation: 15 degrees longitude ≈ 1 hour
        var hours = Math.Round(Longitude / 15.0);
        return TimeSpan.FromHours(Math.Max(-12, Math.Min(14, hours)));
    }

    private static string ToDegreesMinutesSeconds(double coordinate, bool isLatitude)
    {
        var direction = coordinate >= 0
            ? (isLatitude ? "N" : "E")
            : (isLatitude ? "S" : "W");

        var absolute = Math.Abs(coordinate);
        var degrees = (int)absolute;
        var minutesDecimal = (absolute - degrees) * 60;
        var minutes = (int)minutesDecimal;
        var seconds = (minutesDecimal - minutes) * 60;

        return $"{degrees}°{minutes}'{seconds:F2}\"{direction}";
    }

    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }

    private static double ToDegrees(double radians)
    {
        return radians * 180.0 / Math.PI;
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        // Round to 6 decimal places for equality (approximately 0.1 meter precision)
        yield return Math.Round(Latitude, 6);
        yield return Math.Round(Longitude, 6);
    }

    public override string ToString()
    {
        return $"{Latitude:F6},{Longitude:F6}";
    }

    /// <summary>
    /// Format coordinates for display with specified precision
    /// </summary>
    public string ToString(int decimalPlaces)
    {
        return $"{Latitude.ToString($"F{decimalPlaces}")},{Longitude.ToString($"F{decimalPlaces}")}";
    }

    /// <summary>
    /// Get coordinates formatted for different systems
    /// </summary>
    public string ToFormat(CoordinateFormat format)
    {
        return format switch
        {
            CoordinateFormat.DecimalDegrees => ToString(),
            CoordinateFormat.DegreesMinutesSeconds => ToDmsFormat(),
            CoordinateFormat.GoogleMaps => ToGoogleMapsUrl(),
            CoordinateFormat.GeoJson => $"[{Longitude:F6}, {Latitude:F6}]", // Note: GeoJSON uses [lon, lat]
            _ => ToString()
        };
    }
}

/// <summary>
/// Coordinate format options
/// </summary>
public enum CoordinateFormat
{
    DecimalDegrees,
    DegreesMinutesSeconds,
    GoogleMaps,
    GeoJson
}