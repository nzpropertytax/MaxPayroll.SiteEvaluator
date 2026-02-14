namespace MaxPayroll.SiteEvaluator.Models;

/// <summary>
/// Complete site evaluation containing all gathered data.
/// </summary>
public class SiteEvaluation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? LastUpdated { get; set; }
    
    // === Location Identification ===
    public SiteLocation Location { get; set; } = new();
    
    // === Data Sections ===
    public ZoningData? Zoning { get; set; }
    public HazardData? Hazards { get; set; }
    public GeotechnicalData? Geotech { get; set; }
    public InfrastructureData? Infrastructure { get; set; }
    public LandData? Land { get; set; }
    public ClimateData? Climate { get; set; }
    public HistoricalData? Historical { get; set; }
    
    // === Status ===
    public EvaluationStatus Status { get; set; } = EvaluationStatus.InProgress;
    public DataCompleteness Completeness { get; set; } = new();
    public List<DataGap> DataGaps { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}

public class SiteLocation
{
    public string Address { get; set; } = string.Empty;
    public string LegalDescription { get; set; } = string.Empty;
    public string? TitleReference { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Suburb { get; set; }
    public string? City { get; set; }
    public string? TerritorialAuthority { get; set; }
    public string? RegionalCouncil { get; set; }
    
    // Boundary polygon for mapping
    public List<Coordinate>? Boundary { get; set; }
}

public class Coordinate
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public enum EvaluationStatus
{
    InProgress,
    Complete,
    RequiresManualData,
    Error
}

/// <summary>
/// Tracks data source and currency.
/// </summary>
public class DataSource
{
    public string SourceName { get; set; } = string.Empty;
    public string? SourceUrl { get; set; }
    public DateTime DataDate { get; set; }
    public DateTime RetrievedDate { get; set; } = DateTime.UtcNow;
    public string? ApiVersion { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Tracks data completeness for the evaluation.
/// </summary>
public class DataCompleteness
{
    public int TotalSections { get; set; } = 7;
    public int CompleteSections { get; set; }
    public int PartialSections { get; set; }
    public int MissingSections { get; set; }
    
    public double CompletionPercentage => 
        TotalSections > 0 ? (CompleteSections + PartialSections * 0.5) / TotalSections * 100 : 0;
    
    public Dictionary<string, SectionCompleteness> Sections { get; set; } = [];
}

public class SectionCompleteness
{
    public string Section { get; set; } = string.Empty;
    public CompletenessStatus Status { get; set; }
    public int TotalFields { get; set; }
    public int PopulatedFields { get; set; }
    public string? Notes { get; set; }
}

public enum CompletenessStatus
{
    Complete,
    Partial,
    Missing,
    Error,
    NotApplicable
}

/// <summary>
/// Identifies gaps in data that require manual action.
/// </summary>
public class DataGap
{
    public string Section { get; set; } = string.Empty;
    public string Field { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? SuggestedAction { get; set; }
    public GapSeverity Severity { get; set; }
}

public enum GapSeverity
{
    Low,      // Nice to have
    Medium,   // Should be obtained
    High,     // Required for assessment
    Critical  // Cannot proceed without
}

/// <summary>
/// Historical site data.
/// </summary>
public class HistoricalData
{
    public List<HistoricalImage> AerialImages { get; set; } = [];
    public string? PreviousUse { get; set; }
    public List<string> PreviousConsents { get; set; } = [];
    public DataSource Source { get; set; } = new();
}

public class HistoricalImage
{
    public int Year { get; set; }
    public string? Source { get; set; }
    public string? Url { get; set; }
    public string? Description { get; set; }
}
