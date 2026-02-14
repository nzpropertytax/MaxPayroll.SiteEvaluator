# NZGD (New Zealand Geotechnical Database) API Implementation Guide

**Service**: NZGD - New Zealand Geotechnical Database  
**Status**: ?? Partially Implemented (30%)  
**Implementation File**: `Services/Integration/NzgdDataService.cs`

---

## ?? Overview

The New Zealand Geotechnical Database (NZGD) contains geotechnical investigation data including boreholes, CPT (Cone Penetration Test) results, and geotechnical reports. This data is critical for site evaluation, especially in Canterbury post-earthquake.

---

## ?? Official Documentation

| Resource | URL |
|----------|-----|
| **NZGD Website** | https://www.nzgd.org.nz/ |
| **Data Portal** | https://www.nzgd.org.nz/data-portal |
| **API Documentation** | https://www.nzgd.org.nz/api/docs (requires login) |
| **Canterbury NZGD** | https://canterbury.nzgd.org.nz/ |
| **Data Standards** | https://www.nzgd.org.nz/Standards |

---

## ?? Authentication

### Registration

1. Visit https://www.nzgd.org.nz/register
2. Complete registration form (organization required)
3. Wait for approval (may take 1-2 business days)
4. Receive API credentials via email

### Configuration

```json
// appsettings.json
{
  "SiteEvaluator": {
    "Nzgd": {
      "BaseUrl": "https://www.nzgd.org.nz/api/v1",
      "ApiKey": "your-api-key",
      "Username": "your-username"
    }
  }
}
```

### Request Headers

```http
Authorization: Bearer {access-token}
X-API-Key: {your-api-key}
Content-Type: application/json
```

---

## ?? API Endpoints

### 1. Search Boreholes by Location

Returns boreholes within a radius of a point.

**Endpoint**: `GET /boreholes/search`

**Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `lat` | double | Yes | Latitude (WGS84) |
| `lon` | double | Yes | Longitude (WGS84) |
| `radius` | double | No | Search radius in meters (default: 500) |
| `limit` | integer | No | Max results (default: 50) |

**Example Request**:
```http
GET https://www.nzgd.org.nz/api/v1/boreholes/search?lat=-43.532&lon=172.636&radius=500
Authorization: Bearer {token}
```

**Example Response**:
```json
{
  "count": 15,
  "boreholes": [
    {
      "id": "BH-12345",
      "name": "BH-1 Site Investigation",
      "location": {
        "lat": -43.5318,
        "lon": 172.6365,
        "nztmX": 1570350,
        "nztmY": 5180200
      },
      "depth": 15.5,
      "date": "2018-06-15",
      "project": "Commercial Development",
      "distanceMeters": 45,
      "hasLogData": true,
      "hasLabResults": true,
      "dataUrl": "/boreholes/BH-12345/data"
    }
  ]
}
```

### 2. Get Borehole Details

Returns detailed borehole log data including soil layers.

**Endpoint**: `GET /boreholes/{id}`

**Example Response**:
```json
{
  "id": "BH-12345",
  "name": "BH-1 Site Investigation",
  "location": {
    "lat": -43.5318,
    "lon": 172.6365
  },
  "groundLevel": 5.2,
  "totalDepth": 15.5,
  "waterTable": 2.1,
  "drillDate": "2018-06-15",
  "drillMethod": "Hollow Stem Auger",
  "contractor": "Geotechnical Ltd",
  "layers": [
    {
      "topDepth": 0.0,
      "bottomDepth": 0.3,
      "description": "TOPSOIL - dark brown organic silt",
      "uscsCode": "OH",
      "consistency": null,
      "sptN": null
    },
    {
      "topDepth": 0.3,
      "bottomDepth": 2.5,
      "description": "SILT - grey, soft to firm",
      "uscsCode": "ML",
      "consistency": "soft to firm",
      "sptN": 4
    },
    {
      "topDepth": 2.5,
      "bottomDepth": 8.0,
      "description": "SAND - grey, fine to medium, loose to medium dense",
      "uscsCode": "SP",
      "consistency": "loose to medium dense",
      "sptN": 12
    },
    {
      "topDepth": 8.0,
      "bottomDepth": 15.5,
      "description": "GRAVEL - grey, sandy, dense",
      "uscsCode": "GP",
      "consistency": "dense",
      "sptN": 35
    }
  ],
  "labResults": [
    {
      "depth": 1.5,
      "testType": "Atterberg Limits",
      "liquidLimit": 38,
      "plasticLimit": 22,
      "plasticityIndex": 16
    }
  ]
}
```

### 3. Search CPTs by Location

