# NIWA API Implementation Guide

**Service**: NIWA (National Institute of Water & Atmospheric Research)  
**Status**: ? Stub Only  
**Implementation File**: `Services/Integration/NiwaDataService.cs`

---

## ?? Overview

NIWA provides climate and weather data essential for site evaluation, including:
- **HIRDS** (High Intensity Rainfall Design System) - rainfall intensity for stormwater design
- **Wind zones** - for structural design per NZS 3604
- **Climate data** - temperature, sunshine, humidity

---

## ?? Official Documentation

| Resource | URL |
|----------|-----|
| **NIWA Website** | https://niwa.co.nz/ |
| **HIRDS v4** | https://hirds.niwa.co.nz/ |
| **CliFlo (Climate Database)** | https://cliflo.niwa.co.nz/ |
| **NIWA Atlas** | https://www.niwa.co.nz/our-science/climate/information-and-resources/nz-climate-atlas |
| **Climate Data** | https://niwa.co.nz/information-services |

---

## ?? API Services

### 1. HIRDS (High Intensity Rainfall Design System)

HIRDS provides rainfall intensity-duration-frequency (IDF) data for stormwater design.

**Web Interface**: https://hirds.niwa.co.nz/

**Note**: HIRDS is primarily a web-based tool. API access may require subscription.

#### Web Query Pattern

```
URL: https://hirds.niwa.co.nz/
Input: 
  - Latitude/Longitude OR
  - Address search
  - Return period (years): 2, 5, 10, 20, 50, 100
  - Duration (minutes): 10, 20, 30, 60, 120, 360, 720, 1440
```

#### HIRDS Output Data

```json
{
  "location": {
    "lat": -43.532,
    "lon": 172.636,
    "name": "Christchurch Central"
  },
  "rainfallDepths": {
    "2yr": {
      "10min": 8.2,
      "30min": 16.4,
      "60min": 22.1,
      "120min": 28.5,
      "360min": 38.2,
      "1440min": 52.8
    },
    "10yr": {
      "10min": 14.5,
      "30min": 28.1,
      "60min": 36.8,
      "120min": 47.2,
      "360min": 62.1,
      "1440min": 84.5
    },
    "50yr": {
      "10min": 21.2,
      "30min": 40.8,
      "60min": 52.6,
      "120min": 67.4,
      "360min": 88.2,
      "1440min": 118.6
    },
    "100yr": {
      "10min": 24.8,
      "30min": 47.2,
      "60min": 60.5,
      "120min": 77.8,
      "360min": 101.5,
      "1440min": 136.2
    }
  },
  "intensities_mm_hr": {
    "2yr": {
      "10min": 49.2,
      "30min": 32.8,
      "60min": 22.1
    }
  },
  "climateChangeFactors": {
    "2040_RCP45": 1.06,
    "2040_RCP85": 1.08,
    "2090_RCP45": 1.12,
    "2090_RCP85": 1.24
  }
}
```

### 2. Wind Zones (NZS 3604)

Wind zones for New Zealand are defined in NZS 3604:2011 (Timber Framed Buildings).

**Zones**:
| Zone | Basic Wind Speed | Typical Locations |
|------|------------------|-------------------|
| **Low** | < 32 m/s | Sheltered urban areas |
| **Medium** | 32-37 m/s | Most of NZ |
| **High** | 37-44 m/s | Exposed coastal, Wellington |
| **Very High** | 44-50 m/s | Extreme exposed locations |
| **Extra High (EH)** | > 50 m/s | Special cases |

#### Wind Zone Determination

Factors affecting wind zone:
1. **Geographic location** (base zone from maps)
2. **Terrain category** (1-4)
3. **Topography** (hill shape, escarpments)
4. **Shielding** (buildings, vegetation)
5. **Building height**

### 3. CliFlo (Climate Database)

CliFlo provides historical climate data from NIWA weather stations.

**Web Interface**: https://cliflo.niwa.co.nz/

**Requires**: Registration (free for limited queries)

#### Available Data

| Data Type | Parameters |
|-----------|------------|
| Temperature | Max, min, mean |
| Rainfall | Daily, monthly, annual |
| Sunshine | Hours per day |
| Wind | Speed, direction, gust |
| Humidity | Relative humidity |
| Evaporation | Potential ET |

---

## ?? Implementation

### Interface Definition

```csharp
// Services/Integration/IntegrationInterfaces.cs

public interface INiwaDataService
{
    /// <summary>
    /// Get HIRDS rainfall data for a location.
    /// </summary>
    Task<RainfallData?> GetRainfallDataAsync(
        double lat, double lon, CancellationToken ct = default);
    
    /// <summary>
    /// Get wind zone for a location.
    /// </summary>
    Task<string?> GetWindZoneAsync(
        double lat, double lon, CancellationToken ct = default);
    
    /// <summary>
    /// Get climate summary for a location.
    /// </summary>
    Task<ClimateSummary?> GetClimateSummaryAsync(
        double lat, double lon, CancellationToken ct = default);
}
```

### Proposed Implementation

