# LINZ Address API Implementation Guide

**Service**: LINZ Data Service - Address Geocoding  
**Status**: ? Implemented (80%)  
**Implementation File**: `Services/Integration/LinzDataService.cs`

---

## ?? Overview

The LINZ (Land Information New Zealand) Data Service provides address geocoding and autocomplete functionality for New Zealand addresses. This is the primary address lookup service for Site Evaluator.

---

## ?? Official Documentation

| Resource | URL |
|----------|-----|
| **LINZ Data Service** | https://data.linz.govt.nz/ |
| **API Documentation** | https://data.linz.govt.nz/services/api/ |
| **Address Layer** | https://data.linz.govt.nz/layer/53353-nz-addresses/ |
| **Coordinate Reference System** | NZGD2000 (EPSG:4167) or WGS84 (EPSG:4326) |

---

## ?? Authentication

### Getting an API Key

1. Create an account at https://data.linz.govt.nz/
2. Navigate to **My Account** ? **API Keys**
3. Click **Generate Key**
4. Copy the key and store securely

### Configuration

```json
// appsettings.json
{
  "SiteEvaluator": {
    "Linz": {
      "BaseUrl": "https://data.linz.govt.nz",
      "ApiKey": "your-api-key-here"
    }
  }
}
```

### Request Headers

```http
Authorization: key {your-api-key}
```

---

## ?? API Endpoints

### 1. Address Geocoding

Converts a text address to coordinates.

**Endpoint**: `GET /services/api/v1/geocode`

**Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `q` | string | Yes | Address search query |
| `count` | integer | No | Max results (default: 10) |

**Example Request**:
```http
GET https://data.linz.govt.nz/services/api/v1/geocode?q=123+Main+Street+Christchurch&count=5
Authorization: key {api-key}
```

**Example Response**:
```json
{
  "results": [
    {
      "fullAddress": "123 Main Street, Christchurch Central, Christchurch 8011",
      "latitude": -43.5320,
      "longitude": 172.6362,
      "suburb": "Christchurch Central",
      "city": "Christchurch",
      "territorialAuthority": "Christchurch City",
      "legalDescription": "Lot 1 DP 12345"
    }
  ]
}
```

### 2. Parcel Boundaries (WFS)

Retrieves parcel geometry for display on maps.

**Endpoint**: `GET /services/api/v1/wfs`

**Parameters**:
| Parameter | Value |
|-----------|-------|
| `service` | WFS |
| `version` | 2.0.0 |
| `request` | GetFeature |
| `typeName` | layer-51571 |
| `outputFormat` | application/json |
| `CQL_FILTER` | Filter expression |

**Example Request**:
```http
GET https://data.linz.govt.nz/services/api/v1/wfs?
  service=WFS&
  version=2.0.0&
  request=GetFeature&
  typeName=layer-51571&
  CQL_FILTER=id='12345'&
  outputFormat=application/json
Authorization: key {api-key}
```

---

## ?? Implementation

### Current Service Implementation

```csharp
// Services/Integration/LinzDataService.cs

public class LinzDataService : ILinzDataService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LinzDataService> _logger;

    public async Task<SiteLocation?> LookupAddressAsync(string address, CancellationToken ct = default)
    {
        var encodedAddress = Uri.EscapeDataString(address);
        var response = await _httpClient.GetAsync(
            $"/services/api/v1/geocode?q={encodedAddress}", ct);
        
        if (!response.IsSuccessStatusCode)
            return null;
            
        var result = await response.Content.ReadFromJsonAsync<LinzGeocodeResponse>(ct);
        var first = result?.Results?.FirstOrDefault();
        
        if (first == null) return null;

        return new SiteLocation
        {
            Address = first.FullAddress ?? address,
            Latitude = first.Latitude ?? 0,
            Longitude = first.Longitude ?? 0,
            LegalDescription = first.LegalDescription ?? "",
            TerritorialAuthority = first.TerritorialAuthority,
            Suburb = first.Suburb,
            City = first.City
        };
    }

    public async Task<List<AddressSuggestion>> GetAddressSuggestionsAsync(
        string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 3)
            return new List<AddressSuggestion>();

        var encodedQuery = Uri.EscapeDataString(query);
        var response = await _httpClient.GetAsync(
            $"/services/api/v1/geocode?q={encodedQuery}&count=10", ct);
        
        if (!response.IsSuccessStatusCode)
            return new List<AddressSuggestion>();
            
        var result = await response.Content.ReadFromJsonAsync<LinzGeocodeResponse>(ct);
        
        return result?.Results?.Select(r => new AddressSuggestion
        {
            FullAddress = r.FullAddress ?? "",
            Suburb = r.Suburb,
            City = r.City,
            Latitude = r.Latitude,
            Longitude = r.Longitude
        }).ToList() ?? new List<AddressSuggestion>();
    }
}
```

### Response Models

```csharp
public class LinzGeocodeResponse
{
    public List<LinzGeocodeResult>? Results { get; set; }
}

public class LinzGeocodeResult
{
    public string? FullAddress { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? LegalDescription { get; set; }
    public string? TerritorialAuthority { get; set; }
    public string? Suburb { get; set; }
    public string? City { get; set; }
}

public class AddressSuggestion
{
    public string FullAddress { get; set; } = string.Empty;
    public string? Suburb { get; set; }
    public string? City { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
```

---

## ?? Rate Limits & Quotas

| Tier | Requests/Day | Requests/Second |
|------|-------------|-----------------|
| Free | 10,000 | 10 |
| Registered | 100,000 | 50 |

**Best Practices**:
- Implement caching for repeated lookups
- Use debouncing (300ms) for autocomplete
- Cache successful results for 24 hours

---

## ?? Testing

### Manual Testing

```bash
# Test address lookup
curl -H "Authorization: key YOUR_API_KEY" \
  "https://data.linz.govt.nz/services/api/v1/geocode?q=90+Armagh+Street+Christchurch"
```

### Unit Test Example

```csharp
[Fact]
public async Task LookupAddressAsync_ValidAddress_ReturnsLocation()
{
    // Arrange
    var mockHandler = new MockHttpMessageHandler();
    mockHandler.When("*/geocode*")
        .Respond("application/json", @"{
            ""results"": [{
                ""fullAddress"": ""90 Armagh Street, Christchurch"",
                ""latitude"": -43.5301,
                ""longitude"": 172.6353
            }]
        }");
    
    var httpClient = mockHandler.ToHttpClient();
    httpClient.BaseAddress = new Uri("https://data.linz.govt.nz");
    
    var service = new LinzDataService(httpClient, Mock.Of<ILogger<LinzDataService>>());
    
    // Act
    var result = await service.LookupAddressAsync("90 Armagh Street Christchurch");
    
    // Assert
    Assert.NotNull(result);
    Assert.Contains("Armagh", result.Address);
    Assert.InRange(result.Latitude, -44, -43);
}
```

---

## ? Implementation Checklist

- [x] Basic address geocoding
- [x] Address autocomplete suggestions
- [x] Response model mapping
- [x] Error handling
- [x] Logging
- [ ] Caching layer
- [ ] Rate limit handling
- [ ] Parcel boundary retrieval
- [ ] Unit tests
- [ ] Integration tests

---

## ?? Related

- [IntegrationInterfaces.cs](../../Services/Integration/IntegrationInterfaces.cs)
- [LinzDataService.cs](../../Services/Integration/LinzDataService.cs)
- [LINZ Landonline Guide](LINZ-Landonline-API.md)
