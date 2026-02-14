# Council GIS APIs Implementation Guide

**Service**: Local Council GIS Portals  
**Status**: ?? Partially Implemented (CCC 70%, others stub)  
**Implementation File**: `Services/Integration/CouncilServices/`

---

## ?? Overview

Each New Zealand council operates their own GIS (Geographic Information System) portal with planning, hazard, and infrastructure data. The Site Evaluator needs to integrate with multiple councils to provide nationwide coverage.

---

## ??? Council Coverage Status

| Council | Region | Population | Status | Priority |
|---------|--------|------------|--------|----------|
| **Christchurch City** | Canterbury | 380,000 | ? 70% | High |
| **Auckland Council** | Auckland | 1,700,000 | ? Stub | High |
| **Wellington City** | Wellington | 215,000 | ? Stub | High |
| **Hamilton City** | Waikato | 170,000 | ? Not started | Medium |
| **Tauranga City** | Bay of Plenty | 155,000 | ? Not started | Medium |
| **Selwyn District** | Canterbury | 72,000 | ? Not started | Medium |
| **Waimakariri District** | Canterbury | 65,000 | ? Not started | Medium |
| **Dunedin City** | Otago | 135,000 | ? Not started | Low |

---

## ?? GIS Portal URLs

| Council | GIS Portal | API Type |
|---------|-----------|----------|
| **Christchurch** | https://ccc.govt.nz/city-maps | ArcGIS REST |
| **Auckland** | https://geomapspublic.aucklandcouncil.govt.nz/ | ArcGIS REST |
| **Wellington** | https://wellington.govt.nz/your-council/maps | ArcGIS REST |
| **Hamilton** | https://www.hamilton.govt.nz/our-city/gis-mapping | ArcGIS REST |
| **Environment Canterbury** | https://ecan.govt.nz/data/geospatial-data | WFS/WMS |

---

## ?? Common API Patterns

### ArcGIS REST API Pattern

Most councils use Esri ArcGIS server. Common endpoints:

```
Base URL: https://{council}.maps.arcgis.com/arcgis/rest/services/

Query Pattern:
  /MapServer/{layerId}/query?
    where=1=1&
    geometry={lon},{lat}&
    geometryType=esriGeometryPoint&
    spatialRel=esriSpatialRelIntersects&
    outFields=*&
    returnGeometry=true&
    f=json
```

### WFS (Web Feature Service) Pattern

Some councils use OGC WFS:

```
Base URL: https://{council}.govt.nz/services/wfs?

Query Pattern:
  service=WFS&
  version=2.0.0&
  request=GetFeature&
  typeName={layerName}&
  CQL_FILTER=INTERSECTS(geometry, POINT({lon} {lat}))&
  outputFormat=application/json
```

---

# ??? Christchurch City Council (CCC)

## GIS Portal

| Resource | URL |
|----------|-----|
| **City Maps** | https://ccc.govt.nz/city-maps |
| **ArcGIS Server** | https://opendata.canterburymaps.govt.nz/arcgis/rest/services |
| **Open Data** | https://opendata.canterburymaps.govt.nz/ |
| **District Plan** | https://ccc.govt.nz/district-plan |

## Layer IDs

| Data | Layer ID | Service |
|------|----------|---------|
| **Zoning** | 0 | PlanningZones |
| **Flood Management** | 0 | FloodManagement |
| **Liquefaction** | 0 | LiquefactionSusceptibility |
| **Lateral Spread** | 0 | LateralSpread |
| **Infrastructure Overlay** | 0 | InfrastructureBoundaries |

## Example API Calls

### Zoning Data

```http
GET https://opendata.canterburymaps.govt.nz/arcgis/rest/services/
    PlanningZones/MapServer/0/query?
    geometry=172.6362,-43.5320&
    geometryType=esriGeometryPoint&
    spatialRel=esriSpatialRelIntersects&
    outFields=*&
    f=json
```

