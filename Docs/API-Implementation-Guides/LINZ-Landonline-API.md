# LINZ Landonline API Implementation Guide

**Service**: LINZ Landonline - Property Title Data  
**Status**: ? Blocked (Subscription Required)  
**Implementation File**: `Services/Integration/LinzDataService.cs`

---

## ?? Overview

LINZ Landonline is New Zealand's authoritative source for property title data, including:
- Certificate of Title information
- Ownership details (current and historical)
- Legal descriptions
- Easements, covenants, and encumbrances
- Survey plans

---

## ?? Official Documentation

| Resource | URL |
|----------|-----|
| **LINZ Landonline** | https://www.linz.govt.nz/products-services/data/linz-data-service |
| **Landonline Portal** | https://www.landonline.govt.nz/ |
| **Data Service** | https://data.linz.govt.nz/ |
| **API Documentation** | https://www.linz.govt.nz/products-services/data/linz-data-service/guides-and-documentation |
| **Property Ownership** | https://www.linz.govt.nz/products-services/property-information |

---

## ?? Subscription Requirements

### Access Tiers

| Tier | Cost | Features |
|------|------|----------|
| **Basic Search** | Free | Address/parcel search only |
| **Title View** | ~$5/title | View individual title records |
| **Bulk Access** | ~$500/year | API access, bulk downloads |
| **Enterprise** | Contact LINZ | Full Landonline integration |

### To Get API Access

1. Visit https://www.linz.govt.nz/products-services/data/types-linz-data/property-ownership-and-boundary-data
2. Apply for data subscription
3. Complete organization verification
4. Receive API credentials

---

## ?? API Endpoints

### 1. Title Search

Search for titles by address or legal description.

**Endpoint**: `GET /v1/titles/search`

**Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `address` | string | Optional | Street address |
| `legalDescription` | string | Optional | Legal description |
| `parcelId` | string | Optional | Parcel identifier |

**Example Request**:
```http
GET https://api.landonline.govt.nz/v1/titles/search?address=123+Main+Street+Christchurch
Authorization: Bearer {token}
```

**Example Response**:
```json
{
  "titles": [
    {
      "titleReference": "CB45A/123",
      "status": "Live",
      "type": "Freehold",
      "legalDescription": "Lot 1 DP 12345",
      "area": 456.0,
      "areaUnit": "sqm",
      "parcelId": "12345"
    }
  ]
}
```

### 2. Title Details

Get full details for a specific title.

**Endpoint**: `GET /v1/titles/{titleReference}`

**Example Response**:
```json
{
  "titleReference": "CB45A/123",
  "status": "Live",
  "type": "Freehold",
  "legalDescription": "Lot 1 DP 12345",
  "area": 456.0,
  "areaUnit": "sqm",
  "registrationDate": "1985-06-15",
  "owners": [
    {
      "name": "John Smith",
      "share": "1/1",
      "registrationDate": "2020-01-15"
    }
  ],
  "encumbrances": [
    {
      "type": "Mortgage",
      "mortgagee": "ANZ Bank",
      "registrationNumber": "M12345",
      "registrationDate": "2020-01-15"
    }
  ],
  "easements": [
    {
      "type": "Right of Way",
      "purpose": "Pedestrian and vehicular access",
      "benefiting": "Lot 2 DP 12345",
      "burdened": "Lot 1 DP 12345",
      "instrumentNumber": "E67890"
    }
  ],
  "covenants": [
    {
      "type": "Building Covenant",
      "description": "No building within 3m of boundary",
      "instrumentNumber": "C11111"
    }
  ],
  "memorials": [
    {
      "type": "Resource Consent Notice",
      "description": "CRC123456 - Stormwater discharge consent",
      "registrationDate": "2018-06-01"
    }
  ]
}
```

### 3. Ownership History

Get historical ownership records.

**Endpoint**: `GET /v1/titles/{titleReference}/history`

**Example Response**:
```json
{
  "titleReference": "CB45A/123",
  "transfers": [
    {
      "date": "2020-01-15",
      "from": "Jane Doe",
      "to": "John Smith",
      "consideration": "$650,000",
      "instrumentNumber": "T12345"
    },
    {
      "date": "2015-03-20",
      "from": "ABC Properties Ltd",
      "to": "Jane Doe",
      "consideration": "$480,000",
      "instrumentNumber": "T11111"
    }
  ]
}
```

