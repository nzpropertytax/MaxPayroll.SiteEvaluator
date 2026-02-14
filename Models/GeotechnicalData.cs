namespace MaxPayroll.SiteEvaluator.Models;

/// <summary>
/// Geotechnical data from NZGD and other sources.
/// </summary>
public class GeotechnicalData
{
    // === Site Geotechnical Classification ===
    public string? SiteClass { get; set; }  // NZS 1170.5 Site Class A-E
    public string? SiteClassSource { get; set; }
    
    // === Nearby Investigation Data ===
    public List<NearbyBorehole> NearbyBoreholes { get; set; } = [];
    public List<NearbyCpt> NearbyCpts { get; set; } = [];
    public List<NearbyGeotechReport> NearbyReports { get; set; } = [];
    
    // === Ground Conditions Summary ===
    public string? SoilDescription { get; set; }
    public double? EstimatedGroundwaterLevel { get; set; }
    public string? FoundationRecommendation { get; set; }
    
    // === Assessment Required ===
    public bool GeotechInvestigationRequired { get; set; }
    public string? RecommendedInvestigation { get; set; }
    
    public DataSource Source { get; set; } = new();
}

public class NearbyBorehole
{
    public string Id { get; set; } = string.Empty;
    public string? NzgdId { get; set; }
    public double DistanceMeters { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Depth { get; set; }
    public DateTime? Date { get; set; }
    public string? Description { get; set; }
    public string? SourceUrl { get; set; }
    
    // Simplified soil profile
    public List<SoilLayer> SoilLayers { get; set; } = [];
}

public class SoilLayer
{
    public double TopDepth { get; set; }
    public double BottomDepth { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? SoilType { get; set; }
}

public class NearbyCpt
{
    public string Id { get; set; } = string.Empty;
    public double DistanceMeters { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Depth { get; set; }
    public DateTime? Date { get; set; }
    public string? SourceUrl { get; set; }
}

public class NearbyGeotechReport
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public double DistanceMeters { get; set; }
    public string? Author { get; set; }
    public DateTime? Date { get; set; }
    public string? SourceUrl { get; set; }
}