**Response**:
```json
{
  "features": [
    {
      "attributes": {
        "OBJECTID": 12345,
        "Zone": "Commercial Core Zone",
        "ZoneCode": "CCZ",
        "MaxHeight": 90,
        "MaxCoverage": 100,
        "MinSetback": 0,
        "DistrictPlanUrl": "https://..."
      },
      "geometry": { "rings": [...] }
    }
  ]
}
```

### Flood Zone Data

```http
GET https://opendata.canterburymaps.govt.nz/arcgis/rest/services/
    FloodManagement/MapServer/0/query?
    geometry=172.6362,-43.5320&
    geometryType=esriGeometryPoint&
    spatialRel=esriSpatialRelIntersects&
    outFields=*&
    f=json
```

**Response**:
```json
{
  "features": [
    {
      "attributes": {
        "FloodZone": "FMAZ",
        "Description": "Flood Management Area Zone",
        "FloorLevel": "Check site-specific requirements",
        "AEP": "1%"
      }
    }
  ]
}
```

### Liquefaction Data

```http
GET https://opendata.canterburymaps.govt.nz/arcgis/rest/services/
    LiquefactionSusceptibility/MapServer/0/query?
    geometry=172.6362,-43.5320&
    geometryType=esriGeometryPoint&
    spatialRel=esriSpatialRelIntersects&
    outFields=*&
    f=json
```

**Response**:
```json
{
  "features": [
    {
      "attributes": {
        "LiqCategory": "TC2",
        "Description": "Technical Category 2",
        "Susceptibility": "Liquefaction damage is unlikely",
        "FoundationRequirements": "Standard TC2 foundations"
      }
    }
  ]
}
```

## Implementation

