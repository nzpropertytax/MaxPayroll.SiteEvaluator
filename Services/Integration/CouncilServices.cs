using MaxPayroll.SiteEvaluator.Models;

namespace MaxPayroll.SiteEvaluator.Services.Integration;

/// <summary>
/// Integration with Christchurch City Council GIS.
/// https://cccatlas.ccc.govt.nz/
/// </summary>
public class ChristchurchCouncilService : ICouncilDataService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ChristchurchCouncilService> _logger;

    public ChristchurchCouncilService(HttpClient httpClient, ILogger<ChristchurchCouncilService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.BaseAddress = new Uri("https://opendata.ccc.govt.nz");
    }

    public string CouncilName => "Christchurch City Council";

    public bool SupportsRegion(double lat, double lon)
    {
        // Approximate Christchurch bounding box
        return lat >= -43.65 && lat <= -43.40 &&
               lon >= 172.45 && lon <= 172.80;
    }

    public async Task<ZoningData?> GetZoningDataAsync(double lat, double lon, CancellationToken ct = default)
    {
        try
        {
            var url = $"/arcgis/rest/services/DistrictPlan/MapServer/0/query?" +
                      $"geometry={lon},{lat}&geometryType=esriGeometryPoint&" +
                      "inSR=4326&spatialRel=esriSpatialRelIntersects&" +
                      "outFields=*&returnGeometry=false&f=json";
            
            var response = await _httpClient.GetAsync(url, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("CCC zone query failed: {StatusCode}", response.StatusCode);
                return null;
            }
            
            var result = await response.Content.ReadFromJsonAsync<ArcGisQueryResult>(ct);
            var feature = result?.Features?.FirstOrDefault();
            
            if (feature?.Attributes == null)
                return null;
            
            return new ZoningData
            {
                Zone = GetAttribute(feature, "ZONE_NAME") ?? "Unknown",
                ZoneCode = GetAttribute(feature, "ZONE_CODE") ?? "",
                ZoneDescription = GetAttribute(feature, "ZONE_DESCRIPTION") ?? "",
                DistrictPlan = "Christchurch District Plan",
                MaxHeight = ParseDouble(feature.Attributes.GetValueOrDefault("MAX_HEIGHT")),
                MaxCoverage = ParseDouble(feature.Attributes.GetValueOrDefault("SITE_COVERAGE")),
                DistrictPlanLink = "https://districtplan.ccc.govt.nz/",
                Source = new DataSource
                {
                    SourceName = "Christchurch City Council GIS",
                    SourceUrl = "https://opendata.ccc.govt.nz/",
                    DataDate = DateTime.UtcNow,
                    RetrievedDate = DateTime.UtcNow
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying CCC zoning");
            return null;
        }
    }

    public async Task<HazardData?> GetHazardDataAsync(double lat, double lon, CancellationToken ct = default)
    {
        var hazardData = new HazardData();
        
        try
        {
            // Flood hazard
            var floodTask = QueryHazardLayerAsync(1, lat, lon, ct);
            // Liquefaction
            var liqTask = QueryHazardLayerAsync(2, lat, lon, ct);
            
            await Task.WhenAll(floodTask, liqTask);
            
            var floodResult = await floodTask;
            var liqResult = await liqTask;
            
            if (floodResult != null)
            {
                hazardData.Flooding = new FloodHazard
                {
                    Zone = GetAttribute(floodResult, "FLOOD_ZONE") ?? "Unknown",
                    Description = GetAttribute(floodResult, "DESCRIPTION") ?? ""
                };
            }
            
            if (liqResult != null)
            {
                var tcCategory = GetAttribute(liqResult, "TC_CATEGORY") ?? "Unknown";
                hazardData.Liquefaction = new LiquefactionHazard
                {
                    Category = tcCategory,
                    Description = GetLiquefactionDescription(tcCategory),
                    RequiresGeotechAssessment = tcCategory == "TC3"
                };
            }
            
            hazardData.Source = new DataSource
            {
                SourceName = "Christchurch City Council GIS",
                SourceUrl = "https://opendata.ccc.govt.nz/",
                DataDate = DateTime.UtcNow,
                RetrievedDate = DateTime.UtcNow
            };
            
            return hazardData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying CCC hazards");
            return null;
        }
    }

    public async Task<InfrastructureData?> GetInfrastructureDataAsync(double lat, double lon, CancellationToken ct = default)
    {
        try
        {
            // Query 3 Waters layers
            var waterTask = QueryInfrastructureLayerAsync("WaterSupply", lat, lon, ct);
            var wastewaterTask = QueryInfrastructureLayerAsync("Wastewater", lat, lon, ct);
            var stormwaterTask = QueryInfrastructureLayerAsync("Stormwater", lat, lon, ct);
            
            await Task.WhenAll(waterTask, wastewaterTask, stormwaterTask);
            
            return new InfrastructureData
            {
                Water = new WaterSupply { Available = await waterTask != null },
                Wastewater = new Wastewater { Available = await wastewaterTask != null },
                Stormwater = new Stormwater { Available = await stormwaterTask != null },
                Source = new DataSource
                {
                    SourceName = "Christchurch City Council GIS",
                    SourceUrl = "https://opendata.ccc.govt.nz/",
                    DataDate = DateTime.UtcNow,
                    RetrievedDate = DateTime.UtcNow
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying CCC infrastructure");
            return null;
        }
    }

    private async Task<ArcGisFeature?> QueryHazardLayerAsync(int layerId, double lat, double lon, CancellationToken ct)
    {
        var url = $"/arcgis/rest/services/Hazards/MapServer/{layerId}/query?" +
                  $"geometry={lon},{lat}&geometryType=esriGeometryPoint&" +
                  "inSR=4326&spatialRel=esriSpatialRelIntersects&" +
                  "outFields=*&returnGeometry=false&f=json";
        
        var response = await _httpClient.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode) return null;
        
        var result = await response.Content.ReadFromJsonAsync<ArcGisQueryResult>(ct);
        return result?.Features?.FirstOrDefault();
    }

    private async Task<ArcGisFeature?> QueryInfrastructureLayerAsync(string layerName, double lat, double lon, CancellationToken ct)
    {
        // This is a simplified implementation
        // Real implementation would query specific infrastructure layers
        await Task.Delay(1, ct); // Placeholder
        return null;
    }

    private static string? GetAttribute(ArcGisFeature feature, string name)
    {
        return feature.Attributes?.GetValueOrDefault(name)?.ToString();
    }

    private static double? ParseDouble(object? value)
    {
        if (value == null) return null;
        return double.TryParse(value.ToString(), out var result) ? result : null;
    }

    private static string GetLiquefactionDescription(string tcCategory)
    {
        return tcCategory switch
        {
            "TC1" => "Low liquefaction vulnerability. Standard foundations typically suitable.",
            "TC2" => "Moderate liquefaction vulnerability. Enhanced foundations may be required.",
            "TC3" => "High liquefaction vulnerability. Specific geotechnical investigation required.",
            _ => "Unknown liquefaction category"
        };
    }
}

/// <summary>
/// Auckland Council GIS integration.
/// </summary>
public class AucklandCouncilService : ICouncilDataService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AucklandCouncilService> _logger;

    public AucklandCouncilService(HttpClient httpClient, ILogger<AucklandCouncilService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public string CouncilName => "Auckland Council";

    public bool SupportsRegion(double lat, double lon)
    {
        // Approximate Auckland bounding box
        return lat >= -37.4 && lat <= -36.4 &&
               lon >= 174.4 && lon <= 175.4;
    }

    public Task<ZoningData?> GetZoningDataAsync(double lat, double lon, CancellationToken ct = default)
    {
        // TODO: Implement Auckland Council GIS integration
        _logger.LogInformation("Auckland zoning query for {Lat}, {Lon}", lat, lon);
        return Task.FromResult<ZoningData?>(null);
    }

    public Task<HazardData?> GetHazardDataAsync(double lat, double lon, CancellationToken ct = default)
    {
        _logger.LogInformation("Auckland hazard query for {Lat}, {Lon}", lat, lon);
        return Task.FromResult<HazardData?>(null);
    }

    public Task<InfrastructureData?> GetInfrastructureDataAsync(double lat, double lon, CancellationToken ct = default)
    {
        _logger.LogInformation("Auckland infrastructure query for {Lat}, {Lon}", lat, lon);
        return Task.FromResult<InfrastructureData?>(null);
    }
}

/// <summary>
/// Wellington City Council GIS integration.
/// </summary>
public class WellingtonCouncilService : ICouncilDataService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WellingtonCouncilService> _logger;

    public WellingtonCouncilService(HttpClient httpClient, ILogger<WellingtonCouncilService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public string CouncilName => "Wellington City Council";

    public bool SupportsRegion(double lat, double lon)
    {
        // Approximate Wellington bounding box
        return lat >= -41.4 && lat <= -41.2 &&
               lon >= 174.7 && lon <= 174.9;
    }

    public Task<ZoningData?> GetZoningDataAsync(double lat, double lon, CancellationToken ct = default)
    {
        _logger.LogInformation("Wellington zoning query for {Lat}, {Lon}", lat, lon);
        return Task.FromResult<ZoningData?>(null);
    }

    public Task<HazardData?> GetHazardDataAsync(double lat, double lon, CancellationToken ct = default)
    {
        _logger.LogInformation("Wellington hazard query for {Lat}, {Lon}", lat, lon);
        return Task.FromResult<HazardData?>(null);
    }

    public Task<InfrastructureData?> GetInfrastructureDataAsync(double lat, double lon, CancellationToken ct = default)
    {
        _logger.LogInformation("Wellington infrastructure query for {Lat}, {Lon}", lat, lon);
        return Task.FromResult<InfrastructureData?>(null);
    }
}

// ArcGIS response models
public class ArcGisQueryResult
{
    public List<ArcGisFeature>? Features { get; set; }
}

public class ArcGisFeature
{
    public Dictionary<string, object>? Attributes { get; set; }
}
