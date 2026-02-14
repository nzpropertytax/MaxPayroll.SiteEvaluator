# GNS Science API Implementation Guide

**Service**: GNS Science - Geological & Seismic Data  
**Status**: ? Stub Only  
**Implementation File**: `Services/Integration/GnsDataService.cs`

---

## ?? Overview

GNS Science (Institute of Geological and Nuclear Sciences) provides geological, seismic, and natural hazard data for New Zealand. Key data includes active fault locations, seismic hazard assessments, and Peak Ground Acceleration (PGA) values.

---

## ?? Official Documentation

| Resource | URL |
|----------|-----|
| **GNS Science** | https://www.gns.cri.nz/ |
| **GeoNet** | https://www.geonet.org.nz/ |
| **Active Faults Database** | https://data.gns.cri.nz/af/ |
| **QMAP (Geological Maps)** | https://www.gns.cri.nz/data-and-resources/databases/qmap |
| **GeoNet API** | https://api.geonet.org.nz/ |
| **Hazard Platform** | https://www.gns.cri.nz/research-projects/national-seismic-hazard-model |

---

## ?? API Endpoints

### 1. GeoNet API (Seismic Data)

GeoNet provides real-time seismic monitoring data and historical earthquake records.

**Base URL**: `https://api.geonet.org.nz/`

#### Get Recent Quakes

```http
GET https://api.geonet.org.nz/quake?MMI=3
```

**Response**:
```json
{
  "type": "FeatureCollection",
  "features": [
    {
      "type": "Feature",
      "geometry": {
        "type": "Point",
        "coordinates": [172.636, -43.532]
      },
      "properties": {
        "publicID": "2024p123456",
        "time": "2024-06-15T10:30:00.000Z",
        "depth": 12.5,
        "magnitude": 4.2,
        "locality": "15 km east of Christchurch"
      }
    }
  ]
}
```

#### Get Quakes Near Location

```http
GET https://api.geonet.org.nz/quake/search?bbox=172.0,-44.0,173.0,-43.0&startdate=2020-01-01
```

### 2. Active Faults Database

The Active Faults Database provides information about known active faults in New Zealand.

**Data Portal**: https://data.gns.cri.nz/af/

**Note**: This database is primarily accessed via a web interface and downloadable datasets. Direct API access may be limited.

#### Downloadable Data

```
Format: GeoJSON, Shapefile, KML
URL: https://data.gns.cri.nz/af/download
Contains:
  - Fault traces (line geometry)
  - Fault names
  - Slip rates
  - Recurrence intervals
  - Last rupture date
  - Fault type (strike-slip, normal, reverse)
```

#### Fault Data Structure

```json
{
  "type": "Feature",
  "geometry": {
    "type": "LineString",
    "coordinates": [[172.5, -43.5], [172.6, -43.6]]
  },
  "properties": {
    "fault_name": "Alpine Fault",
    "fault_id": "AF-001",
    "slip_rate_mm_yr": 27,
    "recurrence_interval_yrs": 300,
    "last_rupture": "1717",
    "fault_type": "dextral strike-slip"
  }
}
```

### 3. National Seismic Hazard Model (NSHM)

Provides PGA (Peak Ground Acceleration) values for building design.

**Reference**: NZS 1170.5:2004 (Structural Design Actions - Earthquake Actions)

#### Seismic Hazard Parameters

```
Z-values (zone factor) by location
PGA values for:
  - 500-year return period (SLS)
  - 2500-year return period (ULS)
Site Class (A, B, C, D, E)
Near-fault factors
```

---

## ?? Implementation

### Interface Definition

```csharp
// Services/Integration/IntegrationInterfaces.cs

public interface IGnsDataService
{
    /// <summary>
    /// Get seismic hazard assessment for a location.
    /// </summary>
    Task<SeismicHazard?> GetSeismicHazardAsync(
        double lat, double lon, CancellationToken ct = default);
    
    /// <summary>
    /// Get active faults within a radius of a location.
    /// </summary>
    Task<List<ActiveFault>> GetNearbyFaultsAsync(
        double lat, double lon, double radiusKm, CancellationToken ct = default);
    
    /// <summary>
    /// Get historical earthquakes near a location.
    /// </summary>
    Task<List<HistoricalQuake>> GetHistoricalQuakesAsync(
        double lat, double lon, double radiusKm, 
        DateTime since, CancellationToken ct = default);
}
```

### Proposed Implementation

