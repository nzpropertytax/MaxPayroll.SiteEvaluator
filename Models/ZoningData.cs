namespace MaxPayroll.SiteEvaluator.Models;

/// <summary>
/// District Plan zoning and planning data.
/// </summary>
public class ZoningData
{
    // === Zone Information ===
    public string Zone { get; set; } = string.Empty;
    public string ZoneCode { get; set; } = string.Empty;
    public string ZoneDescription { get; set; } = string.Empty;
    public string DistrictPlan { get; set; } = string.Empty;
    
    // === Built Form Standards ===
    public double? MaxHeight { get; set; }
    public double? MaxCoverage { get; set; }
    public double? MinFrontSetback { get; set; }
    public double? MinSideSetback { get; set; }
    public double? MinRearSetback { get; set; }
    public double? MaxImpervious { get; set; }
    
    // === Density Controls ===
    public string? DensityStandard { get; set; }
    public int? MaxUnitsPerSite { get; set; }
    public double? MinSiteArea { get; set; }
    public double? MinNetSiteArea { get; set; }
    
    // === Activity Status ===
    public List<string> PermittedActivities { get; set; } = [];
    public List<string> ControlledActivities { get; set; } = [];
    public List<string> RestrictedDiscretionary { get; set; } = [];
    public List<string> DiscretionaryActivities { get; set; } = [];
    public List<string> NonComplying { get; set; } = [];
    public List<string> ProhibitedActivities { get; set; } = [];
    
    // === Overlays ===
    public List<PlanningOverlay> Overlays { get; set; } = [];
    
    // === Links ===
    public string? DistrictPlanLink { get; set; }
    public string? ZoneRulesLink { get; set; }
    
    // === Data Provenance ===
    public DataSource Source { get; set; } = new();
}

public class PlanningOverlay
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? RulesLink { get; set; }
}
