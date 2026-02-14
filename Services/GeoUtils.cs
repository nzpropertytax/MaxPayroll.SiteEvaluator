namespace MaxPayroll.SiteEvaluator.Services;

/// <summary>
/// Geographic utility methods for distance calculations.
/// </summary>
public static class GeoUtils
{
    /// <summary>
    /// Earth's radius in meters.
    /// </summary>
    private const double EarthRadiusMeters = 6371000;

    /// <summary>
    /// Calculate the distance between two points using the Haversine formula.
    /// </summary>
    /// <param name="lat1">Latitude of point 1 (degrees)</param>
    /// <param name="lon1">Longitude of point 1 (degrees)</param>
    /// <param name="lat2">Latitude of point 2 (degrees)</param>
    /// <param name="lon2">Longitude of point 2 (degrees)</param>
    /// <returns>Distance in meters</returns>
    public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusMeters * c;
    }

    /// <summary>
    /// Calculate the distance between two coordinates.
    /// </summary>
    public static double CalculateDistance(Models.Coordinate point1, Models.Coordinate point2)
    {
        return CalculateDistance(point1.Latitude, point1.Longitude, point2.Latitude, point2.Longitude);
    }

    /// <summary>
    /// Check if a point is within a radius of another point.
    /// </summary>
    /// <param name="centerLat">Center latitude</param>
    /// <param name="centerLon">Center longitude</param>
    /// <param name="pointLat">Point latitude to check</param>
    /// <param name="pointLon">Point longitude to check</param>
    /// <param name="radiusMeters">Radius in meters</param>
    /// <returns>True if point is within radius</returns>
    public static bool IsWithinRadius(double centerLat, double centerLon, double pointLat, double pointLon, double radiusMeters)
    {
        return CalculateDistance(centerLat, centerLon, pointLat, pointLon) <= radiusMeters;
    }

    /// <summary>
    /// Calculate the bounding box for a point and radius.
    /// Useful for quick filtering before precise distance calculations.
    /// </summary>
    /// <param name="lat">Center latitude</param>
    /// <param name="lon">Center longitude</param>
    /// <param name="radiusMeters">Radius in meters</param>
    /// <returns>Tuple of (minLat, maxLat, minLon, maxLon)</returns>
    public static (double MinLat, double MaxLat, double MinLon, double MaxLon) CalculateBoundingBox(
        double lat, double lon, double radiusMeters)
    {
        // Rough approximation: 1 degree of latitude = 111km
        var latDelta = radiusMeters / 111000.0;
        
        // Longitude delta varies with latitude
        var lonDelta = radiusMeters / (111000.0 * Math.Cos(DegreesToRadians(lat)));

        return (
            MinLat: lat - latDelta,
            MaxLat: lat + latDelta,
            MinLon: lon - lonDelta,
            MaxLon: lon + lonDelta
        );
    }

    /// <summary>
    /// Calculate the centroid of a polygon.
    /// </summary>
    /// <param name="coordinates">List of coordinates forming the polygon</param>
    /// <returns>The centroid coordinate</returns>
    public static Models.Coordinate CalculateCentroid(IList<Models.Coordinate> coordinates)
    {
        if (coordinates == null || coordinates.Count == 0)
            return new Models.Coordinate();

        var lat = coordinates.Average(c => c.Latitude);
        var lon = coordinates.Average(c => c.Longitude);

        return new Models.Coordinate { Latitude = lat, Longitude = lon };
    }

    /// <summary>
    /// Calculate the area of a polygon in square meters using the Shoelace formula.
    /// Note: This is an approximation that works well for small areas.
    /// </summary>
    /// <param name="coordinates">List of coordinates forming a closed polygon</param>
    /// <returns>Area in square meters</returns>
    public static double CalculatePolygonArea(IList<Models.Coordinate> coordinates)
    {
        if (coordinates == null || coordinates.Count < 3)
            return 0;

        // Convert to a local coordinate system (meters) centered on the centroid
        var centroid = CalculateCentroid(coordinates);
        var points = coordinates.Select(c => ToLocalMeters(c, centroid)).ToList();

        // Shoelace formula
        double area = 0;
        int j = points.Count - 1;

        for (int i = 0; i < points.Count; i++)
        {
            area += (points[j].X + points[i].X) * (points[j].Y - points[i].Y);
            j = i;
        }

        return Math.Abs(area / 2);
    }

    /// <summary>
    /// Format coordinates as a string.
    /// </summary>
    public static string FormatCoordinates(double lat, double lon, int decimals = 6)
    {
        return $"{lat.ToString($"F{decimals}")}, {lon.ToString($"F{decimals}")}";
    }

    /// <summary>
    /// Parse a coordinate string in format "lat, lon" or "lat,lon".
    /// </summary>
    /// <returns>Coordinate or null if parsing fails</returns>
    public static Models.Coordinate? ParseCoordinates(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var parts = input.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
            return null;

        if (double.TryParse(parts[0], out var lat) && double.TryParse(parts[1], out var lon))
        {
            return new Models.Coordinate { Latitude = lat, Longitude = lon };
        }

        return null;
    }

    /// <summary>
    /// Check if coordinates are valid (within valid lat/lon ranges).
    /// </summary>
    public static bool AreValidCoordinates(double lat, double lon)
    {
        return lat >= -90 && lat <= 90 && lon >= -180 && lon <= 180;
    }

    /// <summary>
    /// Check if coordinates are in New Zealand (approximate bounding box).
    /// </summary>
    public static bool IsInNewZealand(double lat, double lon)
    {
        // Approximate bounding box for New Zealand
        return lat >= -47.5 && lat <= -34.0 && lon >= 166.0 && lon <= 179.0;
    }

    // === Private helpers ===

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }

    private static (double X, double Y) ToLocalMeters(Models.Coordinate coord, Models.Coordinate origin)
    {
        // Convert to meters relative to origin
        var latDiff = coord.Latitude - origin.Latitude;
        var lonDiff = coord.Longitude - origin.Longitude;

        var y = latDiff * 111000.0;
        var x = lonDiff * 111000.0 * Math.Cos(DegreesToRadians(origin.Latitude));

        return (x, y);
    }
}