```csharp
// Services/Integration/GnsDataService.cs

public class GnsDataService : IGnsDataService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GnsDataService> _logger;
    
    // Static Z-values from NZS 1170.5 (simplified - actual is interpolated from maps)
    private static readonly Dictionary<string, double> ZoneFactors = new()
    {
        ["Auckland"] = 0.13,
        ["Hamilton"] = 0.13,
        ["Tauranga"] = 0.19,
        ["Wellington"] = 0.40,
        ["Christchurch"] = 0.30,
        ["Dunedin"] = 0.13,
        ["Queenstown"] = 0.30
    };

    public GnsDataService(HttpClient httpClient, ILogger<GnsDataService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<SeismicHazard?> GetSeismicHazardAsync(
        double lat, double lon, CancellationToken ct = default)
    {
        try
        {
            // Get nearest city for Z-value lookup
            var nearestCity = GetNearestMajorCity(lat, lon);
            var zFactor = ZoneFactors.GetValueOrDefault(nearestCity, 0.20);
            
            // Get nearby faults for near-fault factor
            var nearbyFaults = await GetNearbyFaultsAsync(lat, lon, 20, ct);
            var nearFaultFactor = CalculateNearFaultFactor(nearbyFaults);
            
            // Determine site class (simplified - actual requires geotech investigation)
            var siteClass = "C"; // Default assumption for soft soils
            
            return new SeismicHazard
            {
                ZoneFactor = zFactor,
                SiteClass = siteClass,
                NearFaultFactor = nearFaultFactor,
                PgaSls = zFactor * 0.5,  // 500-year return period (simplified)
                PgaUls = zFactor,         // 2500-year return period (simplified)
                SeismicZone = GetSeismicZone(zFactor),
                NearbyFaults = nearbyFaults.Take(5).ToList(),
                DesignStandard = "NZS 1170.5:2004"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting seismic hazard data");
            return null;
        }
    }

    public async Task<List<ActiveFault>> GetNearbyFaultsAsync(
        double lat, double lon, double radiusKm, CancellationToken ct = default)
    {
        try
        {
            // In production, this would query the Active Faults Database
            // For now, return known major faults in Canterbury
            
            var faults = new List<ActiveFault>();
            
            // Example: Alpine Fault (simplified - actual has complex geometry)
            var distanceToAlpineFault = CalculateDistanceToAlpineFault(lat, lon);
            if (distanceToAlpineFault <= radiusKm)
            {
                faults.Add(new ActiveFault
                {
                    Name = "Alpine Fault",
                    FaultId = "AF-001",
                    DistanceKm = distanceToAlpineFault,
                    SlipRateMmPerYear = 27,
                    RecurrenceIntervalYears = 300,
                    LastRuptureYear = 1717,
                    FaultType = "Dextral strike-slip",
                    MaxMagnitude = 8.2
                });
            }
            
            // Port Hills Fault (Canterbury)
            if (IsInCanterbury(lat, lon))
            {
                var distanceToPortHills = CalculateDistanceToFault(
                    lat, lon, -43.59, 172.68);
                if (distanceToPortHills <= radiusKm)
                {
                    faults.Add(new ActiveFault
                    {
                        Name = "Port Hills Fault",
                        FaultId = "PHF-001",
                        DistanceKm = distanceToPortHills,
                        SlipRateMmPerYear = 0.5,
                        RecurrenceIntervalYears = 10000,
                        FaultType = "Reverse",
                        MaxMagnitude = 6.5
                    });
                }
            }
            
            return faults.OrderBy(f => f.DistanceKm).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting nearby faults");
            return new List<ActiveFault>();
        }
    }

    public async Task<List<HistoricalQuake>> GetHistoricalQuakesAsync(
        double lat, double lon, double radiusKm, 
        DateTime since, CancellationToken ct = default)
    {
        try
        {
            // Query GeoNet API
            var bbox = CalculateBoundingBox(lat, lon, radiusKm);
            var url = $"https://api.geonet.org.nz/quake?" +
                $"bbox={bbox.MinLon},{bbox.MinLat},{bbox.MaxLon},{bbox.MaxLat}&" +
                $"startdate={since:yyyy-MM-dd}";
            
            var response = await _httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return new List<HistoricalQuake>();
            
            var geoJson = await response.Content
                .ReadFromJsonAsync<GeoNetQuakeResponse>(ct);
            
            return geoJson?.Features?.Select(f => new HistoricalQuake
            {
                Id = f.Properties.PublicID,
                Time = f.Properties.Time,
                Magnitude = f.Properties.Magnitude,
                Depth = f.Properties.Depth,
                Latitude = f.Geometry.Coordinates[1],
                Longitude = f.Geometry.Coordinates[0],
                Locality = f.Properties.Locality,
                MMI = f.Properties.MMI
            }).ToList() ?? new List<HistoricalQuake>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting historical quakes");
            return new List<HistoricalQuake>();
        }
    }

    private string GetSeismicZone(double zFactor) => zFactor switch
    {
        >= 0.40 => "Zone 4 (High)",
        >= 0.30 => "Zone 3 (Medium-High)",
        >= 0.20 => "Zone 2 (Medium)",
        >= 0.10 => "Zone 1 (Low)",
        _ => "Zone 1 (Low)"
    };

    private double CalculateNearFaultFactor(List<ActiveFault> faults)
    {
        var nearestFault = faults.MinBy(f => f.DistanceKm);
        if (nearestFault == null)
            return 1.0;
        
        // Simplified near-fault factor (NZS 1170.5 Section 3.1.6)
        return nearestFault.DistanceKm switch
        {
            < 2 => 1.5,
            < 5 => 1.3,
            < 10 => 1.1,
            _ => 1.0
        };
    }
}
```