Returns Cone Penetration Test data near a location.

**Endpoint**: `GET /cpts/search`

**Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `lat` | double | Yes | Latitude (WGS84) |
| `lon` | double | Yes | Longitude (WGS84) |
| `radius` | double | No | Search radius in meters |

**Example Response**:
```json
{
  "count": 8,
  "cpts": [
    {
      "id": "CPT-5678",
      "name": "CPT-1",
      "location": {
        "lat": -43.5315,
        "lon": 172.6370
      },
      "depth": 12.0,
      "date": "2019-03-22",
      "distanceMeters": 78,
      "hasFullData": true
    }
  ]
}
```

### 4. Get CPT Data

Returns CPT cone resistance and friction data.

**Endpoint**: `GET /cpts/{id}`

**Example Response**:
```json
{
  "id": "CPT-5678",
  "name": "CPT-1",
  "location": {
    "lat": -43.5315,
    "lon": 172.6370
  },
  "groundLevel": 5.1,
  "waterTable": 2.0,
  "maxDepth": 12.0,
  "readings": [
    {
      "depth": 0.0,
      "qc": 0.5,
      "fs": 0.02,
      "u2": 0,
      "Rf": 4.0,
      "soilType": "Organic soil"
    },
    {
      "depth": 1.0,
      "qc": 2.5,
      "fs": 0.08,
      "u2": 15,
      "Rf": 3.2,
      "soilType": "Clay"
    },
    {
      "depth": 2.0,
      "qc": 8.0,
      "fs": 0.12,
      "u2": 5,
      "Rf": 1.5,
      "soilType": "Silty sand"
    }
  ],
  "liquefactionAssessment": {
    "susceptible": true,
    "criticalDepths": [2.0, 6.5],
    "fsLiquefaction": [0.6, 0.8, 1.2, 1.5]
  }
}
```

### 5. Search Geotechnical Reports

**Endpoint**: `GET /reports/search`

**Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `lat` | double | Yes | Latitude |
| `lon` | double | Yes | Longitude |
| `radius` | double | No | Search radius in meters |
| `type` | string | No | Report type filter |

**Example Response**:
```json
{
  "count": 5,
  "reports": [
    {
      "id": "RPT-001",
      "title": "Geotechnical Investigation Report - 123 Main St",
      "author": "Geotechnical Consultants Ltd",
      "date": "2020-08-15",
      "type": "Site Investigation",
      "distanceMeters": 120,
      "downloadUrl": "/reports/RPT-001/download",
      "summary": "Site investigation for proposed commercial building..."
    }
  ]
}
```

---

## ?? Implementation

### Interface Definition

```csharp
// Services/Integration/IntegrationInterfaces.cs

public interface INzgdDataService
{
    Task<List<NearbyBorehole>> GetNearbyBoreholesAsync(
        double lat, double lon, double radiusMeters, CancellationToken ct = default);
    
    Task<List<NearbyCpt>> GetNearbyCptsAsync(
        double lat, double lon, double radiusMeters, CancellationToken ct = default);
    
    Task<List<NearbyGeotechReport>> GetNearbyReportsAsync(
        double lat, double lon, double radiusMeters, CancellationToken ct = default);
    
    Task<BoreholeDetail?> GetBoreholeDetailAsync(
        string boreholeId, CancellationToken ct = default);
    
    Task<CptDetail?> GetCptDetailAsync(
        string cptId, CancellationToken ct = default);
}
```

### Proposed Implementation

