namespace MaxPayroll.SiteEvaluator.Models;

/// <summary>
/// A unique property location. Multiple jobs can reference the same location.
/// Location data is shared to avoid redundant API calls.
/// </summary>
public class PropertyLocation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    // === Primary Identifiers ===
    
    /// <summary>
    /// Full formatted address
    /// </summary>
    public string Address { get; set; } = string.Empty;
    
    /// <summary>
    /// LINZ title reference (e.g., "CB123/456")
    /// </summary>
    public string? TitleReference { get; set; }
    
    /// <summary>
    /// Legal description (e.g., "Lot 1 DP 12345")
    /// </summary>
    public string? LegalDescription { get; set; }
    
    /// <summary>
    /// Valuation reference number
    /// </summary>
    public string? ValuationReference { get; set; }
    
    // === Coordinates ===
    
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    
    /// <summary>
    /// Boundary polygon (if available)
    /// </summary>
    public List<Coordinate>? Boundary { get; set; }
    
    /// <summary>
    /// Site area in m² (if known)
    /// </summary>
    public double? SiteAreaM2 { get; set; }
    
    // === Address Components ===
    
    public string? StreetNumber { get; set; }
    public string? StreetName { get; set; }
    public string? Suburb { get; set; }
    public string? City { get; set; }
    public string? PostCode { get; set; }
    
    // === Administrative Boundaries ===
    
    /// <summary>
    /// Territorial authority (e.g., "Christchurch City Council")
    /// </summary>
    public string? TerritorialAuthority { get; set; }
    
    /// <summary>
    /// Regional council (e.g., "Environment Canterbury")
    /// </summary>
    public string? RegionalCouncil { get; set; }
    
    /// <summary>
    /// Ward (if applicable)
    /// </summary>
    public string? Ward { get; set; }
    
    // === Tracking ===
    
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? LastUpdated { get; set; }
    
    /// <summary>
    /// Source of the location data (LINZ, manual entry, etc.)
    /// </summary>
    public string Source { get; set; } = "LINZ";
    
    /// <summary>
    /// Confidence score for the geocode match (0-100)
    /// </summary>
    public int? GeocodeConfidence { get; set; }
    
    // === Cached Data Snapshots ===
    // These are cached to avoid repeated API calls
    // Each job can refresh if needed
    
    /// <summary>
    /// Last retrieved zoning data
    /// </summary>
    public ZoningData? CachedZoning { get; set; }
    public DateTime? ZoningCachedAt { get; set; }
    
    /// <summary>
    /// Last retrieved hazard data
    /// </summary>
    public HazardData? CachedHazards { get; set; }
    public DateTime? HazardsCachedAt { get; set; }
    
    /// <summary>
    /// Last retrieved geotechnical data
    /// </summary>
    public GeotechnicalData? CachedGeotech { get; set; }
    public DateTime? GeotechCachedAt { get; set; }
    
    /// <summary>
    /// Last retrieved infrastructure data
    /// </summary>
    public InfrastructureData? CachedInfrastructure { get; set; }
    public DateTime? InfrastructureCachedAt { get; set; }
    
    /// <summary>
    /// Last retrieved climate data
    /// </summary>
    public ClimateData? CachedClimate { get; set; }
    public DateTime? ClimateCachedAt { get; set; }
    
    /// <summary>
    /// Last retrieved land/title data
    /// </summary>
    public LandData? CachedLand { get; set; }
    public DateTime? LandCachedAt { get; set; }
    
    // === Helper Methods ===
    
    /// <summary>
    /// Check if cached data is stale (older than specified hours)
    /// </summary>
    public bool IsCacheStale(string section, int maxAgeHours = 24)
    {
        var cachedAt = section.ToLowerInvariant() switch
        {
            "zoning" => ZoningCachedAt,
            "hazards" => HazardsCachedAt,
            "geotech" => GeotechCachedAt,
            "infrastructure" => InfrastructureCachedAt,
            "climate" => ClimateCachedAt,
            "land" => LandCachedAt,
            _ => null
        };
        
        if (cachedAt == null) return true;
        return (DateTime.UtcNow - cachedAt.Value).TotalHours > maxAgeHours;
    }
    
    /// <summary>
    /// Get a short display string for this location
    /// </summary>
    public string GetShortAddress()
    {
        if (!string.IsNullOrEmpty(Suburb))
            return $"{StreetNumber} {StreetName}, {Suburb}";
        return Address.Length > 50 ? Address[..47] + "..." : Address;
    }
}

/// <summary>
/// Summary of a location for list views
/// </summary>
public class LocationSummary
{
    public string Id { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? TitleReference { get; set; }
    public string? Suburb { get; set; }
    public string? City { get; set; }
    public string? TerritorialAuthority { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int JobCount { get; set; }
    public DateTime? LastJobDate { get; set; }
}
