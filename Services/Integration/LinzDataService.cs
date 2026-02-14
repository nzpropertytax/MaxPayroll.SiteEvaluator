using MaxPayroll.SiteEvaluator.Models;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace MaxPayroll.SiteEvaluator.Services.Integration;

/// <summary>
/// Integration with LINZ Data Service API and Landonline.
/// https://data.linz.govt.nz/
/// https://www.landonline.govt.nz/
/// </summary>
public class LinzDataService : ILinzDataService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LinzDataService> _logger;
    private readonly IConfiguration _configuration;

    public LinzDataService(HttpClient httpClient, ILogger<LinzDataService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
        
        // Set up authentication header for LINZ Data Service
        var apiKey = _configuration["SiteEvaluator:Linz:ApiKey"] ?? _configuration["Linz:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"key {apiKey}");
        }
    }

    public async Task<SiteLocation?> LookupAddressAsync(string address, CancellationToken ct = default)
    {
        try
        {
            // Use LINZ Address API
            var encodedAddress = Uri.EscapeDataString(address);
            var response = await _httpClient.GetAsync($"/services/api/v1/geocode?q={encodedAddress}", ct);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("LINZ geocode failed: {StatusCode}", response.StatusCode);
                return null;
            }
            
            var result = await response.Content.ReadFromJsonAsync<LinzGeocodeResponse>(ct);
            var firstResult = result?.Results?.FirstOrDefault();
            
            if (firstResult == null)
                return null;

            return new SiteLocation
            {
                Address = firstResult.FullAddress ?? address,
                Latitude = firstResult.Latitude ?? 0,
                Longitude = firstResult.Longitude ?? 0,
                LegalDescription = firstResult.LegalDescription ?? string.Empty,
                TerritorialAuthority = firstResult.TerritorialAuthority,
                Suburb = firstResult.Suburb,
                City = firstResult.City
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error looking up address: {Address}", address);
            return null;
        }
    }

    public async Task<LandData?> GetTitleDataAsync(string titleReference, CancellationToken ct = default)
    {
        _logger.LogInformation("Title lookup requested for: {TitleReference}", titleReference);
        
        // Check if Landonline API key is configured
        var landonlineApiKey = _configuration["SiteEvaluator:Linz:LandonlineApiKey"];
        
        if (!string.IsNullOrEmpty(landonlineApiKey))
        {
            // Try actual Landonline API
            var result = await GetTitleFromLandonlineAsync(titleReference, landonlineApiKey, ct);
            if (result != null)
                return result;
        }
        
        // Fall back to estimated data based on title reference format
        return GetEstimatedTitleData(titleReference);
    }

    /// <summary>
    /// Get title data from Landonline API (requires subscription).
    /// </summary>
    private async Task<LandData?> GetTitleFromLandonlineAsync(string titleReference, string apiKey, CancellationToken ct)
    {
        try
        {
            var baseUrl = _configuration["SiteEvaluator:Linz:LandonlineBaseUrl"] ?? "https://api.landonline.govt.nz";
            var url = $"{baseUrl}/v1/titles/{Uri.EscapeDataString(titleReference)}";
            
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            
            var response = await _httpClient.SendAsync(request, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Landonline title lookup failed: {StatusCode} for {Title}", 
                    response.StatusCode, titleReference);
                return null;
            }
            
            var result = await response.Content.ReadFromJsonAsync<LandonlineTitleResponse>(ct);
            if (result == null)
                return null;

            return MapLandonlineResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting title from Landonline: {TitleReference}", titleReference);
            return null;
        }
    }

    /// <summary>
    /// Map Landonline API response to our LandData model.
    /// </summary>
    private LandData MapLandonlineResponse(LandonlineTitleResponse response)
    {
        return new LandData
        {
            TitleReference = response.TitleReference,
            TitleType = response.Type,
            TitleStatus = response.Status,
            TitleDate = response.RegistrationDate,
            LegalDescription = response.LegalDescription,
            LotNumber = ExtractLotNumber(response.LegalDescription),
            DpNumber = ExtractDpNumber(response.LegalDescription),
            AreaSquareMeters = response.AreaUnit?.ToLower() == "sqm" ? response.Area : response.Area * 10000,
            AreaHectares = response.AreaUnit?.ToLower() == "ha" ? response.Area : response.Area / 10000,
            
            Owners = response.Owners?.Select(o => new Owner
            {
                Name = o.Name ?? "Unknown",
                Share = o.Share
            }).ToList() ?? [],
            
            Easements = response.Easements?.Select(e => new Easement
            {
                Type = e.Type ?? "Easement",
                Purpose = e.Purpose,
                InFavourOf = e.Benefiting,
                DocumentReference = e.InstrumentNumber
            }).ToList() ?? [],
            
            Covenants = response.Covenants?.Select(c => new Covenant
            {
                Type = c.Type ?? "Covenant",
                Description = c.Description,
                DocumentReference = c.InstrumentNumber
            }).ToList() ?? [],
            
            OtherEncumbrances = response.Encumbrances?.Select(e => new Encumbrance
            {
                Type = e.Type ?? "Encumbrance",
                Description = e.Mortgagee != null ? $"Mortgagee: {e.Mortgagee}" : null,
                DocumentReference = e.RegistrationNumber
            }).ToList() ?? [],
            
            Source = new DataSource
            {
                SourceName = "LINZ Landonline",
                SourceUrl = "https://www.landonline.govt.nz/",
                DataDate = DateTime.UtcNow,
                RetrievedDate = DateTime.UtcNow
            }
        };
    }

    /// <summary>
    /// Get estimated title data based on title reference format.
    /// Used when Landonline subscription is not available.
    /// </summary>
    private LandData? GetEstimatedTitleData(string titleReference)
    {
        if (string.IsNullOrWhiteSpace(titleReference))
            return null;

        // Parse title reference format (e.g., "CB45A/123", "NA123/456")
        var titleInfo = ParseTitleReference(titleReference);
        
        return new LandData
        {
            TitleReference = titleReference.ToUpperInvariant(),
            TitleType = titleInfo.TitleType,
            TitleStatus = "Live (Estimated)",
            LegalDescription = $"Title {titleReference} - full details require Landonline subscription",
            
            // Can't determine ownership without API access
            Owners = [],
            Easements = [],
            Covenants = [],
            OtherEncumbrances = [],
            
            Source = new DataSource
            {
                SourceName = "LINZ (Estimated)",
                SourceUrl = "https://www.linz.govt.nz/",
                DataDate = DateTime.UtcNow,
                RetrievedDate = DateTime.UtcNow,
                Notes = "Full title data requires Landonline subscription (~$500/year). " +
                        "Contact LINZ at https://www.linz.govt.nz/products-services/data to subscribe."
            }
        };
    }

    /// <summary>
    /// Parse title reference to extract land district and type information.
    /// </summary>
    private (string TitleType, string LandDistrict) ParseTitleReference(string titleReference)
    {
        // NZ title references typically follow format: XX123/456 or XX123A/456
        // Where XX is the land district code
        
        var upperRef = titleReference.ToUpperInvariant().Trim();
        
        // Extract land district prefix
        var match = Regex.Match(upperRef, @"^([A-Z]{2,3})");
        var prefix = match.Success ? match.Groups[1].Value : "";
        
        var landDistrict = prefix switch
        {
            "NA" => "North Auckland",
            "SA" => "South Auckland",
            "GS" => "Gisborne",
            "HB" => "Hawke's Bay",
            "TN" => "Taranaki",
            "WN" => "Wellington",
            "NL" => "Nelson",
            "ML" => "Marlborough",
            "WL" => "Westland",
            "CB" => "Canterbury",
            "OT" => "Otago",
            "SL" => "Southland",
            _ => "Unknown"
        };
        
        // Determine title type based on format
        var titleType = "Freehold"; // Most common
        if (upperRef.Contains("LEASE") || upperRef.Contains("LS"))
            titleType = "Leasehold";
        else if (upperRef.Contains("STRATUM") || upperRef.Contains("ST"))
            titleType = "Stratum in Freehold";
        else if (upperRef.Contains("UNIT"))
            titleType = "Unit Title";
        
        return (titleType, landDistrict);
    }

    /// <summary>
    /// Search for titles by address (requires Landonline subscription).
    /// </summary>
    public async Task<List<TitleSearchResult>> SearchTitlesAsync(string address, CancellationToken ct = default)
    {
        var landonlineApiKey = _configuration["SiteEvaluator:Linz:LandonlineApiKey"];
        
        if (string.IsNullOrEmpty(landonlineApiKey))
        {
            _logger.LogWarning("Landonline API key not configured - title search unavailable");
            return [];
        }

        try
        {
            var baseUrl = _configuration["SiteEvaluator:Linz:LandonlineBaseUrl"] ?? "https://api.landonline.govt.nz";
            var url = $"{baseUrl}/v1/titles/search?address={Uri.EscapeDataString(address)}";
            
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", landonlineApiKey);
            
            var response = await _httpClient.SendAsync(request, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Landonline title search failed: {StatusCode}", response.StatusCode);
                return [];
            }
            
            var result = await response.Content.ReadFromJsonAsync<LandonlineTitleSearchResponse>(ct);
            
            return result?.Titles?.Select(t => new TitleSearchResult
            {
                TitleReference = t.TitleReference ?? "",
                LegalDescription = t.LegalDescription,
                AreaSquareMeters = t.AreaUnit?.ToLower() == "sqm" ? t.Area : null,
                TitleType = t.Type,
                Status = t.Status
            }).ToList() ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching titles: {Address}", address);
            return [];
        }
    }

    /// <summary>
    /// Get ownership history for a title (requires Landonline subscription).
    /// </summary>
    public async Task<OwnershipHistory?> GetOwnershipHistoryAsync(string titleReference, CancellationToken ct = default)
    {
        var landonlineApiKey = _configuration["SiteEvaluator:Linz:LandonlineApiKey"];
        
        if (string.IsNullOrEmpty(landonlineApiKey))
        {
            _logger.LogWarning("Landonline API key not configured - ownership history unavailable");
            return null;
        }

        try
        {
            var baseUrl = _configuration["SiteEvaluator:Linz:LandonlineBaseUrl"] ?? "https://api.landonline.govt.nz";
            var url = $"{baseUrl}/v1/titles/{Uri.EscapeDataString(titleReference)}/history";
            
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", landonlineApiKey);
            
            var response = await _httpClient.SendAsync(request, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Landonline history lookup failed: {StatusCode}", response.StatusCode);
                return null;
            }
            
            var result = await response.Content.ReadFromJsonAsync<LandonlineHistoryResponse>(ct);
            if (result == null)
                return null;

            return new OwnershipHistory
            {
                TitleReference = titleReference,
                Transfers = result.Transfers?.Select(t => new OwnershipTransfer
                {
                    Date = t.Date,
                    FromOwner = t.From,
                    ToOwner = t.To,
                    Consideration = t.Consideration,
                    InstrumentNumber = t.InstrumentNumber
                }).ToList() ?? [],
                Source = new DataSource
                {
                    SourceName = "LINZ Landonline",
                    SourceUrl = "https://www.landonline.govt.nz/",
                    DataDate = DateTime.UtcNow,
                    RetrievedDate = DateTime.UtcNow
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ownership history: {TitleReference}", titleReference);
            return null;
        }
    }

    public async Task<List<Coordinate>?> GetParcelBoundaryAsync(string parcelId, CancellationToken ct = default)
    {
        try
        {
            // Use LINZ WFS service for parcel boundaries
            var url = $"/services/api/v1/wfs?service=WFS&version=2.0.0&request=GetFeature&typeName=layer-51571&CQL_FILTER=id='{parcelId}'&outputFormat=application/json";
            
            var response = await _httpClient.GetAsync(url, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("LINZ parcel boundary failed: {StatusCode}", response.StatusCode);
                return null;
            }
            
            var geoJson = await response.Content.ReadFromJsonAsync<GeoJsonFeatureCollection>(ct);
            var feature = geoJson?.Features?.FirstOrDefault();
            
            if (feature?.Geometry?.Coordinates == null)
                return null;

            return feature.Geometry.Coordinates
                .Select(c => new Coordinate { Latitude = c[1], Longitude = c[0] })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting parcel boundary: {ParcelId}", parcelId);
            return null;
        }
    }

    public async Task<List<AddressSuggestion>> GetAddressSuggestionsAsync(string query, CancellationToken ct = default)
    {
        try
        {
            // Use LINZ Address API for autocomplete suggestions
            var encodedQuery = Uri.EscapeDataString(query);
            var response = await _httpClient.GetAsync($"/services/api/v1/geocode?q={encodedQuery}&count=10", ct);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("LINZ autocomplete failed: {StatusCode}", response.StatusCode);
                return [];
            }
            
            var result = await response.Content.ReadFromJsonAsync<LinzGeocodeResponse>(ct);
            
            if (result?.Results == null)
                return [];

            return result.Results.Select(r => new AddressSuggestion
            {
                FullAddress = r.FullAddress ?? "",
                Suburb = r.Suburb,
                City = r.City,
                Latitude = r.Latitude,
                Longitude = r.Longitude
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting address suggestions for: {Query}", query);
            return [];
        }
    }

    /// <summary>
    /// Extract lot number from legal description.
    /// </summary>
    private static string? ExtractLotNumber(string? legalDescription)
    {
        if (string.IsNullOrWhiteSpace(legalDescription))
            return null;
        
        var match = Regex.Match(legalDescription, @"Lot\s+(\d+)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// Extract DP number from legal description.
    /// </summary>
    private static string? ExtractDpNumber(string? legalDescription)
    {
        if (string.IsNullOrWhiteSpace(legalDescription))
            return null;
        
        var match = Regex.Match(legalDescription, @"DP\s*(\d+)", RegexOptions.IgnoreCase);
        return match.Success ? $"DP {match.Groups[1].Value}" : null;
    }
}

// LINZ API response models
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

public class GeoJsonFeatureCollection
{
    public List<GeoJsonFeature>? Features { get; set; }
}

public class GeoJsonFeature
{
    public GeoJsonGeometry? Geometry { get; set; }
}

public class GeoJsonGeometry
{
    public string? Type { get; set; }
    public List<double[]>? Coordinates { get; set; }
}

// Landonline API response models
public class LandonlineTitleResponse
{
    public string? TitleReference { get; set; }
    public string? Status { get; set; }
    public string? Type { get; set; }
    public string? LegalDescription { get; set; }
    public double Area { get; set; }
    public string? AreaUnit { get; set; }
    public DateTime? RegistrationDate { get; set; }
    public List<LandonlineOwner>? Owners { get; set; }
    public List<LandonlineEncumbrance>? Encumbrances { get; set; }
    public List<LandonlineEasement>? Easements { get; set; }
    public List<LandonlineCovenant>? Covenants { get; set; }
    public List<LandonlineMemorial>? Memorials { get; set; }
}

public class LandonlineOwner
{
    public string? Name { get; set; }
    public string? Share { get; set; }
    public DateTime? RegistrationDate { get; set; }
}

public class LandonlineEncumbrance
{
    public string? Type { get; set; }
    public string? Mortgagee { get; set; }
    public string? RegistrationNumber { get; set; }
    public DateTime? RegistrationDate { get; set; }
}

public class LandonlineEasement
{
    public string? Type { get; set; }
    public string? Purpose { get; set; }
    public string? Benefiting { get; set; }
    public string? Burdened { get; set; }
    public string? InstrumentNumber { get; set; }
}

public class LandonlineCovenant
{
    public string? Type { get; set; }
    public string? Description { get; set; }
    public string? InstrumentNumber { get; set; }
}

public class LandonlineMemorial
{
    public string? Type { get; set; }
    public string? Description { get; set; }
    public DateTime? RegistrationDate { get; set; }
}

public class LandonlineTitleSearchResponse
{
    public List<LandonlineTitleSummary>? Titles { get; set; }
}

public class LandonlineTitleSummary
{
    public string? TitleReference { get; set; }
    public string? Status { get; set; }
    public string? Type { get; set; }
    public string? LegalDescription { get; set; }
    public double Area { get; set; }
    public string? AreaUnit { get; set; }
    public string? ParcelId { get; set; }
}

public class LandonlineHistoryResponse
{
    public string? TitleReference { get; set; }
    public List<LandonlineTransfer>? Transfers { get; set; }
}

public class LandonlineTransfer
{
    public DateTime? Date { get; set; }
    public string? From { get; set; }
    public string? To { get; set; }
    public string? Consideration { get; set; }
    public string? InstrumentNumber { get; set; }
}

// Additional result models
public class TitleSearchResult
{
    public string TitleReference { get; set; } = string.Empty;
    public string? LegalDescription { get; set; }
    public double? AreaSquareMeters { get; set; }
    public string? TitleType { get; set; }
    public string? Status { get; set; }
}

public class OwnershipHistory
{
    public string TitleReference { get; set; } = string.Empty;
    public List<OwnershipTransfer> Transfers { get; set; } = [];
    public DataSource? Source { get; set; }
}

public class OwnershipTransfer
{
    public DateTime? Date { get; set; }
    public string? FromOwner { get; set; }
    public string? ToOwner { get; set; }
    public string? Consideration { get; set; }
    public string? InstrumentNumber { get; set; }
}
