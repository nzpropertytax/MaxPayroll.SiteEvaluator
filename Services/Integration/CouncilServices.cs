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
/// https://geomapspublic.aucklandcouncil.govt.nz/
/// https://services2.arcgis.com/JnqnZOOYLNJIPLTI/arcgis/rest/services
/// </summary>
public class AucklandCouncilService : ICouncilDataService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AucklandCouncilService> _logger;
    
    private const string BaseUrl = "https://services2.arcgis.com/JnqnZOOYLNJIPLTI/arcgis/rest/services";

    public AucklandCouncilService(HttpClient httpClient, ILogger<AucklandCouncilService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public string CouncilName => "Auckland Council";

    public bool SupportsRegion(double lat, double lon)
    {
        // Approximate Auckland region bounding box
        return lat >= -37.5 && lat <= -36.2 &&
               lon >= 174.3 && lon <= 175.5;
    }

    public async Task<ZoningData?> GetZoningDataAsync(double lat, double lon, CancellationToken ct = default)
    {
        try
        {
            // Auckland Unitary Plan Operative zones
            var url = $"{BaseUrl}/Auckland_Unitary_Plan_Operative_in_part_Zones/FeatureServer/0/query?" +
                      $"geometry={lon},{lat}&geometryType=esriGeometryPoint&" +
                      "inSR=4326&spatialRel=esriSpatialRelIntersects&" +
                      "outFields=*&returnGeometry=false&f=json";
            
            _logger.LogDebug("Querying Auckland zoning: {Url}", url);
            
            var response = await _httpClient.GetAsync(url, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Auckland zone query failed: {StatusCode}", response.StatusCode);
                return null;
            }
            
            var result = await response.Content.ReadFromJsonAsync<ArcGisQueryResult>(ct);
            var feature = result?.Features?.FirstOrDefault();
            
            if (feature?.Attributes == null)
            {
                _logger.LogInformation("No zoning data found for Auckland location {Lat}, {Lon}", lat, lon);
                return null;
            }
            
            var zoneName = GetAttribute(feature, "ZONE") ?? GetAttribute(feature, "Zone_Name") ?? "Unknown";
            var zoneCode = GetAttribute(feature, "ZONE_CODE") ?? GetAttribute(feature, "Zone_Code") ?? "";
            
            return new ZoningData
            {
                Zone = zoneName,
                ZoneCode = zoneCode,
                ZoneDescription = GetZoneDescription(zoneName),
                DistrictPlan = "Auckland Unitary Plan (Operative in part)",
                MaxHeight = ParseMaxHeight(zoneName),
                DistrictPlanLink = "https://unitaryplan.aucklandcouncil.govt.nz/",
                Source = new DataSource
                {
                    SourceName = "Auckland Council GIS",
                    SourceUrl = "https://geomapspublic.aucklandcouncil.govt.nz/",
                    DataDate = DateTime.UtcNow,
                    RetrievedDate = DateTime.UtcNow
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying Auckland zoning for {Lat}, {Lon}", lat, lon);
            return null;
        }
    }

    public async Task<HazardData?> GetHazardDataAsync(double lat, double lon, CancellationToken ct = default)
    {
        var hazardData = new HazardData();
        
        try
        {
            // Query flood plains
            var floodTask = QueryFloodZoneAsync(lat, lon, ct);
            // Query coastal hazards
            var coastalTask = QueryCoastalHazardAsync(lat, lon, ct);
            
            await Task.WhenAll(floodTask, coastalTask);
            
            var floodResult = await floodTask;
            var coastalResult = await coastalTask;
            
            if (floodResult != null)
            {
                hazardData.Flooding = new FloodHazard
                {
                    Zone = GetAttribute(floodResult, "FLOOD_ZONE") ?? GetAttribute(floodResult, "Zone") ?? "In Flood Plain",
                    Description = GetAttribute(floodResult, "DESCRIPTION") ?? "Property is within a flood-prone area",
                    RequiresFloodAssessment = true
                };
            }
            
            if (coastalResult != null)
            {
                hazardData.CoastalErosion = true;
                hazardData.CoastalInundation = true;
            }
            
            hazardData.Source = new DataSource
            {
                SourceName = "Auckland Council GIS",
                SourceUrl = "https://geomapspublic.aucklandcouncil.govt.nz/",
                DataDate = DateTime.UtcNow,
                RetrievedDate = DateTime.UtcNow
            };
            
            return hazardData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying Auckland hazards for {Lat}, {Lon}", lat, lon);
            return null;
        }
    }

    public async Task<InfrastructureData?> GetInfrastructureDataAsync(double lat, double lon, CancellationToken ct = default)
    {
        try
        {
            // Auckland uses Watercare for water/wastewater
            // Most urban Auckland has full services available
            
            // For now, assume services available in urban Auckland
            var isUrban = IsUrbanAuckland(lat, lon);
            
            return await Task.FromResult(new InfrastructureData
            {
                Water = new WaterSupply 
                { 
                    Available = isUrban,
                    Provider = "Watercare",
                    Notes = isUrban ? "Watercare municipal supply" : "May require private supply"
                },
                Wastewater = new Wastewater 
                { 
                    Available = isUrban,
                    Provider = "Watercare",
                    Notes = isUrban ? "Watercare reticulated" : "May require on-site system"
                },
                Stormwater = new Stormwater 
                { 
                    Available = isUrban,
                    Notes = isUrban ? "Council stormwater system" : "On-site disposal required"
                },
                Source = new DataSource
                {
                    SourceName = "Auckland Council / Watercare",
                    SourceUrl = "https://www.watercare.co.nz/",
                    DataDate = DateTime.UtcNow,
                    RetrievedDate = DateTime.UtcNow,
                    Notes = "Infrastructure availability is estimated based on location. Verify with Watercare."
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying Auckland infrastructure for {Lat}, {Lon}", lat, lon);
            return null;
        }
    }

    private async Task<ArcGisFeature?> QueryFloodZoneAsync(double lat, double lon, CancellationToken ct)
    {
        try
        {
            var url = $"{BaseUrl}/Floodplains/FeatureServer/0/query?" +
                      $"geometry={lon},{lat}&geometryType=esriGeometryPoint&" +
                      "inSR=4326&spatialRel=esriSpatialRelIntersects&" +
                      "outFields=*&returnGeometry=false&f=json";
            
            var response = await _httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode) return null;
            
            var result = await response.Content.ReadFromJsonAsync<ArcGisQueryResult>(ct);
            return result?.Features?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error querying Auckland flood zones");
            return null;
        }
    }

    private async Task<ArcGisFeature?> QueryCoastalHazardAsync(double lat, double lon, CancellationToken ct)
    {
        try
        {
            var url = $"{BaseUrl}/Coastal_Erosion_Hazard_Areas/FeatureServer/0/query?" +
                      $"geometry={lon},{lat}&geometryType=esriGeometryPoint&" +
                      "inSR=4326&spatialRel=esriSpatialRelIntersects&" +
                      "outFields=*&returnGeometry=false&f=json";
            
            var response = await _httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode) return null;
            
            var result = await response.Content.ReadFromJsonAsync<ArcGisQueryResult>(ct);
            return result?.Features?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error querying Auckland coastal hazards");
            return null;
        }
    }

    private bool IsUrbanAuckland(double lat, double lon)
    {
        // Rough check for urban Auckland (isthmus and surrounds)
        // CBD and inner suburbs
        if (lat >= -36.95 && lat <= -36.82 && lon >= 174.70 && lon <= 174.85)
            return true;
        
        // North Shore
        if (lat >= -36.82 && lat <= -36.70 && lon >= 174.70 && lon <= 174.80)
            return true;
        
        // West Auckland (Henderson, New Lynn)
        if (lat >= -36.95 && lat <= -36.85 && lon >= 174.58 && lon <= 174.70)
            return true;
        
        // South Auckland (Manukau, Papakura)
        if (lat >= -37.10 && lat <= -36.95 && lon >= 174.85 && lon <= 175.00)
            return true;
        
        // East Auckland (Howick, Pakuranga)
        if (lat >= -36.95 && lat <= -36.85 && lon >= 174.90 && lon <= 175.05)
            return true;
        
        return false;
    }

    private static string? GetAttribute(ArcGisFeature feature, string name)
    {
        return feature.Attributes?.GetValueOrDefault(name)?.ToString();
    }

    private static double? ParseMaxHeight(string zoneName)
    {
        // Auckland Unitary Plan height limits by zone type
        return zoneName.ToLower() switch
        {
            var z when z.Contains("metropolitan centre") => 72.5,
            var z when z.Contains("city centre") => 100.0,
            var z when z.Contains("town centre") => 18.0,
            var z when z.Contains("mixed housing urban") => 11.0,
            var z when z.Contains("mixed housing suburban") => 8.0,
            var z when z.Contains("terrace housing") => 16.0,
            var z when z.Contains("single house") => 8.0,
            var z when z.Contains("large lot") => 8.0,
            var z when z.Contains("light industry") => 20.0,
            var z when z.Contains("heavy industry") => 20.0,
            var z when z.Contains("business") => 12.0,
            _ => null
        };
    }

    private static string GetZoneDescription(string zoneName)
    {
        return zoneName.ToLower() switch
        {
            var z when z.Contains("metropolitan centre") => "High-density mixed-use zone for major centres.",
            var z when z.Contains("city centre") => "Auckland CBD - highest density, major commercial.",
            var z when z.Contains("town centre") => "Mixed-use zone for local centres.",
            var z when z.Contains("mixed housing urban") => "Medium-density residential enabling multi-unit development.",
            var z when z.Contains("mixed housing suburban") => "Standard suburban residential with increased density.",
            var z when z.Contains("terrace housing") => "High-density residential near centres and transport.",
            var z when z.Contains("single house") => "Traditional low-density single-house residential.",
            var z when z.Contains("large lot") => "Low-density residential with large lot requirements.",
            var z when z.Contains("light industry") => "Light industrial activities.",
            var z when z.Contains("heavy industry") => "Heavy industrial activities.",
            var z when z.Contains("rural") => "Rural zone with limited development.",
            _ => $"{zoneName} - See Auckland Unitary Plan for details."
        };
    }
}

/// <summary>
/// Wellington City Council GIS integration.
/// https://wellington.govt.nz/your-council/maps
/// https://gis.wcc.govt.nz/arcgis/rest/services
/// </summary>
public class WellingtonCouncilService : ICouncilDataService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WellingtonCouncilService> _logger;
    
    private const string BaseUrl = "https://gis.wcc.govt.nz/arcgis/rest/services";

    public WellingtonCouncilService(HttpClient httpClient, ILogger<WellingtonCouncilService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public string CouncilName => "Wellington City Council";

    public bool SupportsRegion(double lat, double lon)
    {
        // Wellington City bounding box (excludes Hutt, Porirua)
        return lat >= -41.35 && lat <= -41.20 &&
               lon >= 174.70 && lon <= 174.85;
    }

    public async Task<ZoningData?> GetZoningDataAsync(double lat, double lon, CancellationToken ct = default)
    {
        try
        {
            // Wellington District Plan zones
            var url = $"{BaseUrl}/Property/District_Plan_Zones/MapServer/0/query?" +
                      $"geometry={lon},{lat}&geometryType=esriGeometryPoint&" +
                      "inSR=4326&spatialRel=esriSpatialRelIntersects&" +
                      "outFields=*&returnGeometry=false&f=json";
            
            _logger.LogDebug("Querying Wellington zoning: {Url}", url);
            
            var response = await _httpClient.GetAsync(url, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Wellington zone query failed: {StatusCode}", response.StatusCode);
                return GetFallbackZoningData(lat, lon);
            }
            
            var result = await response.Content.ReadFromJsonAsync<ArcGisQueryResult>(ct);
            var feature = result?.Features?.FirstOrDefault();
            
            if (feature?.Attributes == null)
            {
                _logger.LogInformation("No zoning data found for Wellington location {Lat}, {Lon}", lat, lon);
                return GetFallbackZoningData(lat, lon);
            }
            
            var zoneName = GetAttribute(feature, "ZONE") ?? GetAttribute(feature, "Zone_Name") ?? "Unknown";
            
            return new ZoningData
            {
                Zone = zoneName,
                ZoneCode = GetAttribute(feature, "ZONE_CODE") ?? "",
                ZoneDescription = GetWellingtonZoneDescription(zoneName),
                DistrictPlan = "Wellington City District Plan",
                MaxHeight = GetWellingtonMaxHeight(zoneName),
                DistrictPlanLink = "https://wellington.govt.nz/your-council/plans-policies-and-bylaws/district-plan",
                Source = new DataSource
                {
                    SourceName = "Wellington City Council GIS",
                    SourceUrl = "https://gis.wcc.govt.nz/",
                    DataDate = DateTime.UtcNow,
                    RetrievedDate = DateTime.UtcNow
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying Wellington zoning for {Lat}, {Lon}", lat, lon);
            return GetFallbackZoningData(lat, lon);
        }
    }

    public async Task<HazardData?> GetHazardDataAsync(double lat, double lon, CancellationToken ct = default)
    {
        var hazardData = new HazardData();
        
        try
        {
            // Wellington has significant seismic hazards - add default Wellington Fault proximity
            var distanceToWellingtonFault = CalculateDistanceToWellingtonFault(lat, lon);
            
            // Query fault hazard zones
            var faultTask = QueryFaultHazardAsync(lat, lon, ct);
            var tsunamiTask = QueryTsunamiZoneAsync(lat, lon, ct);
            
            await Task.WhenAll(faultTask, tsunamiTask);
            
            var faultResult = await faultTask;
            var tsunamiResult = await tsunamiTask;
            
            // Wellington-specific seismic hazard (always include)
            hazardData.Seismic = new SeismicHazard
            {
                Zone = "High Seismic Zone (Z = 0.40)",
                ZoneFactor = 0.40,
                SiteClass = "C", // Default - needs geotech
                PGA = 0.40,
                NearbyFaults = 
                [
                    new ActiveFault
                    {
                        Name = "Wellington Fault",
                        DistanceKm = distanceToWellingtonFault,
                        RecurrenceInterval = "~840 years",
                        LastRupture = "~1400 AD",
                        FaultType = "Dextral strike-slip",
                        SlipRate = "6-8 mm/year",
                        MaxMagnitude = 7.5
                    }
                ],
                DesignStandard = "NZS 1170.5:2004",
                Source = new DataSource
                {
                    SourceName = "GNS Science / Wellington City Council",
                    SourceUrl = "https://www.gns.cri.nz/",
                    DataDate = DateTime.UtcNow,
                    RetrievedDate = DateTime.UtcNow
                }
            };
            
            // Apply near-fault factor
            if (distanceToWellingtonFault < 20)
            {
                hazardData.Seismic.NearFaultFactor = distanceToWellingtonFault switch
                {
                    < 2 => 1.5,
                    < 5 => 1.3,
                    < 10 => 1.15,
                    _ => 1.0
                };
            }
            
            // Tsunami zone
            if (tsunamiResult != null)
            {
                hazardData.CoastalInundation = true;
                hazardData.AllHazards.Add(new HazardSummary
                {
                    HazardType = "Tsunami",
                    Severity = "High",
                    Description = "Property is within a tsunami evacuation zone",
                    Action = "Evacuation planning required"
                });
            }
            
            hazardData.Source = new DataSource
            {
                SourceName = "Wellington City Council GIS",
                SourceUrl = "https://gis.wcc.govt.nz/",
                DataDate = DateTime.UtcNow,
                RetrievedDate = DateTime.UtcNow
            };
            
            return hazardData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying Wellington hazards for {Lat}, {Lon}", lat, lon);
            // Return basic seismic data even on error
            return new HazardData
            {
                Seismic = new SeismicHazard
                {
                    Zone = "High Seismic Zone (Z = 0.40)",
                    ZoneFactor = 0.40,
                    DesignStandard = "NZS 1170.5:2004"
                }
            };
        }
    }

    public async Task<InfrastructureData?> GetInfrastructureDataAsync(double lat, double lon, CancellationToken ct = default)
    {
        try
        {
            // Wellington Water manages 3 waters
            // Most of Wellington City is reticulated
            
            return await Task.FromResult(new InfrastructureData
            {
                Water = new WaterSupply 
                { 
                    Available = true,
                    Provider = "Wellington Water",
                    Notes = "Municipal supply from Hutt catchment"
                },
                Wastewater = new Wastewater 
                { 
                    Available = true,
                    Provider = "Wellington Water",
                    Notes = "Reticulated wastewater system"
                },
                Stormwater = new Stormwater 
                { 
                    Available = true,
                    Notes = "Council stormwater system"
                },
                Source = new DataSource
                {
                    SourceName = "Wellington Water",
                    SourceUrl = "https://www.wellingtonwater.co.nz/",
                    DataDate = DateTime.UtcNow,
                    RetrievedDate = DateTime.UtcNow
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying Wellington infrastructure for {Lat}, {Lon}", lat, lon);
            return null;
        }
    }

    private async Task<ArcGisFeature?> QueryFaultHazardAsync(double lat, double lon, CancellationToken ct)
    {
        try
        {
            var url = $"{BaseUrl}/Hazards/Wellington_Fault_Zones/MapServer/0/query?" +
                      $"geometry={lon},{lat}&geometryType=esriGeometryPoint&" +
                      "inSR=4326&spatialRel=esriSpatialRelIntersects&" +
                      "outFields=*&returnGeometry=false&f=json";
            
            var response = await _httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode) return null;
            
            var result = await response.Content.ReadFromJsonAsync<ArcGisQueryResult>(ct);
            return result?.Features?.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    private async Task<ArcGisFeature?> QueryTsunamiZoneAsync(double lat, double lon, CancellationToken ct)
    {
        try
        {
            var url = $"{BaseUrl}/Hazards/Tsunami_Evacuation_Zones/MapServer/0/query?" +
                      $"geometry={lon},{lat}&geometryType=esriGeometryPoint&" +
                      "inSR=4326&spatialRel=esriSpatialRelIntersects&" +
                      "outFields=*&returnGeometry=false&f=json";
            
            var response = await _httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode) return null;
            
            var result = await response.Content.ReadFromJsonAsync<ArcGisQueryResult>(ct);
            return result?.Features?.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    private ZoningData GetFallbackZoningData(double lat, double lon)
    {
        // Estimate zone based on location
        var zone = EstimateWellingtonZone(lat, lon);
        
        return new ZoningData
        {
            Zone = zone,
            ZoneDescription = GetWellingtonZoneDescription(zone),
            DistrictPlan = "Wellington City District Plan",
            MaxHeight = GetWellingtonMaxHeight(zone),
            DistrictPlanLink = "https://wellington.govt.nz/your-council/plans-policies-and-bylaws/district-plan",
            Source = new DataSource
            {
                SourceName = "Wellington City Council (estimated)",
                SourceUrl = "https://gis.wcc.govt.nz/",
                DataDate = DateTime.UtcNow,
                RetrievedDate = DateTime.UtcNow,
                Notes = "Zone estimated based on location. Verify with Wellington City Council."
            }
        };
    }

    private string EstimateWellingtonZone(double lat, double lon)
    {
        // CBD area
        if (lat >= -41.295 && lat <= -41.275 && lon >= 174.765 && lon <= 174.785)
            return "Central Area";
        
        // Te Aro / inner suburbs
        if (lat >= -41.31 && lat <= -41.28 && lon >= 174.76 && lon <= 174.79)
            return "Inner Residential";
        
        // Oriental Bay / waterfront
        if (lon >= 174.79)
            return "Outer Residential";
        
        // Default suburban
        return "Outer Residential";
    }

    private static double CalculateDistanceToWellingtonFault(double lat, double lon)
    {
        // Wellington Fault runs roughly through the city
        // Approximate fault line at lon ~174.78
        var faultLon = 174.78;
        var faultLat = lat; // Parallel to latitude
        
        return GeoUtils.CalculateDistance(lat, lon, faultLat, faultLon) / 1000;
    }

    private static string? GetAttribute(ArcGisFeature feature, string name)
    {
        return feature.Attributes?.GetValueOrDefault(name)?.ToString();
    }

    private static double? GetWellingtonMaxHeight(string zoneName)
    {
        return zoneName.ToLower() switch
        {
            var z when z.Contains("central area") || z.Contains("central city") => 80.0,
            var z when z.Contains("inner residential") => 10.0,
            var z when z.Contains("outer residential") => 8.0,
            var z when z.Contains("medium density") => 12.0,
            var z when z.Contains("suburban centre") => 12.0,
            var z when z.Contains("business") => 12.0,
            _ => 8.0
        };
    }

    private static string GetWellingtonZoneDescription(string zoneName)
    {
        return zoneName.ToLower() switch
        {
            var z when z.Contains("central area") => "Wellington CBD - high-density mixed use",
            var z when z.Contains("inner residential") => "Inner suburbs - medium density residential",
            var z when z.Contains("outer residential") => "Suburban residential - lower density",
            var z when z.Contains("medium density") => "Medium density residential development",
            var z when z.Contains("suburban centre") => "Local commercial/retail centre",
            _ => $"{zoneName} - See Wellington District Plan for details"
        };
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
