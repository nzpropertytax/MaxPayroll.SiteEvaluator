using MaxPayroll.SiteEvaluator.Models;

namespace MaxPayroll.SiteEvaluator.Services.Integration;

/// <summary>
/// Integration with NZ Geotechnical Database.
/// https://www.nzgd.org.nz/
/// </summary>
public class NzgdDataService : INzgdDataService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NzgdDataService> _logger;

    public NzgdDataService(HttpClient httpClient, ILogger<NzgdDataService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<NearbyBorehole>> GetNearbyBoreholesAsync(double lat, double lon, double radiusMeters, CancellationToken ct = default)
    {
        try
        {
            // NZGD spatial search API
            var url = $"/api/v1/boreholes/search?lat={lat}&lon={lon}&radius={radiusMeters}";
            var response = await _httpClient.GetAsync(url, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("NZGD borehole search failed: {StatusCode}", response.StatusCode);
                return [];
            }
            
            var nzgdBoreholes = await response.Content.ReadFromJsonAsync<List<NzgdBorehole>>(ct);
            
            return nzgdBoreholes?.Select(b => new NearbyBorehole
            {
                Id = b.Id ?? Guid.NewGuid().ToString(),
                NzgdId = b.NzgdId,
                DistanceMeters = GeoUtils.CalculateDistance(lat, lon, b.Latitude ?? 0, b.Longitude ?? 0),
                Latitude = b.Latitude ?? 0,
                Longitude = b.Longitude ?? 0,
                Depth = b.Depth,
                Date = b.Date,
                Description = b.Description,
                SourceUrl = $"https://www.nzgd.org.nz/borehole/{b.NzgdId}",
                SoilLayers = b.Layers?.Select(l => new SoilLayer
                {
                    TopDepth = l.TopDepth ?? 0,
                    BottomDepth = l.BottomDepth ?? 0,
                    Description = l.Description ?? string.Empty,
                    SoilType = l.SoilType
                }).ToList() ?? []
            }).ToList() ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching NZGD boreholes");
            return [];
        }
    }

    public async Task<List<NearbyCpt>> GetNearbyCptsAsync(double lat, double lon, double radiusMeters, CancellationToken ct = default)
    {
        try
        {
            var url = $"/api/v1/cpts/search?lat={lat}&lon={lon}&radius={radiusMeters}";
            var response = await _httpClient.GetAsync(url, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("NZGD CPT search failed: {StatusCode}", response.StatusCode);
                return [];
            }
            
            var nzgdCpts = await response.Content.ReadFromJsonAsync<List<NzgdCpt>>(ct);
            
            return nzgdCpts?.Select(c => new NearbyCpt
            {
                Id = c.Id ?? Guid.NewGuid().ToString(),
                DistanceMeters = GeoUtils.CalculateDistance(lat, lon, c.Latitude ?? 0, c.Longitude ?? 0),
                Latitude = c.Latitude ?? 0,
                Longitude = c.Longitude ?? 0,
                Depth = c.Depth,
                Date = c.Date,
                SourceUrl = $"https://www.nzgd.org.nz/cpt/{c.Id}"
            }).ToList() ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching NZGD CPTs");
            return [];
        }
    }

    public async Task<List<NearbyGeotechReport>> GetNearbyReportsAsync(double lat, double lon, double radiusMeters, CancellationToken ct = default)
    {
        try
        {
            var url = $"/api/v1/reports/search?lat={lat}&lon={lon}&radius={radiusMeters}";
            var response = await _httpClient.GetAsync(url, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("NZGD report search failed: {StatusCode}", response.StatusCode);
                return [];
            }
            
            var nzgdReports = await response.Content.ReadFromJsonAsync<List<NzgdReport>>(ct);
            
            return nzgdReports?.Select(r => new NearbyGeotechReport
            {
                Id = r.Id ?? Guid.NewGuid().ToString(),
                Title = r.Title ?? "Untitled Report",
                DistanceMeters = GeoUtils.CalculateDistance(lat, lon, r.Latitude ?? 0, r.Longitude ?? 0),
                Author = r.Author,
                Date = r.Date,
                SourceUrl = r.Url
            }).ToList() ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching NZGD reports");
            return [];
        }
    }
}

// NZGD API response models
public class NzgdBorehole
{
    public string? Id { get; set; }
    public string? NzgdId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? Depth { get; set; }
    public DateTime? Date { get; set; }
    public string? Description { get; set; }
    public List<NzgdSoilLayer>? Layers { get; set; }
}

public class NzgdSoilLayer
{
    public double? TopDepth { get; set; }
    public double? BottomDepth { get; set; }
    public string? Description { get; set; }
    public string? SoilType { get; set; }
}

public class NzgdCpt
{
    public string? Id { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? Depth { get; set; }
    public DateTime? Date { get; set; }
}

public class NzgdReport
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Author { get; set; }
    public DateTime? Date { get; set; }
    public string? Url { get; set; }
}

/// <summary>
/// Geographic utility methods.
/// </summary>
public static class GeoUtils
{
    /// <summary>
    /// Calculate distance between two points using Haversine formula.
    /// </summary>
    public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000; // Earth's radius in meters
        
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        
        return R * c;
    }

    private static double ToRad(double deg) => deg * Math.PI / 180;
}