```csharp
// Services/Integration/NzgdDataService.cs

public class NzgdDataService : INzgdDataService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NzgdDataService> _logger;

    public NzgdDataService(HttpClient httpClient, ILogger<NzgdDataService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<NearbyBorehole>> GetNearbyBoreholesAsync(
        double lat, double lon, double radiusMeters, CancellationToken ct = default)
    {
        try
        {
            var url = $"/boreholes/search?lat={lat}&lon={lon}&radius={radiusMeters}";
            var response = await _httpClient.GetAsync(url, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("NZGD borehole search failed: {StatusCode}", 
                    response.StatusCode);
                return new List<NearbyBorehole>();
            }
            
            var result = await response.Content
                .ReadFromJsonAsync<NzgdBoreholeSearchResponse>(ct);
            
            return result?.Boreholes?.Select(b => new NearbyBorehole
            {
                Id = b.Id,
                Name = b.Name,
                Latitude = b.Location.Lat,
                Longitude = b.Location.Lon,
                DepthMeters = b.Depth,
                Date = b.Date,
                DistanceMeters = b.DistanceMeters,
                HasLogData = b.HasLogData
            }).ToList() ?? new List<NearbyBorehole>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching NZGD boreholes");
            return new List<NearbyBorehole>();
        }
    }

    public async Task<List<NearbyCpt>> GetNearbyCptsAsync(
        double lat, double lon, double radiusMeters, CancellationToken ct = default)
    {
        try
        {
            var url = $"/cpts/search?lat={lat}&lon={lon}&radius={radiusMeters}";
            var response = await _httpClient.GetAsync(url, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("NZGD CPT search failed: {StatusCode}", 
                    response.StatusCode);
                return new List<NearbyCpt>();
            }
            
            var result = await response.Content
                .ReadFromJsonAsync<NzgdCptSearchResponse>(ct);
            
            return result?.Cpts?.Select(c => new NearbyCpt
            {
                Id = c.Id,
                Name = c.Name,
                Latitude = c.Location.Lat,
                Longitude = c.Location.Lon,
                DepthMeters = c.Depth,
                Date = c.Date,
                DistanceMeters = c.DistanceMeters
            }).ToList() ?? new List<NearbyCpt>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching NZGD CPTs");
            return new List<NearbyCpt>();
        }
    }

    public async Task<List<NearbyGeotechReport>> GetNearbyReportsAsync(
        double lat, double lon, double radiusMeters, CancellationToken ct = default)
    {
        try
        {
            var url = $"/reports/search?lat={lat}&lon={lon}&radius={radiusMeters}";
            var response = await _httpClient.GetAsync(url, ct);
            
            if (!response.IsSuccessStatusCode)
                return new List<NearbyGeotechReport>();
            
            var result = await response.Content
                .ReadFromJsonAsync<NzgdReportSearchResponse>(ct);
            
            return result?.Reports?.Select(r => new NearbyGeotechReport
            {
                Id = r.Id,
                Title = r.Title,
                Author = r.Author,
                Date = r.Date,
                DistanceMeters = r.DistanceMeters,
                DownloadUrl = r.DownloadUrl
            }).ToList() ?? new List<NearbyGeotechReport>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching NZGD reports");
            return new List<NearbyGeotechReport>();
        }
    }
}
```

### Response Models

```csharp
// Models for NZGD API responses

public class NzgdBoreholeSearchResponse
{
    public int Count { get; set; }
    public List<NzgdBorehole>? Boreholes { get; set; }
}

public class NzgdBorehole
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public NzgdLocation Location { get; set; } = new();
    public double Depth { get; set; }
    public DateTime Date { get; set; }
    public double DistanceMeters { get; set; }
    public bool HasLogData { get; set; }
    public bool HasLabResults { get; set; }
}

public class NzgdLocation
{
    public double Lat { get; set; }
    public double Lon { get; set; }
    public double? NztmX { get; set; }
    public double? NztmY { get; set; }
}

public class NzgdCptSearchResponse
{
    public int Count { get; set; }
    public List<NzgdCpt>? Cpts { get; set; }
}

public class NzgdCpt
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public NzgdLocation Location { get; set; } = new();
    public double Depth { get; set; }
    public DateTime Date { get; set; }
    public double DistanceMeters { get; set; }
}

public class NzgdReportSearchResponse
{
    public int Count { get; set; }
    public List<NzgdReport>? Reports { get; set; }
}

public class NzgdReport
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public double DistanceMeters { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
}
```

---

## ?? Rate Limits & Access

| Tier | Requests/Day | Notes |
|------|-------------|-------|
| Registered | 1,000 | Standard access |
| Professional | 10,000 | Commercial use |
| Enterprise | Unlimited | Contact NZGD |

**Important Notes**:
- Registration required for API access
- Some data may be restricted to Canterbury region
- Commercial use may require professional subscription

---

## ?? Data Privacy

- Some investigation data may be embargoed
- Property owner consent may be required for some reports
- Respect data licensing terms

---

## ? Implementation Checklist

- [x] Interface definition
- [x] Basic service structure
- [ ] Borehole search endpoint
- [ ] Borehole detail endpoint
- [ ] CPT search endpoint
- [ ] CPT detail endpoint
- [ ] Report search endpoint
- [ ] Response model parsing
- [ ] Error handling
- [ ] Caching (frequently accessed data)
- [ ] Unit tests
- [ ] Integration tests

---

## ?? Related

- [IntegrationInterfaces.cs](../../Services/Integration/IntegrationInterfaces.cs)
- [GeotechnicalData Model](../../Models/GeotechnicalData.cs)
- [Main Gaps Document](../../../MaxPayroll.Website.Platform/Docs/super-admin/portfolio-business-plans/Max-Site-Evaluator-Gaps.md)