### 4. Survey Plans

Get survey plan information.

**Endpoint**: `GET /v1/plans/{planNumber}`

**Example Response**:
```json
{
  "planNumber": "DP 12345",
  "type": "Deposited Plan",
  "status": "Deposited",
  "surveyDate": "1985-05-20",
  "depositDate": "1985-06-15",
  "surveyor": "Smith & Associates",
  "lots": [
    {
      "lotNumber": 1,
      "area": 456.0,
      "purpose": "Residential"
    },
    {
      "lotNumber": 2,
      "area": 523.0,
      "purpose": "Residential"
    }
  ],
  "pdfUrl": "/plans/DP12345/download"
}
```

---

## ?? Implementation

### Interface Definition

```csharp
// Services/Integration/IntegrationInterfaces.cs

public interface ILinzDataService
{
    // Address lookup (already implemented)
    Task<SiteLocation?> LookupAddressAsync(string address, CancellationToken ct = default);
    Task<List<AddressSuggestion>> GetAddressSuggestionsAsync(string query, CancellationToken ct = default);
    Task<List<Coordinate>?> GetParcelBoundaryAsync(string parcelId, CancellationToken ct = default);
    
    // Title data (requires Landonline subscription)
    Task<LandData?> GetTitleDataAsync(string titleReference, CancellationToken ct = default);
    Task<List<TitleSearchResult>> SearchTitlesAsync(string address, CancellationToken ct = default);
    Task<OwnershipHistory?> GetOwnershipHistoryAsync(string titleReference, CancellationToken ct = default);
}
```

### Proposed Implementation

```csharp
// Services/Integration/LinzDataService.cs (extended)

public async Task<LandData?> GetTitleDataAsync(string titleReference, CancellationToken ct = default)
{
    // Check if Landonline subscription is configured
    var apiKey = _configuration["SiteEvaluator:Linz:LandonlineApiKey"];
    if (string.IsNullOrEmpty(apiKey))
    {
        _logger.LogWarning("Landonline API key not configured - title lookup unavailable");
        return null;
    }

    try
    {
        var url = $"https://api.landonline.govt.nz/v1/titles/{Uri.EscapeDataString(titleReference)}";
        
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        
        var response = await _httpClient.SendAsync(request, ct);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Landonline title lookup failed: {StatusCode}", response.StatusCode);
            return null;
        }
        
        var result = await response.Content.ReadFromJsonAsync<LandonlineTitleResponse>(ct);
        if (result == null)
            return null;

        return new LandData
        {
            TitleReference = result.TitleReference,
            LegalDescription = result.LegalDescription,
            TitleType = result.Type,
            Area = result.Area,
            AreaUnit = result.AreaUnit,
            Status = result.Status,
            
            Owners = result.Owners?.Select(o => new Owner
            {
                Name = o.Name,
                Share = o.Share,
                RegistrationDate = o.RegistrationDate
            }).ToList() ?? new List<Owner>(),
            
            Encumbrances = result.Encumbrances?.Select(e => new Encumbrance
            {
                Type = e.Type,
                Description = GetEncumbranceDescription(e),
                RegistrationNumber = e.RegistrationNumber,
                RegistrationDate = e.RegistrationDate
            }).ToList() ?? new List<Encumbrance>(),
            
            Easements = result.Easements?.Select(e => new Easement
            {
                Type = e.Type,
                Purpose = e.Purpose,
                Benefiting = e.Benefiting,
                Burdened = e.Burdened
            }).ToList() ?? new List<Easement>(),
            
            Covenants = result.Covenants?.Select(c => new Covenant
            {
                Type = c.Type,
                Description = c.Description
            }).ToList() ?? new List<Covenant>(),
            
            DataSource = "LINZ Landonline",
            RetrievedDate = DateTime.UtcNow
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting title data: {TitleReference}", titleReference);
        return null;
    }
}

public async Task<List<TitleSearchResult>> SearchTitlesAsync(string address, CancellationToken ct = default)
{
    var apiKey = _configuration["SiteEvaluator:Linz:LandonlineApiKey"];
    if (string.IsNullOrEmpty(apiKey))
    {
        _logger.LogWarning("Landonline API key not configured");
        return new List<TitleSearchResult>();
    }

    try
    {
        var url = $"https://api.landonline.govt.nz/v1/titles/search?address={Uri.EscapeDataString(address)}";
        
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        
        var response = await _httpClient.SendAsync(request, ct);
        
        if (!response.IsSuccessStatusCode)
            return new List<TitleSearchResult>();
        
        var result = await response.Content.ReadFromJsonAsync<LandonlineTitleSearchResponse>(ct);
        
        return result?.Titles?.Select(t => new TitleSearchResult
        {
            TitleReference = t.TitleReference,
            LegalDescription = t.LegalDescription,
            Area = t.Area,
            Type = t.Type
        }).ToList() ?? new List<TitleSearchResult>();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error searching titles: {Address}", address);
        return new List<TitleSearchResult>();
    }
}
```

