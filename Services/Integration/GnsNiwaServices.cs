using MaxPayroll.SiteEvaluator.Models;

namespace MaxPayroll.SiteEvaluator.Services.Integration;

/// <summary>
/// Integration with GNS Science seismic data.
/// </summary>
public class GnsDataService : IGnsDataService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GnsDataService> _logger;

    public GnsDataService(HttpClient httpClient, ILogger<GnsDataService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<SeismicHazard?> GetSeismicHazardAsync(double lat, double lon, CancellationToken ct = default)
    {
        try
        {
            // Query GNS seismic hazard API
            var url = $"/api/v1/seismic/hazard?lat={lat}&lon={lon}";
            var response = await _httpClient.GetAsync(url, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("GNS seismic query failed: {StatusCode}", response.StatusCode);
                return null;
            }
            
            var result = await response.Content.ReadFromJsonAsync<GnsSeismicResponse>(ct);
            
            if (result == null)
                return null;

            return new SeismicHazard
            {
                Zone = result.Zone ?? "Unknown",
                PGA = result.Pga,
                NearbyFaults = result.Faults?.Select(f => new ActiveFault
                {
                    Name = f.Name ?? "Unknown",
                    DistanceKm = f.DistanceKm ?? 0,
                    RecurrenceInterval = f.RecurrenceInterval,
                    LastRupture = f.LastRupture
                }).ToList() ?? []
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying GNS seismic data");
            return null;
        }
    }

    public async Task<List<ActiveFault>> GetNearbyFaultsAsync(double lat, double lon, double radiusKm, CancellationToken ct = default)
    {
        try
        {
            var url = $"/api/v1/faults/nearby?lat={lat}&lon={lon}&radius={radiusKm}";
            var response = await _httpClient.GetAsync(url, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("GNS fault query failed: {StatusCode}", response.StatusCode);
                return [];
            }
            
            var faults = await response.Content.ReadFromJsonAsync<List<GnsFault>>(ct);
            
            return faults?.Select(f => new ActiveFault
            {
                Name = f.Name ?? "Unknown",
                DistanceKm = f.DistanceKm ?? 0,
                RecurrenceInterval = f.RecurrenceInterval,
                LastRupture = f.LastRupture
            }).ToList() ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying GNS faults");
            return [];
        }
    }
}

// GNS API response models
public class GnsSeismicResponse
{
    public string? Zone { get; set; }
    public double? Pga { get; set; }
    public List<GnsFault>? Faults { get; set; }
}

public class GnsFault
{
    public string? Name { get; set; }
    public double? DistanceKm { get; set; }
    public string? RecurrenceInterval { get; set; }
    public string? LastRupture { get; set; }
}

/// <summary>
/// Integration with NIWA climate data.
/// </summary>
public class NiwaDataService : INiwaDataService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NiwaDataService> _logger;

    public NiwaDataService(HttpClient httpClient, ILogger<NiwaDataService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<RainfallData?> GetRainfallDataAsync(double lat, double lon, CancellationToken ct = default)
    {
        try
        {
            // NIWA CliFlo API for rainfall data
            var url = $"/api/v1/rainfall?lat={lat}&lon={lon}";
            var response = await _httpClient.GetAsync(url, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("NIWA rainfall query failed: {StatusCode}", response.StatusCode);
                return null;
            }
            
            var result = await response.Content.ReadFromJsonAsync<NiwaRainfallResponse>(ct);
            
            if (result == null)
                return null;

            return new RainfallData
            {
                AnnualMean = result.AnnualMean,
                I10_10 = result.I10_10,
                I10_60 = result.I10_60,
                I100_10 = result.I100_10,
                I100_60 = result.I100_60,
                HirtdsStation = result.Station
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying NIWA rainfall");
            return null;
        }
    }

    public async Task<string?> GetWindZoneAsync(double lat, double lon, CancellationToken ct = default)
    {
        try
        {
            // Wind zone lookup (based on NZS 3604)
            var url = $"/api/v1/wind?lat={lat}&lon={lon}";
            var response = await _httpClient.GetAsync(url, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                // Fall back to static lookup based on location
                return GetStaticWindZone(lat, lon);
            }
            
            var result = await response.Content.ReadFromJsonAsync<NiwaWindResponse>(ct);
            return result?.WindZone;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying NIWA wind zone");
            return GetStaticWindZone(lat, lon);
        }
    }

    private static string GetStaticWindZone(double lat, double lon)
    {
        // Simplified static wind zone lookup based on NZS 3604
        // This is a rough approximation - real implementation would use proper maps
        
        // Wellington region - generally Very High or High
        if (lat <= -41.0 && lat >= -41.5 && lon >= 174.5 && lon <= 175.5)
            return "Very High";
        
        // Canterbury plains - generally Medium
        if (lat <= -43.0 && lat >= -44.0 && lon >= 171.5 && lon <= 173.0)
            return "Medium";
        
        // Auckland - generally Medium to High
        if (lat <= -36.5 && lat >= -37.5)
            return "Medium";
        
        // Default
        return "Medium";
    }
}

// NIWA API response models
public class NiwaRainfallResponse
{
    public double? AnnualMean { get; set; }
    public double? I10_10 { get; set; }
    public double? I10_60 { get; set; }
    public double? I100_10 { get; set; }
    public double? I100_60 { get; set; }
    public string? Station { get; set; }
}

public class NiwaWindResponse
{
    public string? WindZone { get; set; }
    public double? BasicWindSpeed { get; set; }
}