```csharp
// Services/Integration/CouncilServices/ChristchurchCouncilService.cs

public class ChristchurchCouncilService : ICouncilDataService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ChristchurchCouncilService> _logger;
    
    private const string BaseUrl = "https://opendata.canterburymaps.govt.nz/arcgis/rest/services";
    
    public string CouncilName => "Christchurch City";
    
    public bool SupportsRegion(double lat, double lon)
    {
        // Canterbury region bounds
        return lat >= -44.0 && lat <= -43.0 
            && lon >= 171.5 && lon <= 173.5;
    }
    
    public async Task<ZoningData?> GetZoningDataAsync(
        double lat, double lon, CancellationToken ct = default)
    {
        try
        {
            var url = $"{BaseUrl}/PlanningZones/MapServer/0/query?" +
                $"geometry={lon},{lat}&" +
                "geometryType=esriGeometryPoint&" +
                "spatialRel=esriSpatialRelIntersects&" +
                "outFields=*&f=json";
            
            var response = await _httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return null;
            
            var result = await response.Content
                .ReadFromJsonAsync<ArcGisQueryResponse>(ct);
            
            var feature = result?.Features?.FirstOrDefault();
            if (feature == null)
                return null;
            
            return new ZoningData
            {
                ZoneName = feature.Attributes.GetValueOrDefault("Zone")?.ToString(),
                ZoneCode = feature.Attributes.GetValueOrDefault("ZoneCode")?.ToString(),
                MaxHeight = ParseDouble(feature.Attributes, "MaxHeight"),
                MaxCoverage = ParseDouble(feature.Attributes, "MaxCoverage"),
                MinSetback = ParseDouble(feature.Attributes, "MinSetback")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting CCC zoning data");
            return null;
        }
    }
    
    public async Task<HazardData?> GetHazardDataAsync(
        double lat, double lon, CancellationToken ct = default)
    {
        var hazardData = new HazardData();
        
        // Get flood zone
        var floodTask = GetFloodZoneAsync(lat, lon, ct);
        var liqTask = GetLiquefactionAsync(lat, lon, ct);
        var lateralTask = GetLateralSpreadAsync(lat, lon, ct);
        
        await Task.WhenAll(floodTask, liqTask, lateralTask);
        
        hazardData.FloodZone = await floodTask;
        hazardData.LiquefactionRisk = await liqTask;
        hazardData.LateralSpreadRisk = await lateralTask;
        
        return hazardData;
    }
    
    private async Task<FloodZoneInfo?> GetFloodZoneAsync(
        double lat, double lon, CancellationToken ct)
    {
        var url = $"{BaseUrl}/FloodManagement/MapServer/0/query?" +
            $"geometry={lon},{lat}&" +
            "geometryType=esriGeometryPoint&" +
            "spatialRel=esriSpatialRelIntersects&" +
            "outFields=*&f=json";
        
        var response = await _httpClient.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
            return null;
        
        var result = await response.Content
            .ReadFromJsonAsync<ArcGisQueryResponse>(ct);
        
        var feature = result?.Features?.FirstOrDefault();
        if (feature == null)
            return new FloodZoneInfo { Zone = "Outside Flood Management Area" };
        
        return new FloodZoneInfo
        {
            Zone = feature.Attributes.GetValueOrDefault("FloodZone")?.ToString(),
            Description = feature.Attributes.GetValueOrDefault("Description")?.ToString(),
            AnnualExceedanceProbability = feature.Attributes.GetValueOrDefault("AEP")?.ToString()
        };
    }
    
    private async Task<LiquefactionRisk?> GetLiquefactionAsync(
        double lat, double lon, CancellationToken ct)
    {
        var url = $"{BaseUrl}/LiquefactionSusceptibility/MapServer/0/query?" +
            $"geometry={lon},{lat}&" +
            "geometryType=esriGeometryPoint&" +
            "spatialRel=esriSpatialRelIntersects&" +
            "outFields=*&f=json";
        
        var response = await _httpClient.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
            return null;
        
        var result = await response.Content
            .ReadFromJsonAsync<ArcGisQueryResponse>(ct);
        
        var feature = result?.Features?.FirstOrDefault();
        if (feature == null)
            return null;
        
        var category = feature.Attributes.GetValueOrDefault("LiqCategory")?.ToString();
        
        return new LiquefactionRisk
        {
            Category = category,
            Description = feature.Attributes.GetValueOrDefault("Description")?.ToString(),
            RiskLevel = MapTcToRiskLevel(category),
            FoundationRequirements = feature.Attributes.GetValueOrDefault("FoundationRequirements")?.ToString()
        };
    }
    
    private string MapTcToRiskLevel(string? tcCategory) => tcCategory switch
    {
        "TC1" => "Low",
        "TC2" => "Low-Medium",
        "TC3" => "Medium-High",
        _ => "Unknown"
    };
}
```

---

# ??? Auckland Council

## GIS Portal

| Resource | URL |
|----------|-----|
| **GeoMaps Public** | https://geomapspublic.aucklandcouncil.govt.nz/ |
| **ArcGIS Server** | https://services2.arcgis.com/JnqnZOOYLNJIPLTI/arcgis/rest/services |
| **Open Data** | https://data-aucklandcouncil.opendata.arcgis.com/ |

## Key Layers

| Data | Layer Name | Notes |
|------|------------|-------|
| **Unitary Plan Zones** | Auckland_Unitary_Plan_Operative_Zones | Main zoning layer |
| **Flood Plains** | Floodplains | 100-year flood extents |
| **Coastal Hazards** | CoastalErosion | Erosion and inundation |
| **Special Housing Areas** | SHA_Locations | Development areas |

## Example API Call

```http
GET https://services2.arcgis.com/JnqnZOOYLNJIPLTI/arcgis/rest/services/
    Auckland_Unitary_Plan_Operative_Zones/FeatureServer/0/query?
    geometry=174.7633,-36.8485&
    geometryType=esriGeometryPoint&
    spatialRel=esriSpatialRelIntersects&
    outFields=*&
    f=json
```

## Implementation Stub