```csharp
// Services/Integration/NiwaDataService.cs

public class NiwaDataService : INiwaDataService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NiwaDataService> _logger;
    
    // Wind zone lookup based on region (simplified - actual requires map lookup)
    private static readonly Dictionary<string, string> RegionWindZones = new()
    {
        ["Auckland"] = "Medium",
        ["Hamilton"] = "Medium",
        ["Tauranga"] = "Medium",
        ["Wellington"] = "Very High",
        ["Nelson"] = "High",
        ["Christchurch"] = "Medium",
        ["Queenstown"] = "High",
        ["Dunedin"] = "High"
    };

    public NiwaDataService(HttpClient httpClient, ILogger<NiwaDataService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<RainfallData?> GetRainfallDataAsync(
        double lat, double lon, CancellationToken ct = default)
    {
        try
        {
            // HIRDS doesn't have a public API - would need to scrape or use subscription
            // For now, return estimated values based on region
            
            var region = GetRegion(lat, lon);
            var baseIntensity = GetBaseRainfallIntensity(region);
            
            return new RainfallData
            {
                Location = new Location { Latitude = lat, Longitude = lon },
                DataSource = "NIWA HIRDS v4 (estimated)",
                
                // Rainfall depths (mm) for various return periods and durations
                RainfallDepths = new Dictionary<string, Dictionary<string, double>>
                {
                    ["10yr"] = new()
                    {
                        ["10min"] = baseIntensity * 0.22,
                        ["30min"] = baseIntensity * 0.43,
                        ["60min"] = baseIntensity * 0.57,
                        ["120min"] = baseIntensity * 0.73,
                        ["1440min"] = baseIntensity * 1.30
                    },
                    ["50yr"] = new()
                    {
                        ["10min"] = baseIntensity * 0.32,
                        ["30min"] = baseIntensity * 0.62,
                        ["60min"] = baseIntensity * 0.81,
                        ["120min"] = baseIntensity * 1.04,
                        ["1440min"] = baseIntensity * 1.84
                    },
                    ["100yr"] = new()
                    {
                        ["10min"] = baseIntensity * 0.38,
                        ["30min"] = baseIntensity * 0.72,
                        ["60min"] = baseIntensity * 0.93,
                        ["120min"] = baseIntensity * 1.20,
                        ["1440min"] = baseIntensity * 2.10
                    }
                },
                
                // Climate change adjustment factors
                ClimateChangeFactors = new Dictionary<string, double>
                {
                    ["2040_RCP45"] = 1.06,
                    ["2040_RCP85"] = 1.08,
                    ["2090_RCP45"] = 1.12,
                    ["2090_RCP85"] = 1.24
                },
                
                // Design notes
                DesignNotes = new List<string>
                {
                    "Values are estimates - use HIRDS directly for detailed design",
                    "Climate change factors should be applied for future projections",
                    "Refer to local council requirements for design standards"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rainfall data");
            return null;
        }
    }

    public async Task<string?> GetWindZoneAsync(
        double lat, double lon, CancellationToken ct = default)
    {
        try
        {
            // Determine wind zone based on location
            // This is simplified - actual requires NZS 3604 map lookup
            
            var region = GetRegion(lat, lon);
            var baseZone = RegionWindZones.GetValueOrDefault(region, "Medium");
            
            // Apply modifiers for coastal/exposed locations
            if (IsCoastal(lat, lon))
            {
                baseZone = UpgradeWindZone(baseZone);
            }
            
            // Wellington always Very High or higher
            if (region == "Wellington")
            {
                baseZone = "Very High";
            }
            
            return await Task.FromResult(baseZone);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting wind zone");
            return null;
        }
    }

    public async Task<ClimateSummary?> GetClimateSummaryAsync(
        double lat, double lon, CancellationToken ct = default)
    {
        try
        {
            var region = GetRegion(lat, lon);
            
            // Return regional climate averages
            // In production, this would query CliFlo or NIWA's climate atlas
            
            return new ClimateSummary
            {
                Region = region,
                AnnualRainfall = GetAnnualRainfall(region),
                AnnualSunshineHours = GetAnnualSunshine(region),
                MeanTemperature = GetMeanTemperature(region),
                FrostDaysPerYear = GetFrostDays(region),
                DataSource = "NIWA Climate Atlas",
                Notes = new List<string>
                {
                    "Values are regional averages",
                    "Site-specific conditions may vary significantly"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting climate summary");
            return null;
        }
    }

    private string GetRegion(double lat, double lon)
    {
        // Simplified region determination
        if (lat > -37.5) return "Auckland";
        if (lat > -38.5 && lon < 176) return "Hamilton";
        if (lat > -38.5 && lon >= 176) return "Tauranga";
        if (lat > -42 && lon > 174) return "Wellington";
        if (lat > -42 && lon <= 174) return "Nelson";
        if (lat > -44 && lon > 172) return "Christchurch";
        if (lat > -45.5) return "Queenstown";
        return "Dunedin";
    }

    private double GetBaseRainfallIntensity(string region) => region switch
    {
        "Auckland" => 65,
        "Wellington" => 75,
        "Christchurch" => 55,
        "Dunedin" => 60,
        "Queenstown" => 80,
        "Nelson" => 70,
        _ => 65
    };

    private double GetAnnualRainfall(string region) => region switch
    {
        "Auckland" => 1200,
        "Wellington" => 1250,
        "Christchurch" => 620,
        "Dunedin" => 800,
        "Queenstown" => 900,
        "Nelson" => 1000,
        "Hamilton" => 1200,
        "Tauranga" => 1350,
        _ => 1000
    };

    private double GetAnnualSunshine(string region) => region switch
    {
        "Auckland" => 2060,
        "Wellington" => 2025,
        "Christchurch" => 2100,
        "Dunedin" => 1650,
        "Nelson" => 2400,
        "Hamilton" => 2000,
        "Tauranga" => 2280,
        _ => 2000
    };

    private double GetMeanTemperature(string region) => region switch
    {
        "Auckland" => 15.4,
        "Wellington" => 12.8,
        "Christchurch" => 12.1,
        "Dunedin" => 10.7,
        "Queenstown" => 10.0,
        "Nelson" => 13.2,
        "Hamilton" => 14.0,
        "Tauranga" => 14.9,
        _ => 12.5
    };

    private int GetFrostDays(string region) => region switch
    {
        "Auckland" => 2,
        "Wellington" => 5,
        "Christchurch" => 35,
        "Dunedin" => 25,
        "Queenstown" => 90,
        "Nelson" => 10,
        "Hamilton" => 30,
        _ => 20
    };

    private bool IsCoastal(double lat, double lon)
    {
        // Simplified coastal check - within 5km of coast
        // In production, use actual coastline geometry
        return false; // Placeholder
    }

    private string UpgradeWindZone(string zone) => zone switch
    {
        "Low" => "Medium",
        "Medium" => "High",
        "High" => "Very High",
        "Very High" => "Extra High",
        _ => zone
    };
}
```