### Response Models

```csharp
// Models for Landonline API responses

public class LandonlineTitleResponse
{
    public string TitleReference { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string LegalDescription { get; set; } = string.Empty;
    public double Area { get; set; }
    public string AreaUnit { get; set; } = "sqm";
    public DateTime? RegistrationDate { get; set; }
    public List<LandonlineOwner>? Owners { get; set; }
    public List<LandonlineEncumbrance>? Encumbrances { get; set; }
    public List<LandonlineEasement>? Easements { get; set; }
    public List<LandonlineCovenant>? Covenants { get; set; }
    public List<LandonlineMemorial>? Memorials { get; set; }
}

public class LandonlineOwner
{
    public string Name { get; set; } = string.Empty;
    public string Share { get; set; } = "1/1";
    public DateTime RegistrationDate { get; set; }
}

public class LandonlineEncumbrance
{
    public string Type { get; set; } = string.Empty;
    public string? Mortgagee { get; set; }
    public string RegistrationNumber { get; set; } = string.Empty;
    public DateTime RegistrationDate { get; set; }
}

public class LandonlineEasement
{
    public string Type { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string? Benefiting { get; set; }
    public string? Burdened { get; set; }
    public string InstrumentNumber { get; set; } = string.Empty;
}

public class LandonlineCovenant
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string InstrumentNumber { get; set; } = string.Empty;
}

public class LandonlineMemorial
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime RegistrationDate { get; set; }
}
```

---

## ?? Current Blockers

| Blocker | Impact | Resolution |
|---------|--------|------------|
| **Subscription Cost** | ~$500/year | Budget approval needed |
| **API Access Approval** | 1-2 weeks | Apply to LINZ |
| **Data Licensing** | Display restrictions | Review terms |

---

## ?? Alternative Data Sources

While waiting for Landonline access, consider:

### 1. PropertyGuru / Homes.co.nz
- Basic property information
- Estimated values
- Sale history (public records)

### 2. CoreLogic NZ
- Property data API
- Commercial subscription

### 3. Council LIM Reports
- Can be ordered programmatically from some councils
- Contains title reference and basic encumbrance info

---

## ?? Configuration

```json
// appsettings.json
{
  "SiteEvaluator": {
    "Linz": {
      "BaseUrl": "https://data.linz.govt.nz",
      "ApiKey": "your-linz-api-key",
      "LandonlineApiKey": "your-landonline-api-key",
      "LandonlineBaseUrl": "https://api.landonline.govt.nz"
    }
  }
}
```

---

## ? Implementation Checklist

- [x] Interface definition
- [x] Address lookup (free tier)
- [x] Parcel boundary lookup (free tier)
- [ ] Landonline subscription setup
- [ ] Title search implementation
- [ ] Title details implementation
- [ ] Ownership history implementation
- [ ] Survey plan access
- [ ] Response model mapping
- [ ] Unit tests
- [ ] Integration tests

---

## ?? Related

- [LinzDataService.cs](../../Services/Integration/LinzDataService.cs)
- [LandData Model](../../Models/LandData.cs)
- [LINZ Address API Guide](LINZ-Address-API.md)