```csharp
// Services/Integration/CouncilServices/AucklandCouncilService.cs

public class AucklandCouncilService : ICouncilDataService
{
    public string CouncilName => "Auckland Council";
    
    public bool SupportsRegion(double lat, double lon)
    {
        // Auckland region bounds
        return lat >= -37.5 && lat <= -36.0 
            && lon >= 174.0 && lon <= 176.0;
    }
    
    public async Task<ZoningData?> GetZoningDataAsync(
        double lat, double lon, CancellationToken ct = default)
    {
        // TODO: Implement Auckland zoning lookup
        throw new NotImplementedException("Auckland zoning lookup not yet implemented");
    }
    
    public async Task<HazardData?> GetHazardDataAsync(
        double lat, double lon, CancellationToken ct = default)
    {
        // TODO: Implement Auckland hazard lookup
        throw new NotImplementedException("Auckland hazard lookup not yet implemented");
    }
    
    public async Task<InfrastructureData?> GetInfrastructureDataAsync(
        double lat, double lon, CancellationToken ct = default)
    {
        // TODO: Implement Auckland infrastructure lookup
        throw new NotImplementedException("Auckland infrastructure lookup not yet implemented");
    }
}
```

---

# ??? Wellington City Council

## GIS Portal

| Resource | URL |
|----------|-----|
| **Council Maps** | https://wellington.govt.nz/your-council/maps |
| **WCC GIS** | https://gis.wcc.govt.nz/ |

## Key Layers

| Data | Layer | Notes |
|------|-------|-------|
| **District Plan Zones** | DistrictPlanZoning | Current operative plan |
| **Fault Lines** | WellingtonFault | Proximity to active faults |
| **Tsunami Zones** | TsunamiEvacuationZones | Inundation risk |
| **Wind Zones** | WindDesignZones | High wind areas |

---

## ?? Common Response Model

```csharp
// Shared ArcGIS response models

public class ArcGisQueryResponse
{
    public List<ArcGisFeature>? Features { get; set; }
}

public class ArcGisFeature
{
    public Dictionary<string, object?> Attributes { get; set; } = new();
    public ArcGisGeometry? Geometry { get; set; }
}

public class ArcGisGeometry
{
    public List<List<double[]>>? Rings { get; set; }  // For polygons
    public double? X { get; set; }  // For points
    public double? Y { get; set; }
}
```

---

## ?? Service Factory Pattern

```csharp
// Services/Integration/CouncilServiceFactory.cs

public class CouncilServiceFactory
{
    private readonly IEnumerable<ICouncilDataService> _councilServices;
    
    public CouncilServiceFactory(IEnumerable<ICouncilDataService> councilServices)
    {
        _councilServices = councilServices;
    }
    
    public ICouncilDataService? GetServiceForLocation(double lat, double lon)
    {
        return _councilServices.FirstOrDefault(s => s.SupportsRegion(lat, lon));
    }
    
    public IEnumerable<string> GetSupportedCouncils()
    {
        return _councilServices.Select(s => s.CouncilName);
    }
}
```

---

## ? Implementation Checklist

### Christchurch (CCC)
- [x] Zoning lookup
- [x] Flood zone lookup
- [x] Liquefaction lookup
- [x] Lateral spread lookup
- [ ] Infrastructure lookup
- [ ] Full response parsing
- [ ] Unit tests

### Auckland
- [ ] Zoning lookup
- [ ] Flood zone lookup
- [ ] Coastal hazards
- [ ] Infrastructure lookup
- [ ] Unit tests

### Wellington
- [ ] Zoning lookup
- [ ] Fault proximity
- [ ] Tsunami zones
- [ ] Wind zones
- [ ] Unit tests

---

## ?? Related

- [ICouncilDataService Interface](../../Services/Integration/IntegrationInterfaces.cs)
- [ChristchurchCouncilService](../../Services/Integration/ChristchurchCouncilService.cs)
- [ZoningData Model](../../Models/ZoningData.cs)
- [HazardData Model](../../Models/HazardData.cs)
