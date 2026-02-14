namespace MaxPayroll.SiteEvaluator.Models;

/// <summary>
/// Natural hazard information from council, GNS, and other sources.
/// </summary>
public class HazardData
{
    // === Flooding ===
    public FloodHazard? Flooding { get; set; }
    
    // === Liquefaction ===
    public LiquefactionHazard? Liquefaction { get; set; }
    
    // === Seismic ===
    public SeismicHazard? Seismic { get; set; }
    
    // === Other Hazards ===
    public bool CoastalErosion { get; set; }
    public bool CoastalInundation { get; set; }
    public bool SlopeInstability { get; set; }
    public bool Subsidence { get; set; }
    public bool Wildfire { get; set; }
    
    // === Contamination ===
    public ContaminationStatus? Contamination { get; set; }
    
    // === Summary ===
    public List<HazardSummary> AllHazards { get; set; } = [];
    
    public DataSource Source { get; set; } = new();
}

public class FloodHazard
{
    public string Zone { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? FloodLevel { get; set; }  // e.g., "1% AEP"
    public double? FloorLevelRequirement { get; set; }
    public bool RequiresFloodAssessment { get; set; }
}

public class LiquefactionHazard
{
    public string Category { get; set; } = string.Empty;  // TC1, TC2, TC3
    public string Description { get; set; } = string.Empty;
    public string? FoundationGuidance { get; set; }
    public bool RequiresGeotechAssessment { get; set; }
}

public class SeismicHazard
{
    public string Zone { get; set; } = string.Empty;
    public double? PGA { get; set; }  // Peak Ground Acceleration
    public List<ActiveFault> NearbyFaults { get; set; } = [];
}

public class ActiveFault
{
    public string Name { get; set; } = string.Empty;
    public double DistanceKm { get; set; }
    public string? RecurrenceInterval { get; set; }
    public string? LastRupture { get; set; }
}

public class ContaminationStatus
{
    public bool OnHail { get; set; }  // Hazardous Activities and Industries List
    public bool OnLlur { get; set; }  // Listed Land Use Register
    public string? Status { get; set; }
    public string? Description { get; set; }
}

public class HazardSummary
{
    public string HazardType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;  // Low, Medium, High
    public string Description { get; set; } = string.Empty;
    public string? Action { get; set; }
}