### Response Models

```csharp
// Models/SeismicHazard.cs

public class SeismicHazard
{
    /// <summary>NZS 1170.5 Zone Factor (Z)</summary>
    public double ZoneFactor { get; set; }
    
    /// <summary>Site Class (A-E per NZS 1170.5)</summary>
    public string SiteClass { get; set; } = "C";
    
    /// <summary>Near-fault factor (N)</summary>
    public double NearFaultFactor { get; set; } = 1.0;
    
    /// <summary>PGA for Serviceability Limit State (500-year)</summary>
    public double PgaSls { get; set; }
    
    /// <summary>PGA for Ultimate Limit State (2500-year)</summary>
    public double PgaUls { get; set; }
    
    /// <summary>Seismic zone description</summary>
    public string SeismicZone { get; set; } = string.Empty;
    
    /// <summary>Design standard reference</summary>
    public string DesignStandard { get; set; } = "NZS 1170.5:2004";
    
    /// <summary>Nearby active faults</summary>
    public List<ActiveFault> NearbyFaults { get; set; } = new();
}

public class ActiveFault
{
    public string Name { get; set; } = string.Empty;
    public string FaultId { get; set; } = string.Empty;
    public double DistanceKm { get; set; }
    public double SlipRateMmPerYear { get; set; }
    public int? RecurrenceIntervalYears { get; set; }
    public int? LastRuptureYear { get; set; }
    public string FaultType { get; set; } = string.Empty;
    public double? MaxMagnitude { get; set; }
}

public class HistoricalQuake
{
    public string Id { get; set; } = string.Empty;
    public DateTime Time { get; set; }
    public double Magnitude { get; set; }
    public double Depth { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Locality { get; set; }
    public int? MMI { get; set; }
}
```

### GeoNet API Response Model

```csharp
public class GeoNetQuakeResponse
{
    public string Type { get; set; } = "FeatureCollection";
    public List<GeoNetQuakeFeature>? Features { get; set; }
}

public class GeoNetQuakeFeature
{
    public GeoNetGeometry Geometry { get; set; } = new();
    public GeoNetQuakeProperties Properties { get; set; } = new();
}

public class GeoNetGeometry
{
    public string Type { get; set; } = "Point";
    public double[] Coordinates { get; set; } = new double[2];
}

public class GeoNetQuakeProperties
{
    public string PublicID { get; set; } = string.Empty;
    public DateTime Time { get; set; }
    public double Magnitude { get; set; }
    public double Depth { get; set; }
    public string? Locality { get; set; }
    public int? MMI { get; set; }
}
```

---

## ?? NZS 1170.5 Z-Values Reference

| Location | Z-Value | Seismic Zone |
|----------|---------|--------------|
| Auckland | 0.13 | Low |
| Hamilton | 0.13 | Low |
| Tauranga | 0.19 | Medium |
| Gisborne | 0.36 | High |
| Napier | 0.39 | High |
| Wellington | 0.40 | High |
| Nelson | 0.27 | Medium |
| Christchurch | 0.30 | Medium-High |
| Queenstown | 0.30 | Medium-High |
| Dunedin | 0.13 | Low |

---

## ?? Important Notes

1. **Z-values are interpolated** - The actual Z-factor should be interpolated from the NZS 1170.5 maps, not taken from city-specific values.

2. **Site Class requires investigation** - Site Class (A-E) should be determined from geotechnical investigation, not assumed.

3. **Near-fault factors** - Apply within 20km of active faults for magnitude ? 7.0 events.

4. **Updated NSHM 2022** - A new National Seismic Hazard Model was released in 2022 and may supersede some NZS 1170.5 values.

---

## ? Implementation Checklist

- [x] Interface definition
- [ ] GeoNet quake history API
- [ ] Active Faults Database integration
- [ ] Z-value lookup/interpolation
- [ ] Near-fault factor calculation
- [ ] Site class guidance
- [ ] Historical quake display
- [ ] Unit tests
- [ ] Integration tests

---

## ?? Related

- [IntegrationInterfaces.cs](../../Services/Integration/IntegrationInterfaces.cs)
- [SeismicHazard Model](../../Models/HazardData.cs)
- [NZS 1170.5:2004](https://www.standards.govt.nz/)