### Response Models

```csharp
// Models/ClimateData.cs

public class RainfallData
{
    public Location Location { get; set; } = new();
    public string DataSource { get; set; } = string.Empty;
    
    /// <summary>
    /// Rainfall depths in mm, keyed by return period (e.g., "10yr") 
    /// then by duration (e.g., "60min")
    /// </summary>
    public Dictionary<string, Dictionary<string, double>> RainfallDepths { get; set; } = new();
    
    /// <summary>
    /// Climate change adjustment factors, keyed by scenario (e.g., "2090_RCP85")
    /// </summary>
    public Dictionary<string, double> ClimateChangeFactors { get; set; } = new();
    
    public List<string> DesignNotes { get; set; } = new();
    
    /// <summary>
    /// Get rainfall intensity (mm/hr) for a given return period and duration
    /// </summary>
    public double GetIntensity(string returnPeriod, string durationMin)
    {
        if (!RainfallDepths.TryGetValue(returnPeriod, out var durations))
            return 0;
        if (!durations.TryGetValue(durationMin, out var depth))
            return 0;
        
        var minutes = int.Parse(durationMin.Replace("min", ""));
        return depth / minutes * 60; // Convert to mm/hr
    }
}

public class ClimateSummary
{
    public string Region { get; set; } = string.Empty;
    public double AnnualRainfall { get; set; }  // mm
    public double AnnualSunshineHours { get; set; }
    public double MeanTemperature { get; set; }  // °C
    public int FrostDaysPerYear { get; set; }
    public string DataSource { get; set; } = string.Empty;
    public List<string> Notes { get; set; } = new();
}

public class Location
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
```

---

## ?? Regional Climate Summary

| Region | Annual Rain (mm) | Sunshine (hrs) | Mean Temp (°C) | Frost Days |
|--------|-----------------|----------------|----------------|------------|
| Auckland | 1,200 | 2,060 | 15.4 | 2 |
| Wellington | 1,250 | 2,025 | 12.8 | 5 |
| Christchurch | 620 | 2,100 | 12.1 | 35 |
| Dunedin | 800 | 1,650 | 10.7 | 25 |
| Nelson | 1,000 | 2,400 | 13.2 | 10 |
| Queenstown | 900 | N/A | 10.0 | 90 |

---

## ?? Important Notes

1. **HIRDS Access**: HIRDS v4 is a web-based tool. For programmatic access, consider:
   - NIWA data subscription
   - Web scraping (check terms of use)
   - Pre-computed regional values

2. **Wind Zones**: Wind zone determination requires:
   - NZS 3604:2011 Appendix B maps
   - Site-specific assessment for exposed/coastal locations
   - Building height consideration

3. **Climate Change**: All rainfall design should consider climate change impacts using NIWA's published factors.

---

## ? Implementation Checklist

- [x] Interface definition
- [ ] HIRDS rainfall lookup (web scrape or API)
- [ ] Wind zone map integration
- [ ] CliFlo integration
- [ ] Climate summary data
- [ ] Regional averages fallback
- [ ] Unit tests
- [ ] Integration tests

---

## ?? Related

- [IntegrationInterfaces.cs](../../Services/Integration/IntegrationInterfaces.cs)
- [ClimateData Model](../../Models/ClimateData.cs)
- [NZS 3604:2011](https://www.standards.govt.nz/)
