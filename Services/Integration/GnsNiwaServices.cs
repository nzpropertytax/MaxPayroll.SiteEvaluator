using MaxPayroll.SiteEvaluator.Models;

namespace MaxPayroll.SiteEvaluator.Services.Integration;

/// <summary>
/// Integration with GNS Science seismic data.
/// https://www.gns.cri.nz/
/// https://api.geonet.org.nz/
/// </summary>
public class GnsDataService : IGnsDataService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GnsDataService> _logger;
    
    // NZS 1170.5 Z-values by region (simplified - actual uses interpolated maps)
    private static readonly Dictionary<string, double> RegionZValues = new()
    {
        ["Auckland"] = 0.13,
        ["Hamilton"] = 0.13,
        ["Tauranga"] = 0.19,
        ["Gisborne"] = 0.36,
        ["Napier"] = 0.39,
        ["Wellington"] = 0.40,
        ["Nelson"] = 0.27,
        ["Christchurch"] = 0.30,
        ["Queenstown"] = 0.30,
        ["Dunedin"] = 0.13
    };

    public GnsDataService(HttpClient httpClient, ILogger<GnsDataService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<SeismicHazard?> GetSeismicHazardAsync(double lat, double lon, CancellationToken ct = default)
    {
        try
        {
            // Try GeoNet API first
            var hazard = await TryGeoNetApiAsync(lat, lon, ct);
            if (hazard != null)
                return hazard;
            
            // Fall back to static Z-value lookup
            return await GetStaticSeismicHazardAsync(lat, lon, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying seismic data, using static fallback");
            return await GetStaticSeismicHazardAsync(lat, lon, ct);
        }
    }

    private async Task<SeismicHazard?> TryGeoNetApiAsync(double lat, double lon, CancellationToken ct)
    {
        try
        {
            // GeoNet doesn't have a direct seismic hazard API, but we can get recent quakes
            // to provide context about seismic activity in the area
            var bbox = CalculateBoundingBox(lat, lon, 50); // 50km radius
            var url = $"https://api.geonet.org.nz/quake?bbox={bbox}&MMI=3";
            
            var response = await _httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return null;
            
            var geoJson = await response.Content.ReadFromJsonAsync<GeoNetQuakeResponse>(ct);
            var recentQuakes = geoJson?.Features?.Count ?? 0;
            
            _logger.LogInformation("Found {Count} recent quakes near {Lat}, {Lon}", recentQuakes, lat, lon);
            
            // Still need to use static Z-values, but we now have quake context
            return null; // Fall through to static lookup
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GeoNet API query failed");
            return null;
        }
    }

    private Task<SeismicHazard> GetStaticSeismicHazardAsync(double lat, double lon, CancellationToken ct)
    {
        var region = GetRegion(lat, lon);
        var zValue = RegionZValues.GetValueOrDefault(region, 0.20);
        var nearbyFaults = GetStaticNearbyFaults(lat, lon);
        var nearFaultFactor = CalculateNearFaultFactor(nearbyFaults);
        
        var hazard = new SeismicHazard
        {
            Zone = GetSeismicZoneDescription(zValue),
            ZoneFactor = zValue,
            SiteClass = "C", // Default assumption - actual requires geotech investigation
            NearFaultFactor = nearFaultFactor,
            PGA = zValue * nearFaultFactor, // Simplified PGA calculation
            PgaSls = zValue * 0.5, // 500-year return period (SLS)
            PgaUls = zValue * nearFaultFactor, // 2500-year return period (ULS)
            NearbyFaults = nearbyFaults,
            DesignStandard = "NZS 1170.5:2004",
            Source = new DataSource
            {
                SourceName = "GNS Science / NZS 1170.5",
                SourceUrl = "https://www.gns.cri.nz/",
                DataDate = DateTime.UtcNow,
                RetrievedDate = DateTime.UtcNow,
                Notes = "Z-values from NZS 1170.5:2004. Site class assumed as C. Verify with geotechnical investigation."
            }
        };
        
        return Task.FromResult(hazard);
    }

    public async Task<List<ActiveFault>> GetNearbyFaultsAsync(double lat, double lon, double radiusKm, CancellationToken ct = default)
    {
        try
        {
            // Try GNS Active Faults Database API
            var url = $"https://data.gns.cri.nz/af/api/faults?lat={lat}&lon={lon}&radius={radiusKm}";
            var response = await _httpClient.GetAsync(url, ct);
            
            if (response.IsSuccessStatusCode)
            {
                var faults = await response.Content.ReadFromJsonAsync<List<GnsFault>>(ct);
                if (faults != null && faults.Count > 0)
                {
                    return faults.Select(f => new ActiveFault
                    {
                        Name = f.Name ?? "Unknown",
                        DistanceKm = f.DistanceKm ?? GeoUtils.CalculateDistance(lat, lon, f.Lat ?? lat, f.Lon ?? lon) / 1000,
                        RecurrenceInterval = f.RecurrenceInterval,
                        LastRupture = f.LastRupture,
                        FaultType = f.FaultType,
                        SlipRate = f.SlipRate
                    }).ToList();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GNS fault API query failed, using static data");
        }
        
        // Fall back to static fault data
        return GetStaticNearbyFaults(lat, lon)
            .Where(f => f.DistanceKm <= radiusKm)
            .ToList();
    }

    private List<ActiveFault> GetStaticNearbyFaults(double lat, double lon)
    {
        var faults = new List<ActiveFault>();
        
        // Alpine Fault (runs along South Island)
        var alpineDistance = CalculateDistanceToAlpineFault(lat, lon);
        if (alpineDistance < 100)
        {
            faults.Add(new ActiveFault
            {
                Name = "Alpine Fault",
                DistanceKm = alpineDistance,
                RecurrenceInterval = "~300 years",
                LastRupture = "1717",
                FaultType = "Dextral strike-slip",
                SlipRate = "27 mm/year",
                MaxMagnitude = 8.2
            });
        }
        
        // Wellington Fault
        if (lat < -40.5 && lat > -42 && lon > 174 && lon < 176)
        {
            var wellingtonDistance = GeoUtils.CalculateDistance(lat, lon, -41.29, 174.78) / 1000;
            if (wellingtonDistance < 30)
            {
                faults.Add(new ActiveFault
                {
                    Name = "Wellington Fault",
                    DistanceKm = wellingtonDistance,
                    RecurrenceInterval = "~840 years",
                    LastRupture = "~1400 AD",
                    FaultType = "Dextral strike-slip",
                    SlipRate = "6-8 mm/year",
                    MaxMagnitude = 7.5
                });
            }
        }
        
        // Port Hills Fault (Canterbury)
        if (lat < -43.4 && lat > -43.7 && lon > 172.5 && lon < 172.8)
        {
            var portHillsDistance = GeoUtils.CalculateDistance(lat, lon, -43.59, 172.68) / 1000;
            if (portHillsDistance < 20)
            {
                faults.Add(new ActiveFault
                {
                    Name = "Port Hills Fault",
                    DistanceKm = portHillsDistance,
                    RecurrenceInterval = "~10,000 years",
                    FaultType = "Reverse",
                    SlipRate = "0.5 mm/year",
                    MaxMagnitude = 6.5
                });
            }
        }
        
        // Greendale Fault (Canterbury - ruptured 2010)
        if (lat < -43.4 && lat > -43.7 && lon > 171.8 && lon < 172.5)
        {
            var greendaleDistance = GeoUtils.CalculateDistance(lat, lon, -43.58, 172.18) / 1000;
            if (greendaleDistance < 30)
            {
                faults.Add(new ActiveFault
                {
                    Name = "Greendale Fault",
                    DistanceKm = greendaleDistance,
                    RecurrenceInterval = "~16,000 years",
                    LastRupture = "2010",
                    FaultType = "Strike-slip",
                    SlipRate = "0.3 mm/year",
                    MaxMagnitude = 7.1
                });
            }
        }
        
        return faults.OrderBy(f => f.DistanceKm).ToList();
    }

    private double CalculateDistanceToAlpineFault(double lat, double lon)
    {
        // Simplified - Alpine Fault runs roughly from (-42.5, 171.2) to (-44.5, 168.0)
        // Calculate perpendicular distance to line
        if (lon > 173 || lat > -42) return 200; // Too far east or north
        
        // Rough approximation
        var faultLat = -43.5;
        var faultLon = 170.0;
        return GeoUtils.CalculateDistance(lat, lon, faultLat, faultLon) / 1000;
    }

    private double CalculateNearFaultFactor(List<ActiveFault> faults)
    {
        var nearest = faults.MinBy(f => f.DistanceKm);
        if (nearest == null) return 1.0;
        
        // NZS 1170.5 Section 3.1.6 near-fault factor
        return nearest.DistanceKm switch
        {
            < 2 => 1.5,
            < 5 => 1.3,
            < 10 => 1.15,
            < 20 => 1.0,
            _ => 1.0
        };
    }

    private string GetRegion(double lat, double lon)
    {
        if (lat > -37.5) return "Auckland";
        if (lat > -38.5 && lon < 176) return "Hamilton";
        if (lat > -38.5 && lon >= 176) return "Tauranga";
        if (lat > -40 && lon > 177) return "Gisborne";
        if (lat > -40 && lon > 176) return "Napier";
        if (lat > -42 && lon > 174) return "Wellington";
        if (lat > -42 && lon <= 174) return "Nelson";
        if (lat > -44 && lon > 172) return "Christchurch";
        if (lat > -45.5 && lon < 170) return "Queenstown";
        return "Dunedin";
    }

    private string GetSeismicZoneDescription(double zValue) => zValue switch
    {
        >= 0.40 => "High Seismic Zone (Z ? 0.40)",
        >= 0.30 => "Medium-High Seismic Zone (0.30 ? Z < 0.40)",
        >= 0.20 => "Medium Seismic Zone (0.20 ? Z < 0.30)",
        >= 0.10 => "Low Seismic Zone (Z < 0.20)",
        _ => "Low Seismic Zone"
    };

    private string CalculateBoundingBox(double lat, double lon, double radiusKm)
    {
        // Approximate 1 degree = 111km
        var delta = radiusKm / 111.0;
        var minLon = lon - delta;
        var minLat = lat - delta;
        var maxLon = lon + delta;
        var maxLat = lat + delta;
        return $"{minLon:F4},{minLat:F4},{maxLon:F4},{maxLat:F4}";
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
    public double? Lat { get; set; }
    public double? Lon { get; set; }
    public string? RecurrenceInterval { get; set; }
    public string? LastRupture { get; set; }
    public string? FaultType { get; set; }
    public string? SlipRate { get; set; }
}

public class GeoNetQuakeResponse
{
    public string? Type { get; set; }
    public List<GeoNetFeature>? Features { get; set; }
}

public class GeoNetFeature
{
    public GeoNetGeometry? Geometry { get; set; }
    public GeoNetProperties? Properties { get; set; }
}

public class GeoNetGeometry
{
    public double[]? Coordinates { get; set; }
}

public class GeoNetProperties
{
    public string? PublicID { get; set; }
    public DateTime? Time { get; set; }
    public double? Magnitude { get; set; }
    public double? Depth { get; set; }
    public string? Locality { get; set; }
    public int? MMI { get; set; }
}

/// <summary>
/// Integration with NIWA climate data.
/// https://niwa.co.nz/
/// https://hirds.niwa.co.nz/ (HIRDS - High Intensity Rainfall Design System)
/// </summary>
public class NiwaDataService : INiwaDataService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NiwaDataService> _logger;
    
    // Regional annual rainfall averages (mm)
    private static readonly Dictionary<string, double> RegionalRainfall = new()
    {
        ["Auckland"] = 1240,
        ["Hamilton"] = 1200,
        ["Tauranga"] = 1350,
        ["Gisborne"] = 1000,
        ["Napier"] = 800,
        ["Wellington"] = 1250,
        ["Nelson"] = 1000,
        ["Blenheim"] = 650,
        ["Christchurch"] = 620,
        ["Timaru"] = 560,
        ["Queenstown"] = 900,
        ["Dunedin"] = 800,
        ["Invercargill"] = 1050
    };
    
    // Regional sunshine hours per year
    private static readonly Dictionary<string, double> RegionalSunshine = new()
    {
        ["Auckland"] = 2060,
        ["Hamilton"] = 2000,
        ["Tauranga"] = 2280,
        ["Gisborne"] = 2200,
        ["Napier"] = 2200,
        ["Wellington"] = 2025,
        ["Nelson"] = 2400,
        ["Blenheim"] = 2450,
        ["Christchurch"] = 2100,
        ["Timaru"] = 1900,
        ["Queenstown"] = 1900,
        ["Dunedin"] = 1650,
        ["Invercargill"] = 1600
    };
    
    // NZS 3604:2011 Basic Wind Speeds (m/s) by Wind Zone
    private static readonly Dictionary<string, double> WindZoneBasicSpeeds = new()
    {
        ["Low"] = 32.0,
        ["Medium"] = 37.0,
        ["High"] = 44.0,
        ["Very High"] = 50.0,
        ["Extra High"] = 55.0
    };

    public NiwaDataService(HttpClient httpClient, ILogger<NiwaDataService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<RainfallData?> GetRainfallDataAsync(double lat, double lon, CancellationToken ct = default)
    {
        try
        {
            // HIRDS doesn't have a public API - use static regional data
            // In production, you'd need NIWA subscription or web scraping
            
            var region = GetRegion(lat, lon);
            var baseIntensity = GetBaseRainfallIntensity(region);
            
            return await Task.FromResult(new RainfallData
            {
                AnnualMean = RegionalRainfall.GetValueOrDefault(region, 1000),
                
                // HIRDS rainfall depths (mm) - estimated from regional data
                // Format: I{return period}_{duration in minutes}
                I10_10 = baseIntensity * 0.22,   // 10-year ARI, 10-min duration
                I10_60 = baseIntensity * 0.57,   // 10-year ARI, 60-min duration
                I100_10 = baseIntensity * 0.38,  // 100-year ARI, 10-min duration
                I100_60 = baseIntensity * 0.93,  // 100-year ARI, 60-min duration
                
                HirtdsStation = $"Estimated from {region} regional data",
                
                // Full HIRDS rainfall depth table
                RainfallDepths = new Dictionary<string, Dictionary<string, double>>
                {
                    ["2yr"] = new()
                    {
                        ["10min"] = baseIntensity * 0.13,
                        ["20min"] = baseIntensity * 0.20,
                        ["30min"] = baseIntensity * 0.26,
                        ["60min"] = baseIntensity * 0.35,
                        ["120min"] = baseIntensity * 0.45,
                        ["360min"] = baseIntensity * 0.62,
                        ["720min"] = baseIntensity * 0.74,
                        ["1440min"] = baseIntensity * 0.84
                    },
                    ["5yr"] = new()
                    {
                        ["10min"] = baseIntensity * 0.18,
                        ["20min"] = baseIntensity * 0.28,
                        ["30min"] = baseIntensity * 0.35,
                        ["60min"] = baseIntensity * 0.47,
                        ["120min"] = baseIntensity * 0.60,
                        ["360min"] = baseIntensity * 0.84,
                        ["720min"] = baseIntensity * 1.00,
                        ["1440min"] = baseIntensity * 1.12
                    },
                    ["10yr"] = new()
                    {
                        ["10min"] = baseIntensity * 0.22,
                        ["20min"] = baseIntensity * 0.33,
                        ["30min"] = baseIntensity * 0.43,
                        ["60min"] = baseIntensity * 0.57,
                        ["120min"] = baseIntensity * 0.73,
                        ["360min"] = baseIntensity * 1.00,
                        ["720min"] = baseIntensity * 1.18,
                        ["1440min"] = baseIntensity * 1.30
                    },
                    ["20yr"] = new()
                    {
                        ["10min"] = baseIntensity * 0.26,
                        ["20min"] = baseIntensity * 0.40,
                        ["30min"] = baseIntensity * 0.51,
                        ["60min"] = baseIntensity * 0.67,
                        ["120min"] = baseIntensity * 0.86,
                        ["360min"] = baseIntensity * 1.18,
                        ["720min"] = baseIntensity * 1.40,
                        ["1440min"] = baseIntensity * 1.55
                    },
                    ["50yr"] = new()
                    {
                        ["10min"] = baseIntensity * 0.32,
                        ["20min"] = baseIntensity * 0.48,
                        ["30min"] = baseIntensity * 0.62,
                        ["60min"] = baseIntensity * 0.81,
                        ["120min"] = baseIntensity * 1.04,
                        ["360min"] = baseIntensity * 1.43,
                        ["720min"] = baseIntensity * 1.68,
                        ["1440min"] = baseIntensity * 1.84
                    },
                    ["100yr"] = new()
                    {
                        ["10min"] = baseIntensity * 0.38,
                        ["20min"] = baseIntensity * 0.56,
                        ["30min"] = baseIntensity * 0.72,
                        ["60min"] = baseIntensity * 0.93,
                        ["120min"] = baseIntensity * 1.20,
                        ["360min"] = baseIntensity * 1.65,
                        ["720min"] = baseIntensity * 1.94,
                        ["1440min"] = baseIntensity * 2.10
                    }
                },
                
                ClimateChangeFactors = new Dictionary<string, double>
                {
                    // MfE guidance for climate change rainfall adjustments
                    ["2040_RCP26"] = 1.04,  // Low emissions scenario
                    ["2040_RCP45"] = 1.06,  // Medium emissions
                    ["2040_RCP60"] = 1.07,
                    ["2040_RCP85"] = 1.08,  // High emissions
                    ["2090_RCP26"] = 1.06,
                    ["2090_RCP45"] = 1.12,
                    ["2090_RCP60"] = 1.16,
                    ["2090_RCP85"] = 1.24,  // Worst case
                    ["2130_RCP85"] = 1.35   // Extended projection
                },
                
                Source = new DataSource
                {
                    SourceName = "NIWA HIRDS v4 (estimated)",
                    SourceUrl = "https://hirds.niwa.co.nz/",
                    DataDate = DateTime.UtcNow,
                    RetrievedDate = DateTime.UtcNow,
                    Notes = "Values are regional estimates based on HIRDS v4 methodology. " +
                            "Use HIRDS directly (https://hirds.niwa.co.nz) for detailed design. " +
                            "Climate change factors from MfE guidance."
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rainfall data");
            return null;
        }
    }

    public async Task<string?> GetWindZoneAsync(double lat, double lon, CancellationToken ct = default)
    {
        try
        {
            // Wind zone lookup based on NZS 3604:2011
            return await Task.FromResult(GetStaticWindZone(lat, lon));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying wind zone");
            return GetStaticWindZone(lat, lon);
        }
    }

    /// <summary>
    /// Get detailed wind data for a location including basic wind speed.
    /// </summary>
    public async Task<WindData?> GetWindDataAsync(double lat, double lon, CancellationToken ct = default)
    {
        var windZone = GetStaticWindZone(lat, lon);
        var basicSpeed = WindZoneBasicSpeeds.GetValueOrDefault(windZone, 37.0);
        var region = GetRegion(lat, lon);
        
        return await Task.FromResult(new WindData
        {
            WindZone = windZone,
            BasicWindSpeed = basicSpeed,
            UltimateWindSpeed = basicSpeed * 1.0, // V_u for Ultimate limit state
            ServiceabilityWindSpeed = basicSpeed * 0.75, // V_s for Serviceability
            DirectionalMultiplier = GetDirectionalMultiplier(lat, lon),
            TerrainCategory = EstimateTerrainCategory(lat, lon),
            ShieldingFactor = 1.0, // Default - needs site-specific assessment
            TopographyFactor = GetTopographyFactor(lat, lon),
            
            // Regional wind characteristics
            PredominantDirection = GetPredominantWindDirection(region),
            
            Source = new DataSource
            {
                SourceName = "NZS 3604:2011 / AS/NZS 1170.2",
                SourceUrl = "https://www.standards.govt.nz/",
                DataDate = DateTime.UtcNow,
                RetrievedDate = DateTime.UtcNow,
                Notes = $"Wind Zone: {windZone}. Basic wind speed V_R = {basicSpeed} m/s. " +
                        "Site-specific assessment recommended for exposed or elevated sites."
            }
        });
    }

    /// <summary>
    /// Get climate summary for a location.
    /// </summary>
    public async Task<ClimateSummary?> GetClimateSummaryAsync(double lat, double lon, CancellationToken ct = default)
    {
        var region = GetRegion(lat, lon);
        
        return await Task.FromResult(new ClimateSummary
        {
            Region = region,
            AnnualRainfall = RegionalRainfall.GetValueOrDefault(region, 1000),
            AnnualSunshineHours = RegionalSunshine.GetValueOrDefault(region, 2000),
            MeanTemperature = GetMeanTemperature(region),
            FrostDaysPerYear = GetFrostDays(region),
            WindZone = GetStaticWindZone(lat, lon),
            Source = new DataSource
            {
                SourceName = "NIWA Climate Atlas",
                SourceUrl = "https://niwa.co.nz/our-science/climate/information-and-resources/nz-climate-atlas",
                DataDate = DateTime.UtcNow,
                RetrievedDate = DateTime.UtcNow,
                Notes = "Regional averages from NIWA Climate Database (CliFlo). " +
                        "Site-specific conditions may vary due to local topography."
            }
        });
    }

    private double GetBaseRainfallIntensity(string region) => region switch
    {
        "Auckland" => 65,
        "Hamilton" => 60,
        "Tauranga" => 70,
        "Gisborne" => 55,
        "Napier" => 50,
        "Wellington" => 75,
        "Nelson" => 55,
        "Blenheim" => 45,
        "Christchurch" => 55,
        "Timaru" => 45,
        "Queenstown" => 80,
        "Dunedin" => 60,
        "Invercargill" => 65,
        _ => 65
    };

    private double GetMeanTemperature(string region) => region switch
    {
        "Auckland" => 15.4,
        "Hamilton" => 14.0,
        "Tauranga" => 14.9,
        "Gisborne" => 14.5,
        "Napier" => 14.6,
        "Wellington" => 12.8,
        "Nelson" => 13.2,
        "Blenheim" => 13.0,
        "Christchurch" => 12.1,
        "Timaru" => 11.5,
        "Queenstown" => 10.0,
        "Dunedin" => 10.7,
        "Invercargill" => 9.8,
        _ => 12.5
    };

    private int GetFrostDays(string region) => region switch
    {
        "Auckland" => 2,
        "Hamilton" => 30,
        "Tauranga" => 5,
        "Gisborne" => 15,
        "Napier" => 20,
        "Wellington" => 5,
        "Nelson" => 10,
        "Blenheim" => 35,
        "Christchurch" => 35,
        "Timaru" => 40,
        "Queenstown" => 90,
        "Dunedin" => 25,
        "Invercargill" => 45,
        _ => 20
    };

    private string GetRegion(double lat, double lon)
    {
        // More detailed region mapping
        if (lat > -36.0) return "Auckland"; // Northland defaults to Auckland
        if (lat > -37.5 && lon < 175.5) return "Auckland";
        if (lat > -38.5 && lon < 176) return "Hamilton";
        if (lat > -38.5 && lon >= 176) return "Tauranga";
        if (lat > -39.5 && lon > 177.5) return "Gisborne";
        if (lat > -40.0 && lon > 176) return "Napier";
        if (lat > -41.8 && lon > 174) return "Wellington";
        if (lat > -42.0 && lon <= 174 && lon > 173) return "Nelson";
        if (lat > -42.0 && lon <= 174 && lon <= 174) return "Blenheim";
        if (lat > -44.0 && lon > 172) return "Christchurch";
        if (lat > -44.5 && lon > 170.5 && lon <= 172) return "Timaru";
        if (lat > -45.5 && lon < 170) return "Queenstown";
        if (lat > -46.0) return "Dunedin";
        return "Invercargill";
    }

    private string GetStaticWindZone(double lat, double lon)
    {
        // NZS 3604:2011 Wind Zone lookup (enhanced)
        
        // Wellington region - Very High due to Cook Strait funneling
        if (lat <= -40.8 && lat >= -41.5 && lon >= 174.5 && lon <= 175.5)
            return "Very High";
        
        // Foveaux Strait area - Very High
        if (lat < -46.0 && lon > 167 && lon < 169)
            return "Very High";
        
        // Exposed coastal North Island west coast
        if (lon < 174.0 && lat > -39.0 && lat < -36.0)
            return "High";
        
        // Canterbury nor'wester zone - High
        if (lat <= -43.0 && lat >= -44.5 && lon >= 171.0 && lon <= 172.0)
            return "High";
        
        // Exposed coastal areas - generally High
        if (IsCoastalExposed(lat, lon))
            return "High";
        
        // Inland Canterbury - Medium
        if (lat <= -43.0 && lat >= -44.5 && lon >= 171.5 && lon <= 173.0)
            return "Medium";
        
        // Auckland urban - Low to Medium
        if (lat <= -36.5 && lat >= -37.5 && lon >= 174.5 && lon <= 175.0)
            return "Low";
        
        // Sheltered inland areas
        if (IsSheltered(lat, lon))
            return "Low";
        
        // Default - Medium
        return "Medium";
    }

    private bool IsCoastalExposed(double lat, double lon)
    {
        // West coast South Island - very exposed
        if (lon < 171.0 && lat < -42.0 && lat > -46.0) return true;
        
        // Far north exposed
        if (lat > -35.5) return true;
        
        // Stewart Island / Southland coast
        if (lat < -46.0) return true;
        
        // East Cape
        if (lon > 178.0 && lat > -38.5) return true;
        
        return false;
    }

    private bool IsSheltered(double lat, double lon)
    {
        // Inland Otago (Alexandra, Cromwell)
        if (lat < -44.8 && lat > -45.5 && lon > 169.0 && lon < 170.0)
            return true;
        
        // Marlborough valleys
        if (lat > -42.0 && lat < -41.3 && lon > 173.5 && lon < 174.0)
            return true;
        
        return false;
    }

    private double GetDirectionalMultiplier(double lat, double lon)
    {
        // Simplified - actual depends on building orientation and local wind patterns
        // NZ typically has strong westerly/southerly winds
        return 1.0; // Conservative default
    }

    private string EstimateTerrainCategory(double lat, double lon)
    {
        // NZS 3604 terrain categories
        // TC1: Exposed open terrain (coasts, flat rural)
        // TC2: Water surfaces, open terrain, grassland
        // TC2.5: Semi-sheltered (suburban, scattered trees)
        // TC3: Suburban, forest
        
        if (IsCoastalExposed(lat, lon)) return "TC1";
        if (IsSheltered(lat, lon)) return "TC3";
        
        // Default suburban
        return "TC2.5";
    }

    private double GetTopographyFactor(double lat, double lon)
    {
        // Hills and ridges can increase wind speed by up to 1.5x
        // This would need DEM data for proper implementation
        
        // Wellington hills
        if (lat >= -41.35 && lat <= -41.25 && lon >= 174.75 && lon <= 174.85)
            return 1.2;
        
        // Port Hills Christchurch
        if (lat >= -43.65 && lat <= -43.55 && lon >= 172.6 && lon <= 172.75)
            return 1.15;
        
        return 1.0; // Flat terrain default
    }

    private string GetPredominantWindDirection(string region) => region switch
    {
        "Auckland" => "SW",
        "Hamilton" => "W",
        "Tauranga" => "W",
        "Wellington" => "NW/S",  // Bi-modal due to Cook Strait
        "Nelson" => "NW",
        "Christchurch" => "NE/SW", // Nor'wester and southerly
        "Queenstown" => "NW",
        "Dunedin" => "SW",
        _ => "W"
    };
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

/// <summary>
/// Climate summary data.
/// </summary>
public class ClimateSummary
{
    public string Region { get; set; } = string.Empty;
    public double AnnualRainfall { get; set; }
    public double AnnualSunshineHours { get; set; }
    public double MeanTemperature { get; set; }
    public int FrostDaysPerYear { get; set; }
    public string WindZone { get; set; } = string.Empty;
    public DataSource? Source { get; set; }
}
