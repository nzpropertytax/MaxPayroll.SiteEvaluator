using MaxPayroll.SiteEvaluator.Models;

namespace MaxPayroll.SiteEvaluator.Services.Integration;

/// <summary>
/// Integration with LINZ Data Service API.
/// https://data.linz.govt.nz/
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
        
        // Set up authentication header
        var apiKey = _configuration["Linz:ApiKey"];
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
        // LINZ Title API requires LANDONLINE subscription
        // For MVP, this is a placeholder
        _logger.LogInformation("Title lookup requested for: {TitleReference}", titleReference);
        
        // TODO: Implement when LANDONLINE access is available
        return await Task.FromResult<LandData?>(null);
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
                return new List<AddressSuggestion>();
            }
            
            var result = await response.Content.ReadFromJsonAsync<LinzGeocodeResponse>(ct);
            
            if (result?.Results == null)
                return new List<AddressSuggestion>();

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
            return new List<AddressSuggestion>();
        }
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
