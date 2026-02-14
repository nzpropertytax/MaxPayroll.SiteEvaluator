using MaxPayroll.SiteEvaluator.Models;

namespace MaxPayroll.SiteEvaluator.Services.Integration;

/// <summary>
/// Interface for LINZ data integration.
/// Includes both free LINZ Data Service and paid Landonline APIs.
/// </summary>
public interface ILinzDataService
{
    // === Free LINZ Data Service APIs ===
    
    /// <summary>
    /// Geocode an address to get location details.
    /// </summary>
    Task<SiteLocation?> LookupAddressAsync(string address, CancellationToken ct = default);
    
    /// <summary>
    /// Get address autocomplete suggestions.
    /// </summary>
    Task<List<AddressSuggestion>> GetAddressSuggestionsAsync(string query, CancellationToken ct = default);
    
    /// <summary>
    /// Get parcel boundary coordinates.
    /// </summary>
    Task<List<Coordinate>?> GetParcelBoundaryAsync(string parcelId, CancellationToken ct = default);
    
    // === Landonline APIs (requires subscription) ===
    
    /// <summary>
    /// Get full title data including ownership and encumbrances.
    /// Returns estimated data if Landonline subscription not configured.
    /// </summary>
    Task<LandData?> GetTitleDataAsync(string titleReference, CancellationToken ct = default);
    
    /// <summary>
    /// Search for titles by address.
    /// Requires Landonline subscription.
    /// </summary>
    Task<List<TitleSearchResult>> SearchTitlesAsync(string address, CancellationToken ct = default);
    
    /// <summary>
    /// Get ownership transfer history for a title.
    /// Requires Landonline subscription.
    /// </summary>
    Task<OwnershipHistory?> GetOwnershipHistoryAsync(string titleReference, CancellationToken ct = default);
}

/// <summary>
/// Address suggestion for autocomplete.
/// </summary>
public class AddressSuggestion
{
    public string FullAddress { get; set; } = string.Empty;
    public string? Suburb { get; set; }
    public string? City { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

/// <summary>
/// Interface for NZGD geotechnical data.
/// </summary>
public interface INzgdDataService
{
    Task<List<NearbyBorehole>> GetNearbyBoreholesAsync(double lat, double lon, double radiusMeters, CancellationToken ct = default);
    Task<List<NearbyCpt>> GetNearbyCptsAsync(double lat, double lon, double radiusMeters, CancellationToken ct = default);
    Task<List<NearbyGeotechReport>> GetNearbyReportsAsync(double lat, double lon, double radiusMeters, CancellationToken ct = default);
}

/// <summary>
/// Interface for council data (multiple implementations per council).
/// </summary>
public interface ICouncilDataService
{
    string CouncilName { get; }
    bool SupportsRegion(double lat, double lon);
    
    Task<ZoningData?> GetZoningDataAsync(double lat, double lon, CancellationToken ct = default);
    Task<HazardData?> GetHazardDataAsync(double lat, double lon, CancellationToken ct = default);
    Task<InfrastructureData?> GetInfrastructureDataAsync(double lat, double lon, CancellationToken ct = default);
}

/// <summary>
/// Interface for GNS Science seismic/fault data.
/// </summary>
public interface IGnsDataService
{
    Task<SeismicHazard?> GetSeismicHazardAsync(double lat, double lon, CancellationToken ct = default);
    Task<List<ActiveFault>> GetNearbyFaultsAsync(double lat, double lon, double radiusKm, CancellationToken ct = default);
}

/// <summary>
/// Interface for NIWA climate data.
/// </summary>
public interface INiwaDataService
{
    Task<RainfallData?> GetRainfallDataAsync(double lat, double lon, CancellationToken ct = default);
    Task<string?> GetWindZoneAsync(double lat, double lon, CancellationToken ct = default);
}
