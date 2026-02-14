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
    public double? ZoneFactor { get; set; }  // NZS 1170.5 Z-value
    public string? SiteClass { get; set; }  // A, B, C, D, E per NZS 1170.5
    public double? NearFaultFactor { get; set; }  // N-value per NZS 1170.5
    public double? PGA { get; set; }  // Peak Ground Acceleration
    public double? PgaSls { get; set; }  // PGA for Serviceability Limit State (500-year)
    public double? PgaUls { get; set; }  // PGA for Ultimate Limit State (2500-year)
    public string? DesignStandard { get; set; }  // e.g., "NZS 1170.5:2004"
    public List<ActiveFault> NearbyFaults { get; set; } = [];
    public DataSource? Source { get; set; }
}

public class ActiveFault
{
    public string Name { get; set; } = string.Empty;
    public double DistanceKm { get; set; }
    public string? RecurrenceInterval { get; set; }
    public string? LastRupture { get; set; }
    public string? FaultType { get; set; }  // e.g., "Dextral strike-slip", "Reverse"
    public string? SlipRate { get; set; }  // e.g., "27 mm/year"
    public double? MaxMagnitude { get; set; }  // Maximum expected magnitude
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
